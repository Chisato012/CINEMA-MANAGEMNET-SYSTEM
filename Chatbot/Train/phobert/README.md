# PhoBERT Models For Chatbot API

Folder này chứa dữ liệu train/test và là nơi đặt 2 model PhoBERT local cho chatbot phim:

```text
finetune-phobert/                 # intent/movie model
phobert-mood/checkpoint-1680/     # mood model
```

Các folder model đã được `.gitignore`, vì vậy **không push weights lên GitHub**. Sau khi clone repo, mỗi máy dev/server cần tự đặt model vào đúng path hoặc sửa đường dẫn trong API config.  
Link cho 2 model phobert: [phobert](https://cmcuni-my.sharepoint.com/:u:/g/personal/bit242520_st_cmcu_edu_vn/IQA-nClQ5Os0SIRBv3SFW-YNAYpbMMsH6NSIpb5XrLHVLoQ?e=CuDEaK)  
## Vai Trò 2 Model

`finetune-phobert`

Model phân loại intent của câu người dùng:

```text
Greeting
Goodbye
Thanks
Help
MovieInfo
RecommendMovie
SearchMovie
```

Model này giúp backend biết câu đó là xã giao, hỏi thông tin phim, tìm phim hay cần gợi ý phim.

`phobert-mood`

Model phân loại tâm trạng:

```text
none
stress
sad
laugh
healing
excited
lonely
cry
scary
```

Model này giúp chatbot cá nhân hóa gợi ý phim theo cảm xúc người dùng, ví dụ `stress -> phim nhẹ nhàng/chữa lành`, `scary -> kinh dị/trinh thám`, `laugh -> hài/giải trí`.

## Cấu Trúc Model Cần Có

Mỗi folder model cần có các file tối thiểu:

```text
config.json
model.safetensors hoặc pytorch_model.bin
vocab.txt
bpe.codes
tokenizer_config.json
```

Với mood model hiện tại, API đang trỏ tới:

```text
Chatbot/Train/phobert/phobert-mood/checkpoint-1680
```

Nếu sau này bạn save model mood ra root `phobert-mood/`, hãy đổi `MoodModelDir` trong:

```text
Chatbot/RecommendationAPI/TestModel/appsettings.json
```

## API Chạy Model Nằm Ở Đâu

Backend web **không gọi trực tiếp file model**. Backend nên gọi API test model ở:

```text
Chatbot/RecommendationAPI/TestModel
```

API này là ASP.NET Minimal API, bên trong sẽ gọi Python script để load PhoBERT, query SQL Server, query MongoDB rồi trả JSON.

Luồng đầy đủ:

```text
Web backend / frontend
-> POST /chat
-> intent model
-> mood model
-> SQL Server lấy phim/suất chiếu
-> MongoDB lấy enrichment/lịch sử user
-> rank phim
-> trả JSON recommendations
```

## Cấu Hình API

File:

```text
Chatbot/RecommendationAPI/TestModel/appsettings.json
```

Section chính:

```json
{
  "PhoBert": {
    "PythonExecutable": "..\\..\\.venv\\Scripts\\python.exe",
    "PredictScriptPath": "predict_intent.py",
    "HybridScriptPath": "hybrid_chat.py",
    "IntentModelDir": "..\\..\\Train\\phobert\\finetune-phobert",
    "MoodModelDir": "..\\..\\Train\\phobert\\phobert-mood\\checkpoint-1680",
    "TimeoutSeconds": 120,
    "MaxLength": 96,
    "TopK": 7,
    "SqlServer": "(localdb)\\MSSQLLocalDB",
    "SqlDatabase": "MovieTicketDB",
    "OdbcDriver": "ODBC Driver 17 for SQL Server",
    "SqlAuth": "windows",
    "SqlUsername": "sa",
    "SqlPassword": "",
    "SqlEncrypt": "No",
    "MongoUri": "mongodb://localhost:27017",
    "MongoDatabase": "MovieTicketRecommendationDB"
  }
}
```

Nếu deploy sang server khác, cần sửa:

```text
PythonExecutable
IntentModelDir
MoodModelDir
SqlServer
SqlDatabase
MongoUri
MongoDatabase
```

## Chạy Model API

Từ root repo:

```powershell
dotnet run --project Chatbot\RecommendationAPI\TestModel\TestModel.csproj --urls http://localhost:5083
```

Swagger:

```text
http://localhost:5083/swagger
```

Kiểm tra nhanh:

```powershell
Invoke-RestMethod http://localhost:5083/health
Invoke-RestMethod http://localhost:5083/diagnostics
```

`/diagnostics` phải thấy:

```text
models.intent.available/readable
models.mood.available/readable
databases.sqlServer.status = sqlserver
databases.mongodb.status = mongodb
```

Nếu SQL Server chưa chạy, API vẫn có thể fallback sang phim mẫu. Nếu MongoDB chưa chạy, API vẫn gợi ý được nhưng không cá nhân hóa.

## Endpoint Cho Backend Web

### 1. Test Intent Model

Endpoint: `POST /predict/intent`

```http
POST http://localhost:5083/predict/intent
Content-Type: application/json
```

Request:

```json
{
  "text": "cảm ơn nhé",
  "topK": 7
}
```

Response rút gọn:

```json
{
  "task": "intent",
  "text": "cảm ơn nhé",
  "label": "Thanks",
  "intent": "Thanks",
  "confidence": 0.996,
  "device": "cpu",
  "scores": [
    { "label": "Thanks", "score": 0.996 }
  ]
}
```

Backend dùng endpoint này khi chỉ muốn kiểm tra model intent, chưa cần query database.

### 2. Test Mood Model

Endpoint: `POST /predict/mood`

```http
POST http://localhost:5083/predict/mood
Content-Type: application/json
```

Request:

```json
{
  "text": "tôi buồn quá muốn xem phim ấm áp",
  "topK": 9
}
```

Response rút gọn:

```json
{
  "task": "mood",
  "text": "tôi buồn quá muốn xem phim ấm áp",
  "label": "sad",
  "mood": "sad",
  "confidence": 0.955,
  "device": "cpu",
  "scores": [
    { "label": "sad", "score": 0.955 }
  ]
}
```

Backend dùng endpoint này khi chỉ muốn biết tâm trạng, chưa cần gợi ý phim.

### 3. Endpoint Chính Cho Chatbot

Endpoint: `POST /chat`

```http
POST http://localhost:5083/chat
Content-Type: application/json
```

Request:

```json
{
  "userId": 1,
  "message": "hôm nay tôi hơi stress, gợi ý phim gì đó nhẹ nhàng",
  "topK": 5
}
```

`userId` có thể null. Nếu có `userId`, API sẽ đọc lịch sử MongoDB để cá nhân hóa ranking.

Response rút gọn:

```json
{
  "mode": "hybrid",
  "message": "hôm nay tôi hơi stress, gợi ý phim gì đó nhẹ nhàng",
  "userId": 1,
  "answer": "Mình gợi ý theo tâm trạng căng thẳng và thể loại Hoạt hình, Gia đình, Cảm động: Cô bé Ponyo, Your Name...",
  "intent": {
    "label": "RecommendMovie",
    "confidence": 0.998
  },
  "mood": {
    "label": "stress",
    "confidence": 0.952
  },
  "entities": {
    "moods": ["stress"],
    "moodNames": ["căng thẳng"],
    "genres": ["Hoạt hình", "Gia đình", "Cảm động"],
    "time": {
      "kind": "today",
      "from": "2026-06-21",
      "to": "2026-06-21"
    },
    "movieTitle": null
  },
  "recommendations": [
    {
      "movieId": 7,
      "title": "Cô bé Ponyo",
      "genres": "Hoạt hình, Gia đình, Cảm động",
      "score": 19.5,
      "reasons": ["Khớp thể loại Hoạt hình"],
      "synopsis": "...",
      "ageRating": "P",
      "duration": 101,
      "posterUrl": "/img/poster/ponyo.jpg",
      "showtimes": [
        {
          "showtimeId": 3111,
          "roomName": "Phòng chiếu 7 (Standard)",
          "startTime": "2026-06-21T12:30:00",
          "basePrice": 85000
        }
      ]
    }
  ],
  "dataSources": {
    "movies": "sqlserver",
    "personalization": "mongodb",
    "mongoEnrichments": 208,
    "userSignals": 0
  }
}
```

Frontend/backend nên dùng:

```text
answer: nội dung chatbot nói với user
recommendations: render card phim
recommendations[].showtimes: render suất chiếu nếu có
intent.label: debug/routing nếu cần
mood.label: debug/cá nhân hóa UI nếu cần
dataSources: kiểm tra API đang dùng SQL/Mongo thật hay fallback
```

### 4. Test Nhiều Câu Như Một User Flow

Endpoint: `POST /test/user-flow`

```http
POST http://localhost:5083/test/user-flow
Content-Type: application/json
```

Request:

```json
{
  "userId": 1,
  "topK": 5,
  "messages": [
    "xin chào, bạn giúp mình chọn phim được không",
    "hôm nay tôi hơi stress, gợi ý phim gì đó nhẹ nhàng",
    "tìm phim kinh dị tối nay có suất chiếu không",
    "nội dung phim Your Name là gì",
    "cảm ơn nhé"
  ]
}
```

Endpoint này chủ yếu để demo Swagger và kiểm tra toàn luồng.

## Ví Dụ Backend ASP.NET Gọi API

Ví dụ service đơn giản trong backend web:

```csharp
using System.Net.Http.Json;

public sealed class ChatbotModelClient
{
    private readonly HttpClient _http;

    public ChatbotModelClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<ChatbotResponse?> ChatAsync(string message, int? userId, CancellationToken cancellationToken)
    {
        var request = new
        {
            message,
            userId,
            topK = 5
        };

        var response = await _http.PostAsJsonAsync("/chat", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ChatbotResponse>(cancellationToken: cancellationToken);
    }
}

public sealed class ChatbotResponse
{
    public string? Mode { get; set; }
    public string? Answer { get; set; }
    public LabelResult? Intent { get; set; }
    public LabelResult? Mood { get; set; }
    public List<MovieRecommendation> Recommendations { get; set; } = [];
}

public sealed class LabelResult
{
    public string? Label { get; set; }
    public double Confidence { get; set; }
}

public sealed class MovieRecommendation
{
    public int MovieId { get; set; }
    public string? Title { get; set; }
    public string? Genres { get; set; }
    public double Score { get; set; }
    public string? PosterUrl { get; set; }
    public List<ShowtimeDto> Showtimes { get; set; } = [];
}

public sealed class ShowtimeDto
{
    public int ShowtimeId { get; set; }
    public string? RoomName { get; set; }
    public DateTime StartTime { get; set; }
    public decimal BasePrice { get; set; }
}
```

Đăng ký `HttpClient`:

```csharp
builder.Services.AddHttpClient<ChatbotModelClient>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5083");
    client.Timeout = TimeSpan.FromSeconds(180);
});
```

Controller backend web có thể gọi:

```csharp
[HttpPost("chatbot/message")]
public async Task<IActionResult> SendMessage(ChatRequest request, CancellationToken cancellationToken)
{
    var result = await _chatbotModelClient.ChatAsync(
        request.Message,
        request.UserId,
        cancellationToken);

    return Ok(result);
}
```

## Lưu Ý Khi Tích Hợp Web Thật

Hiện `TestModel` chạy Python theo từng request. Cách này ổn để demo, test Swagger và gắn thử backend, nhưng request đầu có thể chậm vì phải load model.

Khi hoàn thiện production, nên cân nhắc:

```text
ASP.NET web chính
-> gọi một Python FastAPI service giữ model trong RAM
-> model chỉ load một lần khi service start
```

Tuy vậy contract JSON của `/chat`, `/predict/intent`, `/predict/mood` nên giữ ổn định để frontend/backend không phải sửa nhiều.

## Troubleshooting

`/health` báo model thiếu weights:

Kiểm tra folder model có `model.safetensors` hoặc `pytorch_model.bin`.

`/diagnostics` báo `canReadWeights = false`:

File model đang bị lock hoặc quyền đọc sai. Đóng process Python/Colab/Drive sync đang giữ file, hoặc copy lại model sang folder local khác rồi sửa `MoodModelDir` / `IntentModelDir`.

`/diagnostics` báo SQL Server unavailable:

Kiểm tra `SqlServer`, `SqlDatabase`, ODBC Driver và database `MovieTicketDB`.

`/diagnostics` báo MongoDB unavailable:

Kiểm tra MongoDB service, `MongoUri`, `MongoDatabase` và package `pymongo`.

Response có `dataSources.movies = static_fallback`:

API đang không lấy được SQL Server, nên chỉ dùng danh sách phim mẫu trong Python.

Response có `dataSources.personalization = unavailable`:

API không lấy được MongoDB, nên vẫn gợi ý phim nhưng chưa cá nhân hóa theo lịch sử user.
