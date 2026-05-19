using System.ComponentModel.DataAnnotations;

namespace FODUN.Reservations.Api.Models.Auth;

public sealed class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "El correo es requerido.")]
    [EmailAddress(ErrorMessage = "Formato de correo inválido.")]
    public string Email { get; set; } = string.Empty;
}
