using RadarOfertas.Benchmarks;
using RadarOfertas.Core.Matching;
using RadarOfertas.Core.Models;
using RadarOfertas.Data.Repositories;
using RadarOfertas.Pipeline;
using RadarOfertas.Scrapers.Discovery;
using RadarOfertas.Scrapers.Replay;
using RadarOfertas.Scrapers.Resilience;

var mode = args.Contains("--parallel") ? ExecutionMode.Parallel
         : args.Contains("--concurrent") ? ExecutionMode.Concurrent
         : ExecutionMode.Sequential;

var mechanism = args.Contains("--plinq") ? CpuMechanism.Plinq : CpuMechanism.ParallelForEach;
bool isLive = args.Contains("--live");
bool useDb = args.Contains("--use-db") || args.Contains("--discover") || args.Contains("--init-db");

int ioWorkers = GetIntArg(args, "--io-workers", 32);
int cpuWorkers = GetIntArg(args, "--cpu-workers", Environment.ProcessorCount);
int latencyMs = GetIntArg(args, "--latency", 120);
int gamesCount = GetIntArg(args, "--games", 500);

string connString = GetStringArg(args, "--conn",
    Environment.GetEnvironmentVariable("RADAR_DB_CONN")
    ?? "Host=localhost;Port=5432;Database=radar_ofertas;Username=postgres;Password=postgres");

var config = new PipelineConfig
{
    Mode = mode,
    MatchingMechanism = mechanism,
    IoWorkers = ioWorkers,
    CpuWorkers = cpuWorkers,
    IsReplayMode = !isLive,
    SimulatedLatencyMs = latencyMs,
    ConnectionString = connString
};

HttpMessageHandler handler = config.IsReplayMode
    ? new ReplayHttpMessageHandler(config)
    : new HttpClientHandler();

using var httpClient = new HttpClient(handler);
httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("RadarOfertasMultinucleo-TEC/1.0 (+https://github.com/Esqui0A0I07/Ultimo-proyecto-Arquitectura)");

var rateLimiter = new DomainRateLimiter();
var repo = new PostgresGameRepository(config.ConnectionString);

if (args.Contains("--init-db"))
{
    Console.WriteLine("Inicializando esquema de base de datos en PostgreSQL...");
    await repo.InitializeDatabaseAsync();
    Console.WriteLine("Esquema y tiendas base creados correctamente.");
    return;
}

if (args.Contains("--discover"))
{
    Console.WriteLine("Ejecutando Etapa 0: Descubrimiento paginado en OpenCritic...");
    await repo.InitializeDatabaseAsync();
    var discoveryScraper = new OpenCriticDiscoveryScraper(httpClient, rateLimiter, config.IsReplayMode);
    var discovered = await discoveryScraper.DiscoverCatalogAsync(gamesCount);
    int saved = await repo.UpsertDiscoveredGamesAsync(discovered);
    int hardCases = discovered.Count(g => g.IsHardCase);
    Console.WriteLine($"Catálogo poblado en PostgreSQL: {saved} juegos procesados | Casos difíciles incluidos: {hardCases}");
    return;
}

if (args.Contains("--contention"))
{
    Console.WriteLine($"Ejecutando Experimento de Contención en {cpuWorkers} hilos...");
    var results = ContentionExperiment.RunAll(2_000_000, cpuWorkers);
    foreach (var r in results)
    {
        Console.WriteLine($"{r.Mechanism,-42} | {r.ElapsedMs,6} ms | Esperado: {r.ExpectedTotal:N0} | Real: {r.ActualTotal:N0}");
    }
    return;
}

IReadOnlyList<GameTarget> catalog;
if (useDb)
{
    catalog = await repo.GetCatalogAsync(gamesCount);
    if (catalog.Count == 0)
    {
        Console.WriteLine("La base de datos está vacía. Poblando catálogo inicial con Etapa 0...");
        var discoveryScraper = new OpenCriticDiscoveryScraper(httpClient, rateLimiter, config.IsReplayMode);
        var discovered = await discoveryScraper.DiscoverCatalogAsync(Math.Max(520, gamesCount));
        await repo.UpsertDiscoveredGamesAsync(discovered);
        catalog = await repo.GetCatalogAsync(gamesCount);
    }
    Console.WriteLine($"Catálogo leído desde PostgreSQL: {catalog.Count} juegos ({catalog.Count(g => g.IsHardCase)} casos difíciles).");
}
else
{
    var discoveryScraper = new OpenCriticDiscoveryScraper(httpClient, rateLimiter, config.IsReplayMode);
    catalog = (await discoveryScraper.DiscoverCatalogAsync(gamesCount)).Take(gamesCount).ToList();
}

if (args.Contains("--images"))
{
    int reps = GetIntArg(args, "--reps", 5);
    _ = BenchmarkRunner.RunCoverImageProcessingSweep(catalog, reps, out var coverResults);

    if (useDb && coverResults.Count > 0)
    {
        Console.WriteLine("Guardando colores dominantes y hashes perceptuales (dHash) en PostgreSQL...");
        await repo.UpdateCoverAnalysisBatchAsync(coverResults);
        Console.WriteLine($"Actualizados {coverResults.Count} juegos en PostgreSQL.");
    }

    if (args.Contains("--publish-web"))
    {
        string webDataDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../web/public/data"));
        var exporter = new StaticSiteExporter(config.ConnectionString);
        await exporter.ExportPublicSiteDataAsync(webDataDir);
        Console.WriteLine($"[Etapa 5 · Publicar] catalog.json actualizado en: {webDataDir}");
    }
    return;
}

if (args.Contains("--benchmark"))
{
    int reps = GetIntArg(args, "--reps", 5);
    int poolSize = GetIntArg(args, "--candidates", 2500);
    await BenchmarkRunner.RunFullExperimentalSuiteAsync(catalog, reps, latencyMs, poolSize);
    return;
}

if (args.Contains("--predict"))
{
    int poolSize = GetIntArg(args, "--candidates", 2500);
    await BenchmarkRunner.RunInteractiveLivePredictionAsync(catalog, ioWorkers, cpuWorkers, latencyMs, poolSize);
    return;
}

var replayCandidates = Enumerable.Range(1, 1000)
    .Select(i => new StoreCandidate(
        (i % 2 == 0) ? "steam" : "eshop",
        $"The Legend of Zelda: Breath of the Wild Candidate #{i} (Switch)",
        39.99m,
        59.99m,
        "digital",
        "https://store.steampowered.com",
        null))
    .ToList();

Console.WriteLine($"Iniciando Pipeline | Modo: {config.Mode} | Replay: {config.IsReplayMode} ({config.SimulatedLatencyMs}ms) | W_io: {config.IoWorkers} | W_cpu: {config.CpuWorkers} | BD: {useDb}");

var orchestrator = new PipelineOrchestrator(config, httpClient, rateLimiter, useDb ? repo : null);
var metrics = await orchestrator.ExecuteAsync(catalog, replayCandidates);

Console.WriteLine($"[Resultado] Total: {metrics.TotalPipelineMs / 1000.0:F2}s | Descarga: {metrics.DownloadStageMs / 1000.0:F2}s | Emparejar: {metrics.MatchStageMs / 1000.0:F2}s | Persistir: {metrics.PersistStageMs / 1000.0:F2}s | Solicitudes: {metrics.TotalRequests}");

if (args.Contains("--publish-web"))
{
    string webDataDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../web/public/data"));
    var exporter = new StaticSiteExporter(config.ConnectionString);
    await exporter.ExportPublicSiteDataAsync(webDataDir);
    Console.WriteLine($"[Etapa 5 · Publicar] Archivos catalog.json y metrics.json generados en: {webDataDir}");
}

static int GetIntArg(string[] args, string key, int defaultValue)
{
    int idx = Array.IndexOf(args, key);
    return (idx >= 0 && idx + 1 < args.Length && int.TryParse(args[idx + 1], out int val)) ? val : defaultValue;
}

static string GetStringArg(string[] args, string key, string defaultValue)
{
    int idx = Array.IndexOf(args, key);
    return (idx >= 0 && idx + 1 < args.Length) ? args[idx + 1] : defaultValue;
}