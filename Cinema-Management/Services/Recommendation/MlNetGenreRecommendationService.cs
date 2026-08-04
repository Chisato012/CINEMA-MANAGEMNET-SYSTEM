using Cinema_Management.Models.Recommendation;
using Microsoft.ML;

namespace Cinema_Management.Services.Recommendation;

// Service dự đoán thể loại phim cho chatbot.
// Service này được đăng ký singleton nên CSV/model chỉ load một lần trong vòng đời
public class MlNetGenreRecommendationService : IGenreRecommendationService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<MlNetGenreRecommendationService> _logger;
    private readonly MLContext _mlContext = new(seed: 1);
    private readonly Lazy<ITransformer?> _model;
    private readonly Lazy<IReadOnlyDictionary<string, GenreRecommendationResult>> _trainingLookup;
    private readonly Lazy<IReadOnlyList<TrainingExample>> _trainingExamples;

    public MlNetGenreRecommendationService(
        IWebHostEnvironment environment,
        ILogger<MlNetGenreRecommendationService> logger)
    {
        _environment = environment;
        _logger = logger;
        _model = new Lazy<ITransformer?>(LoadModel);
        _trainingLookup = new Lazy<IReadOnlyDictionary<string, GenreRecommendationResult>>(LoadTrainingLookup);
        _trainingExamples = new Lazy<IReadOnlyList<TrainingExample>>(LoadTrainingExamples);
    }

    public GenreRecommendationResult? Predict(GenreRecommendationInput input)
    {
        // Dataset hiện còn nhỏ và các input là select cố định
        // Nếu tổ hợp lựa chọn trùng với dòng đã có trong CSV, lấy luôn label CSV để trả kết quả
        if (_trainingLookup.Value.TryGetValue(BuildLookupKey(input), out var exactResult))
        {
            return exactResult;
        }

        // Nếu tổ hợp chưa có trong CSV, lấy dòng training gần nhất
        // tránh việc model dự đoán lặp lại cùng một genre quá nhiều
        var nearestResult = PredictFromNearestTrainingExample(input);
        if (nearestResult is not null)
        {
            return nearestResult;
        }

        // dùng model zip ML.NET nếu CSV không có dữ liệu lookup.
        var model = _model.Value;
        if (model is null)
        {
            return null;
        }

        try
        {
            var engine = _mlContext.Model.CreatePredictionEngine<MovieGenreTrainingData, MovieGenrePrediction>(model);
            var prediction = engine.Predict(new MovieGenreTrainingData
            {
                Mood = input.Mood,
                Companion = input.Companion,
                Intensity = input.Intensity,
                AgeRating = input.AgeRating
            });

            if (string.IsNullOrWhiteSpace(prediction.PreferredGenreCode))
            {
                return null;
            }

            return new GenreRecommendationResult
            {
                GenreCode = prediction.PreferredGenreCode,
                GenreName = GenreRecommendationMappings.GetGenreName(prediction.PreferredGenreCode),
                Confidence = prediction.Score.Length == 0 ? 0 : prediction.Score.Max()
            };
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Could not predict movie genre with ML.NET model.");
            return null;
        }
    }

    public static string GetDefaultModelPath(string contentRootPath)
    {
        return Path.Combine(
            MovieGenreModelTrainer.GetMlRootPath(contentRootPath),
            "artifacts",
            "movie_genre_model.zip");
    }

    private ITransformer? LoadModel()
    {
        var modelPath = GetDefaultModelPath(_environment.ContentRootPath);
        if (!File.Exists(modelPath))
        {
            _logger.LogInformation("ML.NET recommendation model not found at {ModelPath}.", modelPath);
            return null;
        }

        try
        {
            return _mlContext.Model.Load(modelPath, out _);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Could not load ML.NET recommendation model from {ModelPath}.", modelPath);
            return null;
        }
    }

    private IReadOnlyDictionary<string, GenreRecommendationResult> LoadTrainingLookup()
    {
        return _trainingExamples.Value
            .GroupBy(example => BuildLookupKey(example.Input), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.First().Result,
                StringComparer.OrdinalIgnoreCase);
    }

    private IReadOnlyList<TrainingExample> LoadTrainingExamples()
    {
        var dataPath = MovieGenreModelTrainer.GetDefaultDataPath(_environment.ContentRootPath);
        var results = new List<TrainingExample>();

        if (!File.Exists(dataPath))
        {
            _logger.LogInformation("Recommendation training CSV not found at {DataPath}.", dataPath);
            return results;
        }

        foreach (var line in File.ReadLines(dataPath).Skip(1))
        {
            var columns = line.Split(',');
            if (columns.Length < 6)
            {
                continue;
            }

            results.Add(new TrainingExample(
                new GenreRecommendationInput
                {
                    Mood = columns[0],
                    Companion = columns[1],
                    Intensity = columns[2],
                    AgeRating = columns[3]
                },
                new GenreRecommendationResult
                {
                    GenreCode = columns[4],
                    GenreName = columns[5],
                    Confidence = 1
                }));
        }

        return results;
    }

    private GenreRecommendationResult? PredictFromNearestTrainingExample(GenreRecommendationInput input)
    {
        var examples = _trainingExamples.Value;
        if (examples.Count == 0)
        {
            return null;
        }

        var nearest = examples
            .Select(example => new
            {
                example.Result,
                Score = ScoreSimilarity(input, example.Input)
            })
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Result.GenreCode)
            .First();

        return new GenreRecommendationResult
        {
            GenreCode = nearest.Result.GenreCode,
            GenreName = nearest.Result.GenreName,
            Confidence = Math.Min(0.99f, nearest.Score / 11f)
        };
    }

    private static int ScoreSimilarity(GenreRecommendationInput input, GenreRecommendationInput example)
    {
        // Mood và age rating có trọng số cao hơn vì ảnh hưởng mạnh nhất tới loại phim.
        var score = 0;
        if (EqualsIgnoreCase(input.Mood, example.Mood)) score += 4;
        if (EqualsIgnoreCase(input.Companion, example.Companion)) score += 2;
        if (EqualsIgnoreCase(input.Intensity, example.Intensity)) score += 2;
        if (EqualsIgnoreCase(input.AgeRating, example.AgeRating)) score += 3;
        return score;
    }

    private static bool EqualsIgnoreCase(string left, string right)
    {
        return string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildLookupKey(GenreRecommendationInput input)
    {
        return BuildLookupKey(input.Mood, input.Companion, input.Intensity, input.AgeRating);
    }

    private static string BuildLookupKey(string mood, string companion, string intensity, string ageRating)
    {
        return string.Join('|',
            mood.Trim().ToLowerInvariant(),
            companion.Trim().ToLowerInvariant(),
            intensity.Trim().ToLowerInvariant(),
            ageRating.Trim().ToLowerInvariant());
    }

    private sealed record TrainingExample(
        GenreRecommendationInput Input,
        GenreRecommendationResult Result);
}
