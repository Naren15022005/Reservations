using System.Net;
using System.Net.Mail;
using FODUN.Reservations.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FODUN.Reservations.Infrastructure.External;

public sealed class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendPasswordResetEmailAsync(
        string toEmail, string toName, string resetLink, CancellationToken cancellationToken = default)
    {
        var subject = "Restablecer contraseña";
        var body = $"""
            <h2>Hola {toName},</h2>
            <p>Recibimos una solicitud para restablecer tu contraseña.</p>
            <p>Haz clic en el siguiente enlace para continuar (válido por 30 minutos):</p>
            <a href="{resetLink}" style="background:#007bff;color:white;padding:10px 20px;text-decoration:none;border-radius:5px;">
                Restablecer contraseña
            </a>
            <p>Si no solicitaste esto, puedes ignorar este mensaje.</p>
            <br><p>Sistema de Reservas</p>
            """;
        await SendEmailAsync(toEmail, toName, subject, body, cancellationToken);
    }

    public async Task SendEmailConfirmationAsync(
        string toEmail, string toName, string confirmationLink, CancellationToken cancellationToken = default)
    {
        var subject = "Confirma tu correo electrónico";
        var body = $"""
            <h2>Bienvenido, {toName}!</h2>
            <p>Confirma tu correo electrónico haciendo clic en el siguiente enlace:</p>
            <a href="{confirmationLink}" style="background:#28a745;color:white;padding:10px 20px;text-decoration:none;border-radius:5px;">
                Confirmar correo
            </a>
            <br><p>Sistema de Reservas</p>
            """;
        await SendEmailAsync(toEmail, toName, subject, body, cancellationToken);
    }

    public async Task SendReservationConfirmationAsync(
        string toEmail, string toName, Guid reservationId, CancellationToken cancellationToken = default)
    {
        var subject = "Confirmación de reserva";
        var body = $"""
            <h2>Hola {toName},</h2>
            <p>Tu reserva ha sido registrada exitosamente.</p>
            <p><strong>Número de reserva:</strong> {reservationId}</p>
            <p>Inicia sesión para ver todos los detalles de tu reserva.</p>
            <br><p>Sistema de Reservas</p>
            """;
        await SendEmailAsync(toEmail, toName, subject, body, cancellationToken);
    }

    public async Task SendPaymentTicketAsync(
        string toEmail, string toName, string ticketHtml, string reservationRef,
        byte[]? pdfAttachment = null, CancellationToken cancellationToken = default)
    {
        var subject = $"✅ Proceso exitoso — Ticket #{reservationRef}";
        var body = $"""
            <div style="font-family:'Segoe UI',Arial,sans-serif;max-width:600px;margin:0 auto;background:#fff;border-radius:10px;overflow:hidden;border:1px solid #eee">
              <div style="background:#8B1A1A;padding:28px 32px;text-align:center">
                <h1 style="color:#fff;font-size:20px;margin:0">Proceso completado exitosamente</h1>
                <p style="color:rgba(255,255,255,.85);font-size:13px;margin:8px 0 0">Tu comprobante de pago fue recibido</p>
              </div>
              <div style="padding:28px 32px">
                <p style="font-size:14px;color:#333">Hola <strong>{toName}</strong>,</p>
                <p style="font-size:14px;color:#555;margin-top:10px">
                  Hemos recibido tu comprobante de pago para la reserva <strong>#{reservationRef}</strong>.
                  Nuestro equipo lo verificará y recibirás confirmación final en breve.
                </p>
                <div style="background:#f8f8f8;border-radius:8px;padding:16px;margin:20px 0;font-size:13px;color:#555">
                  Adjunto a este correo encontrarás tu <strong>ticket de reserva en PDF</strong> como constancia del proceso.
                </div>
                <p style="font-size:12px;color:#999;margin-top:20px">Sistema de Reservas &mdash; Sedes y Apartamentos</p>
              </div>
            </div>
            """;

        var smtp = _configuration.GetSection("Smtp");
        var host = smtp["Host"] ?? "smtp.gmail.com";
        var port = int.Parse(smtp["Port"] ?? "587");
        var fromEmail = smtp["From"] ?? throw new InvalidOperationException("SMTP From no configurado.");
        var fromName = smtp["FromName"] ?? "Sistema de Reservas";
        var username = smtp["Username"] ?? fromEmail;
        var password = smtp["Password"] ?? throw new InvalidOperationException("SMTP Password no configurado.");

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(username, password)
        };

        using var message = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };
        message.To.Add(new MailAddress(toEmail, toName));

        MemoryStream? attachStream = null;
        if (pdfAttachment is { Length: > 0 })
        {
            attachStream = new MemoryStream(pdfAttachment);
            message.Attachments.Add(new Attachment(attachStream, $"ticket-{reservationRef}.pdf", "application/pdf"));
        }

        try
        {
            await client.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("Ticket enviado a {Email} — Ref: #{Ref}", toEmail, reservationRef);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar ticket a {Email}", toEmail);
            throw;
        }
        finally
        {
            attachStream?.Dispose();
        }
    }

    private async Task SendEmailAsync(
        string toEmail, string toName, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        var smtp = _configuration.GetSection("Smtp");
        var host = smtp["Host"] ?? "smtp.gmail.com";
        var port = int.Parse(smtp["Port"] ?? "587");
        var fromEmail = smtp["From"] ?? throw new InvalidOperationException("SMTP From no configurado.");
        var fromName = smtp["FromName"] ?? "Sistema de Reservas";
        var username = smtp["Username"] ?? fromEmail;
        var password = smtp["Password"] ?? throw new InvalidOperationException("SMTP Password no configurado.");

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(username, password)
        };

        using var message = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(new MailAddress(toEmail, toName));

        try
        {
            await client.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("Correo enviado a {Email} — Asunto: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar correo a {Email}", toEmail);
            throw;
        }
    }
}
