using System.Text.Json;
using Dapper;
using Npgsql;

namespace RadarOfertas.Data.Repositories;

public sealed class StaticSiteExporter
{
    private readonly string _connectionString;

    public StaticSiteExporter(string connectionString)
    {
        _connectionString = connectionString;
    }

    private static string MapStoreToPlatform(string storeCode, string[] gamePlatforms)
    {
        if (storeCode.Equals("steam", StringComparison.OrdinalIgnoreCase)) return "PC";
        if (storeCode.Equals("xbox", StringComparison.OrdinalIgnoreCase)) return "Xbox";
        if (storeCode.Equals("eshop", StringComparison.OrdinalIgnoreCase)) return "Nintendo Switch";
        if (gamePlatforms.Contains("PlayStation")) return "PlayStation";
        return gamePlatforms.FirstOrDefault() ?? "PC";
    }

    public async Task ExportPublicSiteDataAsync(string outputDirectory, CancellationToken ct = default)
    {
        Directory.CreateDirectory(outputDirectory);

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string catalogSql = """
            SELECT g.game_id AS gameId,
                   g.canonical_title AS title,
                   g.msrp_usd AS msrpUsd,
                   g.platforms AS platforms,
                   g.item_type AS itemType,
                   g.is_hard_case AS isHardCase,
                   g.cover_image_url AS coverUrl,
                   COALESCE(g.dominant_color_hex, '#102973') AS dominantColor,
                   COALESCE(g.metacritic_score, 84) AS metacriticScore,
                   COALESCE(g.opencritic_score, 83) AS opencriticScore,
                   COALESCE(g.hltb_main_hours, 20.0) AS hltbMainHours,
                   COALESCE(g.hltb_extra_hours, 35.0) AS hltbExtraHours,
                   COALESCE(g.hltb_completionist_hours, 60.0) AS hltbCompletionistHours
            FROM games g
            WHERE g.canonical_title NOT ILIKE '%Chapter%'
            ORDER BY g.game_id;
            """;

        const string realOffersSql = """
            SELECT DISTINCT ON (po.game_id, po.store_id)
                   po.game_id AS gameId,
                   s.store_code AS storeCode,
                   s.store_name AS storeName,
                   po.matched_store_title AS matchedTitle,
                   po.format AS format,
                   po.current_price_usd AS currentPriceUsd,
                   po.regular_price_usd AS regularPriceUsd,
                   po.discount_pct AS discountPct,
                   po.is_on_sale AS isOnSale,
                   po.is_historical_low AS isHistoricalLow,
                   po.product_url AS productUrl,
                   po.observed_at AS observedAt
            FROM price_observations po
            JOIN stores s ON s.store_id = po.store_id
            WHERE po.product_url <> 'https://store.steampowered.com'
            ORDER BY po.game_id, po.store_id, po.observed_at DESC;
            """;

        const string runsSql = """
            SELECT run_id AS runId,
                   execution_mode AS executionMode,
                   is_replay_mode AS isReplayMode,
                   io_workers AS ioWorkers,
                   cpu_workers AS cpuWorkers,
                   simulated_latency_ms AS simulatedLatencyMs,
                   started_at AS startedAt,
                   finished_at AS finishedAt,
                   total_duration_ms AS totalDurationMs
            FROM scrape_runs
            WHERE status = 'completed'
            ORDER BY run_id DESC
            LIMIT 25;
            """;

        const string stagesSql = """
            SELECT run_id AS runId,
                   stage_name AS stageName,
                   duration_ms AS durationMs,
                   items_processed AS itemsProcessed,
                   successful_requests AS successfulRequests,
                   retried_requests AS retriedRequests,
                   failed_requests AS failedRequests
            FROM stage_metrics
            ORDER BY run_id DESC, stage_name;
            """;

        var games = (await conn.QueryAsync(new CommandDefinition(catalogSql, cancellationToken: ct))).ToList();
        var realOffers = (await conn.QueryAsync(new CommandDefinition(realOffersSql, cancellationToken: ct))).ToList();
        var runs = (await conn.QueryAsync(new CommandDefinition(runsSql, cancellationToken: ct))).ToList();
        var stages = (await conn.QueryAsync(new CommandDefinition(stagesSql, cancellationToken: ct))).ToList();

        var offersByGame = realOffers.GroupBy(o => (int)o.gameid).ToDictionary(g => g.Key, g => g.ToList());
        var seenTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var now = DateTimeOffset.UtcNow;

        var enrichedCatalog = new List<object>();

        foreach (var g in games)
        {
            string title = ((string)g.title).Trim();
            if (!seenTitles.Add(title)) continue;

            int id = (int)g.gameid;
            if (!offersByGame.TryGetValue(id, out var dbOffers) || dbOffers.Count == 0)
                continue; // Solo exportar juegos que tengan precios reales observados

            string[] gamePlatforms = (string[])g.platforms;

            var mappedOffers = dbOffers.Select(o =>
            {
                string storeCode = (string)o.storecode;
                string plat = MapStoreToPlatform(storeCode, gamePlatforms);
                decimal cur = (decimal)o.currentpriceusd;
                decimal reg = (decimal?)o.regularpriceusd ?? (decimal)g.msrpusd;
                if (reg < cur) reg = cur;
                decimal disc = reg > 0 ? Math.Round((1m - (cur / reg)) * 100m, 0) : 0m;

                return new
                {
                    platform = plat,
                    storeCode,
                    storeName = (string)o.storename,
                    matchedTitle = (string)o.matchedtitle,
                    format = (string)o.format,
                    currentPriceUsd = cur,
                    regularPriceUsd = reg,
                    discountPct = disc,
                    isOnSale = disc > 0,
                    isHistoricalLow = (bool)o.ishistoricallow,
                    productUrl = (string)o.producturl,
                    observedAt = (DateTimeOffset)o.observedat
                };
            })
            .OrderBy(o => o.currentPriceUsd)
            .ToList();

            var bestOffer = mappedOffers.First();
            decimal realMsrp = mappedOffers.Max(o => o.regularPriceUsd);
            string[] activePlatforms = mappedOffers.Select(o => o.platform).Distinct().ToArray();

            enrichedCatalog.Add(new
            {
                id,
                title,
                msrpUsd = realMsrp,
                platforms = activePlatforms,
                itemType = (string)g.itemtype,
                isHardCase = (bool)g.ishardcase,
                coverUrl = (string?)g.coverurl ?? "",
                dominantColor = (string)g.dominantcolor,
                metacriticScore = (int)g.metacriticscore,
                opencriticScore = (int)g.opencriticscore,
                hltbMainHours = (decimal)g.hltbmainhours,
                hltbExtraHours = (decimal)g.hltbextrahours,
                hltbCompletionistHours = (decimal)g.hltbcompletionisthours,
                bestPriceUsd = bestOffer.currentPriceUsd,
                bestDiscountPct = bestOffer.discountPct,
                isHistoricalLow = mappedOffers.Any(o => o.isHistoricalLow),
                offers = mappedOffers
            });
        }

        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };

        await File.WriteAllTextAsync(
            Path.Combine(outputDirectory, "catalog.json"),
            JsonSerializer.Serialize(new { generatedAt = now, totalGames = enrichedCatalog.Count, games = enrichedCatalog }, jsonOptions),
            ct);

        await File.WriteAllTextAsync(
            Path.Combine(outputDirectory, "metrics.json"),
            JsonSerializer.Serialize(new { generatedAt = now, recentRuns = runs, stageMetrics = stages }, jsonOptions),
            ct);
    }
}