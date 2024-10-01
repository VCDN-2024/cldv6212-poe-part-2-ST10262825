using System.ComponentModel.DataAnnotations;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid Email Address.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
    [DataType(DataType.Password)]
    public string? Password { get; set; }

    [Required(ErrorMessage = "Please confirm your password.")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    [DataType(DataType.Password)]
    public string? ConfirmPassword { get; set; }

    [Required(ErrorMessage = "Full Name is required.")]
    public string? UserName { get; set; }
}
