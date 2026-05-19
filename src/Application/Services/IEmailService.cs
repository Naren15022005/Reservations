namespace FODUN.Reservations.Application.Services;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetLink, CancellationToken cancellationToken = default);
    Task SendEmailConfirmationAsync(string toEmail, string toName, string confirmationLink, CancellationToken cancellationToken = default);
    Task SendReservationConfirmationAsync(string toEmail, string toName, Guid reservationId, CancellationToken cancellationToken = default);
    Task SendPaymentTicketAsync(string toEmail, string toName, string ticketHtml, string reservationRef, byte[]? pdfAttachment = null, CancellationToken cancellationToken = default);
}
