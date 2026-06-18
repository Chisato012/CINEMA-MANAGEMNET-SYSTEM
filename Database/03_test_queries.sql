USE MovieTicketDB;
GO
-- câu lệnh truy vấn tổng hợp hoàn chỉnh nhất cho hệ thống:
WITH TicketInfo AS (
    -- 1. Gom nhóm thông tin Phim, Phòng chiếu, Suất chiếu và Ghế theo từng BookingID
    SELECT 
        t.BookingID,
        m.Title,
        r.RoomName,
        sh.StartTime,
        STRING_AGG(s.SeatCode, ', ') AS DanhSachGhe
    FROM Tickets t
    JOIN Showtimes sh ON t.ShowtimeID = sh.ShowtimeID
    JOIN Movies m ON sh.MovieID = m.MovieID
    JOIN Rooms r ON sh.RoomID = r.RoomID
    JOIN Seats s ON t.SeatID = s.SeatID
    GROUP BY t.BookingID, m.Title, r.RoomName, sh.StartTime
),

ComboInfo AS (
    -- 2. Gom nhóm các Combo khách đã mua theo từng BookingID
    SELECT 
        bc.BookingID,
        STRING_AGG(CAST(bc.Quantity AS VARCHAR) + 'x ' + c.ComboName, N', ') AS DanhSachCombo
    FROM BookingCombos bc
    JOIN Combos c ON bc.ComboID = c.ComboID
    GROUP BY bc.BookingID
)
-- 3. Nối bảng Bookings với các bảng tạm và thông tin Thanh toán, Người dùng
SELECT 
    b.BookingID AS N'Mã Đơn',
    u.FullName AS N'Khách Hàng',
    u.PhoneNumber AS N'SĐT',
    ti.Title AS N'Tên Phim',
    ti.RoomName AS N'Phòng Chiếu',
    CONVERT(VARCHAR(16), ti.StartTime, 120) AS N'Giờ Chiếu',
    ti.DanhSachGhe AS N'Ghế Đã Đặt',
    ISNULL(ci.DanhSachCombo, N'Không mua combo') AS N'Combo Kèm Theo',
    FORMAT(b.TotalAmount, 'N0') + ' đ' AS N'Tổng Tiền',
    ISNULL(pm.MethodName, N'Chưa chọn') AS N'Phương Thức TT',
    ISNULL(p.Status, b.Status) AS N'Trạng Thái TT',
    CONVERT(VARCHAR(16), b.BookingDate, 120) AS N'Ngày Tạo Đơn'
FROM Bookings b
JOIN Users u ON b.UserID = u.UserID
LEFT JOIN TicketInfo ti ON b.BookingID = ti.BookingID
LEFT JOIN ComboInfo ci ON b.BookingID = ci.BookingID
LEFT JOIN Payments p ON b.BookingID = p.BookingID
LEFT JOIN PaymentMethods pm ON p.MethodID = pm.MethodID
ORDER BY b.BookingDate DESC;
GO