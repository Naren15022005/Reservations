-- Create database
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'FODUN_Reservations')
BEGIN
    CREATE DATABASE [FODUN_Reservations];
END
GO

USE [FODUN_Reservations];
GO

-- ============================================
-- Create Tables
-- ============================================

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Users')
BEGIN
    CREATE TABLE [dbo].[Users] (
        [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        [DocumentNumber] NVARCHAR(20) NOT NULL UNIQUE,
        [FullName] NVARCHAR(255) NOT NULL,
        [Email] NVARCHAR(255) NOT NULL UNIQUE,
        [PhoneNumber] NVARCHAR(20),
        [PasswordHash] NVARCHAR(MAX) NOT NULL,
        [PasswordSalt] NVARCHAR(MAX),
        [IsEmailConfirmed] BIT DEFAULT 0,
        [EmailConfirmationToken] NVARCHAR(MAX),
        [PasswordResetToken] NVARCHAR(MAX),
        [PasswordResetTokenExpiry] DATETIME2,
        [LockoutEnabled] BIT DEFAULT 0,
        [LockoutEndTime] DATETIME2,
        [FailedLoginAttempts] INT DEFAULT 0,
        [IsActive] BIT DEFAULT 1,
        [CreatedAt] DATETIME2 DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2,
        [CreatedBy] NVARCHAR(255),
        [UpdatedBy] NVARCHAR(255)
    );
    
    CREATE INDEX [IX_Users_Email] ON [dbo].[Users]([Email]);
    CREATE INDEX [IX_Users_DocumentNumber] ON [dbo].[Users]([DocumentNumber]);
END
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Accommodations')
BEGIN
    CREATE TABLE [dbo].[Accommodations] (
        [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        [Code] NVARCHAR(50) NOT NULL UNIQUE,
        [Name] NVARCHAR(255) NOT NULL,
        [Description] NVARCHAR(MAX),
        [Type] NVARCHAR(50) NOT NULL,
        [City] NVARCHAR(100) NOT NULL,
        [Address] NVARCHAR(MAX),
        [MaxCapacity] INT NOT NULL,
        [CreatedAt] DATETIME2 DEFAULT GETUTCDATE(),
        [IsActive] BIT DEFAULT 1
    );
    
    CREATE INDEX [IX_Accommodations_Code] ON [dbo].[Accommodations]([Code]);
END
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Seats')
BEGIN
    CREATE TABLE [dbo].[Seats] (
        [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        [AccommodationId] UNIQUEIDENTIFIER NOT NULL,
        [SeatNumber] NVARCHAR(50) NOT NULL,
        [Type] NVARCHAR(100) NOT NULL,
        [Capacity] INT NOT NULL,
        [Description] NVARCHAR(MAX),
        [Amenities] NVARCHAR(MAX),
        [IsActive] BIT DEFAULT 1,
        [CreatedAt] DATETIME2 DEFAULT GETUTCDATE(),
        FOREIGN KEY ([AccommodationId]) REFERENCES [dbo].[Accommodations]([Id]) ON DELETE CASCADE,
        UNIQUE ([AccommodationId], [SeatNumber])
    );
    
    CREATE INDEX [IX_Seats_AccommodationId] ON [dbo].[Seats]([AccommodationId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Tariffs')
BEGIN
    CREATE TABLE [dbo].[Tariffs] (
        [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        [SeatId] UNIQUEIDENTIFIER NOT NULL,
        [Season] INT NOT NULL,
        [MinPersons] INT NOT NULL,
        [MaxPersons] INT NOT NULL,
        [PricePerNight] DECIMAL(18, 2) NOT NULL,
        [AdditionalPersonPrice] DECIMAL(18, 2) NOT NULL,
        [SpecialDays] NVARCHAR(MAX),
        [IsSpecial] BIT DEFAULT 0,
        [ValidFrom] DATETIME2 NOT NULL,
        [ValidUntil] DATETIME2,
        [CreatedAt] DATETIME2 DEFAULT GETUTCDATE(),
        FOREIGN KEY ([SeatId]) REFERENCES [dbo].[Seats]([Id]) ON DELETE CASCADE
    );
    
    CREATE INDEX [IX_Tariffs_SeatId_Season] ON [dbo].[Tariffs]([SeatId], [Season]);
END
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Blackouts')
BEGIN
    CREATE TABLE [dbo].[Blackouts] (
        [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        [SeatId] UNIQUEIDENTIFIER NOT NULL,
        [StartDate] DATE NOT NULL,
        [EndDate] DATE NOT NULL,
        [Reason] NVARCHAR(MAX),
        [CreatedAt] DATETIME2 DEFAULT GETUTCDATE(),
        FOREIGN KEY ([SeatId]) REFERENCES [dbo].[Seats]([Id]) ON DELETE CASCADE
    );
    
    CREATE INDEX [IX_Blackouts_SeatId_Dates] ON [dbo].[Blackouts]([SeatId], [StartDate], [EndDate]);
END
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Reservations')
BEGIN
    CREATE TABLE [dbo].[Reservations] (
        [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        [UserId] UNIQUEIDENTIFIER NOT NULL,
        [CheckInDate] DATE NOT NULL,
        [CheckOutDate] DATE NOT NULL,
        [TotalPersons] INT NOT NULL,
        [NumberOfRoomsNeeded] INT NOT NULL,
        [Status] INT NOT NULL DEFAULT 0,
        [TotalCost] DECIMAL(18, 2) NOT NULL,
        [LaundryService] BIT DEFAULT 0,
        [LaundryServiceCost] DECIMAL(18, 2) DEFAULT 0,
        [Notes] NVARCHAR(MAX),
        [CreatedAt] DATETIME2 DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2,
        [CancelledAt] DATETIME2,
        [CancelledReason] NVARCHAR(MAX),
        FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE
    );
    
    CREATE INDEX [IX_Reservations_UserId_Status] ON [dbo].[Reservations]([UserId], [Status]);
END
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ReservationItems')
BEGIN
    CREATE TABLE [dbo].[ReservationItems] (
        [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        [ReservationId] UNIQUEIDENTIFIER NOT NULL,
        [SeatId] UNIQUEIDENTIFIER NOT NULL,
        [TariffId] UNIQUEIDENTIFIER,
        [PricePerNight] DECIMAL(18, 2) NOT NULL,
        [Nights] INT NOT NULL,
        [Subtotal] DECIMAL(18, 2) NOT NULL,
        FOREIGN KEY ([ReservationId]) REFERENCES [dbo].[Reservations]([Id]) ON DELETE CASCADE,
        FOREIGN KEY ([SeatId]) REFERENCES [dbo].[Seats]([Id]),
        FOREIGN KEY ([TariffId]) REFERENCES [dbo].[Tariffs]([Id])
    );
    
    CREATE INDEX [IX_ReservationItems_SeatId] ON [dbo].[ReservationItems]([SeatId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AuditLogs')
BEGIN
    CREATE TABLE [dbo].[AuditLogs] (
        [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        [UserId] UNIQUEIDENTIFIER,
        [Action] NVARCHAR(100) NOT NULL,
        [EntityType] NVARCHAR(100) NOT NULL,
        [OldValues] NVARCHAR(MAX),
        [NewValues] NVARCHAR(MAX),
        [Timestamp] DATETIME2 DEFAULT GETUTCDATE(),
        [IpAddress] NVARCHAR(50),
        FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id])
    );
END
GO

-- ============================================
-- Create Stored Procedures (from stored-procedures.sql)
-- ============================================

-- Drop procedures if they exist
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_NAME = 'sp_GetAvailableRooms_ByDateRange')
    DROP PROCEDURE [dbo].[sp_GetAvailableRooms_ByDateRange];
GO

IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_NAME = 'sp_GetAvailableRooms_ByDateRangeAndPersons')
    DROP PROCEDURE [dbo].[sp_GetAvailableRooms_ByDateRangeAndPersons];
GO

IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_NAME = 'sp_GetApplicableTariffs')
    DROP PROCEDURE [dbo].[sp_GetApplicableTariffs];
GO

IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_NAME = 'sp_CalculateReservationCost')
    DROP PROCEDURE [dbo].[sp_CalculateReservationCost];
GO

-- Create SP 1
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
GO

-- Create SP 2
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
GO

-- Create SP 3
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
GO

-- Create SP 4
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
        SET @OutTotalCost = @OutTotalCost + 18000;
    END
END;
GO

PRINT 'Database schema and stored procedures created successfully!';
