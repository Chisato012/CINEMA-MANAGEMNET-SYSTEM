using System.Globalization;
using System.Text;
using Customer.Models;

namespace Customer.Services;

public sealed class ChatOrchestrator(
    MovieDataService movieDataService,
    RecommendationService recommendationService,
    LmStudioClient lmStudioClient)
{
    public async Task<ChatResponse> AnswerAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        UserPreferenceProfile? profile = null;
        if (request.UserId is int userId)
        {
            profile = await movieDataService.GetUserPreferenceProfileAsync(userId, cancellationToken);
        }

        if (recommendationService.NeedsMood(request, profile))
        {
            return new ChatResponse(
                "Bạn muốn xem phim theo tâm trạng nào hôm nay: vui vẻ, thư giãn, lãng mạn, hành động, kịch tính hay cảm động? Bạn cũng có thể nói thêm là đi một mình, đi cùng bạn bè, gia đình hoặc người yêu.",
                [],
                ["Conversation"],
                NeedsMood: true,
                UsedLmStudio: false);
        }

        var upcoming = await movieDataService.GetUpcomingShowtimesAsync(14, cancellationToken);
        if (upcoming.Count == 0)
        {
            return new ChatResponse(
                "Hiện tại mình chưa thấy suất chiếu nào trong 14 ngày tới trong rạp. Hãy kiểm tra lại dữ liệu bảng Showtimes nhé.",
                [],
                ["Showtimes"],
                NeedsMood: false,
                UsedLmStudio: false);
        }

        var recommendations = recommendationService.Rank(upcoming, profile, request);
        var prompt = BuildPrompt(request, profile, recommendations);
        var (success, lmAnswer) = await lmStudioClient.TryCompleteAsync(prompt, cancellationToken);
        var answer = success && lmAnswer is not null
            ? lmAnswer
            : BuildFallbackAnswer(profile, recommendations);

        return new ChatResponse(
            answer,
            recommendations,
            ["Movies", "MovieGenres", "Showtimes", "Rooms", "Bookings", "Tickets"],
            NeedsMood: false,
            UsedLmStudio: success);
    }

    private static string BuildPrompt(
        ChatRequest request,
        UserPreferenceProfile? profile,
        IReadOnlyList<MovieRecommendation> recommendations)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Nhiệm vụ: tư vấn phim cá nhân hóa bằng tiếng Việt, ngắn gọn, thân thiện.");
        builder.AppendLine("Quy tắc: chỉ đề xuất phim trong danh sách bên dưới; không bịa thêm suất chiếu.");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Câu hỏi khách hàng: {request.Message}");
        builder.AppendLine(profile?.HasHistory == true
            ? "Khách đã có lịch sử đặt vé. Hãy nhắc nhẹ rằng gợi ý dựa trên thể loại/khung giờ từng xem."
            : "Khách mới hoặc chưa có lịch sử. Hãy gợi ý dựa trên tâm trạng/sở thích trong hội thoại.");

        if (profile?.GenreCounts.Count > 0)
        {
            builder.AppendLine("Thể loại khách hay xem:");
            foreach (var (genre, count) in profile.GenreCounts.Take(5))
            {
                builder.AppendLine(CultureInfo.InvariantCulture, $"- {genre}: {count} vé");
            }
        }

        builder.AppendLine("Danh sách phim/suất chiếu được backend chấm điểm:");
        foreach (var item in recommendations)
        {
            builder.AppendLine(CultureInfo.InvariantCulture,
                $"- {item.Title} | {item.Genres} | {item.AgeRating} | {item.Duration} phút | {item.Date:dd/MM/yyyy} {item.StartTime:HH:mm} | {item.RoomName} | {item.BasePrice:N0}đ | Lý do: {item.Reason}");
        }

        builder.AppendLine("Hãy chọn 2-3 phim nổi bật nhất, giải thích lý do, kèm ngày giờ chiếu và giá cơ bản.");
        return builder.ToString();
    }

    private static string BuildFallbackAnswer(
        UserPreferenceProfile? profile,
        IReadOnlyList<MovieRecommendation> recommendations)
    {
        var intro = profile?.HasHistory == true
            ? "Dựa trên lịch sử đặt vé của bạn, mình gợi ý vài suất chiếu sắp tới:"
            : "Dựa trên tâm trạng/sở thích bạn vừa nói, mình gợi ý vài suất chiếu sắp tới:";

        var builder = new StringBuilder(intro);
        foreach (var item in recommendations.Take(3))
        {
            builder.AppendLine();
            builder.Append(CultureInfo.InvariantCulture,
                $"- {item.Title}: {item.Date:dd/MM/yyyy} lúc {item.StartTime:HH:mm}, {item.RoomName}, giá cơ bản {item.BasePrice:N0}đ. Lý do: {item.Reason}.");
        }

        builder.AppendLine();
        builder.Append("Bạn muốn mình lọc tiếp theo hôm nay, cuối tuần, hoặc theo thể loại cụ thể không?");
        return builder.ToString();
    }
}
