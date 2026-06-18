namespace Customer.Models;

public sealed record ChatRequest(
    string Message,
    int? UserId = null,
    string? Mood = null,
    string? Companion = null,
    string? DatePreference = null);

public sealed record ChatResponse(
    string Answer,
    IReadOnlyList<MovieRecommendation> Recommendations,
    IReadOnlyList<string> Sources,
    bool NeedsMood,
    bool UsedLmStudio);

public sealed record MovieRecommendation(
    int MovieId,
    string Title,
    string Genres,
    string AgeRating,
    short Duration,
    string Synopsis,
    string RoomName,
    DateOnly Date,
    TimeOnly StartTime,
    decimal BasePrice,
    double Score,
    string Reason);

public sealed record UserPreferenceProfile(
    bool HasHistory,
    IReadOnlyDictionary<string, int> GenreCounts,
    int? PreferredHour);

