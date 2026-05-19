-- ============================================================
-- FODUN - Sistema de Reservas
-- Script 01: Creación de Base de Datos
-- Ejecutar como: sa o usuario con permisos CREATE DATABASE
-- ============================================================

USE [master];
GO

IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE [name] = 'FODUN_Reservations')
BEGIN
    CREATE DATABASE [FODUN_Reservations]
    ON PRIMARY (
        NAME = N'FODUN_Reservations_data',
        SIZE = 100MB,
        FILEGROWTH = 10MB
    )
    LOG ON (
        NAME = N'FODUN_Reservations_log',
        SIZE = 50MB,
        FILEGROWTH = 5MB
    );
    PRINT 'Base de datos FODUN_Reservations creada.';
END
ELSE
    PRINT 'La base de datos FODUN_Reservations ya existe.';
GO

USE [FODUN_Reservations];
GO
