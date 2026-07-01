using TestModel.Models;
using MongoDB.Driver;

namespace TestModel.Services
{
    public class MongoRecommendationService
    {
        private readonly IMongoCollection<MovieEnrichment> _collection;

        public MongoRecommendationService(IConfiguration configuration)
        {
            var mongoUri = configuration.GetConnectionString("MongoDb");
            var databaseName = configuration["Mongo:Database"];

            var client = new MongoClient(mongoUri);
            var database = client.GetDatabase(databaseName);

            _collection = database.GetCollection<MovieEnrichment>("movie_enrichments");
        }

        public async Task<List<int>> FindMovieIdsAsync(string? mood, string? keywords)
        {
            var terms = new List<string>();

            terms.AddRange(SplitTerms(mood));
            terms.AddRange(SplitTerms(keywords));

            if (terms.Count == 0)
            {
                return new List<int>();
            }

            var filters = new List<FilterDefinition<MovieEnrichment>>();

            foreach (var term in terms)
            {
                var regex = new MongoDB.Bson.BsonRegularExpression(term, "i");

                filters.Add(Builders<MovieEnrichment>.Filter.AnyEq(x => x.Moods, term));
                filters.Add(Builders<MovieEnrichment>.Filter.AnyEq(x => x.Keywords, term));
                filters.Add(Builders<MovieEnrichment>.Filter.AnyEq(x => x.Themes, term));

                // Fallback regex cho trường hợp keyword không khớp y nguyên
                filters.Add(Builders<MovieEnrichment>.Filter.Regex("moods", regex));
                filters.Add(Builders<MovieEnrichment>.Filter.Regex("keywords", regex));
                filters.Add(Builders<MovieEnrichment>.Filter.Regex("themes", regex));
            }

            var filter = Builders<MovieEnrichment>.Filter.Or(filters);

            var docs = await _collection
                .Find(filter)
                .Limit(20)
                .ToListAsync();

            return docs
                .Select(x => x.MovieId)
                .Distinct()
                .ToList();
        }

        private static List<string> SplitTerms(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new List<string>();
            }

            return text
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(x => x.Length >= 2)
                .Distinct()
                .ToList();
        }
    }
}
