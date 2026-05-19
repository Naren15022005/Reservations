using System.Security.Claims;
using FODUN.Reservations.Application.Commands.CreateReservation;
using FODUN.Reservations.Application.Dtos;
using FODUN.Reservations.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FODUN.Reservations.Api.Pages.Reservations;

[Authorize]
public class CreateModel : PageModel
{
    private readonly IReservationService _reservationService;
    private readonly IAvailabilityService _availabilityService;
    private readonly ITariffService _tariffService;

    public CreateModel(
        IReservationService reservationService,
        IAvailabilityService availabilityService,
        ITariffService tariffService)
    {
        _reservationService = reservationService;
        _availabilityService = availabilityService;
        _tariffService = tariffService;
    }

    [BindProperty]
    public CreateReservationInput Input { get; set; } = new();

    public AvailableRoomDto? RoomInfo { get; private set; }
    public ReservationCostDto? CostEstimate { get; private set; }
    public string? ErrorMessage { get; private set; }

    public DateOnly CheckIn { get; private set; }
    public DateOnly CheckOut { get; private set; }
    public int Persons { get; private set; }
    public bool IncludeLaundry => Input.IncludeLaundry;

    public async Task<IActionResult> OnGetAsync(
        Guid seatId, Guid accommodationId, string checkIn, string checkOut, int persons = 1)
    {
        if (!DateOnly.TryParse(checkIn, out var ci) || !DateOnly.TryParse(checkOut, out var co))
        {
            ErrorMessage = "Fechas inválidas.";
            return Page();
        }

        CheckIn = ci;
        CheckOut = co;
        Persons = persons;

        Input.AccommodationId = accommodationId;
        Input.SeatId = seatId;
        Input.CheckIn = checkIn;
        Input.CheckOut = checkOut;
        Input.TotalPersons = persons;

        var availResult = await _availabilityService.GetAvailableRoomsAsync(
            accommodationId, ci, co, persons);

        if (!availResult.IsSuccess)
        {
            ErrorMessage = availResult.Error;
            return Page();
        }

        RoomInfo = availResult.Value?.FirstOrDefault(r => r.SeatId == seatId);
        if (RoomInfo is null)
        {
            ErrorMessage = "El alojamiento seleccionado no está disponible para las fechas indicadas.";
            return Page();
        }

        var costResult = await _tariffService.CalculateCostAsync(
            seatId, ci, co, persons, includeLaundry: false);

        if (costResult.IsSuccess)
            CostEstimate = costResult.Value;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return RedirectToPage("/Auth/Login");

        if (!DateOnly.TryParse(Input.CheckIn, out var ci) ||
            !DateOnly.TryParse(Input.CheckOut, out var co))
        {
            ErrorMessage = "Las fechas de la reserva son inválidas.";
            return Page();
        }

        CheckIn = ci;
        CheckOut = co;
        Persons = Input.TotalPersons;

        var command = new CreateReservationCommand(
            userId,
            Input.AccommodationId,
            ci, co,
            Input.TotalPersons,
            Input.IncludeLaundry,
            Input.Notes,
            Input.SeatId);

        var result = await _reservationService.CreateAsync(command);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Error;
            await RestoreCostAsync(ci, co);
            return Page();
        }

        return RedirectToPage("/Reservations/Pay", new { id = result.Value!.Id });
    }

    private async Task RestoreCostAsync(DateOnly ci, DateOnly co)
    {
        if (Input.SeatId != Guid.Empty && ci != default && co != default)
        {
            var costResult = await _tariffService.CalculateCostAsync(
                Input.SeatId, ci, co, Input.TotalPersons, Input.IncludeLaundry);
            if (costResult.IsSuccess)
                CostEstimate = costResult.Value;

            var availResult = await _availabilityService.GetAvailableRoomsAsync(
                Input.AccommodationId, ci, co, Input.TotalPersons);
            if (availResult.IsSuccess)
                RoomInfo = availResult.Value?.FirstOrDefault(r => r.SeatId == Input.SeatId);
        }
    }
}
