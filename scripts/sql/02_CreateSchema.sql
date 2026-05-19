-- ============================================================
-- FODUN - Sistema de Reservas
-- Script 02: Creación de Tablas e Índices
-- ============================================================

USE [FODUN_Reservations];
GO

-- ===== USUARIOS =====
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE [name] = 'Users')
BEGIN
    CREATE TABLE [dbo].[Users] (
        [Id]                        UNIQUEIDENTIFIER    NOT NULL    PRIMARY KEY DEFAULT NEWID(),
        [DocumentNumber]            NVARCHAR(20)        NOT NULL,
        [FullName]                  NVARCHAR(255)       NOT NULL,
        [Email]                     NVARCHAR(255)       NOT NULL,
        [PhoneNumber]               NVARCHAR(20)        NULL,
        [PasswordHash]              NVARCHAR(MAX)       NOT NULL,
        [PasswordSalt]              NVARCHAR(MAX)       NULL,
        [IsEmailConfirmed]          BIT                 NOT NULL    DEFAULT 0,
        [EmailConfirmationToken]    NVARCHAR(MAX)       NULL,
        [PasswordResetToken]        NVARCHAR(MAX)       NULL,
        [PasswordResetTokenExpiry]  DATETIME2           NULL,
        [LockoutEnabled]            BIT                 NOT NULL    DEFAULT 0,
        [LockoutEndTime]            DATETIME2           NULL,
        [FailedLoginAttempts]       INT                 NOT NULL    DEFAULT 0,
        [IsActive]                  BIT                 NOT NULL    DEFAULT 1,
        [CreatedAt]                 DATETIME2           NOT NULL    DEFAULT GETUTCDATE(),
        [UpdatedAt]                 DATETIME2           NOT NULL    DEFAULT GETUTCDATE(),
        [CreatedBy]                 NVARCHAR(255)       NULL,
        [UpdatedBy]                 NVARCHAR(255)       NULL,
        CONSTRAINT UQ_Users_Email           UNIQUE ([Email]),
        CONSTRAINT UQ_Users_DocumentNumber  UNIQUE ([DocumentNumber])
    );
    CREATE INDEX IX_Users_Email ON [dbo].[Users] ([Email]);
    PRINT 'Tabla Users creada.';
END
GO

-- ===== SEDES / APARTAMENTOS =====
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE [name] = 'Accommodations')
BEGIN
    CREATE TABLE [dbo].[Accommodations] (
        [Id]            UNIQUEIDENTIFIER    NOT NULL    PRIMARY KEY DEFAULT NEWID(),
        [Code]          NVARCHAR(50)        NOT NULL,
        [Name]          NVARCHAR(255)       NOT NULL,
        [Description]   NVARCHAR(MAX)       NULL,
        [Type]          NVARCHAR(50)        NOT NULL,   -- 'RecreationalSite', 'Apartment'
        [City]          NVARCHAR(100)       NOT NULL,
        [Address]       NVARCHAR(MAX)       NULL,
        [MaxCapacity]   INT                 NOT NULL,
        [IsActive]      BIT                 NOT NULL    DEFAULT 1,
        [CreatedAt]     DATETIME2           NOT NULL    DEFAULT GETUTCDATE(),
        [UpdatedAt]     DATETIME2           NOT NULL    DEFAULT GETUTCDATE(),
        CONSTRAINT UQ_Accommodations_Code UNIQUE ([Code])
    );
    PRINT 'Tabla Accommodations creada.';
END
GO

-- ===== ALOJAMIENTOS / HABITACIONES =====
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE [name] = 'Seats')
BEGIN
    CREATE TABLE [dbo].[Seats] (
        [Id]                UNIQUEIDENTIFIER    NOT NULL    PRIMARY KEY DEFAULT NEWID(),
        [AccommodationId]   UNIQUEIDENTIFIER    NOT NULL,
        [SeatNumber]        NVARCHAR(50)        NOT NULL,
        [Type]              NVARCHAR(100)       NOT NULL,
        [Capacity]          INT                 NOT NULL,
        [Description]       NVARCHAR(MAX)       NULL,
        [Amenities]         NVARCHAR(MAX)       NULL,   -- JSON
        [IsActive]          BIT                 NOT NULL    DEFAULT 1,
        [CreatedAt]         DATETIME2           NOT NULL    DEFAULT GETUTCDATE(),
        [UpdatedAt]         DATETIME2           NOT NULL    DEFAULT GETUTCDATE(),
        CONSTRAINT FK_Seats_Accommodations FOREIGN KEY ([AccommodationId])
            REFERENCES [dbo].[Accommodations]([Id]) ON DELETE CASCADE,
        CONSTRAINT UQ_Seats_AccommodationId_SeatNumber UNIQUE ([AccommodationId], [SeatNumber])
    );
    PRINT 'Tabla Seats creada.';
END
GO

-- ===== TARIFAS =====
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE [name] = 'Tariffs')
BEGIN
    CREATE TABLE [dbo].[Tariffs] (
        [Id]                    UNIQUEIDENTIFIER    NOT NULL    PRIMARY KEY DEFAULT NEWID(),
        [SeatId]                UNIQUEIDENTIFIER    NOT NULL,
        [Season]                NVARCHAR(50)        NOT NULL,   -- 'Low', 'High', 'Special'
        [MinPersons]            INT                 NOT NULL    DEFAULT 1,
        [MaxPersons]            INT                 NOT NULL,
        [PricePerNight]         DECIMAL(18,2)       NOT NULL,
        [AdditionalPersonPrice] DECIMAL(18,2)       NULL,       -- Precio por persona adicional
        [DaysOfWeek]            NVARCHAR(50)        NULL,
        [IsExceptional]         BIT                 NOT NULL    DEFAULT 0,
        [ValidFrom]             DATE                NOT NULL,
        [ValidUntil]            DATE                NULL,
        [CreatedAt]             DATETIME2           NOT NULL    DEFAULT GETUTCDATE(),
        [UpdatedAt]             DATETIME2           NOT NULL    DEFAULT GETUTCDATE(),
        CONSTRAINT FK_Tariffs_Seats FOREIGN KEY ([SeatId])
            REFERENCES [dbo].[Seats]([Id]) ON DELETE CASCADE
    );
    CREATE INDEX IX_Tariffs_SeatId_Season ON [dbo].[Tariffs] ([SeatId], [Season]);
    PRINT 'Tabla Tariffs creada.';
END
GO

-- ===== BLOQUEOS DE FECHAS =====
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE [name] = 'Blackouts')
BEGIN
    CREATE TABLE [dbo].[Blackouts] (
        [Id]        UNIQUEIDENTIFIER    NOT NULL    PRIMARY KEY DEFAULT NEWID(),
        [SeatId]    UNIQUEIDENTIFIER    NOT NULL,
        [StartDate] DATE                NOT NULL,
        [EndDate]   DATE                NOT NULL,
        [Reason]    NVARCHAR(255)       NULL,
        [CreatedAt] DATETIME2           NOT NULL    DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2           NOT NULL    DEFAULT GETUTCDATE(),
        CONSTRAINT FK_Blackouts_Seats FOREIGN KEY ([SeatId])
            REFERENCES [dbo].[Seats]([Id]) ON DELETE CASCADE
    );
    CREATE INDEX IX_Blackouts_SeatId_Dates ON [dbo].[Blackouts] ([SeatId], [StartDate], [EndDate]);
    PRINT 'Tabla Blackouts creada.';
END
GO

-- ===== RESERVAS =====
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE [name] = 'Reservations')
BEGIN
    CREATE TABLE [dbo].[Reservations] (
        [Id]                    UNIQUEIDENTIFIER    NOT NULL    PRIMARY KEY DEFAULT NEWID(),
        [UserId]                UNIQUEIDENTIFIER    NOT NULL,
        [CheckInDate]           DATE                NOT NULL,
        [CheckOutDate]          DATE                NOT NULL,
        [TotalPersons]          INT                 NOT NULL,
        [NumberOfRoomsNeeded]   INT                 NOT NULL,
        [Status]                NVARCHAR(50)        NOT NULL    DEFAULT 'Pending',
        [TotalCost]             DECIMAL(18,2)       NOT NULL,
        [LaundryService]        BIT                 NOT NULL    DEFAULT 0,
        [LaundryServiceCost]    DECIMAL(18,2)       NOT NULL    DEFAULT 0,
        [Notes]                 NVARCHAR(MAX)       NULL,
        [CreatedAt]             DATETIME2           NOT NULL    DEFAULT GETUTCDATE(),
        [UpdatedAt]             DATETIME2           NOT NULL    DEFAULT GETUTCDATE(),
        [CancelledAt]           DATETIME2           NULL,
        [CancelledReason]       NVARCHAR(MAX)       NULL,
        CONSTRAINT FK_Reservations_Users FOREIGN KEY ([UserId])
            REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE
    );
    CREATE INDEX IX_Reservations_UserId_Status ON [dbo].[Reservations] ([UserId], [Status]);
    PRINT 'Tabla Reservations creada.';
END
GO

-- ===== ITEMS DE RESERVA =====
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE [name] = 'ReservationItems')
BEGIN
    CREATE TABLE [dbo].[ReservationItems] (
        [Id]            UNIQUEIDENTIFIER    NOT NULL    PRIMARY KEY DEFAULT NEWID(),
        [ReservationId] UNIQUEIDENTIFIER    NOT NULL,
        [SeatId]        UNIQUEIDENTIFIER    NOT NULL,
        [TariffId]      UNIQUEIDENTIFIER    NULL,
        [PricePerNight] DECIMAL(18,2)       NOT NULL,
        [Nights]        INT                 NOT NULL,
        CONSTRAINT FK_ReservationItems_Reservations FOREIGN KEY ([ReservationId])
            REFERENCES [dbo].[Reservations]([Id]) ON DELETE CASCADE,
        CONSTRAINT FK_ReservationItems_Seats FOREIGN KEY ([SeatId])
            REFERENCES [dbo].[Seats]([Id]),
        CONSTRAINT FK_ReservationItems_Tariffs FOREIGN KEY ([TariffId])
            REFERENCES [dbo].[Tariffs]([Id])
    );
    CREATE INDEX IX_ReservationItems_SeatId ON [dbo].[ReservationItems] ([SeatId])
        INCLUDE ([PricePerNight], [Nights]);
    PRINT 'Tabla ReservationItems creada.';
END
GO

-- ===== AUDITORÍA =====
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE [name] = 'AuditLogs')
BEGIN
    CREATE TABLE [dbo].[AuditLogs] (
        [Id]            BIGINT          NOT NULL    PRIMARY KEY IDENTITY(1,1),
        [UserId]        UNIQUEIDENTIFIER NULL,
        [Action]        NVARCHAR(255)   NOT NULL,
        [EntityType]    NVARCHAR(100)   NULL,
        [EntityId]      UNIQUEIDENTIFIER NULL,
        [OldValues]     NVARCHAR(MAX)   NULL,
        [NewValues]     NVARCHAR(MAX)   NULL,
        [Timestamp]     DATETIME2       NOT NULL    DEFAULT GETUTCDATE(),
        [IpAddress]     NVARCHAR(45)    NULL
    );
    PRINT 'Tabla AuditLogs creada.';
END
GO

PRINT '=== Schema creado exitosamente ===';
GO
