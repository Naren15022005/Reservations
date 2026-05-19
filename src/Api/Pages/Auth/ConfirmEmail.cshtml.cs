using FODUN.Reservations.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FODUN.Reservations.Api.Pages.Auth;

public class ConfirmEmailModel : PageModel
{
    private readonly IUserService _userService;

    public ConfirmEmailModel(IUserService userService)
    {
        _userService = userService;
    }

    public bool Success { get; private set; }
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid userId, string token)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(token))
        {
            ErrorMessage = "El enlace de confirmación es inválido o está incompleto.";
            return Page();
        }

        var result = await _userService.ConfirmEmailAsync(userId, token);
        if (result.IsSuccess)
        {
            Success = true;
        }
        else
        {
            ErrorMessage = result.Error ?? "El enlace de confirmación es inválido o ha expirado.";
        }

        return Page();
    }
}
