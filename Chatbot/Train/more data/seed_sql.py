"""
Seed dữ liệu mô phỏng cho dự án Movie Ticket + Recommendation.

Mục tiêu:
- Nạp thêm nhiều Users, Showtimes, Bookings, Tickets, Payments vào SQL Server.
- Nạp dữ liệu phụ cho chatbot/recommendation vào MongoDB:
  movie_enrichments, user_interactions, chat_sessions, recommendation_feedback.

Cài thư viện:
    pip install pyodbc
    pip install pymongo
"""

from __future__ import annotations

import argparse
import random
import string
import sys
from collections import defaultdict
from dataclasses import dataclass
from datetime import date, datetime, timedelta
from decimal import Decimal
from typing import Any

try:
    import pyodbc
except ImportError:
    print("Thiếu thư viện pyodbc. Cài bằng: pip install pyodbc")
    raise

try:
    from pymongo import MongoClient, UpdateOne
except ImportError:
    MongoClient = None
    UpdateOne = None


# =========================================================
# Config mặc định
# =========================================================

DEFAULT_SQL_SERVER = r"(localdb)\MSSQLLocalDB"
DEFAULT_SQL_DATABASE = "MovieTicketDB"
DEFAULT_ODBC_DRIVER = "ODBC Driver 17 for SQL Server"

DEFAULT_MONGO_URI = "mongodb://localhost:27017"
DEFAULT_MONGO_DB = "MovieTicketRecommendationDB"

PASSWORD_HASH_DEMO = "DEMO_HASH_NOT_FOR_PRODUCTION"


# =========================================================
# Data giả lập tiếng Việt
# =========================================================

LAST_NAMES = [
    "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Huỳnh", "Phan", "Vũ", "Võ",
    "Đặng", "Bùi", "Đỗ", "Hồ", "Ngô", "Dương", "Lý", "Mai", "Tô", "Đinh"
]

MIDDLE_NAMES = [
    "Văn", "Thị", "Minh", "Hoàng", "Gia", "Thanh", "Quốc", "Anh", "Ngọc",
    "Bảo", "Khánh", "Tuấn", "Hải", "Phương", "Nhật", "Kim", "Hữu", "Đức"
]

FIRST_NAMES = [
    "An", "Anh", "Bình", "Châu", "Chi", "Dũng", "Duy", "Giang", "Hà", "Hải",
    "Hân", "Hiếu", "Hùng", "Huy", "Khang", "Khánh", "Lan", "Linh", "Long",
    "Mai", "Minh", "Nam", "Ngân", "Nhi", "Phúc", "Quân", "Quang", "Sơn",
    "Thảo", "Thư", "Trang", "Trí", "Tú", "Tuấn", "Vy", "Yến"
]

SEARCH_QUERIES = [
    "phim hay hôm nay", "phim hoạt hình gia đình", "phim hành động", "phim kinh dị",
    "phim tình cảm Nhật", "phim khoa học viễn tưởng", "phim nhẹ nhàng", "phim cho cuối tuần",
    "phim giống Doraemon", "phim cảm động", "phim phù hợp đi cùng bạn bè"
]

MOODS = [
    "vui vẻ", "buồn", "căng thẳng", "thư giãn", "hào hứng", "muốn xem nhẹ nhàng",
    "muốn xem gay cấn", "đi cùng gia đình", "đi cùng bạn bè", "đi hẹn hò"
]

GENRE_KEYWORD_MAP = {
    "Hoạt hình": ["hoạt hình", "gia đình", "trẻ em", "vui vẻ", "phiêu lưu", "tuổi thơ"],
    "Gia đình": ["gia đình", "ấm áp", "nhẹ nhàng", "phù hợp nhiều độ tuổi", "cuối tuần"],
    "Cảm động": ["cảm động", "chữa lành", "tình cảm", "sâu lắng", "nhẹ nhàng"],
    "Tình cảm": ["tình yêu", "lãng mạn", "cảm xúc", "hẹn hò", "nhẹ nhàng"],
    "Hành động": ["hành động", "gay cấn", "kịch tính", "mạnh mẽ", "đối đầu"],
    "Khoa học viễn tưởng": ["tương lai", "công nghệ", "vũ trụ", "sinh tồn", "giả tưởng"],
    "Kinh dị": ["kinh dị", "bí ẩn", "rùng rợn", "tâm linh", "căng thẳng"],
    "Trinh thám": ["điều tra", "bí ẩn", "phá án", "suy luận", "tội phạm"],
}

GENRE_MOOD_MAP = {
    "Hoạt hình": ["vui vẻ", "thư giãn", "đi cùng gia đình"],
    "Gia đình": ["thư giãn", "đi cùng gia đình", "muốn xem nhẹ nhàng"],
    "Cảm động": ["buồn", "muốn xem nhẹ nhàng", "thư giãn"],
    "Tình cảm": ["đi hẹn hò", "buồn", "muốn xem nhẹ nhàng"],
    "Hành động": ["hào hứng", "muốn xem gay cấn", "đi cùng bạn bè"],
    "Khoa học viễn tưởng": ["hào hứng", "muốn xem gay cấn", "căng thẳng"],
    "Kinh dị": ["muốn xem gay cấn", "căng thẳng", "đi cùng bạn bè"],
    "Trinh thám": ["muốn xem gay cấn", "căng thẳng", "hào hứng"],
}


# =========================================================
# Helper
# =========================================================

@dataclass
class Movie:
    movie_id: int
    title: str
    duration: int
    age_rating: str
    genres: list[str]
    country_name: str
    language_name: str


@dataclass
class Showtime:
    showtime_id: int
    movie_id: int
    room_id: int
    start_time: datetime
    end_time: datetime
    base_price: Decimal


@dataclass
class Seat:
    seat_id: int
    room_id: int
    seat_code: str
    seat_type: str


def now_local() -> datetime:
    return datetime.now().replace(microsecond=0)


def random_full_name() -> str:
    return f"{random.choice(LAST_NAMES)} {random.choice(MIDDLE_NAMES)} {random.choice(FIRST_NAMES)}"


def random_phone(i: int) -> str:
    # Số demo, tránh trùng tương đối.
    return "09" + str(10000000 + i)[-8:]


def random_dob() -> date:
    # Người dùng >= 16 tuổi để phù hợp mô tả DB.
    start = date(1970, 1, 1)
    end = date(2008, 12, 31)
    delta = (end - start).days
    return start + timedelta(days=random.randint(0, delta))


def random_code(length: int = 8) -> str:
    alphabet = string.ascii_uppercase + string.digits
    return "".join(random.choice(alphabet) for _ in range(length))


def chunks(items: list[Any], size: int):
    for i in range(0, len(items), size):
        yield items[i:i + size]


def to_decimal_money(value: float) -> Decimal:
    return Decimal(str(round(value, 2)))


# =========================================================
# SQL Server
# =========================================================

class SqlSeeder:
    def __init__(
        self,
        server: str,
        database: str,
        driver: str,
        use_windows_auth: bool = True,
        username: str = "sa",
        password: str = "",
    ):
        self.server = server
        self.database = database
        self.driver = driver
        self.use_windows_auth = use_windows_auth
        self.username = username
        self.password = password
        self.connection: pyodbc.Connection | None = None

    def connection_string(self) -> str:
        if self.use_windows_auth:
            return (
                f"DRIVER={{{self.driver}}};"
                f"SERVER={self.server};"
                f"DATABASE={self.database};"
                "Trusted_Connection=yes;"
                "TrustServerCertificate=yes;"
            )
        return (
            f"DRIVER={{{self.driver}}};"
            f"SERVER={self.server};"
            f"DATABASE={self.database};"
            f"UID={self.username};"
            f"PWD={self.password};"
            "TrustServerCertificate=yes;"
        )

    def connect(self):
        print(f"Kết nối SQL Server: {self.server} / {self.database}")
        self.connection = pyodbc.connect(self.connection_string())
        self.connection.autocommit = False
        return self.connection

    @property
    def conn(self) -> pyodbc.Connection:
        if self.connection is None:
            raise RuntimeError("Chưa connect SQL Server")
        return self.connection

    def close(self):
        if self.connection:
            self.connection.close()

    def reset_sim_data(self):
        print("Đang xóa dữ liệu SQL mô phỏng do script tạo: sim_user_%@demo.local")
        cursor = self.conn.cursor()
        cursor.execute("""
            SELECT BookingID
            FROM Bookings
            WHERE UserID IN (
                SELECT UserID FROM Users WHERE Email LIKE 'sim_user_%@demo.local'
            )
        """)
        booking_ids = [row.BookingID for row in cursor.fetchall()]

        if booking_ids:
            for batch in chunks(booking_ids, 1000):
                placeholders = ",".join("?" for _ in batch)
                cursor.execute(f"DELETE FROM Payments WHERE BookingID IN ({placeholders})", batch)
                cursor.execute(f"DELETE FROM BookingCombos WHERE BookingID IN ({placeholders})", batch)
                cursor.execute(f"DELETE FROM Tickets WHERE BookingID IN ({placeholders})", batch)
                cursor.execute(f"DELETE FROM Bookings WHERE BookingID IN ({placeholders})", batch)

        cursor.execute("DELETE FROM Users WHERE Email LIKE 'sim_user_%@demo.local'")
        self.conn.commit()
        print(f"Đã xóa {len(booking_ids)} đơn mô phỏng và user mô phỏng.")

    def fetch_movies(self) -> list[Movie]:
        cursor = self.conn.cursor()
        cursor.execute("""
            SELECT
                m.MovieID,
                m.Title,
                m.Duration,
                ISNULL(m.AgeRating, N'') AS AgeRating,
                ISNULL(c.CountryName, N'') AS CountryName,
                ISNULL(l.LanguageName, N'') AS LanguageName,
                ISNULL(g.Genres, N'') AS Genres
            FROM Movies m
            LEFT JOIN Countries c ON m.CountryID = c.CountryID
            LEFT JOIN Languages l ON m.LanguageID = l.LanguageID
            OUTER APPLY (
                SELECT STRING_AGG(x.Name, N'|') AS Genres
                FROM (
                    SELECT DISTINCT ge.Name
                    FROM MovieGenres mg
                    JOIN Genres ge ON mg.GenreID = ge.GenreID
                    WHERE mg.MovieID = m.MovieID
                ) x
            ) g
            ORDER BY m.MovieID
        """)
        movies: list[Movie] = []
        for row in cursor.fetchall():
            genres = [g for g in str(row.Genres).split("|") if g]
            movies.append(Movie(
                movie_id=int(row.MovieID),
                title=str(row.Title),
                duration=int(row.Duration),
                age_rating=str(row.AgeRating),
                country_name=str(row.CountryName),
                language_name=str(row.LanguageName),
                genres=genres,
            ))
        return movies

    def ensure_basic_lookup_data(self):
        """Chỉ insert nếu bảng trống hoặc thiếu dữ liệu tối thiểu."""
        cursor = self.conn.cursor()

        cursor.execute("SELECT COUNT(*) FROM SeatTypePricing")
        if cursor.fetchone()[0] == 0:
            cursor.executemany(
                "INSERT INTO SeatTypePricing (SeatType, Multiplier) VALUES (?, ?)",
                [("Regular", Decimal("1.00")), ("VIP", Decimal("1.30")), ("Couple", Decimal("2.10"))]
            )

        cursor.execute("SELECT COUNT(*) FROM PaymentMethods")
        if cursor.fetchone()[0] == 0:
            cursor.executemany(
                "INSERT INTO PaymentMethods (MethodName) VALUES (?)",
                [("VNPay",), ("MoMo",), ("Tiền mặt",), ("Visa",)]
            )

        cursor.execute("SELECT COUNT(*) FROM Combos")
        if cursor.fetchone()[0] == 0:
            cursor.executemany(
                "INSERT INTO Combos (ComboName, ComboPrice) VALUES (?, ?)",
                [
                    ("Combo bắp nước nhỏ", Decimal("45000")),
                    ("Combo bắp nước lớn", Decimal("75000")),
                    ("Combo đôi", Decimal("120000")),
                ]
            )

        cursor.execute("SELECT COUNT(*) FROM Rooms")
        if cursor.fetchone()[0] == 0:
            cursor.executemany("INSERT INTO Rooms (RoomName) VALUES (?)", [(f"Phòng {i}",) for i in range(1, 6)])

        self.conn.commit()

        cursor.execute("SELECT COUNT(*) FROM Seats")
        if cursor.fetchone()[0] == 0:
            cursor.execute("SELECT RoomID FROM Rooms ORDER BY RoomID")
            room_ids = [int(r.RoomID) for r in cursor.fetchall()]
            seat_rows = []
            for room_id in room_ids:
                for row_letter in ["A", "B", "C", "D", "E", "F", "G", "H"]:
                    for number in range(1, 11):
                        seat_code = f"{row_letter}{number:02d}"
                        if row_letter in ["G", "H"]:
                            seat_type = "VIP"
                        elif row_letter == "F" and number in [9, 10]:
                            seat_type = "Couple"
                        else:
                            seat_type = "Regular"
                        seat_rows.append((room_id, seat_code, seat_type))
            cursor.executemany(
                "INSERT INTO Seats (RoomID, SeatCode, SeatType) VALUES (?, ?, ?)",
                seat_rows
            )
            self.conn.commit()

    def fetch_rooms(self) -> list[int]:
        cursor = self.conn.cursor()
        cursor.execute("SELECT RoomID FROM Rooms ORDER BY RoomID")
        return [int(row.RoomID) for row in cursor.fetchall()]

    def fetch_seats_by_room(self) -> dict[int, list[Seat]]:
        cursor = self.conn.cursor()
        cursor.execute("SELECT SeatID, RoomID, SeatCode, SeatType FROM Seats ORDER BY RoomID, SeatCode")
        result: dict[int, list[Seat]] = defaultdict(list)
        for row in cursor.fetchall():
            seat = Seat(int(row.SeatID), int(row.RoomID), str(row.SeatCode), str(row.SeatType))
            result[seat.room_id].append(seat)
        return result

    def fetch_seat_multipliers(self) -> dict[str, Decimal]:
        cursor = self.conn.cursor()
        cursor.execute("SELECT SeatType, Multiplier FROM SeatTypePricing")
        return {str(row.SeatType): Decimal(str(row.Multiplier)) for row in cursor.fetchall()}

    def fetch_payment_methods(self) -> list[int]:
        cursor = self.conn.cursor()
        cursor.execute("SELECT MethodID FROM PaymentMethods ORDER BY MethodID")
        return [int(row.MethodID) for row in cursor.fetchall()]

    def fetch_combos(self) -> list[dict[str, Any]]:
        cursor = self.conn.cursor()
        cursor.execute("SELECT ComboID, ComboName, ComboPrice FROM Combos ORDER BY ComboID")
        return [
            {"combo_id": int(row.ComboID), "name": str(row.ComboName), "price": Decimal(str(row.ComboPrice))}
            for row in cursor.fetchall()
        ]

    def create_users(self, count: int, run_id: str) -> list[int]:
        print(f"Đang tạo {count} khách hàng mô phỏng...")
        cursor = self.conn.cursor()
        cursor.fast_executemany = True

        rows = []
        for i in range(1, count + 1):
            rows.append((
                random_full_name(),
                f"sim_user_{run_id}_{i:06d}@demo.local",
                PASSWORD_HASH_DEMO,
                random_phone(i),
                random_dob(),
                1 if random.random() < 0.98 else 0,
                "KhachHang",
            ))

        for batch in chunks(rows, 1000):
            cursor.executemany("""
                INSERT INTO Users (FullName, Email, PasswordHash, PhoneNumber, DOB, Status, [Role])
                VALUES (?, ?, ?, ?, ?, ?, ?)
            """, batch)
        self.conn.commit()

        cursor.execute(
            "SELECT UserID FROM Users WHERE Email LIKE ? ORDER BY UserID",
            f"sim_user_{run_id}_%@demo.local",
        )
        user_ids = [int(row.UserID) for row in cursor.fetchall()]
        print(f"Đã tạo {len(user_ids)} khách hàng.")
        return user_ids

    def create_showtimes(self, movies: list[Movie], rooms: list[int], days_past: int, days_future: int) -> list[Showtime]:
        print(f"Đang tạo suất chiếu từ -{days_past} đến +{days_future} ngày...")
        cursor = self.conn.cursor()
        cursor.fast_executemany = True

        today = now_local().date()
        start_date = today - timedelta(days=days_past)
        end_date = today + timedelta(days=days_future)

        start_hours = [8, 10, 12, 14, 16, 18, 20, 22]
        base_prices = [70000, 75000, 80000, 90000, 100000, 110000, 120000]

        rows = []
        current = start_date
        while current <= end_date:
            # Cuối tuần tạo nhiều suất hơn.
            weekend = current.weekday() >= 5
            movies_today = random.sample(movies, k=min(len(movies), random.randint(4, len(movies))))

            for movie in movies_today:
                show_count = random.randint(2, 4) if weekend else random.randint(1, 3)
                for _ in range(show_count):
                    room_id = random.choice(rooms)
                    hour = random.choice(start_hours)
                    minute = random.choice([0, 15, 30, 45])
                    start_time = datetime.combine(current, datetime.min.time()).replace(hour=hour, minute=minute)
                    end_time = start_time + timedelta(minutes=movie.duration + 15)
                    base_price = Decimal(str(random.choice(base_prices)))
                    rows.append((movie.movie_id, room_id, current, start_time, end_time, base_price))
            current += timedelta(days=1)

        for batch in chunks(rows, 1000):
            cursor.executemany("""
                INSERT INTO Showtimes (MovieID, RoomID, [Date], StartTime, EndTime, BasePrice)
                VALUES (?, ?, ?, ?, ?, ?)
            """, batch)
        self.conn.commit()

        print(f"Đã tạo thêm {len(rows)} suất chiếu.")
        return self.fetch_showtimes(start_date, end_date)

    def fetch_showtimes(self, start_date: date, end_date: date) -> list[Showtime]:
        cursor = self.conn.cursor()
        cursor.execute("""
            SELECT ShowtimeID, MovieID, RoomID, StartTime, EndTime, BasePrice
            FROM Showtimes
            WHERE [Date] BETWEEN ? AND ?
            ORDER BY ShowtimeID
        """, start_date, end_date)
        result = []
        for row in cursor.fetchall():
            result.append(Showtime(
                showtime_id=int(row.ShowtimeID),
                movie_id=int(row.MovieID),
                room_id=int(row.RoomID),
                start_time=row.StartTime,
                end_time=row.EndTime,
                base_price=Decimal(str(row.BasePrice)),
            ))
        return result

    def fetch_used_seats_by_showtime(self) -> dict[int, set[int]]:
        cursor = self.conn.cursor()
        cursor.execute("SELECT ShowtimeID, SeatID FROM Tickets")
        used: dict[int, set[int]] = defaultdict(set)
        for row in cursor.fetchall():
            used[int(row.ShowtimeID)].add(int(row.SeatID))
        return used

    def create_bookings_and_related_data(
        self,
        booking_count: int,
        user_ids: list[int],
        showtimes: list[Showtime],
        seats_by_room: dict[int, list[Seat]],
        seat_multipliers: dict[str, Decimal],
        payment_method_ids: list[int],
        combos: list[dict[str, Any]],
        run_id: str,
    ) -> list[dict[str, Any]]:
        print(f"Đang tạo {booking_count} đơn đặt vé mô phỏng...")
        cursor = self.conn.cursor()
        used_seats = self.fetch_used_seats_by_showtime()
        generated_events: list[dict[str, Any]] = []

        created = 0
        attempts = 0
        max_attempts = booking_count * 10

        status_choices = ["Confirmed", "Pending", "Cancelled"]
        status_weights = [0.78, 0.12, 0.10]

        while created < booking_count and attempts < max_attempts:
            attempts += 1
            user_id = random.choice(user_ids)
            showtime = random.choice(showtimes)
            seats = seats_by_room.get(showtime.room_id, [])
            if not seats:
                continue

            available = [s for s in seats if s.seat_id not in used_seats[showtime.showtime_id]]
            ticket_quantity = random.choices([1, 2, 3, 4], weights=[0.40, 0.38, 0.15, 0.07], k=1)[0]
            if len(available) < ticket_quantity:
                continue

            selected_seats = random.sample(available, ticket_quantity)
            ticket_total = Decimal("0")
            for seat in selected_seats:
                multiplier = seat_multipliers.get(seat.seat_type, Decimal("1.0"))
                ticket_total += showtime.base_price * multiplier

            selected_combos = []
            combo_total = Decimal("0")
            if combos and random.random() < 0.35:
                for combo in random.sample(combos, k=random.randint(1, min(2, len(combos)))):
                    qty = random.choices([1, 2, 3], weights=[0.72, 0.22, 0.06], k=1)[0]
                    selected_combos.append((combo, qty))
                    combo_total += combo["price"] * qty

            total_amount = ticket_total + combo_total
            status = random.choices(status_choices, weights=status_weights, k=1)[0]

            max_days_before = min(30, max(1, (showtime.start_time.date() - date(2024, 1, 1)).days))
            booking_date = showtime.start_time - timedelta(
                days=random.randint(0, max_days_before),
                hours=random.randint(0, 8),
                minutes=random.randint(0, 59),
            )
            if booking_date > now_local() and showtime.start_time > now_local():
                # Đơn tương lai thì vẫn có thể đặt từ hiện tại hoặc vài ngày trước hiện tại.
                booking_date = now_local() - timedelta(days=random.randint(0, 7), minutes=random.randint(0, 300))

            cursor.execute("""
                INSERT INTO Bookings (UserID, BookingDate, TotalAmount, Status)
                OUTPUT INSERTED.BookingID
                VALUES (?, ?, ?, ?)
            """, user_id, booking_date, total_amount, status)
            booking_id = int(cursor.fetchone()[0])

            for seat in selected_seats:
                used_seats[showtime.showtime_id].add(seat.seat_id)
                ticket_code = f"SIM-{run_id}-{booking_id}-{seat.seat_id}-{random_code(5)}"
                cursor.execute("""
                    INSERT INTO Tickets (BookingID, ShowtimeID, SeatID, TicketCode)
                    VALUES (?, ?, ?, ?)
                """, booking_id, showtime.showtime_id, seat.seat_id, ticket_code)

            for combo, qty in selected_combos:
                cursor.execute("""
                    INSERT INTO BookingCombos (BookingID, ComboID, Quantity, UnitPrice)
                    VALUES (?, ?, ?, ?)
                """, booking_id, combo["combo_id"], qty, combo["price"])

            method_id = random.choice(payment_method_ids)
            if status == "Confirmed":
                payment_status = "Success"
            elif status == "Cancelled":
                payment_status = random.choice(["Failed", "Pending"])
            else:
                payment_status = "Pending"

            payment_date = booking_date + timedelta(minutes=random.randint(1, 20))
            cursor.execute("""
                INSERT INTO Payments (BookingID, MethodID, Amount, PaymentDate, Status)
                VALUES (?, ?, ?, ?, ?)
            """, booking_id, method_id, total_amount, payment_date, payment_status)

            # Metadata để nạp MongoDB.
            generated_events.append({
                "bookingId": booking_id,
                "userId": user_id,
                "movieId": showtime.movie_id,
                "showtimeId": showtime.showtime_id,
                "bookingDate": booking_date,
                "showtimeStartTime": showtime.start_time,
                "status": status,
                "ticketQuantity": ticket_quantity,
                "totalAmount": float(total_amount),
            })

            created += 1
            if created % 1000 == 0:
                self.conn.commit()
                print(f"  Đã tạo {created}/{booking_count} đơn...")

        self.conn.commit()
        print(f"Đã tạo {created} đơn đặt vé.")
        if created < booking_count:
            print("Cảnh báo: Không tạo đủ đơn vì có thể hết ghế trong các suất chiếu.")
        return generated_events


# =========================================================
# MongoDB
# =========================================================

class MongoRecommendationSeeder:
    def __init__(self, uri: str, db_name: str):
        if MongoClient is None:
            raise ImportError("Thiếu pymongo. Cài bằng: pip install pymongo")
        self.uri = uri
        self.db_name = db_name
        self.client = MongoClient(uri)
        self.db = self.client[db_name]

    def close(self):
        self.client.close()

    def reset_sim_data(self):
        print(f"Đang xóa dữ liệu MongoDB trong database {self.db_name}...")
        for collection in ["movie_enrichments", "user_interactions", "chat_sessions", "recommendation_feedback"]:
            self.db[collection].delete_many({})
        print("Đã xóa dữ liệu MongoDB mô phỏng.")

    def create_indexes(self):
        self.db.movie_enrichments.create_index("movieId", unique=True)
        self.db.user_interactions.create_index([("userId", 1), ("movieId", 1), ("createdAt", -1)])
        self.db.user_interactions.create_index([("sessionId", 1), ("createdAt", 1)])
        self.db.chat_sessions.create_index("sessionId", unique=True)
        self.db.chat_sessions.create_index([("userId", 1), ("createdAt", -1)])
        self.db.recommendation_feedback.create_index([("userId", 1), ("createdAt", -1)])

    def seed_movie_enrichments(self, movies: list[Movie]):
        print("Đang nạp MongoDB: movie_enrichments...")
        operations = []
        for movie in movies:
            keywords = set()
            moods = set()
            themes = set()

            for genre in movie.genres:
                for kw in GENRE_KEYWORD_MAP.get(genre, []):
                    keywords.add(kw)
                    themes.add(kw)
                for mood in GENRE_MOOD_MAP.get(genre, []):
                    moods.add(mood)

            if not keywords:
                keywords.update(["phim chiếu rạp", "giải trí", "cuối tuần"])
            if not moods:
                moods.update(["thư giãn", "đi cùng bạn bè"])

            target_audience = ["người lớn", "bạn bè"]
            if "Gia đình" in movie.genres or "Hoạt hình" in movie.genres:
                target_audience = ["gia đình", "trẻ em", "bạn bè"]
            if movie.age_rating.upper() in ["T18", "18+"]:
                target_audience = ["người trưởng thành"]

            doc = {
                "movieId": movie.movie_id,
                "title": movie.title,
                "keywords": sorted(keywords),
                "moods": sorted(moods),
                "themes": sorted(themes),
                "targetAudience": target_audience,
                "countryName": movie.country_name,
                "languageName": movie.language_name,
                "extraDescription": f"{movie.title} phù hợp với các nhu cầu như {', '.join(sorted(moods)[:3])}.",
                "source": "simulation_seed",
                "updatedAt": now_local(),
            }
            operations.append(UpdateOne({"movieId": movie.movie_id}, {"$set": doc}, upsert=True))

        if operations:
            self.db.movie_enrichments.bulk_write(operations)
        print(f"Đã upsert {len(operations)} movie_enrichments.")

    def seed_interactions_from_bookings(self, booking_events: list[dict[str, Any]], run_id: str):
        print("Đang nạp MongoDB: user_interactions từ booking SQL...")
        docs = []
        count = 0

        for event in booking_events:
            session_id = f"sim_session_{run_id}_{event['bookingId']}"
            base_time = event["bookingDate"] - timedelta(minutes=random.randint(5, 60))

            docs.append({
                "userId": event["userId"],
                "sessionId": session_id,
                "movieId": event["movieId"],
                "eventType": "ViewDetail",
                "weight": 1.0,
                "source": "web",
                "createdAt": base_time,
            })

            if random.random() < 0.45:
                docs.append({
                    "userId": event["userId"],
                    "sessionId": session_id,
                    "movieId": event["movieId"],
                    "eventType": "ClickTrailer",
                    "weight": 1.5,
                    "source": "web",
                    "createdAt": base_time + timedelta(minutes=random.randint(1, 5)),
                })

            if event["status"] == "Confirmed":
                docs.append({
                    "userId": event["userId"],
                    "sessionId": session_id,
                    "movieId": event["movieId"],
                    "eventType": "BookTicket",
                    "weight": 5.0,
                    "ticketQuantity": event["ticketQuantity"],
                    "bookingId": event["bookingId"],
                    "showtimeId": event["showtimeId"],
                    "source": "sql_booking",
                    "createdAt": event["bookingDate"],
                })

            if len(docs) >= 5000:
                self.db.user_interactions.insert_many(docs)
                count += len(docs)
                docs.clear()
                print(f"  Đã nạp {count} interactions...")

        if docs:
            self.db.user_interactions.insert_many(docs)
            count += len(docs)

        print(f"Đã nạp {count} user_interactions.")

    def seed_extra_browsing_interactions(self, user_ids: list[int], movies: list[Movie], run_id: str, count: int):
        print(f"Đang nạp thêm {count} interactions dạng xem/tìm kiếm...")
        docs = []
        start_time = now_local() - timedelta(days=90)
        movie_ids = [m.movie_id for m in movies]

        for i in range(count):
            user_id = random.choice(user_ids)
            movie_id = random.choice(movie_ids)
            created_at = start_time + timedelta(minutes=random.randint(0, 90 * 24 * 60))
            event_type = random.choices(
                ["Search", "ViewDetail", "ClickTrailer", "Favorite"],
                weights=[0.25, 0.50, 0.20, 0.05],
                k=1,
            )[0]
            weight = {"Search": 0.5, "ViewDetail": 1.0, "ClickTrailer": 1.5, "Favorite": 3.0}[event_type]
            docs.append({
                "userId": user_id,
                "sessionId": f"sim_browse_{run_id}_{user_id}_{random.randint(1, 99999)}",
                "movieId": movie_id,
                "eventType": event_type,
                "query": random.choice(SEARCH_QUERIES) if event_type == "Search" else None,
                "weight": weight,
                "source": "simulation_seed",
                "createdAt": created_at,
            })

            if len(docs) >= 5000:
                self.db.user_interactions.insert_many(docs)
                docs.clear()

        if docs:
            self.db.user_interactions.insert_many(docs)
        print("Đã nạp xong browsing interactions.")

    def seed_chat_sessions_and_feedback(self, user_ids: list[int], movies: list[Movie], run_id: str, session_count: int):
        print(f"Đang nạp {session_count} chat_sessions và recommendation_feedback...")
        movie_ids = [m.movie_id for m in movies]
        title_by_id = {m.movie_id: m.title for m in movies}
        sessions = []
        feedbacks = []

        for i in range(1, session_count + 1):
            user_id = random.choice(user_ids)
            session_id = f"sim_chat_{run_id}_{i:06d}"
            mood = random.choice(MOODS)
            recommended = random.sample(movie_ids, k=min(5, len(movie_ids)))
            clicked = random.choice(recommended) if random.random() < 0.55 else None
            created_at = now_local() - timedelta(days=random.randint(0, 90), minutes=random.randint(0, 1440))

            sessions.append({
                "sessionId": session_id,
                "userId": user_id,
                "mood": mood,
                "intent": "recommend_by_mood",
                "messages": [
                    {
                        "role": "user",
                        "content": f"Tôi đang {mood}, gợi ý cho tôi vài phim phù hợp được không?",
                        "createdAt": created_at,
                    },
                    {
                        "role": "assistant",
                        "content": "Mình gợi ý một số phim phù hợp với tâm trạng và sở thích của bạn.",
                        "recommendedMovieIds": recommended,
                        "createdAt": created_at + timedelta(seconds=random.randint(2, 8)),
                    }
                ],
                "recommendedMovieIds": recommended,
                "createdAt": created_at,
                "source": "simulation_seed",
            })

            feedbacks.append({
                "userId": user_id,
                "sessionId": session_id,
                "query": f"Tôi đang {mood}, gợi ý phim phù hợp",
                "mood": mood,
                "recommendedMovieIds": recommended,
                "recommendedMovieTitles": [title_by_id[mid] for mid in recommended],
                "clickedMovieId": clicked,
                "ignoredMovieIds": [mid for mid in recommended if mid != clicked],
                "feedbackType": "clicked" if clicked else "ignored_all",
                "createdAt": created_at + timedelta(minutes=random.randint(1, 10)),
                "source": "simulation_seed",
            })

            if len(sessions) >= 1000:
                self.db.chat_sessions.insert_many(sessions)
                self.db.recommendation_feedback.insert_many(feedbacks)
                sessions.clear()
                feedbacks.clear()

        if sessions:
            self.db.chat_sessions.insert_many(sessions)
            self.db.recommendation_feedback.insert_many(feedbacks)

        print("Đã nạp xong chat_sessions và recommendation_feedback.")


# =========================================================
# Main
# =========================================================

def parse_args():
    parser = argparse.ArgumentParser(description="Seed SQL Server + MongoDB simulation data")

    parser.add_argument("--sql-server", default=DEFAULT_SQL_SERVER)
    parser.add_argument("--sql-database", default=DEFAULT_SQL_DATABASE)
    parser.add_argument("--odbc-driver", default=DEFAULT_ODBC_DRIVER)
    parser.add_argument("--sql-auth", choices=["windows", "sql"], default="windows")
    parser.add_argument("--sql-username", default="sa")
    parser.add_argument("--sql-password", default="")

    parser.add_argument("--mongo-uri", default=DEFAULT_MONGO_URI)
    parser.add_argument("--mongo-db", default=DEFAULT_MONGO_DB)
    parser.add_argument("--skip-mongo", action="store_true")
    parser.add_argument("--skip-sql", action="store_true")

    parser.add_argument("--users", type=int, default=5000)
    parser.add_argument("--bookings", type=int, default=20000)
    parser.add_argument("--days-past", type=int, default=90)
    parser.add_argument("--days-future", type=int, default=45)
    parser.add_argument("--extra-interactions", type=int, default=30000)
    parser.add_argument("--chat-sessions", type=int, default=3000)

    parser.add_argument("--reset-sim-data", action="store_true", help="Xóa data mô phỏng cũ rồi thoát")
    parser.add_argument("--seed", type=int, default=42)

    return parser.parse_args()


def main():
    args = parse_args()
    random.seed(args.seed)
    run_id = now_local().strftime("%Y%m%d%H%M%S")

    sql = None
    mongo = None

    try:
        if not args.skip_sql:
            sql = SqlSeeder(
                server=args.sql_server,
                database=args.sql_database,
                driver=args.odbc_driver,
                use_windows_auth=args.sql_auth == "windows",
                username=args.sql_username,
                password=args.sql_password,
            )
            sql.connect()

        if not args.skip_mongo:
            if MongoClient is None:
                print("Thiếu pymongo. Cài bằng: pip install pymongo")
                sys.exit(1)
            mongo = MongoRecommendationSeeder(args.mongo_uri, args.mongo_db)
            mongo.create_indexes()

        if args.reset_sim_data:
            if sql:
                sql.reset_sim_data()
            if mongo:
                mongo.reset_sim_data()
            return

        movies: list[Movie] = []
        user_ids: list[int] = []
        booking_events: list[dict[str, Any]] = []

        if sql:
            sql.ensure_basic_lookup_data()
            movies = sql.fetch_movies()
            if not movies:
                raise RuntimeError("SQL chưa có phim. Hãy chạy file seed phim cơ bản trước.")

            rooms = sql.fetch_rooms()
            seats_by_room = sql.fetch_seats_by_room()
            seat_multipliers = sql.fetch_seat_multipliers()
            payment_methods = sql.fetch_payment_methods()
            combos = sql.fetch_combos()

            if not rooms or not seats_by_room:
                raise RuntimeError("SQL chưa có Rooms/Seats. Script đã thử tạo nhưng vẫn thiếu dữ liệu.")
            if not payment_methods:
                raise RuntimeError("SQL chưa có PaymentMethods.")

            user_ids = sql.create_users(args.users, run_id)
            showtimes = sql.create_showtimes(movies, rooms, args.days_past, args.days_future)
            booking_events = sql.create_bookings_and_related_data(
                booking_count=args.bookings,
                user_ids=user_ids,
                showtimes=showtimes,
                seats_by_room=seats_by_room,
                seat_multipliers=seat_multipliers,
                payment_method_ids=payment_methods,
                combos=combos,
                run_id=run_id,
            )

        if mongo:
            # Nếu skip SQL thì vẫn không có movies/user_ids để bơm Mongo chuẩn.
            if not movies and sql is None:
                raise RuntimeError("Muốn seed Mongo theo phim/user thật thì không nên dùng --skip-sql trong lần đầu.")

            mongo.seed_movie_enrichments(movies)
            if user_ids:
                mongo.seed_interactions_from_bookings(booking_events, run_id)
                mongo.seed_extra_browsing_interactions(user_ids, movies, run_id, args.extra_interactions)
                mongo.seed_chat_sessions_and_feedback(user_ids, movies, run_id, args.chat_sessions)

        print("\nHOÀN TẤT SEED DỮ LIỆU MÔ PHỎNG")
        print(f"Run ID: {run_id}")
        print(f"SQL users: {len(user_ids)}")
        print(f"SQL bookings: {len(booking_events)}")
        print(f"Mongo database: {args.mongo_db if not args.skip_mongo else 'skip'}")

    finally:
        if sql:
            sql.close()
        if mongo:
            mongo.close()


if __name__ == "__main__":
    main()
