using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using RadarOfertas.Core.Models;

namespace RadarOfertas.Core.Matching;

public static partial class JaroWinklerMatcher
{
    private static readonly string[] EditionTokens =
        ["goty", "game of the year", "deluxe", "definitive", "remastered", "remake", "bundle", "dlc", "director s cut"];

    private static readonly string[] PlatformSuffixes =
        ["nintendo switch", "ps5", "ps4", "playstation", "xbox series x", "xbox one", "pc", "steam", "digital"];

    [GeneratedRegex(@"[^\w\s]")]
    private static partial Regex PunctuationRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex MultiSpaceRegex();

    public static string NormalizeTitle(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        string text = input.ToLowerInvariant();
        text = PunctuationRegex().Replace(text, " ");

        foreach (string suffix in PlatformSuffixes)
        {
            text = Regex.Replace(text, $@"\b{Regex.Escape(suffix)}\b", " ");
        }

        // Conversión básica de números romanos frecuentes para secuelas
        text = Regex.Replace(text, @"\bvii\b", "7");
        text = Regex.Replace(text, @"\bvi\b", "6");
        text = Regex.Replace(text, @"\biv\b", "4");
        text = Regex.Replace(text, @"\bv\b", "5");
        text = Regex.Replace(text, @"\biii\b", "3");
        text = Regex.Replace(text, @"\bii\b", "2");

        return MultiSpaceRegex().Replace(text, " ").Trim();
    }

    public static double CalculateSimilarity(string s1, string s2)
    {
        if (s1 == s2) return 1.0;
        if (s1.Length == 0 || s2.Length == 0) return 0.0;

        int matchDistance = Math.Max(s1.Length, s2.Length) / 2 - 1;
        bool[] s1Matches = new bool[s1.Length];
        bool[] s2Matches = new bool[s2.Length];

        int matches = 0;
        for (int i = 0; i < s1.Length; i++)
        {
            int start = Math.Max(0, i - matchDistance);
            int end = Math.Min(i + matchDistance + 1, s2.Length);

            for (int j = start; j < end; j++)
            {
                if (s2Matches[j] || s1[i] != s2[j]) continue;
                s1Matches[i] = true;
                s2Matches[j] = true;
                matches++;
                break;
            }
        }

        if (matches == 0) return 0.0;

        double transpositions = 0;
        int k = 0;
        for (int i = 0; i < s1.Length; i++)
        {
            if (!s1Matches[i]) continue;
            while (!s2Matches[k]) k++;
            if (s1[i] != s2[k]) transpositions++;
            k++;
        }

        double m = matches;
        double jaro = ((m / s1.Length) + (m / s2.Length) + ((m - transpositions / 2.0) / m)) / 3.0;

        int prefix = 0;
        int maxPrefix = Math.Min(4, Math.Min(s1.Length, s2.Length));
        for (int i = 0; i < maxPrefix; i++)
        {
            if (s1[i] == s2[i]) prefix++;
            else break;
        }

        return jaro + (prefix * 0.1 * (1.0 - jaro));
    }

    public static IReadOnlyList<MatchedStoreOffer> MatchCandidates(
        GameTarget game,
        IReadOnlyList<StoreCandidate> allCandidates,
        ExecutionMode mode,
        int cpuWorkers,
        CpuMechanism mechanism = CpuMechanism.ParallelForEach,
        double acceptThreshold = 0.88,
        double dubiousLowerBound = 0.78)
    {
        string normalizedCanonical = NormalizeTitle(game.CanonicalTitle);

        // Modo 1 y Modo 2 ejecutan el emparejamiento en 1 solo hilo
        if (mode != ExecutionMode.Parallel || cpuWorkers <= 1)
        {
            var results = new List<MatchedStoreOffer>(allCandidates.Count);
            foreach (var candidate in allCandidates)
            {
                results.Add(EvaluateSingle(game, normalizedCanonical, candidate, acceptThreshold, dubiousLowerBound));
            }
            return results.Where(r => r.IsAccepted || r.IsDubious).ToList();
        }

        // Extra (+2 pts): Comparación entre Parallel.ForEach y PLINQ
        if (mechanism == CpuMechanism.Plinq)
        {
            return allCandidates
                .AsParallel()
                .WithDegreeOfParallelism(cpuWorkers)
                .Select(c => EvaluateSingle(game, normalizedCanonical, c, acceptThreshold, dubiousLowerBound))
                .Where(r => r.IsAccepted || r.IsDubious)
                .ToList();
        }

        var bag = new ConcurrentBag<MatchedStoreOffer>();
        var options = new ParallelOptions { MaxDegreeOfParallelism = cpuWorkers };

        Parallel.ForEach(allCandidates, options, candidate =>
        {
            var evaluated = EvaluateSingle(game, normalizedCanonical, candidate, acceptThreshold, dubiousLowerBound);
            if (evaluated.IsAccepted || evaluated.IsDubious)
            {
                bag.Add(evaluated);
            }
        });

        return bag.ToList();
    }

    private static MatchedStoreOffer EvaluateSingle(
        GameTarget game,
        string normalizedCanonical,
        StoreCandidate candidate,
        double acceptThreshold,
        double dubiousLowerBound)
    {
        string rawLower = candidate.RawTitle.ToLowerInvariant();
        string normalizedCandidate = NormalizeTitle(candidate.RawTitle);
        double score = CalculateSimilarity(normalizedCanonical, normalizedCandidate);

        bool hasSpecialToken = EditionTokens.Any(t => rawLower.Contains(t));
        bool isAccepted = score >= acceptThreshold && !hasSpecialToken;
        bool isDubious = (score >= dubiousLowerBound && score < acceptThreshold) || (score >= acceptThreshold && hasSpecialToken);

        // ADR-003: Criterio combinado de oferta
        bool isOnSale = false;
        decimal discountPct = 0m;

        if (candidate.RegularPriceUsd is > 0m && candidate.CurrentPriceUsd <= candidate.RegularPriceUsd.Value * 0.95m)
        {
            isOnSale = true;
            discountPct = Math.Round((1m - (candidate.CurrentPriceUsd / candidate.RegularPriceUsd.Value)) * 100m, 2);
        }
        else if (game.MsrpUsd > 0m && candidate.CurrentPriceUsd <= game.MsrpUsd * 0.90m)
        {
            isOnSale = true;
            discountPct = Math.Round((1m - (candidate.CurrentPriceUsd / game.MsrpUsd)) * 100m, 2);
        }

        return new MatchedStoreOffer(candidate, score, isAccepted, isDubious, isOnSale, discountPct);
    }
}