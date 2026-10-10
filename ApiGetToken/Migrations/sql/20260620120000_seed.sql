-- Seed 20260620120000: 32 permisos + 3 roles + 54 asignaciones RolPermiso + 1 usuario admin.
-- Se asume DB ya seleccionada por el caller (MANAGER_ODONTOSOFT).
-- NO usar USE porque sobreescribe la DB del command line.

SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- ============================================================
-- 1) Permisos
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM Permiso WHERE Codigo = 'pacientes.ver')
BEGIN
    SET IDENTITY_INSERT Permiso ON;
    INSERT INTO Permiso (Id, Codigo, Modulo, Nombre, Descripcion, CreatedUserId, CreatedUserName, CreatedDate, Disabled) VALUES
        ( 1, 'pacientes.ver',         'Pacientes',    'Ver pacientes',                          NULL, 0, 'system', GETUTCDATE(), 0),
        ( 2, 'pacientes.crear',       'Pacientes',    'Crear pacientes',                        NULL, 0, 'system', GETUTCDATE(), 0),
        ( 3, 'pacientes.editar',      'Pacientes',    'Editar pacientes',                       NULL, 0, 'system', GETUTCDATE(), 0),
        ( 4, 'pacientes.eliminar',    'Pacientes',    'Eliminar pacientes',                     NULL, 0, 'system', GETUTCDATE(), 0),
        ( 5, 'turnos.ver',            'Turnos',       'Ver agenda',                             NULL, 0, 'system', GETUTCDATE(), 0),
        ( 6, 'turnos.crear',          'Turnos',       'Crear turnos',                           NULL, 0, 'system', GETUTCDATE(), 0),
        ( 7, 'turnos.editar',         'Turnos',       'Editar turnos',                          NULL, 0, 'system', GETUTCDATE(), 0),
        ( 8, 'turnos.eliminar',       'Turnos',       'Eliminar turnos',                        NULL, 0, 'system', GETUTCDATE(), 0),
        ( 9, 'profesionales.ver',     'Profesionales','Ver profesionales',                      NULL, 0, 'system', GETUTCDATE(), 0),
        (10, 'profesionales.crear',   'Profesionales','Crear profesionales',                    NULL, 0, 'system', GETUTCDATE(), 0),
        (11, 'profesionales.editar',  'Profesionales','Editar profesionales',                   NULL, 0, 'system', GETUTCDATE(), 0),
        (12, 'profesionales.eliminar','Profesionales','Eliminar profesionales',                 NULL, 0, 'system', GETUTCDATE(), 0),
        (13, 'tratamientos.ver',      'Tratamientos', 'Ver tratamientos',                       NULL, 0, 'system', GETUTCDATE(), 0),
        (14, 'tratamientos.crear',    'Tratamientos', 'Crear tratamientos',                     NULL, 0, 'system', GETUTCDATE(), 0),
        (15, 'tratamientos.editar',   'Tratamientos', 'Editar tratamientos',                    NULL, 0, 'system', GETUTCDATE(), 0),
        (16, 'tratamientos.eliminar', 'Tratamientos', 'Eliminar tratamientos',                  NULL, 0, 'system', GETUTCDATE(), 0),
        (17, 'presupuestos.ver',      'Facturacion',  'Ver presupuestos',                       NULL, 0, 'system', GETUTCDATE(), 0),
        (18, 'presupuestos.crear',    'Facturacion',  'Crear presupuestos',                     NULL, 0, 'system', GETUTCDATE(), 0),
        (19, 'presupuestos.editar',   'Facturacion',  'Editar presupuestos',                    NULL, 0, 'system', GETUTCDATE(), 0),
        (20, 'presupuestos.eliminar', 'Facturacion',  'Eliminar presupuestos',                  NULL, 0, 'system', GETUTCDATE(), 0),
        (21, 'pagos.ver',             'Facturacion',  'Ver pagos',                              NULL, 0, 'system', GETUTCDATE(), 0),
        (22, 'pagos.crear',           'Facturacion',  'Registrar pagos',                        NULL, 0, 'system', GETUTCDATE(), 0),
        (23, 'pagos.editar',          'Facturacion',  'Editar pagos',                           NULL, 0, 'system', GETUTCDATE(), 0),
        (24, 'pagos.eliminar',        'Facturacion',  'Anular pagos',                           NULL, 0, 'system', GETUTCDATE(), 0),
        (25, 'ubicaciones.ver',       'Ubicaciones',  'Ver ubicaciones',                        NULL, 0, 'system', GETUTCDATE(), 0),
        (26, 'ubicaciones.editar',    'Ubicaciones',  'Editar ubicaciones',                     NULL, 0, 'system', GETUTCDATE(), 0),
        (27, 'roles.ver',             'Sistema',      'Ver roles y permisos',                   NULL, 0, 'system', GETUTCDATE(), 0),
        (28, 'roles.editar',          'Sistema',      'Crear/editar roles y asignar permisos',  NULL, 0, 'system', GETUTCDATE(), 0),
        (29, 'usuarios.ver',          'Sistema',      'Ver usuarios',                           NULL, 0, 'system', GETUTCDATE(), 0),
        (30, 'usuarios.editar',       'Sistema',      'Asignar roles a usuarios',               NULL, 0, 'system', GETUTCDATE(), 0),
        (31, 'administrator',         'Sistema',      'Superusuario: bypasea toda autorización',NULL, 0, 'system', GETUTCDATE(), 0),
        (32, 'superuser',             'Sistema',      'Bandera legacy de ApiGetToken',          NULL, 0, 'system', GETUTCDATE(), 0);
    SET IDENTITY_INSERT Permiso OFF;
END

-- ============================================================
-- 2) Roles
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM Rol WHERE Codigo = 'admin')
BEGIN
    SET IDENTITY_INSERT Rol ON;
    INSERT INTO Rol (Id, Codigo, Nombre, Descripcion, CreatedUserId, CreatedUserName, CreatedDate, Disabled) VALUES
        (1, 'admin',         'Administrador',  'Acceso total al sistema', 0, 'system', GETUTCDATE(), 0),
        (2, 'profesional',   'Profesional',    'Odontólogo/a',            0, 'system', GETUTCDATE(), 0),
        (3, 'recepcionista', 'Recepcionista',  'Atención al paciente',    0, 'system', GETUTCDATE(), 0);
    SET IDENTITY_INSERT Rol OFF;
END

-- ============================================================
-- 3) RolPermiso
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM RolPermiso)
BEGIN
    -- admin: todos los permisos
    INSERT INTO RolPermiso (RolId, PermisoId, CreatedUserId, CreatedUserName, CreatedDate)
    SELECT 1, Id, 0, 'system', GETUTCDATE() FROM Permiso;

    -- profesional: turnos + pacientes (ver/crear/editar) + tratamientos.ver + presupuestos.ver + pagos.ver
    INSERT INTO RolPermiso (RolId, PermisoId, CreatedUserId, CreatedUserName, CreatedDate) VALUES
        (2, 1, 0, 'system', GETUTCDATE()),
        (2, 2, 0, 'system', GETUTCDATE()),
        (2, 3, 0, 'system', GETUTCDATE()),
        (2, 5, 0, 'system', GETUTCDATE()),
        (2, 6, 0, 'system', GETUTCDATE()),
        (2, 7, 0, 'system', GETUTCDATE()),
        (2, 13, 0, 'system', GETUTCDATE()),
        (2, 17, 0, 'system', GETUTCDATE()),
        (2, 21, 0, 'system', GETUTCDATE());

    -- recepcionista: CRUD pacientes/turnos + ver profesionales, tratamientos, presupuestos, pagos, ubicaciones
    INSERT INTO RolPermiso (RolId, PermisoId, CreatedUserId, CreatedUserName, CreatedDate) VALUES
        (3, 1, 0, 'system', GETUTCDATE()),
        (3, 2, 0, 'system', GETUTCDATE()),
        (3, 3, 0, 'system', GETUTCDATE()),
        (3, 4, 0, 'system', GETUTCDATE()),
        (3, 5, 0, 'system', GETUTCDATE()),
        (3, 6, 0, 'system', GETUTCDATE()),
        (3, 7, 0, 'system', GETUTCDATE()),
        (3, 8, 0, 'system', GETUTCDATE()),
        (3, 9, 0, 'system', GETUTCDATE()),
        (3, 13, 0, 'system', GETUTCDATE()),
        (3, 17, 0, 'system', GETUTCDATE()),
        (3, 21, 0, 'system', GETUTCDATE()),
        (3, 25, 0, 'system', GETUTCDATE());
END

-- ============================================================
-- 4) Asignar al usuario existente (Id=1) el rol admin.
--    La DB ya tiene el usuario real; el seed no crea uno nuevo.
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM UsuarioRol WHERE UsuarioId = 1 AND RolId = 1)
    INSERT INTO UsuarioRol (UsuarioId, RolId, CreatedUserId, CreatedUserName, CreatedDate) VALUES (1, 1, 0, 'system', GETUTCDATE());

COMMIT;

PRINT 'Seed 20260620120000 aplicado correctamente.';
GO
