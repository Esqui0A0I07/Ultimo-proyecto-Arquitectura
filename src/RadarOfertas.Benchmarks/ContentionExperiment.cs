using System.Collections.Concurrent;
using System.Diagnostics;

namespace RadarOfertas.Benchmarks;

public sealed record ContentionResult(
    string Mechanism,
    long ElapsedMs,
    int ExpectedTotal,
    int ActualTotal);

public static class ContentionExperiment
{
    public static IReadOnlyList<ContentionResult> RunAll(int totalIterations = 2_000_000, int workers = 12)
    {
        string[] platforms = ["PC", "PlayStation", "Nintendo Switch", "Xbox"];
        var results = new List<ContentionResult>(4)
        {
            RunUnsafeRaceCondition(platforms, totalIterations, workers),
            RunWithGlobalLock(platforms, totalIterations, workers),
            RunWithConcurrentDictionary(platforms, totalIterations, workers),
            RunWithThreadLocal(platforms, totalIterations, workers)
        };
        return results;
    }

    // 1. Sin sincronización: Demuestra condición de carrera real (pérdida de actualizaciones read-modify-write)
    private static ContentionResult RunUnsafeRaceCondition(string[] platforms, int total, int workers)
    {
        var counts = platforms.ToDictionary(p => p, _ => 0);
        var sw = Stopwatch.StartNew();

        Parallel.For(0, total, new ParallelOptions { MaxDegreeOfParallelism = workers }, i =>
        {
            string key = platforms[i % platforms.Length];
            counts[key] = counts[key] + 1; // Condición de carrera intencional
        });

        sw.Stop();
        return new ContentionResult("1. Sin Sincronización (Race Condition)", sw.ElapsedMilliseconds, total, counts.Values.Sum());
    }

    // 2. Lock global
    private static ContentionResult RunWithGlobalLock(string[] platforms, int total, int workers)
    {
        var counts = platforms.ToDictionary(p => p, _ => 0);
        object syncLock = new();
        var sw = Stopwatch.StartNew();

        Parallel.For(0, total, new ParallelOptions { MaxDegreeOfParallelism = workers }, i =>
        {
            string key = platforms[i % platforms.Length];
            lock (syncLock)
            {
                counts[key]++;
            }
        });

        sw.Stop();
        return new ContentionResult("2. Lock Global", sw.ElapsedMilliseconds, total, counts.Values.Sum());
    }

    // 3. Colección concurrente
    private static ContentionResult RunWithConcurrentDictionary(string[] platforms, int total, int workers)
    {
        var counts = new ConcurrentDictionary<string, int>(
            platforms.Select(p => new KeyValuePair<string, int>(p, 0)));
        var sw = Stopwatch.StartNew();

        Parallel.For(0, total, new ParallelOptions { MaxDegreeOfParallelism = workers }, i =>
        {
            string key = platforms[i % platforms.Length];
            counts.AddOrUpdate(key, 1, (_, current) => current + 1);
        });

        sw.Stop();
        return new ContentionResult("3. ConcurrentDictionary", sw.ElapsedMilliseconds, total, counts.Values.Sum());
    }

    // 4. Estado local por hilo combinado al final
    private static ContentionResult RunWithThreadLocal(string[] platforms, int total, int workers)
    {
        var finalCounts = platforms.ToDictionary(p => p, _ => 0);
        object mergeLock = new();
        using var threadLocal = new ThreadLocal<Dictionary<string, int>>(
            () => platforms.ToDictionary(p => p, _ => 0), trackAllValues: true);

        var sw = Stopwatch.StartNew();

        Parallel.For(0, total, new ParallelOptions { MaxDegreeOfParallelism = workers }, i =>
        {
            string key = platforms[i % platforms.Length];
            threadLocal.Value![key]++;
        });

        foreach (var localDict in threadLocal.Values)
        {
            lock (mergeLock)
            {
                foreach (var kvp in localDict)
                {
                    finalCounts[kvp.Key] += kvp.Value;
                }
            }
        }

        sw.Stop();
        return new ContentionResult("4. ThreadLocal<T> + Reducción Final", sw.ElapsedMilliseconds, total, finalCounts.Values.Sum());
    }
}