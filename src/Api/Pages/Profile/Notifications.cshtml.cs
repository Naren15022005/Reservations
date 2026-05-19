using FODUN.Reservations.Application.Dtos;
using FODUN.Reservations.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace FODUN.Reservations.Api.Pages.Profile;

[Authorize]
public sealed class NotificationsModel : PageModel
{
    private readonly INotificationService _notificationService;

    public NotificationsModel(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public IEnumerable<NotificationDto> Notifications { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return RedirectToPage("/Auth/Login");

        await _notificationService.MarkAllAsReadAsync(userId, cancellationToken);

        var result = await _notificationService.GetByUserAsync(userId, cancellationToken);
        if (result.IsSuccess)
            Notifications = result.Value!;

        return Page();
    }

    public async Task<IActionResult> OnPostMarkReadAsync(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId != Guid.Empty)
            await _notificationService.MarkAllAsReadAsync(userId, cancellationToken);

        return RedirectToPage();
    }

    private Guid GetUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }
}
