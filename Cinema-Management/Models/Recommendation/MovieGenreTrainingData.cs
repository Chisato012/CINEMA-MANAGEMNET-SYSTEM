using Microsoft.ML.Data;

namespace Cinema_Management.Models.Recommendation;

public class MovieGenreTrainingData
{
    // Thứ tự LoadColumn phải khớp chính xác với header trong ML/ml_recommendation_train.csv.
    // 4 cột đầu là feature đầu vào, PreferredGenreCode là label để model học.
    [LoadColumn(0)]
    public string Mood { get; set; } = string.Empty;

    [LoadColumn(1)]
    public string Companion { get; set; } = string.Empty;

    [LoadColumn(2)]
    public string Intensity { get; set; } = string.Empty;

    [LoadColumn(3)]
    public string AgeRating { get; set; } = string.Empty;

    [LoadColumn(4)]
    public string PreferredGenreCode { get; set; } = string.Empty;

    [LoadColumn(5)]
    public string PreferredGenreName { get; set; } = string.Empty;
}
