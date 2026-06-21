from __future__ import annotations

import argparse
import json
import math
import random
import re
import sys
import unicodedata
from dataclasses import dataclass
from datetime import date, timedelta
from pathlib import Path
from typing import Any

try:
    import pyodbc
except ImportError:  # pragma: no cover
    pyodbc = None

try:
    from pymongo import MongoClient
except ImportError:  # pragma: no cover
    MongoClient = None

import torch
from transformers import AutoModelForSequenceClassification, AutoTokenizer


SCRIPT_DIR = Path(__file__).resolve().parent
DEFAULT_INTENT_MODEL_DIR = (SCRIPT_DIR / ".." / ".." / "Train" / "phobert" / "finetune-phobert").resolve()
DEFAULT_MOOD_MODEL_DIR = (SCRIPT_DIR / ".." / ".." / "Train" / "phobert" / "phobert-mood" / "checkpoint-1680").resolve()

SOCIAL_ANSWERS = {
    "Greeting": [
        "Chào bạn, mình có thể giúp tìm phim, xem thông tin phim hoặc gợi ý phim theo tâm trạng nhé.",
        "Xin chào, hôm nay bạn muốn tìm phim hay cần mình gợi ý phim hợp mood?",
    ],
    "Goodbye": [
        "Hẹn gặp lại bạn. Khi nào muốn chọn phim thì cứ nhắn mình nhé.",
        "Tạm biệt bạn, chúc bạn có một buổi xem phim thật vui.",
    ],
    "Thanks": [
        "Không có gì đâu, cần tìm thêm phim thì mình vẫn ở đây.",
        "Rất vui được giúp bạn. Bạn muốn xem thêm gợi ý nào không?",
    ],
    "Help": [
        "Bạn có thể hỏi: 'gợi ý phim hài', 'tìm suất chiếu Toy Story hôm nay', hoặc 'nội dung Your Name là gì'.",
        "Mình hỗ trợ chào hỏi, cảm ơn, tìm phim, xem thông tin phim và gợi ý phim theo thể loại hoặc tâm trạng.",
    ],
}

GENRE_KEYWORDS = {
    "Trinh thám": ["trinh thám", "bí ẩn", "phá án", "detective", "suy luận"],
    "Khoa học viễn tưởng": ["khoa học viễn tưởng", "khvt", "viễn tưởng", "sci-fi", "tương lai", "vũ trụ"],
    "Hoạt hình": ["hoạt hình", "animation", "doraemon", "ponyo", "toy story"],
    "Tình cảm": ["tình cảm", "lãng mạn", "romance", "hẹn hò", "người yêu"],
    "Hành động": ["hành động", "siêu anh hùng", "chiến đấu", "đánh nhau"],
    "Kinh dị": ["kinh dị", "ma", "rùng rợn", "tâm linh", "horror"],
    "Gia đình": ["gia đình", "trẻ em", "em nhỏ", "cả nhà", "ba mẹ"],
    "Cảm động": ["cảm động", "lấy nước mắt", "sâu lắng"],
}

MOOD_DISPLAY = {
    "stress": "căng thẳng",
    "sad": "buồn",
    "laugh": "muốn cười",
    "healing": "chữa lành",
    "excited": "hào hứng",
    "lonely": "cô đơn",
    "cry": "muốn khóc",
    "scary": "hơi sợ",
    "none": "không rõ",
}

MOOD_MATCH_TERMS = {
    "stress": ["stress", "căng thẳng", "áp lực", "mệt mỏi", "quá tải", "xả stress"],
    "sad": ["buồn", "chán", "không vui", "tụt mood", "down mood", "suy"],
    "laugh": ["muốn cười", "vui vẻ", "hài", "giải trí", "vui nhộn"],
    "healing": ["chữa lành", "healing", "ấm áp", "nhẹ nhàng", "bình yên", "thư giãn"],
    "excited": ["hào hứng", "phấn khích", "hồi hộp", "kịch tính", "bùng nổ"],
    "lonely": ["cô đơn", "một mình", "lẻ loi", "trống trải"],
    "cry": ["muốn khóc", "khóc", "rơi nước mắt", "cảm động", "nghẹn ngào"],
    "scary": ["hơi sợ", "đáng sợ", "ám ảnh", "lạnh gáy", "hù dọa"],
}

MOOD_TO_GENRES = {
    "stress": ["Hoạt hình", "Gia đình", "Cảm động"],
    "sad": ["Cảm động", "Tình cảm", "Hoạt hình"],
    "laugh": ["Hoạt hình", "Gia đình"],
    "healing": ["Cảm động", "Gia đình", "Hoạt hình"],
    "excited": ["Hành động", "Khoa học viễn tưởng", "Trinh thám"],
    "lonely": ["Cảm động", "Tình cảm", "Gia đình"],
    "cry": ["Cảm động", "Tình cảm"],
    "scary": ["Kinh dị", "Trinh thám"],
}

TIME_KEYWORDS = {
    "today": ["hôm nay", "tối nay", "chiều nay", "sáng nay", "bây giờ"],
    "tomorrow": ["ngày mai", "sáng mai", "chiều mai", "tối mai", "mai"],
    "weekend": ["cuối tuần", "thứ bảy", "chủ nhật"],
    "week": ["tuần này"],
}

STATIC_MOVIES = [
    {
        "movieId": 1,
        "title": "Doraemon",
        "genres": "Hoạt hình, Gia đình",
        "synopsis": "Cuộc phiêu lưu mới đầy thú vị của Doraemon, Nobita và những người bạn quen thuộc.",
        "ageRating": "P",
        "duration": 105,
        "posterUrl": "poster_doraemon.jpg",
        "trailer": "trailer_doraemon.mp4",
        "countryName": "Nhật",
        "languageName": "Tiếng Nhật",
    },
    {
        "movieId": 2,
        "title": "Toy Story",
        "genres": "Hoạt hình, Gia đình",
        "synopsis": "Hành trình kỳ thú của thế giới đồ chơi khi đối mặt với những thử thách mới.",
        "ageRating": "P",
        "duration": 100,
        "posterUrl": "poster_toy_story.jpg",
        "trailer": "trailer_toy_story.mp4",
        "countryName": "Mỹ",
        "languageName": "Tiếng Anh",
    },
    {
        "movieId": 3,
        "title": "Your Name",
        "genres": "Hoạt hình, Tình cảm, Cảm động",
        "synopsis": "Câu chuyện hoán đổi thân xác kỳ diệu giữa một cô gái vùng quê và một chàng trai Tokyo.",
        "ageRating": "T13",
        "duration": 106,
        "posterUrl": "poster_your_name.jpg",
        "trailer": "trailer_your_name.mp4",
        "countryName": "Nhật",
        "languageName": "Tiếng Nhật",
    },
    {
        "movieId": 4,
        "title": "Ma Xó",
        "genres": "Kinh dị",
        "synopsis": "Bộ phim kinh dị tâm linh Việt Nam xoay quanh những bí ẩn cổ xưa tại một ngôi làng nhỏ.",
        "ageRating": "T18",
        "duration": 110,
        "posterUrl": "poster_ma_xo.jpg",
        "trailer": "trailer_ma_xo.mp4",
        "countryName": "Việt Nam",
        "languageName": "Tiếng Việt",
    },
    {
        "movieId": 5,
        "title": "Colony",
        "genres": "Khoa học viễn tưởng, Hành động",
        "synopsis": "Bối cảnh tương lai nơi con người phải chiến đấu sinh tồn trong một trật tự xã hội mới.",
        "ageRating": "T16",
        "duration": 125,
        "posterUrl": "poster_colony.jpg",
        "trailer": "trailer_colony.mp4",
        "countryName": "Hàn Quốc",
        "languageName": "Tiếng Hàn",
    },
    {
        "movieId": 6,
        "title": "Super Girls",
        "genres": "Khoa học viễn tưởng, Hành động",
        "synopsis": "Hành trình của các nữ siêu anh hùng thế hệ mới trong việc bảo vệ công lý.",
        "ageRating": "T13",
        "duration": 130,
        "posterUrl": "poster_super_girls.jpg",
        "trailer": "trailer_super_girls.mp4",
        "countryName": "Mỹ",
        "languageName": "Tiếng Anh",
    },
    {
        "movieId": 7,
        "title": "Cô bé Ponyo",
        "genres": "Hoạt hình, Gia đình, Cảm động",
        "synopsis": "Câu chuyện đáng yêu về tình bạn giữa một cậu bé loài người và cô bé cá vàng Ponyo.",
        "ageRating": "P",
        "duration": 101,
        "posterUrl": "poster_co_be_ponyo.jpg",
        "trailer": "trailer_co_be_ponyo.mp4",
        "countryName": "Nhật",
        "languageName": "Tiếng Nhật",
    },
]


@dataclass
class LabelResult:
    label: str
    confidence: float
    scores: list[dict[str, Any]]
    model_dir: str
    device: str
    available: bool = True
    error: str | None = None


def strip_accents(text: str) -> str:
    value = unicodedata.normalize("NFD", str(text or ""))
    value = "".join(ch for ch in value if unicodedata.category(ch) != "Mn")
    return value.replace("đ", "d").replace("Đ", "D")


def normalize(text: str) -> str:
    return re.sub(r"\s+", " ", strip_accents(text).casefold()).strip()


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Hybrid chatbot using PhoBERT + SQL Server + MongoDB.")
    parser.add_argument("--message", default="")
    parser.add_argument("--user-id", type=int, default=None)
    parser.add_argument("--top-k", type=int, default=5)
    parser.add_argument("--model-dir", "--intent-model-dir", dest="intent_model_dir", default=str(DEFAULT_INTENT_MODEL_DIR))
    parser.add_argument("--mood-model-dir", default=str(DEFAULT_MOOD_MODEL_DIR))
    parser.add_argument("--max-length", type=int, default=96)
    parser.add_argument("--diagnostics", action="store_true")
    parser.add_argument("--sql-server", default=r"(localdb)\MSSQLLocalDB")
    parser.add_argument("--sql-database", default="MovieTicketDB")
    parser.add_argument("--odbc-driver", default="ODBC Driver 17 for SQL Server")
    parser.add_argument("--sql-auth", choices=["windows", "sql"], default="windows")
    parser.add_argument("--sql-username", default="sa")
    parser.add_argument("--sql-password", default="")
    parser.add_argument("--sql-encrypt", default="No")
    parser.add_argument("--mongo-uri", default="mongodb://localhost:27017")
    parser.add_argument("--mongo-database", default="MovieTicketRecommendationDB")
    return parser.parse_args()


def has_model_weights(model_dir: Path) -> bool:
    return any((model_dir / name).exists() for name in ["model.safetensors", "pytorch_model.bin"])


def predict_label(text: str, model_dir: Path, max_length: int, top_k: int = 7) -> LabelResult:
    model_dir = model_dir.resolve()
    if not model_dir.exists():
        return LabelResult("none", 0.0, [], str(model_dir), "none", available=False, error="model directory not found")
    if not has_model_weights(model_dir):
        return LabelResult("none", 0.0, [], str(model_dir), "none", available=False, error="model weights not found")

    try:
        tokenizer = AutoTokenizer.from_pretrained(model_dir, use_fast=False)
        model = AutoModelForSequenceClassification.from_pretrained(model_dir)
        device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
        model.to(device)
        model.eval()

        inputs = tokenizer(
            text,
            return_tensors="pt",
            truncation=True,
            padding=True,
            max_length=max_length,
        )
        inputs = {key: value.to(device) for key, value in inputs.items()}

        with torch.no_grad():
            logits = model(**inputs).logits
            probabilities = torch.softmax(logits, dim=-1)[0]
    except Exception as exc:
        return LabelResult("none", 0.0, [], str(model_dir), "none", available=False, error=str(exc))

    id2label = {int(key): value for key, value in model.config.id2label.items()}
    predicted_id = int(torch.argmax(probabilities).item())
    top_values, top_indices = torch.topk(probabilities, k=min(top_k, len(probabilities)))

    return LabelResult(
        label=str(id2label[predicted_id]),
        confidence=float(probabilities[predicted_id].item()),
        scores=[
            {"label": str(id2label[int(index.item())]), "score": float(value.item())}
            for value, index in zip(top_values, top_indices)
        ],
        model_dir=str(model_dir),
        device=str(device),
    )


def fallback_mood(message: str) -> str:
    value = normalize(message)
    rules = {
        "stress": ["stress", "cang thang", "ap luc", "met moi", "qua tai", "xa stress"],
        "cry": ["muon khoc", "can khoc", "roi nuoc mat", "lay nuoc mat"],
        "lonely": ["co don", "mot minh", "le loi", "trong trai"],
        "sad": ["dang buon", "hoi buon", "buon qua", "khong vui", "tut mood", "down mood", "chan qua"],
        "healing": ["chua lanh", "healing", "am ap", "nhe nhang", "binh yen", "thu gian"],
        "laugh": ["muon cuoi", "cuoi that nhieu", "vui ve", "vui nhon"],
        "excited": ["hao hung", "phan khich", "hoi hop", "bung no"],
        "scary": ["hoi so", "dang so", "so qua", "lanh gay", "am anh"],
    }
    for mood, keywords in rules.items():
        if any(keyword in value for keyword in keywords):
            return mood
    return "none"


def choose_moods(message: str, mood_prediction: LabelResult) -> list[str]:
    moods: list[str] = []
    predicted = normalize(mood_prediction.label).replace(" ", "_")
    if mood_prediction.available and predicted != "none" and mood_prediction.confidence >= 0.35:
        moods.append(predicted)

    fallback = fallback_mood(message)
    if fallback != "none" and fallback not in moods:
        moods.append(fallback)

    return moods


def extract_genres(message: str, moods: list[str]) -> list[str]:
    value = normalize(message)
    genres = []
    for genre, keywords in GENRE_KEYWORDS.items():
        if any(normalize(keyword) in value for keyword in keywords):
            genres.append(genre)
    for mood in moods:
        genres.extend(MOOD_TO_GENRES.get(mood, []))
    return list(dict.fromkeys(genres))


def extract_time_hint(message: str) -> dict[str, Any] | None:
    value = normalize(message)
    today = date.today()
    for key, keywords in TIME_KEYWORDS.items():
        if not any(normalize(keyword) in value for keyword in keywords):
            continue
        if key == "today":
            return {"kind": key, "from": str(today), "to": str(today)}
        if key == "tomorrow":
            tomorrow = today + timedelta(days=1)
            return {"kind": key, "from": str(tomorrow), "to": str(tomorrow)}
        if key == "weekend":
            saturday = today + timedelta(days=(5 - today.weekday()) % 7)
            sunday = saturday + timedelta(days=1)
            return {"kind": key, "from": str(saturday), "to": str(sunday)}
        if key == "week":
            return {"kind": key, "from": str(today), "to": str(today + timedelta(days=7))}
    return None


def sql_connection_string(args: argparse.Namespace) -> str:
    parts = [
        f"DRIVER={{{args.odbc_driver}}}",
        f"SERVER={args.sql_server}",
        f"DATABASE={args.sql_database}",
        f"Encrypt={args.sql_encrypt}",
        "TrustServerCertificate=Yes",
    ]
    if args.sql_auth == "windows":
        parts.append("Trusted_Connection=Yes")
    else:
        parts.extend([f"UID={args.sql_username}", f"PWD={args.sql_password}"])
    return ";".join(parts) + ";"


def connect_sql(args: argparse.Namespace):
    if pyodbc is None:
        return None
    try:
        return pyodbc.connect(sql_connection_string(args), timeout=4)
    except Exception:
        return None


def rows_to_dicts(cursor) -> list[dict[str, Any]]:
    columns = [column[0] for column in cursor.description]
    return [dict(zip(columns, row)) for row in cursor.fetchall()]


def fetch_movies(args: argparse.Namespace) -> tuple[list[dict[str, Any]], str]:
    connection = connect_sql(args)
    if connection is None:
        return [dict(movie, score=0.0, reasons=[]) for movie in STATIC_MOVIES], "static_fallback"

    query = """
        SELECT
            m.MovieID AS movieId,
            m.Title AS title,
            COALESCE(gl.Genres, N'') AS genres,
            m.Synopsis AS synopsis,
            m.AgeRating AS ageRating,
            CAST(m.Duration AS int) AS duration,
            m.PosterURL AS posterUrl,
            m.Trailer AS trailer,
            COALESCE(c.CountryName, N'') AS countryName,
            COALESCE(l.LanguageName, N'') AS languageName
        FROM Movies m
        LEFT JOIN Countries c ON c.CountryID = m.CountryID
        LEFT JOIN Languages l ON l.LanguageID = m.LanguageID
        OUTER APPLY (
            SELECT STRING_AGG(g.Name, N', ') AS Genres
            FROM MovieGenres mg
            JOIN Genres g ON g.GenreID = mg.GenreID
            WHERE mg.MovieID = m.MovieID
        ) gl
        ORDER BY m.MovieID;
    """
    with connection:
        cursor = connection.cursor()
        cursor.execute(query)
        rows = rows_to_dicts(cursor)

    movies = []
    for row in rows:
        movies.append({
            "movieId": int(row["movieId"]),
            "title": str(row["title"] or ""),
            "genres": str(row["genres"] or ""),
            "synopsis": str(row["synopsis"] or ""),
            "ageRating": str(row["ageRating"] or ""),
            "duration": int(row["duration"]) if row["duration"] is not None else None,
            "posterUrl": str(row["posterUrl"] or ""),
            "trailer": str(row["trailer"] or ""),
            "countryName": str(row["countryName"] or ""),
            "languageName": str(row["languageName"] or ""),
            "score": 0.0,
            "reasons": [],
        })
    return movies, "sqlserver"


def fetch_showtimes(args: argparse.Namespace, movie_ids: list[int], time_hint: dict[str, Any] | None) -> dict[int, list[dict[str, Any]]]:
    if not movie_ids:
        return {}

    connection = connect_sql(args)
    if connection is None:
        return {}

    start_date = time_hint["from"] if time_hint else str(date.today())
    end_date = time_hint["to"] if time_hint else str(date.today() + timedelta(days=14))
    placeholders = ",".join("?" for _ in movie_ids)
    query = f"""
        SELECT TOP 80
            s.MovieID AS movieId,
            s.ShowtimeID AS showtimeId,
            r.RoomName AS roomName,
            s.StartTime AS startTime,
            s.EndTime AS endTime,
            s.BasePrice AS basePrice
        FROM Showtimes s
        JOIN Rooms r ON r.RoomID = s.RoomID
        WHERE s.MovieID IN ({placeholders})
          AND CAST(s.StartTime AS date) BETWEEN ? AND ?
          AND s.StartTime >= GETDATE()
        ORDER BY s.StartTime;
    """
    with connection:
        cursor = connection.cursor()
        cursor.execute(query, [*movie_ids, start_date, end_date])
        rows = rows_to_dicts(cursor)

    result: dict[int, list[dict[str, Any]]] = {}
    for row in rows:
        movie_id = int(row["movieId"])
        result.setdefault(movie_id, []).append({
            "showtimeId": int(row["showtimeId"]),
            "roomName": str(row["roomName"] or ""),
            "startTime": row["startTime"].isoformat() if hasattr(row["startTime"], "isoformat") else str(row["startTime"]),
            "endTime": row["endTime"].isoformat() if hasattr(row["endTime"], "isoformat") else str(row["endTime"]),
            "basePrice": float(row["basePrice"]),
        })
    return result


def connect_mongo(args: argparse.Namespace):
    if MongoClient is None:
        return None
    try:
        client = MongoClient(args.mongo_uri, serverSelectionTimeoutMS=1200)
        client.admin.command("ping")
        return client
    except Exception:
        return None


def fetch_mongo_context(args: argparse.Namespace, user_id: int | None) -> tuple[dict[int, dict[str, Any]], dict[int, float], str]:
    client = connect_mongo(args)
    if client is None:
        return {}, {}, "unavailable"

    db = client[args.mongo_database]
    enrichments = {
        int(doc["movieId"]): doc
        for doc in db.movie_enrichments.find({}, {"_id": 0})
        if doc.get("movieId") is not None
    }

    user_scores: dict[int, float] = {}
    if user_id is not None:
        for doc in db.user_interactions.find({"userId": user_id}, {"_id": 0, "movieId": 1, "weight": 1, "eventType": 1}).limit(300):
            movie_id = doc.get("movieId")
            if movie_id is None:
                continue
            user_scores[int(movie_id)] = user_scores.get(int(movie_id), 0.0) + float(doc.get("weight") or 1.0)

        for doc in db.recommendation_feedback.find({"userId": user_id}, {"_id": 0, "clickedMovieId": 1, "ignoredMovieIds": 1}).limit(200):
            clicked = doc.get("clickedMovieId")
            if clicked is not None:
                user_scores[int(clicked)] = user_scores.get(int(clicked), 0.0) + 4.0
            for ignored in doc.get("ignoredMovieIds") or []:
                user_scores[int(ignored)] = user_scores.get(int(ignored), 0.0) - 0.5

    client.close()
    return enrichments, user_scores, "mongodb"


def find_movie_by_title(message: str, movies: list[dict[str, Any]]) -> dict[str, Any] | None:
    value = normalize(message)
    for movie in sorted(movies, key=lambda item: len(item["title"]), reverse=True):
        title = normalize(movie["title"])
        if title and title in value:
            return movie
    return None


def score_movie(
    movie: dict[str, Any],
    message: str,
    moods: list[str],
    genres: list[str],
    enrichments: dict[int, dict[str, Any]],
    user_scores: dict[int, float],
) -> dict[str, Any]:
    scored = dict(movie)
    scored["score"] = 0.0
    scored["reasons"] = []

    movie_id = int(movie["movieId"])
    movie_genres = normalize(movie.get("genres", ""))
    blob = normalize(" ".join([
        movie.get("title", ""),
        movie.get("genres", ""),
        movie.get("synopsis", ""),
        movie.get("countryName", ""),
        movie.get("languageName", ""),
    ]))

    for genre in genres:
        if normalize(genre) in movie_genres:
            scored["score"] += 5.0
            scored["reasons"].append(f"Khớp thể loại {genre}")

    enrichment = enrichments.get(movie_id, {})
    enrichment_keywords = [normalize(item) for item in enrichment.get("keywords", [])]
    enrichment_moods = [normalize(item) for item in enrichment.get("moods", [])]
    enrichment_themes = [normalize(item) for item in enrichment.get("themes", [])]

    for mood in moods:
        mood_terms = [normalize(item) for item in [mood, MOOD_DISPLAY.get(mood, mood), *MOOD_MATCH_TERMS.get(mood, [])]]
        if any(any(term in value or value in term for value in enrichment_moods) for term in mood_terms):
            scored["score"] += 4.0
            scored["reasons"].append(f"MongoDB đánh dấu hợp mood {MOOD_DISPLAY.get(mood, mood)}")

    for genre in genres:
        norm_genre = normalize(genre)
        if norm_genre in enrichment_keywords or norm_genre in enrichment_themes:
            scored["score"] += 1.5

    user_score = user_scores.get(movie_id, 0.0)
    if user_score > 0:
        scored["score"] += min(6.0, math.log1p(user_score) * 2.2)
        scored["reasons"].append("Có tín hiệu cá nhân hóa từ lịch sử người dùng")
    elif user_score < 0:
        scored["score"] += max(-2.0, user_score)

    for token in re.findall(r"[\wÀ-ỹ]+", normalize(message)):
        if len(token) >= 3 and token in blob:
            scored["score"] += 0.2

    if normalize(movie.get("title", "")) in normalize(message):
        scored["score"] += 8.0
        scored["reasons"].append("Khớp tên phim")

    if "tre em" in normalize(message) or "gia dinh" in normalize(message):
        if movie.get("ageRating") == "P":
            scored["score"] += 2.0
            scored["reasons"].append("Phù hợp gia đình/trẻ em")

    scored["score"] = round(float(scored["score"]), 4)
    scored["reasons"] = list(dict.fromkeys(scored["reasons"]))[:4]
    return scored


def rank_movies(
    movies: list[dict[str, Any]],
    message: str,
    moods: list[str],
    genres: list[str],
    enrichments: dict[int, dict[str, Any]],
    user_scores: dict[int, float],
    top_k: int,
    exclude_movie_id: int | None = None,
) -> list[dict[str, Any]]:
    ranked = []
    for movie in movies:
        if exclude_movie_id is not None and int(movie["movieId"]) == exclude_movie_id:
            continue
        ranked.append(score_movie(movie, message, moods, genres, enrichments, user_scores))

    ranked.sort(key=lambda item: (item["score"], item["movieId"]), reverse=True)
    if ranked and ranked[0]["score"] <= 0:
        random.Random(42).shuffle(ranked)
    return ranked[: max(1, min(top_k, 20))]


def social_answer(intent: str) -> str:
    return random.Random(intent).choice(SOCIAL_ANSWERS.get(intent, SOCIAL_ANSWERS["Help"]))


def info_answer(movie: dict[str, Any]) -> str:
    return (
        f"{movie['title']} thuộc thể loại {movie.get('genres') or 'chưa rõ'}, "
        f"thời lượng {movie.get('duration') or 'chưa rõ'} phút, nhãn tuổi {movie.get('ageRating') or 'chưa rõ'}. "
        f"Nội dung: {movie.get('synopsis') or 'chưa có mô tả'}"
    )


def recommendation_answer(items: list[dict[str, Any]], moods: list[str], genres: list[str]) -> str:
    if not items:
        return "Mình chưa tìm thấy phim phù hợp trong dữ liệu hiện tại."
    mood_part = f" theo tâm trạng {', '.join(MOOD_DISPLAY.get(mood, mood) for mood in moods)}" if moods else ""
    genre_part = f" và thể loại {', '.join(genres[:3])}" if genres else ""
    titles = ", ".join(item["title"] for item in items[:3])
    return f"Mình gợi ý{mood_part}{genre_part}: {titles}."


def search_answer(items: list[dict[str, Any]], showtimes: dict[int, list[dict[str, Any]]]) -> str:
    if not items:
        return "Mình chưa tìm thấy phim phù hợp trong database."

    parts = []
    for item in items[:5]:
        schedules = showtimes.get(int(item["movieId"]), [])
        if schedules:
            first = schedules[0]
            parts.append(f"{item['title']} ({item['genres']}; suất gần nhất {first['startTime']})")
        else:
            parts.append(f"{item['title']} ({item['genres']})")
    return "Mình tìm thấy: " + "; ".join(parts) + "."


def compact_movie(item: dict[str, Any], showtimes: dict[int, list[dict[str, Any]]]) -> dict[str, Any]:
    movie_id = int(item["movieId"])
    return {
        "movieId": movie_id,
        "title": item["title"],
        "genres": item.get("genres", ""),
        "score": item.get("score", 0.0),
        "reasons": item.get("reasons", []),
        "synopsis": item.get("synopsis", ""),
        "ageRating": item.get("ageRating", ""),
        "duration": item.get("duration"),
        "posterUrl": item.get("posterUrl", ""),
        "trailer": item.get("trailer", ""),
        "countryName": item.get("countryName", ""),
        "languageName": item.get("languageName", ""),
        "showtimes": showtimes.get(movie_id, [])[:5],
    }


def prediction_payload(result: LabelResult, key: str) -> dict[str, Any]:
    payload = {
        "label": result.label,
        "confidence": result.confidence,
        "scores": result.scores,
        "modelDir": result.model_dir,
        "device": result.device,
        "available": result.available,
    }
    if result.error:
        payload["error"] = result.error
    payload[key] = result.label
    return payload


def handle_chat(args: argparse.Namespace) -> dict[str, Any]:
    message = args.message.strip()
    if not message:
        raise ValueError("message is required unless --diagnostics is used")

    intent_prediction = predict_label(
        message,
        Path(args.intent_model_dir),
        args.max_length,
        top_k=max(1, min(args.top_k, 20)),
    )
    if not intent_prediction.available:
        raise FileNotFoundError(f"Intent model is not available: {intent_prediction.error} ({intent_prediction.model_dir})")

    mood_prediction = predict_label(
        message,
        Path(args.mood_model_dir),
        args.max_length,
        top_k=9,
    )

    moods = choose_moods(message, mood_prediction)
    genres = extract_genres(message, moods)
    time_hint = extract_time_hint(message)
    intent_label = intent_prediction.label

    base_payload = {
        "message": message,
        "userId": args.user_id,
        "intent": prediction_payload(intent_prediction, "intent"),
        "mood": prediction_payload(mood_prediction, "mood"),
        "entities": {
            "moods": moods,
            "moodNames": [MOOD_DISPLAY.get(mood, mood) for mood in moods],
            "genres": genres,
            "time": time_hint,
            "movieTitle": None,
        },
    }

    if intent_label in SOCIAL_ANSWERS:
        return {
            "mode": "social",
            **base_payload,
            "answer": social_answer(intent_label),
            "recommendations": [],
            "dataSources": {
                "movies": None,
                "personalization": None,
                "intentModel": intent_prediction.model_dir,
                "moodModel": mood_prediction.model_dir,
            },
        }

    movies, movie_source = fetch_movies(args)
    enrichments, user_scores, mongo_source = fetch_mongo_context(args, args.user_id)
    matched_movie = find_movie_by_title(message, movies)
    if matched_movie:
        base_payload["entities"]["movieTitle"] = matched_movie["title"]

    top_k = max(1, min(args.top_k, 20))
    if intent_label == "MovieInfo" and matched_movie:
        related = rank_movies(
            movies,
            f"{matched_movie.get('genres', '')} {matched_movie.get('synopsis', '')}",
            moods,
            [genre.strip() for genre in matched_movie.get("genres", "").split(",") if genre.strip()],
            enrichments,
            user_scores,
            top_k,
            exclude_movie_id=int(matched_movie["movieId"]),
        )
        selected = [matched_movie, *related[: top_k - 1]]
        showtimes = fetch_showtimes(args, [int(item["movieId"]) for item in selected], time_hint)
        answer = info_answer(matched_movie)
    else:
        selected = rank_movies(movies, message, moods, genres, enrichments, user_scores, top_k)
        showtimes = fetch_showtimes(args, [int(item["movieId"]) for item in selected], time_hint)
        answer = search_answer(selected, showtimes) if intent_label == "SearchMovie" else recommendation_answer(selected, moods, genres)

    return {
        "mode": "hybrid",
        **base_payload,
        "answer": answer,
        "recommendations": [compact_movie(item, showtimes) for item in selected],
        "dataSources": {
            "movies": movie_source,
            "personalization": mongo_source,
            "mongoEnrichments": len(enrichments),
            "userSignals": len(user_scores),
            "intentModel": intent_prediction.model_dir,
            "moodModel": mood_prediction.model_dir,
        },
    }


def model_diagnostics(model_dir: Path) -> dict[str, Any]:
    model_dir = model_dir.resolve()
    config_path = model_dir / "config.json"
    safetensors_path = model_dir / "model.safetensors"
    pytorch_path = model_dir / "pytorch_model.bin"
    return {
        "path": str(model_dir),
        "exists": model_dir.exists(),
        "hasConfig": config_path.exists(),
        "canReadConfig": can_read_file(config_path),
        "hasTokenizer": (model_dir / "vocab.txt").exists() and (model_dir / "bpe.codes").exists(),
        "hasWeights": has_model_weights(model_dir),
        "canReadWeights": can_read_file(safetensors_path) or can_read_file(pytorch_path),
    }


def can_read_file(path: Path) -> bool:
    if not path.exists():
        return False
    try:
        with path.open("rb") as handle:
            handle.read(1)
        return True
    except Exception:
        return False


def database_diagnostics(args: argparse.Namespace) -> dict[str, Any]:
    sql = {"status": "unavailable", "driverInstalled": pyodbc is not None}
    connection = connect_sql(args)
    if connection is not None:
        try:
            with connection:
                cursor = connection.cursor()
                cursor.execute("SELECT COUNT(*) FROM Movies")
                movie_count = int(cursor.fetchone()[0])
                cursor.execute("SELECT COUNT(*) FROM Showtimes")
                showtime_count = int(cursor.fetchone()[0])
            sql = {"status": "sqlserver", "movieCount": movie_count, "showtimeCount": showtime_count}
        except Exception as exc:
            sql = {"status": "error", "error": str(exc)}

    mongo = {"status": "unavailable", "driverInstalled": MongoClient is not None}
    client = connect_mongo(args)
    if client is not None:
        try:
            db = client[args.mongo_database]
            mongo = {
                "status": "mongodb",
                "movieEnrichments": db.movie_enrichments.count_documents({}),
                "userInteractions": db.user_interactions.count_documents({}),
                "recommendationFeedback": db.recommendation_feedback.count_documents({}),
            }
        except Exception as exc:
            mongo = {"status": "error", "error": str(exc)}
        finally:
            client.close()

    return {"sqlServer": sql, "mongodb": mongo}


def handle_diagnostics(args: argparse.Namespace) -> dict[str, Any]:
    return {
        "python": sys.executable,
        "dependencies": {
            "torch": torch.__version__,
            "pyodbc": pyodbc is not None,
            "pymongo": MongoClient is not None,
        },
        "models": {
            "intent": model_diagnostics(Path(args.intent_model_dir)),
            "mood": model_diagnostics(Path(args.mood_model_dir)),
        },
        "databases": database_diagnostics(args),
    }


def main() -> None:
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    if hasattr(sys.stderr, "reconfigure"):
        sys.stderr.reconfigure(encoding="utf-8")

    args = parse_args()
    try:
        payload = handle_diagnostics(args) if args.diagnostics else handle_chat(args)
        print(json.dumps(payload, ensure_ascii=False))
    except Exception as exc:
        print(json.dumps({"error": str(exc)}, ensure_ascii=False), file=sys.stderr)
        raise SystemExit(1)


if __name__ == "__main__":
    main()
