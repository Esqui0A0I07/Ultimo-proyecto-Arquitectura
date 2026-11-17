using System.Text;

namespace RadarOfertas.Core.Matching;

public sealed record ValidationPair(
    int Id,
    string CanonicalTitle,
    string StoreCandidateTitle,
    bool IsTrueMatch,
    string Category);

public sealed record ThresholdMetrics(
    double Threshold,
    int TruePositives,
    int FalsePositives,
    int TrueNegatives,
    int FalseNegatives,
    double Precision,
    double Recall,
    double F1Score);

public static class MatchingValidator
{
    private static readonly string[] DisqualifyingTokens =
        ["dlc", "season pass", "soundtrack", "artbook", "expansion", "upgrade", "bundle"];

    public static IReadOnlyList<ValidationPair> GetManualValidationSet() =>
    [
        // 1-16: Coincidencias verdaderas con variaciones de plataforma, signos y números romanos (IsTrueMatch = true)
        new(1,  "The Legend of Zelda: Breath of the Wild", "The Legend of Zelda Breath of the Wild - Nintendo Switch Digital", true, "Sufijo plataforma"),
        new(2,  "The Witcher 3: Wild Hunt", "The Witcher III: Wild Hunt (PC / Steam)", true, "Número romano III -> 3"),
        new(3,  "Final Fantasy VII Remake", "FINAL FANTASY 7 REMAKE (PS5)", true, "Romano VII -> 7"),
        new(4,  "Resident Evil 4", "Resident Evil IV (Xbox Series X)", true, "Romano IV -> 4"),
        new(5,  "Grand Theft Auto V", "Grand Theft Auto 5 - PC Digital", true, "Romano V -> 5"),
        new(6,  "Dark Souls III", "Dark Souls 3 (PlayStation 4)", true, "Romano III -> 3"),
        new(7,  "Red Dead Redemption 2", "Red Dead Redemption II - Xbox", true, "Romano II -> 2"),
        new(8,  "God of War Ragnarök", "God of War Ragnarok (PS5)", true, "Carácter especial ö"),
        new(9,  "Marvel's Spider-Man 2", "Marvels Spider Man II - PS5", true, "Puntuación y romano"),
        new(10, "Baldur's Gate 3", "Baldurs Gate III (PC Steam)", true, "Apóstrofo y romano"),
        new(11, "NieR:Automata", "NieR Automata (PC)", true, "Dos puntos pegados"),
        new(12, "Dragon Quest XI S: Echoes of an Elusive Age", "Dragon Quest XI S Echoes of an Elusive Age (Switch)", true, "Título largo"),
        new(13, "Monster Hunter Rise", "Monster Hunter: Rise - Nintendo Switch", true, "Signos de puntuación"),
        new(14, "Street Fighter 6", "Street Fighter VI (PC / Steam)", true, "Romano VI -> 6"),
        new(15, "Diablo IV", "Diablo 4 - Xbox Series X", true, "Romano IV -> 4"),
        new(16, "Persona 5 Royal", "Persona V Royal (Nintendo Switch)", true, "Romano V -> 5"),

        // 17-32: Más coincidencias verdaderas con formatos de tienda reales (IsTrueMatch = true)
        new(17, "Hollow Knight", "Hollow Knight - Digital Code (Nintendo Switch)", true, "Sufijo tienda"),
        new(18, "Elden Ring", "ELDEN RING™ (PlayStation 5)", true, "Símbolo TM y mayúsculas"),
        new(19, "Cyberpunk 2077", "Cyberpunk 2077 [PC] - Steam", true, "Corchetes de plataforma"),
        new(20, "Hades", "Hades (Nintendo Switch)", true, "Título corto"),
        new(21, "Celeste", "Celeste - PC/Mac/Linux (Steam)", true, "Sufijo SO"),
        new(22, "Stardew Valley", "Stardew Valley - Xbox One / Series X", true, "Múltiples plataformas"),
        new(23, "Sekiro: Shadows Die Twice", "Sekiro Shadows Die Twice (PS4)", true, "Puntuación"),
        new(24, "Metroid Dread", "Metroid™ Dread - Nintendo Switch", true, "Marca registrada"),
        new(25, "Super Mario Bros. Wonder", "Super Mario Bros Wonder (Switch)", true, "Abreviatura con punto"),
        new(26, "Super Mario Odyssey", "Super Mario Odyssey - Digital", true, "Sufijo formato"),
        new(27, "Lies of P", "Lies of P (PlayStation 5)", true, "Título corto"),
        new(28, "Alan Wake 2", "Alan Wake II - Xbox Series X", true, "Romano II -> 2"),
        new(29, "Divinity: Original Sin 2", "Divinity Original Sin II (PC)", true, "Romano y dos puntos"),
        new(30, "Death Stranding", "DEATH STRANDING (PC Steam)", true, "Mayúsculas"),
        new(31, "Ghost of Tsushima", "Ghost of Tsushima (PS5)", true, "Estándar"),
        new(32, "DOOM Eternal", "Doom Eternal - PC Digital", true, "Capitalización"),

        // 33-48: Casos difíciles NEGATIVOS: Secuelas, precuelas o juegos distintos de la misma saga (IsTrueMatch = false)
        new(33, "The Legend of Zelda: Breath of the Wild", "The Legend of Zelda: Tears of the Kingdom", false, "Secuela misma franquicia"),
        new(34, "Final Fantasy VII Remake", "Final Fantasy VII Rebirth", false, "Secuela directa"),
        new(35, "Final Fantasy XVI", "Final Fantasy XV Windows Edition", false, "Número romano cercano"),
        new(36, "Dark Souls II", "Dark Souls III", false, "Secuela número romano"),
        new(37, "Dark Souls: Remastered", "Dark Souls III", false, "Entrega distinta"),
        new(38, "Resident Evil 2", "Resident Evil 3", false, "Secuela numérica"),
        new(39, "Resident Evil 4", "Resident Evil Village", false, "Misma franquicia"),
        new(40, "Persona 5 Royal", "Persona 5 Strikers", false, "Spin-off"),
        new(41, "Persona 5 Royal", "Persona 3 Reload", false, "Entrega distinta"),
        new(42, "Marvel's Spider-Man Remastered", "Marvel's Spider-Man: Miles Morales", false, "Spin-off"),
        new(43, "Horizon Zero Dawn", "Horizon Forbidden West", false, "Secuela"),
        new(44, "The Last of Us Part I", "The Last of Us Part II", false, "Secuela Part I vs II"),
        new(45, "God of War", "God of War Ragnarök", false, "Secuela con subtítulo"),
        new(46, "Monster Hunter World", "Monster Hunter Rise", false, "Entrega distinta"),
        new(47, "NieR:Automata", "NieR Replicant ver.1.22474487139", false, "Misma saga"),
        new(48, "Subnautica", "Subnautica: Below Zero", false, "Expansión independiente"),

        // 49-64: Casos difíciles NEGATIVOS: DLCs sueltos, bandas sonoras, bundles y ediciones incompatibles (IsTrueMatch = false)
        new(49, "Elden Ring", "Elden Ring: Shadow of the Erdtree DLC Upgrade", false, "DLC suelto"),
        new(50, "Cyberpunk 2077", "Cyberpunk 2077: Phantom Liberty Expansion DLC", false, "DLC expansión"),
        new(51, "The Witcher 3: Wild Hunt", "The Witcher 3: Wild Hunt - Blood and Wine DLC", false, "DLC expansión"),
        new(52, "Monster Hunter Rise", "Monster Hunter Rise: Sunbreak DLC", false, "DLC expansión"),
        new(53, "Hollow Knight", "Hollow Knight - Official Soundtrack Artbook", false, "Soundtrack / Artbook"),
        new(54, "Street Fighter 6", "Street Fighter 6 - Year 1 Character Season Pass DLC", false, "Season Pass"),
        new(55, "Diablo IV", "Diablo IV: Vessel of Hatred Expansion Pack", false, "Expansión"),
        new(56, "Kingdom Hearts III", "Kingdom Hearts III Re Mind DLC", false, "DLC"),
        new(57, "Control", "Control Season Pass Expansion Bundle", false, "Season Pass"),
        new(58, "DOOM Eternal", "DOOM Eternal: The Ancient Gods Part One DLC", false, "DLC"),
        new(59, "Red Dead Redemption 2", "Red Dead Online Standalone", false, "Modo online suelto"),
        new(60, "Grand Theft Auto V", "Grand Theft Auto Online: Megalodon Shark Cash Card", false, "Microtransacción"),
        new(61, "Baldur's Gate 3", "Baldur's Gate: Enhanced Edition", false, "Entrega anterior"),
        new(62, "Dragon Quest XI S: Echoes of an Elusive Age", "Dragon Quest III HD-2D Remake", false, "Entrega distinta"),
        new(63, "Ori and the Will of the Wisps", "Ori and the Blind Forest", false, "Precuela"),
        new(64, "Dishonored 2", "Dishonored: Death of the Outsider", false, "Spin-off")
    ];

    public static IReadOnlyList<ThresholdMetrics> EvaluateThresholds(double[]? thresholds = null)
    {
        thresholds ??= [0.75, 0.80, 0.85, 0.88, 0.92, 0.95];
        var pairs = GetManualValidationSet();
        var results = new List<ThresholdMetrics>(thresholds.Length);

        foreach (double th in thresholds)
        {
            int tp = 0, fp = 0, tn = 0, fn = 0;

            foreach (var p in pairs)
            {
                string norm1 = JaroWinklerMatcher.NormalizeTitle(p.CanonicalTitle);
                string norm2 = JaroWinklerMatcher.NormalizeTitle(p.StoreCandidateTitle);
                double score = JaroWinklerMatcher.CalculateSimilarity(norm1, norm2);

                string lowerCand = p.StoreCandidateTitle.ToLowerInvariant();
                bool hasDisqualifyingToken = DisqualifyingTokens.Any(t => lowerCand.Contains(t));

                bool predictedPositive = score >= th && !hasDisqualifyingToken;

                if (predictedPositive && p.IsTrueMatch) tp++;
                else if (predictedPositive && !p.IsTrueMatch) fp++;
                else if (!predictedPositive && !p.IsTrueMatch) tn++;
                else fn++;
            }

            double precision = (tp + fp) > 0 ? (double)tp / (tp + fp) : 0.0;
            double recall = (tp + fn) > 0 ? (double)tp / (tp + fn) : 0.0;
            double f1 = (precision + recall) > 0 ? 2.0 * (precision * recall) / (precision + recall) : 0.0;

            results.Add(new ThresholdMetrics(th, tp, fp, tn, fn, precision, recall, f1));
        }

        return results;
    }

    public static string BuildValidationMarkdownSection()
    {
        var metrics = EvaluateThresholds();
        var sb = new StringBuilder();

        sb.AppendLine("\n## 7. Validación del Umbral de Emparejamiento Jaro–Winkler (64 Pares Manuales — Sección 6)");
        sb.AppendLine("| Umbral | VP (TP) | FP | VN (TN) | FN | Precisión | Exhaustividad (Recall) | F1-Score |");
        sb.AppendLine("| :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: |");

        foreach (var m in metrics)
        {
            string bold = Math.Abs(m.Threshold - 0.88) < 0.001 ? "**" : "";
            sb.AppendLine($"| {bold}{m.Threshold:F2}{bold} | {m.TruePositives} | {m.FalsePositives} | {m.TrueNegatives} | {m.FalseNegatives} | {bold}{m.Precision * 100:F1}%{bold} | {bold}{m.Recall * 100:F1}%{bold} | {bold}{m.F1Score:F3}{bold} |");
        }

        return sb.ToString();
    }
}