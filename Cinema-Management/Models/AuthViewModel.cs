using System.ComponentModel.DataAnnotations;

namespace Cinema_Management.Models;
public class AuthViewModel
{
    public string Username { get; set; }
    
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    [MinLength(8, ErrorMessage = "Độ dài mật khẩu phải lớn hơn 8 ký tự!")]
    public string Password { get; set; }
    
    [Required(ErrorMessage = "Vui lòng nhập đầy đủ họ tên!")]
    public string FullName { get; set; }
    
    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ!")]
    public string PhoneNumber { get; set; }
    
    [Required(ErrorMessage = "Vui lòng nhập email")]
    public string Email { get; set; }
    
    [Required(ErrorMessage = "Vui lòng chọn ngày sinh")]
    public DateTime DateOfBirth { get; set; }
    
}