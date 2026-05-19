using System.Security.Claims;
using FODUN.Reservations.Application.Dtos;
using FODUN.Reservations.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FODUN.Reservations.Api.Pages.Dashboard;

[Authorize]
public class SubmitReceiptModel : PageModel
{
    private readonly IReservationService _reservationService;

    public SubmitReceiptModel(IReservationService reservationService) =>
        _reservationService = reservationService;

    public ReservationDto? Reservation { get; private set; }
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return RedirectToPage("/Auth/Login");

        var result = await _reservationService.GetByIdAsync(id, userId);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToPage("./Index");
        }

        Reservation = result.Value;
        if (Reservation?.Status == "Cancelled")
        {
            TempData["Error"] = "No se puede registrar pago en una reserva cancelada.";
            return RedirectToPage("./Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return RedirectToPage("/Auth/Login");

        var result = await _reservationService.SubmitPaymentReceiptAsync(id, userId);
        if (!result.IsSuccess)
        {
            var resResult = await _reservationService.GetByIdAsync(id, userId);
            Reservation = resResult.IsSuccess ? resResult.Value : null;
            ErrorMessage = result.Error;
            return Page();
        }

        TempData["Success"] = "Comprobante de pago registrado correctamente.";
        return RedirectToPage("./Index");
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}
