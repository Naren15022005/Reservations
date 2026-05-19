using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FODUN.Reservations.Infrastructure.Persistence;

public static class StoredProcedureInitializer
{
    public static async Task EnsureCreatedAsync(ReservationsDbContext context, ILogger logger)
    {
        var missing = await GetMissingProceduresAsync(context);
        if (missing.Count == 0) return;

        logger.LogInformation("Creando {Count} stored procedure(s) faltante(s): {Names}",
            missing.Count, string.Join(", ", missing));

        foreach (var name in missing)
        {
            var sql = name switch
            {
                "sp_GetAvailableRooms_ByDateRange"           => Sp1,
                "sp_GetAvailableRooms_ByDateRangeAndPersons" => Sp2,
                "sp_GetApplicableTariffs"                    => Sp3,
                "sp_CalculateReservationCost"                => Sp4,
                _ => null
            };
            if (sql is null) continue;
            await context.Database.ExecuteSqlRawAsync(sql);
            logger.LogInformation("SP {Name} creado.", name);
        }
    }

    private static async Task<List<string>> GetMissingProceduresAsync(ReservationsDbContext context)
    {
        var required = new[]
        {
            "sp_GetAvailableRooms_ByDateRange",
            "sp_GetAvailableRooms_ByDateRangeAndPersons",
            "sp_GetApplicableTariffs",
            "sp_CalculateReservationCost"
        };

        var conn = context.Database.GetDbConnection();
        var wasOpen = conn.State == System.Data.ConnectionState.Open;
        if (!wasOpen) await conn.OpenAsync();

        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "SELECT name FROM sys.procedures " +
                "WHERE name IN (N'sp_GetAvailableRooms_ByDateRange'," +
                               "N'sp_GetAvailableRooms_ByDateRangeAndPersons'," +
                               "N'sp_GetApplicableTariffs'," +
                               "N'sp_CalculateReservationCost')";
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                existing.Add(reader.GetString(0));
        }
        finally
        {
            if (!wasOpen) await conn.CloseAsync();
        }

        return required.Where(r => !existing.Contains(r)).ToList();
    }

    private const string Sp1 = """
        CREATE OR ALTER PROCEDURE [dbo].[sp_GetAvailableRooms_ByDateRange]
            @AccommodationId    UNIQUEIDENTIFIER,
            @CheckInDate        DATE,
            @CheckOutDate       DATE,
            @MinCapacity        INT = 1
        AS
        BEGIN
            SET NOCOUNT ON;
            IF @CheckInDate IS NULL OR @CheckOutDate IS NULL
            BEGIN
                RAISERROR('Las fechas de check-in y check-out son requeridas.', 16, 1);
                RETURN;
            END;
            IF @CheckInDate >= @CheckOutDate
            BEGIN
                RAISERROR('La fecha de check-in debe ser anterior al check-out.', 16, 1);
                RETURN;
            END;
            IF @MinCapacity <= 0 SET @MinCapacity = 1;
            SELECT
                s.[Id]              AS SeatId,
                s.[SeatNumber],
                s.[Type],
                s.[Capacity],
                s.[Description],
                s.[Amenities],
                DATEDIFF(DAY, @CheckInDate, @CheckOutDate) AS Nights
            FROM [dbo].[Seats] s
            WHERE s.[AccommodationId] = @AccommodationId
              AND s.[IsActive] = 1
              AND s.[Capacity] >= @MinCapacity
              AND NOT EXISTS (
                  SELECT 1 FROM [dbo].[Blackouts] b
                  WHERE b.[SeatId] = s.[Id]
                    AND b.[StartDate] < @CheckOutDate
                    AND b.[EndDate]   > @CheckInDate
              )
              AND NOT EXISTS (
                  SELECT 1
                  FROM [dbo].[ReservationItems] ri
                  INNER JOIN [dbo].[Reservations] r ON ri.[ReservationId] = r.[Id]
                  WHERE ri.[SeatId] = s.[Id]
                    AND r.[Status] IN ('Confirmed', 'CheckedIn')
                    AND r.[CheckInDate]  < @CheckOutDate
                    AND r.[CheckOutDate] > @CheckInDate
              )
            ORDER BY s.[Capacity], s.[SeatNumber];
        END;
        """;

    private const string Sp2 = """
        CREATE OR ALTER PROCEDURE [dbo].[sp_GetAvailableRooms_ByDateRangeAndPersons]
            @AccommodationId    UNIQUEIDENTIFIER,
            @CheckInDate        DATE,
            @CheckOutDate       DATE,
            @TotalPersons       INT
        AS
        BEGIN
            SET NOCOUNT ON;
            IF @CheckInDate >= @CheckOutDate
            BEGIN
                RAISERROR('CheckInDate debe ser anterior al CheckOutDate.', 16, 1);
                RETURN;
            END;
            IF @TotalPersons <= 0
            BEGIN
                RAISERROR('TotalPersons debe ser mayor a 0.', 16, 1);
                RETURN;
            END;
            SELECT
                s.[Id]              AS SeatId,
                s.[SeatNumber],
                s.[Type],
                s.[Capacity],
                s.[Description],
                s.[Amenities],
                CEILING(CAST(@TotalPersons AS FLOAT) / CAST(s.[Capacity] AS FLOAT)) AS RoomsNeededForAllPersons,
                CASE WHEN s.[Capacity] >= @TotalPersons THEN 1 ELSE 0 END           AS FitsInOneRoom,
                DATEDIFF(DAY, @CheckInDate, @CheckOutDate)                          AS Nights
            FROM [dbo].[Seats] s
            WHERE s.[AccommodationId] = @AccommodationId
              AND s.[IsActive] = 1
              AND NOT EXISTS (
                  SELECT 1 FROM [dbo].[Blackouts] b
                  WHERE b.[SeatId] = s.[Id]
                    AND b.[StartDate] < @CheckOutDate
                    AND b.[EndDate]   > @CheckInDate
              )
              AND NOT EXISTS (
                  SELECT 1
                  FROM [dbo].[ReservationItems] ri
                  INNER JOIN [dbo].[Reservations] r ON ri.[ReservationId] = r.[Id]
                  WHERE ri.[SeatId] = s.[Id]
                    AND r.[Status] IN ('Confirmed', 'CheckedIn')
                    AND r.[CheckInDate]  < @CheckOutDate
                    AND r.[CheckOutDate] > @CheckInDate
              )
            ORDER BY FitsInOneRoom DESC, s.[Capacity] ASC, s.[SeatNumber];
        END;
        """;

    private const string Sp3 = """
        CREATE OR ALTER PROCEDURE [dbo].[sp_GetApplicableTariffs]
            @SeatId         UNIQUEIDENTIFIER,
            @CheckInDate    DATE,
            @TotalPersons   INT
        AS
        BEGIN
            SET NOCOUNT ON;
            IF @TotalPersons <= 0
            BEGIN
                RAISERROR('TotalPersons debe ser mayor a 0.', 16, 1);
                RETURN;
            END;
            DECLARE @DayOfWeek INT = DATEPART(WEEKDAY, @CheckInDate);
            SELECT
                t.[Id],
                t.[SeatId],
                t.[Season],
                t.[MinPersons],
                t.[MaxPersons],
                t.[PricePerNight],
                t.[AdditionalPersonPrice],
                t.[DaysOfWeek],
                t.[IsExceptional],
                t.[ValidFrom],
                t.[ValidUntil],
                CASE
                    WHEN t.[IsExceptional] = 1 AND @DayOfWeek IN (2, 3, 4, 5)
                    THEN 1 ELSE 0
                END AS IsSpecialApplicableToday
            FROM [dbo].[Tariffs] t
            WHERE t.[SeatId] = @SeatId
              AND t.[MinPersons] <= @TotalPersons
              AND @TotalPersons  <= t.[MaxPersons]
              AND t.[ValidFrom]  <= @CheckInDate
              AND (t.[ValidUntil] IS NULL OR t.[ValidUntil] >= @CheckInDate)
            ORDER BY
                IsSpecialApplicableToday DESC,
                t.[Season] DESC;
        END;
        """;

    private const string Sp4 = """
        CREATE OR ALTER PROCEDURE [dbo].[sp_CalculateReservationCost]
            @SeatId                 UNIQUEIDENTIFIER,
            @CheckInDate            DATE,
            @CheckOutDate           DATE,
            @TotalPersons           INT,
            @IncludeLaundry         BIT = 0,
            @OutTotalCost           DECIMAL(18,2)   OUTPUT,
            @OutNights              INT             OUTPUT,
            @OutAdditionalPersons   INT             OUTPUT
        AS
        BEGIN
            SET NOCOUNT ON;
            DECLARE
                @Nights             INT,
                @Capacity           INT,
                @AdditionalPersons  INT          = 0,
                @BasePrice          DECIMAL(18,2) = 0,
                @AddPersonPrice     DECIMAL(18,2) = 16000,
                @LaundryPrice       DECIMAL(18,2) = 18000,
                @TotalCost          DECIMAL(18,2) = 0;

            IF @CheckInDate >= @CheckOutDate
            BEGIN
                RAISERROR('Rango de fechas inválido: CheckIn debe ser anterior a CheckOut.', 16, 1);
                RETURN;
            END;
            IF @TotalPersons <= 0
            BEGIN
                RAISERROR('TotalPersons debe ser mayor a 0.', 16, 1);
                RETURN;
            END;

            SET @Nights = DATEDIFF(DAY, @CheckInDate, @CheckOutDate);
            SELECT @Capacity = [Capacity] FROM [dbo].[Seats] WHERE [Id] = @SeatId AND [IsActive] = 1;
            IF @Capacity IS NULL
            BEGIN
                RAISERROR('El alojamiento no fue encontrado o está inactivo.', 16, 1);
                RETURN;
            END;

            IF @TotalPersons > @Capacity
                SET @AdditionalPersons = @TotalPersons - @Capacity;

            DECLARE @DayOfWeek INT = DATEPART(WEEKDAY, @CheckInDate);
            SELECT TOP 1
                @BasePrice      = t.[PricePerNight],
                @AddPersonPrice = ISNULL(t.[AdditionalPersonPrice], 16000)
            FROM [dbo].[Tariffs] t
            WHERE t.[SeatId]       = @SeatId
              AND t.[MinPersons]  <= @TotalPersons
              AND @TotalPersons   <= t.[MaxPersons]
              AND t.[ValidFrom]   <= @CheckInDate
              AND (t.[ValidUntil] IS NULL OR t.[ValidUntil] >= @CheckInDate)
            ORDER BY
                CASE WHEN t.[IsExceptional] = 1 AND @DayOfWeek IN (2,3,4,5) THEN 0 ELSE 1 END,
                CASE t.[Season]
                    WHEN 'High'    THEN 0
                    WHEN 'Special' THEN 1
                    WHEN 'Low'     THEN 2
                    ELSE 3
                END;

            IF @BasePrice = 0
            BEGIN
                RAISERROR('No hay tarifa disponible para el alojamiento, fechas y personas seleccionados.', 16, 1);
                RETURN;
            END;

            SET @TotalCost = (@BasePrice * @Nights) + (@AddPersonPrice * @AdditionalPersons * @Nights);
            IF @IncludeLaundry = 1
                SET @TotalCost = @TotalCost + @LaundryPrice;

            SET @OutTotalCost         = @TotalCost;
            SET @OutNights            = @Nights;
            SET @OutAdditionalPersons = @AdditionalPersons;

            SELECT
                @TotalCost          AS TotalCost,
                @Nights             AS Nights,
                @AdditionalPersons  AS AdditionalPersons,
                @BasePrice          AS PricePerNight,
                @AddPersonPrice     AS AdditionalPersonPrice,
                CASE WHEN @IncludeLaundry = 1 THEN @LaundryPrice ELSE 0 END AS LaundryServiceCost;
        END;
        """;
}
