if not exists (select * from sys.databases where name = 'MovieTicketDB')
begin
    create database MovieTicketDB;
    print N'Đã tạo mới database';
end
else
begin
    print N'Database đã tồn tại, skip tạo database';
end
go

use MovieTicketDB;
go

EXEC sys.sp_executesql N'USE MovieTicketDB';
go

create table Genres (
  GenreID  int          primary key identity(1, 1),
  Name     nvarchar(100) NOT NULL
)
go

create table Countries (
  CountryID   int          primary key identity(1, 1),
  CountryName nvarchar(100) NOT NULL
)
go

create table Languages (
  LanguageID   int          primary key identity(1, 1),
  LanguageName nvarchar(100) NOT NULL
)
go

create table Persons (
  PersonID int          primary key identity(1, 1),
  FullName nvarchar(150) NOT NULL
)
go

-- ==========================================
-- 2. PHIM
-- ==========================================

create table Movies (
  MovieID     int           primary key identity(1, 1),
  Title      nvarchar(200) NOT NULL,
  ReleaseDate datetime      NOT NULL,
  AgeRating   nvarchar(10)  default '--',
  Duration    smallint      NOT NULL,
  Synopsis    nvarchar(1000) default N'Chưa có tóm tắt',
  PosterURL   varchar(1000) NOT NULL,
  Trailer     varchar(1000) NOT NULL,
  CountryID   int,
  LanguageID  int,

CONSTRAINT FK_Movies_Countries FOREIGN KEY (CountryID) REFERENCES Countries (CountryID),
CONSTRAINT FK_Movies_Languages FOREIGN KEY (LanguageID) REFERENCES Languages (LanguageID)
)
go

create table MovieGenres (
  MovieID int,
  GenreID int,
  primary key (MovieID, GenreID),

CONSTRAINT FK_MovieGenres_Movies FOREIGN KEY (MovieID) REFERENCES [Movies] (MovieID),
CONSTRAINT FK_MovieGenres_Genres FOREIGN KEY (GenreID) REFERENCES [Genres] (GenreID)
)
go

create table MovieDirectors (
  MovieID  int,
  PersonID int,
  primary key (MovieID, PersonID),

CONSTRAINT FK_MovieDirectors_Movies FOREIGN KEY (MovieID) REFERENCES Movies (MovieID),
CONSTRAINT FK_MovieDirectors_Persons FOREIGN KEY (PersonID) REFERENCES Persons (PersonID)
)
go

create table MovieCasts (
  MovieID       int,
  PersonID      int,
  CharacterName nvarchar(150),
  primary key (MovieID, PersonID),

CONSTRAINT FK_MovieCasts_Movies FOREIGN KEY (MovieID) REFERENCES Movies (MovieID),
CONSTRAINT FK_MovieCasts_Persons FOREIGN KEY (PersonID) REFERENCES Persons (PersonID)
)
go

-- ==========================================
-- 3. RẠP — PHÒNG — GHẾ
-- ==========================================

create table Rooms (
  RoomID   int PRIMARY KEY IDENTITY(1, 1),
  RoomName nvarchar(100) NOT NULL
)
GO

create table SeatTypePricing (
  SeatType   varchar(10) NOT NULL PRIMARY KEY,
  Multiplier decimal(4,2) NOT NULL DEFAULT (1.0),
  CONSTRAINT CK_SeatTypePricing_SeatType CHECK (SeatType IN ('Regular', 'VIP', 'Couple'))
)
GO

create table Seats (
  SeatID   int          PRIMARY KEY IDENTITY(1, 1),
  RoomID   int          NOT NULL,
  SeatCode nvarchar(10) NOT NULL,
  SeatType varchar(10)  NOT NULL,
  SeatStatus varchar(50) NOT NULL,
CONSTRAINT CK_Seats_SeatType CHECK (SeatType IN ('Regular', 'VIP', 'Couple')),
CONSTRAINT CK_Seats_SeatType CHECK (SeatStatus IN ('Order', 'Not Order', 'Orderring')),
CONSTRAINT FK_Seats_Rooms FOREIGN KEY (RoomID) REFERENCES Rooms (RoomID),
CONSTRAINT FK_Seats_SeatTypePricing FOREIGN KEY (SeatType) REFERENCES SeatTypePricing (SeatType)
)
GO

-- ==========================================
-- 4. SUẤT CHIẾU
-- ==========================================
create table Showtimes (
  ShowtimeID int          PRIMARY KEY IDENTITY(1, 1),
  MovieID    int          NOT NULL,
  RoomID     int          NOT NULL,
  [Date]     date         NOT NULL,
  StartTime  datetime     NOT NULL,
  EndTime    datetime     NOT NULL,
  BasePrice  decimal(10,2) NOT NULL,

CONSTRAINT FK_Showtimes_Movies FOREIGN KEY (MovieID) REFERENCES Movies (MovieID),
CONSTRAINT FK_Showtimes_Rooms FOREIGN KEY (RoomID) REFERENCES Rooms (RoomID)
)
GO

-- ==========================================
-- 5. NGƯỜI DÙNG
-- ==========================================
create table Users (
  UserID       int          PRIMARY KEY IDENTITY(1, 1),
  FullName    nvarchar(150) NOT NULL,
  Email        varchar(200)  UNIQUE NOT NULL,
  PasswordHash varchar(512)  NOT NULL,
  PhoneNumber  varchar(15),
  DOB          date,
  Status       bit           DEFAULT (1),
  [Role]       nvarchar(20)  NOT NULL DEFAULT N'KhachHang',

CONSTRAINT CK_Users_Role CHECK ([Role] IN ('Admin', 'Staff', 'KhachHang'))
)
GO

-- ==========================================
-- 6. ĐẶT VÉ
-- ==========================================
create table Bookings (
  BookingID   int           PRIMARY KEY IDENTITY(1, 1),
  UserID      int           NOT NULL,
  BookingDate datetime      NOT NULL,
  TotalAmount decimal(10,2) NOT NULL,
  Status      nvarchar(10)  NOT NULL DEFAULT 'Pending',

CONSTRAINT CK_Bookings_Status CHECK (Status IN ('Pending', 'Confirmed', 'Cancelled')),
CONSTRAINT FK_Bookings_Users FOREIGN KEY (UserID) REFERENCES Users (UserID)
)
GO

create table Tickets (
  TicketID   int         PRIMARY KEY IDENTITY(1, 1),
  BookingID  int         NOT NULL,
  ShowtimeID int         NOT NULL,
  SeatID     int         NOT NULL,
  TicketCode varchar(100) UNIQUE NOT NULL,

CONSTRAINT FK_Tickets_Bookings FOREIGN KEY (BookingID) REFERENCES Bookings (BookingID),
CONSTRAINT FK_Tickets_Showtimes FOREIGN KEY (ShowtimeID) REFERENCES Showtimes (ShowtimeID),
CONSTRAINT FK_Tickets_Seats FOREIGN KEY (SeatID) REFERENCES Seats (SeatID)
)
GO

-- ==========================================
-- 7. COMBO BẮP NƯỚC
-- ==========================================

create table Combos (
  ComboID    int           PRIMARY KEY IDENTITY(1, 1),
  ComboName  nvarchar(150) NOT NULL,
  ComboPrice decimal(10,2) NOT NULL
)
GO

create table BookingCombos (
  BookingID int,
  ComboID   int,
  Quantity  int           NOT NULL DEFAULT (1),
  UnitPrice decimal(10,2) NOT NULL,
  PRIMARY KEY (BookingID, ComboID),

CONSTRAINT FK_BookingCombos_Bookings FOREIGN KEY (BookingID) REFERENCES Bookings (BookingID),
CONSTRAINT FK_BookingCombos_Combos FOREIGN KEY (ComboID) REFERENCES Combos (ComboID)
)
GO

-- ==========================================
-- 8. THANH TOÁN
-- ==========================================

create table PaymentMethods (
  MethodID   int          PRIMARY KEY IDENTITY(1, 1),
  MethodName nvarchar(100) NOT NULL
)
GO

create table Payments (
  PaymentID   int           PRIMARY KEY IDENTITY(1, 1),
  BookingID   int           NOT NULL,
  MethodID    int           NOT NULL,
  Amount      decimal(10,2) NOT NULL,
  PaymentDate datetime      NOT NULL,
  Status      nvarchar(10)  NOT NULL DEFAULT 'Pending',

CONSTRAINT CK_Payments_Status CHECK (Status IN ('Pending', 'Success', 'Failed')),
CONSTRAINT FK_Payments_Bookings FOREIGN KEY (BookingID) REFERENCES Bookings (BookingID),
CONSTRAINT FK_Payments_PaymentMethods FOREIGN KEY (MethodID) REFERENCES PaymentMethods (MethodID)
)
GO

-- ==========================================
-- 9. INDEX
-- ==========================================
CREATE UNIQUE INDEX UQ_Tickets_Showtime_Seat
  ON Tickets (ShowtimeID, SeatID)
GO

-- ==========================================
-- 10. EXTENDED PROPERTIES (MÔ TẢ CỘT)
-- ==========================================

EXEC sp_addextendedproperty
  @name      = N'Column_Description',
  @value     = N'Thời lượng phim tính bằng số phút',
  @level0type = N'Schema', @level0name = N'dbo',
  @level1type = N'Table',  @level1name = N'Movies',
  @level2type = N'Column', @level2name = N'Duration'
GO

EXEC sp_addextendedproperty
  @name      = N'Column_Description',
  @value     = N'Nước sản xuất gốc',
  @level0type = N'Schema', @level0name = N'dbo',
  @level1type = N'Table',  @level1name = N'Movies',
  @level2type = N'Column', @level2name = N'CountryID'
GO

EXEC sp_addextendedproperty
  @name      = N'Column_Description',
  @value     = N'Ngôn ngữ gốc',
  @level0type = N'Schema', @level0name = N'dbo',
  @level1type = N'Table',  @level1name = N'Movies',
  @level2type = N'Column', @level2name = N'LanguageID'
GO

EXEC sp_addextendedproperty
  @name      = N'Column_Description',
  @value     = N'Mã ghế, ví dụ: A1, F05',
  @level0type = N'Schema', @level0name = N'dbo',
  @level1type = N'Table',  @level1name = N'Seats',
  @level2type = N'Column', @level2name = N'SeatCode'
GO

EXEC sp_addextendedproperty
  @name      = N'Column_Description',
  @value     = N'Giá vé cơ bản, nhân với Multiplier ra giá thực',
  @level0type = N'Schema', @level0name = N'dbo',
  @level1type = N'Table',  @level1name = N'Showtimes',
  @level2type = N'Column', @level2name = N'BasePrice'
GO

EXEC sp_addextendedproperty
  @name      = N'Column_Description',
  @value     = N'Ngày chiếu, dùng để lọc lịch chiếu theo ngày',
  @level0type = N'Schema', @level0name = N'dbo',
  @level1type = N'Table',  @level1name = N'Showtimes',
  @level2type = N'Column', @level2name = N'Date'
GO

EXEC sp_addextendedproperty
  @name      = N'Column_Description',
  @value     = N'Ngày giờ bắt đầu suất chiếu (bao gồm cả ngày)',
  @level0type = N'Schema', @level0name = N'dbo',
  @level1type = N'Table',  @level1name = N'Showtimes',
  @level2type = N'Column', @level2name = N'StartTime'
GO

EXEC sp_addextendedproperty
  @name      = N'Column_Description',
  @value     = N'Dùng để kiểm tra điều kiện tuổi T13/T18',
  @level0type = N'Schema', @level0name = N'dbo',
  @level1type = N'Table',  @level1name = N'Users',
  @level2type = N'Column', @level2name = N'DOB'
GO

EXEC sp_updateextendedproperty 
    @name = N'Column_Description', 
    @value = N'Kiểm tra điều kiện người dùng > 16 tuổi mới được phép tạo tài khoản / đặt vé', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE',  @level1name = N'Users', 
    @level2type = N'COLUMN', @level2name = N'DOB';
GO

EXEC sp_addextendedproperty
  @name      = N'Column_Description',
  @value     = N'1 = hoạt động, 0 = bị khoá',
  @level0type = N'Schema', @level0name = N'dbo',
  @level1type = N'Table',  @level1name = N'Users',
  @level2type = N'Column', @level2name = N'Status'
GO

EXEC sp_addextendedproperty
  @name      = N'Column_Description',
  @value     = N'Vai trò tài khoản: Admin, Staff hoặc KhachHang',
  @level0type = N'Schema', @level0name = N'dbo',
  @level1type = N'Table',  @level1name = N'Users',
  @level2type = N'Column', @level2name = N'Role'
GO

EXEC sp_addextendedproperty
  @name = N'Column_Description',
  @value     = N'Snapshot giá combo tại thời điểm đặt vé',
  @level0type = N'Schema', @level0name = N'dbo',
  @level1type = N'Table',  @level1name = N'BookingCombos',
  @level2type = N'Column', @level2name = N'UnitPrice'
GO

EXEC sp_addextendedproperty
  @name      = N'Column_Description',
  @value     = N'Ví dụ: VNPay, MoMo, Visa',
  @level0type = N'Schema', @level0name = N'dbo',
  @level1type = N'Table',  @level1name = N'PaymentMethods',
  @level2type = N'Column', @level2name = N'MethodName'
GO
