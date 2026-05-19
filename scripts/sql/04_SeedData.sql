-- ============================================================
-- FODUN - Sistema de Reservas
-- Script 04: Datos Iniciales (Seed)
-- ============================================================

USE [FODUN_Reservations];
GO

BEGIN TRANSACTION;

-- ===== SEDES RECREATIVAS (6) =====
IF NOT EXISTS (SELECT 1 FROM [dbo].[Accommodations] WHERE [Code] = 'VLL')
INSERT INTO [dbo].[Accommodations] ([Id],[Code],[Name],[Description],[Type],[City],[Address],[MaxCapacity])
VALUES
    (NEWID(),'VLL','Sede Villeta',        'Sede recreativa con piscinas y zonas verdes.',          'RecreationalSite','Villeta',                'Carretera Principal Km 2',  80),
    (NEWID(),'FUS','Sede Fusagasugá',     'Rodeada de montañas, ideal para descanso familiar.',    'RecreationalSite','Fusagasugá',             'Vía Panamericana Km 5',     60),
    (NEWID(),'CHN','Sede Chinchiná',      'En el corazón del Eje Cafetero.',                       'RecreationalSite','Chinchiná',              'Calle del Café #12-34',     50),
    (NEWID(),'PLM','Sede Palmira',        'Valle del Cauca, clima privilegiado.',                  'RecreationalSite','Palmira',                'Avenida de la Caña #45',    70),
    (NEWID(),'SFA','Sede Santa Fe Ant.', 'Pueblo colonial patrimonio histórico.',                  'RecreationalSite','Santa Fe de Antioquia',  'Plaza Mayor #1',            45),
    (NEWID(),'BOG','Sede Bogotá',         'Sede urbana en la capital.',                            'RecreationalSite','Bogotá',                 'Calle 100 #15-20',          100);

-- ===== APARTAMENTOS (2 ciudades, 4 unidades) =====
IF NOT EXISTS (SELECT 1 FROM [dbo].[Accommodations] WHERE [Code] = 'APT-MED')
INSERT INTO [dbo].[Accommodations] ([Id],[Code],[Name],[Description],[Type],[City],[Address],[MaxCapacity])
VALUES
    (NEWID(),'APT-MED','Apartamento Medellín',   'Apartamento ejecutivo en El Poblado.',         'Apartment','Medellín',    'Carrera 43A #18-5 El Poblado', 6),
    (NEWID(),'APT-STM','Apartamentos Santa Marta','3 apartamentos frente al mar en el Rodadero.','Apartment','Santa Marta', 'Cra 2 #11-20 El Rodadero',     18);

PRINT 'Sedes y Apartamentos insertados.';
GO

-- ===== HABITACIONES POR SEDE =====
-- Villeta
DECLARE @VillId UNIQUEIDENTIFIER = (SELECT [Id] FROM [dbo].[Accommodations] WHERE [Code] = 'VLL');
IF @VillId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[Seats] WHERE [AccommodationId] = @VillId)
BEGIN
    INSERT INTO [dbo].[Seats] ([Id],[AccommodationId],[SeatNumber],[Type],[Capacity],[Description])
    VALUES
        (NEWID(),@VillId,'Alojamiento 1','Standard',4,'Cabaña estándar con baño privado'),
        (NEWID(),@VillId,'Alojamiento 2','Standard',4,'Cabaña estándar con baño privado'),
        (NEWID(),@VillId,'Alojamiento 3','Standard',4,'Cabaña estándar con baño privado'),
        (NEWID(),@VillId,'Alojamiento 4','Premium', 6,'Cabaña premium con sala y cocina'),
        (NEWID(),@VillId,'Alojamiento 5','Premium', 6,'Cabaña premium con sala y cocina');
    PRINT 'Habitaciones Villeta insertadas.';
END;

-- Fusagasugá
DECLARE @FusId UNIQUEIDENTIFIER = (SELECT [Id] FROM [dbo].[Accommodations] WHERE [Code] = 'FUS');
IF @FusId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[Seats] WHERE [AccommodationId] = @FusId)
BEGIN
    INSERT INTO [dbo].[Seats] ([Id],[AccommodationId],[SeatNumber],[Type],[Capacity],[Description])
    VALUES
        (NEWID(),@FusId,'Habitación A','Standard',4,'Habitación doble con baño'),
        (NEWID(),@FusId,'Habitación B','Standard',4,'Habitación doble con baño'),
        (NEWID(),@FusId,'Habitación C','Premium', 6,'Suite familiar con balcón');
    PRINT 'Habitaciones Fusagasugá insertadas.';
END;

-- Apartamento Medellín
DECLARE @MedId UNIQUEIDENTIFIER = (SELECT [Id] FROM [dbo].[Accommodations] WHERE [Code] = 'APT-MED');
IF @MedId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[Seats] WHERE [AccommodationId] = @MedId)
BEGIN
    INSERT INTO [dbo].[Seats] ([Id],[AccommodationId],[SeatNumber],[Type],[Capacity],[Description])
    VALUES
        (NEWID(),@MedId,'Apartamento 101','Apartment',6,'Apartamento de 2 habitaciones, totalmente equipado');
    PRINT 'Habitación Medellín insertada.';
END;

-- Apartamentos Santa Marta (3 unidades)
DECLARE @StmId UNIQUEIDENTIFIER = (SELECT [Id] FROM [dbo].[Accommodations] WHERE [Code] = 'APT-STM');
IF @StmId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[Seats] WHERE [AccommodationId] = @StmId)
BEGIN
    INSERT INTO [dbo].[Seats] ([Id],[AccommodationId],[SeatNumber],[Type],[Capacity],[Description])
    VALUES
        (NEWID(),@StmId,'Apartamento 1','Apartment',6,'Apto frente al mar, 2 habitaciones'),
        (NEWID(),@StmId,'Apartamento 2','Apartment',6,'Apto frente al mar, 2 habitaciones'),
        (NEWID(),@StmId,'Apartamento 3','Apartment',6,'Apto frente al mar, 2 habitaciones');
    PRINT 'Habitaciones Santa Marta insertadas.';
END;
GO

-- ===== TARIFAS =====
-- Villeta - Alojamiento 1 (ejemplo completo con 3 temporadas)
DECLARE @SeatVll1 UNIQUEIDENTIFIER = (
    SELECT s.[Id] FROM [dbo].[Seats] s
    INNER JOIN [dbo].[Accommodations] a ON s.[AccommodationId] = a.[Id]
    WHERE a.[Code] = 'VLL' AND s.[SeatNumber] = 'Alojamiento 1'
);

IF @SeatVll1 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[Tariffs] WHERE [SeatId] = @SeatVll1)
BEGIN
    INSERT INTO [dbo].[Tariffs] ([Id],[SeatId],[Season],[MinPersons],[MaxPersons],[PricePerNight],[AdditionalPersonPrice],[IsExceptional],[ValidFrom])
    VALUES
        -- Temporada baja
        (NEWID(), @SeatVll1, 'Low',     1, 4,  70000, 16000, 0, '2026-01-01'),
        -- Temporada alta (+77%)
        (NEWID(), @SeatVll1, 'High',    1, 4, 124000, 16000, 0, '2026-01-01'),
        -- Tarifa especial lunes-jueves (-61%)
        (NEWID(), @SeatVll1, 'Special', 1, 4,  27000, 16000, 1, '2026-01-01');
    PRINT 'Tarifas Villeta Alojamiento 1 insertadas.';
END;

-- Apartamento Medellín - 3 temporadas
DECLARE @SeatMed UNIQUEIDENTIFIER = (
    SELECT s.[Id] FROM [dbo].[Seats] s
    INNER JOIN [dbo].[Accommodations] a ON s.[AccommodationId] = a.[Id]
    WHERE a.[Code] = 'APT-MED' AND s.[SeatNumber] = 'Apartamento 101'
);

IF @SeatMed IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[Tariffs] WHERE [SeatId] = @SeatMed)
BEGIN
    INSERT INTO [dbo].[Tariffs] ([Id],[SeatId],[Season],[MinPersons],[MaxPersons],[PricePerNight],[AdditionalPersonPrice],[IsExceptional],[ValidFrom])
    VALUES
        (NEWID(), @SeatMed, 'Low',     1, 6,  70000, 16000, 0, '2026-01-01'),
        (NEWID(), @SeatMed, 'High',    1, 6, 124000, 16000, 0, '2026-01-01'),
        (NEWID(), @SeatMed, 'Special', 1, 6,  27000, 16000, 1, '2026-01-01');
    PRINT 'Tarifas Apartamento Medellín insertadas.';
END;
GO

COMMIT TRANSACTION;
PRINT '=== Seed data insertado exitosamente ===';
GO
