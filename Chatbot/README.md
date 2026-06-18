# Chatbot RAG tư vấn phim

Thư mục này chứa API chatbot cho hệ thống rạp phim.

- Hiện tại đang dùng LM Studio để chạy model local.
- `Customer/`: ASP.NET Core Web API.
- API đọc dữ liệu từ `MovieTicketDB`.
- API gọi model qua LM Studio.
- Web `Cinema-Management/` gọi chatbot qua endpoint `/api/chat`.

## Luồng chatbot

```text
Web Cinema-Management
  -> Chatbot API: http://localhost:CHATBOT_PORT/api/chat
      -> MovieTicketDB
      -> LM Studio: http://localhost:1234/v1/chat/completions
```

Chatbot có 2 kiểu gợi ý:

- User mới/chưa có lịch sử: bot hỏi hoặc đọc tâm trạng/sở thích trong câu chat để gợi ý phim sắp chiếu.
- User có lịch sử booking: bot khai phá lịch sử đặt vé, thể loại hay xem, khung giờ hay xem để gợi ý suất chiếu phù hợp.

Nếu LM Studio chưa chạy, chatbot vẫn có fallback trả lời bằng logic gợi ý nội bộ.

## Port đang dùng

Port không phải cố định tuyệt đối. Repo đang để mặc định như sau:

| Thành phần | Port mặc định | Cấu hình |
| --- | --- | --- |
| Chatbot API | `http://localhost:5218` | `Customer/Properties/launchSettings.json` |
| Web MVC | `http://localhost:5074`, `https://localhost:7155` | `../Cinema-Management/Properties/launchSettings.json` |
| LM Studio | `http://localhost:1234/v1` | LM Studio app và `Customer/appsettings.json` |

`Customer/Properties/launchSettings.json` đã được dọn còn 1 profile `http` để tránh nhầm lẫn:

```json
"applicationUrl": "http://localhost:5218"
```

Khi chạy bằng lệnh có `--urls`, giá trị `--urls` sẽ override port trong `launchSettings.json`.

Ví dụ chạy chatbot bằng port khác:

```powershell
dotnet run --project Chatbot\Customer\Customer.csproj --urls http://localhost:6001
```

Khi đổi port chatbot, nhớ sửa URL widget ở:

```text
../Cinema-Management/Views/Shared/_Layout.cshtml
```

Ví dụ:

```html
data-chat-api="http://localhost:6001/api/chat"
```

## CORS là port của web

Cấu hình CORS nằm ở:

```text
Customer/appsettings.json
```

Hiện chỉ cần cho phép origin của web MVC:

```json
"Cors": {
  "AllowedOrigins": [
    "http://localhost:5074",
    "https://localhost:7155"
  ]
}
```

Không cần thêm port của chính chatbot vào CORS.

Nếu đổi port web MVC, thêm origin web mới vào `AllowedOrigins`.

## Chuẩn bị database

Database dùng tên:

```text
MovieTicketDB
```

Chạy các file SQL ở root repo theo thứ tự:

```text
../Database/01_init_tables.sql
../Database/02_insert_data_backend_style.sql
```

File `02_insert_data_backend_style.sql` là dữ liệu demo dùng để chatbot gợi ý phim, lịch chiếu, combo và khai phá lịch sử booking.

## Cấu hình database

Cấu hình connection string của chatbot tại:

```text
Customer/appsettings.json
```

Ví dụ dùng SQL Server local với tài khoản `sa`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=127.0.0.1,1433;Database=MovieTicketDB;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
}
```

Ví dụ dùng LocalDB:

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=MovieTicketDB;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;"
}
```

Lưu ý: web MVC cũng có connection string riêng tại:

```text
../Cinema-Management/appsettings.json
```

Hai project nên trỏ về cùng một database `MovieTicketDB`.

## Cấu hình LM Studio

Trong LM Studio:

1. Load model chat/instruct.
2. Mở Local Server.
3. Start server ở:

```text
http://localhost:1234/v1
```

Cấu hình chatbot gọi LM Studio tại:

```text
Customer/appsettings.json
```

```json
"LmStudio": {
  "BaseUrl": "http://localhost:1234/v1",
  "Model": "qwen3-1.7b",
  "ApiKey": "lm-studio",
  "Temperature": 0.3
}
```

Nếu đổi model trong LM Studio, sửa giá trị `Model`.

Nếu đổi port LM Studio, sửa `BaseUrl`.

## Chạy chatbot API

Từ thư mục root repo, chạy bằng port mặc định:

```powershell
dotnet run --project Chatbot\Customer\Customer.csproj
```

Hoặc chỉ định port rõ ràng:

```powershell
dotnet run --project Chatbot\Customer\Customer.csproj --urls http://localhost:5218
```

Hoặc từ thư mục `Chatbot/`:

```powershell
dotnet run --project Customer\Customer.csproj --urls http://localhost:5218
```

Swagger mặc định:

```text
http://localhost:5218/swagger
```

Endpoint chính:

```http
POST http://localhost:5218/api/chat
```

Body cho user mới:

```json
{
  "message": "Tôi muốn xem phim vui vẻ cuối tuần"
}
```

Body cho user đã có lịch sử:

```json
{
  "message": "Gợi ý phim cho tôi",
  "userId": 4
}
```

Test bằng PowerShell:

```powershell
$body = @{
  message = "Tôi muốn xem phim vui vẻ cuối tuần"
} | ConvertTo-Json

Invoke-RestMethod `
  -Uri "http://localhost:5218/api/chat" `
  -Method Post `
  -ContentType "application/json" `
  -Body $body
```

Nếu bạn chạy chatbot bằng port khác, đổi URL trong `Invoke-RestMethod` tương ứng.

## Gắn vào web MVC

Widget chat đã được gắn trong:

```text
../Cinema-Management/Views/Shared/_Layout.cshtml
```

Web đang gọi API qua:

```html
data-chat-api="http://localhost:5218/api/chat"
```

JavaScript gọi API nằm ở:

```text
../Cinema-Management/wwwroot/js/site.js
```

CSS widget nằm ở:

```text
../Cinema-Management/wwwroot/css/site.css
```

Chạy web MVC từ root repo:

```powershell
dotnet run --project Cinema-Management\Cinema-Management.csproj
```

Web mặc định chạy ở:

```text
http://localhost:5074
https://localhost:7155
```

Vào web, góc phải dưới sẽ có nút `Chat`.

## Khi đổi port

Đổi port chatbot API:

1. Chạy chatbot bằng port mới, ví dụ:

```powershell
dotnet run --project Chatbot\Customer\Customer.csproj --urls http://localhost:6001
```

2. Sửa `data-chat-api` trong:

```text
../Cinema-Management/Views/Shared/_Layout.cshtml
```

```html
data-chat-api="http://localhost:6001/api/chat"
```

Không cần sửa CORS chỉ vì đổi port chatbot.

Đổi port web MVC:

1. Sửa port web trong:

```text
../Cinema-Management/Properties/launchSettings.json
```

2. Thêm origin web mới vào `Cors:AllowedOrigins` trong:

```text
Customer/appsettings.json
```

Ví dụ web đổi sang `http://localhost:7000`:

```json
"Cors": {
  "AllowedOrigins": [
    "http://localhost:7000"
  ]
}
```

## Các file chính của chatbot

- `Customer/Controllers/ChatController.cs`: endpoint `/api/chat`.
- `Customer/Services/MovieDataService.cs`: đọc phim, lịch chiếu, lịch sử booking từ SQL Server.
- `Customer/Services/RecommendationService.cs`: chấm điểm gợi ý theo tâm trạng/lịch sử.
- `Customer/Services/LmStudioClient.cs`: gọi LM Studio.
- `Customer/Services/ChatOrchestrator.cs`: phối hợp toàn bộ luồng trả lời.

## Build kiểm tra

Từ root repo:

```powershell
dotnet build Chatbot\Customer\Customer.csproj
dotnet build Cinema-Management\Cinema-Management.csproj
```

## Lỗi thường gặp

Nếu widget báo chưa kết nối được chatbot API:

- Kiểm tra `Chatbot/Customer` đã chạy đúng port trong `data-chat-api`.
- Kiểm tra `data-chat-api` trong `_Layout.cshtml`.
- Nếu web đổi port, kiểm tra CORS trong `Customer/appsettings.json`.

Nếu API báo không có suất chiếu:

- Kiểm tra đã chạy file seed `Database/02_insert_data_backend_style.sql`.
- Kiểm tra bảng `Showtimes` có ngày từ hôm nay trở đi.

Nếu `usedLmStudio` là `false`:

- LM Studio chưa bật local server, sai port, hoặc sai tên model.
- Chatbot vẫn trả lời fallback, nhưng câu văn sẽ ít tự nhiên hơn.
