using System.Collections.Concurrent;
using System.Numerics;
using RadarOfertas.Core.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace RadarOfertas.Core.Imaging;

public sealed record CoverAnalysisResult(
    int GameId,
    string DominantColorHex,
    string PerceptualHashHex,
    int HammingDistanceToCandidate,
    bool IsSameCoverVerified,
    int NormalizedSizeBytes);

public static class CoverImageProcessor
{
    private const int TargetWidth = 400;
    private const int TargetHeight = 225;
    private const int MaxAcceptedHammingDistance = 10;

    private static readonly JpegEncoder UniformJpegEncoder = new()
    {
        Quality = 82
    };

    static CoverImageProcessor()
    {
        // Desactiva el paralelismo interno de ImageSharp para que el escalamiento
        // dependa exclusivamente de los workers de nuestro Parallel.ForEach / PLINQ
        Configuration.Default.MaxDegreeOfParallelism = 1;
    }

    public static IReadOnlyList<(int GameId, byte[] CanonicalCoverBytes, byte[] StoreCandidateBytes)> GenerateReplayCoverDataset(
        IReadOnlyList<GameTarget> catalog,
        int sourceWidth = 640,
        int sourceHeight = 360)
    {
        var dataset = new List<(int, byte[], byte[])>(catalog.Count);

        foreach (var game in catalog)
        {
            byte[] canonicalBytes = CreateSyntheticCoverBytes(game.GameId, sourceWidth, sourceHeight, variantOffset: 0);
            int offset = (game.GameId % 10 == 0) ? 120 : 3;
            byte[] candidateBytes = CreateSyntheticCoverBytes(game.GameId, sourceWidth, sourceHeight, variantOffset: offset);

            dataset.Add((game.GameId, canonicalBytes, candidateBytes));
        }

        return dataset;
    }

    public static IReadOnlyList<CoverAnalysisResult> ProcessCatalogCovers(
        IReadOnlyList<(int GameId, byte[] CanonicalCoverBytes, byte[] StoreCandidateBytes)> dataset,
        ExecutionMode mode,
        int cpuWorkers,
        CpuMechanism mechanism = CpuMechanism.ParallelForEach)
    {
        if (mode != ExecutionMode.Parallel || cpuWorkers <= 1)
        {
            var sequentialList = new List<CoverAnalysisResult>(dataset.Count);
            foreach (var item in dataset)
            {
                sequentialList.Add(AnalyzeSingleGamePair(item.GameId, item.CanonicalCoverBytes, item.StoreCandidateBytes));
            }
            return sequentialList;
        }

        if (mechanism == CpuMechanism.Plinq)
        {
            return dataset
                .AsParallel()
                .WithDegreeOfParallelism(cpuWorkers)
                .Select(item => AnalyzeSingleGamePair(item.GameId, item.CanonicalCoverBytes, item.StoreCandidateBytes))
                .ToList();
        }

        var results = new ConcurrentBag<CoverAnalysisResult>();
        var options = new ParallelOptions { MaxDegreeOfParallelism = cpuWorkers };

        Parallel.ForEach(dataset, options, item =>
        {
            results.Add(AnalyzeSingleGamePair(item.GameId, item.CanonicalCoverBytes, item.StoreCandidateBytes));
        });

        return results.ToList();
    }

    public static CoverAnalysisResult AnalyzeSingleGamePair(
        int gameId,
        byte[] canonicalBytes,
        byte[] candidateBytes)
    {
        using var canonicalImg = Image.Load<Rgba32>(canonicalBytes);
        using var candidateImg = Image.Load<Rgba32>(candidateBytes);

        canonicalImg.Mutate(ctx => ctx.Resize(TargetWidth, TargetHeight, KnownResamplers.Bicubic));

        using var outStream = new MemoryStream(16384);
        canonicalImg.Save(outStream, UniformJpegEncoder);
        int normalizedBytesCount = (int)outStream.Length;

        string dominantHex = ExtractDominantColorHex(canonicalImg);

        ulong canonicalHash = ComputePerceptualDHash64(canonicalImg);
        ulong candidateHash = ComputePerceptualDHash64(candidateImg);

        int hammingDistance = BitOperations.PopCount(canonicalHash ^ candidateHash);
        bool isSameCover = hammingDistance <= MaxAcceptedHammingDistance;

        return new CoverAnalysisResult(
            gameId,
            dominantHex,
            canonicalHash.ToString("X16"),
            hammingDistance,
            isSameCover,
            normalizedBytesCount);
    }

    private static string ExtractDominantColorHex(Image<Rgba32> image)
    {
        Span<int> buckets = stackalloc int[4096];
        Span<long> sumR = stackalloc long[4096];
        Span<long> sumG = stackalloc long[4096];
        Span<long> sumB = stackalloc long[4096];

        int bestBucket = 0;
        int maxCount = -1;

        for (int y = 0; y < image.Height; y += 2)
        {
            for (int x = 0; x < image.Width; x += 2)
            {
                Rgba32 p = image[x, y];
                int brightness = (p.R + p.G + p.B) / 3;
                if (brightness < 25 || brightness > 235) continue;

                int idx = ((p.R >> 4) << 8) | ((p.G >> 4) << 4) | (p.B >> 4);
                int count = ++buckets[idx];
                sumR[idx] += p.R;
                sumG[idx] += p.G;
                sumB[idx] += p.B;

                if (count > maxCount)
                {
                    maxCount = count;
                    bestBucket = idx;
                }
            }
        }

        if (maxCount <= 0) return "#102973";

        byte r = (byte)(sumR[bestBucket] / maxCount);
        byte g = (byte)(sumG[bestBucket] / maxCount);
        byte b = (byte)(sumB[bestBucket] / maxCount);

        return $"#{r:X2}{g:X2}{b:X2}";
    }

    private static ulong ComputePerceptualDHash64(Image<Rgba32> source)
    {
        using var tiny = source.Clone(ctx => ctx.Resize(9, 8, KnownResamplers.Bicubic));
        ulong hash = 0UL;
        int bitIndex = 0;

        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                Rgba32 left = tiny[x, y];
                Rgba32 right = tiny[x + 1, y];

                int lumaLeft = (left.R * 299 + left.G * 587 + left.B * 114) / 1000;
                int lumaRight = (right.R * 299 + right.G * 587 + right.B * 114) / 1000;

                if (lumaLeft > lumaRight)
                {
                    hash |= (1UL << bitIndex);
                }
                bitIndex++;
            }
        }

        return hash;
    }

    private static byte[] CreateSyntheticCoverBytes(int gameId, int width, int height, int variantOffset)
    {
        using var img = new Image<Rgba32>(width, height);

        byte baseR = (byte)((gameId * 47 + variantOffset) % 200 + 30);
        byte baseG = (byte)((gameId * 89 + variantOffset * 2) % 180 + 40);
        byte baseB = (byte)((gameId * 131 + variantOffset * 3) % 210 + 35);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                byte r = (byte)Math.Clamp(baseR + ((x * 40) / width) - ((y * 20) / height), 0, 255);
                byte g = (byte)Math.Clamp(baseG + ((y * 50) / height), 0, 255);
                byte b = (byte)Math.Clamp(baseB + (((x ^ y) + variantOffset) & 31), 0, 255);
                img[x, y] = new Rgba32(r, g, b);
            }
        }

        using var ms = new MemoryStream(16384);
        img.Save(ms, UniformJpegEncoder);
        return ms.ToArray();
    }
}