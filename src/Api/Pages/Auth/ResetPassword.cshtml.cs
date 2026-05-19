using FODUN.Reservations.Api.Models.Auth;
using FODUN.Reservations.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FODUN.Reservations.Api.Pages.Auth;

public class ResetPasswordModel : PageModel
{
    private readonly IUserService _userService;

    public ResetPasswordModel(IUserService userService)
    {
        _userService = userService;
    }

    [BindProperty(SupportsGet = true)]
    public string Token { get; set; } = string.Empty;

    [BindProperty]
    public ResetPasswordViewModel Input { get; set; } = new();

    public bool IsTokenValid { get; private set; }
    public string? ErrorMessage { get; private set; }

    public void OnGet()
    {
        IsTokenValid = !string.IsNullOrWhiteSpace(Token);
        if (!IsTokenValid)
            ErrorMessage = "El enlace no contiene un token válido.";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            IsTokenValid = true;
            return Page();
        }

        var result = await _userService.ResetPasswordAsync(Token, Input.NewPassword);
        if (!result.IsSuccess)
        {
            IsTokenValid = true;
            ErrorMessage = result.Error;
            return Page();
        }

        TempData["Success"] = "Contraseña restablecida correctamente. Inicia sesión con tu nueva contraseña.";
        return RedirectToPage("/Auth/Login");
    }
}
