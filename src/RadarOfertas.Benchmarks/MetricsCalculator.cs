namespace RadarOfertas.Benchmarks;

public static class MetricsCalculator
{
    public static double SpeedupAbsolute(double tSec, double tParP) => tSec / tParP;

    public static double SpeedupRelative(double tPar1, double tParP) => tPar1 / tParP;

    public static double Efficiency(double sRelP, int p) => sRelP / p;

    public static double AmdahlLaw(double serialFractionF, int p) =>
        1.0 / (serialFractionF + ((1.0 - serialFractionF) / p));

    public static double KarpFlattMetric(double sRelP, int p)
    {
        if (p <= 1) return 0.0;
        return ((1.0 / sRelP) - (1.0 / p)) / (1.0 - (1.0 / p));
    }
}