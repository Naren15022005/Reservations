using FODUN.Reservations.Application.Commands.CancelReservation;
using FODUN.Reservations.Application.Commands.CreateReservation;
using FODUN.Reservations.Application.Common;
using FODUN.Reservations.Application.Dtos;
using FODUN.Reservations.Application.Services;
using FODUN.Reservations.Domain.Aggregates.Reservation;
using FODUN.Reservations.Domain.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace FODUN.Reservations.Api.Services;

public sealed class ReservationService : IReservationService
{
    private readonly IReservationRepository _reservationRepository;
    private readonly IAccommodationRepository _accommodationRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITariffService _tariffService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly INotificationService _notificationService;
    private readonly IValidator<CreateReservationCommand> _createValidator;
    private readonly IValidator<CancelReservationCommand> _cancelValidator;
    private readonly ILogger<ReservationService> _logger;

    public ReservationService(
        IReservationRepository reservationRepository,
        IAccommodationRepository accommodationRepository,
        IUserRepository userRepository,
        ITariffService tariffService,
        IUnitOfWork unitOfWork,
        IAuditLogRepository auditLogRepository,
        INotificationService notificationService,
        IValidator<CreateReservationCommand> createValidator,
        IValidator<CancelReservationCommand> cancelValidator,
        ILogger<ReservationService> logger)
    {
        _reservationRepository = reservationRepository;
        _accommodationRepository = accommodationRepository;
        _userRepository = userRepository;
        _tariffService = tariffService;
        _unitOfWork = unitOfWork;
        _auditLogRepository = auditLogRepository;
        _notificationService = notificationService;
        _createValidator = createValidator;
        _cancelValidator = cancelValidator;
        _logger = logger;
    }

    public async Task<Result<ReservationDto>> CreateAsync(
        CreateReservationCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<ReservationDto>.Failure(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));

        Domain.Aggregates.Accommodation.Seat? seat;

        if (command.SeatId.HasValue)
        {
            // Verificar puntualmente que ese asiento específico no tenga conflicto
            var hasConflict = await _reservationRepository.HasConflictingReservationAsync(
                command.SeatId.Value, command.CheckIn, command.CheckOut,
                excludeReservationId: null, cancellationToken);

            if (hasConflict)
                return Result<ReservationDto>.Failure(
                    "El alojamiento seleccionado ya no está disponible para las fechas indicadas.");

            seat = await _accommodationRepository.GetSeatByIdAsync(command.SeatId.Value, cancellationToken);
            if (seat is null)
                return Result<ReservationDto>.Failure("El alojamiento no fue encontrado.");
        }
        else
        {
            var availableSeats = await _accommodationRepository.GetAvailableSeatsAsync(
                command.AccommodationId, command.CheckIn, command.CheckOut,
                command.TotalPersons, cancellationToken);

            seat = availableSeats.FirstOrDefault();
            if (seat is null)
                return Result<ReservationDto>.Failure(
                    "No hay alojamientos disponibles para las fechas seleccionadas.");
        }

        var costResult = await _tariffService.CalculateCostAsync(
            seat.Id, command.CheckIn, command.CheckOut, command.TotalPersons,
            command.IncludeLaundry, cancellationToken);

        if (!costResult.IsSuccess)
            return Result<ReservationDto>.Failure(costResult.Error!);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var reservation = Reservation.Create(
                command.UserId, command.CheckIn, command.CheckOut,
                command.TotalPersons, 1, costResult.Value!.GrandTotal,
                command.IncludeLaundry, command.Notes);

            reservation.AddItem(ReservationItem.Create(
                reservation.Id, seat.Id, costResult.Value.BaseTotal / costResult.Value.Nights,
                costResult.Value.Nights));

            reservation.Confirm();

            await _reservationRepository.AddAsync(reservation, cancellationToken);
            await _auditLogRepository.AddAsync(
                Domain.Aggregates.AuditLog.Create("ReservationCreated", command.UserId, "Reservation", reservation.Id),
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            await _notificationService.CreateAsync(
                command.UserId,
                "Reserva confirmada",
                $"Tu reserva #{reservation.Id.ToString("N")[..8].ToUpper()} ha sido registrada exitosamente. Completa el pago para asegurar tu lugar.",
                "reservation_created",
                reservation.Id, cancellationToken);

            _logger.LogInformation("Reserva creada: {Id} por usuario {UserId}", reservation.Id, command.UserId);
            return Result<ReservationDto>.Success(await BuildReservationDtoAsync(reservation, cancellationToken));
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error al crear reserva para usuario {UserId}", command.UserId);
            return Result<ReservationDto>.Failure("Ocurrió un error al procesar la reserva.");
        }
    }

    public async Task<Result> CancelAsync(CancelReservationCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _cancelValidator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result.Failure(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));

        var reservation = await _reservationRepository.GetByIdAsync(command.ReservationId, cancellationToken);
        if (reservation is null)
            return Result.Failure("La reserva no fue encontrada.");

        if (reservation.UserId != command.RequestingUserId)
            return Result.Failure("No tiene permiso para cancelar esta reserva.");

        if (!reservation.CanBeCancelled())
            return Result.Failure("La reserva no puede ser cancelada en su estado actual.");

        reservation.Cancel(command.CancellationReason);
        await _auditLogRepository.AddAsync(
            Domain.Aggregates.AuditLog.Create("ReservationCancelled", command.RequestingUserId,
                "Reservation", reservation.Id), cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notificationService.CreateAsync(
            command.RequestingUserId,
            "Reserva cancelada",
            $"Tu reserva #{reservation.Id.ToString("N")[..8].ToUpper()} ha sido cancelada." +
            (string.IsNullOrWhiteSpace(command.CancellationReason) ? "" : $" Motivo: {command.CancellationReason}"),
            "reservation_cancelled",
            reservation.Id, cancellationToken);

        _logger.LogInformation("Reserva cancelada: {Id}", command.ReservationId);
        return Result.Success();
    }

    public async Task<Result<ReservationDto>> GetByIdAsync(
        Guid reservationId, Guid requestingUserId, CancellationToken cancellationToken = default)
    {
        var reservation = await _reservationRepository.GetByIdAsync(reservationId, cancellationToken);
        if (reservation is null)
            return Result<ReservationDto>.Failure("La reserva no fue encontrada.");

        if (reservation.UserId != requestingUserId)
            return Result<ReservationDto>.Failure("No tiene permiso para ver esta reserva.");

        return Result<ReservationDto>.Success(await BuildReservationDtoAsync(reservation, cancellationToken));
    }

    public async Task<Result> ConfirmAsync(
        Guid reservationId, Guid requestingUserId, CancellationToken cancellationToken = default)
    {
        var reservation = await _reservationRepository.GetByIdAsync(reservationId, cancellationToken);
        if (reservation is null)
            return Result.Failure("La reserva no fue encontrada.");

        if (reservation.UserId != requestingUserId)
            return Result.Failure("No tiene permiso para confirmar esta reserva.");

        try
        {
            reservation.Confirm();
            await _reservationRepository.UpdateAsync(reservation, cancellationToken);
            await _auditLogRepository.AddAsync(
                Domain.Aggregates.AuditLog.Create("ReservationConfirmed", requestingUserId,
                    "Reservation", reservation.Id), cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Reserva confirmada: {Id}", reservation.Id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al confirmar reserva {Id}", reservationId);
            return Result.Failure("Ocurrió un error al confirmar la reserva.");
        }
    }

    public async Task<Result<IEnumerable<ReservationDto>>> GetByUserAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var reservations = await _reservationRepository.GetByUserIdAsync(userId, cancellationToken);
        var list = reservations.ToList();

        // El usuario es el mismo para todas; lo cargamos una sola vez
        var user = list.Count > 0
            ? await _userRepository.GetByIdAsync(userId, cancellationToken)
            : null;

        var dtos = new List<ReservationDto>(list.Count);
        foreach (var r in list)
            dtos.Add(await BuildReservationDtoAsync(r, cancellationToken, user));

        return Result<IEnumerable<ReservationDto>>.Success(dtos);
    }

    public async Task<Result> SubmitPaymentReceiptAsync(
        Guid reservationId, Guid requestingUserId,
        string? paymentMethod = null, string? receiptFileName = null,
        CancellationToken cancellationToken = default)
    {
        var reservation = await _reservationRepository.GetByIdAsync(reservationId, cancellationToken);
        if (reservation is null)
            return Result.Failure("La reserva no fue encontrada.");

        if (reservation.UserId != requestingUserId)
            return Result.Failure("No tiene permiso para registrar el pago de esta reserva.");

        try
        {
            reservation.SubmitPaymentReceipt(paymentMethod, receiptFileName);
            await _reservationRepository.UpdateAsync(reservation, cancellationToken);
            await _auditLogRepository.AddAsync(
                Domain.Aggregates.AuditLog.Create("PaymentReceiptSubmitted", requestingUserId,
                    "Reservation", reservation.Id), cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var method = paymentMethod switch
            {
                "PSE"     => "PSE",
                "Tarjeta" => "tarjeta de crédito/débito",
                "Nomina"  => "descuento por nómina",
                _         => paymentMethod ?? "desconocido"
            };
            await _notificationService.CreateAsync(
                requestingUserId,
                "Comprobante de pago recibido",
                $"Recibimos tu comprobante de pago ({method}) para la reserva #{reservation.Id.ToString("N")[..8].ToUpper()}. Lo revisaremos y confirmaremos en breve.",
                "payment_submitted",
                reservation.Id, cancellationToken);

            _logger.LogInformation("Comprobante de pago registrado: {Id}", reservation.Id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar comprobante de pago {Id}", reservationId);
            return Result.Failure("Ocurrió un error al registrar el comprobante.");
        }
    }

    private async Task<ReservationDto> BuildReservationDtoAsync(
        Domain.Aggregates.Reservation.Reservation reservation,
        CancellationToken cancellationToken,
        Domain.Aggregates.User.User? preloadedUser = null)
    {
        var user = preloadedUser ?? await _userRepository.GetByIdAsync(reservation.UserId, cancellationToken);

        string placeName = "—";
        if (reservation.Items.Any())
        {
            var seat = await _accommodationRepository.GetSeatByIdAsync(
                reservation.Items.First().SeatId, cancellationToken);
            if (seat is not null)
            {
                var accommodation = await _accommodationRepository.GetByIdAsync(
                    seat.AccommodationId, cancellationToken);
                placeName = accommodation is not null
                    ? $"{accommodation.Name} — {seat.Type}"
                    : seat.Type;
            }
        }

        var itemDtos = reservation.Items.Select(i => new ReservationItemDto(
            i.SeatId, string.Empty, i.PricePerNight, i.Nights, i.Subtotal)).ToList();

        return new ReservationDto(
            reservation.Id, reservation.UserId, user?.FullName ?? "Usuario",
            reservation.CheckInDate, reservation.CheckOutDate,
            reservation.TotalPersons, reservation.NumberOfRoomsNeeded,
            reservation.Status.ToString(), reservation.TotalCost,
            reservation.LaundryService, reservation.LaundryServiceCost,
            reservation.Notes, reservation.CreatedAt,
            reservation.CancelledAt, reservation.CancelledReason, itemDtos,
            placeName, reservation.HasPaymentReceipt);
    }
}
