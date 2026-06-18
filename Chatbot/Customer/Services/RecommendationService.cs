using Customer.Models;

namespace Customer.Services;

public sealed class RecommendationService
{
    private static readonly Dictionary<string, string[]> MoodToGenres = new(StringComparer.OrdinalIgnoreCase)
    {
        ["vui"] = ["Hoạt hình", "Gia đình"],
        ["vui vẻ"] = ["Hoạt hình", "Gia đình"],
        ["thư giãn"] = ["Hoạt hình", "Gia đình", "Tình cảm"],
        ["gia đình"] = ["Gia đình", "Hoạt hình"],
        ["hành động"] = ["Hành động", "Khoa học viễn tưởng"],
        ["kịch tính"] = ["Hành động", "Trinh thám", "Khoa học viễn tưởng"],
        ["sợ"] = ["Kinh dị", "Trinh thám"],
        ["kinh dị"] = ["Kinh dị"],
        ["lãng mạn"] = ["Tình cảm", "Cảm động"],
        ["buồn"] = ["Cảm động", "Tình cảm"],
        ["cảm động"] = ["Cảm động", "Tình cảm"]
    };

    public IReadOnlyList<MovieRecommendation> Rank(
        IReadOnlyList<MovieRecommendation> upcoming,
        UserPreferenceProfile? profile,
        ChatRequest request)
    {
        var requestedGenres = ExtractMoodGenres(request);
        var now = DateOnly.FromDateTime(DateTime.Now);

        return upcoming
            .Select(item =>
            {
                var score = 0.0;
                var reasons = new List<string>();

                foreach (var genre in SplitGenres(item.Genres))
                {
                    if (requestedGenres.Contains(genre))
                    {
                        score += 4;
                        reasons.Add($"hợp tâm trạng/sở thích {genre}");
                    }

                    if (profile?.GenreCounts.TryGetValue(genre, out var count) == true)
                    {
                        score += Math.Min(5, count * 1.5);
                        reasons.Add($"bạn từng xem nhiều phim {genre}");
                    }
                }

                if (profile?.PreferredHour is int hour)
                {
                    var distance = Math.Abs(item.StartTime.Hour - hour);
                    if (distance <= 2)
                    {
                        score += 2;
                        reasons.Add($"gần khung giờ bạn hay xem ({hour}:00)");
                    }
                }

                var daysAway = Math.Max(0, item.Date.DayNumber - now.DayNumber);
                score += Math.Max(0, 3 - daysAway * 0.4);

                if (request.Message.Contains("gia đình", StringComparison.OrdinalIgnoreCase)
                    && item.Genres.Contains("Gia đình", StringComparison.OrdinalIgnoreCase))
                {
                    score += 3;
                    reasons.Add("phù hợp đi cùng gia đình");
                }

                if (request.Message.Contains("cuối tuần", StringComparison.OrdinalIgnoreCase)
                    && IsWeekend(item.Date))
                {
                    score += 2;
                    reasons.Add("có suất cuối tuần");
                }

                if (reasons.Count == 0)
                {
                    reasons.Add("có lịch chiếu sắp tới");
                }

                return item with
                {
                    Score = Math.Round(score, 2),
                    Reason = string.Join("; ", reasons.Distinct(StringComparer.OrdinalIgnoreCase))
                };
            })
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Date)
            .ThenBy(item => item.StartTime)
            .Take(5)
            .ToList();
    }

    public bool NeedsMood(ChatRequest request, UserPreferenceProfile? profile)
    {
        if (profile?.HasHistory == true)
        {
            return false;
        }

        return ExtractMoodGenres(request).Count == 0
            && !request.Message.Contains("gợi ý", StringComparison.OrdinalIgnoreCase)
            && !request.Message.Contains("phim", StringComparison.OrdinalIgnoreCase);
    }

    private static HashSet<string> ExtractMoodGenres(ChatRequest request)
    {
        var text = $"{request.Message} {request.Mood} {request.Companion}".ToLowerInvariant();
        var genres = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (keyword, mappedGenres) in MoodToGenres)
        {
            if (!text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var genre in mappedGenres)
            {
                genres.Add(genre);
            }
        }

        return genres;
    }

    private static IEnumerable<string> SplitGenres(string genres)
    {
        return genres.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static bool IsWeekend(DateOnly date)
    {
        return date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
    }
}
