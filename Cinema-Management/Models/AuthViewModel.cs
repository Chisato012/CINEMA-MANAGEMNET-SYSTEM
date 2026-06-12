using System.ComponentModel.DataAnnotations;
using Cinema_Management.Models.Validation;

namespace Cinema_Management.Models;

public class AuthViewModel
{
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{8,}$",
        ErrorMessage = "Mật khẩu cần ít nhất 8 ký tự, gồm 1 chữ hoa, 1 số và 1 ký tự đặc biệt")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập đầy đủ họ tên")]
    [RegularExpression(@"^[\p{L}\s]+$", ErrorMessage = "Tên không được có số hoặc ký tự đặc biệt")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại phải gồm 10 chữ số và bắt đầu bằng 0")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn ngày sinh")]
    [DataType(DataType.Date)]
    [BirthYearNotBefore(2010, ErrorMessage = "Năm sinh không được nhỏ hơn năm 2010")]
    public DateTime? DateOfBirth { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu")]
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
