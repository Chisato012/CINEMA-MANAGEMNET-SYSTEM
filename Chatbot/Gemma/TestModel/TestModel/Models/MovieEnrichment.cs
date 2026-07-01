using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TestModel.Models
{
    [BsonIgnoreExtraElements]
    public class MovieEnrichment
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("movieId")]
        public int MovieId { get; set; }

        [BsonElement("title")]
        public string Title { get; set; } = "";

        [BsonElement("keywords")]
        public List<string> Keywords { get; set; } = new();

        [BsonElement("moods")]
        public List<string> Moods { get; set; } = new();

        [BsonElement("themes")]
        public List<string> Themes { get; set; } = new();

        [BsonElement("targetAudience")]
        public List<string> TargetAudience { get; set; } = new();

        [BsonElement("countryName")]
        public string? CountryName { get; set; }

        [BsonElement("languageName")]
        public string? LanguageName { get; set; }

        [BsonElement("extraDescription")]
        public string? ExtraDescription { get; set; }

        [BsonElement("cluster")]
        public string? Cluster { get; set; }

        [BsonElement("source")]
        public string? Source { get; set; }
    }
}