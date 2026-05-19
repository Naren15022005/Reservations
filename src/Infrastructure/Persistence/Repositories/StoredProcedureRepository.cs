using System.Data;
using System.Data.Common;
using FODUN.Reservations.Application.Dtos;
using FODUN.Reservations.Application.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FODUN.Reservations.Infrastructure.Persistence.Repositories;

/// <summary>
/// Ejecuta los cuatro stored procedures requeridos usando ADO.NET sobre la
/// conexión gestionada por EF Core, sin interferir con el Unit-of-Work.
/// </summary>
public sealed class StoredProcedureRepository : IStoredProcedureRepository
{
    private readonly ReservationsDbContext _context;

    public StoredProcedureRepository(ReservationsDbContext context) => _context = context;

    // -----------------------------------------------------------------------
    // SP 1: habitaciones disponibles por rango de fechas
    // -----------------------------------------------------------------------
    public async Task<IReadOnlyList<SpAvailableRoomResult>> GetAvailableRoomsByDateRangeAsync(
        Guid accommodationId, DateOnly checkIn, DateOnly checkOut, int minCapacity,
        CancellationToken cancellationToken = default)
    {
        var parameters = new SqlParameter[]
        {
            new("@AccommodationId", accommodationId),
            new("@CheckInDate",  checkIn.ToDateTime(TimeOnly.MinValue)),
            new("@CheckOutDate", checkOut.ToDateTime(TimeOnly.MinValue)),
            new("@MinCapacity",  minCapacity)
        };

        return await ExecuteAvailabilitySpAsync(
            "dbo.sp_GetAvailableRooms_ByDateRange", parameters, hasPersonColumns: false, cancellationToken);
    }

    // -----------------------------------------------------------------------
    // SP 2: habitaciones disponibles por rango de fechas + número de personas
    // -----------------------------------------------------------------------
    public async Task<IReadOnlyList<SpAvailableRoomResult>> GetAvailableRoomsByDateRangeAndPersonsAsync(
        Guid accommodationId, DateOnly checkIn, DateOnly checkOut, int totalPersons,
        CancellationToken cancellationToken = default)
    {
        var parameters = new SqlParameter[]
        {
            new("@AccommodationId", accommodationId),
            new("@CheckInDate",   checkIn.ToDateTime(TimeOnly.MinValue)),
            new("@CheckOutDate",  checkOut.ToDateTime(TimeOnly.MinValue)),
            new("@TotalPersons",  totalPersons)
        };

        return await ExecuteAvailabilitySpAsync(
            "dbo.sp_GetAvailableRooms_ByDateRangeAndPersons", parameters, hasPersonColumns: true, cancellationToken);
    }

    // -----------------------------------------------------------------------
    // SP 3: tarifas aplicables
    // -----------------------------------------------------------------------
    public async Task<IReadOnlyList<SpTariffResult>> GetApplicableTariffsAsync(
        Guid seatId, DateOnly checkIn, int totalPersons,
        CancellationToken cancellationToken = default)
    {
        var conn = _context.Database.GetDbConnection();
        await EnsureOpenAsync(conn, cancellationToken);

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "dbo.sp_GetApplicableTariffs";
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.Add(new SqlParameter("@SeatId",       seatId));
        cmd.Parameters.Add(new SqlParameter("@CheckInDate",  checkIn.ToDateTime(TimeOnly.MinValue)));
        cmd.Parameters.Add(new SqlParameter("@TotalPersons", totalPersons));

        var results = new List<SpTariffResult>();
        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            results.Add(MapTariff(reader));

        return results;
    }

    // -----------------------------------------------------------------------
    // SP 4: calcular costo total
    // -----------------------------------------------------------------------
    public async Task<SpCostResult?> CalculateReservationCostAsync(
        Guid seatId, DateOnly checkIn, DateOnly checkOut, int totalPersons, bool includeLaundry,
        CancellationToken cancellationToken = default)
    {
        var conn = _context.Database.GetDbConnection();
        await EnsureOpenAsync(conn, cancellationToken);

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "dbo.sp_CalculateReservationCost";
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.Add(new SqlParameter("@SeatId",        seatId));
        cmd.Parameters.Add(new SqlParameter("@CheckInDate",   checkIn.ToDateTime(TimeOnly.MinValue)));
        cmd.Parameters.Add(new SqlParameter("@CheckOutDate",  checkOut.ToDateTime(TimeOnly.MinValue)));
        cmd.Parameters.Add(new SqlParameter("@TotalPersons",  totalPersons));
        cmd.Parameters.Add(new SqlParameter("@IncludeLaundry", includeLaundry ? 1 : 0));

        // Los parámetros OUTPUT son requeridos por la firma del SP
        var pTotalCost = new SqlParameter("@OutTotalCost", SqlDbType.Decimal)
            { Direction = ParameterDirection.Output, Precision = 18, Scale = 2 };
        var pNights = new SqlParameter("@OutNights", SqlDbType.Int)
            { Direction = ParameterDirection.Output };
        var pAdditional = new SqlParameter("@OutAdditionalPersons", SqlDbType.Int)
            { Direction = ParameterDirection.Output };
        cmd.Parameters.Add(pTotalCost);
        cmd.Parameters.Add(pNights);
        cmd.Parameters.Add(pAdditional);

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new SpCostResult
        {
            TotalCost             = reader.GetDecimal(reader.GetOrdinal("TotalCost")),
            Nights                = reader.GetInt32(reader.GetOrdinal("Nights")),
            AdditionalPersons     = reader.GetInt32(reader.GetOrdinal("AdditionalPersons")),
            PricePerNight         = reader.GetDecimal(reader.GetOrdinal("PricePerNight")),
            AdditionalPersonPrice = reader.GetDecimal(reader.GetOrdinal("AdditionalPersonPrice")),
            LaundryServiceCost    = reader.GetDecimal(reader.GetOrdinal("LaundryServiceCost"))
        };
    }

    // -----------------------------------------------------------------------
    // Privados
    // -----------------------------------------------------------------------

    private async Task<IReadOnlyList<SpAvailableRoomResult>> ExecuteAvailabilitySpAsync(
        string spName, SqlParameter[] parameters, bool hasPersonColumns,
        CancellationToken cancellationToken)
    {
        var conn = _context.Database.GetDbConnection();
        await EnsureOpenAsync(conn, cancellationToken);

        using var cmd = conn.CreateCommand();
        cmd.CommandText = spName;
        cmd.CommandType = CommandType.StoredProcedure;
        foreach (var p in parameters)
            cmd.Parameters.Add(p);

        var results = new List<SpAvailableRoomResult>();
        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            results.Add(MapAvailableRoom(reader, hasPersonColumns));

        return results;
    }

    private static SpAvailableRoomResult MapAvailableRoom(DbDataReader reader, bool hasPersonColumns)
    {
        return new SpAvailableRoomResult
        {
            SeatId      = reader.GetGuid(reader.GetOrdinal("SeatId")),
            SeatNumber  = reader.GetString(reader.GetOrdinal("SeatNumber")),
            Type        = reader.GetString(reader.GetOrdinal("Type")),
            Capacity    = reader.GetInt32(reader.GetOrdinal("Capacity")),
            Description = GetNullableString(reader, "Description"),
            Amenities   = GetNullableString(reader, "Amenities"),
            Nights      = reader.GetInt32(reader.GetOrdinal("Nights")),
            // CEILING(float/float) → SQL FLOAT → .NET Double: usar Convert para tolerancia de tipos
            RoomsNeededForAllPersons = hasPersonColumns
                ? Convert.ToInt32(reader.GetValue(reader.GetOrdinal("RoomsNeededForAllPersons")))
                : null,
            // CASE WHEN ... THEN 1 ELSE 0 → SQL INT: Convert.ToBoolean(int) funciona (0=false, resto=true)
            FitsInOneRoom = hasPersonColumns
                ? Convert.ToBoolean(reader.GetValue(reader.GetOrdinal("FitsInOneRoom")))
                : null
        };
    }

    private static SpTariffResult MapTariff(DbDataReader reader)
    {
        return new SpTariffResult
        {
            Id           = reader.GetGuid(reader.GetOrdinal("Id")),
            SeatId       = reader.GetGuid(reader.GetOrdinal("SeatId")),
            Season       = reader.GetString(reader.GetOrdinal("Season")),
            MinPersons   = reader.GetInt32(reader.GetOrdinal("MinPersons")),
            MaxPersons   = reader.GetInt32(reader.GetOrdinal("MaxPersons")),
            PricePerNight         = reader.GetDecimal(reader.GetOrdinal("PricePerNight")),
            AdditionalPersonPrice = GetNullableDecimal(reader, "AdditionalPersonPrice"),
            DaysOfWeek   = GetNullableString(reader, "DaysOfWeek"),
            IsExceptional = reader.GetBoolean(reader.GetOrdinal("IsExceptional")),
            ValidFrom    = reader.GetDateTime(reader.GetOrdinal("ValidFrom")),
            ValidUntil   = GetNullableDateTime(reader, "ValidUntil"),
            // CASE WHEN → SQL INT: Convert tolera tanto Int32 como Boolean
            IsSpecialApplicableToday = Convert.ToBoolean(reader.GetValue(reader.GetOrdinal("IsSpecialApplicableToday")))
        };
    }

    private static string? GetNullableString(DbDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static decimal? GetNullableDecimal(DbDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
    }

    private static DateTime? GetNullableDateTime(DbDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }

    private static async Task EnsureOpenAsync(DbConnection connection, CancellationToken ct)
    {
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(ct);
    }
}
