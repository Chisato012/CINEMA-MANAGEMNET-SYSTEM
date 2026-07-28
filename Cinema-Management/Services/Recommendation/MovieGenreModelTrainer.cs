using Cinema_Management.Models.Recommendation;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace Cinema_Management.Services.Recommendation;

// Train ML.NET model từ CSV và lưu ra file zip để web app load khi gợi ý phim.
public static class MovieGenreModelTrainer
{
    private static readonly string[] RequiredHeaders =
    [
        "Mood",
        "Companion",
        "Intensity",
        "AgeRating",
        "PreferredGenreCode",
        "PreferredGenreName"
    ];

    public static GenreModelTrainingResult Train(string dataPath, string modelPath)
    {
        // Fail sớm nếu CSV sai format; tránh train ra model zip lỗi mà runtime khó debug.
        ValidateDataset(dataPath);

        var mlContext = new MLContext(seed: 1);
        var data = mlContext.Data.LoadFromTextFile<MovieGenreTrainingData>(
            dataPath,
            hasHeader: true,
            separatorChar: ',');

        var rows = mlContext.Data.CreateEnumerable<MovieGenreTrainingData>(
                data,
                reuseRowObject: false)
            .ToList();

        // Train/test split chỉ dùng để đánh giá nhanh chất lượng model.
        var split = mlContext.Data.TrainTestSplit(data, testFraction: 0.2, seed: 1);
        var pipeline = BuildPipeline(mlContext);

        var evaluationModel = pipeline.Fit(split.TrainSet);
        var predictions = evaluationModel.Transform(split.TestSet);
        var metrics = mlContext.MulticlassClassification.Evaluate(
            predictions,
            labelColumnName: "Label",
            predictedLabelColumnName: "PredictedLabel");

        // Model cuối cùng train lại trên toàn bộ CSV để tận dụng hết dữ liệu hiện có.
        var finalModel = pipeline.Fit(data);
        Directory.CreateDirectory(Path.GetDirectoryName(modelPath)!);
        mlContext.Model.Save(finalModel, data.Schema, modelPath);

        return new GenreModelTrainingResult
        {
            DataPath = dataPath,
            ModelPath = modelPath,
            RowCount = rows.Count,
            LabelCount = rows.Select(row => row.PreferredGenreCode).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            MicroAccuracy = metrics.MicroAccuracy,
            MacroAccuracy = metrics.MacroAccuracy,
            LogLoss = metrics.LogLoss
        };
    }

    public static string GetDefaultDataPath(string contentRootPath)
    {
        return Path.GetFullPath(Path.Combine(contentRootPath, "..", "ML", "ml_recommendation_train.csv"));
    }

    private static IEstimator<ITransformer> BuildPipeline(MLContext mlContext)
    {
        // Pipeline:
        // 1. Đổi label genre code sang key nội bộ của ML.NET.
        // 2. One-hot encode các lựa chọn dạng text.
        // 3. Ghép các cột encoded thành cột Features.
        // 4. Train classifier đa lớp.
        // 5. Đổi predicted key về lại genre code ban đầu.
        return mlContext.Transforms.Conversion.MapValueToKey(
                outputColumnName: "Label",
                inputColumnName: nameof(MovieGenreTrainingData.PreferredGenreCode)) // tạo label cho model học
            .Append(mlContext.Transforms.Categorical.OneHotEncoding(
            [ // key
                new InputOutputColumnPair("MoodEncoded", nameof(MovieGenreTrainingData.Mood)), // 0
                new InputOutputColumnPair("CompanionEncoded", nameof(MovieGenreTrainingData.Companion)), // 1
                new InputOutputColumnPair("IntensityEncoded", nameof(MovieGenreTrainingData.Intensity)), // 2
                new InputOutputColumnPair("AgeRatingEncoded", nameof(MovieGenreTrainingData.AgeRating)) // 3
            ]))
            // ví dụ encoding: Mood
            // vui-> [1, 0, 0]
            .Append(mlContext.Transforms.Concatenate( // ghép tất cả input đã encode thành một cột Feature
                // MoodEncoded + CompanionEncoded + IntensityEncoded + AgeRatingEncoded -> Features = [0,1,0, 0,1,0, 1,0, 1,0,0]
                "Features",
                "MoodEncoded",
                "CompanionEncoded",
                "IntensityEncoded",
                "AgeRatingEncoded"))
            .Append(mlContext.MulticlassClassification.Trainers.LbfgsMaximumEntropy( // train model phân loại nhiều lớp (Multiclass Classification)
                labelColumnName: "Label",
                featureColumnName: "Features"))
            .Append(mlContext.Transforms.Conversion.MapKeyToValue( // đổi kết quả từ key về string genre code
                outputColumnName: "PredictedLabel",
                inputColumnName: "PredictedLabel"));
    }

    private static void ValidateDataset(string dataPath)
    {
        if (!File.Exists(dataPath))
        {
            throw new FileNotFoundException("Recommendation training CSV was not found.", dataPath);
        }

        var lines = File.ReadAllLines(dataPath);
        if (lines.Length <= 1)
        {
            throw new InvalidOperationException("Recommendation training CSV does not contain training rows.");
        }

        var headers = lines[0].TrimStart('\uFEFF').Split(',');
        if (!headers.SequenceEqual(RequiredHeaders, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                $"Recommendation training CSV headers must be: {string.Join(", ", RequiredHeaders)}");
        }

        var seenRows = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var lineIndex = 1; lineIndex < lines.Length; lineIndex++)
        {
            var line = lines[lineIndex];
            var columns = line.Split(',');
            if (columns.Length != RequiredHeaders.Length)
            {
                throw new InvalidOperationException($"Invalid column count at CSV line {lineIndex + 1}.");
            }

            if (columns.Any(string.IsNullOrWhiteSpace))
            {
                throw new InvalidOperationException($"Blank value found at CSV line {lineIndex + 1}.");
            }

            if (!seenRows.Add(line))
            {
                throw new InvalidOperationException($"Duplicate training row found at CSV line {lineIndex + 1}.");
            }
        }
    }
}
