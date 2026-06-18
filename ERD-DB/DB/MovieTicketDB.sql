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

/* ==========================================
1. ĐỊNH NGHĨA CÁC BẢNG DANH MỤC CƠ BẢN
==========================================*/
create table Genres 
(
	GenreID int identity(1,1) primary key,
	GenreName nvarchar(100) not null
);
go

create table Countries 
(
	CountryID int identity(1,1) primary key,
	CountryCode varchar(10) unique not null,
	CountryName nvarchar(100) not null
);
go
-- Ghi chú cho Countries
exec sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'Mã quốc gia theo chuẩn ISO (Ví dụ: VN, US, KR, JP)',
    @level0type = 'SCHEMA', @level0name = 'dbo', 
    @level1type = 'TABLE',  @level1name = 'Countries', 
    @level2type = 'COLUMN', @level2name = 'CountryCode';
go

create table Languages
(
    LanguageID int identity(1,1) primary key,
    LanguageCode varchar(10) unique not null,
    LanguageName nvarchar(100) not null
);
go
-- Ghi chú cho Languages
exec sys.sp_addextendedproperty 
    @name = N'MS_Description', @value = N'Mã ngôn ngữ theo chuẩn ISO (Ví dụ: vi, en, ko, ja)',
    @level0type = 'SCHEMA', @level0name = 'dbo', 
    @level1type = 'TABLE',  @level1name = 'Languages', 
    @level2type = 'COLUMN', @level2name = 'LanguageCode';
go

create table Persons
(
    PersonID int identity(1,1) primary key,
    FullName nvarchar (200) not null,
    AvatarURL varchar(1000),
    Biography nvarchar(max)
);
go


/* ==========================================
2. THÔNG TIN PHIM CHI TIẾT
==========================================*/
create table Movies 
(
    MovieID int identity(1,1) primary key,
    CountryID int,
    LanguageID int,
    Title nvarchar(100) not null,
    [Description] nvarchar(max),
    Duration int not null,
    ReleaseDate date,
    PosterURL varchar(1000),
    TrailerURL varchar(1000),
    AgeRating varchar(3) not null default 'P' check (AgeRating in ('P', 'K', 'T13', 'T16', 'T18', 'C')),
    IsActive bit not null default 1, 

    constraint FK_Movies_Countries foreign key (CountryID) references Countries(CountryID),
    constraint FK_Movies_Languages foreign key (LanguageID) references Languages(LanguageID)
);
go
-- Ghi chú cho Movies
exec sys.sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Thời lượng phim (phút)', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE', @level1name = N'Movies', 
    @level2type = N'COLUMN', @level2name = N'Duration';
exec sys.sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Ngày khởi chiếu', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE', @level1name = N'Movies', 
    @level2type = N'COLUMN', @level2name = N'ReleaseDate';
exec sys.sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Độ tuổi kiểm duyệt', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE', @level1name = N'Movies', 
    @level2type = N'COLUMN', @level2name = N'AgeRating';
exec sys.sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Cờ xóa mềm (Soft delete)', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE', @level1name = N'Movies', 
    @level2type = N'COLUMN', @level2name = N'IsActive';
go


/* ==========================================
3. THÔNG TIN RẠP & PHÒNG CHIẾU
==========================================*/
create table Theaters (
  TheaterID int identity(1,1) primary key,
  Title nvarchar(150) not null,
  [Location] nvarchar(max) not null,
  Hotline VARCHAR(20)
);
go

create table Rooms (
  RoomID int identity(1,1) primary key,
  TheaterID int not null,
  RoomName varchar(50) not null,
  RoomType varchar(20) default 'Standard' check (RoomType in ('Standard', 'IMAX', 'FourDX', 'ScreenX', 'VIP')),
  TotalSeats int not null,
  
  constraint FK_Rooms_Theaters foreign key (TheaterID) references Theaters(TheaterID)
);
go
-- Ghi chú cho Rooms
exec sys.sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Một phòng thuộc 1 rạp', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE', @level1name = N'Rooms', 
    @level2type = N'COLUMN', @level2name = N'TheaterID';
exec sys.sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Phòng này thuộc loại gì? Có công nghệ gì?', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE', @level1name = N'Rooms', 
    @level2type = N'COLUMN', @level2name = N'RoomType';
go

create table SeatTypePricing
(
    SeatType varchar(20) primary key check (SeatType in ('Standard', 'VIP', 'Sweetbox')),
    Multiplier decimal(10,2) not null
);
go

create table Seats 
(
    SeatID int identity(1,1) primary key,
    RoomID int not null,
    RowName varchar(4) not null,
    SeatNumber int not null,
    SeatType varchar(20) not null default 'Standard' check (SeatType in ('Standard', 'VIP', 'Sweetbox')),

    constraint FK_Seats_Rooms foreign key (RoomID) references Rooms(RoomID),
    constraint FK_Seats_SeatTypePricin foreign key (SeatType) references SeatTypePricing(SeatType),
    constraint UQ_Room_Row_Seat unique (RoomID, RowName, SeatNumber)
);
go
-- Ghi chú cho Seats
exec sys.sp_addextendedproperty 
    @name = N'MS_Description', @value = N'Ghế thuộc phòng nào', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE', @level1name = N'Seats', 
    @level2type = N'COLUMN', @level2name = N'RoomID';
exec sys.sp_addextendedproperty 
    @name = N'MS_Description', @value = N'Hàng ghế: A, B, C...', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE', @level1name = N'Seats', 
    @level2type = N'COLUMN', @level2name = N'RowName';
exec sys.sp_addextendedproperty 
    @name = N'MS_Description', @value = N'Số ghế: 1, 2, 3...', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE', @level1name = N'Seats', 
    @level2type = N'COLUMN', @level2name = N'SeatNumber';
exec sys.sp_addextendedproperty 
    @name = N'MS_Description', @value = N'Phân loại ghế ngồi (Standard, VIP, Sweetbox)', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE', @level1name = N'Seats', 
    @level2type = N'COLUMN', @level2name = N'SeatType';
go

/*==========================================
4. NGƯỜI DÙNG & LOOKUP THANH TOÁN
==========================================*/
create table Users 
(
  UserID int identity(1,1) primary key,
  FullName nvarchar(100) not null,
  Email varchar(100) unique not null,
  Phone varchar(15) unique,
  PasswordHash varchar(255) not null,
  [Role] varchar(8) not null default 'Customer' check ([Role] in ('Customer', 'Staff', 'Admin')),
  CreatedAt datetime default getdate(),
  IsActive bit not null default 1
);
go

create table PaymentMethods 
(
  MethodID int identity(1,1) primary key,
  MethodCode varchar(50) unique not null,
  MethodName nvarchar(100) not null,
  IsActive bit not null default 1
);
go
-- Ghi chú cho PaymentMethods 
exec sys.sp_addextendedproperty 
    @name = N'MS_Description', @value = N'Có thể là: MOMO, VNPAY, TECHCOMBANK, ...', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE', @level1name = N'PaymentMethods', 
    @level2type = N'COLUMN', @level2name = N'MethodName';
go


/*==========================================
5. SUẤT CHIẾU & ĐẶT VÉ (BOOKING)
==========================================*/
create table Showtimes
(
  ShowtimeID int identity(1,1) primary key,
  MovieID int not null,
  RoomID int not null,
  StartTime datetime not null,
  EndTime datetime not null,
  ScreenFormat varchar(6) not null default 'TwoD' check (ScreenFormat in ('TwoD','ThreeD')),
  MovieFormat varchar(3) not null default 'SUB' check (MovieFormat in ('SUB','DB', 'VO', 'NOS')),
  AudioLanguageID int,
  SubtitleLanguageID int,
  TicketPrice decimal(10,2) not null,
  [Status] varchar(20) not null default 'Scheduled' check ([Status] in ('Scheduled', 'Cancelled', 'Finished')),
  
  constraint FK_Showtimes_Movies foreign key (MovieID) references Movies(MovieID),
  constraint FK_Showtimes_Rooms foreign key (RoomID) references Rooms(RoomID),
  constraint FK_Showtimes_AudioLanguages foreign key (AudioLanguageID) references Languages(LanguageID),
  constraint FK_Showtimes_SubtitleLanguages foreign key (SubtitleLanguageID) references Languages(LanguageID),
  constraint CK_Showtimes_Time check (EndTime > StartTime)
);
go
-- Ghi chú cho Showtimes 
exec sys.sp_addextendedproperty 
    @name = N'MS_Description', @value = N'Giờ bắt đầu chiếu', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE', @level1name = N'Showtimes', 
    @level2type = N'COLUMN', @level2name = N'StartTime';
exec sys.sp_addextendedproperty 
    @name = N'MS_Description', @value = N'Giờ kết thúc chiếu', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE', @level1name = N'Showtimes', 
    @level2type = N'COLUMN', @level2name = N'EndTime';
exec sys.sp_addextendedproperty 
    @name = N'MS_Description', @value = N'Định dạng màn hình (2D, 3D)', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE', @level1name = N'Showtimes', 
    @level2type = N'COLUMN', @level2name = N'ScreenFormat';
exec sys.sp_addextendedproperty 
    @name = N'MS_Description', @value = N'Định dạng phim (SUB, DB, ...)', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE', @level1name = N'Showtimes', 
    @level2type = N'COLUMN', @level2name = N'MovieFormat';
exec sys.sp_addextendedproperty 
    @name = N'MS_Description', @value = N'Ngôn ngữ âm thanh (Thuyết minh tiếng việt, ...)', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE', @level1name = N'Showtimes', 
    @level2type = N'COLUMN', @level2name = N'AudioLanguageID';
exec sys.sp_addextendedproperty 
    @name = N'MS_Description', @value = N'Ngôn ngữ phụ đề', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE', @level1name = N'Showtimes', 
    @level2type = N'COLUMN', @level2name = N'SubtitleLanguageID';
exec sys.sp_addextendedproperty 
    @name = N'MS_Description', @value = N'Suất chiếu đã được tạo, còn hiệu lực / suất chiếu bị huỷ / suất chiếu đã chiếu xong', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE', @level1name = N'Showtimes', 
    @level2type = N'COLUMN', @level2name = N'Status';
go

/* 1 ghế được xem là KHÔNG còn trống nếu:
- đã có Ticket chưa bị Cancelled/Refunded
- hoặc có SeatHold Active và ExpiresAt > getdate()
*/
create table SeatHolds
(
    HoldID int identity(1,1) primary key,
    UserID int not null,
    ShowtimeID int not null,
    SeatID int not null,
    HoldToken varchar(100) unique not null,
    ExpiresAt datetime not null,
    CreatedAt datetime not null default getdate(),
    [Status] varchar(10) not null default 'Active'
        check ([Status] in ('Active', 'Expired', 'Cancelled', 'Converted')),

    constraint FK_SeatHolds_Users foreign key (UserID) references Users(UserID),
    constraint FK_SeatHolds_Showtimes foreign key (ShowtimeID) references Showtimes(ShowtimeID),
    constraint FK_SeatHolds_Seats foreign key (SeatID) references Seats(SeatID)
);
go

create table Bookings
(
  BookingID int identity(1,1) primary key,
  UserID int,
  BookingDate datetime default getdate(),
  TotalAmount decimal(10,2) not null,
  [Status] varchar(10) default 'Pending' 
      check ([Status] in ('Pending', 'Completed', 'Failed', 'Expired', 'Cancelled')),

  constraint FK_Bookings_Users foreign key (UserID) references Users(UserID)
);
go

create table Payments
(
    PaymentID int identity(1,1) primary key,
    BookingID int not null,
    MethodID int not null,
    Amount decimal(10,2) not null,
    ProviderTransactionID varchar(100),
    [Status] varchar(20) not null default 'Pending' check ([Status] in ('Pending', 'Success', 'Failed', 'Cancelled', 'Refunded')),
    CreatedAt datetime not null default getdate(),
    PaidAt datetime,
    RawResponse nvarchar(max),

    constraint FK_Payments_Bookings foreign key (BookingID) references Bookings(BookingID),
    constraint FK_Payments_Methods foreign key (MethodID) references PaymentMethods(MethodID)
);
go

create table Tickets
(
  TicketID int identity(1,1) primary key,
  BookingID int not null,
  ShowtimeID int not null,
  SeatID int not null,
  FinalPrice decimal(10,2) not null,
  [Status] varchar(10) default 'Booked' check ([Status] in ('Booked', 'Cancelled', 'Refunded')),
  CreatedAt datetime default getdate(),
  TicketCode varchar(50) unique not null,
  QRCodeURL varchar(1000),
  IssuedAt datetime,

  constraint FK_Tickets_Bookings foreign key (BookingID) references Bookings(BookingID),
  constraint FK_Tickets_Showtimes foreign key (ShowtimeID) references Showtimes(ShowtimeID),
  constraint FK_Tickets_Seats foreign key (SeatID) references Seats(SeatID)
);
go
-- Ghi chú cho Tickets
exec sys.sp_addextendedproperty 
    @name = N'MS_Description', @value = N'Giá thực tế tại thời điểm mua vé', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE', @level1name = N'Tickets', 
    @level2type = N'COLUMN', @level2name = N'FinalPrice';
go


/* ==========================================
6. COMBO
==========================================*/
create table Combos
(
    ComboID int identity(1,1) primary key,
    ComboName nvarchar(100) not null,
    [Description] nvarchar(max),
    Price decimal(10,2) not null,
    ImageURL varchar(1000),
    IsActive bit not null default 1
);
go

create table BookingCombos
(
    BookingComboID int identity(1,1) primary key,
    BookingID int not null,
    ComboID int not null,
    Quantity int not null check (Quantity > 0),
    UnitPrice decimal(10,2) not null,

    constraint FK_BookingCombos_Bookings foreign key (BookingID) references Bookings(BookingID),
    constraint FK_BookingCombos_Combos foreign key (ComboID) references Combos(ComboID)
);
go


/* ==========================================
7. CÁC BẢNG TRUNG GIAN QUẢN LÝ QUAN HỆ (N-N)
==========================================*/
create table Movie_Genre
(
    MovieID int not null,
    GenreID int not null,

    constraint PK_Movie_Genre primary key (MovieID, GenreID),
    constraint FK_Movie_Genre_Movies foreign key (MovieID) references Movies(MovieID),
    constraint FK_Movie_Genre_Genres foreign key (GenreID) references Genres(GenreID)
);
go

create table Movie_Theater
(
    MovieID int not null,
    TheaterID int not null,

    constraint PK_Movie_Theater primary key (MovieID, TheaterID),
    constraint FK_Movie_Theater_Movies foreign key (MovieID) references Movies(MovieID),
    constraint FK_Movie_Theater_Theaters foreign key (TheaterID) references Theaters(TheaterID)
);
go

create table Movie_Directors
(
    MovieID int not null,
    PersonID int not null,

    constraint PK_Movie_Directors primary key (MovieID, PersonID),
    constraint FK_Movie_Directors_Movies foreign key (MovieID) references Movies(MovieID),
    constraint FK_Movie_Directors_Persons foreign key (PersonID) references Persons(PersonID)
);
go

create table Movie_Cast
(
    MovieID int not null,
    PersonID int not null,
    CharacterName nvarchar(100),

    constraint PK_Movie_Cast primary key (MovieID, PersonID),
    constraint FK_Movie_Cast_Movies foreign key (MovieID) references Movies(MovieID),
    constraint FK_Movie_Cast_Persons foreign key (PersonID) references Persons(PersonID)
);
go


/* ==========================================
8. CREATE INDEX
==========================================*/
create unique index UX_Tickets_ActiveSeat
on Tickets(ShowtimeID, SeatID)
where [Status] = 'Booked';
go

create index IX_SeatHolds_Showtime_Seat_Status_ExpiresAt
on SeatHolds(ShowtimeID, SeatID, [Status], ExpiresAt);
go

create index IX_Showtimes_Movie_StartTime
on Showtimes(MovieID, StartTime);
go

create index IX_Showtimes_Room_StartTime
on Showtimes(RoomID, StartTime);
go

create index IX_Rooms_Theater
on Rooms(TheaterID);
go