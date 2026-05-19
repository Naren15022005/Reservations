using FODUN.Reservations.Application.Dtos;

namespace FODUN.Reservations.Application.Services;

public interface ITicketService
{
    string GenerateHtml(ReservationDto reservation, string userName, string userEmail);
    byte[] GeneratePdf(ReservationDto reservation, string userName, string userEmail);
    Task SaveHtmlAsync(Guid reservationId, string html, CancellationToken cancellationToken = default);
    Task SavePdfAsync(Guid reservationId, byte[] pdf, CancellationToken cancellationToken = default);
    bool TicketExists(Guid reservationId);
}
