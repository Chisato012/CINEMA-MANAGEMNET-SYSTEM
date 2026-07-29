using System.ComponentModel.DataAnnotations;

namespace Cinema_Management.Models;

public class MovieReviewSummaryViewModel
{
    public decimal AverageRating { get; init; }

    public int TotalRatings { get; init; }
}

public class MovieReviewViewModel
{
    public int ReviewID { get; init; }

    public decimal Rating { get; init; }

    public string Comment { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }

    public string UserFullName { get; init; } = string.Empty;

    public string? UserAvatarUrl { get; init; }
}

public class MovieReviewFormViewModel
{
    [Required]
    public int MovieId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn điểm đánh giá.")]
    [Range(0.00, 5.00, ErrorMessage = "Điểm đánh giá phải từ 0 đến 5.")]
    public decimal? Rating { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập nội dung đánh giá.")]
    [StringLength(2000, ErrorMessage = "Nội dung đánh giá tối đa 2000 ký tự.")]
    public string Comment { get; set; } = string.Empty;
}
