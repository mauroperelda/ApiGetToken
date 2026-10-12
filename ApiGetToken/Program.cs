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
builder.Services.AddSwaggerGen();

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
builder.Services.AddAuthorization();

builder.Services.AddTransient<IGenerateToken, GenerateToken>();
builder.Services.AddTransient<IAuthenticateUser, AuthenticateUser>();
// IUserCreator centraliza la creación de usuarios (registro self-service
// y alta administrativa). Reemplaza al viejo IRegisterUser.
builder.Services.AddTransient<IUserCreator, UserCreator>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
//app.UseHttpsRedirection(); // desactivado: rompe el flujo a través de tunnels HTTP

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
