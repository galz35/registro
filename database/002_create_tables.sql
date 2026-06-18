-- ============================================================================
-- Script 002: Crear tablas del sistema Asistencia
-- Proyecto: Asistencia y Despacho de Juguetes Dia del Nino
-- Reglas: NO usar ORM, OUTPUT INSERTED, SQL parametrizado
-- ============================================================================

USE Asistencia;
GO

-- ============================================================================
-- 1. tblEventos - Eventos corporativos (ej. Dia del Nino 2026)
-- ============================================================================
IF OBJECT_ID('dbo.tblEventos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblEventos (
        Id INT IDENTITY(1,1) NOT NULL,
        Nombre VARCHAR(250) NOT NULL,
        Anio INT NOT NULL,
        FechaInicio DATE NULL,
        FechaFin DATE NULL,
        Activo BIT NOT NULL CONSTRAINT DF_Eventos_Activo DEFAULT (0),
        CONSTRAINT PK_Eventos PRIMARY KEY CLUSTERED (Id ASC)
    );
    PRINT 'Tabla tblEventos creada.';
END
GO

-- ============================================================================
-- 2. tblColaboradores - Empleados (maestro desde HCM + Excel)
-- ============================================================================
IF OBJECT_ID('dbo.tblColaboradores', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblColaboradores (
        Carnet VARCHAR(50) NOT NULL,
        Nombre VARCHAR(250) NOT NULL,
        Puesto VARCHAR(250) NULL,
        Gerencia VARCHAR(250) NULL,
        Ubicacion VARCHAR(250) NULL,
        Edificio VARCHAR(250) NULL,
        FechaContratacion DATE NULL,
        Genero VARCHAR(10) NULL,
        DepartamentoGeografico VARCHAR(250) NULL,
        Activo BIT NOT NULL CONSTRAINT DF_Colaboradores_Activo DEFAULT (1),
        FechaRegistro DATETIME2(0) NOT NULL CONSTRAINT DF_Colaboradores_FechaReg DEFAULT (GETDATE()),
        CONSTRAINT PK_Colaboradores PRIMARY KEY CLUSTERED (Carnet ASC)
    );
    PRINT 'Tabla tblColaboradores creada.';
END
GO

-- ============================================================================
-- 3. tblHijos - Hijos de colaboradores (desde Excel, source of truth)
-- ============================================================================
IF OBJECT_ID('dbo.tblHijos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblHijos (
        Id INT IDENTITY(1,1) NOT NULL,
        CarnetColaborador VARCHAR(50) NOT NULL,
        NombreHijo VARCHAR(250) NOT NULL,
        FechaNacimiento DATE NOT NULL,
        EdadHijo DECIMAL(18,2) NOT NULL,
        GeneroHijo VARCHAR(10) NOT NULL,
        Categoria VARCHAR(100) NOT NULL,
        Activo BIT NOT NULL CONSTRAINT DF_Hijos_Activo DEFAULT (1),
        CONSTRAINT PK_Hijos PRIMARY KEY CLUSTERED (Id ASC),
        CONSTRAINT FK_Hijos_Colaboradores FOREIGN KEY (CarnetColaborador)
            REFERENCES dbo.tblColaboradores(Carnet),
        CONSTRAINT CK_Hijos_Genero CHECK (GeneroHijo IN ('M', 'F'))
    );
    PRINT 'Tabla tblHijos creada.';
END
GO

-- ============================================================================
-- 4. tblCatalogoJuguetes - Inventario de juguetes con stock
-- ============================================================================
IF OBJECT_ID('dbo.tblCatalogoJuguetes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblCatalogoJuguetes (
        Id INT IDENTITY(1,1) NOT NULL,
        Categoria VARCHAR(100) NOT NULL,
        Genero VARCHAR(10) NOT NULL,
        NombreJuguete VARCHAR(250) NOT NULL,
        Proveedor VARCHAR(100) NULL,
        CostoUnitario DECIMAL(18,2) NULL,
        StockInicial INT NOT NULL,
        StockActual INT NOT NULL,
        FotoUrl VARCHAR(500) NULL,
        Activo BIT NOT NULL CONSTRAINT DF_CatalogoJuguetes_Activo DEFAULT (1),
        CONSTRAINT PK_CatalogoJuguetes PRIMARY KEY CLUSTERED (Id ASC),
        CONSTRAINT CK_CatalogoJuguetes_Genero CHECK (Genero IN ('M', 'F', 'TODOS')),
        CONSTRAINT CK_CatalogoJuguetes_Stock CHECK (StockActual >= 0)
    );
    PRINT 'Tabla tblCatalogoJuguetes creada.';
END
GO

-- ============================================================================
-- 5. tblAsistenciaEventos - Registro de asistencia de colaboradores
-- ============================================================================
IF OBJECT_ID('dbo.tblAsistenciaEventos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAsistenciaEventos (
        Id INT IDENTITY(1,1) NOT NULL,
        EventoId INT NOT NULL,
        CarnetColaborador VARCHAR(50) NOT NULL,
        FechaRegistro DATETIME2(0) NOT NULL CONSTRAINT DF_Asistencia_Fecha DEFAULT (GETDATE()),
        RegistradoPor VARCHAR(100) NOT NULL,
        CONSTRAINT PK_Asistencia PRIMARY KEY CLUSTERED (Id ASC),
        CONSTRAINT FK_Asistencia_Eventos FOREIGN KEY (EventoId)
            REFERENCES dbo.tblEventos(Id),
        CONSTRAINT FK_Asistencia_Colaboradores FOREIGN KEY (CarnetColaborador)
            REFERENCES dbo.tblColaboradores(Carnet)
    );
    PRINT 'Tabla tblAsistenciaEventos creada.';
END
GO

-- ============================================================================
-- 6. tblEntregasJuguetes - Historial de despachos y reversiones
-- ============================================================================
IF OBJECT_ID('dbo.tblEntregasJuguetes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblEntregasJuguetes (
        Id INT IDENTITY(1,1) NOT NULL,
        EventoId INT NOT NULL,
        HijoId INT NOT NULL,
        JugueteId INT NOT NULL,
        CarnetColaborador VARCHAR(50) NOT NULL,
        Estado VARCHAR(20) NOT NULL,
        RecibidoPor VARCHAR(20) NOT NULL,
        NombreReceptor VARCHAR(250) NULL,
        FotoEvidenciaUrl VARCHAR(500) NULL,
        FechaEntrega DATETIME2(0) NOT NULL CONSTRAINT DF_Entregas_Fecha DEFAULT (GETDATE()),
        UsuarioDespacho VARCHAR(100) NOT NULL,
        FechaReversion DATETIME2(0) NULL,
        UsuarioReversion VARCHAR(100) NULL,
        MotivoReversion VARCHAR(500) NULL,
        CONSTRAINT PK_Entregas PRIMARY KEY CLUSTERED (Id ASC),
        CONSTRAINT FK_Entregas_Eventos FOREIGN KEY (EventoId)
            REFERENCES dbo.tblEventos(Id),
        CONSTRAINT FK_Entregas_Hijos FOREIGN KEY (HijoId)
            REFERENCES dbo.tblHijos(Id),
        CONSTRAINT FK_Entregas_Catalogo FOREIGN KEY (JugueteId)
            REFERENCES dbo.tblCatalogoJuguetes(Id),
        CONSTRAINT FK_Entregas_Colaboradores FOREIGN KEY (CarnetColaborador)
            REFERENCES dbo.tblColaboradores(Carnet),
        CONSTRAINT CK_Entregas_Estado CHECK (Estado IN ('DELIVERED', 'REVERTED')),
        CONSTRAINT CK_Entregas_Recibido CHECK (RecibidoPor IN ('COLABORADOR', 'CONYUGE', 'TERCERO'))
    );
    PRINT 'Tabla tblEntregasJuguetes creada.';
END
GO

-- ============================================================================
-- 7. tblUsuariosAsistencia - Usuarios del sistema (login y roles)
-- ============================================================================
IF OBJECT_ID('dbo.tblUsuariosAsistencia', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblUsuariosAsistencia (
        Id INT IDENTITY(1,1) NOT NULL,
        Carnet VARCHAR(50) NOT NULL,
        Correo VARCHAR(250) NULL,
        Nombre VARCHAR(250) NOT NULL,
        Rol VARCHAR(50) NOT NULL,
        Activo BIT NOT NULL CONSTRAINT DF_Usuarios_Activo DEFAULT (1),
        UltimoLogin DATETIME2(0) NULL,
        CONSTRAINT PK_Usuarios PRIMARY KEY CLUSTERED (Id ASC),
        CONSTRAINT UQ_Usuarios_Carnet UNIQUE (Carnet),
        CONSTRAINT CK_Usuarios_Rol CHECK (Rol IN ('admin', 'supervisor', 'despachador', 'consulta'))
    );
    PRINT 'Tabla tblUsuariosAsistencia creada.';
END
GO

-- ============================================================================
-- 8. tblImportBatches - Control de importaciones de Excel
-- ============================================================================
IF OBJECT_ID('dbo.tblImportBatches', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblImportBatches (
        Id INT IDENTITY(1,1) NOT NULL,
        Tipo VARCHAR(20) NOT NULL,
        ArchivoNombre VARCHAR(500) NOT NULL,
        HashArchivo VARCHAR(64) NULL,
        UsuarioImporta VARCHAR(100) NOT NULL,
        FechaImportacion DATETIME2(0) NOT NULL CONSTRAINT DF_ImportBatches_Fecha DEFAULT (GETDATE()),
        Estado VARCHAR(20) NOT NULL,
        ResumenJson VARCHAR(MAX) NULL,
        CONSTRAINT PK_ImportBatches PRIMARY KEY CLUSTERED (Id ASC),
        CONSTRAINT CK_ImportBatches_Tipo CHECK (Tipo IN ('CENSO', 'CATALOGO')),
        CONSTRAINT CK_ImportBatches_Estado CHECK (Estado IN ('VALIDADO', 'APLICADO', 'FALLIDO'))
    );
    PRINT 'Tabla tblImportBatches creada.';
END
GO

-- ============================================================================
-- 9. tblImportErrores - Errores detectados en validacion de importaciones
-- ============================================================================
IF OBJECT_ID('dbo.tblImportErrores', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblImportErrores (
        Id INT IDENTITY(1,1) NOT NULL,
        BatchId INT NOT NULL,
        Hoja VARCHAR(100) NOT NULL,
        Fila INT NOT NULL,
        Campo VARCHAR(100) NULL,
        Valor VARCHAR(500) NULL,
        Mensaje VARCHAR(500) NOT NULL,
        CONSTRAINT PK_ImportErrores PRIMARY KEY CLUSTERED (Id ASC),
        CONSTRAINT FK_ImportErrores_Batch FOREIGN KEY (BatchId)
            REFERENCES dbo.tblImportBatches(Id) ON DELETE CASCADE
    );
    PRINT 'Tabla tblImportErrores creada.';
END
GO

-- ============================================================================
-- 10. tblAuditLog - Auditoria de operaciones criticas
-- ============================================================================
IF OBJECT_ID('dbo.tblAuditLog', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAuditLog (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        Tabla VARCHAR(100) NOT NULL,
        Operacion VARCHAR(20) NOT NULL,
        RegistroId INT NULL,
        DatosAnteriores VARCHAR(MAX) NULL,
        DatosNuevos VARCHAR(MAX) NULL,
        Usuario VARCHAR(100) NOT NULL,
        Fecha DATETIME2(0) NOT NULL CONSTRAINT DF_AuditLog_Fecha DEFAULT (GETDATE()),
        CONSTRAINT PK_AuditLog PRIMARY KEY CLUSTERED (Id ASC)
    );
    PRINT 'Tabla tblAuditLog creada.';
END
GO

PRINT 'Todas las tablas creadas exitosamente.';
GO
