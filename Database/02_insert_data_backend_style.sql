USE MovieTicketDB;
GO

-- ==========================================
-- CHẠY 2 LẦN
-- RESET DỮ LIỆU DEMO
-- Chạy file 02 sau file 01. Mục đích: xóa sạch data demo cũ rồi nạp lại từ đầu.
-- DELETE theo thứ tự bảng con -> bảng cha để tránh lỗi khóa ngoại.
-- DBCC CHECKIDENT chỉ dùng sau khi đã xóa toàn bộ dữ liệu demo của bảng.
-- ==========================================

DELETE FROM Payments;
DELETE FROM BookingCombos;
DELETE FROM Tickets;
DELETE FROM Bookings;
DELETE FROM Users;

DELETE FROM Showtimes;
DELETE FROM Seats;
DELETE FROM SeatTypePricing;

DELETE FROM PaymentMethods;
DELETE FROM Combos;

DELETE FROM MovieCasts;
DELETE FROM MovieDirectors;
DELETE FROM MovieGenres;

DELETE FROM Rooms;
DELETE FROM Movies;
DELETE FROM Persons;
DELETE FROM Countries;
DELETE FROM Languages;
DELETE FROM Genres;
GO

DBCC CHECKIDENT ('Payments', RESEED, 0);
DBCC CHECKIDENT ('Tickets', RESEED, 0);
DBCC CHECKIDENT ('Bookings', RESEED, 0);
DBCC CHECKIDENT ('Users', RESEED, 0);
DBCC CHECKIDENT ('Showtimes', RESEED, 0);
DBCC CHECKIDENT ('Seats', RESEED, 0);
DBCC CHECKIDENT ('PaymentMethods', RESEED, 0);
DBCC CHECKIDENT ('Combos', RESEED, 0);
DBCC CHECKIDENT ('Rooms', RESEED, 0);
DBCC CHECKIDENT ('Movies', RESEED, 0);
DBCC CHECKIDENT ('Persons', RESEED, 0);
DBCC CHECKIDENT ('Countries', RESEED, 0);
DBCC CHECKIDENT ('Languages', RESEED, 0);
DBCC CHECKIDENT ('Genres', RESEED, 0);
GO

-- Lưu ý kiến trúc:
-- File này chỉ nạp dữ liệu mẫu. Các giá trị như BasePrice, EndTime, TotalAmount, Amount
-- được ghi như snapshot đã được backend tính sẵn trước khi insert.
-- Không đặt business logic tính giá / tính tuổi / tính tổng tiền trong database seed.
GO

--======== DATA CỦA PERSON ===========

INSERT INTO Persons (FullName)
VALUES
-- =================== Doraemon ===================
(N'Wasabi Mizuta'),     -- 1
(N'Megumi Oohara'),     -- 2
(N'Yumi Kakazu'),       -- 3
(N'Subaru Kimura'),     -- 4
(N'Tomokazu Seki'),     -- 5
(N'Tetsuo Yajima'),     -- 6

-- =================== Toys Story ===================
(N'Keanu Reeves'),      -- 7
(N'Tom Hanks'),         -- 8
(N'Annie Potts'),       -- 9
(N'Kenna Harris'),      -- 10
(N'Andrew Stanton'),    -- 11

-- =================== Your Name ===================
(N'Kamiki Ryunosuke'),  -- 12
(N'Kamishiraishi Mone'),-- 13
(N'Narita Ryo'),        -- 14
(N'Shinkai Makoto'),    -- 15

-- =================== Ma Xó ===================
(N'Lê Khánh'),          -- 16
(N'Tín Nguyễn'),        -- 17
(N'Avin Lu'),           -- 18
(N'NSƯT Hạnh Thúy'),    -- 19
(N'Nguyễn Sỹ Hậu'),     -- 20
(N'Gi A Nguyễn'),       -- 21
(N'Leona Khánh Tiên'),  -- 22
(N'Phan Bá Hỷ'),        -- 23

-- =================== Colony ===================
(N'Jun Ji-hyun'),       -- 24
(N'Koo Kyo-hwan'),      -- 25
(N'Ji Chang-wook'),     -- 26
(N'Yeon Sang-ho'),      -- 27

-- =================== Super Girls ===================
(N'Milly Alcock'),          -- 28
(N'Matthias Schoenaerts'),  -- 29
(N'Eve Ridley'),            -- 30
(N'David Krumholtz'),       -- 31
(N'Emily Beecham'),         -- 32
(N'Jason Momoa'),           -- 33
(N'Craig Gillespie'),       -- 34

-- =================== Cô bé Ponyo ===================
(N'MIYAZAKI HAYAO'),    -- 35

-- ======== HE-MAN VÀ NHỮNG CHIẾN BINH VŨ TRỤ ========
(N'Nicholas Galitzine'),            -- 36
(N'Camila Mendes'),                 -- 37
(N'Alison Brie'),                   -- 38
(N'James Purefoy'),                 -- 39
(N'Morena Baccarin'),               -- 40
(N'Jóhannes Haukur Jóhannesson'),   -- 41
(N'Travis Knight');                 -- 42
GO


-- ==========================================
-- NẠP DỮ LIỆU BẢNG QUỐC GIA (Countries)
-- ==========================================
INSERT INTO Countries (CountryName)
VALUES 
(N'Anh'),         -- CountryID: 1
(N'Mỹ'),         -- CountryID: 2
(N'Nhật'),       -- CountryID: 3
(N'Hàn'),        -- CountryID: 4
(N'Việt');       -- CountryID: 5
GO

-- ==========================================
-- NẠP DỮ LIỆU BẢNG NGÔN NGỮ (Languages)
-- ==========================================
INSERT INTO [Languages] (LanguageName)
VALUES 
(N'Tiếng Anh'),   -- LanguageID: 1 (Dùng chung cho cả phim Anh/Mỹ)
(N'Tiếng Mỹ'),    -- LanguageID: 2 (Nếu muốn tách biệt biến thể)
(N'Tiếng Nhật'),  -- LanguageID: 3
(N'Tiếng Hàn'),   -- LanguageID: 4
(N'Tiếng Việt');  -- LanguageID: 5
GO

-- ==========================================
-- NẠP DỮ LIỆU BẢNG THỂ LOẠI (Genres)
-- ==========================================
INSERT INTO Genres (Name)
VALUES 
(N'Trinh thám'),           -- GenreID: 1
(N'Khoa học viễn tưởng'),  -- GenreID: 2
(N'Hoạt hình'),           -- GenreID: 3
(N'Tình cảm'),            -- GenreID: 4
(N'Hành động'),           -- GenreID: 5
(N'Kinh dị'),             -- GenreID: 6
(N'Gia đình'),            -- GenreID: 7
(N'Cảm động');            -- GenreID: 8
GO

-- ==========================================
-- NẠP DỮ LIỆU BẢNG PHIM VỚI QUỐC GIA VÀ NGÔN NGỮ TƯƠNG ỨNG ID
-- ==========================================

INSERT INTO Movies (Title, ReleaseDate, AgeRating, Duration, Synopsis, PosterURL, Trailer, CountryID, LanguageID)
VALUES
-- 1. Doraemon
(N'Doraemon', '2025-05-31', N'P', 105, 
 N'Cuộc phiêu lưu mới đầy thú vị của Doraemon, Nobita và những người bạn quen thuộc.', 
 '/img/poster/doraemon.jpg', 'trailer_doraemon.mp4', 3, 3),

-- 2. Toy Story
(N'Toy Story', '2025-06-20', N'P', 100, 
 N'Hành trình kỳ thú của thế giới đồ chơi khi đối mặt với những thử thách mới ngoài thế giới thực.', 
 '/img/poster/toystory.png', 'trailer_toy_story.mp4', 2, 1),

-- 3. Your Name
(N'Your Name', '2016-08-26', N'T13', 106, 
 N'Câu chuyện hoán đổi thân xác kỳ diệu giữa một cô gái vùng quê và một chàng trai Tokyo.', 
 '/img/poster/yourname400x600.png', 'trailer_your_name.mp4', 3, 3),

-- 4. Ma Xó
(N'Ma Xó', '2026-02-13', N'T18', 110, 
 N'Bộ phim kinh dị tâm linh Việt Nam xoay quanh những bí ẩn cổ xưa đầy rùng rợn tại một ngôi làng nhỏ.', 
 '/img/poster/maxo.jpg', 'trailer_ma_xo.mp4', 5, 5),

-- 5. Colony
(N'Colony', '2026-04-15', N'T16', 125, 
 N'Bối cảnh tương lai nơi con người phải chiến đấu sinh tồn trong một trật tự xã hội khắc nghiệt mới.', 
 '/img/poster/colony.jpg', 'trailer_colony.mp4', 4, 4),

-- 6. Super Girls
(N'Super Girls', '2026-06-05', N'T13', 130, 
 N'Hành trình của các nữ siêu anh hùng thế hệ mới trong việc bảo vệ công lý thế giới.', 
 '/img/poster/supergirl.png', 'trailer_super_girls.mp4', 2, 1),

-- 7. Cô bé Ponyo
(N'Cô bé Ponyo', '2008-07-19', N'P', 101, 
 N'Câu chuyện đáng yêu về tình bạn giữa một cậu bé loài người và cô bé cá vàng Ponyo muốn trở thành người.', 
 '/img/poster/ponyo.jpg', 'trailer_co_be_ponyo.mp4', 3, 3),

-- 8. HE-MAN VÀ NHỮNG CHIẾN BINH VŨ TRỤ
(N'HE-MAN VÀ NHỮNG CHIẾN BINH VŨ TRỤ', '2026-03-20', N'T13', 135, 
 N'Trận chiến sử thi bảo vệ vũ trụ của chiến binh He-Man chống lại thế lực đen tối Skeletor.', 
 '/img/poster/heman.jpg', 'trailer_he_man.mp4', 2, 1);
GO

-- ==========================================
-- NẠP DỮ LIỆU ĐẠO DIỄN (MovieDirectors)
-- ==========================================
INSERT INTO MovieDirectors (MovieID, PersonID)
VALUES
(1, 6),   -- Doraemon: Tetsuo Yajima
(2, 10),  -- Toy Story: Kenna Harris
(2, 11),  -- Toy Story: Andrew Stanton
(3, 15),  -- Your Name: Shinkai Makoto
(4, 23),  -- Ma Xó: Phan Bá Hỷ
(5, 27),  -- Colony: Yeon Sang-ho
(6, 34),  -- Super Girls: Craig Gillespie
(7, 35),  -- Cô bé Ponyo: MIYAZAKI HAYAO
(8, 42);  -- HE-MAN: Travis Knight
GO

-- ==========================================
-- NẠP DỮ LIỆU DIỄN VIÊN (MovieCasts)
-- Mặc định CharacterName để trống (NULL) hoặc có thể update sau
-- ==========================================
INSERT INTO MovieCasts (MovieID, PersonID)
VALUES
-- Doraemon
(1, 1), (1, 2), (1, 3), (1, 4), (1, 5),
-- Toy Story
(2, 7), (2, 8), (2, 9),
-- Your Name
(3, 12), (3, 13), (3, 14),
-- Ma Xó
(4, 16), (4, 17), (4, 18), (4, 19), (4, 20), (4, 21), (4, 22),
-- Colony
(5, 24), (5, 25), (5, 26),
-- Super Girls
(6, 28), (6, 29), (6, 30), (6, 31), (6, 32), (6, 33),
-- Cô bé Ponyo (Không có diễn viên lồng tiếng trong danh sách, bỏ qua)
-- HE-MAN
(8, 36), (8, 37), (8, 38), (8, 39), (8, 40), (8, 41);
GO

-- ==========================================
-- NẠP DỮ LIỆU THỂ LOẠI (MovieGenres)
-- ==========================================
-- Thể loại ID: 1:Trinh thám, 2:KHVT, 3:Hoạt hình, 4:Tình cảm, 5:Hành động, 6:Kinh dị, 7:Gia đình, 8:Cảm động
INSERT INTO MovieGenres (MovieID, GenreID)
VALUES
(1, 3), (1, 7),           -- Doraemon: Hoạt hình, Gia đình
(2, 3), (2, 7),           -- Toy Story: Hoạt hình, Gia đình
(3, 3), (3, 4), (3, 8),   -- Your Name: Hoạt hình, Tình cảm, Cảm động
(4, 6),                   -- Ma Xó: Kinh dị
(5, 2), (5, 5),           -- Colony: KHVT, Hành động
(6, 2), (6, 5),           -- Super Girls: KHVT, Hành động
(7, 3), (7, 7), (7, 8),   -- Cô bé Ponyo: Hoạt hình, Gia đình, Cảm động
(8, 2), (8, 5);           -- HE-MAN: KHVT, Hành động
GO

-- ==========================================
-- NẠP DỮ LIỆU PHÒNG CHIẾU (Rooms)
-- ==========================================
INSERT INTO Rooms (RoomName)
VALUES 
(N'Phòng chiếu 1 (Standard)'),  -- RoomID: 1
(N'Phòng chiếu 2 (Standard)'),  -- RoomID: 2
(N'Phòng chiếu 3 (Standard)'),  -- RoomID: 3
(N'Phòng chiếu 4 (Standard)'),  -- RoomID: 4
(N'Phòng chiếu 5 (Standard)'),  -- RoomID: 5
(N'Phòng chiếu 6 (Standard)'),  -- RoomID: 6
(N'Phòng chiếu 7 (Standard)'),  -- RoomID: 7
(N'Phòng chiếu 8 (Standard)'),  -- RoomID: 8
(N'Phòng chiếu 9 (Standard)'),  -- RoomID: 9
(N'Phòng chiếu 10 (IMAX)')  ,   -- RoomID: 10
(N'Phòng chiếu 11 (IMAX)')      -- RoomID: 11
GO



-- ==========================================
-- XÓA DỮ LIỆU CŨ VÀ RESET ID BẢNG SHOWTIMES
-- ==========================================
DELETE FROM Showtimes;
DBCC CHECKIDENT ('Showtimes', RESEED, 0);
GO

-- Thiết lập Chủ Nhật là ngày đầu tuần (Mã số 1), Thứ Bảy là mã số 7
SET DATEFIRST 7;
GO

-- ==========================================
-- SCRIPT TỰ ĐỘNG SINH SUẤT CHIẾU KÈM TÍNH GIÁ ĐỘNG
-- ==========================================

-- 1. Tạo bảng tạm (Bỏ cột BasePrice ở đây, giá sẽ được tính động bên dưới)
CREATE TABLE #DailySchedule (
    MovieID INT,
    RoomID INT,
    StartHour INT,
    StartMinute INT
);

-- Nạp kịch bản chiếu (Giữ nguyên giờ chiếu theo yêu cầu trước đó)
INSERT INTO #DailySchedule VALUES
-- Phim Việt / Kinh dị (Ma Xó)
(4, 1, 18, 00), (4, 1, 20, 30), (4, 1, 22, 45),
(4, 2, 19, 00), (4, 2, 21, 15), (4, 2, 23, 30),
-- Phim Hàn (Colony)
(5, 3, 09, 30), (5, 3, 13, 30), (5, 3, 17, 30), (5, 3, 21, 00),
-- Phim Hoạt hình (Doraemon)
(1, 5, 08, 00), (1, 5, 10, 30), (1, 5, 13, 00), (1, 5, 15, 30), (1, 5, 18, 00), (1, 5, 20, 30),
(1, 6, 09, 00), (1, 6, 11, 30), (1, 6, 14, 00), (1, 6, 16, 30), (1, 6, 19, 00),
-- Phim Hoạt hình (Toy Story)
(2, 8, 08, 30), (2, 8, 11, 00), (2, 8, 13, 30), (2, 8, 16, 00), (2, 8, 18, 30),
-- Phim Cũ (Your Name & Ponyo)
(3, 7, 10, 00), (3, 7, 15, 00),
(7, 7, 12, 30), (7, 7, 19, 30),
-- Phim Bom tấn (Super Girls)
(6, 4,  09, 30), (6, 4,  13, 00), (6, 4,  16, 30), (6, 4,  20, 00),
(6, 10, 10, 00), (6, 10, 14, 00), (6, 10, 18, 00), (6, 10, 22, 00),
-- Phim Bom tấn (HE-MAN)
(8, 9,  10, 30), (8, 9,  14, 00), (8, 9,  17, 30), (8, 9,  21, 00),
(8, 11, 09, 00), (8, 11, 13, 00), (8, 11, 17, 00), (8, 11, 21, 00);

-- 2. Tạo Dải ngày (Từ 15/04/2026 đến 30/06/2026)
WITH DateRange AS (
    SELECT CAST('2026-04-15' AS DATE) AS ShowDate
    UNION ALL
    SELECT DATEADD(DAY, 1, ShowDate)
    FROM DateRange
    WHERE ShowDate < '2026-06-30'
)
-- 3. Chèn dữ liệu & TÍNH TOÁN GIÁ VÉ ĐỘNG THEO CUỐI TUẦN / LOẠI RẠP
INSERT INTO Showtimes (MovieID, RoomID, Date, StartTime, EndTime, BasePrice)
SELECT 
    ds.MovieID,
    ds.RoomID,
    dr.ShowDate AS Date,
    DATEADD(MINUTE, ds.StartMinute, DATEADD(HOUR, ds.StartHour, CAST(dr.ShowDate AS DATETIME))) AS StartTime,
    DATEADD(MINUTE, m.Duration + 10, DATEADD(MINUTE, ds.StartMinute, DATEADD(HOUR, ds.StartHour, CAST(dr.ShowDate AS DATETIME)))) AS EndTime,
    
    -- LOGIC TÍNH GIÁ TRỰC TIẾP BẰNG SQL
    CASE 
        -- Nếu là rạp IMAX (Tìm kiếm chữ IMAX trong tên phòng)
        WHEN r.RoomName LIKE '%IMAX%' THEN
            CASE 
                WHEN DATEPART(dw, dr.ShowDate) IN (1, 7) THEN 165000 -- Thứ 7, Chủ Nhật
                ELSE 135000 -- Ngày thường
            END
        
        -- Nếu là rạp Thường (Standard)
        ELSE 
            CASE 
                WHEN DATEPART(dw, dr.ShowDate) IN (1, 7) THEN 85000  -- Thứ 7, Chủ Nhật
                ELSE 65000 -- Ngày thường
            END
    END AS BasePrice

FROM #DailySchedule ds
CROSS JOIN DateRange dr
JOIN Movies m ON ds.MovieID = m.MovieID
JOIN Rooms r ON ds.RoomID = r.RoomID
OPTION (MAXRECURSION 1000);

-- 4. Xóa bảng tạm
DROP TABLE #DailySchedule;
GO

-- ==========================================
-- NẠP DỮ LIỆU BẢNG GIÁ HỆ SỐ GHẾ (SeatTypePricing)
-- ==========================================
INSERT INTO SeatTypePricing (SeatType, Multiplier)
VALUES 
('Regular', 1.00), -- Hệ số để backend tính giá ghế từ BasePrice
('VIP', 1.50),     -- Backend nhân hệ số khi tính giá
('Couple', 2.50);  -- Backend nhân hệ số khi tính giá
GO


-- ==========================================
-- NẠP DỮ LIỆU GHẾ (Seats)
-- Danh sách ghế được liệt kê sẵn để file seed không tự sinh/tính toán bằng CTE.
-- ==========================================
INSERT INTO Seats (RoomID, SeatCode, SeatType, SeatStatus)
VALUES
-- ================= ROOM 1 =================
(1, 'A01', 'Regular', 'Not Order'), (1, 'A02', 'Regular', 'Not Order'), (1, 'A03', 'Regular', 'Order'), (1, 'A04', 'Regular', 'Not Order'), (1, 'A05', 'Regular', 'Not Order'),
(1, 'A06', 'Regular', 'Not Order'), (1, 'A07', 'Regular', 'Not Order'), (1, 'A08', 'Regular', 'Order'), (1, 'A09', 'Regular', 'Not Order'), (1, 'A10', 'Regular', 'Not Order'),
(1, 'B01', 'Regular', 'Not Order'), (1, 'B02', 'Regular', 'Order'), (1, 'B03', 'Regular', 'Not Order'), (1, 'B04', 'Regular', 'Not Order'), (1, 'B05', 'Regular', 'Not Order'),
(1, 'B06', 'Regular', 'Not Order'), (1, 'B07', 'Regular', 'Not Order'), (1, 'B08', 'Regular', 'Not Order'), (1, 'B09', 'Regular', 'Order'), (1, 'B10', 'Regular', 'Not Order'),
(1, 'C01', 'Regular', 'Order'), (1, 'C02', 'Regular', 'Not Order'), (1, 'C03', 'Regular', 'Not Order'), (1, 'C04', 'Regular', 'Not Order'), (1, 'C05', 'Regular', 'Not Order'),
(1, 'C06', 'Regular', 'Not Order'), (1, 'C07', 'Regular', 'Order'), (1, 'C08', 'Regular', 'Not Order'), (1, 'C09', 'Regular', 'Not Order'), (1, 'C10', 'Regular', 'Not Order'),
(1, 'D01', 'Regular', 'Not Order'), (1, 'D02', 'Regular', 'Not Order'), (1, 'D03', 'Regular', 'Not Order'), (1, 'D04', 'Regular', 'Order'), (1, 'D05', 'Regular', 'Not Order'),
(1, 'D06', 'Regular', 'Order'), (1, 'D07', 'Regular', 'Not Order'), (1, 'D08', 'Regular', 'Not Order'), (1, 'D09', 'Regular', 'Not Order'), (1, 'D10', 'Regular', 'Not Order'),
(1, 'E01', 'VIP', 'Order'), (1, 'E02', 'VIP', 'Order'), (1, 'E03', 'VIP', 'Not Order'), (1, 'E04', 'VIP', 'Order'), (1, 'E05', 'VIP', 'Order'),
(1, 'E06', 'VIP', 'Order'), (1, 'E07', 'VIP', 'Order'), (1, 'E08', 'VIP', 'Order'), (1, 'E09', 'VIP', 'Not Order'), (1, 'E10', 'VIP', 'Order'),
(1, 'F01', 'VIP', 'Order'), (1, 'F02', 'VIP', 'Not Order'), (1, 'F03', 'VIP', 'Order'), (1, 'F04', 'VIP', 'Order'), (1, 'F05', 'VIP', 'Order'),
(1, 'F06', 'VIP', 'Order'), (1, 'F07', 'VIP', 'Order'), (1, 'F08', 'VIP', 'Not Order'), (1, 'F09', 'VIP', 'Order'), (1, 'F10', 'VIP', 'Order'),
(1, 'G01', 'VIP', 'Not Order'), (1, 'G02', 'VIP', 'Order'), (1, 'G03', 'VIP', 'Order'), (1, 'G04', 'VIP', 'Order'), (1, 'G05', 'VIP', 'Order'),
(1, 'G06', 'VIP', 'Order'), (1, 'G07', 'VIP', 'Not Order'), (1, 'G08', 'VIP', 'Order'), (1, 'G09', 'VIP', 'Order'), (1, 'G10', 'VIP', 'Order'),
(1, 'H01', 'Couple', 'Order'), (1, 'H02', 'Couple', 'Not Order'), (1, 'H03', 'Couple', 'Order'), (1, 'H04', 'Couple', 'Order'), (1, 'H05', 'Couple', 'Not Order'),

-- ================= ROOM 2 =================
(2, 'A01', 'Regular', 'Not Order'), (2, 'A02', 'Regular', 'Not Order'), (2, 'A03', 'Regular', 'Order'), (2, 'A04', 'Regular', 'Not Order'), (2, 'A05', 'Regular', 'Not Order'),
(2, 'A06', 'Regular', 'Not Order'), (2, 'A07', 'Regular', 'Not Order'), (2, 'A08', 'Regular', 'Order'), (2, 'A09', 'Regular', 'Not Order'), (2, 'A10', 'Regular', 'Not Order'),
(2, 'B01', 'Regular', 'Not Order'), (2, 'B02', 'Regular', 'Order'), (2, 'B03', 'Regular', 'Not Order'), (2, 'B04', 'Regular', 'Not Order'), (2, 'B05', 'Regular', 'Not Order'),
(2, 'B06', 'Regular', 'Not Order'), (2, 'B07', 'Regular', 'Not Order'), (2, 'B08', 'Regular', 'Not Order'), (2, 'B09', 'Regular', 'Order'), (2, 'B10', 'Regular', 'Not Order'),
(2, 'C01', 'Regular', 'Order'), (2, 'C02', 'Regular', 'Not Order'), (2, 'C03', 'Regular', 'Not Order'), (2, 'C04', 'Regular', 'Not Order'), (2, 'C05', 'Regular', 'Not Order'),
(2, 'C06', 'Regular', 'Not Order'), (2, 'C07', 'Regular', 'Order'), (2, 'C08', 'Regular', 'Not Order'), (2, 'C09', 'Regular', 'Not Order'), (2, 'C10', 'Regular', 'Not Order'),
(2, 'D01', 'Regular', 'Not Order'), (2, 'D02', 'Regular', 'Not Order'), (2, 'D03', 'Regular', 'Not Order'), (2, 'D04', 'Regular', 'Order'), (2, 'D05', 'Regular', 'Not Order'),
(2, 'D06', 'Regular', 'Order'), (2, 'D07', 'Regular', 'Not Order'), (2, 'D08', 'Regular', 'Not Order'), (2, 'D09', 'Regular', 'Not Order'), (2, 'D10', 'Regular', 'Not Order'),
(2, 'E01', 'VIP', 'Order'), (2, 'E02', 'VIP', 'Order'), (2, 'E03', 'VIP', 'Not Order'), (2, 'E04', 'VIP', 'Order'), (2, 'E05', 'VIP', 'Order'),
(2, 'E06', 'VIP', 'Order'), (2, 'E07', 'VIP', 'Order'), (2, 'E08', 'VIP', 'Order'), (2, 'E09', 'VIP', 'Not Order'), (2, 'E10', 'VIP', 'Order'),
(2, 'F01', 'VIP', 'Order'), (2, 'F02', 'VIP', 'Not Order'), (2, 'F03', 'VIP', 'Order'), (2, 'F04', 'VIP', 'Order'), (2, 'F05', 'VIP', 'Order'),
(2, 'F06', 'VIP', 'Order'), (2, 'F07', 'VIP', 'Order'), (2, 'F08', 'VIP', 'Not Order'), (2, 'F09', 'VIP', 'Order'), (2, 'F10', 'VIP', 'Order'),
(2, 'G01', 'VIP', 'Not Order'), (2, 'G02', 'VIP', 'Order'), (2, 'G03', 'VIP', 'Order'), (2, 'G04', 'VIP', 'Order'), (2, 'G05', 'VIP', 'Order'),
(2, 'G06', 'VIP', 'Order'), (2, 'G07', 'VIP', 'Not Order'), (2, 'G08', 'VIP', 'Order'), (2, 'G09', 'VIP', 'Order'), (2, 'G10', 'VIP', 'Order'),
(2, 'H01', 'Couple', 'Order'), (2, 'H02', 'Couple', 'Not Order'), (2, 'H03', 'Couple', 'Order'), (2, 'H04', 'Couple', 'Order'), (2, 'H05', 'Couple', 'Not Order'),

-- ================= ROOM 3 =================
(3, 'A01', 'Regular', 'Not Order'), (3, 'A02', 'Regular', 'Not Order'), (3, 'A03', 'Regular', 'Order'), (3, 'A04', 'Regular', 'Not Order'), (3, 'A05', 'Regular', 'Not Order'),
(3, 'A06', 'Regular', 'Not Order'), (3, 'A07', 'Regular', 'Not Order'), (3, 'A08', 'Regular', 'Order'), (3, 'A09', 'Regular', 'Not Order'), (3, 'A10', 'Regular', 'Not Order'),
(3, 'B01', 'Regular', 'Not Order'), (3, 'B02', 'Regular', 'Order'), (3, 'B03', 'Regular', 'Not Order'), (3, 'B04', 'Regular', 'Not Order'), (3, 'B05', 'Regular', 'Not Order'),
(3, 'B06', 'Regular', 'Not Order'), (3, 'B07', 'Regular', 'Not Order'), (3, 'B08', 'Regular', 'Not Order'), (3, 'B09', 'Regular', 'Order'), (3, 'B10', 'Regular', 'Not Order'),
(3, 'C01', 'Regular', 'Order'), (3, 'C02', 'Regular', 'Not Order'), (3, 'C03', 'Regular', 'Not Order'), (3, 'C04', 'Regular', 'Not Order'), (3, 'C05', 'Regular', 'Not Order'),
(3, 'C06', 'Regular', 'Not Order'), (3, 'C07', 'Regular', 'Order'), (3, 'C08', 'Regular', 'Not Order'), (3, 'C09', 'Regular', 'Not Order'), (3, 'C10', 'Regular', 'Not Order'),
(3, 'D01', 'Regular', 'Not Order'), (3, 'D02', 'Regular', 'Not Order'), (3, 'D03', 'Regular', 'Not Order'), (3, 'D04', 'Regular', 'Order'), (3, 'D05', 'Regular', 'Not Order'),
(3, 'D06', 'Regular', 'Order'), (3, 'D07', 'Regular', 'Not Order'), (3, 'D08', 'Regular', 'Not Order'), (3, 'D09', 'Regular', 'Not Order'), (3, 'D10', 'Regular', 'Not Order'),
(3, 'E01', 'VIP', 'Order'), (3, 'E02', 'VIP', 'Order'), (3, 'E03', 'VIP', 'Not Order'), (3, 'E04', 'VIP', 'Order'), (3, 'E05', 'VIP', 'Order'),
(3, 'E06', 'VIP', 'Order'), (3, 'E07', 'VIP', 'Order'), (3, 'E08', 'VIP', 'Order'), (3, 'E09', 'VIP', 'Not Order'), (3, 'E10', 'VIP', 'Order'),
(3, 'F01', 'VIP', 'Order'), (3, 'F02', 'VIP', 'Not Order'), (3, 'F03', 'VIP', 'Order'), (3, 'F04', 'VIP', 'Order'), (3, 'F05', 'VIP', 'Order'),
(3, 'F06', 'VIP', 'Order'), (3, 'F07', 'VIP', 'Order'), (3, 'F08', 'VIP', 'Not Order'), (3, 'F09', 'VIP', 'Order'), (3, 'F10', 'VIP', 'Order'),
(3, 'G01', 'VIP', 'Not Order'), (3, 'G02', 'VIP', 'Order'), (3, 'G03', 'VIP', 'Order'), (3, 'G04', 'VIP', 'Order'), (3, 'G05', 'VIP', 'Order'),
(3, 'G06', 'VIP', 'Order'), (3, 'G07', 'VIP', 'Not Order'), (3, 'G08', 'VIP', 'Order'), (3, 'G09', 'VIP', 'Order'), (3, 'G10', 'VIP', 'Order'),
(3, 'H01', 'Couple', 'Order'), (3, 'H02', 'Couple', 'Not Order'), (3, 'H03', 'Couple', 'Order'), (3, 'H04', 'Couple', 'Order'), (3, 'H05', 'Couple', 'Not Order'),

-- ================= ROOM 4 =================
(4, 'A01', 'Regular', 'Not Order'), (4, 'A02', 'Regular', 'Not Order'), (4, 'A03', 'Regular', 'Order'), (4, 'A04', 'Regular', 'Not Order'), (4, 'A05', 'Regular', 'Not Order'),
(4, 'A06', 'Regular', 'Not Order'), (4, 'A07', 'Regular', 'Not Order'), (4, 'A08', 'Regular', 'Order'), (4, 'A09', 'Regular', 'Not Order'), (4, 'A10', 'Regular', 'Not Order'),
(4, 'B01', 'Regular', 'Not Order'), (4, 'B02', 'Regular', 'Order'), (4, 'B03', 'Regular', 'Not Order'), (4, 'B04', 'Regular', 'Not Order'), (4, 'B05', 'Regular', 'Not Order'),
(4, 'B06', 'Regular', 'Not Order'), (4, 'B07', 'Regular', 'Not Order'), (4, 'B08', 'Regular', 'Not Order'), (4, 'B09', 'Regular', 'Order'), (4, 'B10', 'Regular', 'Not Order'),
(4, 'C01', 'Regular', 'Order'), (4, 'C02', 'Regular', 'Not Order'), (4, 'C03', 'Regular', 'Not Order'), (4, 'C04', 'Regular', 'Not Order'), (4, 'C05', 'Regular', 'Not Order'),
(4, 'C06', 'Regular', 'Not Order'), (4, 'C07', 'Regular', 'Order'), (4, 'C08', 'Regular', 'Not Order'), (4, 'C09', 'Regular', 'Not Order'), (4, 'C10', 'Regular', 'Not Order'),
(4, 'D01', 'Regular', 'Not Order'), (4, 'D02', 'Regular', 'Not Order'), (4, 'D03', 'Regular', 'Not Order'), (4, 'D04', 'Regular', 'Order'), (4, 'D05', 'Regular', 'Not Order'),
(4, 'D06', 'Regular', 'Order'), (4, 'D07', 'Regular', 'Not Order'), (4, 'D08', 'Regular', 'Not Order'), (4, 'D09', 'Regular', 'Not Order'), (4, 'D10', 'Regular', 'Not Order'),
(4, 'E01', 'VIP', 'Order'), (4, 'E02', 'VIP', 'Order'), (4, 'E03', 'VIP', 'Not Order'), (4, 'E04', 'VIP', 'Order'), (4, 'E05', 'VIP', 'Order'),
(4, 'E06', 'VIP', 'Order'), (4, 'E07', 'VIP', 'Order'), (4, 'E08', 'VIP', 'Order'), (4, 'E09', 'VIP', 'Not Order'), (4, 'E10', 'VIP', 'Order'),
(4, 'F01', 'VIP', 'Order'), (4, 'F02', 'VIP', 'Not Order'), (4, 'F03', 'VIP', 'Order'), (4, 'F04', 'VIP', 'Order'), (4, 'F05', 'VIP', 'Order'),
(4, 'F06', 'VIP', 'Order'), (4, 'F07', 'VIP', 'Order'), (4, 'F08', 'VIP', 'Not Order'), (4, 'F09', 'VIP', 'Order'), (4, 'F10', 'VIP', 'Order'),
(4, 'G01', 'VIP', 'Not Order'), (4, 'G02', 'VIP', 'Order'), (4, 'G03', 'VIP', 'Order'), (4, 'G04', 'VIP', 'Order'), (4, 'G05', 'VIP', 'Order'),
(4, 'G06', 'VIP', 'Order'), (4, 'G07', 'VIP', 'Not Order'), (4, 'G08', 'VIP', 'Order'), (4, 'G09', 'VIP', 'Order'), (4, 'G10', 'VIP', 'Order'),
(4, 'H01', 'Couple', 'Order'), (4, 'H02', 'Couple', 'Not Order'), (4, 'H03', 'Couple', 'Order'), (4, 'H04', 'Couple', 'Order'), (4, 'H05', 'Couple', 'Not Order'),

-- ================= ROOM 5 =================
(5, 'A01', 'Regular', 'Not Order'), (5, 'A02', 'Regular', 'Not Order'), (5, 'A03', 'Regular', 'Order'), (5, 'A04', 'Regular', 'Not Order'), (5, 'A05', 'Regular', 'Not Order'),
(5, 'A06', 'Regular', 'Not Order'), (5, 'A07', 'Regular', 'Not Order'), (5, 'A08', 'Regular', 'Order'), (5, 'A09', 'Regular', 'Not Order'), (5, 'A10', 'Regular', 'Not Order'),
(5, 'B01', 'Regular', 'Not Order'), (5, 'B02', 'Regular', 'Order'), (5, 'B03', 'Regular', 'Not Order'), (5, 'B04', 'Regular', 'Not Order'), (5, 'B05', 'Regular', 'Not Order'),
(5, 'B06', 'Regular', 'Not Order'), (5, 'B07', 'Regular', 'Not Order'), (5, 'B08', 'Regular', 'Not Order'), (5, 'B09', 'Regular', 'Order'), (5, 'B10', 'Regular', 'Not Order'),
(5, 'C01', 'Regular', 'Order'), (5, 'C02', 'Regular', 'Not Order'), (5, 'C03', 'Regular', 'Not Order'), (5, 'C04', 'Regular', 'Not Order'), (5, 'C05', 'Regular', 'Not Order'),
(5, 'C06', 'Regular', 'Not Order'), (5, 'C07', 'Regular', 'Order'), (5, 'C08', 'Regular', 'Not Order'), (5, 'C09', 'Regular', 'Not Order'), (5, 'C10', 'Regular', 'Not Order'),
(5, 'D01', 'Regular', 'Not Order'), (5, 'D02', 'Regular', 'Not Order'), (5, 'D03', 'Regular', 'Not Order'), (5, 'D04', 'Regular', 'Order'), (5, 'D05', 'Regular', 'Not Order'),
(5, 'D06', 'Regular', 'Order'), (5, 'D07', 'Regular', 'Not Order'), (5, 'D08', 'Regular', 'Not Order'), (5, 'D09', 'Regular', 'Not Order'), (5, 'D10', 'Regular', 'Not Order'),
(5, 'E01', 'VIP', 'Order'), (5, 'E02', 'VIP', 'Order'), (5, 'E03', 'VIP', 'Not Order'), (5, 'E04', 'VIP', 'Order'), (5, 'E05', 'VIP', 'Order'),
(5, 'E06', 'VIP', 'Order'), (5, 'E07', 'VIP', 'Order'), (5, 'E08', 'VIP', 'Order'), (5, 'E09', 'VIP', 'Not Order'), (5, 'E10', 'VIP', 'Order'),
(5, 'F01', 'VIP', 'Order'), (5, 'F02', 'VIP', 'Not Order'), (5, 'F03', 'VIP', 'Order'), (5, 'F04', 'VIP', 'Order'), (5, 'F05', 'VIP', 'Order'),
(5, 'F06', 'VIP', 'Order'), (5, 'F07', 'VIP', 'Order'), (5, 'F08', 'VIP', 'Not Order'), (5, 'F09', 'VIP', 'Order'), (5, 'F10', 'VIP', 'Order'),
(5, 'G01', 'VIP', 'Not Order'), (5, 'G02', 'VIP', 'Order'), (5, 'G03', 'VIP', 'Order'), (5, 'G04', 'VIP', 'Order'), (5, 'G05', 'VIP', 'Order'),
(5, 'G06', 'VIP', 'Order'), (5, 'G07', 'VIP', 'Not Order'), (5, 'G08', 'VIP', 'Order'), (5, 'G09', 'VIP', 'Order'), (5, 'G10', 'VIP', 'Order'),
(5, 'H01', 'Couple', 'Order'), (5, 'H02', 'Couple', 'Not Order'), (5, 'H03', 'Couple', 'Order'), (5, 'H04', 'Couple', 'Order'), (5, 'H05', 'Couple', 'Not Order'),

-- ================= ROOM 6 =================
(6, 'A01', 'Regular', 'Not Order'), (6, 'A02', 'Regular', 'Not Order'), (6, 'A03', 'Regular', 'Order'), (6, 'A04', 'Regular', 'Not Order'), (6, 'A05', 'Regular', 'Not Order'),
(6, 'A06', 'Regular', 'Not Order'), (6, 'A07', 'Regular', 'Not Order'), (6, 'A08', 'Regular', 'Order'), (6, 'A09', 'Regular', 'Not Order'), (6, 'A10', 'Regular', 'Not Order'),
(6, 'B01', 'Regular', 'Not Order'), (6, 'B02', 'Regular', 'Order'), (6, 'B03', 'Regular', 'Not Order'), (6, 'B04', 'Regular', 'Not Order'), (6, 'B05', 'Regular', 'Not Order'),
(6, 'B06', 'Regular', 'Not Order'), (6, 'B07', 'Regular', 'Not Order'), (6, 'B08', 'Regular', 'Not Order'), (6, 'B09', 'Regular', 'Order'), (6, 'B10', 'Regular', 'Not Order'),
(6, 'C01', 'Regular', 'Order'), (6, 'C02', 'Regular', 'Not Order'), (6, 'C03', 'Regular', 'Not Order'), (6, 'C04', 'Regular', 'Not Order'), (6, 'C05', 'Regular', 'Not Order'),
(6, 'C06', 'Regular', 'Not Order'), (6, 'C07', 'Regular', 'Order'), (6, 'C08', 'Regular', 'Not Order'), (6, 'C09', 'Regular', 'Not Order'), (6, 'C10', 'Regular', 'Not Order'),
(6, 'D01', 'Regular', 'Not Order'), (6, 'D02', 'Regular', 'Not Order'), (6, 'D03', 'Regular', 'Not Order'), (6, 'D04', 'Regular', 'Order'), (6, 'D05', 'Regular', 'Not Order'),
(6, 'D06', 'Regular', 'Order'), (6, 'D07', 'Regular', 'Not Order'), (6, 'D08', 'Regular', 'Not Order'), (6, 'D09', 'Regular', 'Not Order'), (6, 'D10', 'Regular', 'Not Order'),
(6, 'E01', 'VIP', 'Order'), (6, 'E02', 'VIP', 'Order'), (6, 'E03', 'VIP', 'Not Order'), (6, 'E04', 'VIP', 'Order'), (6, 'E05', 'VIP', 'Order'),
(6, 'E06', 'VIP', 'Order'), (6, 'E07', 'VIP', 'Order'), (6, 'E08', 'VIP', 'Order'), (6, 'E09', 'VIP', 'Not Order'), (6, 'E10', 'VIP', 'Order'),
(6, 'F01', 'VIP', 'Order'), (6, 'F02', 'VIP', 'Not Order'), (6, 'F03', 'VIP', 'Order'), (6, 'F04', 'VIP', 'Order'), (6, 'F05', 'VIP', 'Order'),
(6, 'F06', 'VIP', 'Order'), (6, 'F07', 'VIP', 'Order'), (6, 'F08', 'VIP', 'Not Order'), (6, 'F09', 'VIP', 'Order'), (6, 'F10', 'VIP', 'Order'),
(6, 'G01', 'VIP', 'Not Order'), (6, 'G02', 'VIP', 'Order'), (6, 'G03', 'VIP', 'Order'), (6, 'G04', 'VIP', 'Order'), (6, 'G05', 'VIP', 'Order'),
(6, 'G06', 'VIP', 'Order'), (6, 'G07', 'VIP', 'Not Order'), (6, 'G08', 'VIP', 'Order'), (6, 'G09', 'VIP', 'Order'), (6, 'G10', 'VIP', 'Order'),
(6, 'H01', 'Couple', 'Order'), (6, 'H02', 'Couple', 'Not Order'), (6, 'H03', 'Couple', 'Order'), (6, 'H04', 'Couple', 'Order'), (6, 'H05', 'Couple', 'Not Order'),

-- ================= ROOM 7 =================
(7, 'A01', 'Regular', 'Not Order'), (7, 'A02', 'Regular', 'Not Order'), (7, 'A03', 'Regular', 'Order'), (7, 'A04', 'Regular', 'Not Order'), (7, 'A05', 'Regular', 'Not Order'),
(7, 'A06', 'Regular', 'Not Order'), (7, 'A07', 'Regular', 'Not Order'), (7, 'A08', 'Regular', 'Order'), (7, 'A09', 'Regular', 'Not Order'), (7, 'A10', 'Regular', 'Not Order'),
(7, 'B01', 'Regular', 'Not Order'), (7, 'B02', 'Regular', 'Order'), (7, 'B03', 'Regular', 'Not Order'), (7, 'B04', 'Regular', 'Not Order'), (7, 'B05', 'Regular', 'Not Order'),
(7, 'B06', 'Regular', 'Not Order'), (7, 'B07', 'Regular', 'Not Order'), (7, 'B08', 'Regular', 'Not Order'), (7, 'B09', 'Regular', 'Order'), (7, 'B10', 'Regular', 'Not Order'),
(7, 'C01', 'Regular', 'Order'), (7, 'C02', 'Regular', 'Not Order'), (7, 'C03', 'Regular', 'Not Order'), (7, 'C04', 'Regular', 'Not Order'), (7, 'C05', 'Regular', 'Not Order'),
(7, 'C06', 'Regular', 'Not Order'), (7, 'C07', 'Regular', 'Order'), (7, 'C08', 'Regular', 'Not Order'), (7, 'C09', 'Regular', 'Not Order'), (7, 'C10', 'Regular', 'Not Order'),
(7, 'D01', 'Regular', 'Not Order'), (7, 'D02', 'Regular', 'Not Order'), (7, 'D03', 'Regular', 'Not Order'), (7, 'D04', 'Regular', 'Order'), (7, 'D05', 'Regular', 'Not Order'),
(7, 'D06', 'Regular', 'Order'), (7, 'D07', 'Regular', 'Not Order'), (7, 'D08', 'Regular', 'Not Order'), (7, 'D09', 'Regular', 'Not Order'), (7, 'D10', 'Regular', 'Not Order'),
(7, 'E01', 'VIP', 'Order'), (7, 'E02', 'VIP', 'Order'), (7, 'E03', 'VIP', 'Not Order'), (7, 'E04', 'VIP', 'Order'), (7, 'E05', 'VIP', 'Order'),
(7, 'E06', 'VIP', 'Order'), (7, 'E07', 'VIP', 'Order'), (7, 'E08', 'VIP', 'Order'), (7, 'E09', 'VIP', 'Not Order'), (7, 'E10', 'VIP', 'Order'),
(7, 'F01', 'VIP', 'Order'), (7, 'F02', 'VIP', 'Not Order'), (7, 'F03', 'VIP', 'Order'), (7, 'F04', 'VIP', 'Order'), (7, 'F05', 'VIP', 'Order'),
(7, 'F06', 'VIP', 'Order'), (7, 'F07', 'VIP', 'Order'), (7, 'F08', 'VIP', 'Not Order'), (7, 'F09', 'VIP', 'Order'), (7, 'F10', 'VIP', 'Order'),
(7, 'G01', 'VIP', 'Not Order'), (7, 'G02', 'VIP', 'Order'), (7, 'G03', 'VIP', 'Order'), (7, 'G04', 'VIP', 'Order'), (7, 'G05', 'VIP', 'Order'),
(7, 'G06', 'VIP', 'Order'), (7, 'G07', 'VIP', 'Not Order'), (7, 'G08', 'VIP', 'Order'), (7, 'G09', 'VIP', 'Order'), (7, 'G10', 'VIP', 'Order'),
(7, 'H01', 'Couple', 'Order'), (7, 'H02', 'Couple', 'Not Order'), (7, 'H03', 'Couple', 'Order'), (7, 'H04', 'Couple', 'Order'), (7, 'H05', 'Couple', 'Not Order'),

-- ================= ROOM 8 =================
(8, 'A01', 'Regular', 'Not Order'), (8, 'A02', 'Regular', 'Not Order'), (8, 'A03', 'Regular', 'Order'), (8, 'A04', 'Regular', 'Not Order'), (8, 'A05', 'Regular', 'Not Order'),
(8, 'A06', 'Regular', 'Not Order'), (8, 'A07', 'Regular', 'Not Order'), (8, 'A08', 'Regular', 'Order'), (8, 'A09', 'Regular', 'Not Order'), (8, 'A10', 'Regular', 'Not Order'),
(8, 'B01', 'Regular', 'Not Order'), (8, 'B02', 'Regular', 'Order'), (8, 'B03', 'Regular', 'Not Order'), (8, 'B04', 'Regular', 'Not Order'), (8, 'B05', 'Regular', 'Not Order'),
(8, 'B06', 'Regular', 'Not Order'), (8, 'B07', 'Regular', 'Not Order'), (8, 'B08', 'Regular', 'Not Order'), (8, 'B09', 'Regular', 'Order'), (8, 'B10', 'Regular', 'Not Order'),
(8, 'C01', 'Regular', 'Order'), (8, 'C02', 'Regular', 'Not Order'), (8, 'C03', 'Regular', 'Not Order'), (8, 'C04', 'Regular', 'Not Order'), (8, 'C05', 'Regular', 'Not Order'),
(8, 'C06', 'Regular', 'Not Order'), (8, 'C07', 'Regular', 'Order'), (8, 'C08', 'Regular', 'Not Order'), (8, 'C09', 'Regular', 'Not Order'), (8, 'C10', 'Regular', 'Not Order'),
(8, 'D01', 'Regular', 'Not Order'), (8, 'D02', 'Regular', 'Not Order'), (8, 'D03', 'Regular', 'Not Order'), (8, 'D04', 'Regular', 'Order'), (8, 'D05', 'Regular', 'Not Order'),
(8, 'D06', 'Regular', 'Order'), (8, 'D07', 'Regular', 'Not Order'), (8, 'D08', 'Regular', 'Not Order'), (8, 'D09', 'Regular', 'Not Order'), (8, 'D10', 'Regular', 'Not Order'),
(8, 'E01', 'VIP', 'Order'), (8, 'E02', 'VIP', 'Order'), (8, 'E03', 'VIP', 'Not Order'), (8, 'E04', 'VIP', 'Order'), (8, 'E05', 'VIP', 'Order'),
(8, 'E06', 'VIP', 'Order'), (8, 'E07', 'VIP', 'Order'), (8, 'E08', 'VIP', 'Order'), (8, 'E09', 'VIP', 'Not Order'), (8, 'E10', 'VIP', 'Order'),
(8, 'F01', 'VIP', 'Order'), (8, 'F02', 'VIP', 'Not Order'), (8, 'F03', 'VIP', 'Order'), (8, 'F04', 'VIP', 'Order'), (8, 'F05', 'VIP', 'Order'),
(8, 'F06', 'VIP', 'Order'), (8, 'F07', 'VIP', 'Order'), (8, 'F08', 'VIP', 'Not Order'), (8, 'F09', 'VIP', 'Order'), (8, 'F10', 'VIP', 'Order'),
(8, 'G01', 'VIP', 'Not Order'), (8, 'G02', 'VIP', 'Order'), (8, 'G03', 'VIP', 'Order'), (8, 'G04', 'VIP', 'Order'), (8, 'G05', 'VIP', 'Order'),
(8, 'G06', 'VIP', 'Order'), (8, 'G07', 'VIP', 'Not Order'), (8, 'G08', 'VIP', 'Order'), (8, 'G09', 'VIP', 'Order'), (8, 'G10', 'VIP', 'Order'),
(8, 'H01', 'Couple', 'Order'), (8, 'H02', 'Couple', 'Not Order'), (8, 'H03', 'Couple', 'Order'), (8, 'H04', 'Couple', 'Order'), (8, 'H05', 'Couple', 'Not Order'),

-- ================= ROOM 9 =================
(9, 'A01', 'Regular', 'Not Order'), (9, 'A02', 'Regular', 'Not Order'), (9, 'A03', 'Regular', 'Order'), (9, 'A04', 'Regular', 'Not Order'), (9, 'A05', 'Regular', 'Not Order'),
(9, 'A06', 'Regular', 'Not Order'), (9, 'A07', 'Regular', 'Not Order'), (9, 'A08', 'Regular', 'Order'), (9, 'A09', 'Regular', 'Not Order'), (9, 'A10', 'Regular', 'Not Order'),
(9, 'B01', 'Regular', 'Not Order'), (9, 'B02', 'Regular', 'Order'), (9, 'B03', 'Regular', 'Not Order'), (9, 'B04', 'Regular', 'Not Order'), (9, 'B05', 'Regular', 'Not Order'),
(9, 'B06', 'Regular', 'Not Order'), (9, 'B07', 'Regular', 'Not Order'), (9, 'B08', 'Regular', 'Not Order'), (9, 'B09', 'Regular', 'Order'), (9, 'B10', 'Regular', 'Not Order'),
(9, 'C01', 'Regular', 'Order'), (9, 'C02', 'Regular', 'Not Order'), (9, 'C03', 'Regular', 'Not Order'), (9, 'C04', 'Regular', 'Not Order'), (9, 'C05', 'Regular', 'Not Order'),
(9, 'C06', 'Regular', 'Not Order'), (9, 'C07', 'Regular', 'Order'), (9, 'C08', 'Regular', 'Not Order'), (9, 'C09', 'Regular', 'Not Order'), (9, 'C10', 'Regular', 'Not Order'),
(9, 'D01', 'Regular', 'Not Order'), (9, 'D02', 'Regular', 'Not Order'), (9, 'D03', 'Regular', 'Not Order'), (9, 'D04', 'Regular', 'Order'), (9, 'D05', 'Regular', 'Not Order'),
(9, 'D06', 'Regular', 'Order'), (9, 'D07', 'Regular', 'Not Order'), (9, 'D08', 'Regular', 'Not Order'), (9, 'D09', 'Regular', 'Not Order'), (9, 'D10', 'Regular', 'Not Order'),
(9, 'E01', 'VIP', 'Order'), (9, 'E02', 'VIP', 'Order'), (9, 'E03', 'VIP', 'Not Order'), (9, 'E04', 'VIP', 'Order'), (9, 'E05', 'VIP', 'Order'),
(9, 'E06', 'VIP', 'Order'), (9, 'E07', 'VIP', 'Order'), (9, 'E08', 'VIP', 'Order'), (9, 'E09', 'VIP', 'Not Order'), (9, 'E10', 'VIP', 'Order'),
(9, 'F01', 'VIP', 'Order'), (9, 'F02', 'VIP', 'Not Order'), (9, 'F03', 'VIP', 'Order'), (9, 'F04', 'VIP', 'Order'), (9, 'F05', 'VIP', 'Order'),
(9, 'F06', 'VIP', 'Order'), (9, 'F07', 'VIP', 'Order'), (9, 'F08', 'VIP', 'Not Order'), (9, 'F09', 'VIP', 'Order'), (9, 'F10', 'VIP', 'Order'),
(9, 'G01', 'VIP', 'Not Order'), (9, 'G02', 'VIP', 'Order'), (9, 'G03', 'VIP', 'Order'), (9, 'G04', 'VIP', 'Order'), (9, 'G05', 'VIP', 'Order'),
(9, 'G06', 'VIP', 'Order'), (9, 'G07', 'VIP', 'Not Order'), (9, 'G08', 'VIP', 'Order'), (9, 'G09', 'VIP', 'Order'), (9, 'G10', 'VIP', 'Order'),
(9, 'H01', 'Couple', 'Order'), (9, 'H02', 'Couple', 'Not Order'), (9, 'H03', 'Couple', 'Order'), (9, 'H04', 'Couple', 'Order'), (9, 'H05', 'Couple', 'Not Order'),

-- ================= ROOM 10 =================
(10, 'A01', 'Regular', 'Not Order'), (10, 'A02', 'Regular', 'Not Order'), (10, 'A03', 'Regular', 'Order'), (10, 'A04', 'Regular', 'Not Order'), (10, 'A05', 'Regular', 'Not Order'),
(10, 'A06', 'Regular', 'Not Order'), (10, 'A07', 'Regular', 'Not Order'), (10, 'A08', 'Regular', 'Order'), (10, 'A09', 'Regular', 'Not Order'), (10, 'A10', 'Regular', 'Not Order'),
(10, 'B01', 'Regular', 'Not Order'), (10, 'B02', 'Regular', 'Order'), (10, 'B03', 'Regular', 'Not Order'), (10, 'B04', 'Regular', 'Not Order'), (10, 'B05', 'Regular', 'Not Order'),
(10, 'B06', 'Regular', 'Not Order'), (10, 'B07', 'Regular', 'Not Order'), (10, 'B08', 'Regular', 'Not Order'), (10, 'B09', 'Regular', 'Order'), (10, 'B10', 'Regular', 'Not Order'),
(10, 'C01', 'Regular', 'Order'), (10, 'C02', 'Regular', 'Not Order'), (10, 'C03', 'Regular', 'Not Order'), (10, 'C04', 'Regular', 'Not Order'), (10, 'C05', 'Regular', 'Not Order'),
(10, 'C06', 'Regular', 'Not Order'), (10, 'C07', 'Regular', 'Order'), (10, 'C08', 'Regular', 'Not Order'), (10, 'C09', 'Regular', 'Not Order'), (10, 'C10', 'Regular', 'Not Order'),
(10, 'D01', 'Regular', 'Not Order'), (10, 'D02', 'Regular', 'Not Order'), (10, 'D03', 'Regular', 'Not Order'), (10, 'D04', 'Regular', 'Order'), (10, 'D05', 'Regular', 'Not Order'),
(10, 'D06', 'Regular', 'Order'), (10, 'D07', 'Regular', 'Not Order'), (10, 'D08', 'Regular', 'Not Order'), (10, 'D09', 'Regular', 'Not Order'), (10, 'D10', 'Regular', 'Not Order'),
(10, 'E01', 'VIP', 'Order'), (10, 'E02', 'VIP', 'Order'), (10, 'E03', 'VIP', 'Not Order'), (10, 'E04', 'VIP', 'Order'), (10, 'E05', 'VIP', 'Order'),
(10, 'E06', 'VIP', 'Order'), (10, 'E07', 'VIP', 'Order'), (10, 'E08', 'VIP', 'Order'), (10, 'E09', 'VIP', 'Not Order'), (10, 'E10', 'VIP', 'Order'),
(10, 'F01', 'VIP', 'Order'), (10, 'F02', 'VIP', 'Not Order'), (10, 'F03', 'VIP', 'Order'), (10, 'F04', 'VIP', 'Order'), (10, 'F05', 'VIP', 'Order'),
(10, 'F06', 'VIP', 'Order'), (10, 'F07', 'VIP', 'Order'), (10, 'F08', 'VIP', 'Not Order'), (10, 'F09', 'VIP', 'Order'), (10, 'F10', 'VIP', 'Order'),
(10, 'G01', 'VIP', 'Not Order'), (10, 'G02', 'VIP', 'Order'), (10, 'G03', 'VIP', 'Order'), (10, 'G04', 'VIP', 'Order'), (10, 'G05', 'VIP', 'Order'),
(10, 'G06', 'VIP', 'Order'), (10, 'G07', 'VIP', 'Not Order'), (10, 'G08', 'VIP', 'Order'), (10, 'G09', 'VIP', 'Order'), (10, 'G10', 'VIP', 'Order'),
(10, 'H01', 'Couple', 'Order'), (10, 'H02', 'Couple', 'Not Order'), (10, 'H03', 'Couple', 'Order'), (10, 'H04', 'Couple', 'Order'), (10, 'H05', 'Couple', 'Not Order'),

-- ================= ROOM 11 =================
(11, 'A01', 'Regular', 'Not Order'), (11, 'A02', 'Regular', 'Not Order'), (11, 'A03', 'Regular', 'Order'), (11, 'A04', 'Regular', 'Not Order'), (11, 'A05', 'Regular', 'Not Order'),
(11, 'A06', 'Regular', 'Not Order'), (11, 'A07', 'Regular', 'Not Order'), (11, 'A08', 'Regular', 'Order'), (11, 'A09', 'Regular', 'Not Order'), (11, 'A10', 'Regular', 'Not Order'),
(11, 'B01', 'Regular', 'Not Order'), (11, 'B02', 'Regular', 'Order'), (11, 'B03', 'Regular', 'Not Order'), (11, 'B04', 'Regular', 'Not Order'), (11, 'B05', 'Regular', 'Not Order'),
(11, 'B06', 'Regular', 'Not Order'), (11, 'B07', 'Regular', 'Not Order'), (11, 'B08', 'Regular', 'Not Order'), (11, 'B09', 'Regular', 'Order'), (11, 'B10', 'Regular', 'Not Order'),
(11, 'C01', 'Regular', 'Order'), (11, 'C02', 'Regular', 'Not Order'), (11, 'C03', 'Regular', 'Not Order'), (11, 'C04', 'Regular', 'Not Order'), (11, 'C05', 'Regular', 'Not Order'),
(11, 'C06', 'Regular', 'Not Order'), (11, 'C07', 'Regular', 'Order'), (11, 'C08', 'Regular', 'Not Order'), (11, 'C09', 'Regular', 'Not Order'), (11, 'C10', 'Regular', 'Not Order'),
(11, 'D01', 'Regular', 'Not Order'), (11, 'D02', 'Regular', 'Not Order'), (11, 'D03', 'Regular', 'Not Order'), (11, 'D04', 'Regular', 'Order'), (11, 'D05', 'Regular', 'Not Order'),
(11, 'D06', 'Regular', 'Order'), (11, 'D07', 'Regular', 'Not Order'), (11, 'D08', 'Regular', 'Not Order'), (11, 'D09', 'Regular', 'Not Order'), (11, 'D10', 'Regular', 'Not Order'),
(11, 'E01', 'VIP', 'Order'), (11, 'E02', 'VIP', 'Order'), (11, 'E03', 'VIP', 'Not Order'), (11, 'E04', 'VIP', 'Order'), (11, 'E05', 'VIP', 'Order'),
(11, 'E06', 'VIP', 'Order'), (11, 'E07', 'VIP', 'Order'), (11, 'E08', 'VIP', 'Order'), (11, 'E09', 'VIP', 'Not Order'), (11, 'E10', 'VIP', 'Order'),
(11, 'F01', 'VIP', 'Order'), (11, 'F02', 'VIP', 'Not Order'), (11, 'F03', 'VIP', 'Order'), (11, 'F04', 'VIP', 'Order'), (11, 'F05', 'VIP', 'Order'),
(11, 'F06', 'VIP', 'Order'), (11, 'F07', 'VIP', 'Order'), (11, 'F08', 'VIP', 'Not Order'), (11, 'F09', 'VIP', 'Order'), (11, 'F10', 'VIP', 'Order'),
(11, 'G01', 'VIP', 'Not Order'), (11, 'G02', 'VIP', 'Order'), (11, 'G03', 'VIP', 'Order'), (11, 'G04', 'VIP', 'Order'), (11, 'G05', 'VIP', 'Order'),
(11, 'G06', 'VIP', 'Order'), (11, 'G07', 'VIP', 'Not Order'), (11, 'G08', 'VIP', 'Order'), (11, 'G09', 'VIP', 'Order'), (11, 'G10', 'VIP', 'Order'),
(11, 'H01', 'Couple', 'Order'), (11, 'H02', 'Couple', 'Not Order'), (11, 'H03', 'Couple', 'Order'), (11, 'H04', 'Couple', 'Order'), (11, 'H05', 'Couple', 'Not Order');
GO


INSERT INTO Combos (ComboName, ComboPrice)
VALUES 
-- BÁN LẺ
(N'1 Bắp (Size M)', 49000.00),              -- ComboID: 1
(N'1 Bắp (Size L)', 59000.00),              -- ComboID: 2
(N'1 Nước ngọt (Size M)', 19000.00),        -- ComboID: 3
(N'1 Nước ngọt (Size L)', 29000.00),        -- ComboID: 4
(N'1 Nước suối / Trà', 25000.00),           -- ComboID: 5

-- COMBO CƠ BẢN
(N'Combo Solo M (1 Bắp M + 1 Nước M)', 59000.00),    -- ComboID: 6 (Tiết kiệm 9k)
(N'Combo Solo L (1 Bắp L + 1 Nước L)', 79000.00),    -- ComboID: 7 (Tiết kiệm 9k)
(N'Combo Couple M (1 Bắp M + 2 Nước M)', 79000.00),  -- ComboID: 8 (Tiết kiệm 8k)
(N'Combo Couple L (1 Bắp L + 2 Nước L)', 99000.00),  -- ComboID: 9 (Tiết kiệm 18k - Đã sửa giá)
(N'Combo Family M (2 Bắp M + 2 Nước M)', 119000.00), -- ComboID: 10 (Tiết kiệm 17k)
(N'Combo Family L (2 Bắp L + 2 Nước L)', 149000.00), -- ComboID: 11 (Tiết kiệm 27k - Đã sửa giá)

-- COMBO NÂNG CAO (UPSELL & MERCHANDISE)
(N'Combo Snack (1 Bắp L + 2 Nước L + 1 Xúc xích)', 129000.00), -- ComboID: 12
(N'Combo Doraemon (1 Bắp L + 1 Nước L + 1 Ly nhân vật)', 169000.00); -- ComboID: 13
GO



-- ==========================================
-- 3. NẠP LẠI DỮ LIỆU NGƯỜI DÙNG (> 16 TUỔI)
-- ==========================================
INSERT INTO Users (FullName, Email, PasswordHash, PhoneNumber, DOB, Status, Role)
VALUES 
-- Nhóm Quản trị & Nhân viên
(N'Nguyễn Thành An (Admin)', 'admin@movieticket.com', '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', '0912345678', '2004-05-10', 1, 'Admin'),
(N'Nguyễn An (Staff)', 'thanhan1552006@gmail.com', '$2a$11$ql8hFSeayrx09EFzWczD7u0.cxBmkwk4dpNIFGag.zgjICAm75pd6', '0923456789', '1998-05-15', 1, 'Staff'),
                                                      --PasswordHash có nghĩa: An1552006@
(N'Lê Văn Cường (Staff)', 'cuonglv@movieticket.com', '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', '0934567890', '2000-08-20', 1, 'Staff'),

-- Nhóm Khách hàng hợp lệ (Sinh năm 2009 trở về trước -> Đều > 16 tuổi tính tới 2026)
(N'Phạm Minh Hoàng', 'hoangpm@gmail.com', '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', '0945678901', '1990-11-30', 1, 'KhachHang'),
(N'Nguyễn Thị Mai', 'maitn@gmail.com', '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', '0956789012', '2002-03-15', 1, 'KhachHang'),
(N'Lê Thu Hà', 'halt2008@gmail.com', '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', '0978901234', '2008-01-10', 1, 'KhachHang'), 
(N'Trần Tiến Đạt', 'dattr2009@gmail.com', '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', '0967890123', '2009-06-25', 1, 'KhachHang'),

-- Tài khoản khách hàng bị KHÓA (Test chức năng Disable)
(N'Đỗ Hoàng Long (Bị khóa)', 'longdh_locked@gmail.com', '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', '0990123456', '1999-12-25', 0, 'KhachHang');
GO


-- ==========================================
-- NẠP DỮ LIỆU BẢNG ĐƠN HÀNG (Bookings)
-- TotalAmount là snapshot đã được backend tính sẵn tại thời điểm đặt vé
-- ==========================================
INSERT INTO Bookings (UserID, BookingDate, TotalAmount, Status)
VALUES 
-- 1. Đơn hàng thành công (Khách Phạm Minh Hoàng - ID: 4)
-- (Giả lập: Mua 2 vé phim x 85k + 1 Combo L 99k = 269k)
(4, '2026-04-16 14:30:00', 269000.00, 'Confirmed'), 

-- 2. Đơn hàng thành công (Khách Nguyễn Thị Mai - ID: 5)
-- (Giả lập: Mua 1 vé phim IMAX 165k = 165k)
(5, '2026-04-18 09:15:00', 165000.00, 'Confirmed'),

-- 3. Đơn hàng đang chờ thanh toán (Khách Lê Thu Hà - ID: 6)
-- (Giả lập: Mua nhóm, tổng 450k)
(6, '2026-04-20 20:00:00', 450000.00, 'Pending'),

-- 4. Đơn hàng đã hủy (Khách Trần Tiến Đạt - ID: 7)
-- (Khách đặt nhưng không thanh toán trong thời gian quy định)
(7, '2026-05-01 10:45:00', 85000.00, 'Cancelled');
GO

-- ==========================================
-- NẠP DỮ LIỆU CHI TIẾT BẮP NƯỚC (BookingCombos)
-- ==========================================

INSERT INTO BookingCombos (BookingID, ComboID, Quantity, UnitPrice)
VALUES
-- Đơn hàng 1: Mua 1 Combo Couple L (ComboID = 9, Giá thời điểm đó = 99k)
(1, 9, 1, 99000.00),

-- Đơn hàng 3: Mua nhiều loại combo kết hợp
(3, 10, 1, 119000.00), -- 1 Combo Family M (119k)
(3, 6,  1, 59000.00),  -- 1 Combo Solo M (59k)
(3, 3,  1, 19000.00);  -- 1 Nước ngọt M lẻ (19k)
GO


-- ==========================================
-- NẠP DỮ LIỆU CHI TIẾT VÉ (Tickets)
-- Chỉ gán ghế/suất chiếu mẫu, không tính giá vé trong SQL
-- ==========================================

-- Biến tạm để lưu trữ ID phục vụ gán dữ liệu chính xác
DECLARE @ShowtimeWeekendStandard INT, 
        @ShowtimeWeekendIMAX INT, 
        @ShowtimeWeekdayStandard INT;

DECLARE @Seat1_A01 INT, @Seat1_A02 INT,
        @Seat10_E05 INT,
        @Seat5_B01 INT, @Seat5_B02 INT, @Seat5_B03 INT, @Seat5_B04 INT;

-- 1. Lấy thử 1 suất chiếu tiêu chuẩn vào ngày Cuối Tuần (Thứ 7 - 2026-04-18) tại Phòng 1 (Giá 85k)
SELECT TOP 1 @ShowtimeWeekendStandard = ShowtimeID FROM Showtimes 
WHERE RoomID = 1 AND [Date] = '2026-04-18' AND StartTime = '2026-04-18 18:00:00';

-- 2. Lấy 1 suất chiếu IMAX cuối tuần tại Phòng 10
SELECT TOP 1 @ShowtimeWeekendIMAX = ShowtimeID FROM Showtimes 
WHERE RoomID = 10 AND [Date] = '2026-04-19' AND StartTime = '2026-04-19 10:00:00';

-- 3. Lấy 1 suất chiếu tiêu chuẩn ngày thường tại Phòng 5
SELECT TOP 1 @ShowtimeWeekdayStandard = ShowtimeID FROM Showtimes 
WHERE RoomID = 5 AND [Date] = '2026-04-20' AND StartTime = '2026-04-20 08:00:00';

-- 4. Lấy ID các ghế tương ứng của từng phòng để phân phối
SELECT @Seat1_A01 = SeatID FROM Seats WHERE RoomID = 1 AND SeatCode = 'A01';
SELECT @Seat1_A02 = SeatID FROM Seats WHERE RoomID = 1 AND SeatCode = 'A02';

SELECT @Seat10_E05 = SeatID FROM Seats WHERE RoomID = 10 AND SeatCode = 'E05';

SELECT @Seat5_B01 = SeatID FROM Seats WHERE RoomID = 5 AND SeatCode = 'B01';
SELECT @Seat5_B02 = SeatID FROM Seats WHERE RoomID = 5 AND SeatCode = 'B02';
SELECT @Seat5_B03 = SeatID FROM Seats WHERE RoomID = 5 AND SeatCode = 'B03';
SELECT @Seat5_B04 = SeatID FROM Seats WHERE RoomID = 5 AND SeatCode = 'B04';

-- 5. Tiến hành Insert vào bảng Tickets
INSERT INTO Tickets (BookingID, ShowtimeID, SeatID, TicketCode)
VALUES
-- Đơn hàng 1: Mua 2 vé + 1 combo; TotalAmount đã tính sẵn ở Bookings
(1,@ShowtimeWeekendStandard, @Seat1_A01, 'TCK-20260418-001'),
(1,@ShowtimeWeekendStandard, @Seat1_A02, 'TCK-20260418-002'),

-- Đơn hàng 2: Mua 1 vé IMAX; TotalAmount đã tính sẵn ở Bookings
(2, @ShowtimeWeekendIMAX, @Seat10_E05, 'TCK-20260419-001'),

-- Đơn hàng 3: Mua 4 vé + nhiều combo; TotalAmount đã tính sẵn ở Bookings
(3, @ShowtimeWeekdayStandard, @Seat5_B01, 'TCK-20260420-001'),
(3, @ShowtimeWeekdayStandard, @Seat5_B02, 'TCK-20260420-002'),
(3, @ShowtimeWeekdayStandard, @Seat5_B03, 'TCK-20260420-003'),
(3, @ShowtimeWeekdayStandard, @Seat5_B04, 'TCK-20260420-004');
GO

-- ==========================================
--  NẠP DỮ LIỆU PHƯƠNG THỨC THANH TOÁN (PaymentMethods)
-- ==========================================
INSERT INTO PaymentMethods (MethodName)
VALUES 
(N'Ví MoMo'),                  -- MethodID: 1
(N'VNPay (Quét mã QR)'),       -- MethodID: 2
(N'ZaloPay'),                  -- MethodID: 3
(N'Thẻ Tín Dụng/Ghi Nợ (Visa/MasterCard)') -- MethodID: 4
GO

-- ==========================================
-- NẠP DỮ LIỆU LỊCH SỬ GIAO DỊCH (Payments)
-- ==========================================
-- Lưu ý: PaymentDate thường trễ hơn BookingDate từ 1 - 5 phút (thời gian khách nhập mã OTP)

INSERT INTO Payments (BookingID, MethodID, Amount, PaymentDate, Status)
VALUES
-- Đơn hàng 1 (269k): Thanh toán thành công qua MoMo lúc 14:31
(1, 1, 269000.00, '2026-04-16 14:31:15', 'Success'),

-- Đơn hàng 2 (165k): Thanh toán thành công qua VNPay lúc 09:17
(2, 2, 165000.00, '2026-04-18 09:17:22', 'Success'),

-- Đơn hàng 3 (450k): Đang chờ khách quét mã ZaloPay
(3, 3, 450000.00, '2026-04-20 20:00:10', 'Pending'),

-- Đơn hàng 4 (85k): Thanh toán Visa bị lỗi (Failed), dẫn đến đơn bị Cancelled
(4, 4, 85000.00, '2026-05-01 10:46:00', 'Failed');
GO
