from __future__ import annotations

import argparse
import random
import string
from datetime import datetime, timedelta
from typing import Any

import pyodbc

try:
    from pymongo import MongoClient, UpdateOne
except ImportError:
    MongoClient = None
    UpdateOne = None


# =========================================================
# CONFIG MẶC ĐỊNH
# =========================================================

DEFAULT_SQL_SERVER = r"(localdb)\MSSQLLocalDB"
DEFAULT_SQL_DATABASE = "MovieTicketDB"
DEFAULT_ODBC_DRIVER = "ODBC Driver 17 for SQL Server"

DEFAULT_MONGO_URI = "mongodb://localhost:27017"
DEFAULT_MONGO_DB = "MovieTicketRecommendationDB"

SOURCE_TAG = "extra_movie_seed_v1"


# =========================================================
# LOOKUP DATA
# =========================================================

GENRES = [
    "Trinh thám",
    "Khoa học viễn tưởng",
    "Hoạt hình",
    "Tình cảm",
    "Hành động",
    "Kinh dị",
    "Gia đình",
    "Cảm động",
]

COUNTRIES = [
    "Anh",
    "Mỹ",
    "Nhật",
    "Hàn",
    "Việt",
    "Pháp",
    "Trung Quốc",
    "Thái Lan",
    "Ấn Độ",
    "Canada",
]

LANGUAGES = [
    "Tiếng Anh",
    "Tiếng Việt",
    "Tiếng Nhật",
    "Tiếng Hàn",
    "Tiếng Pháp",
    "Tiếng Trung",
    "Tiếng Thái",
    "Tiếng Hindi",
]

COUNTRY_LANGUAGE_HINT = {
    "Anh": "Tiếng Anh",
    "Mỹ": "Tiếng Anh",
    "Nhật": "Tiếng Nhật",
    "Hàn": "Tiếng Hàn",
    "Việt": "Tiếng Việt",
    "Pháp": "Tiếng Pháp",
    "Trung Quốc": "Tiếng Trung",
    "Thái Lan": "Tiếng Thái",
    "Ấn Độ": "Tiếng Hindi",
    "Canada": "Tiếng Anh",
}

AGE_RATINGS = ["P", "T13", "T16", "T18"]

DIRECTOR_FIRST_NAMES = [
    "An", "Bình", "Châu", "Duy", "Hải", "Khánh", "Linh", "Minh", "Nam", "Phong",
    "Quân", "Sơn", "Trang", "Tuấn", "Vy", "Akira", "Haruto", "Min-jun", "Ji-ho",
    "Emily", "James", "Sofia", "Lucas", "Emma"
]

DIRECTOR_LAST_NAMES = [
    "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Vũ", "Đặng", "Bùi", "Mori",
    "Tanaka", "Kim", "Park", "Lee", "Johnson", "Smith", "Brown", "Williams"
]

CAST_FIRST_NAMES = [
    "An", "Anh", "Bảo", "Chi", "Dũng", "Hà", "Hân", "Huy", "Lan", "Long",
    "Mai", "Nhi", "Quang", "Thảo", "Yến", "Aoi", "Yuki", "Sora", "Jisoo",
    "Minho", "Alex", "Mia", "Noah", "Lily", "Oliver"
]

CHARACTER_NAMES = [
    "Minh", "An", "Hải", "Linh", "Quân", "Mai", "Nam", "Vy", "Kaito", "Sora",
    "Ji-eun", "Daniel", "Mia", "Luna", "Leo", "Aria", "Ken", "Sara"
]

MOVIE_BLUEPRINTS = [
    {
        "cluster": "animation_family",
        "genres": ["Hoạt hình", "Gia đình"],
        "title_templates": [
            "Khu Vườn Của {name}",
            "Chuyến Tàu Cầu Vồng {name}",
            "Bí Mật Của Rừng Xanh {name}",
            "Mùa Hè Của Những Người Bạn {name}",
            "Hành Trình Đom Đóm {name}",
        ],
        "synopsis_templates": [
            "Một nhóm bạn nhỏ bước vào cuộc phiêu lưu ấm áp, học cách tin tưởng nhau và bảo vệ gia đình.",
            "Câu chuyện nhẹ nhàng về tình bạn, lòng dũng cảm và những điều kỳ diệu trong thế giới tuổi thơ.",
            "Những nhân vật đáng yêu cùng nhau vượt qua thử thách để tìm lại niềm vui và sự gắn kết.",
        ],
        "keywords": ["hoạt hình", "gia đình", "tình bạn", "tuổi thơ", "phiêu lưu", "vui vẻ"],
        "moods": ["vui vẻ", "thư giãn", "đi cùng gia đình", "muốn xem nhẹ nhàng"],
        "themes": ["tình bạn", "gia đình", "tuổi thơ", "lòng dũng cảm"],
        "targetAudience": ["trẻ em", "gia đình", "bạn bè"],
        "duration_range": (88, 112),
        "age": ["P", "T13"],
    },
    {
        "cluster": "romance_drama",
        "genres": ["Tình cảm", "Cảm động"],
        "title_templates": [
            "Bức Thư Gửi {name}",
            "Ngày Ta Gặp Lại {name}",
            "Mưa Trên Phố Cũ {name}",
            "Khoảng Cách Của Chúng Ta {name}",
            "Nơi Tim Còn Nhớ {name}",
        ],
        "synopsis_templates": [
            "Hai con người gặp nhau trong một thời điểm đặc biệt và học cách đối diện với ký ức, tình yêu và lựa chọn.",
            "Một câu chuyện tình cảm sâu lắng về sự trưởng thành, mất mát và hy vọng.",
            "Bộ phim theo chân những nhân vật trẻ đi tìm câu trả lời cho tình yêu và ước mơ.",
        ],
        "keywords": ["tình cảm", "lãng mạn", "cảm động", "chữa lành", "ký ức"],
        "moods": ["buồn", "đi hẹn hò", "muốn xem nhẹ nhàng", "thư giãn"],
        "themes": ["tình yêu", "trưởng thành", "ký ức", "hy vọng"],
        "targetAudience": ["cặp đôi", "bạn bè", "người lớn"],
        "duration_range": (100, 128),
        "age": ["T13", "T16"],
    },
    {
        "cluster": "action_scifi",
        "genres": ["Hành động", "Khoa học viễn tưởng"],
        "title_templates": [
            "Thành Phố Sau Năm 2099 {name}",
            "Biệt Đội Ánh Sáng {name}",
            "Cánh Cổng Không Gian {name}",
            "Trạm Cuối Sao Hỏa {name}",
            "Cuộc Chiến Tương Lai {name}",
        ],
        "synopsis_templates": [
            "Trong tương lai hỗn loạn, một đội đặc nhiệm phải ngăn chặn âm mưu đe dọa sự sống còn của nhân loại.",
            "Khi công nghệ vượt khỏi kiểm soát, những người hùng bất đắc dĩ bước vào cuộc chiến đầy kịch tính.",
            "Một nhiệm vụ ngoài không gian biến thành hành trình sinh tồn khi bí mật cổ xưa được đánh thức.",
        ],
        "keywords": ["hành động", "khoa học viễn tưởng", "tương lai", "công nghệ", "sinh tồn", "vũ trụ"],
        "moods": ["hào hứng", "muốn xem gay cấn", "đi cùng bạn bè", "căng thẳng"],
        "themes": ["anh hùng", "công nghệ", "sinh tồn", "trách nhiệm"],
        "targetAudience": ["bạn bè", "người lớn", "fan hành động"],
        "duration_range": (110, 145),
        "age": ["T13", "T16", "T18"],
    },
    {
        "cluster": "horror_mystery",
        "genres": ["Kinh dị", "Trinh thám"],
        "title_templates": [
            "Căn Nhà Sau Đồi {name}",
            "Tiếng Gõ Lúc Nửa Đêm {name}",
            "Hồ Sơ Mất Tích {name}",
            "Bóng Đen Trong Hẻm {name}",
            "Lời Nguyền Tháng Bảy {name}",
        ],
        "synopsis_templates": [
            "Một vụ mất tích bí ẩn kéo theo chuỗi sự kiện rùng rợn mà nhóm nhân vật phải tự mình khám phá.",
            "Khi quá khứ bị chôn vùi được khơi lại, những bí mật đáng sợ dần hiện ra.",
            "Một điều tra viên lần theo manh mối trong thị trấn nhỏ và phát hiện sự thật lạnh người.",
        ],
        "keywords": ["kinh dị", "bí ẩn", "trinh thám", "rùng rợn", "tâm linh", "phá án"],
        "moods": ["căng thẳng", "muốn xem gay cấn", "đi cùng bạn bè"],
        "themes": ["bí mật", "lời nguyền", "điều tra", "quá khứ"],
        "targetAudience": ["người trưởng thành", "bạn bè"],
        "duration_range": (95, 125),
        "age": ["T16", "T18"],
    },
    {
        "cluster": "vietnam_life",
        "genres": ["Gia đình", "Cảm động"],
        "title_templates": [
            "Nhà Có Ba Người {name}",
            "Mùa Gió Qua Làng {name}",
            "Quán Nhỏ Cuối Hẻm {name}",
            "Tết Ở Nhà Ngoại {name}",
            "Con Đường Về Nhà {name}",
        ],
        "synopsis_templates": [
            "Một lát cắt đời thường về gia đình Việt Nam, nơi những hiểu lầm được hóa giải bằng tình thương.",
            "Bộ phim kể về hành trình trở về quê nhà và hàn gắn những mối quan hệ tưởng như đã xa cách.",
            "Những câu chuyện nhỏ trong khu phố cũ tạo nên bức tranh ấm áp về tình thân và lòng tử tế.",
        ],
        "keywords": ["gia đình", "Việt Nam", "đời thường", "cảm động", "tình thân", "chữa lành"],
        "moods": ["muốn xem nhẹ nhàng", "thư giãn", "đi cùng gia đình", "buồn"],
        "themes": ["tình thân", "quê hương", "hàn gắn", "trưởng thành"],
        "targetAudience": ["gia đình", "người lớn", "bạn bè"],
        "duration_range": (95, 120),
        "age": ["P", "T13", "T16"],
        "preferred_country": "Việt",
    },
    {
        "cluster": "detective_thriller",
        "genres": ["Trinh thám", "Hành động"],
        "title_templates": [
            "Mật Mã Số {name}",
            "Đường Dây Ngầm {name}",
            "Vụ Án Không Tên {name}",
            "Người Sau Camera {name}",
            "Dấu Vết Cuối Cùng {name}",
        ],
        "synopsis_templates": [
            "Một chuỗi manh mối phức tạp dẫn nhân vật chính vào cuộc truy đuổi giữa sự thật và dối trá.",
            "Khi một vụ án tưởng chừng đã khép lại, bằng chứng mới làm đảo lộn mọi suy luận.",
            "Một chuyên gia điều tra phải chạy đua với thời gian để ngăn chặn kế hoạch nguy hiểm.",
        ],
        "keywords": ["trinh thám", "điều tra", "hành động", "tội phạm", "phá án", "gay cấn"],
        "moods": ["muốn xem gay cấn", "căng thẳng", "hào hứng"],
        "themes": ["sự thật", "công lý", "truy đuổi", "âm mưu"],
        "targetAudience": ["người lớn", "bạn bè", "fan trinh thám"],
        "duration_range": (105, 135),
        "age": ["T16", "T18"],
    },
]


TOKEN_NAMES = [
    "A01", "Bảy", "Xanh", "Đỏ", "Mùa Sao", "Tháng Năm", "Bình Minh", "Hoàng Hôn",
    "Số 8", "Số 9", "Omega", "Luna", "Sakura", "Seoul", "Hà Nội", "Sài Gòn",
    "Tokyo", "Paris", "Galaxy", "Aurora", "Mặt Trăng", "Mặt Trời", "Biển Lặng"
]


# =========================================================
# SQL HELPERS
# =========================================================

def sql_connection_string(server: str, database: str, driver: str, sql_auth: str, username: str, password: str) -> str:
    if sql_auth == "windows":
        return (
            f"DRIVER={{{driver}}};"
            f"SERVER={server};"
            f"DATABASE={database};"
            "Trusted_Connection=yes;"
            "TrustServerCertificate=yes;"
        )

    return (
        f"DRIVER={{{driver}}};"
        f"SERVER={server};"
        f"DATABASE={database};"
        f"UID={username};"
        f"PWD={password};"
        "TrustServerCertificate=yes;"
    )


def get_or_create_id(cursor: pyodbc.Cursor, table: str, id_col: str, name_col: str, value: str) -> int:
    cursor.execute(f"SELECT {id_col} FROM {table} WHERE {name_col} = ?", value)
    row = cursor.fetchone()
    if row:
        return int(row[0])

    cursor.execute(
        f"INSERT INTO {table} ({name_col}) OUTPUT INSERTED.{id_col} VALUES (?)",
        value,
    )
    return int(cursor.fetchone()[0])


def get_or_create_person(cursor: pyodbc.Cursor, full_name: str) -> int:
    return get_or_create_id(cursor, "Persons", "PersonID", "FullName", full_name)


def get_existing_movie_id(cursor: pyodbc.Cursor, title: str) -> int | None:
    cursor.execute("SELECT MovieID FROM Movies WHERE Title = ?", title)
    row = cursor.fetchone()
    return int(row[0]) if row else None


def insert_movie(
    cursor: pyodbc.Cursor,
    title: str,
    release_date: datetime,
    age_rating: str,
    duration: int,
    synopsis: str,
    poster_url: str,
    trailer: str,
    country_id: int,
    language_id: int,
) -> int:
    existing = get_existing_movie_id(cursor, title)
    if existing is not None:
        return existing

    cursor.execute(
        """
        INSERT INTO Movies
            (Title, ReleaseDate, AgeRating, Duration, Synopsis, PosterURL, Trailer, CountryID, LanguageID)
        OUTPUT INSERTED.MovieID
        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
        """,
        title,
        release_date,
        age_rating,
        duration,
        synopsis,
        poster_url,
        trailer,
        country_id,
        language_id,
    )
    return int(cursor.fetchone()[0])


def add_movie_genres(cursor: pyodbc.Cursor, movie_id: int, genre_ids: list[int]):
    for genre_id in genre_ids:
        cursor.execute(
            """
            IF NOT EXISTS (
                SELECT 1 FROM MovieGenres WHERE MovieID = ? AND GenreID = ?
            )
            INSERT INTO MovieGenres (MovieID, GenreID) VALUES (?, ?)
            """,
            movie_id, genre_id, movie_id, genre_id,
        )


def add_movie_directors(cursor: pyodbc.Cursor, movie_id: int, director_ids: list[int]):
    for person_id in director_ids:
        cursor.execute(
            """
            IF NOT EXISTS (
                SELECT 1 FROM MovieDirectors WHERE MovieID = ? AND PersonID = ?
            )
            INSERT INTO MovieDirectors (MovieID, PersonID) VALUES (?, ?)
            """,
            movie_id, person_id, movie_id, person_id,
        )


def add_movie_casts(cursor: pyodbc.Cursor, movie_id: int, cast_items: list[tuple[int, str]]):
    for person_id, character_name in cast_items:
        cursor.execute(
            """
            IF NOT EXISTS (
                SELECT 1 FROM MovieCasts WHERE MovieID = ? AND PersonID = ?
            )
            INSERT INTO MovieCasts (MovieID, PersonID, CharacterName) VALUES (?, ?, ?)
            """,
            movie_id, person_id, movie_id, person_id, character_name,
        )


# =========================================================
# DATA GENERATION
# =========================================================

def random_person_name(prefix: str = "") -> str:
    first = random.choice(DIRECTOR_FIRST_NAMES if prefix == "director" else CAST_FIRST_NAMES)
    last = random.choice(DIRECTOR_LAST_NAMES)
    if last in ["Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Vũ", "Đặng", "Bùi"]:
        return f"{last} {first}"
    return f"{first} {last}"


def random_release_date() -> datetime:
    # Có cả quá khứ và tương lai để mô phỏng.
    start = datetime(2010, 1, 1)
    end = datetime(2027, 12, 31)
    delta_days = (end - start).days
    return start + timedelta(days=random.randint(0, delta_days))


def normalize_filename(text: str) -> str:
    replacements = {
        " ": "_", "Đ": "D", "đ": "d",
        "à": "a", "á": "a", "ả": "a", "ã": "a", "ạ": "a",
        "ă": "a", "ằ": "a", "ắ": "a", "ẳ": "a", "ẵ": "a", "ặ": "a",
        "â": "a", "ầ": "a", "ấ": "a", "ẩ": "a", "ẫ": "a", "ậ": "a",
        "è": "e", "é": "e", "ẻ": "e", "ẽ": "e", "ẹ": "e",
        "ê": "e", "ề": "e", "ế": "e", "ể": "e", "ễ": "e", "ệ": "e",
        "ì": "i", "í": "i", "ỉ": "i", "ĩ": "i", "ị": "i",
        "ò": "o", "ó": "o", "ỏ": "o", "õ": "o", "ọ": "o",
        "ô": "o", "ồ": "o", "ố": "o", "ổ": "o", "ỗ": "o", "ộ": "o",
        "ơ": "o", "ờ": "o", "ớ": "o", "ở": "o", "ỡ": "o", "ợ": "o",
        "ù": "u", "ú": "u", "ủ": "u", "ũ": "u", "ụ": "u",
        "ư": "u", "ừ": "u", "ứ": "u", "ử": "u", "ữ": "u", "ự": "u",
        "ỳ": "y", "ý": "y", "ỷ": "y", "ỹ": "y", "ỵ": "y",
    }
    result = text.lower()
    for src, dst in replacements.items():
        result = result.replace(src, dst)
    result = "".join(ch for ch in result if ch.isalnum() or ch == "_")
    while "__" in result:
        result = result.replace("__", "_")
    return result[:80]


def make_movie_payload(index: int) -> dict[str, Any]:
    blueprint = random.choice(MOVIE_BLUEPRINTS)
    token = random.choice(TOKEN_NAMES)
    title = random.choice(blueprint["title_templates"]).format(name=token)

    # Thêm hậu tố index để tránh trùng title khi seed nhiều.
    title = f"{title} #{index:04d}"

    synopsis = random.choice(blueprint["synopsis_templates"])
    duration = random.randint(*blueprint["duration_range"])
    age_rating = random.choice(blueprint["age"])
    release_date = random_release_date()

    if "preferred_country" in blueprint:
        country_name = blueprint["preferred_country"]
    else:
        country_name = random.choice(COUNTRIES)

    language_name = COUNTRY_LANGUAGE_HINT.get(country_name, random.choice(LANGUAGES))
    filename = normalize_filename(title)

    director_count = random.choice([1, 1, 1, 2])
    cast_count = random.randint(3, 7)

    directors = [random_person_name("director") for _ in range(director_count)]
    casts = [
        (random_person_name("cast"), random.choice(CHARACTER_NAMES))
        for _ in range(cast_count)
    ]

    return {
        "title": title,
        "releaseDate": release_date,
        "ageRating": age_rating,
        "duration": duration,
        "synopsis": synopsis,
        "posterUrl": f"seed_extra_{filename}.jpg",
        "trailer": f"seed_extra_{filename}.mp4",
        "countryName": country_name,
        "languageName": language_name,
        "genres": blueprint["genres"],
        "directors": directors,
        "casts": casts,
        "keywords": blueprint["keywords"],
        "moods": blueprint["moods"],
        "themes": blueprint["themes"],
        "targetAudience": blueprint["targetAudience"],
        "cluster": blueprint["cluster"],
    }


# =========================================================
# MONGO
# =========================================================

def upsert_movie_enrichments(mongo_uri: str, mongo_db: str, docs: list[dict[str, Any]]):
    if MongoClient is None or UpdateOne is None:
        print("Bỏ qua MongoDB vì chưa cài pymongo. Cài bằng: pip install pymongo")
        return

    client = MongoClient(mongo_uri)
    db = client[mongo_db]

    db.movie_enrichments.create_index("movieId", unique=True)

    operations = []
    now = datetime.now().replace(microsecond=0)

    for doc in docs:
        movie_id = doc["movieId"]
        operations.append(UpdateOne(
            {"movieId": movie_id},
            {"$set": {
                "movieId": movie_id,
                "title": doc["title"],
                "keywords": doc["keywords"],
                "moods": doc["moods"],
                "themes": doc["themes"],
                "targetAudience": doc["targetAudience"],
                "countryName": doc["countryName"],
                "languageName": doc["languageName"],
                "extraDescription": doc["extraDescription"],
                "cluster": doc["cluster"],
                "source": SOURCE_TAG,
                "updatedAt": now,
            }},
            upsert=True,
        ))

    if operations:
        result = db.movie_enrichments.bulk_write(operations)
        print(f"MongoDB movie_enrichments upserted: {result.upserted_count}, modified: {result.modified_count}")

    client.close()


# =========================================================
# MAIN SEED
# =========================================================

def seed_extra_movies(args):
    random.seed(args.seed)

    conn_str = sql_connection_string(
        args.sql_server,
        args.sql_database,
        args.odbc_driver,
        args.sql_auth,
        args.sql_username,
        args.sql_password,
    )

    print(f"Kết nối SQL Server: {args.sql_server} / {args.sql_database}")
    conn = pyodbc.connect(conn_str)
    conn.autocommit = False
    cursor = conn.cursor()

    try:
        print("Đảm bảo dữ liệu lookup Genres/Countries/Languages tồn tại...")
        genre_id_by_name = {
            name: get_or_create_id(cursor, "Genres", "GenreID", "Name", name)
            for name in GENRES
        }
        country_id_by_name = {
            name: get_or_create_id(cursor, "Countries", "CountryID", "CountryName", name)
            for name in COUNTRIES
        }
        language_id_by_name = {
            name: get_or_create_id(cursor, "Languages", "LanguageID", "LanguageName", name)
            for name in LANGUAGES
        }
        conn.commit()

        inserted_count = 0
        skipped_count = 0
        mongo_docs: list[dict[str, Any]] = []

        print(f"Đang nạp thêm {args.count} phim...")
        for i in range(1, args.count + 1):
            payload = make_movie_payload(i)

            existing_id = get_existing_movie_id(cursor, payload["title"])
            if existing_id is not None:
                movie_id = existing_id
                skipped_count += 1
            else:
                country_id = country_id_by_name[payload["countryName"]]
                language_id = language_id_by_name[payload["languageName"]]

                movie_id = insert_movie(
                    cursor,
                    payload["title"],
                    payload["releaseDate"],
                    payload["ageRating"],
                    payload["duration"],
                    payload["synopsis"],
                    payload["posterUrl"],
                    payload["trailer"],
                    country_id,
                    language_id,
                )
                inserted_count += 1

            genre_ids = [genre_id_by_name[name] for name in payload["genres"]]
            add_movie_genres(cursor, movie_id, genre_ids)

            director_ids = [get_or_create_person(cursor, name) for name in payload["directors"]]
            add_movie_directors(cursor, movie_id, director_ids)

            cast_items = []
            for cast_name, character_name in payload["casts"]:
                cast_person_id = get_or_create_person(cursor, cast_name)
                cast_items.append((cast_person_id, character_name))
            add_movie_casts(cursor, movie_id, cast_items)

            mongo_docs.append({
                "movieId": movie_id,
                "title": payload["title"],
                "keywords": payload["keywords"],
                "moods": payload["moods"],
                "themes": payload["themes"],
                "targetAudience": payload["targetAudience"],
                "countryName": payload["countryName"],
                "languageName": payload["languageName"],
                "extraDescription": (
                    f"{payload['title']} thuộc nhóm {', '.join(payload['genres'])}, "
                    f"phù hợp với nhu cầu {', '.join(payload['moods'][:3])}."
                ),
                "cluster": payload["cluster"],
            })

            if i % 100 == 0:
                conn.commit()
                print(f"  Đã xử lý {i}/{args.count} phim...")

        conn.commit()

        print(f"SQL inserted movies: {inserted_count}")
        print(f"SQL skipped existing movies: {skipped_count}")

        if not args.skip_mongo:
            upsert_movie_enrichments(args.mongo_uri, args.mongo_db, mongo_docs)
        else:
            print("Bỏ qua MongoDB vì có --skip-mongo")

        print("\nHOÀN TẤT NẠP THÊM PHIM")
        print(f"SQL database: {args.sql_database}")
        print(f"Mongo database: {args.mongo_db if not args.skip_mongo else 'skip'}")

    except Exception:
        conn.rollback()
        raise
    finally:
        cursor.close()
        conn.close()


def parse_args():
    parser = argparse.ArgumentParser(description="Seed thêm phim cho SQL Server và MongoDB movie_enrichments")

    parser.add_argument("--sql-server", default=DEFAULT_SQL_SERVER)
    parser.add_argument("--sql-database", default=DEFAULT_SQL_DATABASE)
    parser.add_argument("--odbc-driver", default=DEFAULT_ODBC_DRIVER)
    parser.add_argument("--sql-auth", choices=["windows", "sql"], default="windows")
    parser.add_argument("--sql-username", default="sa")
    parser.add_argument("--sql-password", default="")

    parser.add_argument("--mongo-uri", default=DEFAULT_MONGO_URI)
    parser.add_argument("--mongo-db", default=DEFAULT_MONGO_DB)
    parser.add_argument("--skip-mongo", action="store_true")

    parser.add_argument("--count", type=int, default=200, help="Số phim muốn nạp thêm")
    parser.add_argument("--seed", type=int, default=2026)

    return parser.parse_args()


if __name__ == "__main__":
    seed_extra_movies(parse_args())
