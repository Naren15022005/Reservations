using FODUN.Reservations.Application.Dtos;
using FODUN.Reservations.Application.Services;
using FODUN.Reservations.Domain.Enums;
using FODUN.Reservations.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace FODUN.Reservations.Api.Pages.Accommodations;

public class AvailabilityModel : PageModel
{
    private readonly IAccommodationRepository _accommodationRepository;
    private readonly IAvailabilityService _availabilityService;
    private readonly ITariffService _tariffService;

    public AvailabilityModel(
        IAccommodationRepository accommodationRepository,
        IAvailabilityService availabilityService,
        ITariffService tariffService)
    {
        _accommodationRepository = accommodationRepository;
        _availabilityService = availabilityService;
        _tariffService = tariffService;
    }

    public Guid AccommodationId { get; private set; }
    public string AccommodationCode { get; private set; } = string.Empty;
    public string AccommodationName { get; private set; } = string.Empty;
    public string AccommodationCity { get; private set; } = string.Empty;
    public string AccommodationTypeLabel { get; private set; } = string.Empty;
    public string? AccommodationDescription { get; private set; }
    public string? AccommodationAddress { get; private set; }
    public int AccommodationMaxCapacity { get; private set; }
    public bool IsApartment { get; private set; }

    public DateOnly? CheckIn { get; private set; }
    public DateOnly? CheckOut { get; private set; }
    public int Persons { get; private set; }

    public bool SearchPerformed { get; private set; }
    public string? ErrorMessage { get; private set; }

    public IEnumerable<AvailableRoomDto> AvailableRooms { get; private set; } = [];
    public Dictionary<Guid, ReservationCostDto> CostMap { get; private set; } = [];

    public record TariffRowDto(string SeatName, int Capacity, decimal? LowPrice, decimal? HighPrice, decimal? AdditionalPersonPrice);
    public IList<TariffRowDto> TariffRows { get; private set; } = [];
    public string OccupiedDatesJson { get; private set; } = "[]";

    public record SeatGalleryDto(Guid SeatId, string SeatNumber, string Type, int Capacity, string? Description, string? Amenities, decimal? LowPrice, decimal? HighPrice);
    public IList<SeatGalleryDto> AllSeats { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(
        Guid accommodationId,
        string? checkIn,
        string? checkOut,
        int persons = 1)
    {
        // GetByIdAsync ya incluye Seats + Tariffs (ThenInclude en el repositorio)
        var accommodation = await _accommodationRepository.GetByIdAsync(accommodationId);
        if (accommodation is null)
            return NotFound();

        AccommodationId = accommodationId;
        AccommodationCode = accommodation.Code;
        AccommodationName = accommodation.Name;
        AccommodationCity = accommodation.City;
        AccommodationTypeLabel = accommodation.Type == AccommodationType.Apartment
            ? "Apartamentos" : "Sede Recreativa";
        IsApartment = accommodation.Type == AccommodationType.Apartment;
        AccommodationDescription = accommodation.Description;
        AccommodationAddress = accommodation.Address;
        AccommodationMaxCapacity = accommodation.MaxCapacity;
        Persons = persons < 1 ? 1 : persons;

        var seatIds = accommodation.Seats.Select(s => s.Id).ToList();

        // Tariffs ya cargados via ThenInclude — sin query adicional al DbContext
        TariffRows = accommodation.Seats.Select(s =>
        {
            var nonExceptional = s.Tariffs.Where(t => !t.IsExceptional).ToList();
            var low  = nonExceptional.FirstOrDefault(t => t.Season == SeasonType.Low);
            var high = nonExceptional.FirstOrDefault(t => t.Season == SeasonType.High);
            return new TariffRowDto(
                $"{s.SeatNumber} · {s.Type}",
                s.Capacity,
                low?.PricePerNight,
                high?.PricePerNight,
                low?.AdditionalPersonPrice);
        }).ToList();

        AllSeats = accommodation.Seats.Select(s =>
        {
            var nonExceptional = s.Tariffs.Where(t => !t.IsExceptional).ToList();
            var low  = nonExceptional.FirstOrDefault(t => t.Season == SeasonType.Low);
            var high = nonExceptional.FirstOrDefault(t => t.Season == SeasonType.High);
            return new SeatGalleryDto(
                s.Id, s.SeatNumber, s.Type, s.Capacity,
                s.Description, s.Amenities,
                low?.PricePerNight, high?.PricePerNight);
        }).ToList();

        // Fechas ocupadas para el calendario — delegado al repositorio
        var today = DateOnly.FromDateTime(DateTime.Today);
        var occupiedDates = await _accommodationRepository.GetOccupiedDatesAsync(
            seatIds, today, today.AddMonths(6));

        OccupiedDatesJson = JsonSerializer.Serialize(
            occupiedDates.OrderBy(d => d).ToList());

        if (string.IsNullOrEmpty(checkIn) || string.IsNullOrEmpty(checkOut))
            return Page();

        if (!DateOnly.TryParse(checkIn, out var ci) || !DateOnly.TryParse(checkOut, out var co))
        {
            ErrorMessage = "Formato de fecha inválido.";
            SearchPerformed = true;
            return Page();
        }

        if (ci >= co)
        {
            ErrorMessage = "La fecha de check-in debe ser anterior al check-out.";
            SearchPerformed = true;
            return Page();
        }

        if (ci < today)
        {
            ErrorMessage = "La fecha de check-in no puede ser en el pasado.";
            SearchPerformed = true;
            return Page();
        }

        CheckIn = ci;
        CheckOut = co;
        SearchPerformed = true;

        var result = await _availabilityService.GetAvailableRoomsAsync(
            accommodationId, ci, co, Persons);

        if (!result.IsSuccess)
        {
            ErrorMessage = result.Error;
            return Page();
        }

        AvailableRooms = result.Value!;

        foreach (var room in AvailableRooms)
        {
            var costResult = await _tariffService.CalculateCostAsync(
                room.SeatId, ci, co, Persons, includeLaundry: false);
            if (costResult.IsSuccess && costResult.Value is not null)
                CostMap[room.SeatId] = costResult.Value;
        }

        return Page();
    }
}
