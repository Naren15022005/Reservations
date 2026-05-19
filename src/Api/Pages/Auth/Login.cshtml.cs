using System.Security.Claims;
using FODUN.Reservations.Api.Models.Auth;
using FODUN.Reservations.Application.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FODUN.Reservations.Api.Pages.Auth;

public class LoginModel : PageModel
{
    private readonly IUserService _userService;

    [BindProperty]
    public LoginViewModel Input { get; set; } = new();

    public LoginModel(IUserService userService) => _userService = userService;

    public string? ErrorMessage { get; private set; }

    public void OnGet(string? returnUrl = null) => Input.ReturnUrl = returnUrl;

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            ErrorMessage = string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return Page();
        }

        var result = await _userService.LoginAsync(Input.Email, Input.Password);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Error;
            return Page();
        }

        var user = result.Value!;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var props = new AuthenticationProperties { IsPersistent = Input.RememberMe };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);

        return LocalRedirect(Input.ReturnUrl ?? "/Dashboard/Index");
    }
}
