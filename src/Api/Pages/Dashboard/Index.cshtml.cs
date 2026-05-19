using System.Security.Claims;
using FODUN.Reservations.Application.Dtos;
using FODUN.Reservations.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FODUN.Reservations.Api.Pages.Dashboard;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IReservationService _reservationService;
    public IEnumerable<ReservationDto> Reservations { get; private set; } = [];

    public IndexModel(IReservationService reservationService) => _reservationService = reservationService;

    public async Task OnGetAsync()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(claim, out var userId)) return;

        var result = await _reservationService.GetByUserAsync(userId);
        if (result.IsSuccess)
            Reservations = result.Value!;
    }
}
