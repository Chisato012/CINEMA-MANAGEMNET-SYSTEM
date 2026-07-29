using System.ComponentModel.DataAnnotations;

namespace Cinema_Management.Models;

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Vui long nhap email")]
    [EmailAddress(ErrorMessage = "Email khong dung dinh dang")]
    public string Email { get; set; } = string.Empty;
}
