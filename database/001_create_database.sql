-- ============================================================================
-- Script 001: Crear base de datos Asistencia
-- Proyecto: Asistencia y Despacho de Juguetes Dia del Nino
-- ============================================================================

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'Asistencia')
BEGIN
    CREATE DATABASE Asistencia;
    PRINT 'Base de datos Asistencia creada.';
END
ELSE
BEGIN
    PRINT 'La base de datos Asistencia ya existe.';
END
GO

USE Asistencia;
GO

-- Configurar aislamiento para lecturas no bloqueantes
ALTER DATABASE Asistencia SET ALLOW_SNAPSHOT_ISOLATION ON;
ALTER DATABASE Asistencia SET READ_COMMITTED_SNAPSHOT ON;
GO

PRINT 'Base de datos Asistencia lista para usar.';
GO
