using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// This project is a local test API, so Swagger is intentionally enabled in every environment.
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();
app.MapControllers();

var defaultScenarioMessages = new List<string>
{
    "xin chào, bạn giúp mình chọn phim được không",
    "hôm nay tôi hơi stress, gợi ý phim gì đó nhẹ nhàng",
    "tìm phim kinh dị tối nay có suất chiếu không",
    "nội dung phim Your Name là gì",
    "cảm ơn nhé",
};

app.MapGet("/health", (IConfiguration configuration, IWebHostEnvironment environment) =>
{
    var intentModelDir = ResolvePath(environment.ContentRootPath, GetSetting(configuration, "PhoBert:IntentModelDir", "PhoBertIntent:ModelDir"));
    var moodModelDir = ResolvePath(environment.ContentRootPath, GetSetting(configuration, "PhoBert:MoodModelDir") ?? "..\\..\\Train\\phobert\\phobert-mood\\checkpoint-1680");
    var predictScript = ResolvePath(environment.ContentRootPath, GetSetting(configuration, "PhoBert:PredictScriptPath", "PhoBertIntent:ScriptPath") ?? "predict_intent.py");
    var hybridScript = ResolvePath(environment.ContentRootPath, GetSetting(configuration, "PhoBert:HybridScriptPath", "PhoBertIntent:HybridScriptPath") ?? "hybrid_chat.py");

    return Results.Ok(new
    {
        status = "ok",
        service = "RecommendationAPI.TestModel",
        swagger = "/swagger",
        scripts = new
        {
            predict = new { path = predictScript, exists = File.Exists(predictScript) },
            hybrid = new { path = hybridScript, exists = File.Exists(hybridScript) },
        },
        models = new
        {
            intent = ModelSnapshot(intentModelDir),
            mood = ModelSnapshot(moodModelDir),
        },
    });
})
.WithTags("00. Health")
.WithSummary("Kiểm tra API, script Python và 2 model PhoBERT.");

app.MapGet("/diagnostics", async (
    IConfiguration configuration,
    IWebHostEnvironment environment,
    CancellationToken cancellationToken) =>
{
    try
    {
        var response = await RunPythonHybridAsync(
            message: null,
            userId: null,
            topK: null,
            diagnostics: true,
            configuration,
            environment,
            cancellationToken);
        return JsonContent(response);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithTags("00. Health")
.WithSummary("Kiểm tra Python dependencies, 2 model, SQL Server và MongoDB.");

app.MapGet("/labels/{task}", async (
    string task,
    IConfiguration configuration,
    IWebHostEnvironment environment) =>
{
    task = task.Trim().ToLowerInvariant();
    if (task is not ("intent" or "mood"))
    {
        return Results.BadRequest(new { error = "task must be either intent or mood." });
    }

    var modelDir = GetModelDir(task, configuration, environment);
    var metadataNames = task == "mood"
        ? new[] { "mood_metadata.json", "intent_metadata.json" }
        : new[] { "intent_metadata.json", "mood_metadata.json" };

    foreach (var metadataName in metadataNames)
    {
        var metadataPath = Path.Combine(modelDir, metadataName);
        if (File.Exists(metadataPath))
        {
            var metadata = await File.ReadAllTextAsync(metadataPath, Encoding.UTF8);
            return Results.Content(metadata, "application/json", Encoding.UTF8);
        }
    }

    var configPath = Path.Combine(modelDir, "config.json");
    if (!File.Exists(configPath))
    {
        return Results.NotFound(new { error = "No metadata/config file was found.", task, modelDir });
    }

    using var document = JsonDocument.Parse(await File.ReadAllTextAsync(configPath, Encoding.UTF8));
    var labels = new List<string>();
    if (document.RootElement.TryGetProperty("id2label", out var id2Label))
    {
        labels.AddRange(id2Label.EnumerateObject()
            .OrderBy(item => int.TryParse(item.Name, out var index) ? index : int.MaxValue)
            .Select(item => item.Value.GetString() ?? item.Name));
    }

    return Results.Ok(new { task, modelDir, labels });
})
.WithTags("01. Models")
.WithSummary("Đọc nhãn của intent model hoặc mood model.");

app.MapPost("/predict/intent", async (
    PredictLabelRequest request,
    IConfiguration configuration,
    IWebHostEnvironment environment,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Text))
    {
        return Results.BadRequest(new { error = "Text must not be empty." });
    }

    try
    {
        var response = await RunPythonPredictAsync("intent", request.Text, request.TopK, configuration, environment, cancellationToken);
        return JsonContent(response);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithTags("01. Models")
.WithSummary("Test PhoBERT intent/movie model.");

app.MapPost("/predict/mood", async (
    PredictLabelRequest request,
    IConfiguration configuration,
    IWebHostEnvironment environment,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Text))
    {
        return Results.BadRequest(new { error = "Text must not be empty." });
    }

    try
    {
        var response = await RunPythonPredictAsync("mood", request.Text, request.TopK, configuration, environment, cancellationToken);
        return JsonContent(response);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithTags("01. Models")
.WithSummary("Test PhoBERT mood model.");

app.MapPost("/predict", async (
    PredictLabelRequest request,
    IConfiguration configuration,
    IWebHostEnvironment environment,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Text))
    {
        return Results.BadRequest(new { error = "Text must not be empty." });
    }

    try
    {
        var response = await RunPythonPredictAsync("intent", request.Text, request.TopK, configuration, environment, cancellationToken);
        return JsonContent(response);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithTags("01. Models")
.WithSummary("Alias cũ cho /predict/intent.");

app.MapPost("/chat", async (
    ChatRequest request,
    IConfiguration configuration,
    IWebHostEnvironment environment,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Message))
    {
        return Results.BadRequest(new { error = "Message must not be empty." });
    }

    try
    {
        var response = await RunPythonHybridAsync(
            request.Message,
            request.UserId,
            request.TopK,
            diagnostics: false,
            configuration,
            environment,
            cancellationToken);
        return JsonContent(response);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithTags("02. Chatbot")
.WithSummary("Endpoint chính: intent model + mood model + SQL Server + MongoDB.");

app.MapGet("/test/scenarios", () => Results.Ok(new
{
    description = "Copy một message qua /chat hoặc chạy cả danh sách bằng /test/user-flow.",
    messages = defaultScenarioMessages,
    sampleUserFlowRequest = new UserFlowTestRequest
    {
        UserId = 1,
        TopK = 5,
        Messages = defaultScenarioMessages,
    },
}))
.WithTags("03. Swagger user tests")
.WithSummary("Danh sách câu test mô phỏng người dùng.");

app.MapPost("/test/user-flow", async (
    UserFlowTestRequest request,
    IConfiguration configuration,
    IWebHostEnvironment environment,
    CancellationToken cancellationToken) =>
{
    var messages = request.Messages is { Count: > 0 }
        ? request.Messages.Where(message => !string.IsNullOrWhiteSpace(message)).ToList()
        : defaultScenarioMessages;

    var results = new List<JsonNode?>();
    foreach (var message in messages)
    {
        try
        {
            var response = await RunPythonHybridAsync(
                message,
                request.UserId,
                request.TopK,
                diagnostics: false,
                configuration,
                environment,
                cancellationToken);
            results.Add(JsonNode.Parse(response));
        }
        catch (Exception ex)
        {
            results.Add(new JsonObject
            {
                ["message"] = message,
                ["error"] = ex.Message,
            });
        }
    }

    return Results.Ok(new
    {
        userId = request.UserId,
        count = messages.Count,
        results,
    });
})
.WithTags("03. Swagger user tests")
.WithSummary("Chạy một luồng chat mẫu qua cả 2 model và 2 database.");

app.Run();

static async Task<string> RunPythonPredictAsync(
    string task,
    string text,
    int? topK,
    IConfiguration configuration,
    IWebHostEnvironment environment,
    CancellationToken cancellationToken)
{
    var pythonExecutable = GetPythonExecutable(configuration, environment);
    var scriptPath = ResolvePath(
        environment.ContentRootPath,
        GetSetting(configuration, "PhoBert:PredictScriptPath", "PhoBertIntent:ScriptPath") ?? "predict_intent.py");
    var modelDir = GetModelDir(task, configuration, environment);
    var timeoutSeconds = Math.Max(1, GetInt(configuration, "TimeoutSeconds", 120));
    var maxLength = Math.Max(1, GetInt(configuration, "MaxLength", 96));
    var configuredTopK = Math.Clamp(topK ?? GetInt(configuration, "TopK", 7), 1, 20);

    if (!File.Exists(scriptPath))
    {
        throw new FileNotFoundException("Python prediction script was not found.", scriptPath);
    }

    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

    var startInfo = CreatePythonStartInfo(pythonExecutable, scriptPath, environment.ContentRootPath);
    startInfo.ArgumentList.Add(scriptPath);
    startInfo.ArgumentList.Add("--text");
    startInfo.ArgumentList.Add(text);
    startInfo.ArgumentList.Add("--task");
    startInfo.ArgumentList.Add(task);
    startInfo.ArgumentList.Add("--model-dir");
    startInfo.ArgumentList.Add(modelDir);
    startInfo.ArgumentList.Add("--max-length");
    startInfo.ArgumentList.Add(maxLength.ToString());
    startInfo.ArgumentList.Add("--top-k");
    startInfo.ArgumentList.Add(configuredTopK.ToString());

    return await RunProcessAsync(startInfo, timeoutCts.Token, $"{task} prediction");
}

static async Task<string> RunPythonHybridAsync(
    string? message,
    int? userId,
    int? topK,
    bool diagnostics,
    IConfiguration configuration,
    IWebHostEnvironment environment,
    CancellationToken cancellationToken)
{
    var pythonExecutable = GetPythonExecutable(configuration, environment);
    var scriptPath = ResolvePath(
        environment.ContentRootPath,
        GetSetting(configuration, "PhoBert:HybridScriptPath", "PhoBertIntent:HybridScriptPath") ?? "hybrid_chat.py");
    var timeoutSeconds = Math.Max(1, GetInt(configuration, "TimeoutSeconds", 120));
    var maxLength = Math.Max(1, GetInt(configuration, "MaxLength", 96));
    var configuredTopK = Math.Clamp(topK ?? GetInt(configuration, "TopK", 5), 1, 20);

    if (!File.Exists(scriptPath))
    {
        throw new FileNotFoundException("Hybrid chatbot Python script was not found.", scriptPath);
    }

    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

    var startInfo = CreatePythonStartInfo(pythonExecutable, scriptPath, environment.ContentRootPath);
    startInfo.ArgumentList.Add(scriptPath);
    if (diagnostics)
    {
        startInfo.ArgumentList.Add("--diagnostics");
    }
    else
    {
        startInfo.ArgumentList.Add("--message");
        startInfo.ArgumentList.Add(message ?? "");
    }

    startInfo.ArgumentList.Add("--top-k");
    startInfo.ArgumentList.Add(configuredTopK.ToString());
    startInfo.ArgumentList.Add("--intent-model-dir");
    startInfo.ArgumentList.Add(GetModelDir("intent", configuration, environment));
    startInfo.ArgumentList.Add("--mood-model-dir");
    startInfo.ArgumentList.Add(GetModelDir("mood", configuration, environment));
    startInfo.ArgumentList.Add("--max-length");
    startInfo.ArgumentList.Add(maxLength.ToString());
    AddOptionalArgument(startInfo, "--user-id", userId?.ToString());
    AddRequiredArgument(startInfo, "--sql-server", GetSetting(configuration, "PhoBert:SqlServer", "PhoBertIntent:SqlServer"));
    AddRequiredArgument(startInfo, "--sql-database", GetSetting(configuration, "PhoBert:SqlDatabase", "PhoBertIntent:SqlDatabase"));
    AddRequiredArgument(startInfo, "--odbc-driver", GetSetting(configuration, "PhoBert:OdbcDriver", "PhoBertIntent:OdbcDriver"));
    AddRequiredArgument(startInfo, "--sql-auth", GetSetting(configuration, "PhoBert:SqlAuth", "PhoBertIntent:SqlAuth"));
    AddRequiredArgument(startInfo, "--sql-username", GetSetting(configuration, "PhoBert:SqlUsername", "PhoBertIntent:SqlUsername"));
    AddRequiredArgument(startInfo, "--sql-password", GetSetting(configuration, "PhoBert:SqlPassword", "PhoBertIntent:SqlPassword"));
    AddRequiredArgument(startInfo, "--sql-encrypt", GetSetting(configuration, "PhoBert:SqlEncrypt", "PhoBertIntent:SqlEncrypt"));
    AddRequiredArgument(startInfo, "--mongo-uri", GetSetting(configuration, "PhoBert:MongoUri", "PhoBertIntent:MongoUri"));
    AddRequiredArgument(startInfo, "--mongo-database", GetSetting(configuration, "PhoBert:MongoDatabase", "PhoBertIntent:MongoDatabase"));

    return await RunProcessAsync(startInfo, timeoutCts.Token, diagnostics ? "diagnostics" : "hybrid chatbot");
}

static ProcessStartInfo CreatePythonStartInfo(string pythonExecutable, string scriptPath, string contentRootPath)
{
    return new ProcessStartInfo
    {
        FileName = pythonExecutable,
        WorkingDirectory = Path.GetDirectoryName(scriptPath) ?? contentRootPath,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        StandardOutputEncoding = Encoding.UTF8,
        StandardErrorEncoding = Encoding.UTF8,
        UseShellExecute = false,
        CreateNoWindow = true,
    };
}

static async Task<string> RunProcessAsync(ProcessStartInfo startInfo, CancellationToken cancellationToken, string operationName)
{
    using var process = Process.Start(startInfo)
        ?? throw new InvalidOperationException("Could not start Python process.");

    var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
    var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

    try
    {
        await process.WaitForExitAsync(cancellationToken);
    }
    catch (OperationCanceledException)
    {
        TryKill(process);
        throw new TimeoutException($"{operationName} timed out.");
    }

    var output = await outputTask;
    var error = await errorTask;

    if (process.ExitCode != 0)
    {
        throw new InvalidOperationException(string.IsNullOrWhiteSpace(error) ? $"{operationName} failed." : error.Trim());
    }

    return output.Trim();
}

static IResult JsonContent(string json)
{
    return Results.Content(json, "application/json", Encoding.UTF8);
}

static object ModelSnapshot(string modelDir)
{
    var configPath = Path.Combine(modelDir, "config.json");
    var safetensorsPath = Path.Combine(modelDir, "model.safetensors");
    var pytorchPath = Path.Combine(modelDir, "pytorch_model.bin");
    return new
    {
        path = modelDir,
        exists = Directory.Exists(modelDir),
        hasConfig = File.Exists(configPath),
        canReadConfig = CanReadFile(configPath),
        hasTokenizer = File.Exists(Path.Combine(modelDir, "vocab.txt")) && File.Exists(Path.Combine(modelDir, "bpe.codes")),
        hasWeights = File.Exists(safetensorsPath) || File.Exists(pytorchPath),
        canReadWeights = CanReadFile(safetensorsPath) || CanReadFile(pytorchPath),
    };
}

static bool CanReadFile(string path)
{
    if (!File.Exists(path))
    {
        return false;
    }

    try
    {
        using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return true;
    }
    catch
    {
        return false;
    }
}

static string GetModelDir(string task, IConfiguration configuration, IWebHostEnvironment environment)
{
    var configuredPath = task.Equals("mood", StringComparison.OrdinalIgnoreCase)
        ? GetSetting(configuration, "PhoBert:MoodModelDir")
        : GetSetting(configuration, "PhoBert:IntentModelDir", "PhoBertIntent:ModelDir");

    configuredPath ??= task.Equals("mood", StringComparison.OrdinalIgnoreCase)
        ? "..\\..\\Train\\phobert\\phobert-mood\\checkpoint-1680"
        : "..\\..\\Train\\phobert\\finetune-phobert";

    return ResolvePath(environment.ContentRootPath, configuredPath);
}

static int GetInt(IConfiguration configuration, string key, int fallback)
{
    var value = GetSetting(configuration, $"PhoBert:{key}", $"PhoBertIntent:{key}");
    return int.TryParse(value, out var parsed) ? parsed : fallback;
}

static string GetPythonExecutable(IConfiguration configuration, IWebHostEnvironment environment)
{
    var configured = GetSetting(configuration, "PhoBert:PythonExecutable", "PhoBertIntent:PythonExecutable") ?? "python";
    return ResolveExecutable(environment.ContentRootPath, configured);
}

static string? GetSetting(IConfiguration configuration, params string[] keys)
{
    foreach (var key in keys)
    {
        var value = configuration[key];
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }
    }

    return null;
}

static void AddRequiredArgument(ProcessStartInfo startInfo, string name, string? value)
{
    startInfo.ArgumentList.Add(name);
    startInfo.ArgumentList.Add(value ?? "");
}

static void AddOptionalArgument(ProcessStartInfo startInfo, string name, string? value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return;
    }

    startInfo.ArgumentList.Add(name);
    startInfo.ArgumentList.Add(value);
}

static string ResolvePath(string contentRootPath, string? path)
{
    if (string.IsNullOrWhiteSpace(path))
    {
        return contentRootPath;
    }

    return Path.GetFullPath(Path.IsPathRooted(path)
        ? path
        : Path.Combine(contentRootPath, path));
}

static string ResolveExecutable(string contentRootPath, string executable)
{
    if (Path.IsPathRooted(executable))
    {
        return Path.GetFullPath(executable);
    }

    if (executable.Contains(Path.DirectorySeparatorChar) ||
        executable.Contains(Path.AltDirectorySeparatorChar))
    {
        return Path.GetFullPath(Path.Combine(contentRootPath, executable));
    }

    return executable;
}

static void TryKill(Process process)
{
    try
    {
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
        }
    }
    catch (InvalidOperationException)
    {
    }
}

public sealed class PredictLabelRequest
{
    public string Text { get; init; } = "";
    public int? TopK { get; init; }
}

public sealed class ChatRequest
{
    public string Message { get; init; } = "";
    public int? UserId { get; init; }
    public int? TopK { get; init; }
}

public sealed class UserFlowTestRequest
{
    public int? UserId { get; init; }
    public int? TopK { get; init; }
    public List<string>? Messages { get; init; }
}
