-- ============================================================================
-- Script 004: Stored procedures transaccionales
-- Proyecto: Asistencia y Despacho de Juguetes Dia del Nino
-- Uso: Transacciones ACID con UPDLOCK, ROWLOCK y OUTPUT INSERTED
-- ============================================================================

USE Asistencia;
GO

-- ============================================================================
-- sp_Asistencia_LookupCarnet - Busqueda completa de colaborador + hijos
-- Devuelve: datos del colaborador, asistencia, hijos con estado de entrega
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_Asistencia_LookupCarnet
    @Carnet VARCHAR(50),
    @EventoId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Datos del colaborador
    SELECT
        c.Carnet,
        c.Nombre,
        c.Puesto,
        c.Gerencia,
        c.Ubicacion,
        c.Edificio
    FROM dbo.tblColaboradores c
    WHERE c.Carnet = @Carnet AND c.Activo = 1;

    -- Asistencia al evento
    SELECT
        a.Id,
        a.FechaRegistro,
        a.RegistradoPor,
        a.Adultos,
        a.Ninos
    FROM dbo.tblAsistenciaEventos a
    WHERE a.CarnetColaborador = @Carnet AND a.EventoId = @EventoId;

    -- Hijos con estado de entrega
    SELECT
        h.Id,
        h.NombreHijo,
        h.EdadHijo,
        h.GeneroHijo,
        h.Categoria,
        e.Id AS EntregaId,
        e.Estado AS EstadoEntrega,
        e.FechaEntrega,
        e.RecibidoPor,
        e.NombreReceptor,
        e.FotoEvidenciaUrl,
        j.Id AS JugueteSugeridoId,
        j.NombreJuguete AS JugueteSugeridoNombre,
        j.StockActual AS JugueteStock,
        j.FotoUrl AS JugueteFotoUrl
    FROM dbo.tblHijos h
    LEFT JOIN dbo.tblEntregasJuguetes e
        ON e.HijoId = h.Id AND e.EventoId = @EventoId AND e.Estado = 'DELIVERED'
    OUTER APPLY (
        SELECT TOP 1 Id, NombreJuguete, StockActual, FotoUrl
        FROM dbo.tblCatalogoJuguetes
        WHERE Categoria = h.Categoria
          AND (Genero = h.GeneroHijo OR Genero = 'TODOS')
          AND Activo = 1
        ORDER BY StockActual DESC, NombreJuguete
    ) j
    WHERE h.CarnetColaborador = @Carnet AND h.Activo = 1
    ORDER BY h.NombreHijo;
END
GO

PRINT 'Stored procedure sp_Asistencia_LookupCarnet creado.';
GO

-- ============================================================================
-- sp_Asistencia_Registrar - Registrar asistencia de colaborador
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_Asistencia_Registrar
    @EventoId INT,
    @CarnetColaborador VARCHAR(50),
    @RegistradoPor VARCHAR(100),
    @Adultos INT = 1,
    @Ninos INT = 0,
    @AsistioPor VARCHAR(20) = NULL,
    @NombreAsistente VARCHAR(250) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        -- Validar que el colaborador existe
        IF NOT EXISTS (SELECT 1 FROM dbo.tblColaboradores WHERE Carnet = @CarnetColaborador AND Activo = 1)
        BEGIN
            THROW 51000, 'El colaborador no existe o no esta activo.', 1;
        END;

        -- Validar que no tenga asistencia duplicada
        IF EXISTS (SELECT 1 FROM dbo.tblAsistenciaEventos WHERE EventoId = @EventoId AND CarnetColaborador = @CarnetColaborador)
        BEGIN
            THROW 51001, 'El colaborador ya tiene asistencia registrada para este evento.', 1;
        END;

        -- Insertar asistencia y devolver el registro creado
        INSERT INTO dbo.tblAsistenciaEventos (EventoId, CarnetColaborador, RegistradoPor, Adultos, Ninos, AsistioPor, NombreAsistente)
        OUTPUT INSERTED.Id, INSERTED.EventoId, INSERTED.CarnetColaborador, INSERTED.FechaRegistro, INSERTED.RegistradoPor, INSERTED.Adultos, INSERTED.Ninos, INSERTED.AsistioPor, INSERTED.NombreAsistente
        VALUES (@EventoId, @CarnetColaborador, @RegistradoPor, @Adultos, @Ninos, @AsistioPor, @NombreAsistente);
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH;
END
GO

PRINT 'Stored procedure sp_Asistencia_Registrar creado.';
GO

-- ============================================================================
-- sp_Despacho_Entregar - Registrar entrega con bloqueo pesimista de stock
-- Transaccion: UPDLOCK + ROWLOCK + validacion stock + decremento
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_Despacho_Entregar
    @EventoId INT,
    @HijoId INT,
    @JugueteId INT,
    @CarnetColaborador VARCHAR(50),
    @RecibidoPor VARCHAR(20),
    @NombreReceptor VARCHAR(250),
    @FotoEvidenciaUrl VARCHAR(500),
    @UsuarioDespacho VARCHAR(100),
    @EntregaId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- 1. Bloquear fila del juguete con bloqueo pesimista
        DECLARE @StockActual INT;
        DECLARE @Activo BIT;

        SELECT @StockActual = StockActual, @Activo = Activo
        FROM dbo.tblCatalogoJuguetes WITH (UPDLOCK, ROWLOCK)
        WHERE Id = @JugueteId;

        -- 2. Validar juguete activo
        IF @Activo IS NULL OR @Activo = 0
        BEGIN
            THROW 51000, 'El juguete especificado no existe o no esta activo en el catalogo.', 1;
        END;

        -- 3. Validar stock disponible
        IF @StockActual <= 0
        BEGIN
            THROW 51001, 'No hay stock disponible para este juguete.', 1;
        END;

        -- 4. Validar que el hijo no tenga entrega activa
        IF EXISTS (SELECT 1 FROM dbo.tblEntregasJuguetes
            WHERE HijoId = @HijoId AND EventoId = @EventoId AND Estado = 'DELIVERED')
        BEGIN
            THROW 51002, 'Este hijo ya tiene una entrega registrada y activa para este evento.', 1;
        END;

        -- 5. Validar que el colaborador exista
        IF NOT EXISTS (SELECT 1 FROM dbo.tblColaboradores WHERE Carnet = @CarnetColaborador AND Activo = 1)
        BEGIN
            THROW 51003, 'El colaborador no existe o no esta activo.', 1;
        END;

        -- 6. Insertar la entrega
        INSERT INTO dbo.tblEntregasJuguetes (
            EventoId, HijoId, JugueteId, CarnetColaborador, Estado,
            RecibidoPor, NombreReceptor, FotoEvidenciaUrl, UsuarioDespacho
        )
        VALUES (
            @EventoId, @HijoId, @JugueteId, @CarnetColaborador, 'DELIVERED',
            @RecibidoPor,
            CASE WHEN @RecibidoPor = 'TERCERO' THEN @NombreReceptor ELSE NULL END,
            @FotoEvidenciaUrl, @UsuarioDespacho
        );

        SET @EntregaId = SCOPE_IDENTITY();

        -- 7. Decrementar stock
        UPDATE dbo.tblCatalogoJuguetes
        SET StockActual = StockActual - 1
        WHERE Id = @JugueteId;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH;
END
GO

PRINT 'Stored procedure sp_Despacho_Entregar creado.';
GO

-- ============================================================================
-- sp_Despacho_Reversar - Reversar entrega y restaurar stock
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_Despacho_Reversar
    @EntregaId INT,
    @UsuarioReversion VARCHAR(100),
    @MotivoReversion VARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @JugueteId INT;
        DECLARE @Estado VARCHAR(20);

        -- 1. Bloquear y leer la entrega
        SELECT @JugueteId = JugueteId, @Estado = Estado
        FROM dbo.tblEntregasJuguetes WITH (UPDLOCK, ROWLOCK)
        WHERE Id = @EntregaId;

        -- 2. Validar que la entrega exista
        IF @Estado IS NULL
        BEGIN
            THROW 52000, 'La entrega especificada no existe.', 1;
        END;

        -- 3. Validar que no este ya reversada
        IF @Estado = 'REVERTED'
        BEGIN
            THROW 52001, 'Esta entrega ya se encuentra reversada.', 1;
        END;

        -- 4. Marcar la entrega como reversada con auditoria
        UPDATE dbo.tblEntregasJuguetes
        SET Estado = 'REVERTED',
            FechaReversion = GETDATE(),
            UsuarioReversion = @UsuarioReversion,
            MotivoReversion = @MotivoReversion
        WHERE Id = @EntregaId;

        -- 5. Restaurar stock
        UPDATE dbo.tblCatalogoJuguetes
        SET StockActual = StockActual + 1
        WHERE Id = @JugueteId;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH;
END
GO

PRINT 'Stored procedure sp_Despacho_Reversar creado.';
GO

-- ============================================================================
-- sp_Dashboard_ResumenEvento - KPIs del dashboard
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_Dashboard_ResumenEvento
    @EventoId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Totales globales
    DECLARE @TotalNinos INT;
    DECLARE @TotalColaboradores INT;
    DECLARE @Asistidos INT;
    DECLARE @Entregados INT;
    DECLARE @Reversados INT;

    SELECT @TotalNinos = COUNT(*) FROM dbo.tblHijos WHERE Activo = 1;
    SELECT @TotalColaboradores = COUNT(*) FROM dbo.tblColaboradores WHERE Activo = 1;
    SELECT @Asistidos = COUNT(DISTINCT CarnetColaborador) FROM dbo.tblAsistenciaEventos WHERE EventoId = @EventoId;
    SELECT @Entregados = COUNT(*) FROM dbo.tblEntregasJuguetes WHERE EventoId = @EventoId AND Estado = 'DELIVERED';
    SELECT @Reversados = COUNT(*) FROM dbo.tblEntregasJuguetes WHERE EventoId = @EventoId AND Estado = 'REVERTED';

    SELECT
        @TotalNinos AS TotalNinos,
        @TotalColaboradores AS TotalColaboradores,
        @Asistidos AS Asistidos,
        @Entregados AS Entregados,
        @Reversados AS Reversados,
        @TotalNinos - @Entregados AS Pendientes,
        CASE WHEN @TotalNinos > 0
            THEN CAST(ROUND((CAST(@Entregados AS FLOAT) / @TotalNinos) * 100, 2) AS DECIMAL(5,2))
            ELSE 0
        END AS PorcentajeAvance;

    -- Stock critico (menos de 10 unidades)
    SELECT
        Id,
        Categoria,
        Genero,
        NombreJuguete,
        StockActual
    FROM dbo.tblCatalogoJuguetes
    WHERE Activo = 1 AND StockActual < 10
    ORDER BY StockActual ASC;

    -- Avance por categoria
    SELECT
        h.Categoria,
        COUNT(*) AS Total,
        SUM(CASE WHEN e.Id IS NOT NULL AND e.Estado = 'DELIVERED' THEN 1 ELSE 0 END) AS Entregados
    FROM dbo.tblHijos h
    LEFT JOIN dbo.tblEntregasJuguetes e
        ON e.HijoId = h.Id AND e.EventoId = @EventoId AND e.Estado = 'DELIVERED'
    WHERE h.Activo = 1
    GROUP BY h.Categoria
    ORDER BY h.Categoria;
END
GO

PRINT 'Stored procedure sp_Dashboard_ResumenEvento creado.';
GO

PRINT 'Todos los stored procedures creados exitosamente.';
GO
