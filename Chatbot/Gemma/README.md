# Gemma Movie Recommendation Chatbot

Thư mục này chứa Web API mẫu để backend gọi model Gemma qua LM Studio và trả về gợi ý phim. API nhận câu hỏi của user, hỏi model để rút ra `mood` và `keywords`, sau đó dùng MongoDB để tìm `MovieID` phù hợp và lấy thông tin phim đầy đủ từ SQL Server.

## 1. Thành phần chính

- `gguf/gemma2-movie-chatbot-Q4_K_M.gguf`: model đã fine-tune/quantize dùng cho LM Studio.
- `TestModel/TestModel`: ASP.NET Core Web API mẫu.
- `ChatController`: endpoint cho frontend/backend gọi gợi ý phim.
- `LmStudioService`: gọi LM Studio OpenAI-compatible API.
- `MongoRecommendationService`: tìm phim theo `mood`, `keywords`, `themes` trong MongoDB.
- `MovieQueryService`: lấy thông tin phim, lịch chiếu và giá vé từ SQL Server.

Lưu ý: file `.gguf` thường rất lớn nên không nên push bằng Git thường. Trong repo này `gguf/*.gguf` được ignore; dev cần tự copy model vào thư mục `gguf` hoặc team dùng Git LFS/GitHub Release để chia sẻ model.

## 2. Điều kiện cần có

- .NET SDK 9.0 hoặc mới hơn theo `TestModel.csproj`.
- LM Studio đã cài trên máy chạy model.
- MongoDB có database recommendation.
- SQL Server có database phim của backend chính.

NuGet packages đang dùng:

- `Dapper`
- `Microsoft.Data.SqlClient`
- `MongoDB.Driver`
- `Swashbuckle.AspNetCore`

## 3. Chạy model bằng LM Studio

1. Mở LM Studio.
2. Import model:

```text
gguf/gemma2-movie-chatbot-Q4_K_M.gguf
```

3. Vào tab Local Server.
4. Chọn model `gemma2-movie-chatbot-Q4_K_M`.
5. Start server OpenAI-compatible.
6. Mặc định service đang gọi LM Studio tại:

```text
http://127.0.0.1:1234/v1/
```

Nếu đổi port hoặc host, cập nhật `LmStudio:BaseUrl` trong `appsettings.json`.

## 4. Cấu hình backend

File cấu hình mẫu:

```json
{
  "ConnectionStrings": {
    "SqlServer": "Server=(localdb)\\MSSQLLocalDB;Database=MovieTicketDB;Trusted_Connection=True;TrustServerCertificate=True;",
    "MongoDb": "mongodb://localhost:27017"
  },
  "Mongo": {
    "Database": "MovieTicketRecommendationDB"
  },
  "LmStudio": {
    "BaseUrl": "http://127.0.0.1:1234/v1/",
    "Model": "gemma2-movie-chatbot-Q4_K_M"
  }
}
```

Cần sửa theo môi trường backend thật:

- `ConnectionStrings:SqlServer`: database SQL Server đang chứa bảng `Movies`, `MovieGenres`, `Genres`, `Showtimes`.
- `ConnectionStrings:MongoDb`: URI MongoDB.
- `Mongo:Database`: database chứa collection `movie_enrichments`.
- `LmStudio:BaseUrl`: base URL của LM Studio local server.
- `LmStudio:Model`: tên model đang load trong LM Studio.

## 5. Dữ liệu MongoDB cần có

Service đang đọc collection:

```text
movie_enrichments
```

Mỗi document nên có các field sau:

```json
{
  "movieId": 1,
  "title": "Tên phim",
  "keywords": ["hành động", "gia đình"],
  "moods": ["vui", "cảm động"],
  "themes": ["tinh thần đồng đội"],
  "targetAudience": ["teen", "family"],
  "countryName": "US",
  "languageName": "English",
  "extraDescription": "Mô tả bổ sung"
}
```

Quan trọng nhất là `movieId`, `keywords`, `moods`, `themes`. `movieId` phải trùng với `Movies.MovieID` trong SQL Server.

## 6. Đăng ký service vào backend ASP.NET Core

Nếu gắn vào backend chính, copy các thư mục/file sau từ project mẫu:

```text
Models/ChatRequest.cs
Models/ChatResponse.cs
Models/LmStudioDtos.cs
Models/MovieEnrichment.cs
Models/MovieRecommendationDto.cs
Services/ILmStudioService.cs
Services/LmStudioService.cs
Services/MongoRecommendationService.cs
Services/MovieQueryService.cs
Controllers/ChatController.cs
```

Sau đó đăng ký DI trong `Program.cs` của backend:

```csharp
builder.Services.AddSingleton<MongoRecommendationService>();
builder.Services.AddScoped<MovieQueryService>();

builder.Services.AddHttpClient<ILmStudioService, LmStudioService>(client =>
{
    var baseUrl = builder.Configuration["LmStudio:BaseUrl"];

    if (string.IsNullOrWhiteSpace(baseUrl))
    {
        throw new Exception("Chưa cấu hình LmStudio:BaseUrl trong appsettings.json");
    }

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromMinutes(3);
});
```

Nếu frontend khác domain, bật CORS theo policy của backend. Project mẫu đang để `AllowAnyOrigin` cho môi trường dev.

## 7. Endpoint gợi ý phim

Endpoint:

```http
POST /api/chat/recommend
Content-Type: application/json
```

Request:

```json
{
  "message": "Tôi muốn xem phim vui vẻ, nhẹ nhàng để đi với gia đình"
}
```

Response mẫu:

```json
{
  "answer": "Mình gợi ý bạn các phim có không khí vui vẻ, phù hợp xem cùng gia đình. <recommend>mood='vui vẻ, nhẹ nhàng', keywords='gia đình, hài, ấm áp'</recommend>",
  "mood": "vui vẻ, nhẹ nhàng",
  "keywords": "gia đình, hài, ấm áp",
  "movies": [
    {
      "movieId": 1,
      "title": "Tên phim",
      "synopsis": "Nội dung phim",
      "posterUrl": "/poster.jpg",
      "trailer": "https://...",
      "ageRating": "P",
      "duration": 120,
      "genres": "Comedy, Family",
      "nextShowtime": "2026-07-01T19:30:00",
      "basePrice": 90000
    }
  ]
}
```

Nếu model không trả được `mood` hoặc `keywords`, API sẽ fallback lấy danh sách phim ngẫu nhiên từ SQL Server.

## 8. Luồng xử lý

1. Client gửi `message` lên `POST /api/chat/recommend`.
2. `ChatController` gọi `ILmStudioService.AskAsync(message)`.
3. `LmStudioService` gửi request tới LM Studio endpoint `chat/completions`.
4. Model trả câu trả lời kèm tag:

```text
<recommend>mood='...', keywords='...'</recommend>
```

5. Backend tách `mood` và `keywords` bằng regex.
6. `MongoRecommendationService` tìm `movieId` trong `movie_enrichments`.
7. `MovieQueryService` lấy thông tin phim từ SQL Server.
8. API trả `answer`, `mood`, `keywords`, `movies`.

## 9. Chạy project mẫu

Tại thư mục này:

```powershell
cd TestModel\TestModel
dotnet restore
dotnet run
```

Mở Swagger theo URL hiện trong console, thường là:

```text
http://localhost:5189/swagger
```

Hoặc gọi nhanh bằng curl:

```bash
curl -X POST "http://localhost:5189/api/chat/recommend" \
  -H "Content-Type: application/json" \
  -d "{\"message\":\"Tôi muốn xem phim hành động kịch tính\"}"
```

## 10. Lưu ý khi gắn vào backend chính

- Không commit connection string thật lên git. Nên dùng `appsettings.Development.json`, environment variables, hoặc secret manager.
- Máy chạy backend phải truy cập được LM Studio. Nếu backend chạy Docker/container, `127.0.0.1` có thể không trỏ tới máy host.
- Tên model trong `LmStudio:Model` phải khớp với tên model LM Studio đang expose.
- Collection MongoDB phải có dữ liệu enrichment trước khi gọi recommendation.
- `movieId` trong MongoDB phải khớp với `MovieID` trong SQL Server, nếu không API sẽ không lấy được thông tin phim.
- Nên xử lý logging và exception middleware trong backend chính để lỗi LM Studio/Mongo/SQL trả về rõ ràng hơn.
