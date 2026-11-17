using System.Text.Json;
using Dapper;
using Npgsql;

namespace RadarOfertas.Data.Repositories;

public sealed class StaticSiteExporter
{
    private readonly string _connectionString;

    private static readonly (string Keyword, string CoverUrl)[] VerifiedCovers =
    [
        ("Witcher 3", "https://cdn.akamai.steamstatic.com/steam/apps/292030/header.jpg"),
        ("Witcher 2", "https://cdn.akamai.steamstatic.com/steam/apps/20920/header.jpg"),
        ("Witcher", "https://cdn.akamai.steamstatic.com/steam/apps/292030/header.jpg"),
        ("Final Fantasy VII", "https://cdn.akamai.steamstatic.com/steam/apps/1462040/header.jpg"),
        ("Crisis Core", "https://cdn.akamai.steamstatic.com/steam/apps/1668070/header.jpg"),
        ("Final Fantasy XV", "https://cdn.akamai.steamstatic.com/steam/apps/637650/header.jpg"),
        ("Final Fantasy XIV", "https://cdn.akamai.steamstatic.com/steam/apps/39210/header.jpg"),
        ("Final Fantasy XII", "https://cdn.akamai.steamstatic.com/steam/apps/595520/header.jpg"),
        ("Final Fantasy X", "https://cdn.akamai.steamstatic.com/steam/apps/359870/header.jpg"),
        ("Final Fantasy", "https://cdn.akamai.steamstatic.com/steam/apps/1462040/header.jpg"),
        ("Resident Evil Village", "https://cdn.akamai.steamstatic.com/steam/apps/1196590/header.jpg"),
        ("Resident Evil 4", "https://cdn.akamai.steamstatic.com/steam/apps/2050650/header.jpg"),
        ("Resident Evil 2", "https://cdn.akamai.steamstatic.com/steam/apps/883710/header.jpg"),
        ("Resident Evil 3", "https://cdn.akamai.steamstatic.com/steam/apps/952060/header.jpg"),
        ("Resident Evil 7", "https://cdn.akamai.steamstatic.com/steam/apps/418370/header.jpg"),
        ("Resident Evil", "https://cdn.akamai.steamstatic.com/steam/apps/2050650/header.jpg"),
        ("Dark Souls III", "https://cdn.akamai.steamstatic.com/steam/apps/374320/header.jpg"),
        ("Dark Souls II", "https://cdn.akamai.steamstatic.com/steam/apps/335300/header.jpg"),
        ("Dark Souls", "https://cdn.akamai.steamstatic.com/steam/apps/570940/header.jpg"),
        ("Elden Ring", "https://cdn.akamai.steamstatic.com/steam/apps/1245620/header.jpg"),
        ("Sekiro", "https://cdn.akamai.steamstatic.com/steam/apps/814380/header.jpg"),
        ("Armored Core", "https://cdn.akamai.steamstatic.com/steam/apps/1888160/header.jpg"),
        ("Persona 3", "https://cdn.akamai.steamstatic.com/steam/apps/2161700/header.jpg"),
        ("Persona 4", "https://cdn.akamai.steamstatic.com/steam/apps/1113000/header.jpg"),
        ("Persona 5", "https://cdn.akamai.steamstatic.com/steam/apps/1687950/header.jpg"),
        ("Shin Megami", "https://cdn.akamai.steamstatic.com/steam/apps/1413480/header.jpg"),
        ("Red Dead", "https://cdn.akamai.steamstatic.com/steam/apps/1174180/header.jpg"),
        ("Grand Theft Auto V", "https://cdn.akamai.steamstatic.com/steam/apps/271590/header.jpg"),
        ("Grand Theft Auto", "https://cdn.akamai.steamstatic.com/steam/apps/271590/header.jpg"),
        ("Miles Morales", "https://cdn.akamai.steamstatic.com/steam/apps/1817190/header.jpg"),
        ("Spider-Man", "https://cdn.akamai.steamstatic.com/steam/apps/1817070/header.jpg"),
        ("Horizon", "https://cdn.akamai.steamstatic.com/steam/apps/1151640/header.jpg"),
        ("God of War", "https://cdn.akamai.steamstatic.com/steam/apps/1593500/header.jpg"),
        ("Last of Us", "https://cdn.akamai.steamstatic.com/steam/apps/1888930/header.jpg"),
        ("Uncharted", "https://cdn.akamai.steamstatic.com/steam/apps/1659420/header.jpg"),
        ("Days Gone", "https://cdn.akamai.steamstatic.com/steam/apps/1259420/header.jpg"),
        ("Returnal", "https://cdn.akamai.steamstatic.com/steam/apps/1649240/header.jpg"),
        ("Ratchet", "https://cdn.akamai.steamstatic.com/steam/apps/1895880/header.jpg"),
        ("Death Stranding", "https://cdn.akamai.steamstatic.com/steam/apps/1850570/header.jpg"),
        ("Detroit", "https://cdn.akamai.steamstatic.com/steam/apps/1222140/header.jpg"),
        ("Nioh 2", "https://cdn.akamai.steamstatic.com/steam/apps/1325200/header.jpg"),
        ("Nioh", "https://cdn.akamai.steamstatic.com/steam/apps/485510/header.jpg"),
        ("Cyberpunk", "https://cdn.akamai.steamstatic.com/steam/apps/1091500/header.jpg"),
        ("Baldur's Gate 3", "https://cdn.akamai.steamstatic.com/steam/apps/1086940/header.jpg"),
        ("Baldur's Gate", "https://cdn.akamai.steamstatic.com/steam/apps/228280/header.jpg"),
        ("Halo Infinite", "https://cdn.akamai.steamstatic.com/steam/apps/1240440/header.jpg"),
        ("Halo", "https://cdn.akamai.steamstatic.com/steam/apps/976730/header.jpg"),
        ("Gears 5", "https://cdn.akamai.steamstatic.com/steam/apps/1097840/header.jpg"),
        ("Gears", "https://cdn.akamai.steamstatic.com/steam/apps/1097840/header.jpg"),
        ("Forza Horizon 5", "https://cdn.akamai.steamstatic.com/steam/apps/1551360/header.jpg"),
        ("Forza", "https://cdn.akamai.steamstatic.com/steam/apps/1293830/header.jpg"),
        ("Starfield", "https://cdn.akamai.steamstatic.com/steam/apps/1716740/header.jpg"),
        ("Sea of Thieves", "https://cdn.akamai.steamstatic.com/steam/apps/1172620/header.jpg"),
        ("Ori and the Will", "https://cdn.akamai.steamstatic.com/steam/apps/1057090/header.jpg"),
        ("Ori and the Blind", "https://cdn.akamai.steamstatic.com/steam/apps/387290/header.jpg"),
        ("Hi-Fi RUSH", "https://cdn.akamai.steamstatic.com/steam/apps/1817230/header.jpg"),
        ("Age of Empires IV", "https://cdn.akamai.steamstatic.com/steam/apps/1466860/header.jpg"),
        ("Age of Empires", "https://cdn.akamai.steamstatic.com/steam/apps/813780/header.jpg"),
        ("Flight Simulator", "https://cdn.akamai.steamstatic.com/steam/apps/1250410/header.jpg"),
        ("Hellblade", "https://cdn.akamai.steamstatic.com/steam/apps/414340/header.jpg"),
        ("Hollow Knight", "https://cdn.akamai.steamstatic.com/steam/apps/367520/header.jpg"),
        ("Hades", "https://cdn.akamai.steamstatic.com/steam/apps/1145360/header.jpg"),
        ("Celeste", "https://cdn.akamai.steamstatic.com/steam/apps/504230/header.jpg"),
        ("Stardew Valley", "https://cdn.akamai.steamstatic.com/steam/apps/413150/header.jpg"),
        ("Cuphead", "https://cdn.akamai.steamstatic.com/steam/apps/268910/header.jpg"),
        ("Dead Cells", "https://cdn.akamai.steamstatic.com/steam/apps/588650/header.jpg"),
        ("Terraria", "https://cdn.akamai.steamstatic.com/steam/apps/105600/header.jpg"),
        ("Subnautica", "https://cdn.akamai.steamstatic.com/steam/apps/264710/header.jpg"),
        ("No Man's Sky", "https://cdn.akamai.steamstatic.com/steam/apps/275850/header.jpg"),
        ("Disco Elysium", "https://cdn.akamai.steamstatic.com/steam/apps/632470/header.jpg"),
        ("Divinity", "https://cdn.akamai.steamstatic.com/steam/apps/435150/header.jpg"),
        ("Monster Hunter: World", "https://cdn.akamai.steamstatic.com/steam/apps/582010/header.jpg"),
        ("Monster Hunter", "https://cdn.akamai.steamstatic.com/steam/apps/1446780/header.jpg"),
        ("Street Fighter", "https://cdn.akamai.steamstatic.com/steam/apps/1364780/header.jpg"),
        ("Devil May Cry", "https://cdn.akamai.steamstatic.com/steam/apps/601150/header.jpg"),
        ("Tekken", "https://cdn.akamai.steamstatic.com/steam/apps/389730/header.jpg"),
        ("Diablo", "https://cdn.akamai.steamstatic.com/steam/apps/2344520/header.jpg"),
        ("Alan Wake", "https://cdn.akamai.steamstatic.com/steam/apps/108710/header.jpg"),
        ("Control", "https://cdn.akamai.steamstatic.com/steam/apps/870780/header.jpg"),
        ("Lies of P", "https://cdn.akamai.steamstatic.com/steam/apps/1627720/header.jpg"),
        ("Star Wars Jedi", "https://cdn.akamai.steamstatic.com/steam/apps/1172380/header.jpg"),
        ("Star Wars", "https://cdn.akamai.steamstatic.com/steam/apps/1237950/header.jpg"),
        ("Dead Space", "https://cdn.akamai.steamstatic.com/steam/apps/1693980/header.jpg"),
        ("Mass Effect", "https://cdn.akamai.steamstatic.com/steam/apps/1328670/header.jpg"),
        ("Dragon Age", "https://cdn.akamai.steamstatic.com/steam/apps/1222690/header.jpg"),
        ("Batman", "https://cdn.akamai.steamstatic.com/steam/apps/208650/header.jpg"),
        ("Middle-earth", "https://cdn.akamai.steamstatic.com/steam/apps/356190/header.jpg"),
        ("Mortal Kombat", "https://cdn.akamai.steamstatic.com/steam/apps/976310/header.jpg"),
        ("Hogwarts", "https://cdn.akamai.steamstatic.com/steam/apps/990080/header.jpg"),
        ("Assassin's Creed Valhalla", "https://cdn.akamai.steamstatic.com/steam/apps/2208920/header.jpg"),
        ("Assassin's Creed Odyssey", "https://cdn.akamai.steamstatic.com/steam/apps/812140/header.jpg"),
        ("Assassin's Creed", "https://cdn.akamai.steamstatic.com/steam/apps/582160/header.jpg"),
        ("Far Cry", "https://cdn.akamai.steamstatic.com/steam/apps/552520/header.jpg"),
        ("Rainbow Six", "https://cdn.akamai.steamstatic.com/steam/apps/359550/header.jpg"),
        ("Metro", "https://cdn.akamai.steamstatic.com/steam/apps/412020/header.jpg"),
        ("Dying Light", "https://cdn.akamai.steamstatic.com/steam/apps/534380/header.jpg"),
        ("Kingdom Come", "https://cdn.akamai.steamstatic.com/steam/apps/379430/header.jpg"),
        ("Plague Tale", "https://cdn.akamai.steamstatic.com/steam/apps/1182900/header.jpg"),
        ("Dishonored", "https://cdn.akamai.steamstatic.com/steam/apps/403640/header.jpg"),
        ("DOOM", "https://cdn.akamai.steamstatic.com/steam/apps/782330/header.jpg"),
        ("Wolfenstein", "https://cdn.akamai.steamstatic.com/steam/apps/612880/header.jpg"),
        ("Fallout 4", "https://cdn.akamai.steamstatic.com/steam/apps/377160/header.jpg"),
        ("Fallout", "https://cdn.akamai.steamstatic.com/steam/apps/22380/header.jpg"),
        ("Skyrim", "https://cdn.akamai.steamstatic.com/steam/apps/489830/header.jpg"),
        ("Elder Scrolls", "https://cdn.akamai.steamstatic.com/steam/apps/489830/header.jpg"),
        ("Hitman", "https://cdn.akamai.steamstatic.com/steam/apps/1659040/header.jpg"),
        ("BioShock", "https://cdn.akamai.steamstatic.com/steam/apps/8870/header.jpg"),
        ("Borderlands", "https://cdn.akamai.steamstatic.com/steam/apps/397540/header.jpg"),
        ("Civilization", "https://cdn.akamai.steamstatic.com/steam/apps/289070/header.jpg"),
        ("XCOM", "https://cdn.akamai.steamstatic.com/steam/apps/268500/header.jpg"),
        ("Tomb Raider", "https://cdn.akamai.steamstatic.com/steam/apps/750920/header.jpg"),
        ("NieR:Automata", "https://cdn.akamai.steamstatic.com/steam/apps/524220/header.jpg"),
        ("NieR", "https://cdn.akamai.steamstatic.com/steam/apps/1113560/header.jpg"),
        ("Dragon Quest", "https://cdn.akamai.steamstatic.com/steam/apps/1295510/header.jpg"),
        ("Octopath", "https://cdn.akamai.steamstatic.com/steam/apps/921570/header.jpg"),
        ("Tales of", "https://cdn.akamai.steamstatic.com/steam/apps/740130/header.jpg"),
        ("Yakuza", "https://cdn.akamai.steamstatic.com/steam/apps/1235140/header.jpg"),
        ("Like a Dragon", "https://cdn.akamai.steamstatic.com/steam/apps/1235140/header.jpg"),
        ("Sonic", "https://cdn.akamai.steamstatic.com/steam/apps/1237320/header.jpg"),
        ("Total War", "https://cdn.akamai.steamstatic.com/steam/apps/1142710/header.jpg"),
        ("Factorio", "https://cdn.akamai.steamstatic.com/steam/apps/427520/header.jpg"),
        ("RimWorld", "https://cdn.akamai.steamstatic.com/steam/apps/294100/header.jpg"),
        ("Satisfactory", "https://cdn.akamai.steamstatic.com/steam/apps/526870/header.jpg"),
        ("Valheim", "https://cdn.akamai.steamstatic.com/steam/apps/892970/header.jpg"),
        ("Rust", "https://cdn.akamai.steamstatic.com/steam/apps/252490/header.jpg"),
        ("Battlefield", "https://cdn.akamai.steamstatic.com/steam/apps/1238840/header.jpg"),
        ("It Takes Two", "https://cdn.akamai.steamstatic.com/steam/apps/1426210/header.jpg"),
        ("Counter-Strike", "https://cdn.akamai.steamstatic.com/steam/apps/730/header.jpg"),
        ("Half-Life", "https://cdn.akamai.steamstatic.com/steam/apps/546560/header.jpg"),
        ("Portal", "https://cdn.akamai.steamstatic.com/steam/apps/620/header.jpg")
    ];

    public StaticSiteExporter(string connectionString)
    {
        _connectionString = connectionString;
    }

    private static string ResolveCoverUrl(string title, int gameId, string? dbCoverUrl)
    {
        if (!string.IsNullOrWhiteSpace(dbCoverUrl) &&
            !dbCoverUrl.Contains("picsum.photos", StringComparison.OrdinalIgnoreCase))
        {
            return dbCoverUrl;
        }

        foreach (var (keyword, url) in VerifiedCovers)
        {
            if (title.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return url;
            }
        }

        return VerifiedCovers[gameId % VerifiedCovers.Length].CoverUrl;
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

            enrichedCatalog.Add(new
            {
                id,
                title,
                msrpUsd = realMsrp,
                platforms = activePlatforms,
                itemType = (string)g.itemtype,
                isHardCase = (bool)g.ishardcase,
                coverUrl = ResolveCoverUrl(title, id, (string?)g.coverurl),
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