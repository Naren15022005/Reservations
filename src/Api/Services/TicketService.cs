using FODUN.Reservations.Application.Dtos;
using FODUN.Reservations.Application.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FODUN.Reservations.Api.Services;

public sealed class TicketService : ITicketService
{
    private readonly IWebHostEnvironment _env;

    public TicketService(IWebHostEnvironment env)
    {
        _env = env;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private static string LocalizeStatus(string status, bool hasPaid) => status switch
    {
        "Cancelled"  => "Cancelada",
        "CheckedOut" => "Finalizada",
        "CheckedIn"  => hasPaid ? "Pagada" : "Pendiente de pago",
        _            => hasPaid ? "Pago enviado" : "Pendiente de pago"
    };

    // ── PDF ─────────────────────────────────────────────────────────────────
    public byte[] GeneratePdf(ReservationDto reservation, string userName, string userEmail)
    {
        var shortId   = reservation.Id.ToString("N")[..8].ToUpper();
        var nights    = reservation.CheckOutDate.DayNumber - reservation.CheckInDate.DayNumber;
        var nightLbl  = nights == 1 ? "noche" : "noches";
        var generated = DateTime.Now.ToString("dd/MM/yyyy");
        var statusLbl = LocalizeStatus(reservation.Status, reservation.HasPaymentReceipt);
        var baseCost  = reservation.TotalCost - reservation.LaundryServiceCost;
        var checkin   = reservation.CheckInDate.ToString("dd/MM/yyyy");
        var checkout  = reservation.CheckOutDate.ToString("dd/MM/yyyy");
        var docBg     = Color.FromHex("#eeecea");
        var line      = Color.FromHex("#cccccc");
        var lineLight = Color.FromHex("#d8d6d6");
        var ink2      = Color.FromHex("#888888");
        var ink3      = Color.FromHex("#aaaaaa");

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(56);
                page.PageColor(docBg);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10).FontColor("#1a1a1a"));

                page.Content().Column(col =>
                {
                    col.Spacing(0);

                    // ── HEADER ───────────────────────────────────────────
                    col.Item().PaddingBottom(40).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Sistema de Reservas").FontSize(15).Bold();
                            c.Item().PaddingTop(2).Text("Sedes y Apartamentos").FontSize(11).FontColor(ink2);
                            c.Item().PaddingTop(22).Text(userName).FontSize(13).Bold();
                            c.Item().PaddingTop(2).Text(userEmail).FontSize(11).FontColor(ink2);
                        });

                        row.AutoItem().Column(c =>
                        {
                            c.Item().AlignRight().Text("COMPROBANTE")
                                .FontSize(34).Bold().LetterSpacing(-0.02f);
                            c.Item().AlignRight().PaddingTop(6)
                                .Text($"#{shortId}").FontSize(14).FontColor(ink2);
                        });
                    });

                    // ── INFO ROW ─────────────────────────────────────────
                    col.Item()
                        .BorderTop(1).BorderColor(line)
                        .BorderBottom(1).BorderColor(line)
                        .PaddingVertical(14)
                        .PaddingBottom(14)
                        .Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("LUGAR").FontSize(8).FontColor(ink3).LetterSpacing(0.06f);
                            c.Item().PaddingTop(4).Text(reservation.PlaceName).FontSize(11).Bold();
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("PERÍODO").FontSize(8).FontColor(ink3).LetterSpacing(0.06f);
                            c.Item().PaddingTop(4).Text($"{checkin} — {checkout}").FontSize(11).Bold();
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().AlignRight().Text("FECHA DE EMISIÓN").FontSize(8).FontColor(ink3).LetterSpacing(0.06f);
                            c.Item().AlignRight().PaddingTop(4).Text(generated).FontSize(11).Bold();
                        });
                    });

                    // ── DESCRIPTION ──────────────────────────────────────
                    col.Item().PaddingTop(30).PaddingBottom(14)
                        .Text("Descripción").FontSize(22).Bold();

                    // ── ITEMS TABLE ───────────────────────────────────────
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3.5f);
                            c.RelativeColumn(1.5f);
                            c.RelativeColumn(1f);
                            c.RelativeColumn(1.8f);
                        });

                        foreach (var h in new[] { "CONCEPTO", "DURACIÓN", "PERSONAS", "IMPORTE" })
                            table.Cell().PaddingBottom(9).BorderBottom(1).BorderColor(line)
                                .Text(h).FontSize(8).FontColor(ink3).LetterSpacing(0.06f);

                        // Alojamiento
                        PdfItemRow(table, $"Alojamiento — {reservation.PlaceName}",
                            $"{nights} {nightLbl}", reservation.TotalPersons.ToString(),
                            baseCost.ToString("C0"), lineLight);

                        // Lavandería
                        if (reservation.LaundryService)
                            PdfItemRow(table, "Servicio de lavandería", "—", "—",
                                reservation.LaundryServiceCost.ToString("C0"), lineLight);
                    });

                    // ── TOTALS ────────────────────────────────────────────
                    col.Item().PaddingTop(6).PaddingBottom(36).Row(outer =>
                    {
                        outer.RelativeItem();
                        outer.ConstantItem(220).Column(c =>
                        {
                            TotRow(c, "Subtotal", baseCost.ToString("C0"), lineLight, ink2, false);
                            if (reservation.LaundryService)
                                TotRow(c, "Lavandería", reservation.LaundryServiceCost.ToString("C0"), lineLight, ink2, false);
                            TotRow(c, "Total", reservation.TotalCost.ToString("C0"), lineLight, "#1a1a1a", true);
                        });
                    });

                    // ── FOOTER ────────────────────────────────────────────
                    col.Item().BorderTop(1).BorderColor(line).PaddingTop(22).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().PaddingBottom(12).Text("Detalles de la reserva").FontSize(12).Bold();
                            FootRow(c, "Ingreso",      checkin,                             ink3);
                            FootRow(c, "Salida",       checkout,                            ink3);
                            FootRow(c, "Duración",     $"{nights} {nightLbl}",              ink3);
                            FootRow(c, "Personas",     reservation.TotalPersons.ToString(), ink3);
                            FootRow(c, "Habitaciones", reservation.NumberOfRoomsNeeded.ToString(), ink3);
                        });

                        row.ConstantItem(28);

                        row.RelativeItem().Column(c =>
                        {
                            c.Item().PaddingBottom(12).Text("Estado del proceso").FontSize(12).Bold();
                            c.Item().Text(
                                "El comprobante de pago fue recibido. Nuestro equipo verificará el pago y enviará confirmación final.")
                                .FontSize(10.5f).FontColor("#666666").LineHeight(1.55f);
                            c.Item().PaddingTop(12).Column(inner =>
                            {
                                FootRow(inner, "Estado",     statusLbl,    ink3);
                                FootRow(inner, "Referencia", $"#{shortId}", ink3);
                                FootRow(inner, "Emitido",    generated,    ink3);
                            });
                        });
                    });
                });
            });
        }).GeneratePdf();
    }

    // ── HTML ────────────────────────────────────────────────────────────────
    public string GenerateHtml(ReservationDto reservation, string userName, string userEmail)
    {
        var shortId      = reservation.Id.ToString("N")[..8].ToUpper();
        var nights       = reservation.CheckOutDate.DayNumber - reservation.CheckInDate.DayNumber;
        var nightLbl     = nights == 1 ? "noche" : "noches";
        var generated    = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        var statusLbl    = LocalizeStatus(reservation.Status, reservation.HasPaymentReceipt);
        var baseCost     = (reservation.TotalCost - reservation.LaundryServiceCost).ToString("C0");
        var checkin      = reservation.CheckInDate.ToString("dd/MM/yyyy");
        var checkout     = reservation.CheckOutDate.ToString("dd/MM/yyyy");
        var laundryRow   = reservation.LaundryService
            ? $"<tr><td>Servicio de lavandería</td><td>&mdash;</td><td>&mdash;</td><td>{reservation.LaundryServiceCost:C0}</td></tr>"
            : "";
        var laundryTot   = reservation.LaundryService
            ? $"<div class=\"tot-row\"><span>Lavandería</span><span>{reservation.LaundryServiceCost:C0}</span></div>"
            : "";

        return $$"""
<!DOCTYPE html>
<html lang="es">
<head>
<meta charset="utf-8"/>
<meta name="viewport" content="width=device-width,initial-scale=1"/>
<title>Comprobante #{{shortId}}</title>
<style>
  *{box-sizing:border-box;margin:0;padding:0}
  body{font-family:'Segoe UI',Arial,sans-serif;background:#dedad8;min-height:100vh;padding:40px 20px;color:#1a1a1a}
  .doc{max-width:700px;margin:0 auto;background:#eeecea;padding:56px 60px}
  /* Header */
  .hd{display:flex;justify-content:space-between;align-items:flex-start;margin-bottom:40px;gap:20px}
  .hd-brand{font-size:16px;font-weight:700}
  .hd-sub{font-size:12px;color:#999;margin-top:3px}
  .hd-client{margin-top:22px}
  .hd-client strong{display:block;font-size:14px;font-weight:700}
  .hd-client span{font-size:12px;color:#888}
  .hd-right{text-align:right;flex-shrink:0}
  .hd-title{font-size:42px;font-weight:800;letter-spacing:-.02em;line-height:1;color:#1a1a1a}
  .hd-ref{font-size:14px;color:#888;margin-top:6px;font-weight:500}
  /* Info row */
  .info-row{display:grid;grid-template-columns:1fr 1fr 1fr;gap:16px;padding:16px 0;border-top:1px solid #c8c6c4;border-bottom:1px solid #c8c6c4;margin-bottom:36px}
  .info-row .cell:last-child{text-align:right}
  .cell label{font-size:9px;color:#aaa;display:block;margin-bottom:4px;text-transform:uppercase;letter-spacing:.07em;font-weight:600}
  .cell span{font-size:12px;font-weight:700;color:#333}
  /* Description */
  .desc-title{font-size:26px;font-weight:700;margin-bottom:14px}
  /* Items */
  .items{width:100%;border-collapse:collapse;margin-bottom:4px}
  .items thead td{font-size:9px;color:#aaa;text-transform:uppercase;letter-spacing:.07em;padding-bottom:10px;font-weight:600;border-bottom:1px solid #c8c6c4}
  .items thead td:last-child{text-align:right}
  .items tbody td{padding:12px 0;font-size:13px;color:#333;border-bottom:1px solid #d8d6d4}
  .items tbody td:last-child{text-align:right;font-weight:700}
  /* Totals */
  .totals-wrap{display:flex;justify-content:flex-end;padding:4px 0 36px}
  .totals{width:250px}
  .tot-row{display:flex;justify-content:space-between;padding:7px 0;font-size:13px;color:#666;border-bottom:1px solid #d8d6d4}
  .tot-row.grand{border-bottom:none;padding-top:12px;font-size:16px;font-weight:700;color:#1a1a1a}
  /* Footer */
  .footer{display:grid;grid-template-columns:1fr 1fr;gap:36px;padding-top:22px;border-top:1px solid #c8c6c4}
  .footer h3{font-size:13px;font-weight:700;margin-bottom:13px;color:#1a1a1a}
  .fd-row{display:flex;gap:10px;margin-bottom:7px}
  .fd-row label{font-size:9px;color:#aaa;text-transform:uppercase;letter-spacing:.06em;font-weight:600;width:86px;flex-shrink:0;padding-top:2px}
  .fd-row span{font-size:12px;font-weight:600;color:#444;line-height:1.4}
  .fd-text{font-size:12px;color:#666;line-height:1.7}
</style>
</head>
<body>
<div class="doc">

  <div class="hd">
    <div>
      <div class="hd-brand">Sistema de Reservas</div>
      <div class="hd-sub">Sedes y Apartamentos</div>
      <div class="hd-client">
        <strong>{{userName}}</strong>
        <span>{{userEmail}}</span>
      </div>
    </div>
    <div class="hd-right">
      <div class="hd-title">COMPROBANTE</div>
      <div class="hd-ref">#{{shortId}}</div>
    </div>
  </div>

  <div class="info-row">
    <div class="cell"><label>Lugar</label><span>{{reservation.PlaceName}}</span></div>
    <div class="cell"><label>Período</label><span>{{checkin}} &mdash; {{checkout}}</span></div>
    <div class="cell"><label>Fecha de emisión</label><span>{{generated}}</span></div>
  </div>

  <div class="desc-title">Descripción</div>
  <table class="items">
    <thead>
      <tr>
        <td>Concepto</td>
        <td>Duración</td>
        <td>Personas</td>
        <td>Importe</td>
      </tr>
    </thead>
    <tbody>
      <tr>
        <td>Alojamiento &mdash; {{reservation.PlaceName}}</td>
        <td>{{nights}} {{nightLbl}}</td>
        <td>{{reservation.TotalPersons}}</td>
        <td>{{baseCost}}</td>
      </tr>
      {{laundryRow}}
    </tbody>
  </table>

  <div class="totals-wrap">
    <div class="totals">
      <div class="tot-row"><span>Subtotal</span><span>{{baseCost}}</span></div>
      {{laundryTot}}
      <div class="tot-row grand"><span>Total</span><span>{{reservation.TotalCost.ToString("C0")}}</span></div>
    </div>
  </div>

  <div class="footer">
    <div>
      <h3>Detalles de la reserva</h3>
      <div class="fd-row"><label>Ingreso</label><span>{{checkin}}</span></div>
      <div class="fd-row"><label>Salida</label><span>{{checkout}}</span></div>
      <div class="fd-row"><label>Duración</label><span>{{nights}} {{nightLbl}}</span></div>
      <div class="fd-row"><label>Personas</label><span>{{reservation.TotalPersons}}</span></div>
      <div class="fd-row"><label>Habitaciones</label><span>{{reservation.NumberOfRoomsNeeded}}</span></div>
    </div>
    <div>
      <h3>Estado del proceso</h3>
      <div class="fd-text">
        El comprobante de pago fue recibido correctamente. Nuestro equipo verificará el pago y enviará confirmación final en breve.
        <br><br>
        <strong>Estado:</strong> {{statusLbl}}<br>
        <strong>Referencia:</strong> #{{shortId}}<br>
        <strong>Emitido:</strong> {{generated}}
      </div>
    </div>
  </div>

</div>
</body>
</html>
""";
    }

    // ── Persistencia ────────────────────────────────────────────────────────
    public async Task SaveHtmlAsync(Guid reservationId, string html, CancellationToken cancellationToken = default)
    {
        var dir = Path.Combine(_env.WebRootPath, "tickets");
        Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(Path.Combine(dir, $"{reservationId:N}.html"), html, cancellationToken);
    }

    public async Task SavePdfAsync(Guid reservationId, byte[] pdf, CancellationToken cancellationToken = default)
    {
        var dir = Path.Combine(_env.WebRootPath, "tickets");
        Directory.CreateDirectory(dir);
        await File.WriteAllBytesAsync(Path.Combine(dir, $"{reservationId:N}.pdf"), pdf, cancellationToken);
    }

    public bool TicketExists(Guid reservationId) =>
        File.Exists(Path.Combine(_env.WebRootPath, "tickets", $"{reservationId:N}.pdf"));

    // ── Helpers PDF ─────────────────────────────────────────────────────────
    private static void PdfItemRow(TableDescriptor table, string concept, string duration, string persons, string amount, Color border)
    {
        table.Cell().PaddingVertical(11).BorderBottom(1).BorderColor(border).Text(concept).FontSize(12);
        table.Cell().PaddingVertical(11).BorderBottom(1).BorderColor(border).Text(duration).FontSize(12);
        table.Cell().PaddingVertical(11).BorderBottom(1).BorderColor(border).Text(persons).FontSize(12);
        table.Cell().PaddingVertical(11).BorderBottom(1).BorderColor(border).AlignRight().Text(amount).FontSize(12).Bold();
    }

    private static void TotRow(ColumnDescriptor col, string label, string value, Color border, Color textColor, bool isTotal)
    {
        col.Item().BorderBottom(isTotal ? 0 : 1).BorderColor(border)
            .PaddingVertical(isTotal ? 10 : 7)
            .Row(row =>
            {
                var lStyle = row.RelativeItem().Text(label)
                    .FontSize(isTotal ? 14 : 12).FontColor(textColor);
                if (isTotal) lStyle.Bold();
                var vStyle = row.AutoItem().Text(value)
                    .FontSize(isTotal ? 14 : 12).FontColor(textColor);
                if (isTotal) vStyle.Bold();
            });
    }

    private static void FootRow(ColumnDescriptor col, string label, string value, Color labelColor)
    {
        col.Item().PaddingBottom(7).Row(row =>
        {
            row.ConstantItem(82).Text(label.ToUpper())
                .FontSize(8).FontColor(labelColor).LetterSpacing(0.05f);
            row.RelativeItem().Text(value).FontSize(11).Bold();
        });
    }
}
