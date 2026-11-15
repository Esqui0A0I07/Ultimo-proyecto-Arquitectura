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
                   COALESCE(g.cover_image_url, 'https://picsum.photos/seed/radar-' || g.game_id || '/400/225') AS coverUrl,
                   COALESCE(g.dominant_color_hex, '#102973') AS dominantColor,
                   COALESCE(g.metacritic_score, 82) AS metacriticScore,
                   COALESCE(g.opencritic_score, 81) AS opencriticScore,
                   COALESCE(g.hltb_main_hours, 18.5) AS hltbMainHours,
                   COALESCE(g.hltb_extra_hours, 31.0) AS hltbExtraHours,
                   COALESCE(g.hltb_completionist_hours, 54.0) AS hltbCompletionistHours
            FROM games g
            ORDER BY g.game_id
            LIMIT 520;
            """;

        const string latestOffersSql = """
            SELECT DISTINCT ON (po.game_id, po.store_id)
                   po.game_id AS gameId,
                   s.store_code AS storeCode,
                   s.store_name AS storeName,
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
        var offers = (await conn.QueryAsync(new CommandDefinition(latestOffersSql, cancellationToken: ct))).ToList();
        var runs = (await conn.QueryAsync(new CommandDefinition(runsSql, cancellationToken: ct))).ToList();
        var stages = (await conn.QueryAsync(new CommandDefinition(stagesSql, cancellationToken: ct))).ToList();

        var offersByGame = offers.GroupBy(o => (int)o.gameid).ToDictionary(g => g.Key, g => g.ToList());

        var enrichedCatalog = games.Select(g =>
        {
            int id = (int)g.gameid;
            var gameOffers = offersByGame.TryGetValue(id, out var list) ? list : [];
            var bestOffer = gameOffers.OrderBy(o => (decimal)o.currentpriceusd).FirstOrDefault();

            return new
            {
                id,
                title = (string)g.title,
                msrpUsd = (decimal)g.msrpusd,
                platforms = (string[])g.platforms,
                itemType = (string)g.itemtype,
                isHardCase = (bool)g.ishardcase,
                coverUrl = (string)g.coverurl,
                dominantColor = (string)g.dominantcolor,
                metacriticScore = (int)g.metacriticscore,
                opencriticScore = (int)g.opencriticscore,
                hltbMainHours = (decimal)g.hltbmainhours,
                hltbExtraHours = (decimal)g.hltbextrahours,
                hltbCompletionistHours = (decimal)g.hltbcompletionisthours,
                bestPriceUsd = bestOffer is not null ? (decimal)bestOffer.currentpriceusd : (decimal)g.msrpusd,
                bestDiscountPct = bestOffer is not null ? (decimal)bestOffer.discountpct : 0m,
                isHistoricalLow = gameOffers.Any(o => (bool)o.ishistoricallow),
                offers = gameOffers.Select(o => new
                {
                    storeCode = (string)o.storecode,
                    storeName = (string)o.storename,
                    format = (string)o.format,
                    currentPriceUsd = (decimal)o.currentpriceusd,
                    regularPriceUsd = (decimal?)o.regularpriceusd,
                    discountPct = (decimal)o.discountpct,
                    isOnSale = (bool)o.isonsale,
                    isHistoricalLow = (bool)o.ishistoricallow,
                    productUrl = (string)o.producturl,
                    observedAt = (DateTimeOffset)o.observedat
                }).OrderBy(o => o.currentPriceUsd).ToList()
            };
        }).ToList();

        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };

        await File.WriteAllTextAsync(
            Path.Combine(outputDirectory, "catalog.json"),
            JsonSerializer.Serialize(new { generatedAt = DateTimeOffset.UtcNow, totalGames = enrichedCatalog.Count, games = enrichedCatalog }, jsonOptions),
            ct);

        await File.WriteAllTextAsync(
            Path.Combine(outputDirectory, "metrics.json"),
            JsonSerializer.Serialize(new { generatedAt = DateTimeOffset.UtcNow, recentRuns = runs, stageMetrics = stages }, jsonOptions),
            ct);
    }
}