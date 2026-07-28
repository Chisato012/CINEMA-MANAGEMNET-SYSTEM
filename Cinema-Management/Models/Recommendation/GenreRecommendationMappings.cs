namespace Cinema_Management.Models.Recommendation;

public static class GenreRecommendationMappings
{
    // Model học bằng code ASCII để tránh lỗi font khi train/predict.
    // Khi lọc phim trong database, ta đổi code về đúng tên thể loại tiếng Việt.
    public static readonly IReadOnlyDictionary<string, string> GenreNamesByCode =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["HoatHinh"] = "Hoạt hình",
            ["GiaDinh"] = "Gia đình",
            ["CamDong"] = "Cảm động",
            ["TinhCam"] = "Tình cảm",
            ["KinhDi"] = "Kinh dị",
            ["HanhDong"] = "Hành động",
            ["KhoaHocVienTuong"] = "Khoa học viễn tưởng",
            ["TrinhTham"] = "Trinh thám"
        };

    public static string GetGenreName(string genreCode)
    {
        return GenreNamesByCode.TryGetValue(genreCode, out var genreName)
            ? genreName
            : genreCode;
    }
}
