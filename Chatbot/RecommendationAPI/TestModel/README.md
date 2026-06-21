# RecommendationAPI TestModel

API này dùng để test chatbot gợi ý phim trước khi gắn vào web ASP.NET chính.

Luồng chính:

```text
User message
-> PhoBERT intent/movie model: xác định Greeting, SearchMovie, RecommendMovie, MovieInfo...
-> PhoBERT mood model: xác định none, stress, sad, laugh, healing, excited, lonely, cry, scary
-> SQL Server: lấy phim và suất chiếu thật
-> MongoDB: lấy movie_enrichments, user_interactions, recommendation_feedback
-> Python ranking
-> ASP.NET trả JSON cho frontend
```

## Chuẩn bị model

Đặt 2 model đã fine-tune ở:

```text
Chatbot/Train/phobert/finetune-phobert
Chatbot/Train/phobert/phobert-mood
```

Mỗi folder model nên có ít nhất:

```text
config.json
model.safetensors hoặc pytorch_model.bin
vocab.txt
bpe.codes
tokenizer_config.json nếu có
```

Config hiện tại đang trỏ mood model tới:

```text
Chatbot/Train/phobert/phobert-mood/checkpoint-1680
```

Nếu đã save mood model ra root `phobert-mood`, hãy đổi `MoodModelDir` trong `appsettings.json` về root đó. Cách save model mood sạch nhất trên Colab:

```python
trainer.save_model("/content/drive/MyDrive/Colab Notebooks/Movie-RS/phobert-mood")
tokenizer.save_pretrained("/content/drive/MyDrive/Colab Notebooks/Movie-RS/phobert-mood")
```

Các folder model đã được `.gitignore` để tránh push weights lớn lên GitHub.

## Cấu hình

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

Nếu dùng SQL auth:

```json
{
  "PhoBert": {
    "SqlAuth": "sql",
    "SqlUsername": "sa",
    "SqlPassword": "your_password"
  }
}
```

## Chạy API

Từ root repo:

```powershell
dotnet run --project Chatbot\RecommendationAPI\TestModel\TestModel.csproj --urls http://localhost:5083
```

Swagger:

```text
http://localhost:5083/swagger
```

File request nhanh:

```text
Chatbot/RecommendationAPI/TestModel/TestModel.http
```

## Swagger test flow

Chạy theo thứ tự này trong Swagger:

1. `GET /health`

Kiểm tra API thấy script Python và 2 model chưa.

2. `GET /diagnostics`

Kiểm tra:

```text
Python executable
torch / pyodbc / pymongo
intent model
mood model
SQL Server movie/showtime count
MongoDB movie_enrichments/user_interactions/recommendation_feedback count
```

3. `POST /predict/intent`

Test riêng model intent:

```json
{
  "text": "cảm ơn nhé",
  "topK": 7
}
```

4. `POST /predict/mood`

Test riêng model mood:

```json
{
  "text": "hôm nay tôi hơi stress, muốn xem phim nhẹ nhàng",
  "topK": 9
}
```

5. `POST /chat`

Test luồng thật qua 2 model + SQL Server + MongoDB:

```json
{
  "userId": 1,
  "message": "mình đang stress, gợi ý phim nhẹ nhàng tối nay",
  "topK": 5
}
```

6. `POST /test/user-flow`

Mô phỏng nhiều câu liên tiếp như người dùng:

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

## Vai trò endpoint

`GET /health`

Kiểm tra nhanh API, script Python, intent model và mood model.

`GET /diagnostics`

Chạm Python, kiểm tra model files và thử kết nối cả SQL Server lẫn MongoDB.

`GET /labels/intent`

Đọc label/metadata của intent model.

`GET /labels/mood`

Đọc label/metadata của mood model. Nếu không có `mood_metadata.json`, API đọc label từ `config.json`.

`POST /predict/intent`

Chỉ chạy intent model, chưa query database.

`POST /predict/mood`

Chỉ chạy mood model, chưa query database.

`POST /chat`

Endpoint chính cho web sau này. Response có:

```text
intent: kết quả intent model
mood: kết quả mood model
entities: mood, genre, time, movieTitle
recommendations: danh sách phim đã rank
dataSources.movies: sqlserver hoặc static_fallback
dataSources.personalization: mongodb hoặc unavailable
```

`POST /test/user-flow`

Chạy nhiều message qua `/chat` để demo một cuộc hội thoại ngắn trong Swagger.

## Ghi chú database

Nếu SQL Server chưa chạy hoặc sai connection string, API dùng `static_fallback` để vẫn test được model và ranking.

Nếu MongoDB chưa chạy hoặc thiếu `pymongo`, API vẫn trả gợi ý nhưng `personalization` sẽ là `unavailable`.

Để kiểm tra cá nhân hóa MongoDB, gửi `userId` có dữ liệu trong `user_interactions` hoặc `recommendation_feedback`.
