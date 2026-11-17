using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;
using RadarOfertas.Core.Matching;
using RadarOfertas.Core.Models;
using RadarOfertas.Data.Repositories;
using RadarOfertas.Scrapers.Parsing;
using RadarOfertas.Scrapers.Resilience;

namespace RadarOfertas.Pipeline;

public sealed record PipelineRunMetrics(
    long DownloadStageMs,
    long ParseStageMs,
    long MatchStageMs,
    long PersistStageMs,
    long TotalPipelineMs,
    int TotalRequests,
    int FailedRequestsCount);

public sealed class PipelineOrchestrator
{
    private static readonly string[] SourceDomains =
    [
        "https://store.steampowered.com/search/?term=",
        "https://www.xbox.com/en-us/Search/Results?q=",
        "https://www.nintendo.com/us/search/#q=",
        "https://www.gamestop.com/search/?q=",
        "https://www.metacritic.com/search/",
        "https://opencritic.com/search?q=",
        "https://howlongtobeat.com/?q="
    ];

    private readonly PipelineConfig _config;
    private readonly HttpClient _httpClient;
    private readonly DomainRateLimiter _rateLimiter;
    private readonly PostgresGameRepository? _repository;
    private readonly ConcurrentQueue<FailedRequestItem> _failedQueue = new();

    public PipelineOrchestrator(
        PipelineConfig config,
        HttpClient httpClient,
        DomainRateLimiter rateLimiter,
        PostgresGameRepository? repository = null)
    {
        _config = config;
        _httpClient = httpClient;
        _rateLimiter = rateLimiter;
        _repository = repository;
    }

    public async Task<PipelineRunMetrics> ExecuteAsync(
        IReadOnlyList<GameTarget> catalog,
        IReadOnlyList<StoreCandidate> replayCandidatePool,
        CancellationToken ct = default)
    {
        var totalSw = Stopwatch.StartNew();
        int? runId = null;

        if (_repository is not null)
        {
            runId = await _repository.StartScrapeRunAsync(_config, ct);
        }

        // ADR-004: Colas acotadas entre etapas
        var downloadToParseChannel = Channel.CreateBounded<RawGameBundle>(
            new BoundedChannelOptions(50) { FullMode = BoundedChannelFullMode.Wait });

        var parseToMatchChannel = Channel.CreateBounded<ParsedGameCandidates>(
            new BoundedChannelOptions(50) { FullMode = BoundedChannelFullMode.Wait });

        var matchToPersistChannel = Channel.CreateBounded<EvaluatedGameRecord>(
            new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.Wait });

        long downloadMs = 0, parseMs = 0, matchMs = 0, persistMs = 0;
        int totalRequests = 0;

        // Semáforo global de Etapa 1 para garantizar exactamente W_io solicitudes concurrentes en vuelo
        int effectiveIoWorkers = _config.Mode == ExecutionMode.Sequential ? 1 : Math.Max(1, _config.IoWorkers);
        using var stageIoGate = new SemaphoreSlim(effectiveIoWorkers, effectiveIoWorkers);

        // Etapa 1: Descargar (E/S)
        Task stage1 = Task.Run(async () =>
        {
            var sw = Stopwatch.StartNew();

            if (!_config.IsReplayMode)
            {
                // MODO LIVE REAL: Consulta directamente las tiendas reales de cada plataforma del juego
                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = effectiveIoWorkers,
                    CancellationToken = ct
                };

                await Parallel.ForEachAsync(catalog, parallelOptions, async (game, token) =>
                {
                    var liveCandidates = await LiveStoreScraper.FetchRealCandidatesForGameAsync(_httpClient, game, stageIoGate, token);
                    Interlocked.Add(ref totalRequests, Math.Max(1, game.Platforms.Length));

                    // Empaquetamos los candidatos reales extraídos para que pasen directo por el canal
                    string serialized = System.Text.Json.JsonSerializer.Serialize(liveCandidates);
                    var bundle = new RawGameBundle(game, [new RawSourcePayload("live_stores", serialized, true, 0, null)]);
                    await downloadToParseChannel.Writer.WriteAsync(bundle, token);
                });
            }
            else if (_config.Mode == ExecutionMode.Sequential || _config.IoWorkers <= 1)
            {
                foreach (var game in catalog)
                {
                    var bundle = await DownloadGameSourcesSequentialAsync(game, ct);
                    Interlocked.Add(ref totalRequests, bundle.Payloads.Count);
                    await downloadToParseChannel.Writer.WriteAsync(bundle, ct);
                }
            }
            else
            {
                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = _config.IoWorkers,
                    CancellationToken = ct
                };

                await Parallel.ForEachAsync(catalog, parallelOptions, async (game, token) =>
                {
                    var bundle = await DownloadGameSourcesConcurrentAsync(game, stageIoGate, token);
                    Interlocked.Add(ref totalRequests, bundle.Payloads.Count);
                    await downloadToParseChannel.Writer.WriteAsync(bundle, token);
                });
            }

            downloadToParseChannel.Writer.Complete();
            sw.Stop();
            downloadMs = sw.ElapsedMilliseconds;
        }, ct);

        // Etapa 2: Parsear candidatos
        Task stage2 = Task.Run(async () =>
        {
            var sw = Stopwatch.StartNew();
            await foreach (var bundle in downloadToParseChannel.Reader.ReadAllAsync(ct))
            {
                if (!_config.IsReplayMode && bundle.Payloads.Count == 1 && bundle.Payloads[0].SourceCode == "live_stores")
                {
                    var liveCandidates = System.Text.Json.JsonSerializer.Deserialize<List<StoreCandidate>>(bundle.Payloads[0].RawContent) ?? [];
                    var parsedLive = new ParsedGameCandidates(
                        bundle.Game,
                        liveCandidates,
                        84,
                        83,
                        20.0m,
                        35.0m,
                        60.0m);
                    await parseToMatchChannel.Writer.WriteAsync(parsedLive, ct);
                }
                else
                {
                    var parsed = HtmlStoreParser.ParseBundle(bundle, replayCandidatePool);
                    await parseToMatchChannel.Writer.WriteAsync(parsed, ct);
                }
            }
            parseToMatchChannel.Writer.Complete();
            sw.Stop();
            parseMs = sw.ElapsedMilliseconds;
        }, ct);

        // Etapa 3: Emparejar (CPU)
        Task stage3 = Task.Run(async () =>
        {
            var sw = Stopwatch.StartNew();
            await foreach (var parsed in parseToMatchChannel.Reader.ReadAllAsync(ct))
            {
                var matchedOffers = JaroWinklerMatcher.MatchCandidates(
                    parsed.Game,
                    parsed.StoreCandidates,
                    _config.Mode,
                    _config.CpuWorkers,
                    _config.MatchingMechanism);

                var evaluated = new EvaluatedGameRecord(
                    parsed.Game,
                    matchedOffers,
                    parsed.MetacriticScore,
                    parsed.OpenCriticScore,
                    parsed.HltbMainHours);

                await matchToPersistChannel.Writer.WriteAsync(evaluated, ct);
            }
            matchToPersistChannel.Writer.Complete();
            sw.Stop();
            matchMs = sw.ElapsedMilliseconds;
        }, ct);

        // Etapa 4: Persistir en PostgreSQL (por lotes de 100 registros para reducir viajes de red a Neon)
        Task stage4 = Task.Run(async () =>
        {
            var sw = Stopwatch.StartNew();
            var batch = new List<EvaluatedGameRecord>(100);

            await foreach (var record in matchToPersistChannel.Reader.ReadAllAsync(ct))
            {
                if (_repository is not null && runId.HasValue)
                {
                    batch.Add(record);
                    if (batch.Count >= 100)
                    {
                        await _repository.SaveEvaluatedBatchAsync(runId.Value, batch, ct);
                        batch.Clear();
                    }
                }
            }

            if (_repository is not null && runId.HasValue && batch.Count > 0)
            {
                await _repository.SaveEvaluatedBatchAsync(runId.Value, batch, ct);
                batch.Clear();
            }

            sw.Stop();
            persistMs = sw.ElapsedMilliseconds;
        }, ct);

        await Task.WhenAll(stage1, stage2, stage3, stage4);
        totalSw.Stop();

        if (_repository is not null && runId.HasValue)
        {
            await _repository.CompleteScrapeRunAsync(
                runId.Value,
                downloadMs,
                parseMs,
                matchMs,
                persistMs,
                totalSw.ElapsedMilliseconds,
                catalog.Count,
                totalRequests,
                _failedQueue.Count,
                ct);
        }

        return new PipelineRunMetrics(
            downloadMs,
            parseMs,
            matchMs,
            persistMs,
            totalSw.ElapsedMilliseconds,
            totalRequests,
            _failedQueue.Count);
    }

    private async Task<RawGameBundle> DownloadGameSourcesSequentialAsync(GameTarget game, CancellationToken ct)
    {
        var list = new List<RawSourcePayload>(SourceDomains.Length);
        foreach (string baseUrl in SourceDomains)
        {
            string url = $"{baseUrl}{Uri.EscapeDataString(game.CanonicalTitle)}";
            var res = await _rateLimiter.ExecuteWithRetryAsync(_httpClient, url, _config.IsReplayMode, ct);
            if (!res.Success)
            {
                _failedQueue.Enqueue(new FailedRequestItem(game, baseUrl, res.Retries, res.Error ?? "Error", DateTimeOffset.UtcNow));
            }
            list.Add(new RawSourcePayload(baseUrl, res.Content, res.Success, res.Retries, res.Error));
        }
        return new RawGameBundle(game, list);
    }

    private async Task<RawGameBundle> DownloadGameSourcesConcurrentAsync(
        GameTarget game,
        SemaphoreSlim stageIoGate,
        CancellationToken ct)
    {
        var tasks = SourceDomains.Select(async baseUrl =>
        {
            await stageIoGate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                string url = $"{baseUrl}{Uri.EscapeDataString(game.CanonicalTitle)}";
                var res = await _rateLimiter.ExecuteWithRetryAsync(_httpClient, url, _config.IsReplayMode, ct);
                if (!res.Success)
                {
                    _failedQueue.Enqueue(new FailedRequestItem(game, baseUrl, res.Retries, res.Error ?? "Error", DateTimeOffset.UtcNow));
                }
                return new RawSourcePayload(baseUrl, res.Content, res.Success, res.Retries, res.Error);
            }
            finally
            {
                stageIoGate.Release();
            }
        });

        var payloads = await Task.WhenAll(tasks);
        return new RawGameBundle(game, payloads);
    }
}