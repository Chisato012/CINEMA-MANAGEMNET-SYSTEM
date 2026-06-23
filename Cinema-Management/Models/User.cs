using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cinema_Management.Models;

[Table("Users")]
public class User
{
    [Key]
    public int UserID { get; set; }

    [Required(ErrorMessage = "Họ tên không được để trống")]
    [StringLength(150, ErrorMessage = "Họ tên tối đa 150 ký tự")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email không được để trống")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [StringLength(512)]
    public string? PasswordHash { get; set; }

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [StringLength(15)]
    public string? PhoneNumber { get; set; }

    [DataType(DataType.Date)]
    public DateTime? DOB { get; set; }

    public bool Status { get; set; } = true;

    public bool EmailConfirmed { get; set; } = false;

    [StringLength(50)]
    public string? ExternalProvider { get; set; }

    [StringLength(200)]
    public string? ExternalProviderKey { get; set; }

    [StringLength(64)]
    public string? EmailVerificationTokenHash { get; set; }

    public DateTime? EmailVerificationTokenExpiresAt { get; set; }

    public DateTime? EmailVerificationLastSentAt { get; set; }

    [Required]
    [StringLength(20)]
    public string Role { get; set; } = "KhachHang";
}
