USE [FODUN_Reservations];
GO

-- ============================================
-- Insert Accommodations (Sedes y Apartamentos)
-- ============================================

DECLARE @VilletaId UNIQUEIDENTIFIER = 'A1111111-1111-1111-1111-111111111111';
DECLARE @FusagasugaId UNIQUEIDENTIFIER = 'A2222222-2222-2222-2222-222222222222';
DECLARE @ChinchinaId UNIQUEIDENTIFIER = 'A3333333-3333-3333-3333-333333333333';
DECLARE @PalmiraId UNIQUEIDENTIFIER = 'A4444444-4444-4444-4444-444444444444';
DECLARE @SantaFeId UNIQUEIDENTIFIER = 'A5555555-5555-5555-5555-555555555555';
DECLARE @BogotaId UNIQUEIDENTIFIER = 'A6666666-6666-6666-6666-666666666666';
DECLARE @MedellinId UNIQUEIDENTIFIER = 'A7777777-7777-7777-7777-777777777777';
DECLARE @SantamartaId UNIQUEIDENTIFIER = 'A8888888-8888-8888-8888-888888888888';

-- Insertar Sedes si no existen
IF NOT EXISTS (SELECT 1 FROM [dbo].[Accommodations] WHERE Code = 'VILLETA')
    INSERT INTO [dbo].[Accommodations] VALUES (@VilletaId, 'VILLETA', 'Villeta', 'Sede recreativa Villeta', 'RecreationalSite', 'Cundinamarca', 'Villeta', 32, GETUTCDATE(), 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Accommodations] WHERE Code = 'FUSAGASUGA')
    INSERT INTO [dbo].[Accommodations] VALUES (@FusagasugaId, 'FUSAGASUGA', 'El Placer - Fusagasugá', 'Sede recreativa Fusagasugá', 'RecreationalSite', 'Cundinamarca', 'Fusagasugá', 34, GETUTCDATE(), 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Accommodations] WHERE Code = 'CHINCHINA')
    INSERT INTO [dbo].[Accommodations] VALUES (@ChinchinaId, 'CHINCHINA', 'Gonzalo Morante - Chinchiná', 'Sede recreativa Chinchiná', 'RecreationalSite', 'Caldas', 'Chinchiná', 30, GETUTCDATE(), 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Accommodations] WHERE Code = 'PALMIRA')
    INSERT INTO [dbo].[Accommodations] VALUES (@PalmiraId, 'PALMIRA', 'Tablones - Palmira', 'Sede recreativa Palmira', 'RecreationalSite', 'Valle', 'Palmira', 24, GETUTCDATE(), 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Accommodations] WHERE Code = 'SANTAFE')
    INSERT INTO [dbo].[Accommodations] VALUES (@SantaFeId, 'SANTAFE', 'Manguruma - Santa Fe Antioquia', 'Sede recreativa Santa Fe Antioquia', 'RecreationalSite', 'Antioquia', 'Santa Fe de Antioquia', 46, GETUTCDATE(), 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Accommodations] WHERE Code = 'BOGOTA')
    INSERT INTO [dbo].[Accommodations] VALUES (@BogotaId, 'BOGOTA', 'Federman - Bogotá', 'Sede recreativa Bogotá', 'RecreationalSite', 'Cundinamarca', 'Bogotá', 8, GETUTCDATE(), 1);

-- Inserttar Apartamentos
IF NOT EXISTS (SELECT 1 FROM [dbo].[Accommodations] WHERE Code = 'MEDELLIN')
    INSERT INTO [dbo].[Accommodations] VALUES (@MedellinId, 'MEDELLIN', 'Edificio Suramericana - Medellín', 'Apartamento Medellín', 'Apartment', 'Antioquia', 'Medellín', 6, GETUTCDATE(), 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Accommodations] WHERE Code = 'SANTAMARTA')
    INSERT INTO [dbo].[Accommodations] VALUES (@SantamartaId, 'SANTAMARTA', 'El Rodadero - Santa Marta', 'Apartamento Santa Marta', 'Apartment', 'Magdalena', 'Santa Marta', 14, GETUTCDATE(), 1);

-- ============================================
-- Insert Seats for Villeta (8 rooms, 4 personas cada una)
-- ============================================

DECLARE @Seat1 UNIQUEIDENTIFIER = 'S1111111-1111-1111-1111-111111111111';
DECLARE @Seat2 UNIQUEIDENTIFIER = 'S1111112-1111-1111-1111-111111111111';
DECLARE @Seat3 UNIQUEIDENTIFIER = 'S1111113-1111-1111-1111-111111111111';
DECLARE @Seat4 UNIQUEIDENTIFIER = 'S1111114-1111-1111-1111-111111111111';
DECLARE @Seat5 UNIQUEIDENTIFIER = 'S1111115-1111-1111-1111-111111111111';
DECLARE @Seat6 UNIQUEIDENTIFIER = 'S1111116-1111-1111-1111-111111111111';
DECLARE @Seat7 UNIQUEIDENTIFIER = 'S1111117-1111-1111-1111-111111111111';
DECLARE @Seat8 UNIQUEIDENTIFIER = 'S1111118-1111-1111-1111-111111111111';

IF NOT EXISTS (SELECT 1 FROM [dbo].[Seats] WHERE AccommodationId = @VilletaId AND SeatNumber = '1')
BEGIN
    INSERT INTO [dbo].[Seats] VALUES (@Seat1, @VilletaId, '1', 'Habitación Standard', 4, 'Cama doble y camarote', '["baño","tv","nevera"]', 1, GETUTCDATE());
    INSERT INTO [dbo].[Seats] VALUES (@Seat2, @VilletaId, '2', 'Habitación Standard', 4, 'Cama doble y camarote', '["baño","tv","nevera"]', 1, GETUTCDATE());
    INSERT INTO [dbo].[Seats] VALUES (@Seat3, @VilletaId, '3', 'Habitación Standard', 4, 'Cama doble y camarote', '["baño","tv","nevera"]', 1, GETUTCDATE());
    INSERT INTO [dbo].[Seats] VALUES (@Seat4, @VilletaId, '4', 'Habitación Standard', 4, 'Cama doble y camarote', '["baño","tv","nevera"]', 1, GETUTCDATE());
    INSERT INTO [dbo].[Seats] VALUES (@Seat5, @VilletaId, '5', 'Habitación Standard', 4, 'Cama doble y camarote', '["baño","tv","nevera"]', 1, GETUTCDATE());
    INSERT INTO [dbo].[Seats] VALUES (@Seat6, @VilletaId, '6', 'Habitación Standard', 4, 'Cama doble y camarote', '["baño","tv","nevera"]', 1, GETUTCDATE());
    INSERT INTO [dbo].[Seats] VALUES (@Seat7, @VilletaId, '7', 'Habitación Standard', 4, 'Cama doble y camarote', '["baño","tv","nevera"]', 1, GETUTCDATE());
    INSERT INTO [dbo].[Seats] VALUES (@Seat8, @VilletaId, '8', 'Habitación Standard', 4, 'Cama doble y camarote', '["baño","tv","nevera"]', 1, GETUTCDATE());
END

-- ============================================
-- Insert Tariffs for Villeta Seats
-- ============================================

IF NOT EXISTS (SELECT 1 FROM [dbo].[Tariffs] WHERE SeatId = @Seat1 AND Season = 0)
BEGIN
    -- Low season tariffs for all 8 rooms
    DECLARE @TariffLow DECIMAL = 70000;
    DECLARE @TariffHigh DECIMAL = 120000;
    DECLARE @AdditionalPerson DECIMAL = 16000;
    
    INSERT INTO [dbo].[Tariffs] VALUES (@Seat1, 0, 1, 4, @TariffLow, @AdditionalPerson, NULL, 0, '2026-01-01', NULL, GETUTCDATE());
    INSERT INTO [dbo].[Tariffs] VALUES (@Seat1, 1, 1, 4, @TariffHigh, @AdditionalPerson, NULL, 0, '2026-06-01', NULL, GETUTCDATE());
    INSERT INTO [dbo].[Tariffs] VALUES (@Seat1, 2, 1, 4, 27000, 11000, 'MonTueWedThu', 1, '2026-01-01', NULL, GETUTCDATE());
    
    INSERT INTO [dbo].[Tariffs] VALUES (@Seat2, 0, 1, 4, @TariffLow, @AdditionalPerson, NULL, 0, '2026-01-01', NULL, GETUTCDATE());
    INSERT INTO [dbo].[Tariffs] VALUES (@Seat2, 1, 1, 4, @TariffHigh, @AdditionalPerson, NULL, 0, '2026-06-01', NULL, GETUTCDATE());
    INSERT INTO [dbo].[Tariffs] VALUES (@Seat2, 2, 1, 4, 27000, 11000, 'MonTueWedThu', 1, '2026-01-01', NULL, GETUTCDATE());
    
    INSERT INTO [dbo].[Tariffs] VALUES (@Seat3, 0, 1, 4, @TariffLow, @AdditionalPerson, NULL, 0, '2026-01-01', NULL, GETUTCDATE());
    INSERT INTO [dbo].[Tariffs] VALUES (@Seat3, 1, 1, 4, @TariffHigh, @AdditionalPerson, NULL, 0, '2026-06-01', NULL, GETUTCDATE());
    INSERT INTO [dbo].[Tariffs] VALUES (@Seat3, 2, 1, 4, 27000, 11000, 'MonTueWedThu', 1, '2026-01-01', NULL, GETUTCDATE());
    
    INSERT INTO [dbo].[Tariffs] VALUES (@Seat4, 0, 1, 4, @TariffLow, @AdditionalPerson, NULL, 0, '2026-01-01', NULL, GETUTCDATE());
    INSERT INTO [dbo].[Tariffs] VALUES (@Seat4, 1, 1, 4, @TariffHigh, @AdditionalPerson, NULL, 0, '2026-06-01', NULL, GETUTCDATE());
    INSERT INTO [dbo].[Tariffs] VALUES (@Seat4, 2, 1, 4, 27000, 11000, 'MonTueWedThu', 1, '2026-01-01', NULL, GETUTCDATE());
    
    INSERT INTO [dbo].[Tariffs] VALUES (@Seat5, 0, 1, 4, @TariffLow, @AdditionalPerson, NULL, 0, '2026-01-01', NULL, GETUTCDATE());
    INSERT INTO [dbo].[Tariffs] VALUES (@Seat5, 1, 1, 4, @TariffHigh, @AdditionalPerson, NULL, 0, '2026-06-01', NULL, GETUTCDATE());
    INSERT INTO [dbo].[Tariffs] VALUES (@Seat5, 2, 1, 4, 27000, 11000, 'MonTueWedThu', 1, '2026-01-01', NULL, GETUTCDATE());
    
    INSERT INTO [dbo].[Tariffs] VALUES (@Seat6, 0, 1, 4, @TariffLow, @AdditionalPerson, NULL, 0, '2026-01-01', NULL, GETUTCDATE());
    INSERT INTO [dbo].[Tariffs] VALUES (@Seat6, 1, 1, 4, @TariffHigh, @AdditionalPerson, NULL, 0, '2026-06-01', NULL, GETUTCDATE());
    INSERT INTO [dbo].[Tariffs] VALUES (@Seat6, 2, 1, 4, 27000, 11000, 'MonTueWedThu', 1, '2026-01-01', NULL, GETUTCDATE());
    
    INSERT INTO [dbo].[Tariffs] VALUES (@Seat7, 0, 1, 4, @TariffLow, @AdditionalPerson, NULL, 0, '2026-01-01', NULL, GETUTCDATE());
    INSERT INTO [dbo].[Tariffs] VALUES (@Seat7, 1, 1, 4, @TariffHigh, @AdditionalPerson, NULL, 0, '2026-06-01', NULL, GETUTCDATE());
    INSERT INTO [dbo].[Tariffs] VALUES (@Seat7, 2, 1, 4, 27000, 11000, 'MonTueWedThu', 1, '2026-01-01', NULL, GETUTCDATE());
    
    INSERT INTO [dbo].[Tariffs] VALUES (@Seat8, 0, 1, 4, @TariffLow, @AdditionalPerson, NULL, 0, '2026-01-01', NULL, GETUTCDATE());
    INSERT INTO [dbo].[Tariffs] VALUES (@Seat8, 1, 1, 4, @TariffHigh, @AdditionalPerson, NULL, 0, '2026-06-01', NULL, GETUTCDATE());
    INSERT INTO [dbo].[Tariffs] VALUES (@Seat8, 2, 1, 4, 27000, 11000, 'MonTueWedThu', 1, '2026-01-01', NULL, GETUTCDATE());
END

PRINT 'Initial data inserted successfully!';
