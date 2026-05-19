namespace FODUN.Reservations.Api.Pages.Reservations;

public sealed class CreateReservationInput
{
    public Guid AccommodationId { get; set; }
    public Guid SeatId { get; set; }
    public string CheckIn { get; set; } = string.Empty;
    public string CheckOut { get; set; } = string.Empty;
    public int TotalPersons { get; set; } = 1;
    public bool IncludeLaundry { get; set; }
    public string? Notes { get; set; }
}
