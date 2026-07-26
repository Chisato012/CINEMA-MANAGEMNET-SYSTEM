using Microsoft.ML.Data;

namespace Cinema_Management.Models.Recommendation;

public class MovieGenrePrediction
{
    [ColumnName("PredictedLabel")]
    public string PreferredGenreCode { get; set; } = string.Empty;

    public float[] Score { get; set; } = [];
}
