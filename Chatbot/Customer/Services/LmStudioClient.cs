using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Customer.Services;

public sealed class LmStudioClient(HttpClient httpClient, IConfiguration configuration)
{
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<(bool Success, string? Answer)> TryCompleteAsync(
        string prompt,
        CancellationToken cancellationToken)
    {
        var baseUrl = configuration["LmStudio:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:1234/v1";
        var model = configuration["LmStudio:Model"] ?? "local-model";
        var apiKey = configuration["LmStudio:ApiKey"] ?? "lm-studio";
        var temperature = configuration.GetValue("LmStudio:Temperature", 0.3);

        var request = new
        {
            model,
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = "Bạn là chatbot tư vấn phim cho rạp COSMOS. Chỉ dùng dữ liệu được cung cấp, không bịa lịch chiếu, giá vé hoặc tên phim."
                },
                new
                {
                    role = "user",
                    content = prompt
                }
            },
            temperature
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(request, _jsonOptions),
            Encoding.UTF8,
            "application/json");

        try
        {
            using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return (false, null);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var completion = await JsonSerializer.DeserializeAsync<LmStudioResponse>(
                stream,
                _jsonOptions,
                cancellationToken);

            var answer = completion?.Choices.FirstOrDefault()?.Message.Content;
            return string.IsNullOrWhiteSpace(answer) ? (false, null) : (true, answer);
        }
        catch (HttpRequestException)
        {
            return (false, null);
        }
        catch (TaskCanceledException)
        {
            return (false, null);
        }
    }

    private sealed record LmStudioResponse(IReadOnlyList<LmStudioChoice> Choices);

    private sealed record LmStudioChoice(LmStudioMessage Message);

    private sealed record LmStudioMessage(string Content);
}
