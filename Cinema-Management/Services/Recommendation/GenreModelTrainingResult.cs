namespace Cinema_Management.Services.Recommendation;

public class GenreModelTrainingResult
{
    public string DataPath { get; set; } = string.Empty;
    public string ModelPath { get; set; } = string.Empty;
    public int RowCount { get; set; }
    public int LabelCount { get; set; }
    public double MicroAccuracy { get; set; }
    public double MacroAccuracy { get; set; }
    public double LogLoss { get; set; }
}
