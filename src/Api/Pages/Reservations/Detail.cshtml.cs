using System.Security.Claims;
using FODUN.Reservations.Application.Dtos;
using FODUN.Reservations.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FODUN.Reservations.Api.Pages.Reservations;

[Authorize]
public class DetailModel : PageModel
{
    private readonly IReservationService _reservationService;

    public DetailModel(IReservationService reservationService) =>
        _reservationService = reservationService;

    public ReservationDto? Reservation { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? SuccessMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return RedirectToPage("/Auth/Login");

        var result = await _reservationService.GetByIdAsync(id, userId);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Error;
            return Page();
        }

        Reservation = result.Value;
        if (TempData["Success"] is string msg)
            SuccessMessage = msg;

        return Page();
    }

    public async Task<IActionResult> OnPostConfirmAsync(Guid id)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return RedirectToPage("/Auth/Login");

        var result = await _reservationService.ConfirmAsync(id, userId);
        if (!result.IsSuccess)
        {
            var resResult = await _reservationService.GetByIdAsync(id, userId);
            if (resResult.IsSuccess) Reservation = resResult.Value;
            ErrorMessage = result.Error;
            return Page();
        }

        return RedirectToPage("/Reservations/Pay", new { id });
    }

    private Guid GetUserId()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idStr, out var id) ? id : Guid.Empty;
    }
}
