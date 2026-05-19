using System.Security.Claims;
using FODUN.Reservations.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FODUN.Reservations.Api.Pages.Profile;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IUserService _userService;

    public IndexModel(IUserService userService) => _userService = userService;

    [BindProperty] public string FullName    { get; set; } = string.Empty;
    [BindProperty] public string? PhoneNumber { get; set; }

    public string Email           { get; private set; } = string.Empty;
    public string DocumentNumber  { get; private set; } = string.Empty;
    public bool   IsEmailConfirmed { get; private set; }
    public string MemberSince     { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return RedirectToPage("/Auth/Login");

        var result = await _userService.GetByIdAsync(userId);
        if (!result.IsSuccess) return RedirectToPage("/Dashboard/Index");

        var u = result.Value!;
        FullName       = u.FullName;
        PhoneNumber    = u.PhoneNumber;
        Email          = u.Email;
        DocumentNumber = u.DocumentNumber;
        IsEmailConfirmed = u.IsEmailConfirmed;
        MemberSince    = u.CreatedAt.ToString("dd/MM/yyyy");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return RedirectToPage("/Auth/Login");

        if (string.IsNullOrWhiteSpace(FullName))
            ModelState.AddModelError(nameof(FullName), "El nombre es obligatorio.");

        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        var result = await _userService.UpdateProfileAsync(userId, FullName, PhoneNumber);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToPage();
        }

        // Refresh name claim so the navbar shows the updated name immediately
        HttpContext.Response.Cookies.Append("profile_updated", "1",
            new CookieOptions { MaxAge = TimeSpan.FromSeconds(5) });

        TempData["Success"] = "Perfil actualizado correctamente.";
        return RedirectToPage();
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}
