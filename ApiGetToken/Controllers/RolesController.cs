using ApiGetToken.DATA;
using ApiGetToken.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiGetToken.Controllers
{
    /// <summary>
    /// CRUD de <see cref="Rol"/> + sub-endpoints para asignar/quitar
    /// permisos a un rol (vía la join <c>RolPermiso</c>). Cuando se
    /// modifican los permisos de un rol, los JWTs ya emitidos no se
    /// invalidan: el usuario debe re-loguear para que el
    /// <c>GenerateToken</c> re-arme los claims con la membresía
    /// actualizada.
    /// </summary>
    [Route("api/Roles")]
    [ApiController]
    [ApiGetToken.RequireAdministrator]
    public class RolesController : ControllerBase
    {
        private readonly ManagerOdontosoftContext _context;

        public RolesController(ManagerOdontosoftContext context)
        {
            _context = context;
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

                var query = _context.Roles.Where(r => !r.Disabled);

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    var s = $"%{searchText.Trim()}%";
                    query = query.Where(r =>
                        EF.Functions.Like(r.Codigo, s) ||
                        EF.Functions.Like(r.Nombre, s) ||
                        (r.Descripcion != null && EF.Functions.Like(r.Descripcion, s)));
                }

                var total = await query.CountAsync();
                var items = await query
                    .OrderBy(r => r.Codigo)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new
                    {
                        r.Id,
                        r.Codigo,
                        r.Nombre,
                        r.Descripcion,
                        r.Disabled
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
                var rol = await _context.Roles
                    .Where(r => r.Id == id)
                    .Select(r => new
                    {
                        r.Id,
                        r.Codigo,
                        r.Nombre,
                        r.Descripcion,
                        r.Disabled,
                        Permisos = r.RolesPermisos
                            .Where(rp => !rp.Permiso!.Disabled)
                            .Select(rp => new
                            {
                                rp.Permiso!.Id,
                                rp.Permiso.Codigo,
                                rp.Permiso.Modulo,
                                rp.Permiso.Nombre
                            })
                            .OrderBy(p => p.Codigo)
                            .ToList()
                    })
                    .FirstOrDefaultAsync();

                if (rol == null) return NotFound(new { error = "Rol no encontrado." });
                return Ok(rol);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RolRequest request)
        {
            try
            {
                if (request == null) return BadRequest(new { error = "Body requerido." });
                if (string.IsNullOrWhiteSpace(request.Codigo))
                    return BadRequest(new { error = "El campo 'codigo' es obligatorio." });
                if (string.IsNullOrWhiteSpace(request.Nombre))
                    return BadRequest(new { error = "El campo 'nombre' es obligatorio." });

                var codigo = request.Codigo.Trim();
                if (await _context.Roles.AnyAsync(x => x.Codigo == codigo && !x.Disabled))
                    return BadRequest(new { error = $"Ya existe un rol con el código '{codigo}'." });

                var now = DateTime.UtcNow;
                var rol = new Rol
                {
                    Codigo = codigo,
                    Nombre = request.Nombre.Trim(),
                    Descripcion = string.IsNullOrWhiteSpace(request.Descripcion) ? null : request.Descripcion.Trim(),
                    CreatedUserId = 0,
                    CreatedUserName = "system",
                    CreatedDate = now,
                    Disabled = false
                };

                _context.Roles.Add(rol);
                await _context.SaveChangesAsync();

                if (request.PermisoIds != null && request.PermisoIds.Count > 0)
                {
                    await ReplacePermisos(rol.Id, request.PermisoIds);
                }

                return Ok(new
                {
                    rol.Id,
                    rol.Codigo,
                    rol.Nombre,
                    rol.Descripcion,
                    rol.Disabled
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] RolRequest request)
        {
            try
            {
                var rol = await _context.Roles.FindAsync(id);
                if (rol == null) return NotFound(new { error = "Rol no encontrado." });

                if (!string.IsNullOrWhiteSpace(request.Codigo)) rol.Codigo = request.Codigo.Trim();
                if (!string.IsNullOrWhiteSpace(request.Nombre)) rol.Nombre = request.Nombre.Trim();
                if (request.Descripcion != null)
                    rol.Descripcion = string.IsNullOrWhiteSpace(request.Descripcion) ? null : request.Descripcion.Trim();

                rol.UpdatedDate = DateTime.UtcNow;
                rol.UpdatedUserId = 0;
                rol.UpdatedUserName = "system";

                if (request.PermisoIds != null)
                {
                    await ReplacePermisos(rol.Id, request.PermisoIds);
                }

                await _context.SaveChangesAsync();
                return Ok(new
                {
                    rol.Id,
                    rol.Codigo,
                    rol.Nombre,
                    rol.Descripcion,
                    rol.Disabled
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
                var rol = await _context.Roles.FindAsync(id);
                if (rol == null) return NotFound(new { error = "Rol no encontrado." });

                rol.Disabled = true;
                rol.UpdatedDate = DateTime.UtcNow;
                rol.UpdatedUserId = 0;
                rol.UpdatedUserName = "system";

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ── Sub-endpoints: membresía Rol ↔ Permiso ──────────────────

        [HttpGet("{rolId:int}/Permisos")]
        public async Task<IActionResult> GetPermisos(int rolId)
        {
            try
            {
                var items = await _context.RolesPermisos
                    .Where(rp => rp.RolId == rolId && !rp.Permiso!.Disabled)
                    .Select(rp => new
                    {
                        rp.Permiso!.Id,
                        rp.Permiso.Codigo,
                        rp.Permiso.Modulo,
                        rp.Permiso.Nombre
                    })
                    .OrderBy(p => p.Codigo)
                    .ToListAsync();

                return Ok(items);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{rolId:int}/Permisos/{permisoId:int}")]
        public async Task<IActionResult> AddPermiso(int rolId, int permisoId)
        {
            try
            {
                var rol = await _context.Roles.FindAsync(rolId);
                if (rol == null) return NotFound(new { error = "Rol no encontrado." });
                if (rol.Disabled) return BadRequest(new { error = "El rol está deshabilitado." });

                var permiso = await _context.Permisos.FindAsync(permisoId);
                if (permiso == null) return NotFound(new { error = "Permiso no encontrado." });
                if (permiso.Disabled) return BadRequest(new { error = "El permiso está deshabilitado." });

                // Idempotente: si ya existe, no falla
                var exists = await _context.RolesPermisos.AnyAsync(x => x.RolId == rolId && x.PermisoId == permisoId);
                if (!exists)
                {
                    _context.RolesPermisos.Add(new RolPermiso
                    {
                        RolId = rolId,
                        PermisoId = permisoId,
                        CreatedUserId = 0,
                        CreatedUserName = "system",
                        CreatedDate = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync();
                }

                return Ok(new { rolId, permisoId, added = !exists });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("{rolId:int}/Permisos/{permisoId:int}")]
        public async Task<IActionResult> RemovePermiso(int rolId, int permisoId)
        {
            try
            {
                var rp = await _context.RolesPermisos
                    .FirstOrDefaultAsync(x => x.RolId == rolId && x.PermisoId == permisoId);

                if (rp == null) return NotFound(new { error = "El permiso no está asignado a este rol." });

                _context.RolesPermisos.Remove(rp);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        private async Task ReplacePermisos(int rolId, List<int> permisoIds)
        {
            // Borra membresía actual e inserta la nueva. Los IDs que no
            // existan o estén deshabilitados se ignoran silenciosamente
            // (el caller verá solo los que efectivamente quedaron).
            var existentes = await _context.RolesPermisos
                .Where(rp => rp.RolId == rolId)
                .ToListAsync();
            _context.RolesPermisos.RemoveRange(existentes);

            if (permisoIds.Count == 0)
            {
                await _context.SaveChangesAsync();
                return;
            }

            var validos = await _context.Permisos
                .Where(p => permisoIds.Contains(p.Id) && !p.Disabled)
                .Select(p => p.Id)
                .ToListAsync();

            var now = DateTime.UtcNow;
            foreach (var pid in validos)
            {
                _context.RolesPermisos.Add(new RolPermiso
                {
                    RolId = rolId,
                    PermisoId = pid,
                    CreatedUserId = 0,
                    CreatedUserName = "system",
                    CreatedDate = now
                });
            }

            await _context.SaveChangesAsync();
        }

        public class RolRequest
        {
            public string? Codigo { get; set; }
            public string? Nombre { get; set; }
            public string? Descripcion { get; set; }
            /// <summary>
            /// Si viene, reemplaza la membresía actual del rol por la
            /// lista. Si es null, la membresía no se toca (útil en Update
            /// cuando sólo querés cambiar datos del rol).
            /// </summary>
            public List<int>? PermisoIds { get; set; }
        }
    }
}
