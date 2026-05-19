using System.Security.Claims;
using FODUN.Reservations.Application.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FODUN.Reservations.Api.Pages.Auth;

public class GoogleCallbackModel : PageModel
{
    private readonly IUserService _userService;
    private readonly ILogger<GoogleCallbackModel> _logger;

    public GoogleCallbackModel(IUserService userService, ILogger<GoogleCallbackModel> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
    {
        var externalResult = await HttpContext.AuthenticateAsync("External");
        if (!externalResult.Succeeded)
        {
            _logger.LogWarning("Autenticación externa fallida: {Error}", externalResult.Failure?.Message);
            TempData["ErrorMessage"] = "No se pudo completar el inicio de sesión con Google.";
            return RedirectToPage("/Auth/Login");
        }

        await HttpContext.SignOutAsync("External");

        var principal = externalResult.Principal!;
        var email     = principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var name      = principal.FindFirstValue(ClaimTypes.Name)  ?? string.Empty;
        var googleId  = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email))
        {
            TempData["ErrorMessage"] = "Google no proporcionó un correo electrónico.";
            return RedirectToPage("/Auth/Login");
        }

        var result = await _userService.GetOrCreateExternalUserAsync(email, name, "Google", googleId);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error;
            return RedirectToPage("/Auth/Login");
        }

        var user = result.Value!;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email)
        };

        var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var appPrincipal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, appPrincipal);

        return LocalRedirect(returnUrl ?? "/Dashboard/Index");
    }
}
