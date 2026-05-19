using FODUN.Reservations.Api.Models.Auth;
using FODUN.Reservations.Application.Commands.CreateUser;
using FODUN.Reservations.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FODUN.Reservations.Api.Pages.Auth;

public class RegisterModel : PageModel
{
    private readonly IUserService _userService;

    [BindProperty]
    public RegisterViewModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public RegisterModel(IUserService userService) => _userService = userService;

    public string? ErrorMessage { get; private set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            ErrorMessage = string.Join(" ", ModelState.Values
                .SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return Page();
        }

        var command = new CreateUserCommand(
            Input.DocumentNumber.Trim(), Input.FullName.Trim(), Input.Email,
            Input.Password, null);

        var result = await _userService.RegisterAsync(command);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Error;
            return Page();
        }

        TempData["SuccessMessage"] = "Cuenta creada. Ya puedes iniciar sesión.";
        return RedirectToPage("./Login", new { returnUrl = ReturnUrl });
    }
}
