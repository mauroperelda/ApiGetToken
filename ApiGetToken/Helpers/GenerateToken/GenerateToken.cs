using ApiGetToken.DATA;
using ApiGetToken.Models;
using ApiGetToken.Models.Custom;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ApiGetToken.Helpers.GenerateToken
{
    public class GenerateToken : IGenerateToken
    {
        private readonly ManagerOdontosoftContext _managerOdontosoftContext;
        private readonly IConfiguration _configuration;

        public GenerateToken(ManagerOdontosoftContext managerOdontosoftContext, IConfiguration configuration)
        {
            _managerOdontosoftContext = managerOdontosoftContext;
            _configuration = configuration;
        }

        public string Generate(LoginUserResponse user)
        {
            // _configuration[...] es string? bajo nullable enabled. Program.cs
            // ya valida que la key exista al arrancar, así que acá el ! es
            // seguro: si llegamos a este punto, la key está garantizada.
            var jwtKey = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("Falta Jwt:Key en la configuración.");
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Defensivo: si llegan nulls no rompemos la generación
            var fullName = $"{user.Name} {user.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(fullName)) fullName = user.Username ?? "user";

            // Crear los claims básicos del usuario
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, fullName),
                new Claim("Nombre", user.Name ?? string.Empty),
                new Claim("Apellido", user.LastName ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim("Id", user.Id.ToString()),
            };

            if (!string.IsNullOrEmpty(user.Username))
                claims.Add(new Claim("Username", user.Username));

            // Banderas legacy que OdontoSoft.API chequea como short-circuit
            if (user.IsAdministrator)
                claims.Add(new Claim("administrator", "true"));
            if (user.IsSuperUser)
                claims.Add(new Claim("superuser", "true"));

            // ── RBAC: roles y permisos efectivos ───────────────────────
            // Resueltos desde la DB con un solo round-trip. El JWT queda
            // firmado con TODO lo que el back necesita para autorizar sin
            // pegarle a la DB en cada request. Tradeoff: si cambian los
            // permisos hay que volver a loguear.
            var (roles, permisos) = GetRolesYPermisos(user.Id);

            // Roles: claim por código (mismo nombre que en la DB)
            foreach (var rol in roles)
            {
                claims.Add(new Claim("Rol", rol));
            }

            // Permisos: un claim por código. OdontoSoft.API los lee con
            // User.HasClaim("Permiso", codigo) o su PermissionService.
            foreach (var permiso in permisos)
            {
                claims.Add(new Claim("Permiso", permiso));
            }

            // Crear el token
            var token = new JwtSecurityToken(_configuration["Jwt:Issuer"],
                                             _configuration["Jwt:Audience"],
                                             claims,
                                             expires: DateTime.UtcNow.AddHours(8),
                                             signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Devuelve (roles, permisos) efectivos del usuario. Hace una sola
        /// query que cruza UsuarioRol → RolPermiso → Permiso. Los roles
        /// con Disabled=1 no se incluyen.
        /// </summary>
        private (IReadOnlyList<string> Roles, IReadOnlyList<string> Permisos) GetRolesYPermisos(int userId)
        {
            if (userId <= 0) return (Array.Empty<string>(), Array.Empty<string>());

            // JOIN simple: UsuarioRol → Rol → RolPermiso → Permiso,
            // filtrando Disabled en ambos lados.
            var rows = _managerOdontosoftContext.UsuariosRoles
                .Where(ur => ur.UsuarioId == userId
                          && ur.Rol!.Disabled == false
                          && ur.Rol.RolesPermisos.Any())
                .SelectMany(ur => ur.Rol!.RolesPermisos
                    .Where(rp => rp.Permiso!.Disabled == false)
                    .Select(rp => new
                    {
                        Rol = ur.Rol!.Codigo,
                        Permiso = rp.Permiso!.Codigo
                    }))
                .ToList();

            var roles = rows.Select(r => r.Rol).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var permisos = rows.Select(r => r.Permiso).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            return (roles, permisos);
        }
    }
}
