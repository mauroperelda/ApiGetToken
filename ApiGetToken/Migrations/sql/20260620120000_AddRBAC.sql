-- Se asume que el caller ya seleccionó la DB correcta (ej. sqlcmd -d MANAGER_ODONTOSOFT).
-- NO usar USE aquí porque sobreescribe la DB del command line.

-- 1) Dropear FK User→Profile (lookup por referenced_object_id evita
--    parsear el nombre con puntos como schema separator).
DECLARE @fkName NVARCHAR(128);
SELECT @fkName = fk.name
FROM sys.foreign_keys fk
WHERE fk.parent_object_id = OBJECT_ID('[User]')
  AND fk.referenced_object_id = OBJECT_ID('Profile');
IF @fkName IS NOT NULL
BEGIN
    DECLARE @sql1 NVARCHAR(MAX) = 'ALTER TABLE [User] DROP CONSTRAINT ' + QUOTENAME(@fkName);
    EXEC sp_executesql @sql1;
END
GO

-- 2) Dropear el índice sobre ProfileId
DECLARE @idxName NVARCHAR(128);
SELECT @idxName = i.name
FROM sys.indexes i
WHERE i.object_id = OBJECT_ID('[User]')
  AND i.name = 'IX_User_ProfileId';
IF @idxName IS NOT NULL
BEGIN
    DECLARE @sql2 NVARCHAR(MAX) = 'DROP INDEX ' + QUOTENAME(@idxName) + ' ON [User]';
    EXEC sp_executesql @sql2;
END
GO

-- 3) Dropear la columna ProfileId
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[User]') AND name = 'ProfileId')
    ALTER TABLE [User] DROP COLUMN ProfileId;
GO

-- 4) Dropear tabla Profile
IF OBJECT_ID('Profile', 'U') IS NOT NULL
    DROP TABLE Profile;
GO

IF OBJECT_ID('Permiso', 'U') IS NULL
BEGIN
    CREATE TABLE Permiso (
        Id                 INT IDENTITY(1,1) NOT NULL,
        Codigo             NVARCHAR(100) NOT NULL,
        Modulo             NVARCHAR(50)  NOT NULL,
        Nombre             NVARCHAR(150) NOT NULL,
        Descripcion        NVARCHAR(500) NULL,
        CreatedUserId      INT           NOT NULL,
        CreatedUserName    NVARCHAR(MAX) NOT NULL,
        CreatedDate        DATETIME      NOT NULL,
        UpdatedUserId      INT           NULL,
        UpdatedUserName    NVARCHAR(MAX) NULL,
        UpdatedDate        DATETIME      NULL,
        Disabled           BIT           NOT NULL CONSTRAINT DF_Permiso_Disabled DEFAULT (0),
        CONSTRAINT PK_Permiso PRIMARY KEY CLUSTERED (Id)
    );
    CREATE UNIQUE INDEX UX_Permiso_Codigo ON Permiso(Codigo);
    CREATE INDEX IX_Permiso_Modulo ON Permiso(Modulo);
END
GO

IF OBJECT_ID('Rol', 'U') IS NULL
BEGIN
    CREATE TABLE Rol (
        Id                 INT IDENTITY(1,1) NOT NULL,
        Codigo             NVARCHAR(100) NOT NULL,
        Nombre             NVARCHAR(150) NOT NULL,
        Descripcion        NVARCHAR(500) NULL,
        CreatedUserId      INT           NOT NULL,
        CreatedUserName    NVARCHAR(MAX) NOT NULL,
        CreatedDate        DATETIME      NOT NULL,
        UpdatedUserId      INT           NULL,
        UpdatedUserName    NVARCHAR(MAX) NULL,
        UpdatedDate        DATETIME      NULL,
        Disabled           BIT           NOT NULL CONSTRAINT DF_Rol_Disabled DEFAULT (0),
        CONSTRAINT PK_Rol PRIMARY KEY CLUSTERED (Id)
    );
    CREATE UNIQUE INDEX UX_Rol_Codigo ON Rol(Codigo);
END
GO

IF OBJECT_ID('RolPermiso', 'U') IS NULL
BEGIN
    CREATE TABLE RolPermiso (
        RolId              INT           NOT NULL,
        PermisoId          INT           NOT NULL,
        CreatedUserId      INT           NOT NULL,
        CreatedUserName    NVARCHAR(MAX) NOT NULL,
        CreatedDate        DATETIME      NOT NULL,
        CONSTRAINT PK_RolPermiso PRIMARY KEY CLUSTERED (RolId, PermisoId),
        CONSTRAINT FK_RolPermiso_Rol_RolId     FOREIGN KEY (RolId)     REFERENCES Rol(Id)     ON DELETE CASCADE,
        CONSTRAINT FK_RolPermiso_Permiso_PermisoId FOREIGN KEY (PermisoId) REFERENCES Permiso(Id) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX UX_RolPermiso ON RolPermiso(RolId, PermisoId);
END
GO

IF OBJECT_ID('UsuarioRol', 'U') IS NULL
BEGIN
    CREATE TABLE UsuarioRol (
        UsuarioId          INT           NOT NULL,
        RolId              INT           NOT NULL,
        CreatedUserId      INT           NOT NULL,
        CreatedUserName    NVARCHAR(MAX) NOT NULL,
        CreatedDate        DATETIME      NOT NULL,
        CONSTRAINT PK_UsuarioRol PRIMARY KEY CLUSTERED (UsuarioId, RolId),
        CONSTRAINT FK_UsuarioRol_User_UsuarioId FOREIGN KEY (UsuarioId) REFERENCES [User](Id) ON DELETE CASCADE,
        CONSTRAINT FK_UsuarioRol_Rol_RolId     FOREIGN KEY (RolId)     REFERENCES Rol(Id)     ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX UX_UsuarioRol ON UsuarioRol(UsuarioId, RolId);
END
GO

PRINT 'Migracion 20260620120000_AddRBAC aplicada correctamente.';
GO
