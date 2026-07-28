# Luồng gợi ý phim bằng ML.NET
## Template hiện tại: Multiclass Classification - phân loại nhiều lớp

## 1. Mục tiêu

User chọn 4 giá trị trong widget:

- `Mood`: tâm trạng/cảm giác muốn xem phim, không chứa người đi cùng
- `Companion`: đi cùng ai
- `Intensity`: nhịp phim mong muốn
- `AgeRating`: độ tuổi phù hợp

Sau đó app dự đoán `PreferredGenreCode`, đổi sang tên genre trong database, rồi lọc phim từ SQL Server.

## 2. File dữ liệu train

File train nằm ở:

```text
ML/ml_recommendation_train.csv
```

Các cột:

```text
Mood, Companion, Intensity, AgeRating, PreferredGenreCode, PreferredGenreName
```

Ý nghĩa:

- `Mood`, `Companion`, `Intensity`, `AgeRating`: feature đầu vào.
- `PreferredGenreCode`: label để ML.NET học, ví dụ `HoatHinh`, `KinhDi`.
- `PreferredGenreName`: tên genre dùng để query DB, ví dụ `Hoạt hình`, `Kinh dị`.

Lý do train bằng `PreferredGenreCode`: code ASCII ổn định hơn khi train/predict. Sau khi predict xong mới map sang tiếng Việt để so với bảng `Genres`.

## 3. Train model

Lệnh train:

```powershell
dotnet run --project Cinema-Management\Cinema-Management.csproj -- --train-recommendation-model
```

Luồng thực thi:

1. App khởi động vào `Cinema-Management/Program.cs`.
2. `Program.cs` thấy argument `--train-recommendation-model`.
3. App gọi `MovieGenreModelTrainer.Train(...)`.
4. Trainer đọc CSV, validate header/null/duplicate.
5. Trainer build ML.NET pipeline:
   - đổi label text sang key nội bộ
   - one-hot encode 4 feature text
   - concat thành cột `Features`
   - train multiclass classifier
   - đổi predicted key về lại genre code
6. Model cuối cùng được train trên toàn bộ CSV.
7. Model được lưu thành:

```text
ML/artifacts/movie_genre_model.zip
```

File zip này là artifact runtime. Khi chạy web bình thường, app load file này để predict.

## 4. Chạy web app

Lệnh chạy:

```powershell
dotnet run --project Cinema-Management\Cinema-Management.csproj
```

Luồng thực thi:

1. `Program.cs` chạy như web app bình thường.
2. Đăng ký `MlNetGenreRecommendationService` dạng singleton.
3. Đăng ký `ChatbotService` dạng scoped.
4. User mở web và chọn dropdown trong widget AI.
5. JavaScript trong `wwwroot/js/site.js` gửi request:

```http
POST /api/chatbot/recommend
```

Body ví dụ:

```json
{
  "mood": "so_hai",
  "companion": "ban_be",
  "intensity": "gay_can",
  "ageRating": "T18"
}
```

## 5. Luồng xử lý request recommend

Request đi qua các file theo thứ tự:

1. `Views/Shared/_ChatbotWidget.cshtml`
   - Render giao diện select form.

2. `wwwroot/js/site.js`
   - Lấy giá trị từ form.
   - Gọi `/api/chatbot/recommend`.
   - Hiển thị câu trả lời ngắn và danh sách phim.

3. `Controllers/ChatbotController.cs`
   - Nhận request.
   - Validate 4 lựa chọn không được rỗng.
   - Gọi `IChatbotService.RecommendAsync(...)`.

4. `Services/Chatbot/ChatbotService.cs`
   - Load phim từ SQL Server bằng EF Core.
   - Gọi `IGenreRecommendationService.Predict(...)`.
   - Lọc phim bắt buộc phải có genre khớp với genre dự đoán.
   - Chấm điểm/rank phim.
   - Trả response về UI.

5. `Services/Recommendation/MlNetGenreRecommendationService.cs`
   - Predict genre.
   - Ưu tiên lookup đúng dòng CSV nếu tổ hợp select đã có.
   - Nếu chưa có, tìm dòng training gần nhất.
   - Nếu vẫn không có, fallback sang model `.zip`.

## 6. Vì sao có cả CSV lookup và model zip?

Vì dataset hiện tại nhỏ. Nếu chỉ dùng model xác suất, nhiều tổ hợp select có thể bị predict giống nhau.

Do đó runtime dùng thứ tự:

```text
Exact CSV lookup -> nearest CSV row -> ML.NET model zip
```

## 7. Lưu ý về database

Recommendation chỉ tốt bằng dữ liệu trong DB.

Ví dụ:

- Genre `Kinh dị` hiện chỉ có `Ma Xó`, nên chọn kinh dị chỉ trả về một phim là đúng.
- `Your Name` có genre `Hoạt hình`, nên khi chọn `Hoạt hình`, nó vẫn có thể xuất hiện sau `Doraemon`, `Toy Story`, `Ponyo`.
- Nếu model dự đoán `Trinh thám` nhưng DB chưa có phim nào thuộc genre này, app sẽ không có phim chính xác để trả.

Muốn kết quả đa dạng hơn thì cần thêm phim/genre trong seed DB.

## 8. Khi sửa CSV thì cần làm gì?

Sau khi sửa `ML/ml_recommendation_train.csv`, chạy lại:

```powershell
dotnet run --project Cinema-Management\Cinema-Management.csproj -- --train-recommendation-model
```

Sau đó restart web app:

```powershell
dotnet run --project Cinema-Management\Cinema-Management.csproj
```

## 8. Lần train gần nhất
Rows: `96`; Labels: `8`
MicroAccuracy: `0.636`; MacroAccuracy: `0.583`; LogLoss: `1.219`