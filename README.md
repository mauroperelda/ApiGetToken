# ApiGetToken

API REST en **ASP.NET Core 10.0** que emite y valida **tokens JWT** con un módulo completo de **RBAC** (Role-Based Access Control: Usuarios, Roles y Permisos). Está pensada como servicio de identidad compartido: el front (Vite + React) consume `POST /api/Account/Login`, recibe un JWT firmado con sus **roles y permisos efectivos ya embebidos en los claims**, y lo presenta en cada request. La API también valida ese mismo token en endpoints protegidos vía `[Authorize]`.

Forma parte del ecosistema **OdontoSoft**: la configuración JWT (`Key`/`Issuer`/`Audience`) está coordinada para que los tokens emitidos acá sean aceptados por `OdontoSoft.API`.

---

## 🧱 Stack tecnológico

| Capa | Tecnología |
|------|------------|
| Framework | ASP.NET Core 10.0 (`net10.0`), `Nullable` + `ImplicitUsings` |
| ORM | Entity Framework Core 10.0 (Code-First + Migrations) |
| Base de datos | SQL Server (`ManagerOdontosoftContext`) |
| Hashing de contraseñas | `BCrypt.Net-Next 4.2.0` (workFactor = 11) |
| Autenticación | `Microsoft.AspNetCore.Authentication.JwtBearer` |
| Tokens | `System.IdentityModel.Tokens.Jwt` + `Microsoft.IdentityModel.Tokens` |
| Documentación | `Swashbuckle.AspNetCore` (Swagger UI en Development) |
| CORS | Política `AllowFrontend` abierta (Vite + tunnels `trycloudflare`) |

---

## 🗂️ Estructura del proyecto

```
ApiGetToken/
├── ApiGetToken.sln                        # Solución con 1 proyecto
├── LICENSE                                # Licencia MIT
├── README.md                              # Este archivo
└── ApiGetToken/                           # Proyecto principal (Web SDK)
    │
    ├── ApiGetToken.csproj                 # Paquetes NuGet y target net10.0
    ├── Program.cs                         # Bootstrap: DI, EF, JWT, CORS, Swagger
    ├── appsettings.json                   # Connection string + Jwt (Key/Issuer/Audience)
    ├── appsettings.Development.json
    ├── Properties/launchSettings.json     # Perfiles de ejecución (IIS Express, etc.)
    │
    ├── Controllers/                       # Capa HTTP (ApiController)
    │   ├── AccountController.cs           # POST /api/Account/Login  +  /Register   (públicos)
    │   ├── AuthController.cs              # GET  /api/Auth/Me                        ([Authorize])
    │   ├── UsuariosController.cs          # CRUD /api/Usuarios + membresía Rol
    │   ├── RolesController.cs             # CRUD /api/Roles    + membresía Permiso
    │   └── PermisosController.cs          # CRUD /api/Permisos
    │
    ├── Models/                            # Entidades de dominio + DTOs
    │   ├── User.cs                        # Entidad principal (Id, Username, Password, flags admin/super)
    │   ├── Rol.cs                         # Entidad Rol (Codigo, Nombre, Disabled)
    │   ├── Permiso.cs                     # Entidad Permiso (Codigo, Modulo, Nombre, Disabled)
    │   ├── RolPermiso.cs                  # Tabla join N:M entre Rol y Permiso
    │   ├── UsuarioRol.cs                  # Tabla join N:M entre User y Rol
    │   └── Custom/                        # DTOs de request/response
    │       ├── LoginUserRequest.cs         # { Username, Password }
    │       ├── LoginUserResponse.cs       # Datos del usuario + Token + Result/Message
    │       └── RegisterUserRequest.cs     # Validación con DataAnnotations + RolIds opcionales
    │
    ├── DATA/                              # Capa de persistencia (EF Core)
    │   └── ManagerOdontosoftContext.cs    # DbContext + Fluent API (índices únicos, FKs)
    │
    ├── Helpers/                           # Servicios de soporte (DI scoped/transient)
    │   ├── Authentication/
    │   │   ├── IAuthenticateUser.cs       # Contrato de verificación de credenciales
    │   │   ├── AuthenticateUser.cs        # BCrypt.Verify + lazy-upgrade desde password plana
    │   │   ├── IUserCreator.cs            # Contrato de creación de usuarios
    │   │   └── UserCreator.cs             # Unicidad + hash + auditoría (compartido por self-register y alta admin)
    │   └── GenerateToken/
    │       ├── IGenerateToken.cs          # Contrato del emisor de JWTs
    │       └── GenerateToken.cs           # Arma claims + firma HS256 + carga roles/permisos de DB
    │
    └── Migrations/                        # Migraciones EF Core
        ├── 20250106223739_MigEntityUserAndProfile.*   # Inicial: User + Profile (ya deprecated)
        └── 20260620120000_AddRBAC.*                  # Reemplaza Profile por módulo RBAC
```

---

## 🏛️ Arquitectura

### Capas y responsabilidades

```
┌─────────────────────────────────────────────────────────────────────┐
│                          Controllers (HTTP)                          │
│  AccountController · AuthController · UsuariosController            │
│  RolesController    · PermisosController                            │
│  - Reciben/responden JSON.                                          │
│  - Validan body básico, mapean a DTOs, gestionan status code.       │
└──────────────────────────────┬──────────────────────────────────────┘
                               ▼
┌─────────────────────────────────────────────────────────────────────┐
│                      Helpers (Application Services)                 │
│                                                                      │
│  IAuthenticateUser ──► AuthenticateUser (BCrypt.Verify + lazy-upgrade)│
│  IUserCreator      ──► UserCreator (unicidad, hash, auditoría)      │
│  IGenerateToken    ──► GenerateToken (claims + firma HS256)         │
│                                                                      │
│  - Lógica reutilizable inyectada vía DI (AddTransient).             │
└──────────────────────────────┬──────────────────────────────────────┘
                               ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    DATA (EF Core / DbContext)                        │
│                                                                      │
│  ManagerOdontosoftContext (SQL Server)                              │
│  - DbSets: Users, Roles, Permisos, RolesPermisos, UsuariosRoles    │
│  - Fluent API: longitudes, índices únicos, FKs en cascada.         │
└──────────────────────────────┬──────────────────────────────────────┘
                               ▼
                       SQL Server (MANAGER_ODONTOSOFT)
```

### Inyección de dependencias (`Program.cs`)

```csharp
builder.Services.AddTransient<IGenerateToken,  GenerateToken>();
builder.Services.AddTransient<IAuthenticateUser, AuthenticateUser>();
builder.Services.AddTransient<IUserCreator,    UserCreator>();

builder.Services.AddDbContext<ManagerOdontosoftContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("cadenaSql"),
                     sql => sql.EnableRetryOnFailure()));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* HS256, Issuer, Audience, Key */ });
```

Patrón elegido: **interfaces + AddTransient**, sin contenedor externo (DI nativo de ASP.NET Core). `ManagerOdontosoftContext` se registra *scoped* automáticamente con `AddDbContext`.

---

## 🗃️ Modelo de datos (RBAC)

```
┌──────────┐        N:M         ┌────────┐        N:M         ┌──────────┐
│   User   │◄──── UsuarioRol ──►│  Rol   │◄──── RolPermiso ──►│ Permiso  │
└──────────┘                    └────────┘                    └──────────┘
   │                               │                              │
   │ Username, Password (BCrypt)   │ Codigo (único)              │ Codigo (único)
   │ IsAdministrator, IsSuperUser  │ Nombre                      │ Modulo
   │ Activated, Disabled           │ Disabled (soft delete)      │ Nombre, Descripcion
   │ ChangePasswordRequired       └─────────────────────────────┘ Disabled (soft delete)
   │ Auditoría: Created/UpdatedUserId, CreatedDate, etc.
   └─────────────────────────────────────────────────────────────► Auditoría idéntica
```

* **Eliminación lógica en todas las entidades** (`Disabled` BIT NOT NULL). El `Delete` de los controllers no borra filas, marca `Disabled = true`.
* **Índices únicos** en `Rol.Codigo`, `Permiso.Codigo` y las PKs compuestas de las joins (`UX_RolPermiso`, `UX_UsuarioRol`).
* **FKs en cascada**: borrar un usuario o un rol limpia sus filas de join automáticamente.
* **`User.ProfileId` se eliminó** en la migración `AddRBAC`: la relación con el perfil pasó de 1:N a N:M vía `UsuarioRol`.

---

## 🔐 Flujo de autenticación JWT

### Login

```
1. Cliente → POST /api/Account/Login  { Username, Password }
                │
                ▼
2. AuthenticateUser.Authenticate(request)
      - Busca en Users WHERE (Username == id OR Email == id) AND Disabled = 0
      - VerifyAndMigratePassword():
          · Si el hash empieza con "$2..." → BCrypt.Verify
          · Si es password plana legacy → compara exacto y, si matchea,
            la re-hashea con BCrypt(workFactor:11) en el mismo login
      - Validaciones: Activated = true
                │
                ▼
3. GenerateToken.Generate(loginUserResponse)
      - Toma Jwt:Key / Issuer / Audience de appsettings.json
      - Crea SymmetricSecurityKey + SigningCredentials(HmacSha256)
      - Inyecta claims:
            Name, Nombre, Apellido, Email, Id, Username,
            "administrator" / "superuser" (si corresponde),
            Rol[]       (códigos de los roles del usuario, vía Join),
            Permiso[]   (códigos de los permisos efectivos)
      - expires: DateTime.UtcNow.AddHours(8)
                │
                ▼
4. Controller responde 200 con LoginUserResponse { Token, ... }
```

### Validación en requests posteriores

1. Cliente envía header `Authorization: Bearer <token>`.
2. El middleware `JwtBearer` valida firma, issuer, audience y lifetime (`ValidateIssuer/Audience/Lifetime/IssuerSigningKey`, `ClockSkew = Zero`).
3. `[Authorize]` acepta o rechaza el endpoint. `User.FindFirstValue("Id")` resuelve el id de usuario.
4. `GET /api/Auth/Me` rehidrata **roles y permisos desde la DB** (no desde el JWT) para reflejar altas/bajas en tiempo real.

> **Trade-off explícito:** los roles/permisos van embebidos en el JWT para que `OdontoSoft.API` autorice sin pegar a la DB. Tras cambiar la membresía, el usuario debe re-loguear para que `GenerateToken` re-arme los claims.

### Hashing y migración de credenciales

* **Nuevos usuarios** → `BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11)` (salt aleatorio embebido en el hash).
* **Login de usuarios legacy** → si la columna `Password` no empieza con `$2…`, se compara en plano y, si matchea, se re-hashea in-place (lazy upgrade transparente).

---

## 🌐 Endpoints

| Verbo | Ruta | Auth | Descripción |
|-------|------|------|-------------|
| POST | `/api/Account/Login` | — | Login con `LoginUserRequest`; devuelve `LoginUserResponse` con `Token`. |
| POST | `/api/Account/Register` | — | Self-register con `RegisterUserRequest`; auto-login. |
| GET  | `/api/Auth/Me` | `[Authorize]` | Devuelve el usuario actual + roles + permisos efectivos desde DB. |
| GET  | `/api/Usuarios` | —* | Listado paginado + búsqueda por `searchText`. |
| GET  | `/api/Usuarios/{id}` | —* | Detalle de usuario con `RolIds`. |
| POST | `/api/Usuarios` | —* | Alta administrativa (acepta `RolIds`, los asigna). |
| PUT  | `/api/Usuarios/{id}` | —* | Modifica datos + reemplaza `RolIds`. |
| DELETE | `/api/Usuarios/{id}` | —* | Soft delete (`Disabled = true`). |
| GET/POST/DELETE | `/api/Usuarios/{id}/Roles[/{rolId}]` | —* | Gestión de membresía User↔Rol. |
| GET  | `/api/Roles` | —* | Listado paginado de roles. |
| GET  | `/api/Roles/{id}` | —* | Detalle de rol con sus permisos. |
| POST/PUT/DELETE | `/api/Roles[...]` | —* | CRUD + reemplazo de `PermisoIds`. |
| GET  | `/api/Permisos` | —* | Listado paginado, filtro opcional por `modulo`. |
| GET/POST/PUT/DELETE | `/api/Permisos[...]` | —* | CRUD (soft delete en DELETE). |

> *Los controllers no declaran `[Authorize]` a nivel de clase; en una siguiente iteración deberían anotarse según los permisos del módulo RBAC. Hoy la autorización vive solo a nivel de `[Authorize]` global en `AuthController` y/o por JWT válido.

Formato de respuesta unificado:
- **Listados** → `{ total, page, pageSize, items }`.
- **Errores** → `{ error: "mensaje" }` con `BadRequest` / `NotFound` / `NoContent`.
- JSON serializado en **camelCase** (forzado en `Program.cs` con `JsonNamingPolicy.CamelCase`).

---

## ⚙️ Configuración

### `appsettings.json` (plantilla)

```json
{
  "ConnectionStrings": {
    "cadenaSql": "Server=...;Database=MANAGER_ODONTOSOFT;User ID=sa;Password=...;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key":      "<clave-secreta-compartida-con-OdontoSoft.API>",
    "Issuer":   "https://localhost:7229",
    "Audience": "https://localhost:7229"
  }
}
```

* `Jwt:Key` se valida al arranque: si falta, la app lanza `InvalidOperationException` (fail-fast en `Program.cs`).
* Los valores por defecto están pensados para entorno local; en producción deben venir de **User Secrets**, **variables de entorno** o un **KeyVault**.

### Comandos útiles

```bash
# Restaurar + compilar
dotnet restore
dotnet build

# Crear una migración nueva (si tocás el modelo)
dotnet ef migrations add NombreMigracion --project ApiGetToken

# Aplicar migraciones a la DB
dotnet ef database update --project ApiGetToken

# Levantar la API (perfil por defecto)
dotnet run --project ApiGetToken

# Swagger UI (sólo en Development)
https://localhost:{puerto}/swagger
```

---

## 🔒 Decisiones de seguridad

* **HTTPS redirection está deshabilitado** a propósito en `Program.cs` para no romper el flujo detrás de tunnels HTTP (`trycloudflare`/Vite dev). En producción debería activarse y exponerse detrás de TLS terminado por el proxy.
* **CORS `AllowFrontend`** abierto a cualquier origen/método/header — aceptable en entorno de desarrollo con front separado; en prod conviene reducirlo a orígenes conocidos.
* **Las passwords son case-sensitive**: `AuthenticateUser` sólo normaliza el identificador (username/email) a minúsculas; la password se compara contra el hash tal cual llega.
* **BCrypt con workFactor 11** balancea seguridad y latencia (~100 ms por hash en hardware estándar).
* **Auditoría mínima** (`CreatedUserId/Name/Date`, `UpdatedUserId/Name/Date`) en todas las entidades para trazabilidad.

---

## 🧭 Roadmap / posibles mejoras

* Anotar cada endpoint con `[Authorize(Policy = "Permiso:X")]` o similar y definir policies centralizadas que lean el claim `"Permiso"`.
* Reemplazar los `CreatedUserId = 0`/`"system"` fijos por el id/username del usuario autenticado (obtenido del JWT).
* Refrescar `Roles`/`Permisos` del JWT sin re-login (sliding session o endpoint de refresh).
* Mover `ManagerOdontosoftContext` y los helpers a una **`Class Library`** para poder reusarlos desde `OdontoSoft.API` y mantener un único mapeo de entidades.
* Endpoints de auditoría / listado de cambios.
* Tests de integración con `WebApplicationFactory` + `Testcontainers` para SQL Server.

---

## 📜 Licencia

MIT — ver [LICENSE](LICENSE).
