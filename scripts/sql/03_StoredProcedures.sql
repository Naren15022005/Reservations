-- ============================================================
-- FODUN - Sistema de Reservas
-- Script 03: Stored Procedures (4 obligatorios)
-- ============================================================

USE [FODUN_Reservations];
GO

-- ============================================================
-- SP 1: Obtener habitaciones disponibles por rango de fechas
-- ============================================================
IF OBJECT_ID('dbo.sp_GetAvailableRooms_ByDateRange', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sp_GetAvailableRooms_ByDateRange];
GO

CREATE PROCEDURE [dbo].[sp_GetAvailableRooms_ByDateRange]
    @AccommodationId    UNIQUEIDENTIFIER,
    @CheckInDate        DATE,
    @CheckOutDate       DATE,
    @MinCapacity        INT = 1
AS
BEGIN
    SET NOCOUNT ON;

    -- Validaciones
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
      -- Sin bloqueos en el rango
      AND NOT EXISTS (
          SELECT 1 FROM [dbo].[Blackouts] b
          WHERE b.[SeatId] = s.[Id]
            AND b.[StartDate] < @CheckOutDate
            AND b.[EndDate]   > @CheckInDate
      )
      -- Sin reservas activas que se solapen
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
GO

PRINT 'SP sp_GetAvailableRooms_ByDateRange creado.';
GO

-- ============================================================
-- SP 2: Obtener habitaciones disponibles por fechas y personas
-- ============================================================
IF OBJECT_ID('dbo.sp_GetAvailableRooms_ByDateRangeAndPersons', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sp_GetAvailableRooms_ByDateRangeAndPersons];
GO

CREATE PROCEDURE [dbo].[sp_GetAvailableRooms_ByDateRangeAndPersons]
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
GO

PRINT 'SP sp_GetAvailableRooms_ByDateRangeAndPersons creado.';
GO

-- ============================================================
-- SP 3: Obtener tarifas aplicables para un alojamiento
-- ============================================================
IF OBJECT_ID('dbo.sp_GetApplicableTariffs', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sp_GetApplicableTariffs];
GO

CREATE PROCEDURE [dbo].[sp_GetApplicableTariffs]
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

    -- Día de semana: 2=Lunes, 3=Martes, 4=Miércoles, 5=Jueves (según DATEPART con SET DATEFIRST 7)
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
        -- Indica si la tarifa especial aplica para este día (lunes a jueves)
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
        IsSpecialApplicableToday DESC,  -- Priorizar tarifa especial
        t.[Season] DESC;                -- Luego High > Low
END;
GO

PRINT 'SP sp_GetApplicableTariffs creado.';
GO

-- ============================================================
-- SP 4: Calcular costo total de una reserva
-- ============================================================
IF OBJECT_ID('dbo.sp_CalculateReservationCost', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sp_CalculateReservationCost];
GO

CREATE PROCEDURE [dbo].[sp_CalculateReservationCost]
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
        @AdditionalPersons  INT         = 0,
        @BasePrice          DECIMAL(18,2) = 0,
        @AddPersonPrice     DECIMAL(18,2) = 16000,  -- Valor por defecto configurado en negocio
        @LaundryPrice       DECIMAL(18,2) = 18000,
        @TotalCost          DECIMAL(18,2) = 0;

    -- Validación de rango
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

    -- Calcular noches
    SET @Nights = DATEDIFF(DAY, @CheckInDate, @CheckOutDate);

    -- Obtener capacidad del alojamiento
    SELECT @Capacity = [Capacity] FROM [dbo].[Seats] WHERE [Id] = @SeatId AND [IsActive] = 1;

    IF @Capacity IS NULL
    BEGIN
        RAISERROR('El alojamiento no fue encontrado o está inactivo.', 16, 1);
        RETURN;
    END;

    -- Calcular personas adicionales sobre la capacidad base
    IF @TotalPersons > @Capacity
        SET @AdditionalPersons = @TotalPersons - @Capacity;

    -- Obtener la tarifa más prioritaria:
    -- 1. Tarifa especial (lunes-jueves) si aplica
    -- 2. Temporada alta
    -- 3. Temporada baja
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

    -- Calcular total
    SET @TotalCost = (@BasePrice * @Nights) + (@AddPersonPrice * @AdditionalPersons * @Nights);

    -- Agregar lavandería si aplica ($18.000 fijo)
    IF @IncludeLaundry = 1
        SET @TotalCost = @TotalCost + @LaundryPrice;

    -- Retornar por OUTPUT
    SET @OutTotalCost           = @TotalCost;
    SET @OutNights              = @Nights;
    SET @OutAdditionalPersons   = @AdditionalPersons;

    -- También retornar como resultado SELECT para facilitar uso desde EF Core
    SELECT
        @TotalCost          AS TotalCost,
        @Nights             AS Nights,
        @AdditionalPersons  AS AdditionalPersons,
        @BasePrice          AS PricePerNight,
        @AddPersonPrice     AS AdditionalPersonPrice,
        CASE WHEN @IncludeLaundry = 1 THEN @LaundryPrice ELSE 0 END AS LaundryServiceCost;
END;
GO

PRINT 'SP sp_CalculateReservationCost creado.';
PRINT '=== Todos los Stored Procedures creados exitosamente ===';
GO
