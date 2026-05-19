using FODUN.Reservations.Api.Models.Auth;
using FODUN.Reservations.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FODUN.Reservations.Api.Pages.Auth;

public class ForgotPasswordModel : PageModel
{
    private readonly IUserService _userService;

    [BindProperty]
    public ForgotPasswordViewModel Input { get; set; } = new();

    public ForgotPasswordModel(IUserService userService) => _userService = userService;

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        await _userService.RequestPasswordResetAsync(Input.Email);
        TempData["SuccessMessage"] = "Si el correo existe, recibirás un enlace en los próximos minutos.";
        return RedirectToPage("./Login");
    }
}
