using System.ComponentModel.DataAnnotations;

namespace FODUN.Reservations.Api.Models.Auth;

public sealed class RegisterViewModel
{
    [Required(ErrorMessage = "El nombre completo es requerido.")]
    [MaxLength(255, ErrorMessage = "Máximo 255 caracteres.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El número de documento es requerido.")]
    [MaxLength(20, ErrorMessage = "Máximo 20 caracteres.")]
    public string DocumentNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es requerido.")]
    [EmailAddress(ErrorMessage = "Formato de correo inválido.")]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es requerida.")]
    [MinLength(8, ErrorMessage = "Mínimo 8 caracteres.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirma tu contraseña.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
