using ApiGetToken.DATA;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ApiGetToken.Controllers
{
    /// <summary>
    /// Endpoints auxiliares de autenticación. El login vive en
    /// <c>AccountController</c>; <c>Me</c> devuelve los datos del
    /// usuario autenticado más sus roles y permisos efectivos (resueltos
    /// desde la DB usando el claim <c>Id</c> del JWT). Sirve para que
    /// el front esconda UI según permisos sin tener que re-loguear.
    /// </summary>
    [Route("api/Auth")]
    [ApiController]
    [Authorize]
    public class AuthController : ControllerBase
    {
        private readonly ManagerOdontosoftContext _context;

        public AuthController(ManagerOdontosoftContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GET api/Auth/Me
        /// Devuelve el usuario actual con roles y permisos efectivos
        /// leídos de la DB. Si el token no tiene el claim <c>Id</c>,
        /// devuelve 400.
        /// </summary>
        [HttpGet("Me")]
        public async Task<IActionResult> Me()
        {
            try
            {
                var userIdClaim = User.FindFirstValue("Id")
                                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdClaim, out var userId))
                {
                    return BadRequest(new { error = "El token no contiene un claim 'Id' válido." });
                }

                var u = await _context.Users
                    .Where(x => x.Id == userId)
                    .Select(x => new
                    {
                        x.Id,
                        x.Username,
                        x.Name,
                        x.LastName,
                        x.Email,
                        x.IsAdministrator,
                        x.IsSuperUser,
                        x.Activated,
                        x.Disabled,
                        Roles = x.UsuariosRoles
                            .Where(ur => !ur.Rol!.Disabled)
                            .Select(ur => ur.Rol!.Codigo)
                            .Distinct()
                            .ToList(),
                        Permisos = x.UsuariosRoles
                            .Where(ur => !ur.Rol!.Disabled)
                            .SelectMany(ur => ur.Rol!.RolesPermisos
                                .Where(rp => !rp.Permiso!.Disabled)
                                .Select(rp => rp.Permiso!.Codigo))
                            .Distinct()
                            .ToList()
                    })
                    .FirstOrDefaultAsync();

                if (u == null)
                {
                    return NotFound(new { error = "Usuario no encontrado." });
                }

                return Ok(u);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
