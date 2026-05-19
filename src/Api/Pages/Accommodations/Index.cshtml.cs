using FODUN.Reservations.Application.Dtos;
using FODUN.Reservations.Domain.Enums;
using FODUN.Reservations.Domain.Interfaces;
using FODUN.Reservations.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FODUN.Reservations.Api.Pages.Accommodations;

public class IndexModel : PageModel
{
    private readonly IAccommodationRepository _accommodationRepository;
    private readonly ReservationsDbContext _context;

    public IEnumerable<AccommodationDto> Accommodations { get; private set; } = [];
    public Dictionary<Guid, decimal> MinPrices { get; private set; } = [];

    public IndexModel(IAccommodationRepository accommodationRepository, ReservationsDbContext context)
    {
        _accommodationRepository = accommodationRepository;
        _context = context;
    }

    public async Task OnGetAsync()
    {
        var accs = await _accommodationRepository.GetAllActiveAsync();
        Accommodations = accs.Select(a => new AccommodationDto(
            a.Id, a.Code, a.Name, a.Description, a.Type.ToString(),
            a.City, a.Address, a.MaxCapacity,
            a.Seats.Select(s => new SeatDto(s.Id, s.SeatNumber, s.Type, s.Capacity, s.Description, s.Amenities))
        )).ToList();

        var allSeatIds = Accommodations.SelectMany(a => a.Seats.Select(s => s.Id)).ToList();

        var minTariffs = await _context.Tariffs
            .Where(t => allSeatIds.Contains(t.SeatId) && !t.IsExceptional && t.Season == SeasonType.Low)
            .GroupBy(t => t.SeatId)
            .Select(g => new { SeatId = g.Key, MinPrice = g.Min(t => t.PricePerNight) })
            .ToListAsync();

        var seatToAcc = Accommodations
            .SelectMany(a => a.Seats.Select(s => (s.Id, a.Id)))
            .ToDictionary(x => x.Item1, x => x.Item2);

        foreach (var t in minTariffs)
        {
            if (!seatToAcc.TryGetValue(t.SeatId, out var accId)) continue;
            if (!MinPrices.TryGetValue(accId, out var cur) || t.MinPrice < cur)
                MinPrices[accId] = t.MinPrice;
        }
    }
}
