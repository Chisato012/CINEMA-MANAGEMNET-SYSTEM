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

    [Required]
    [StringLength(512)]
    public string PasswordHash { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [StringLength(15)]
    public string? PhoneNumber { get; set; }

    [DataType(DataType.Date)]
    public DateTime? DOB { get; set; }

    public bool Status { get; set; } = true;
}
