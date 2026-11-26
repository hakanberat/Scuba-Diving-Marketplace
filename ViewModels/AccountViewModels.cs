using System.ComponentModel.DataAnnotations;
namespace DiveMarketplace.ViewModels;
public class LoginViewModel
{
 [Required, EmailAddress, StringLength(200)] public string Email { get; set; } = "";
 [Required, StringLength(128), DataType(DataType.Password)] public string Password { get; set; } = "";
 public string? ReturnUrl { get; set; }
}
public class RegisterViewModel : LoginViewModel
{
 [Required, StringLength(100)] public string FirstName { get; set; } = "";
 [Required, StringLength(100)] public string LastName { get; set; } = "";
 [StringLength(50)] public string? Phone { get; set; }
 [Required, Compare(nameof(Password)), DataType(DataType.Password)] public string ConfirmPassword { get; set; } = "";
}
