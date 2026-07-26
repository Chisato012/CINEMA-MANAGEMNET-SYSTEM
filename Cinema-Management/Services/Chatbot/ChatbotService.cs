using System.Globalization;
using System.Text;
using Cinema_Management.Data;
using Cinema_Management.Models.Chatbot;
using Cinema_Management.Models.Recommendation;
using Cinema_Management.Services.Recommendation;
using Microsoft.EntityFrameworkCore;

namespace Cinema_Management.Services.Chatbot;

// Service này là lớp nối giữa UI gợi ý phim, ML.NET và database
// UI chỉ gửi 4 lựa chọn cố định mà user chọn: Mood, Companion, Intensity, AgeRating
public class ChatbotService : IChatbotService
{
    private const int RecommendationLimit = 5;

    private readonly ApplicationDbContext _context;
    private readonly IGenreRecommendationService _genreRecommendationService;

    public ChatbotService(
        ApplicationDbContext context,
        IGenreRecommendationService genreRecommendationService)
    {
        _context = context;
        _genreRecommendationService = genreRecommendationService;
    }

    public async Task<ChatbotResponse> RecommendAsync(
        GenreRecommendationInput input,
        CancellationToken cancellationToken)
    {
        // Luồng chính:
        // 1. Load phim, thể loại và lịch chiếu từ SQL Server.
        // 2. Gửi 4 lựa chọn của user sang service ML.NET để dự đoán thể loại.
        // 3. Chỉ giữ các phim có đúng thể loại được dự đoán.
        // 4. Chấm điểm để sắp xếp phim phù hợp hơn lên trước.
        var movies = await LoadMovieProfilesAsync(cancellationToken);
        var prediction = _genreRecommendationService.Predict(input);
        var genreName = prediction?.GenreName;
        var selectedMovies = string.IsNullOrWhiteSpace(genreName)
            ? []
            : RecommendMovies(movies, input, genreName);

        return new ChatbotResponse
        {
            Intent = "recommend_by_mood",
            Mood = input.Mood,
            RecommendedGenreCode = prediction?.GenreCode,
            RecommendedGenreName = genreName,
            Reply = ComposeLocalReply(genreName, selectedMovies),
            Movies = selectedMovies
        };
    }

    private async Task<List<MovieProfile>> LoadMovieProfilesAsync(CancellationToken cancellationToken)
    {
        // AsNoTracking giúp truy vấn chỉ đọc nhẹ hơn vì ta không sửa Movie trong luồng gợi ý.
        var movies = await _context.Movies
            .AsNoTracking()
            .Include(movie => movie.MovieGenres)
                .ThenInclude(movieGenre => movieGenre.Genre)
            .Include(movie => movie.Showtimes)
            .ToListAsync(cancellationToken);

        return movies
            .Select(movie => new MovieProfile
            {
                MovieId = movie.MovieId,
                Title = movie.Title,
                Duration = movie.Duration,
                AgeRating = movie.AgeRating,
                Synopsis = movie.Synopsis,
                Genres = movie.MovieGenres?
                    .Select(movieGenre => movieGenre.Genre.Name)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct()
                    .ToList() ?? [],
                Showtimes = movie.Showtimes?
                    .Select(showtime => showtime.StartTime)
                    .OrderBy(startTime => startTime)
                    .ToList() ?? []
            })
            .ToList();
    }

    private static IReadOnlyList<ChatbotMovieResult> RecommendMovies(
        IReadOnlyList<MovieProfile> movies,
        GenreRecommendationInput input,
        string genreName)
    {
        var normalizedGenreName = NormalizeText(genreName);

        return movies
            // Đây là bước lọc bắt buộc để tránh lỗi phim hoạt hình lọt vào kết quả kinh dị.
            .Where(movie => movie.Genres.Any(genre => NormalizeText(genre) == normalizedGenreName))
            .Select(movie => new { Movie = movie, Score = ScoreMovie(movie, input, normalizedGenreName) })
            .Where(item => item.Score > 0)
            .OrderByDescending(item => item.Score)
            .ThenBy(item => GetNearestShowtime(item.Movie.Showtimes))
            .ThenBy(item => item.Movie.Title)
            .Take(RecommendationLimit)
            .Select(item => ToResult(item.Movie, item.Score))
            .ToList();
    }

    private static double ScoreMovie(
        MovieProfile movie,
        GenreRecommendationInput input,
        string normalizedGenreName)
    {
        // Phim đã đúng genre rồi; score chỉ dùng để xếp phim nào nên hiện trước.
        // Các tiêu chí phụ gồm age rating, synopsis, và việc phim có lịch chiếu.
        var score = 5.0;
        var normalizedSynopsis = NormalizeText(movie.Synopsis);

        if (movie.AgeRating.Equals(input.AgeRating, StringComparison.OrdinalIgnoreCase))
        {
            score += 3;
        }
        else if (input.AgeRating == "P" && movie.AgeRating is "T16" or "T18")
        {
            score -= 5;
        }

        if (input.Companion is "tre_em" or "gia_dinh")
        {
            score += movie.AgeRating.Equals("P", StringComparison.OrdinalIgnoreCase) ? 3 : 0;
            score -= movie.AgeRating.Equals("T18", StringComparison.OrdinalIgnoreCase) ? 4 : 0;
        }

        if (input.Intensity == "gay_can" &&
            ContainsAny(normalizedSynopsis, "chien dau", "sinh ton", "rung ron", "bi an", "vu tru"))
        {
            score += 2;
        }

        if (input.Intensity == "nhe_nhang" &&
            ContainsAny(normalizedSynopsis, "tinh ban", "dang yeu", "gia dinh", "cau chuyen"))
        {
            score += 2;
        }

        if (normalizedGenreName == "khoa hoc vien tuong" &&
            ContainsAny(normalizedSynopsis, "tuong lai", "vu tru", "sieu anh hung", "chien binh"))
        {
            score += 3;
        }

        if (normalizedGenreName == "hanh dong" &&
            ContainsAny(normalizedSynopsis, "chien dau", "tran chien", "bao ve", "cong ly", "sinh ton"))
        {
            score += 3;
        }

        if (normalizedGenreName == "hoat hinh" &&
            ContainsAny(normalizedSynopsis, "doraemon", "do choi", "dang yeu", "tinh ban", "nobita"))
        {
            score += 3;
        }

        if (normalizedGenreName == "tinh cam" &&
            ContainsAny(normalizedSynopsis, "cau chuyen", "hoan doi", "co gai", "chang trai"))
        {
            score += 3;
        }

        if (normalizedGenreName == "cam dong" &&
            ContainsAny(normalizedSynopsis, "tinh ban", "dang yeu", "cau chuyen", "gia dinh"))
        {
            score += 3;
        }

        if (movie.Showtimes.Count > 0)
        {
            score += 1;
        }

        return score;
    }

    private static ChatbotMovieResult ToResult(MovieProfile movie, double score)
    {
        return new ChatbotMovieResult
        {
            MovieId = movie.MovieId,
            Title = movie.Title,
            Genres = string.Join(", ", movie.Genres),
            AgeRating = movie.AgeRating,
            Duration = movie.Duration,
            Synopsis = movie.Synopsis,
            NextShowtime = FormatNearestShowtime(movie.Showtimes),
            ShowtimeCount = movie.Showtimes.Count,
            Score = Math.Round(score, 2)
        };
    }

    private static string ComposeLocalReply(string? genreName, IReadOnlyList<ChatbotMovieResult> movies)
    {
        if (string.IsNullOrWhiteSpace(genreName))
        {
            return "Mình chưa dự đoán được thể loại phù hợp. Bạn thử chọn lại các tiêu chí nhé.";
        }

        if (movies.Count == 0)
        {
            return $"ML.NET dự đoán thể loại {genreName}, nhưng hiện chưa có phim thuộc thể loại này trong dữ liệu lịch chiếu.";
        }

        return $"Thể loại phù hợp: {genreName}. Danh sách phim phù hợp nằm bên dưới.";
    }

    private static DateTime GetNearestShowtime(IEnumerable<DateTime> showtimes)
    {
        var orderedShowtimes = showtimes.OrderBy(showtime => showtime).ToList();
        return orderedShowtimes.FirstOrDefault(showtime => showtime >= DateTime.Now) == default
            ? orderedShowtimes.FirstOrDefault()
            : orderedShowtimes.First(showtime => showtime >= DateTime.Now);
    }

    private static string? FormatNearestShowtime(IEnumerable<DateTime> showtimes)
    {
        var nearest = GetNearestShowtime(showtimes);
        return nearest == default ? null : nearest.ToString("dd/MM/yyyy HH:mm");
    }

    private static bool ContainsAny(string source, params string[] keywords)
    {
        return keywords.Any(source.Contains);
    }

    private static string NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString()
            .Replace('đ', 'd')
            .Replace('Đ', 'd')
            .Normalize(NormalizationForm.FormC);
    }
}
