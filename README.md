# 🎬 Cosmos Cinema Management System

Cosmos Cinema là hệ thống quản lý và đặt vé rạp chiếu phim được phát triển bằng **ASP.NET Core MVC**, **C#**, **Razor View** và **SQL Server**.

Hệ thống hỗ trợ toàn bộ quy trình đặt vé trực tuyến, từ xem thông tin phim, lựa chọn suất chiếu, ghế ngồi, đồ ăn/uống đến thanh toán. Ngoài ra, dự án còn cung cấp các chức năng quản lý dành cho nhân viên và quản trị viên.

## ✨ Chức năng chính

### Khách hàng

- Đăng ký và đăng nhập tài khoản.
- Xem danh sách phim đang chiếu.
- Xem thông tin chi tiết, thể loại, diễn viên và đạo diễn.
- Xem lịch chiếu theo ngày.
- Chọn suất chiếu và phòng chiếu.
- Chọn ghế thường, VIP hoặc ghế đôi.
- Kiểm tra trạng thái ghế đã đặt.
- Chọn combo đồ ăn và thức uống.
- Thanh toán bằng mã QR thông qua SePay/VietQR.
- Xem thông tin vé sau khi thanh toán.
- Xem lịch sử đặt vé trong trang cá nhân.
- Đánh giá và nhận xét phim.
- Nhận gợi ý phim thông qua chatbot và mô hình ML.NET.

### Nhân viên

- Quản lý lịch chiếu phim.
- Quản lý phòng chiếu và sơ đồ ghế.
- Theo dõi trạng thái ghế của từng suất chiếu.
- Quản lý combo đồ ăn và thức uống.
- Theo dõi thông tin đặt vé tại rạp.

### Quản trị viên

- Quản lý tài khoản nhân viên.
- Quản lý phim và dữ liệu liên quan.
- Theo dõi số liệu và thống kê hoạt động.
- Quản lý các chức năng vận hành của hệ thống.

## 🛠 Công nghệ sử dụng

### Front-end

- Razor View (`.cshtml`)
- HTML5
- CSS3
- JavaScript
- Font Awesome
- Responsive Web Design

### Back-end

- C#
- ASP.NET Core MVC
- .NET 8
- Entity Framework Core
- LINQ
- Session và Cookie Authentication

### Cơ sở dữ liệu

- Microsoft SQL Server
- Entity Framework Core Migrations
- SQL Script

### Công nghệ khác

- SePay Webhook
- VietQR
- ML.NET
- Git và GitHub
- GitHub Actions

## 🏗 Kiến trúc dự án

Dự án được xây dựng theo mô hình **MVC – Model View Controller**:

- **Model:** định nghĩa dữ liệu và các đối tượng nghiệp vụ.
- **View:** giao diện Razor hiển thị dữ liệu cho người dùng.
- **Controller:** tiếp nhận request, xử lý nghiệp vụ và trả dữ liệu về View.
- **Entity Framework Core:** truy vấn và cập nhật dữ liệu trong SQL Server.
- **Service:** xử lý các nghiệp vụ riêng như chatbot, đề xuất phim và thanh toán.

## 📁 Cấu trúc thư mục

```text
CINEMA-MANAGEMNET-SYSTEM/
├── Cinema-Management/
│   ├── Controllers/          # Controller xử lý request và nghiệp vụ
│   ├── Data/                 # ApplicationDbContext
│   ├── Migrations/           # Entity Framework Core Migrations
│   ├── Models/               # Entity và ViewModel
│   ├── Services/             # Chatbot, recommendation, payment...
│   ├── ViewModels/           # Dữ liệu dành cho giao diện
│   ├── Views/                # Razor Views (.cshtml)
│   ├── wwwroot/
│   │   ├── css/              # Stylesheet
│   │   ├── js/               # JavaScript
│   │   └── img/              # Hình ảnh và poster
│   ├── Program.cs            # Cấu hình và khởi động ứng dụng
│   └── appsettings.json      # Cấu hình ứng dụng
├── Database/                 # Các SQL script khởi tạo dữ liệu
├── ML/                       # Dữ liệu và mô hình ML.NET
└── README.md
```

## 🔄 Quy trình đặt vé

```text
Chọn phim
    ↓
Chọn ngày và suất chiếu
    ↓
Chọn ghế ngồi
    ↓
Chọn đồ ăn/uống
    ↓
Kiểm tra thông tin đơn hàng
    ↓
Thanh toán QR
    ↓
Nhận vé và mã xác nhận
```

## 🚀 Cài đặt và chạy dự án

### Yêu cầu

Trước khi chạy dự án, hãy cài đặt:

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Microsoft SQL Server
- SQL Server Management Studio hoặc Azure Data Studio
- Git
- Visual Studio 2022, Visual Studio Code hoặc JetBrains Rider

### 1. Clone repository

```bash
git clone https://github.com/Chisato012/CINEMA-MANAGEMNET-SYSTEM.git
cd CINEMA-MANAGEMNET-SYSTEM
```

### 2. Cấu hình kết nối SQL Server

Mở file:

```text
Cinema-Management/appsettings.json
```

Cập nhật connection string phù hợp với SQL Server trên máy:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SQL_SERVER;Database=MovieTicketDB;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

Không đưa mật khẩu, API key hoặc thông tin bí mật lên GitHub. Nên sử dụng biến môi trường hoặc .NET User Secrets cho môi trường phát triển.

### 3. Khởi tạo cơ sở dữ liệu

Có thể sử dụng Entity Framework Core:

```bash
cd Cinema-Management
dotnet restore
dotnet ef database update
```

Nếu chưa cài đặt Entity Framework CLI:

```bash
dotnet tool install --global dotnet-ef
```

Ngoài ra, có thể chạy các SQL script trong thư mục `Database` bằng SQL Server Management Studio.

### 4. Web

```text
cosmos.qsoc.cloud
```

## 🤖 Hệ thống gợi ý phim

Dự án sử dụng **ML.NET** để xây dựng chức năng đề xuất phim theo thể loại và thông tin phim.

Các file liên quan được đặt trong thư mục:

```text
ML/
Cinema-Management/Services/Recommendation/
Cinema-Management/Models/Recommendation/
```

Chatbot sử dụng dữ liệu phim trong hệ thống để hỗ trợ người dùng tìm kiếm và lựa chọn phim phù hợp.

## 💳 Thanh toán

Hệ thống hỗ trợ thanh toán QR thông qua:

- SePay
- VietQR
- Webhook xác nhận giao dịch

Thông tin đơn hàng và giá tiền được kiểm tra lại ở phía server nhằm hạn chế việc chỉnh sửa dữ liệu từ trình duyệt.

## 🔐 Bảo mật

Dự án áp dụng một số biện pháp bảo mật:

- Cookie Authentication.
- Phân quyền khách hàng, nhân viên và quản trị viên.
- Anti-forgery token cho các form gửi dữ liệu.
- Kiểm tra dữ liệu ở phía server.
- Không tin tưởng giá tiền gửi từ trình duyệt.
- Kiểm tra trạng thái ghế trước khi tạo vé.
- Kiểm tra tồn kho combo khi thanh toán.
- Xác minh webhook thanh toán.

## 🌱 Hướng phát triển

- Gửi vé điện tử qua email.
- Hỗ trợ nhiều cụm rạp.
- Tích hợp thêm phương thức thanh toán.
- Hiển thị sơ đồ ghế theo thời gian thực.
- Cải thiện mô hình đề xuất phim.
- Xây dựng ứng dụng dành cho thiết bị di động.
- Bổ sung dashboard thống kê nâng cao.
- Triển khai hệ thống bằng Docker.

## 👥 Đối tượng sử dụng

- Khách hàng đặt vé trực tuyến.
- Nhân viên vận hành rạp phim.
- Quản trị viên hệ thống.

## 🎯 Mục tiêu dự án

Cosmos Cinema được xây dựng nhằm áp dụng các kiến thức về:

- Lập trình C# và ASP.NET Core MVC.
- Thiết kế giao diện bằng Razor View.
- Xây dựng và quản lý cơ sở dữ liệu SQL Server.
- Entity Framework Core và LINQ.
- Xử lý quy trình đặt vé và thanh toán.
- Phân quyền người dùng.
- Tích hợp webhook và machine learning vào ứng dụng web.

---

> Cosmos Cinema Management System — nền tảng quản lý và đặt vé rạp chiếu phim được xây dựng bằng ASP.NET Core MVC.

