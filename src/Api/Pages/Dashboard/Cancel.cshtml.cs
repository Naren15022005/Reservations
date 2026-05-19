using System.Security.Claims;
using FODUN.Reservations.Application.Commands.CancelReservation;
using FODUN.Reservations.Application.Dtos;
using FODUN.Reservations.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FODUN.Reservations.Api.Pages.Dashboard;

[Authorize]
public class CancelModel : PageModel
{
    private readonly IReservationService _reservationService;

    public CancelModel(IReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    public ReservationDto? Reservation { get; private set; }
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return RedirectToPage("/Auth/Login");

        var result = await _reservationService.GetByIdAsync(id, userId);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToPage("./Index");
        }

        Reservation = result.Value;
        if (Reservation?.Status is not ("Pending" or "Confirmed"))
        {
            TempData["Error"] = "Esta reserva no puede ser cancelada en su estado actual.";
            return RedirectToPage("./Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id, string reason)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return RedirectToPage("/Auth/Login");

        if (string.IsNullOrWhiteSpace(reason))
        {
            var getResult = await _reservationService.GetByIdAsync(id, userId);
            Reservation = getResult.IsSuccess ? getResult.Value : null;
            ErrorMessage = "El motivo de cancelación es requerido.";
            return Page();
        }

        var command = new CancelReservationCommand(id, userId, reason.Trim());
        var result = await _reservationService.CancelAsync(command);

        if (!result.IsSuccess)
        {
            var getResult = await _reservationService.GetByIdAsync(id, userId);
            Reservation = getResult.IsSuccess ? getResult.Value : null;
            ErrorMessage = result.Error;
            return Page();
        }

        TempData["Success"] = "La reserva ha sido cancelada correctamente.";
        return RedirectToPage("./Index");
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}
