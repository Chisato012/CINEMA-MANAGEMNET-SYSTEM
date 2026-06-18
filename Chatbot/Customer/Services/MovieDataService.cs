using Customer.Models;
using Microsoft.Data.SqlClient;

namespace Customer.Services;

public sealed class MovieDataService(IConfiguration configuration)
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection.");

    public async Task<IReadOnlyList<MovieRecommendation>> GetUpcomingShowtimesAsync(
        int daysAhead,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP (80)
                m.MovieID,
                m.Title,
                m.AgeRating,
                m.Duration,
                m.Synopsis,
                r.RoomName,
                s.[Date],
                s.StartTime,
                s.BasePrice,
                STRING_AGG(g.Name, N', ') AS Genres
            FROM Showtimes s
            JOIN Movies m ON m.MovieID = s.MovieID
            JOIN Rooms r ON r.RoomID = s.RoomID
            LEFT JOIN MovieGenres mg ON mg.MovieID = m.MovieID
            LEFT JOIN Genres g ON g.GenreID = mg.GenreID
            WHERE s.[Date] >= CAST(GETDATE() AS date)
              AND s.[Date] < DATEADD(day, @DaysAhead, CAST(GETDATE() AS date))
            GROUP BY
                m.MovieID, m.Title, m.AgeRating, m.Duration, m.Synopsis,
                r.RoomName, s.[Date], s.StartTime, s.BasePrice
            ORDER BY s.[Date], s.StartTime;
            """;

        var items = new List<MovieRecommendation>();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@DaysAhead", daysAhead);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var date = DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("Date")));
            var startDateTime = reader.GetDateTime(reader.GetOrdinal("StartTime"));

            items.Add(new MovieRecommendation(
                reader.GetInt32(reader.GetOrdinal("MovieID")),
                reader.GetString(reader.GetOrdinal("Title")),
                ReadNullableString(reader, "Genres", "Chưa phân loại"),
                ReadNullableString(reader, "AgeRating", "--"),
                reader.GetInt16(reader.GetOrdinal("Duration")),
                ReadNullableString(reader, "Synopsis", "Chưa có tóm tắt"),
                reader.GetString(reader.GetOrdinal("RoomName")),
                date,
                TimeOnly.FromDateTime(startDateTime),
                reader.GetDecimal(reader.GetOrdinal("BasePrice")),
                0,
                string.Empty));
        }

        return items;
    }

    public async Task<UserPreferenceProfile> GetUserPreferenceProfileAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        const string genresSql = """
            SELECT g.Name, COUNT(*) AS WatchCount
            FROM Bookings b
            JOIN Tickets t ON t.BookingID = b.BookingID
            JOIN Showtimes s ON s.ShowtimeID = t.ShowtimeID
            JOIN MovieGenres mg ON mg.MovieID = s.MovieID
            JOIN Genres g ON g.GenreID = mg.GenreID
            WHERE b.UserID = @UserId
              AND b.Status <> N'Cancelled'
            GROUP BY g.Name
            ORDER BY WatchCount DESC;
            """;

        const string hourSql = """
            SELECT TOP (1) DATEPART(hour, s.StartTime) AS PreferredHour, COUNT(*) AS WatchCount
            FROM Bookings b
            JOIN Tickets t ON t.BookingID = b.BookingID
            JOIN Showtimes s ON s.ShowtimeID = t.ShowtimeID
            WHERE b.UserID = @UserId
              AND b.Status <> N'Cancelled'
            GROUP BY DATEPART(hour, s.StartTime)
            ORDER BY WatchCount DESC;
            """;

        var genreCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        int? preferredHour = null;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using (var command = new SqlCommand(genresSql, connection))
        {
            command.Parameters.AddWithValue("@UserId", userId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                genreCounts[reader.GetString(0)] = reader.GetInt32(1);
            }
        }

        await using (var command = new SqlCommand(hourSql, connection))
        {
            command.Parameters.AddWithValue("@UserId", userId);
            var value = await command.ExecuteScalarAsync(cancellationToken);
            if (value is int hour)
            {
                preferredHour = hour;
            }
        }

        return new UserPreferenceProfile(genreCounts.Count > 0, genreCounts, preferredHour);
    }

    private static string ReadNullableString(SqlDataReader reader, string column, string fallback)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? fallback : reader.GetString(ordinal);
    }
}
