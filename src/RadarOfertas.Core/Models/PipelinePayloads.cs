namespace RadarOfertas.Core.Models;

public sealed record GameTarget(
    int GameId,
    string CanonicalTitle,
    decimal MsrpUsd,
    string[] Platforms,
    bool IsHardCase);

public sealed record RawSourcePayload(
    string SourceCode,
    string RawContent,
    bool IsSuccess,
    int RetriesUsed,
    string? ErrorMessage = null);

public sealed record RawGameBundle(
    GameTarget Game,
    IReadOnlyList<RawSourcePayload> Payloads);

public sealed record StoreCandidate(
    string StoreCode,
    string RawTitle,
    decimal CurrentPriceUsd,
    decimal? RegularPriceUsd,
    string Format,
    string ProductUrl,
    string? CoverImageUrl);

public sealed record ParsedGameCandidates(
    GameTarget Game,
    IReadOnlyList<StoreCandidate> StoreCandidates,
    int? MetacriticScore,
    int? OpenCriticScore,
    decimal? HltbMainHours,
    decimal? HltbExtraHours,
    decimal? HltbCompletionistHours);

public sealed record MatchedStoreOffer(
    StoreCandidate Candidate,
    double SimilarityScore,
    bool IsAccepted,
    bool IsDubious,
    bool IsOnSale,
    decimal DiscountPct);

public sealed record EvaluatedGameRecord(
    GameTarget Game,
    IReadOnlyList<MatchedStoreOffer> Offers,
    int? MetacriticScore,
    int? OpenCriticScore,
    decimal? HltbMainHours);

public sealed record FailedRequestItem(
    GameTarget Game,
    string SourceCode,
    int Attempts,
    string LastError,
    DateTimeOffset FailedAt);