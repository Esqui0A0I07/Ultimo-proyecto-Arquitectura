namespace RadarOfertas.Core.Models;

public enum ExecutionMode
{
    Sequential, // Modo 1: 1 solo hilo en todas las etapas (T_sec)
    Concurrent, // Modo 2: E/S con múltiples workers, Emparejamiento CPU en 1 hilo
    Parallel    // Modo 3: E/S con múltiples workers + Emparejamiento CPU multinúcleo
}

public enum CpuMechanism
{
    ParallelForEach,
    Plinq // Para el extra de comparación de mecanismos (+2 pts)
}

public sealed class PipelineConfig
{
    public ExecutionMode Mode { get; init; } = ExecutionMode.Sequential;
    public CpuMechanism MatchingMechanism { get; init; } = CpuMechanism.ParallelForEach;
    public bool IsReplayMode { get; init; } = true;
    public int SimulatedLatencyMs { get; init; } = 120;
    public int SimulatedJitterMs { get; init; } = 0; // 0 = fija, >0 = variación aleatoria
    public int IoWorkers { get; init; } = 32;
    public int CpuWorkers { get; init; } = Environment.ProcessorCount;
    public string ReplayCacheDirectory { get; init; } = "../../replay-cache";
    public string ConnectionString { get; init; } = "Host=ep-shy-lake-bbyw3dvd-pooler.c-15.us-east-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_hA9kWgJoBC6N;SSL Mode=Require;";
}