-- ============================================================================
-- Script 003: Crear indices para rendimiento e integridad
-- Proyecto: Asistencia y Despacho de Juguetes Dia del Nino
-- ============================================================================

USE Asistencia;
GO

-- ============================================================================
-- Indices para busqueda rapida de hijos por carnet
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Hijos_CarnetColaborador')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Hijos_CarnetColaborador
    ON dbo.tblHijos(CarnetColaborador ASC)
    INCLUDE (NombreHijo, EdadHijo, GeneroHijo, Categoria);
    PRINT 'Indice IX_Hijos_CarnetColaborador creado.';
END
GO

-- ============================================================================
-- Indice para validar asistencia por evento + carnet
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Asistencia_Evento_Carnet')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Asistencia_Evento_Carnet
    ON dbo.tblAsistenciaEventos(EventoId ASC, CarnetColaborador ASC)
    INCLUDE (FechaRegistro);
    PRINT 'Indice IX_Asistencia_Evento_Carnet creado.';
END
GO

-- ============================================================================
-- Indice para consulta de entregas por evento + hijo + estado
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Entregas_Evento_Hijo_Estado')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Entregas_Evento_Hijo_Estado
    ON dbo.tblEntregasJuguetes(EventoId ASC, HijoId ASC, Estado ASC)
    INCLUDE (JugueteId, FechaEntrega, RecibidoPor, FotoEvidenciaUrl);
    PRINT 'Indice IX_Entregas_Evento_Hijo_Estado creado.';
END
GO

-- ============================================================================
-- Indice para busqueda en catalogo por categoria + genero
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Catalogo_Categoria_Genero_Activo')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Catalogo_Categoria_Genero_Activo
    ON dbo.tblCatalogoJuguetes(Categoria ASC, Genero ASC)
    INCLUDE (NombreJuguete, StockActual, FotoUrl)
    WHERE Activo = 1;
    PRINT 'Indice IX_Catalogo_Categoria_Genero_Activo creado.';
END
GO

-- ============================================================================
-- Indice unico filtrado: una entrega activa por hijo/evento
-- Evita doble entrega a nivel de base de datos
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Entregas_Activo_Por_Hijo')
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_Entregas_Activo_Por_Hijo
    ON dbo.tblEntregasJuguetes(EventoId ASC, HijoId ASC)
    WHERE Estado = 'DELIVERED';
    PRINT 'Indice unico UX_Entregas_Activo_Por_Hijo creado.';
END
GO

-- ============================================================================
-- Indice para busqueda de colaborador por nombre
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Colaboradores_Nombre')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Colaboradores_Nombre
    ON dbo.tblColaboradores(Nombre ASC)
    INCLUDE (Carnet, Gerencia, Ubicacion);
    PRINT 'Indice IX_Colaboradores_Nombre creado.';
END
GO

-- ============================================================================
-- Indice para busqueda de evento activo
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Eventos_Activo')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Eventos_Activo
    ON dbo.tblEventos(Activo ASC)
    INCLUDE (Id, Nombre, Anio)
    WHERE Activo = 1;
    PRINT 'Indice IX_Eventos_Activo creado.';
END
GO

PRINT 'Todos los indices creados exitosamente.';
GO
