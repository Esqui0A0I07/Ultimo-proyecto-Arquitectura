using System.Text.Json;
using Dapper;
using Npgsql;

namespace RadarOfertas.Data.Repositories;

public sealed class StaticSiteExporter
{
    private readonly string _connectionString;

    private static readonly (string Keyword, string CoverUrl)[] OfficialCovers =
    [
        ("Witcher 3", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/292030/header.jpg"),
        ("Witcher", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/292030/header.jpg"),
        ("Final Fantasy VII Rebirth", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/2909400/header.jpg"),
        ("Final Fantasy VII", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1462040/header.jpg"),
        ("Final Fantasy XVI", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/2515020/header.jpg"),
        ("Final Fantasy", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/2515020/header.jpg"),
        ("Resident Evil Village", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1196590/header.jpg"),
        ("Resident Evil 4", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/2050650/header.jpg"),
        ("Resident Evil 2", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/883710/header.jpg"),
        ("Resident Evil 3", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/952060/header.jpg"),
        ("Resident Evil", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/2050650/header.jpg"),
        ("Dark Souls III", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/374320/header.jpg"),
        ("Dark Souls II", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/335300/header.jpg"),
        ("Dark Souls", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/570940/header.jpg"),
        ("Persona 3 Reload", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/2161700/header.jpg"),
        ("Persona 5", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1687950/header.jpg"),
        ("Persona", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1687950/header.jpg"),
        ("Red Dead Redemption 2", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1174180/header.jpg"),
        ("Red Dead", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1174180/header.jpg"),
        ("Miles Morales", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1817190/header.jpg"),
        ("Spider-Man 2", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/2651280/header.jpg"),
        ("Spider-Man", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1817070/header.jpg"),
        ("Horizon Forbidden West", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/2420110/header.jpg"),
        ("Horizon Zero Dawn", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1151640/header.jpg"),
        ("Horizon", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1151640/header.jpg"),
        ("Elden Ring", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1245620/header.jpg"),
        ("Cyberpunk 2077", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1091500/header.jpg"),
        ("Baldur's Gate", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1086940/header.jpg"),
        ("God of War Ragnar", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/2322010/header.jpg"),
        ("God of War", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1593500/header.jpg"),
        ("Hollow Knight", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/367520/header.jpg"),
        ("Hades", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1145360/header.jpg"),
        ("Celeste", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/504230/header.jpg"),
        ("Stardew Valley", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/413150/header.jpg"),
        ("Sekiro", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/814380/header.jpg"),
        ("Ghost of Tsushima", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/2215430/header.jpg"),
        ("Monster Hunter World", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/582010/header.jpg"),
        ("Monster Hunter", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1446780/header.jpg"),
        ("Street Fighter", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1364780/header.jpg"),
        ("Diablo", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/2344520/header.jpg"),
        ("NieR Replicant", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1113560/header.jpg"),
        ("NieR", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/524220/header.jpg"),
        ("Dragon Quest", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1295510/header.jpg"),
        ("Grand Theft Auto", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/271590/header.jpg"),
        ("Alan Wake", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/108710/header.jpg"),
        ("Divinity", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/435150/header.jpg"),
        ("Death Stranding", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1850570/header.jpg"),
        ("DOOM", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/782330/header.jpg"),
        ("Lies of P", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1627720/header.jpg"),
        ("Subnautica", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/264710/header.jpg"),
        ("Control", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/870780/header.jpg"),
        ("Kingdom Hearts", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/2552450/header.jpg"),
        ("Ori and the", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1057090/header.jpg"),
        ("Dishonored", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/403640/header.jpg"),
        ("Last of Us", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1888930/header.jpg"),
        ("Bioshock", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/8870/header.jpg"),
        ("Mass Effect", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1328670/header.jpg"),
        ("Fallout", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/377160/header.jpg"),
        ("Skyrim", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/489830/header.jpg"),
        ("Elder Scrolls", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/489830/header.jpg"),
        ("Halo", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/976730/header.jpg"),
        ("Forza", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1551360/header.jpg"),
        ("Assassin", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/2208920/header.jpg"),
        ("Yakuza", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1235140/header.jpg"),
        ("Like a Dragon", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/2072450/header.jpg"),
        ("Tekken", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1778820/header.jpg"),
        ("Mortal Kombat", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1971870/header.jpg"),
        ("Zelda: Tears", "https://assets.nintendo.com/image/upload/ar_16:9,c_lpad,w_656/b_white/f_auto/q_auto/ncom/software/switch/70010000063714/276a412988e07c5d55a2996c6d38abb408b464413b2dfeb44d2aa460b9f622e1"),
        ("Zelda", "https://assets.nintendo.com/image/upload/ar_16:9,c_lpad,w_656/b_white/f_auto/q_auto/ncom/software/switch/70010000000025/7137262b5a64d921e193653f8aa0b722925abc5680380ca0e18a5cfd91697f58"),
        ("Mario Bros. Wonder", "https://assets.nintendo.com/image/upload/ar_16:9,c_lpad,w_656/b_white/f_auto/q_auto/ncom/software/switch/70010000068688/a13a41a64270365f39f6e595435f60394072b75b2c2a3a6571b94e0e96518066"),
        ("Mario", "https://assets.nintendo.com/image/upload/ar_16:9,c_lpad,w_656/b_white/f_auto/q_auto/ncom/software/switch/70010000001130/c42553b4fd0312c31e70ec7468c6c9bccd739f340152925b9600631f2d29f8b5"),
        ("Metroid", "https://assets.nintendo.com/image/upload/ar_16:9,c_lpad,w_656/b_white/f_auto/q_auto/ncom/software/switch/70010000042924/72c97f2665f95f979e51d8290b08a186919152a19d94f372821c5686274945e5")
    ];

    public StaticSiteExporter(string connectionString)
    {
        _connectionString = connectionString;
    }

    private static string ResolveOfficialCover(string title, int gameId, string? dbCoverUrl)
    {
        if (!string.IsNullOrWhiteSpace(dbCoverUrl) && !dbCoverUrl.Contains("picsum.photos", StringComparison.OrdinalIgnoreCase))
        {
            return dbCoverUrl;
        }

        foreach (var (keyword, url) in OfficialCovers)
        {
            if (title.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return url;
            }
        }

        return OfficialCovers[gameId % OfficialCovers.Length].CoverUrl;
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
            string title = (string)g.title;
            string resolvedCover = ResolveOfficialCover(title, id, (string?)g.coverurl);

            var gameOffers = offersByGame.TryGetValue(id, out var list) ? list : [];
            var bestOffer = gameOffers.OrderBy(o => (decimal)o.currentpriceusd).FirstOrDefault();

            return new
            {
                id,
                title,
                msrpUsd = (decimal)g.msrpusd,
                platforms = (string[])g.platforms,
                itemType = (string)g.itemtype,
                isHardCase = (bool)g.ishardcase,
                coverUrl = resolvedCover,
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