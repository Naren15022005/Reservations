-- ============================================
-- sp_GetAvailableRooms_ByDateRange
-- ============================================
CREATE PROCEDURE [dbo].[sp_GetAvailableRooms_ByDateRange]
    @AccommodationId UNIQUEIDENTIFIER,
    @CheckInDate DATE,
    @CheckOutDate DATE,
    @MinCapacity INT
AS
BEGIN
    SET NOCOUNT ON;
    
    IF @CheckInDate >= @CheckOutDate
    BEGIN
        RAISERROR('CheckInDate must be before CheckOutDate', 16, 1);
        RETURN;
    END
    
    SELECT 
        s.Id,
        s.SeatNumber,
        s.Type,
        s.Capacity,
        s.Description,
        s.Amenities
    FROM [dbo].[Seats] s
    WHERE s.AccommodationId = @AccommodationId
        AND s.Capacity >= @MinCapacity
        AND s.IsActive = 1
        AND NOT EXISTS (
            SELECT 1 FROM [dbo].[Blackouts] b
            WHERE b.SeatId = s.Id
                AND b.StartDate < @CheckOutDate
                AND b.EndDate > @CheckInDate
        )
        AND NOT EXISTS (
            SELECT 1 FROM [dbo].[ReservationItems] ri
            INNER JOIN [dbo].[Reservations] r ON ri.ReservationId = r.Id
            WHERE ri.SeatId = s.Id
                AND r.Status IN (0, 1, 2)
                AND r.CheckInDate < @CheckOutDate
                AND r.CheckOutDate > @CheckInDate
        )
    ORDER BY s.SeatNumber;
END;

-- ============================================
-- sp_GetAvailableRooms_ByDateRangeAndPersons
-- ============================================
CREATE PROCEDURE [dbo].[sp_GetAvailableRooms_ByDateRangeAndPersons]
    @AccommodationId UNIQUEIDENTIFIER,
    @CheckInDate DATE,
    @CheckOutDate DATE,
    @TotalPersons INT
AS
BEGIN
    SET NOCOUNT ON;
    
    IF @CheckInDate >= @CheckOutDate
    BEGIN
        RAISERROR('CheckInDate must be before CheckOutDate', 16, 1);
        RETURN;
    END
    
    SELECT 
        s.Id,
        s.SeatNumber,
        s.Type,
        s.Capacity,
        s.Description,
        s.Amenities
    FROM [dbo].[Seats] s
    WHERE s.AccommodationId = @AccommodationId
        AND s.Capacity >= @TotalPersons
        AND s.IsActive = 1
        AND NOT EXISTS (
            SELECT 1 FROM [dbo].[Blackouts] b
            WHERE b.SeatId = s.Id
                AND b.StartDate < @CheckOutDate
                AND b.EndDate > @CheckInDate
        )
        AND NOT EXISTS (
            SELECT 1 FROM [dbo].[ReservationItems] ri
            INNER JOIN [dbo].[Reservations] r ON ri.ReservationId = r.Id
            WHERE ri.SeatId = s.Id
                AND r.Status IN (0, 1, 2)
                AND r.CheckInDate < @CheckOutDate
                AND r.CheckOutDate > @CheckInDate
        )
    ORDER BY s.SeatNumber;
END;

-- ============================================
-- sp_GetApplicableTariffs
-- ============================================
CREATE PROCEDURE [dbo].[sp_GetApplicableTariffs]
    @SeatId UNIQUEIDENTIFIER,
    @CheckInDate DATE,
    @CheckOutDate DATE,
    @TotalPersons INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Nights INT = DATEDIFF(DAY, @CheckInDate, @CheckOutDate);
    
    SELECT 
        t.Id,
        t.Season,
        t.MinPersons,
        t.MaxPersons,
        t.PricePerNight,
        t.AdditionalPersonPrice,
        t.IsSpecial,
        t.ValidFrom,
        t.ValidUntil
    FROM [dbo].[Tariffs] t
    WHERE t.SeatId = @SeatId
        AND @TotalPersons BETWEEN t.MinPersons AND t.MaxPersons
        AND @CheckInDate >= t.ValidFrom
        AND (t.ValidUntil IS NULL OR @CheckOutDate <= t.ValidUntil)
    ORDER BY t.Season, t.PricePerNight DESC;
END;

-- ============================================
-- sp_CalculateReservationCost
-- ============================================
CREATE PROCEDURE [dbo].[sp_CalculateReservationCost]
    @SeatId UNIQUEIDENTIFIER,
    @CheckInDate DATE,
    @CheckOutDate DATE,
    @TotalPersons INT,
    @IncludeLaundry BIT,
    @OutTotalCost DECIMAL(18,2) OUTPUT,
    @OutNights INT OUTPUT,
    @OutAdditionalPersons INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Nights INT = DATEDIFF(DAY, @CheckInDate, @CheckOutDate);
    DECLARE @BaseCost DECIMAL(18,2) = 0;
    DECLARE @AdditionalCost DECIMAL(18,2) = 0;
    DECLARE @MaxPersonsInTariff INT = 0;
    DECLARE @PricePerNight DECIMAL(18,2) = 0;
    DECLARE @AdditionalPersonPrice DECIMAL(18,2) = 0;
    
    -- Get applicable tariff
    SELECT TOP 1
        @MaxPersonsInTariff = t.MaxPersons,
        @PricePerNight = t.PricePerNight,
        @AdditionalPersonPrice = t.AdditionalPersonPrice
    FROM [dbo].[Tariffs] t
    WHERE t.SeatId = @SeatId
        AND @TotalPersons BETWEEN t.MinPersons AND t.MaxPersons
        AND @CheckInDate >= t.ValidFrom
        AND (t.ValidUntil IS NULL OR @CheckOutDate <= t.ValidUntil)
    ORDER BY t.Season DESC;
    
    IF @PricePerNight = 0
    BEGIN
        SET @OutTotalCost = 0;
        SET @OutNights = @Nights;
        SET @OutAdditionalPersons = 0;
        RETURN;
    END
    
    SET @BaseCost = @PricePerNight * @Nights;
    
    IF @TotalPersons > @MaxPersonsInTariff
    BEGIN
        SET @OutAdditionalPersons = @TotalPersons - @MaxPersonsInTariff;
        SET @AdditionalCost = @OutAdditionalPersons * @AdditionalPersonPrice * @Nights;
    END
    ELSE
    BEGIN
        SET @OutAdditionalPersons = 0;
        SET @AdditionalCost = 0;
    END
    
    SET @OutTotalCost = @BaseCost + @AdditionalCost;
    SET @OutNights = @Nights;
    
    IF @IncludeLaundry = 1
    BEGIN
        SET @OutTotalCost = @OutTotalCost + 18000; -- Fixed laundry cost
    END
END;
