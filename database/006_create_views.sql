-- ============================================================================
-- Script 006: Vistas optimizadas para dashboard y reportes
-- Proyecto: Asistencia y Despacho de Juguetes Dia del Nino
-- ============================================================================

USE Asistencia;
GO

-- ============================================================================
-- vw_ResumenInventario - Inventario por juguete con porcentaje de despacho
-- ============================================================================
CREATE OR ALTER VIEW dbo.vw_ResumenInventario
AS
SELECT
    c.Id AS JugueteId,
    c.Categoria,
    c.Genero,
    c.NombreJuguete,
    c.StockInicial,
    c.StockActual,
    c.StockInicial - c.StockActual AS Entregados,
    CAST(ROUND((CAST((c.StockInicial - c.StockActual) AS FLOAT) / NULLIF(c.StockInicial, 0)) * 100, 2) AS DECIMAL(5,2)) AS PorcentajeDespacho
FROM dbo.tblCatalogoJuguetes c
WHERE c.Activo = 1;
GO

PRINT 'Vista vw_ResumenInventario creada.';
GO

-- ============================================================================
-- vw_BitacoraEntregas - Auditoria consolidada de entregas
-- ============================================================================
CREATE OR ALTER VIEW dbo.vw_BitacoraEntregas
AS
SELECT
    e.Id AS EntregaId,
    e.EventoId,
    ev.Nombre AS EventoNombre,
    col.Carnet AS ColaboradorCarnet,
    col.Nombre AS ColaboradorNombre,
    col.Gerencia AS ColaboradorGerencia,
    h.NombreHijo AS HijoNombre,
    h.EdadHijo AS HijoEdad,
    h.GeneroHijo AS HijoGenero,
    h.Categoria,
    j.NombreJuguete,
    e.Estado,
    e.RecibidoPor,
    COALESCE(e.NombreReceptor, col.Nombre) AS ReceptorFinal,
    e.FotoEvidenciaUrl,
    e.FechaEntrega,
    e.UsuarioDespacho,
    e.FechaReversion,
    e.UsuarioReversion,
    e.MotivoReversion
FROM dbo.tblEntregasJuguetes e
INNER JOIN dbo.tblEventos ev ON e.EventoId = ev.Id
INNER JOIN dbo.tblHijos h ON e.HijoId = h.Id
INNER JOIN dbo.tblColaboradores col ON e.CarnetColaborador = col.Carnet
INNER JOIN dbo.tblCatalogoJuguetes j ON e.JugueteId = j.Id;
GO

PRINT 'Vista vw_BitacoraEntregas creada.';
GO

PRINT 'Todas las vistas creadas exitosamente.';
GO
