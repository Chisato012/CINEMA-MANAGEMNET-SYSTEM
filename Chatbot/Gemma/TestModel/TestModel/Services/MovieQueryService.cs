using TestModel.Models;
using Dapper;
using Microsoft.Data.SqlClient;


namespace TestModel.Services
{
    public class MovieQueryService
    {
        private readonly string _connectionString;

        public MovieQueryService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("SqlServer")
                ?? throw new Exception("Thiếu ConnectionStrings:SqlServer");
        }

        public async Task<List<MovieRecommendationDto>> GetMoviesByIdsAsync(List<int> movieIds)
        {
            if (movieIds.Count == 0)
            {
                return new List<MovieRecommendationDto>();
            }

            const string sql = """
        SELECT TOP 10
            m.MovieID AS MovieId,
            m.Title,
            m.Synopsis,
            m.PosterURL AS PosterUrl,
            m.Trailer,
            m.AgeRating,
            m.Duration,
            STRING_AGG(g.Name, ', ') AS Genres,
            MIN(s.StartTime) AS NextShowtime,
            MIN(s.BasePrice) AS BasePrice
        FROM Movies m
        LEFT JOIN MovieGenres mg ON m.MovieID = mg.MovieID
        LEFT JOIN Genres g ON mg.GenreID = g.GenreID
        LEFT JOIN Showtimes s 
            ON m.MovieID = s.MovieID
            AND s.StartTime >= GETDATE()
        WHERE m.MovieID IN @MovieIds
        GROUP BY 
            m.MovieID,
            m.Title,
            m.Synopsis,
            m.PosterURL,
            m.Trailer,
            m.AgeRating,
            m.Duration
        ORDER BY 
            CASE WHEN MIN(s.StartTime) IS NULL THEN 1 ELSE 0 END,
            MIN(s.StartTime)
        """;

            await using var connection = new SqlConnection(_connectionString);

            var result = await connection.QueryAsync<MovieRecommendationDto>(
                sql,
                new { MovieIds = movieIds }
            );

            return result.ToList();
        }

        public async Task<List<MovieRecommendationDto>> GetFallbackMoviesAsync()
        {
            const string sql = """
        SELECT TOP 10
            m.MovieID AS MovieId,
            m.Title,
            m.Synopsis,
            m.PosterURL AS PosterUrl,
            m.Trailer,
            m.AgeRating,
            m.Duration,
            STRING_AGG(g.Name, ', ') AS Genres,
            MIN(s.StartTime) AS NextShowtime,
            MIN(s.BasePrice) AS BasePrice
        FROM Movies m
        LEFT JOIN MovieGenres mg ON m.MovieID = mg.MovieID
        LEFT JOIN Genres g ON mg.GenreID = g.GenreID
        LEFT JOIN Showtimes s 
            ON m.MovieID = s.MovieID
            AND s.StartTime >= GETDATE()
        GROUP BY 
            m.MovieID,
            m.Title,
            m.Synopsis,
            m.PosterURL,
            m.Trailer,
            m.AgeRating,
            m.Duration
        ORDER BY NEWID()
        """;

            await using var connection = new SqlConnection(_connectionString);

            var result = await connection.QueryAsync<MovieRecommendationDto>(sql);

            return result.ToList();
        }
    }
}
