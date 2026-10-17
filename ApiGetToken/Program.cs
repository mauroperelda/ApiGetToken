using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ApiGetToken.DATA;
using ApiGetToken.Helpers.GenerateToken;
using ApiGetToken.Helpers.Authentication;

// Restauramos el comportamiento legacy de Npgsql pre-6.0: permite escribir DateTime con
// Kind=Utc/Unspecified/Local en columnas "timestamp without time zone" sin lanzar excepción.
// Sin esto, UserCreator (que usa DateTime.UtcNow) falla al hacer INSERT contra columnas
// de tipo timestamp. Debe setearse ANTES de cualquier llamada a Npgsql.
// TODO: cuando podamos migrar el schema, usar "timestamp with time zone" (timestamptz) y
// sacar este switch — es el approach idiomático en Postgres.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Forzamos camelCase para que el front reciba propiedades como
        // "token", "result", "message", "isAdministrator", etc.
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
  options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
  {
    Title = "ApiGetToken",
    Version = "v1",
    Description = "Servicio emisor de JWT. Endpoints públicos: POST /api/Account/Login y POST /api/Account/Register. El resto requiere Bearer JWT.",
  });

  // Definición del esquema "Bearer" → habilita el botón "Authorize" en Swagger UI
  // para pegar el JWT y que se envíe en cada request.
  options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
  {
    Description = "JWT emitido por /api/Account/Login. Pegá SOLO el token (sin el prefijo 'Bearer ').",
    Name = "Authorization",
    In = Microsoft.OpenApi.ParameterLocation.Header,
    Type = Microsoft.OpenApi.SecuritySchemeType.Http,
    Scheme = "bearer",
    BearerFormat = "JWT",
  });

  // El AuthOperationFilter agrega el requisito "Bearer" SOLO a las operaciones
  // que tienen [Authorize] (en runtime). Las públicas (Login/Register) quedan
  // sin candado. Ver filtro para los detalles.
  options.OperationFilter<ApiGetToken.AuthOperationFilter>();
});

// CORS para el front (Vite dev server y tunnels públicos trycloudflare.com)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddDbContext<ManagerOdontosoftContext>(option =>
{
    option.UseNpgsql(builder.Configuration.GetConnectionString("cadenaSql"), npgsqlOptions => npgsqlOptions.EnableRetryOnFailure());
});

// ── JWT (compartido con OdontoSoft.API) ────────────────────────────────────
// Monta el middleware de autenticación para que [Authorize] y
// User.FindFirstValue(...) funcionen. La config (Key/Issuer/Audience)
// tiene que matchear el token que emite GenerateToken.
var jwtKey = builder.Configuration["Jwt:Key"]
             ?? throw new InvalidOperationException("Falta Jwt:Key en appsettings.json");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "https://localhost:7229";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "https://localhost:7229";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero,
        };
    });
builder.Services.AddAuthorization(options =>
{
  // "RequireAdministrator" → chequea el claim custom "administrator" (string "true")
  // que emite GenerateToken para usuarios con User.IsAdministrator = true.
  // La razón de NO usar [Authorize(Roles = "Administrador")] directamente es
  // que .NET solo mapea automáticamente el claim ClaimTypes.Role estándar
  // (http://schemas.microsoft.com/ws/2008/06/identity/claims/role); un claim
  // custom "Rol" o "administrator" lo ignora. Por eso armamos una policy
  // explícita.
  options.AddPolicy("RequireAdministrator", policy =>
  {
    policy.RequireAuthenticatedUser();
    policy.RequireAssertion(ctx => ctx.User.HasClaim("administrator", "true"));
  });
});

builder.Services.AddTransient<IGenerateToken, GenerateToken>();
builder.Services.AddTransient<IAuthenticateUser, AuthenticateUser>();
// IUserCreator centraliza la creación de usuarios (registro self-service
// y alta administrativa). Reemplaza al viejo IRegisterUser.
builder.Services.AddTransient<IUserCreator, UserCreator>();

var app = builder.Build();

// ── EF Migrations automáticas al iniciar ──────────────────────────────────
// Aplica las migrations pendientes en cada startup. Idempotente: si no hay
// nada nuevo, no hace nada. Si falla (DB no accesible, migration corrupta),
// la API no arranca — Render detecta el crash en health check y deja el
// deploy viejo sirviendo en lugar del nuevo. Mejor que arrancar y tirar
// errores en cada request.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ManagerOdontosoftContext>();
    var pending = db.Database.GetPendingMigrations().ToList();
    if (pending.Count > 0)
    {
        app.Logger.LogInformation(
            "Aplicando {Count} migrations pendientes: {Names}",
            pending.Count, string.Join(", ", pending));
        db.Database.Migrate();
    }
}

// Configure the HTTP request pipeline.
// Swagger UI disponible en todos los ambientes (como en OdontoSoft.API) porque
// las rutas no son sensibles y el equipo lo usa para inspeccionar el contrato.
// En producción, considerar restringir por red/VPN si la spec es privada.
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowFrontend");
//app.UseHttpsRedirection(); // desactivado: rompe el flujo a través de tunnels HTTP

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
