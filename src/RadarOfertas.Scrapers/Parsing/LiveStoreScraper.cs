using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using RadarOfertas.Core.Matching;
using RadarOfertas.Core.Models;

namespace RadarOfertas.Scrapers.Parsing;

public static partial class LiveStoreScraper
{
    private static readonly HtmlParser HtmlParser = new();

    private const string NintendoAlgoliaUrl = "https://u3b6gr4ua3-dsn.algolia.net/1/indexes/*/queries";
    private const string NintendoAlgoliaAppId = "U3B6GR4UA3";
    private const string NintendoAlgoliaApiKey = "a29c6927638bfd8cee23993e51e721c9";

    [GeneratedRegex(@"\$\s*(\d+\.\d{2})")]
    private static partial Regex UsdPriceRegex();

    public static async Task<IReadOnlyList<StoreCandidate>> FetchRealCandidatesForGameAsync(
        HttpClient httpClient,
        GameTarget game,
        SemaphoreSlim ioGate,
        CancellationToken ct = default)
    {
        var tasks = new List<Task<StoreCandidate?>>();

        foreach (string plat in game.Platforms)
        {
            if (plat.Equals("PC", StringComparison.OrdinalIgnoreCase))
            {
                tasks.Add(ExecuteGuardedAsync(ioGate, () => FetchSteamRealOfferAsync(httpClient, game, ct), ct));
            }
            else if (plat.Equals("Xbox", StringComparison.OrdinalIgnoreCase))
            {
                tasks.Add(ExecuteGuardedAsync(ioGate, () => FetchXboxRealOfferAsync(httpClient, game, ct), ct));
            }
            else if (plat.Equals("Nintendo Switch", StringComparison.OrdinalIgnoreCase))
            {
                tasks.Add(ExecuteGuardedAsync(ioGate, () => FetchNintendoEshopRealOfferAsync(httpClient, game, ct), ct));
            }
            else if (plat.Equals("PlayStation", StringComparison.OrdinalIgnoreCase))
            {
                tasks.Add(ExecuteGuardedAsync(ioGate, () => FetchPlayStationAndRetailRealOfferAsync(httpClient, game, ct), ct));
            }
        }

        var results = await Task.WhenAll(tasks);
        return results.Where(r => r is not null).Select(r => r!).ToList();
    }

    private static async Task<StoreCandidate?> ExecuteGuardedAsync(
        SemaphoreSlim gate,
        Func<Task<StoreCandidate?>> action,
        CancellationToken ct)
    {
        await gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return await action().ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
        finally
        {
            gate.Release();
        }
    }

    private static async Task<StoreCandidate?> FetchSteamRealOfferAsync(
        HttpClient http,
        GameTarget game,
        CancellationToken ct)
    {
        string cleanQuery = CleanSearchTitle(game.CanonicalTitle);

        string apiUrl = $"https://store.steampowered.com/api/storesearch/?term={Uri.EscapeDataString(cleanQuery)}&l=english&cc=US";
        using (var resp = await http.GetAsync(apiUrl, ct))
        {
            if (resp.IsSuccessStatusCode)
            {
                string json = await resp.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("items", out var items) && items.GetArrayLength() > 0)
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        string name = item.GetProperty("name").GetString() ?? string.Empty;
                        double sim = JaroWinklerMatcher.CalculateSimilarity(
                            JaroWinklerMatcher.NormalizeTitle(game.CanonicalTitle),
                            JaroWinklerMatcher.NormalizeTitle(name));

                        if (sim < 0.75) continue;

                        if (item.TryGetProperty("price", out var priceObj))
                        {
                            decimal finalUsd = priceObj.GetProperty("final").GetInt32() / 100.0m;
                            decimal initialUsd = priceObj.GetProperty("initial").GetInt32() / 100.0m;
                            if (finalUsd <= 0) continue;

                            int appId = item.GetProperty("id").GetInt32();
                            string? tinyImg = item.TryGetProperty("tiny_image", out var ti) ? ti.GetString() : null;

                            string coverUrl = $"https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/{appId}/header.jpg";
                            if (!string.IsNullOrWhiteSpace(tinyImg))
                            {
                                coverUrl = tinyImg.Contains($"/apps/{appId}/capsule_", StringComparison.OrdinalIgnoreCase)
                                    ? tinyImg.Replace("capsule_231x87.jpg", "header.jpg", StringComparison.OrdinalIgnoreCase)
                                    : tinyImg;
                            }

                            string productUrl = $"https://store.steampowered.com/app/{appId}";

                            return new StoreCandidate(
                                "steam",
                                name,
                                finalUsd,
                                initialUsd > finalUsd ? initialUsd : finalUsd,
                                "digital",
                                productUrl,
                                coverUrl);
                        }
                    }
                }
            }
        }

        string htmlUrl = $"https://store.steampowered.com/search/results/?term={Uri.EscapeDataString(cleanQuery)}&cc=us&l=english&category1=998";
        string html = await http.GetStringAsync(htmlUrl, ct);
        using var htmlDoc = await HtmlParser.ParseDocumentAsync(html, ct);

        var row = htmlDoc.QuerySelector("a.search_result_row");
        if (row is null) return null;

        string rawTitle = row.QuerySelector(".search_name .title")?.TextContent.Trim() ?? game.CanonicalTitle;
        string? href = row.GetAttribute("href");
        string? imgUrl = row.QuerySelector(".search_capsule img")?.GetAttribute("src");

        string finalPriceText = row.QuerySelector(".discount_final_price")?.TextContent ?? string.Empty;
        string origPriceText = row.QuerySelector(".discount_original_price")?.TextContent ?? string.Empty;

        var finalMatch = UsdPriceRegex().Match(finalPriceText);
        if (!finalMatch.Success || !decimal.TryParse(finalMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal curPrice))
            return null;

        decimal regPrice = curPrice;
        var origMatch = UsdPriceRegex().Match(origPriceText);
        if (origMatch.Success && decimal.TryParse(origMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedOrig))
        {
            regPrice = parsedOrig;
        }

        return new StoreCandidate(
            "steam",
            rawTitle,
            curPrice,
            regPrice,
            "digital",
            href ?? "https://store.steampowered.com",
            imgUrl);
    }

    private static async Task<StoreCandidate?> FetchXboxRealOfferAsync(
        HttpClient http,
        GameTarget game,
        CancellationToken ct)
    {
        string cleanQuery = CleanSearchTitle(game.CanonicalTitle);
        string suggestUrl = $"https://displaycatalog.mp.microsoft.com/v7.0/productFamilies/autosuggest?market=US&languages=en-US&query={Uri.EscapeDataString(cleanQuery)}&productFamilyNames=Games";

        using var suggestResp = await http.GetAsync(suggestUrl, ct);
        if (!suggestResp.IsSuccessStatusCode) return null;

        string suggestJson = await suggestResp.Content.ReadAsStringAsync(ct);
        using var suggestDoc = JsonDocument.Parse(suggestJson);

        string? bigId = null;
        string? matchedTitle = null;

        if (suggestDoc.RootElement.TryGetProperty("Results", out var results))
        {
            foreach (var family in results.EnumerateArray())
            {
                if (!family.TryGetProperty("Products", out var products)) continue;
                foreach (var prod in products.EnumerateArray())
                {
                    string title = prod.TryGetProperty("Title", out var t) ? (t.GetString() ?? "") : "";
                    string id = prod.TryGetProperty("ProductId", out var pid) ? (pid.GetString() ?? "") : "";
                    if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(title))
                    {
                        double sim = JaroWinklerMatcher.CalculateSimilarity(
                            JaroWinklerMatcher.NormalizeTitle(game.CanonicalTitle),
                            JaroWinklerMatcher.NormalizeTitle(title));

                        if (sim >= 0.72)
                        {
                            bigId = id;
                            matchedTitle = title;
                            break;
                        }
                    }
                }
                if (bigId is not null) break;
            }
        }

        if (string.IsNullOrWhiteSpace(bigId)) return null;

        string detailsUrl = $"https://displaycatalog.mp.microsoft.com/v7.0/products?bigIds={bigId}&market=US&languages=en-US&MS-CV=DGU1mcuYo0WMMp+F.1";
        using var detailsResp = await http.GetAsync(detailsUrl, ct);
        if (!detailsResp.IsSuccessStatusCode) return null;

        string detailsJson = await detailsResp.Content.ReadAsStringAsync(ct);
        using var detailsDoc = JsonDocument.Parse(detailsJson);

        if (!detailsDoc.RootElement.TryGetProperty("Products", out var prodArray) || prodArray.GetArrayLength() == 0)
            return null;

        var product = prodArray[0];

        string? xboxCoverUrl = null;
        if (product.TryGetProperty("LocalizedProperties", out var locProps) &&
            locProps.GetArrayLength() > 0 &&
            locProps[0].TryGetProperty("Images", out var imagesArr))
        {
            foreach (var img in imagesArr.EnumerateArray())
            {
                string purpose = img.TryGetProperty("ImagePurpose", out var ip) ? (ip.GetString() ?? "") : "";
                string uri = img.TryGetProperty("Uri", out var u) ? (u.GetString() ?? "") : "";
                if (!string.IsNullOrWhiteSpace(uri) &&
                    (purpose.Equals("SuperHeroArt", StringComparison.OrdinalIgnoreCase) ||
                     purpose.Equals("TitledHeroArt", StringComparison.OrdinalIgnoreCase) ||
                     purpose.Equals("BoxArt", StringComparison.OrdinalIgnoreCase)))
                {
                    xboxCoverUrl = uri.StartsWith("//") ? $"https:{uri}?w=460" : uri;
                    break;
                }
            }
        }

        if (!product.TryGetProperty("DisplaySkuAvailabilities", out var skuArray))
            return null;

        foreach (var sku in skuArray.EnumerateArray())
        {
            if (!sku.TryGetProperty("Availabilities", out var availArray)) continue;
            foreach (var avail in availArray.EnumerateArray())
            {
                if (avail.TryGetProperty("OrderManagementData", out var orderData) &&
                    orderData.TryGetProperty("Price", out var priceNode))
                {
                    decimal listPrice = priceNode.TryGetProperty("ListPrice", out var lp) ? lp.GetDecimal() : 0m;
                    decimal msrp = priceNode.TryGetProperty("MSRP", out var mp) ? mp.GetDecimal() : listPrice;

                    if (listPrice > 0m)
                    {
                        decimal regular = msrp >= listPrice ? msrp : listPrice;
                        string productUrl = $"https://www.xbox.com/en-US/games/store/_/{bigId}";
                        return new StoreCandidate(
                            "xbox",
                            matchedTitle ?? game.CanonicalTitle,
                            listPrice,
                            regular,
                            "digital",
                            productUrl,
                            xboxCoverUrl);
                    }
                }
            }
        }

        return null;
    }

    private static async Task<StoreCandidate?> FetchNintendoEshopRealOfferAsync(
        HttpClient http,
        GameTarget game,
        CancellationToken ct)
    {
        string cleanQuery = CleanSearchTitle(game.CanonicalTitle);

        var payload = new
        {
            requests = new[]
            {
                new
                {
                    indexName = "store_game_en_us",
                    @params = $"query={Uri.EscapeDataString(cleanQuery)}&hitsPerPage=5"
                }
            }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, NintendoAlgoliaUrl);
        req.Headers.Add("x-algolia-application-id", NintendoAlgoliaAppId);
        req.Headers.Add("x-algolia-api-key", NintendoAlgoliaApiKey);
        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var resp = await http.SendAsync(req, ct);
        if (resp.IsSuccessStatusCode)
        {
            string json = await resp.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("results", out var resArr) &&
                resArr.GetArrayLength() > 0 &&
                resArr[0].TryGetProperty("hits", out var hits))
            {
                foreach (var hit in hits.EnumerateArray())
                {
                    string title = hit.TryGetProperty("title", out var t) ? (t.GetString() ?? "") : "";
                    double sim = JaroWinklerMatcher.CalculateSimilarity(
                        JaroWinklerMatcher.NormalizeTitle(game.CanonicalTitle),
                        JaroWinklerMatcher.NormalizeTitle(title));

                    if (sim < 0.72) continue;

                    decimal msrp = 0m;
                    decimal currentPrice = 0m;

                    if (hit.TryGetProperty("price", out var priceObj) && priceObj.ValueKind == JsonValueKind.Object)
                    {
                        if (priceObj.TryGetProperty("regPrice", out var rp) && rp.ValueKind == JsonValueKind.Number)
                            msrp = rp.GetDecimal();
                        if (priceObj.TryGetProperty("salePrice", out var sp) && sp.ValueKind == JsonValueKind.Number)
                            currentPrice = sp.GetDecimal();
                        if (currentPrice <= 0 && priceObj.TryGetProperty("finalPrice", out var fp) && fp.ValueKind == JsonValueKind.Number)
                            currentPrice = fp.GetDecimal();
                    }

                    if (msrp <= 0 && hit.TryGetProperty("msrp", out var msrpProp) && msrpProp.ValueKind == JsonValueKind.Number)
                        msrp = msrpProp.GetDecimal();

                    if (currentPrice <= 0 && hit.TryGetProperty("salePrice", out var saleProp) && saleProp.ValueKind == JsonValueKind.Number)
                        currentPrice = saleProp.GetDecimal();

                    if (currentPrice <= 0) currentPrice = msrp;
                    if (msrp < currentPrice) msrp = currentPrice;

                    if (currentPrice > 0)
                    {
                        string urlSlug = hit.TryGetProperty("url", out var u) ? (u.GetString() ?? "") : "";
                        string fullUrl = urlSlug.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                            ? urlSlug
                            : $"https://www.nintendo.com{urlSlug}";

                        string? coverImg = null;
                        if (hit.TryGetProperty("productImage", out var pi) && pi.ValueKind == JsonValueKind.String)
                        {
                            string rawImg = pi.GetString() ?? "";
                            if (!string.IsNullOrWhiteSpace(rawImg))
                            {
                                coverImg = rawImg.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                                    ? rawImg
                                    : $"https://assets.nintendo.com/image/upload/ar_16:9,c_lpad,w_656/b_white/f_auto/q_auto/{rawImg.TrimStart('/')}";
                            }
                        }

                        return new StoreCandidate(
                            "eshop",
                            title,
                            currentPrice,
                            msrp,
                            "digital",
                            fullUrl,
                            coverImg);
                    }
                }
            }
        }

        return await FetchDekuDealsHtmlOfferAsync(http, game, "eshop", "digital", ct);
    }

    private static async Task<StoreCandidate?> FetchPlayStationAndRetailRealOfferAsync(
        HttpClient http,
        GameTarget game,
        CancellationToken ct)
    {
        return await FetchDekuDealsHtmlOfferAsync(http, game, "gamestop", "physical", ct);
    }

    private static async Task<StoreCandidate?> FetchDekuDealsHtmlOfferAsync(
        HttpClient http,
        GameTarget game,
        string storeCode,
        string format,
        CancellationToken ct)
    {
        string cleanQuery = CleanSearchTitle(game.CanonicalTitle);
        string url = $"https://www.dekudeals.com/search?q={Uri.EscapeDataString(cleanQuery)}";

        string html = await http.GetStringAsync(url, ct);
        using var doc = await HtmlParser.ParseDocumentAsync(html, ct);

        var cards = doc.QuerySelectorAll(".search-main .col, .cell, .d-block");
        foreach (var card in cards)
        {
            var nameNode = card.QuerySelector(".name, h6, a.main-link");
            if (nameNode is null) continue;

            string title = nameNode.TextContent.Trim();
            double sim = JaroWinklerMatcher.CalculateSimilarity(
                JaroWinklerMatcher.NormalizeTitle(game.CanonicalTitle),
                JaroWinklerMatcher.NormalizeTitle(title));

            if (sim < 0.72) continue;

            var matches = UsdPriceRegex().Matches(card.TextContent);
            if (matches.Count >= 1 &&
                decimal.TryParse(matches[0].Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal p1))
            {
                decimal curPrice = p1;
                decimal regPrice = p1;

                if (matches.Count >= 2 &&
                    decimal.TryParse(matches[1].Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal p2))
                {
                    curPrice = Math.Min(p1, p2);
                    regPrice = Math.Max(p1, p2);
                }

                string href = card.QuerySelector("a")?.GetAttribute("href") ?? "";
                string prodUrl = href.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? href
                    : $"https://www.dekudeals.com{href}";

                string? imgUrl = card.QuerySelector("img")?.GetAttribute("src");

                return new StoreCandidate(storeCode, title, curPrice, regPrice, format, prodUrl, imgUrl);
            }
        }

        return null;
    }

    private static string CleanSearchTitle(string title)
    {
        return title
            .Replace(" - Complete Edition", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" - Digital Deluxe Edition", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" (Switch & PC)", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" (Switch)", "", StringComparison.OrdinalIgnoreCase)
            .Trim();
    }
}