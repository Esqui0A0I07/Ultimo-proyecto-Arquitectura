using RadarOfertas.Benchmarks;
using RadarOfertas.Core.Models;
using RadarOfertas.Data.Repositories;
using RadarOfertas.Pipeline;
using RadarOfertas.Scrapers.Discovery;
using RadarOfertas.Scrapers.Replay;
using RadarOfertas.Scrapers.Resilience;

if (args.Contains("--contention"))
{
    Console.WriteLine("=== EXPERIMENTO DE CONTENCIÓN (2.000.000 actualizaciones, 12 hilos) ===");
    foreach (var r in ContentionExperiment.RunAll(2_000_000, Environment.ProcessorCount))
    {
        Console.WriteLine($"{r.Mechanism,-40} | Tiempo: {r.ElapsedMs,5} ms | Esperado: {r.ExpectedTotal} | Obtenido: {r.ActualTotal}");
    }
    return;
}

ExecutionMode mode = args.Contains("--parallel") ? ExecutionMode.Parallel
    : args.Contains("--concurrent") ? ExecutionMode.Concurrent
    : ExecutionMode.Sequential;

int ioWorkers = GetIntArg(args, "--io-workers", 32);
int cpuWorkers = GetIntArg(args, "--cpu-workers", Environment.ProcessorCount);
int latencyMs = GetIntArg(args, "--latency", 120);
int gamesCount = GetIntArg(args, "--games", 500);
string connString = GetStringArg(args, "--conn",
    Environment.GetEnvironmentVariable("RADAR_DB_CONN")
    ?? "Host=ep-shy-lake-bbyw3dvd-pooler.c-15.us-east-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_hA9kWgJoBC6N;SSL Mode=Require;");

var config = new PipelineConfig
{
    Mode = mode,
    IsReplayMode = !args.Contains("--live"),
    IoWorkers = ioWorkers,
    CpuWorkers = cpuWorkers,
    SimulatedLatencyMs = latencyMs,
    ConnectionString = connString
};

HttpMessageHandler handler = config.IsReplayMode
    ? new ReplayHttpMessageHandler(config)
    : new HttpClientHandler();

using var httpClient = new HttpClient(handler);
httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("RadarOfertasMultinucleo-TEC/1.0 (+https://github.com/equipo/radar-ofertas)");

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
    var discoveryScraper = new OpenCriticDiscoveryScraper(httpClient, rateLimiter, config);
    var discoveredGames = await discoveryScraper.DiscoverCatalogAsync(Math.Max(500, gamesCount));
    int hardCasesCount = discoveredGames.Count(g => g.IsHardCase);

    int saved = await repo.UpsertDiscoveredGamesAsync(discoveredGames);
    Console.WriteLine($"Catálogo poblado en PostgreSQL: {saved} juegos procesados | Casos difíciles incluidos: {hardCasesCount}");
    return;
}

bool useDb = args.Contains("--use-db");
IReadOnlyList<GameTarget> catalog;

if (useDb)
{
    catalog = await repo.GetCatalogAsync(gamesCount);
    Console.WriteLine($"Catálogo leído desde PostgreSQL: {catalog.Count} juegos ({catalog.Count(g => g.IsHardCase)} casos difíciles).");
}
else
{
    catalog = Enumerable.Range(1, gamesCount)
        .Select(i => new GameTarget(i, $"The Legend of Zelda Breath of the Wild Edition {i}", 59.99m, ["Nintendo Switch", "PC"], i <= 60))
        .ToList();
}

// Pool de 1.000 candidatos por corrida para que la Etapa 3 (Jaro-Winkler) ejecute 500 x 1.000 = 500.000 comparaciones
var candidatePool = Enumerable.Range(1, 1000)
    .Select(i => new StoreCandidate(
        (i % 4) switch { 0 => "steam", 1 => "xbox", 2 => "eshop", _ => "gamestop" },
        $"Candidate Title Variant #{i} - Deluxe Remastered Edition",
        39.99m,
        59.99m,
        "digital",
        "https://store.steampowered.com",
        null))
    .ToList();

Console.WriteLine($"Iniciando Pipeline | Modo: {config.Mode} | Replay: {config.IsReplayMode} ({config.SimulatedLatencyMs}ms) | W_io: {config.IoWorkers} | W_cpu: {config.CpuWorkers} | BD: {useDb}");

var orchestrator = new PipelineOrchestrator(config, httpClient, rateLimiter, useDb ? repo : null);
var metrics = await orchestrator.ExecuteAsync(catalog, candidatePool);

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