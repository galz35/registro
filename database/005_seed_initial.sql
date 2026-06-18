-- ============================================================================
-- Script 005: Datos iniciales (seed)
-- Proyecto: Asistencia y Despacho de Juguetes Dia del Nino
-- ============================================================================

USE Asistencia;
GO

-- ============================================================================
-- Evento inicial: Dia del Nino 2026
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.tblEventos WHERE Nombre = 'Dia del Nino 2026')
BEGIN
    INSERT INTO dbo.tblEventos (Nombre, Anio, FechaInicio, FechaFin, Activo)
    VALUES ('Dia del Nino 2026', 2026, '2026-06-18', '2026-06-19', 1);
    PRINT 'Evento Dia del Nino 2026 creado y activado.';
END
GO

-- ============================================================================
-- Usuario administrador por defecto
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.tblUsuariosAsistencia WHERE Carnet = 'admin')
BEGIN
    INSERT INTO dbo.tblUsuariosAsistencia (Carnet, Correo, Nombre, Rol, Activo)
    VALUES ('admin', 'admin@claro.com.ni', 'Administrador', 'admin', 1);
    PRINT 'Usuario admin creado.';
END
GO

PRINT 'Datos iniciales cargados.';
GO
