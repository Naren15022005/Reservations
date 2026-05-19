using System.Security.Claims;
using FODUN.Reservations.Application.Dtos;
using FODUN.Reservations.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FODUN.Reservations.Api.Pages.Reservations;

[Authorize]
public class PayModel : PageModel
{
    private readonly IReservationService _reservationService;
    private readonly ITicketService _ticketService;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<PayModel> _logger;

    public PayModel(
        IReservationService reservationService,
        ITicketService ticketService,
        IEmailService emailService,
        INotificationService notificationService,
        IWebHostEnvironment env,
        ILogger<PayModel> logger)
    {
        _reservationService  = reservationService;
        _ticketService       = ticketService;
        _emailService        = emailService;
        _notificationService = notificationService;
        _env                 = env;
        _logger              = logger;
    }

    public ReservationDto? Reservation { get; private set; }
    public string? ErrorMessage { get; private set; }

    [BindProperty(SupportsGet = true)] public string Step { get; set; } = "method";
    [BindProperty(SupportsGet = true)] public string? SelectedMethod { get; set; }
    [BindProperty(SupportsGet = true)] public string? SelectedBank { get; set; }

    [BindProperty] public string PaymentMethod { get; set; } = "PSE";
    [BindProperty] public string? Bank { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return RedirectToPage("/Auth/Login");

        var result = await _reservationService.GetByIdAsync(id, userId);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToPage("/Dashboard/Index");
        }

        Reservation = result.Value;

        if (Reservation!.HasPaymentReceipt)
        {
            TempData["Success"] = "El pago ya fue registrado correctamente.";
            return RedirectToPage("/Dashboard/Index");
        }

        if (Reservation.Status == "Cancelled")
        {
            TempData["Error"] = "Esta reserva ha sido cancelada.";
            return RedirectToPage("/Dashboard/Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostPayAsync(Guid id)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return RedirectToPage("/Auth/Login");

        var result = await _reservationService.GetByIdAsync(id, userId);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToPage("/Dashboard/Index");
        }
        Reservation = result.Value;

        if (string.IsNullOrEmpty(PaymentMethod))
        {
            ErrorMessage = "Selecciona un método de pago.";
            Step = "method";
            return Page();
        }

        return RedirectToPage(new
        {
            id,
            step           = "upload",
            selectedMethod = PaymentMethod,
            selectedBank   = Bank
        });
    }

    public async Task<IActionResult> OnPostUploadAsync(Guid id, IFormFile? receipt)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return RedirectToPage("/Auth/Login");

        var resResult = await _reservationService.GetByIdAsync(id, userId);
        if (!resResult.IsSuccess)
        {
            TempData["Error"] = resResult.Error;
            return RedirectToPage("/Dashboard/Index");
        }
        Reservation = resResult.Value;

        if (receipt is null || receipt.Length == 0)
        {
            ErrorMessage = "Debes adjuntar el comprobante de pago.";
            Step = "upload";
            return Page();
        }

        var allowed = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
        var ext = Path.GetExtension(receipt.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext))
        {
            ErrorMessage = "Solo se permiten archivos JPG, PNG o PDF.";
            Step = "upload";
            return Page();
        }

        if (receipt.Length > 5 * 1024 * 1024)
        {
            ErrorMessage = "El archivo no puede superar los 5 MB.";
            Step = "upload";
            return Page();
        }

        var fileName = $"{id:N}_{DateTime.UtcNow:yyyyMMddHHmmss}{ext}";
        var uploadsPath = Path.Combine(_env.WebRootPath, "uploads", "receipts");
        Directory.CreateDirectory(uploadsPath);

        await using var stream = new FileStream(Path.Combine(uploadsPath, fileName), FileMode.Create);
        await receipt.CopyToAsync(stream);

        var result = await _reservationService.SubmitPaymentReceiptAsync(
            id, userId, SelectedMethod, fileName);

        if (!result.IsSuccess)
        {
            ErrorMessage = result.Error;
            Step = "upload";
            return Page();
        }

        // Generar y guardar ticket HTML + PDF
        var updatedResult = await _reservationService.GetByIdAsync(id, userId);
        if (updatedResult.IsSuccess)
        {
            var userName  = User.Identity!.Name ?? "Usuario";
            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "";
            var res       = updatedResult.Value!;
            var shortRef  = id.ToString("N")[..8].ToUpper();

            var html = _ticketService.GenerateHtml(res, userName, userEmail);
            await _ticketService.SaveHtmlAsync(id, html);

            byte[]? pdfBytes = null;
            try
            {
                pdfBytes = _ticketService.GeneratePdf(res, userName, userEmail);
                await _ticketService.SavePdfAsync(id, pdfBytes);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo generar el PDF del ticket para reserva {Id}", id);
            }

            if (!string.IsNullOrWhiteSpace(userEmail))
            {
                try
                {
                    await _emailService.SendPaymentTicketAsync(userEmail, userName, html, shortRef, pdfBytes);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo enviar el ticket por correo a {Email}", userEmail);
                }
            }

            try
            {
                await _notificationService.CreateAsync(
                    userId,
                    "Comprobante recibido",
                    $"Recibimos tu comprobante de pago para la reserva #{shortRef}. Pronto verificaremos y confirmaremos.",
                    "payment",
                    id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo crear la notificación de pago para reserva {Id}", id);
            }
        }

        TempData["Success"] = "¡Pago registrado! Te enviamos el ticket de confirmación a tu correo.";
        return RedirectToPage("/Dashboard/Index");
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}
