using FODUN.Reservations.Application.Commands.CreateReservation;
using FODUN.Reservations.Application.Commands.CancelReservation;
using FODUN.Reservations.Application.Common;
using FODUN.Reservations.Application.Dtos;

namespace FODUN.Reservations.Application.Services;

public interface IReservationService
{
    Task<Result<ReservationDto>> CreateAsync(CreateReservationCommand command, CancellationToken cancellationToken = default);
    Task<Result> CancelAsync(CancelReservationCommand command, CancellationToken cancellationToken = default);
    Task<Result<ReservationDto>> GetByIdAsync(Guid reservationId, Guid requestingUserId, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<ReservationDto>>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result> ConfirmAsync(Guid reservationId, Guid requestingUserId, CancellationToken cancellationToken = default);
    Task<Result> SubmitPaymentReceiptAsync(Guid reservationId, Guid requestingUserId, string? paymentMethod = null, string? receiptFileName = null, CancellationToken cancellationToken = default);
}
