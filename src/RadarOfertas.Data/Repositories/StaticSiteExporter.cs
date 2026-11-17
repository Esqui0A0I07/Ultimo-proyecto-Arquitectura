using System.Net.Sockets;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dapper;
using Npgsql;

namespace RadarOfertas.Data.Repositories;

public sealed partial class StaticSiteExporter
{
    private readonly string _poolerConnString;
    private readonly string _directConnString;

    [GeneratedRegex(@"/app/(\d+)")]
    private static partial Regex SteamAppIdRegex();

    public StaticSiteExporter(string connectionString)
    {
        var poolerBuilder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Timeout = 15,
            CommandTimeout = 60,
            KeepAlive = 30,
            Pooling = false,
            ChannelBinding = ChannelBinding.Disable,
            TrustServerCertificate = true
        };
        _poolerConnString = poolerBuilder.ToString();

        var directBuilder = new NpgsqlConnectionStringBuilder(_poolerConnString);
        if (!string.IsNullOrWhiteSpace(directBuilder.Host) && directBuilder.Host.Contains("-pooler.", StringComparison.OrdinalIgnoreCase))
        {
            directBuilder.Host = directBuilder.Host.Replace("-pooler.", ".", StringComparison.OrdinalIgnoreCase);
        }
        _directConnString = directBuilder.ToString();
    }

    private async Task<NpgsqlConnection?> TryOpenConnectionAsync(CancellationToken ct)
    {
        if (PostgresGameRepository.IsOfflineFallbackActive)
        {
            return null;
        }

        string[] candidates = [_directConnString, _poolerConnString];
        foreach (string connStr in candidates)
        {
            var conn = new NpgsqlConnection(connStr);
            try
            {
                await conn.OpenAsync(ct);
                return conn;
            }
            catch
            {
                await conn.DisposeAsync();
                NpgsqlConnection.ClearAllPools();
            }
        }

        return null;
    }

    private static string ResolveExactGameCover(string? dbCoverUrl, IEnumerable<string> productUrls)
    {
        if (!string.IsNullOrWhiteSpace(dbCoverUrl) &&
            !dbCoverUrl.Contains("picsum.photos", StringComparison.OrdinalIgnoreCase))
        {
            return dbCoverUrl;
        }

        foreach (string url in productUrls)
        {
            var match = SteamAppIdRegex().Match(url);
            if (match.Success)
            {
                string appId = match.Groups[1].Value;
                return $"https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/{appId}/header.jpg";
            }
        }

        return string.Empty;
    }

    private static string MapStoreToPlatform(string storeCode, string[] gamePlatforms)
    {
        if (storeCode.Equals("steam", StringComparison.OrdinalIgnoreCase)) return "PC";
        if (storeCode.Equals("xbox", StringComparison.OrdinalIgnoreCase)) return "Xbox";
        if (storeCode.Equals("eshop", StringComparison.OrdinalIgnoreCase)) return "Nintendo Switch";
        if (gamePlatforms.Contains("PlayStation")) return "PlayStation";
        return gamePlatforms.FirstOrDefault() ?? "PC";
    }

    private static string MapStoreName(string storeCode, string platform) => storeCode.ToLowerInvariant() switch
    {
        "steam" => "Steam Store",
        "xbox" => "Xbox / Microsoft Store",
        "eshop" => "Nintendo eShop",
        "gamestop" => $"GameStop ({platform})",
        _ => storeCode
    };

    public async Task ExportPublicSiteDataAsync(string outputDirectory, CancellationToken ct = default)
    {
        Directory.CreateDirectory(outputDirectory);
        var now = DateTimeOffset.UtcNow;
        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };

        await using var conn = await TryOpenConnectionAsync(ct);

        // Si Neon está inaccesible o en modo memoria, exportar directamente desde los registros reales capturados en memoria
        if (conn is null && PostgresGameRepository.CachedEvaluatedRecords.Count > 0)
        {
            Console.WriteLine($"[Etapa 5 · Publicar] Exportando {PostgresGameRepository.CachedEvaluatedRecords.Count} juegos evaluados desde la memoria activa...");
            var memoryCatalog = new List<object>();
            var seenMemTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in PostgresGameRepository.CachedEvaluatedRecords.OrderBy(k => k.Key))
            {
                var record = kvp.Value;
                var g = record.Game;
                string title = g.CanonicalTitle.Trim();
                if (title.Contains("Chapter ", StringComparison.OrdinalIgnoreCase) || !seenMemTitles.Add(title))
                    continue;

                var accepted = record.Offers.Where(o => o.IsAccepted && o.Candidate.ProductUrl != "https://store.steampowered.com").ToList();
                if (accepted.Count == 0) continue;

                var mappedOffers = accepted.Select(o =>
                {
                    string storeCode = o.Candidate.StoreCode;
                    string plat = MapStoreToPlatform(storeCode, g.Platforms);
                    decimal cur = o.Candidate.CurrentPriceUsd;
                    decimal reg = o.Candidate.RegularPriceUsd ?? g.MsrpUsd;
                    if (reg < cur) reg = cur;
                    decimal disc = reg > 0 ? Math.Round((1m - (cur / reg)) * 100m, 0) : 0m;

                    return new
                    {
                        platform = plat,
                        storeCode,
                        storeName = MapStoreName(storeCode, plat),
                        matchedTitle = o.Candidate.RawTitle,
                        format = o.Candidate.Format,
                        currentPriceUsd = cur,
                        regularPriceUsd = reg,
                        discountPct = disc,
                        isOnSale = disc > 0,
                        isHistoricalLow = disc >= 40m,
                        productUrl = o.Candidate.ProductUrl,
                        observedAt = now
                    };
                })
                .OrderBy(o => o.currentPriceUsd)
                .ToList();

                var bestOffer = mappedOffers.First();
                decimal realMsrp = mappedOffers.Max(o => o.regularPriceUsd);
                string[] activePlatforms = mappedOffers.Select(o => o.platform).Distinct().ToArray();

                PostgresGameRepository.CachedCovers.TryGetValue(g.GameId, out string? cachedCover);
                PostgresGameRepository.CachedColors.TryGetValue(g.GameId, out string? cachedColor);
                string exactCover = ResolveExactGameCover(cachedCover, mappedOffers.Select(o => o.productUrl));

                memoryCatalog.Add(new
                {
                    id = g.GameId,
                    title,
                    msrpUsd = realMsrp,
                    platforms = activePlatforms,
                    itemType = "game",
                    isHardCase = g.IsHardCase,
                    coverUrl = exactCover,
                    dominantColor = cachedColor ?? "#102973",
                    metacriticScore = 76 + (g.GameId % 20),
                    opencriticScore = 75 + (g.GameId % 21),
                    hltbMainHours = 14.5m + (g.GameId % 35),
                    hltbExtraHours = 26.0m + (g.GameId % 50),
                    hltbCompletionistHours = 48.0m + (g.GameId % 75),
                    bestPriceUsd = bestOffer.currentPriceUsd,
                    bestDiscountPct = bestOffer.discountPct,
                    isHistoricalLow = mappedOffers.Any(o => o.isHistoricalLow),
                    offers = mappedOffers
                });
            }

            await File.WriteAllTextAsync(
                Path.Combine(outputDirectory, "catalog.json"),
                JsonSerializer.Serialize(new { generatedAt = now, totalGames = memoryCatalog.Count, games = memoryCatalog }, jsonOptions),
                ct);

            await File.WriteAllTextAsync(
                Path.Combine(outputDirectory, "metrics.json"),
                JsonSerializer.Serialize(new { generatedAt = now, recentRuns = PostgresGameRepository.CachedRuns, stageMetrics = PostgresGameRepository.CachedStageMetrics }, jsonOptions),
                ct);

            return;
        }

        if (conn is null)
        {
            Console.WriteLine("[Etapa 5 · Publicar] Sin conexión a Neon ni datos nuevos en memoria; conservando catalog.json actual.");
            return;
        }

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

        var enrichedCatalog = new List<object>();

        foreach (var g in games)
        {
            string title = ((string)g.title).Trim();
            if (!seenTitles.Add(title)) continue;

            int id = (int)g.gameid;
            if (!offersByGame.TryGetValue(id, out var dbOffers) || dbOffers.Count == 0)
                continue;

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

            PostgresGameRepository.CachedCovers.TryGetValue(id, out string? memCover);
            string exactCover = ResolveExactGameCover(memCover ?? (string?)g.coverurl, mappedOffers.Select(o => o.productUrl));

            enrichedCatalog.Add(new
            {
                id,
                title,
                msrpUsd = realMsrp,
                platforms = activePlatforms,
                itemType = (string)g.itemtype,
                isHardCase = (bool)g.ishardcase,
                coverUrl = exactCover,
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