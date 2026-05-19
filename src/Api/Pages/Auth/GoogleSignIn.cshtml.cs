using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FODUN.Reservations.Api.Pages.Auth;

public class GoogleSignInModel : PageModel
{
    public IActionResult OnGet(string? returnUrl = null)
    {
        var redirectUrl = Url.Page("/Auth/GoogleCallback", pageHandler: null,
            values: new { returnUrl }, protocol: Request.Scheme);

        var props = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(props, GoogleDefaults.AuthenticationScheme);
    }
}
