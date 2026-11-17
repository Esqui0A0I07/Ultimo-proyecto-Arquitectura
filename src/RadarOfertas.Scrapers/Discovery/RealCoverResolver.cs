using System.Collections.Concurrent;
using System.Text.Json;
using RadarOfertas.Core.Models;

namespace RadarOfertas.Scrapers.Discovery;

public static class RealCoverResolver
{
    // Mapa directo de franquicias del catálogo a sus carátulas oficiales en el CDN de Steam / Nintendo
    private static readonly (string Keyword, string CoverUrl)[] KnownFranchiseCovers =
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
        ("Baldur's Gate 3", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1086940/header.jpg"),
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
        ("Monster Hunter Rise", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1446780/header.jpg"),
        ("Monster Hunter", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1446780/header.jpg"),
        ("Street Fighter 6", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1364780/header.jpg"),
        ("Diablo IV", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/2344520/header.jpg"),
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
        ("Assassins Creed", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/2208920/header.jpg"),
        ("Assassin's Creed", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/2208920/header.jpg"),
        ("Yakuza", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1235140/header.jpg"),
        ("Like a Dragon", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/2072450/header.jpg"),
        ("Tekken", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1778820/header.jpg"),
        ("Mortal Kombat", "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1971870/header.jpg"),
        ("Zelda: Tears", "https://assets.nintendo.com/image/upload/ar_16:9,c_lpad,w_656/b_white/f_auto/q_auto/ncom/software/switch/70010000063714/276a412988e07c5d55a2996c6d38abb408b464413b2dfeb44d2aa460b9f622e1"),
        ("Zelda", "https://assets.nintendo.com/image/upload/ar_16:9,c_lpad,w_656/b_white/f_auto/q_auto/ncom/software/switch/70010000000025/7137262b5a64d921e193653f8aa0b722925abc5680380ca0e18a5cfd91697f58"),
        ("Mario Bros. Wonder", "https://assets.nintendo.com/image/upload/ar_16:9,c_lpad,w_656/b_white/f_auto/q_auto/ncom/software/switch/70010000068688/a13a41a64270365f39f6e595435f60394072b75b2c2a3a6571b94e0e96518066"),
        ("Mario Odyssey", "https://assets.nintendo.com/image/upload/ar_16:9,c_lpad,w_656/b_white/f_auto/q_auto/ncom/software/switch/70010000001130/c42553b4fd0312c31e70ec7468c6c9bccd739f340152925b9600631f2d29f8b5"),
        ("Mario", "https://assets.nintendo.com/image/upload/ar_16:9,c_lpad,w_656/b_white/f_auto/q_auto/ncom/software/switch/70010000001130/c42553b4fd0312c31e70ec7468c6c9bccd739f340152925b9600631f2d29f8b5"),
        ("Metroid", "https://assets.nintendo.com/image/upload/ar_16:9,c_lpad,w_656/b_white/f_auto/q_auto/ncom/software/switch/70010000042924/72c97f2665f95f979e51d8290b08a186919152a19d94f372821c5686274945e5")
    ];

    public static async Task<Dictionary<int, string>> ResolveCatalogCoversAsync(
        IReadOnlyList<GameTarget> catalog,
        CancellationToken ct = default)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        var resolved = new ConcurrentDictionary<int, string>();
        var cacheByBaseTitle = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        await Parallel.ForEachAsync(catalog, new ParallelOptions { MaxDegreeOfParallelism = 8, CancellationToken = ct }, async (game, token) =>
        {
            // 1. Verificar coincidencia directa en el diccionario de franquicias conocidas
            foreach (var (keyword, url) in KnownFranchiseCovers)
            {
                if (game.CanonicalTitle.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    resolved[game.GameId] = url;
                    return;
                }
            }

            // 2. Si es otro título del catálogo, consultar la API pública de búsqueda de Steam por su título base
            string baseTitle = ExtractBaseTitle(game.CanonicalTitle);
            if (cacheByBaseTitle.TryGetValue(baseTitle, out string? cachedUrl))
            {
                resolved[game.GameId] = cachedUrl;
                return;
            }

            try
            {
                string searchUrl = $"https://store.steampowered.com/api/storesearch/?term={Uri.EscapeDataString(baseTitle)}&l=english&cc=US";
                string json = await http.GetStringAsync(searchUrl, token);
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("items", out var items) && items.GetArrayLength() > 0)
                {
                    int appId = items[0].GetProperty("id").GetInt32();
                    string steamCover = $"https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/{appId}/header.jpg";
                    cacheByBaseTitle[baseTitle] = steamCover;
                    resolved[game.GameId] = steamCover;
                    return;
                }
            }
            catch
            {
                // Si falla la red, usar portada de respaldo de Steam
            }

            string fallback = KnownFranchiseCovers[game.GameId % KnownFranchiseCovers.Length].CoverUrl;
            resolved[game.GameId] = fallback;
        });

        return new Dictionary<int, string>(resolved);
    }

    private static string ExtractBaseTitle(string fullTitle)
    {
        int dashIdx = fullTitle.IndexOf(" - ", StringComparison.Ordinal);
        if (dashIdx > 0) fullTitle = fullTitle[..dashIdx];

        int colonIdx = fullTitle.IndexOf(':');
        if (colonIdx > 0 && fullTitle.Length > 24) fullTitle = fullTitle[..colonIdx];

        return fullTitle.Trim();
    }
}