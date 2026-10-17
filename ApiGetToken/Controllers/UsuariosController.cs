using ApiGetToken.DATA;
using ApiGetToken.Helpers.Authentication;
using ApiGetToken.Models;
using ApiGetToken.Models.Custom;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiGetToken.Controllers
{
    /// <summary>
    /// CRUD de <see cref="User"/> + sub-endpoints para asignar/quitar
    /// roles a un usuario (vía la join <c>UsuarioRol</c>). Al asignar
    /// roles se reemplazan los anteriores; el usuario debe re-loguear
    /// para que el JWT incluya los nuevos permisos efectivos.
    /// </summary>
    [Route("api/Usuarios")]
    [ApiController]
    [ApiGetToken.RequireAdministrator]
    public class UsuariosController : ControllerBase
    {
        private readonly ManagerOdontosoftContext _context;
        private readonly IUserCreator _userCreator;

        public UsuariosController(ManagerOdontosoftContext context, IUserCreator userCreator)
        {
            _context = context;
            _userCreator = userCreator;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? searchText = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 200) pageSize = 20;

                var query = _context.Users.Where(u => !u.Disabled);

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    var s = $"%{searchText.Trim()}%";
                    query = query.Where(u =>
                        EF.Functions.Like(u.Username, s) ||
                        EF.Functions.Like(u.Name, s) ||
                        EF.Functions.Like(u.LastName, s) ||
                        EF.Functions.Like(u.Email, s));
                }

                var total = await query.CountAsync();
                var items = await query
                    .OrderBy(u => u.Username)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new
                    {
                        u.Id,
                        u.Username,
                        u.Name,
                        u.LastName,
                        u.Email,
                        u.IsAdministrator,
                        u.IsSuperUser,
                        u.Activated,
                        u.Disabled,
                        RolIds = u.UsuariosRoles
                            .Where(ur => !ur.Rol!.Disabled)
                            .Select(ur => ur.RolId)
                            .ToList()
                    })
                    .ToListAsync();

                return Ok(new { total, page, pageSize, items });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var u = await _context.Users
                    .Where(x => x.Id == id)
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
                        x.ChangePasswordRequired,
                        RolIds = x.UsuariosRoles
                            .Where(ur => !ur.Rol!.Disabled)
                            .Select(ur => ur.RolId)
                            .ToList()
                    })
                    .FirstOrDefaultAsync();

                if (u == null) return NotFound(new { error = "Usuario no encontrado." });
                return Ok(u);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RegisterUserRequest request)
        {
            try
            {
                if (request == null) return BadRequest(new { error = "Body requerido." });
                if (string.IsNullOrWhiteSpace(request.Username))
                    return BadRequest(new { error = "El campo 'username' es obligatorio." });
                if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
                    return BadRequest(new { error = "La contraseña es obligatoria y debe tener al menos 8 caracteres." });
                if (string.IsNullOrWhiteSpace(request.Email))
                    return BadRequest(new { error = "El campo 'email' es obligatorio." });

                // La creación del User (validación de unicidad, normalización,
                // auditoría) vive en IUserCreator, compartida con el registro
                // self-service de /api/Account/Register.
                var u = _userCreator.Create(request, "admin");
                // (IUserCreator hace SaveChanges sincrónico; el resto del
                //  controller sigue async.)

                if (request.RolIds != null && request.RolIds.Count > 0)
                {
                    await ReplaceRoles(u.Id, request.RolIds);
                }

                return Ok(new
                {
                    u.Id,
                    u.Username,
                    u.Name,
                    u.LastName,
                    u.Email,
                    u.IsAdministrator,
                    u.IsSuperUser,
                    u.Activated,
                    u.Disabled
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUsuarioRequest request)
        {
            try
            {
                var u = await _context.Users.FindAsync(id);
                if (u == null) return NotFound(new { error = "Usuario no encontrado." });

                if (!string.IsNullOrWhiteSpace(request.Username)) u.Username = request.Username.Trim().ToLower();
                if (!string.IsNullOrWhiteSpace(request.Email)) u.Email = request.Email.Trim().ToLower();
                if (!string.IsNullOrWhiteSpace(request.Name)) u.Name = request.Name.Trim();
                if (!string.IsNullOrWhiteSpace(request.LastName)) u.LastName = request.LastName.Trim();
                if (!string.IsNullOrWhiteSpace(request.Password)) u.Password = request.Password;
                if (request.IsAdministrator.HasValue) u.IsAdministrator = request.IsAdministrator.Value;
                if (request.IsSuperUser.HasValue) u.IsSuperUser = request.IsSuperUser.Value;
                if (request.Activated.HasValue)
                {
                    u.Activated = request.Activated.Value;
                    if (u.Activated && u.ActivatedDate == null) u.ActivatedDate = DateTime.UtcNow;
                }
                if (request.Disabled.HasValue) u.Disabled = request.Disabled.Value;
                if (request.ChangePasswordRequired.HasValue) u.ChangePasswordRequired = request.ChangePasswordRequired.Value;

                u.UpdatedDate = DateTime.UtcNow;
                u.UpdatedUserId = 0;
                u.UpdatedUserName = "system";

                if (request.RolIds != null)
                {
                    await ReplaceRoles(u.Id, request.RolIds);
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    u.Id,
                    u.Username,
                    u.Name,
                    u.LastName,
                    u.Email,
                    u.IsAdministrator,
                    u.IsSuperUser,
                    u.Activated,
                    u.Disabled
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var u = await _context.Users.FindAsync(id);
                if (u == null) return NotFound(new { error = "Usuario no encontrado." });

                u.Disabled = true;
                u.UpdatedDate = DateTime.UtcNow;
                u.UpdatedUserId = 0;
                u.UpdatedUserName = "system";

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ── Sub-endpoints: membresía Usuario ↔ Rol ───────────────────

        [HttpGet("{usuarioId:int}/Roles")]
        public async Task<IActionResult> GetRoles(int usuarioId)
        {
            try
            {
                var items = await _context.UsuariosRoles
                    .Where(ur => ur.UsuarioId == usuarioId && !ur.Rol!.Disabled)
                    .Select(ur => new
                    {
                        ur.Rol!.Id,
                        ur.Rol.Codigo,
                        ur.Rol.Nombre,
                        ur.Rol.Descripcion
                    })
                    .OrderBy(r => r.Codigo)
                    .ToListAsync();

                return Ok(items);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{usuarioId:int}/Roles/{rolId:int}")]
        public async Task<IActionResult> AddRol(int usuarioId, int rolId)
        {
            try
            {
                var u = await _context.Users.FindAsync(usuarioId);
                if (u == null) return NotFound(new { error = "Usuario no encontrado." });
                if (u.Disabled) return BadRequest(new { error = "El usuario está deshabilitado." });

                var rol = await _context.Roles.FindAsync(rolId);
                if (rol == null) return NotFound(new { error = "Rol no encontrado." });
                if (rol.Disabled) return BadRequest(new { error = "El rol está deshabilitado." });

                var exists = await _context.UsuariosRoles.AnyAsync(x => x.UsuarioId == usuarioId && x.RolId == rolId);
                if (!exists)
                {
                    _context.UsuariosRoles.Add(new UsuarioRol
                    {
                        UsuarioId = usuarioId,
                        RolId = rolId,
                        CreatedUserId = 0,
                        CreatedUserName = "system",
                        CreatedDate = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync();
                }

                return Ok(new { usuarioId, rolId, added = !exists });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("{usuarioId:int}/Roles/{rolId:int}")]
        public async Task<IActionResult> RemoveRol(int usuarioId, int rolId)
        {
            try
            {
                var ur = await _context.UsuariosRoles
                    .FirstOrDefaultAsync(x => x.UsuarioId == usuarioId && x.RolId == rolId);

                if (ur == null) return NotFound(new { error = "El rol no está asignado a este usuario." });

                _context.UsuariosRoles.Remove(ur);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        private async Task ReplaceRoles(int usuarioId, List<int> rolIds)
        {
            var existentes = await _context.UsuariosRoles
                .Where(ur => ur.UsuarioId == usuarioId)
                .ToListAsync();
            _context.UsuariosRoles.RemoveRange(existentes);

            if (rolIds.Count == 0)
            {
                await _context.SaveChangesAsync();
                return;
            }

            var validos = await _context.Roles
                .Where(r => rolIds.Contains(r.Id) && !r.Disabled)
                .Select(r => r.Id)
                .ToListAsync();

            var now = DateTime.UtcNow;
            foreach (var rid in validos)
            {
                _context.UsuariosRoles.Add(new UsuarioRol
                {
                    UsuarioId = usuarioId,
                    RolId = rid,
                    CreatedUserId = 0,
                    CreatedUserName = "system",
                    CreatedDate = now
                });
            }

            await _context.SaveChangesAsync();
        }

        public class UpdateUsuarioRequest
        {
            public string? Username { get; set; }
            public string? Name { get; set; }
            public string? LastName { get; set; }
            public string? Email { get; set; }
            public string? Password { get; set; }
            public bool? IsAdministrator { get; set; }
            public bool? IsSuperUser { get; set; }
            public bool? Activated { get; set; }
            public bool? Disabled { get; set; }
            public bool? ChangePasswordRequired { get; set; }
            public List<int>? RolIds { get; set; }
        }
    }
}
