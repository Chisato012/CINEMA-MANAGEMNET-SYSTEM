using System.ComponentModel.DataAnnotations;

namespace Cinema_Management.Models;

public class ResetPasswordViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui long nhap mat khau moi")]
    [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{8,}$",
        ErrorMessage = "Mat khau can it nhat 8 ky tu, gom 1 chu hoa, 1 so va 1 ky tu dac biet")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui long nhap lai mat khau moi")]
    [Compare(nameof(Password), ErrorMessage = "Mat khau xac nhan khong khop")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
