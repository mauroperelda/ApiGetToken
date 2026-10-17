using ApiGetToken.DATA;
using ApiGetToken.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiGetToken.Controllers
{
    /// <summary>
    /// CRUD de <see cref="Permiso"/>. Listado paginado con filtro opcional
    /// por módulo. Los permisos son la unidad mínima de autorización: un
    /// permiso se asigna a un <see cref="Rol"/> vía la join RolPermiso
    /// (ver <c>RolesController</c>). Para que un usuario los reciba en el
    /// JWT debe estar asignado al rol que los contiene.
    /// </summary>
    [Route("api/Permisos")]
    [ApiController]
    [ApiGetToken.RequireAdministrator]
    public class PermisosController : ControllerBase
    {
        private readonly ManagerOdontosoftContext _context;

        public PermisosController(ManagerOdontosoftContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GET api/Permisos?modulo=&amp;page=&amp;pageSize=
        /// Lista permisos activos, ordenados por Modulo y luego por Codigo.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? modulo = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 200) pageSize = 20;

                var query = _context.Permisos
                    .Where(p => !p.Disabled);

                if (!string.IsNullOrWhiteSpace(modulo))
                {
                    var m = modulo.Trim();
                    query = query.Where(p => p.Modulo == m);
                }

                var total = await query.CountAsync();
                var items = await query
                    .OrderBy(p => p.Modulo)
                    .ThenBy(p => p.Codigo)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new
                    {
                        p.Id,
                        p.Codigo,
                        p.Modulo,
                        p.Nombre,
                        p.Descripcion,
                        p.Disabled
                    })
                    .ToListAsync();

                return Ok(new { total, page, pageSize, items });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>GET api/Permisos/{id}</summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var p = await _context.Permisos.FindAsync(id);
                if (p == null) return NotFound(new { error = "Permiso no encontrado." });

                return Ok(new
                {
                    p.Id,
                    p.Codigo,
                    p.Modulo,
                    p.Nombre,
                    p.Descripcion,
                    p.Disabled
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>POST api/Permisos</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PermisoRequest request)
        {
            try
            {
                if (request == null) return BadRequest(new { error = "Body requerido." });
                if (string.IsNullOrWhiteSpace(request.Codigo))
                    return BadRequest(new { error = "El campo 'codigo' es obligatorio." });
                if (string.IsNullOrWhiteSpace(request.Modulo))
                    return BadRequest(new { error = "El campo 'modulo' es obligatorio." });
                if (string.IsNullOrWhiteSpace(request.Nombre))
                    return BadRequest(new { error = "El campo 'nombre' es obligatorio." });

                var codigo = request.Codigo.Trim();
                if (await _context.Permisos.AnyAsync(x => x.Codigo == codigo && !x.Disabled))
                    return BadRequest(new { error = $"Ya existe un permiso con el código '{codigo}'." });

                var now = DateTime.UtcNow;
                var p = new Permiso
                {
                    Codigo = codigo,
                    Modulo = request.Modulo.Trim(),
                    Nombre = request.Nombre.Trim(),
                    Descripcion = string.IsNullOrWhiteSpace(request.Descripcion) ? null : request.Descripcion.Trim(),
                    CreatedUserId = 0,
                    CreatedUserName = "system",
                    CreatedDate = now,
                    Disabled = false
                };

                _context.Permisos.Add(p);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    p.Id,
                    p.Codigo,
                    p.Modulo,
                    p.Nombre,
                    p.Descripcion,
                    p.Disabled
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>PUT api/Permisos/{id}</summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] PermisoRequest request)
        {
            try
            {
                var p = await _context.Permisos.FindAsync(id);
                if (p == null) return NotFound(new { error = "Permiso no encontrado." });

                if (!string.IsNullOrWhiteSpace(request.Codigo)) p.Codigo = request.Codigo.Trim();
                if (!string.IsNullOrWhiteSpace(request.Modulo)) p.Modulo = request.Modulo.Trim();
                if (!string.IsNullOrWhiteSpace(request.Nombre)) p.Nombre = request.Nombre.Trim();
                if (request.Descripcion != null)
                    p.Descripcion = string.IsNullOrWhiteSpace(request.Descripcion) ? null : request.Descripcion.Trim();

                p.UpdatedDate = DateTime.UtcNow;
                p.UpdatedUserId = 0;
                p.UpdatedUserName = "system";

                await _context.SaveChangesAsync();
                return Ok(new
                {
                    p.Id,
                    p.Codigo,
                    p.Modulo,
                    p.Nombre,
                    p.Descripcion,
                    p.Disabled
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>DELETE api/Permisos/{id} — soft delete.</summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var p = await _context.Permisos.FindAsync(id);
                if (p == null) return NotFound(new { error = "Permiso no encontrado." });

                p.Disabled = true;
                p.UpdatedDate = DateTime.UtcNow;
                p.UpdatedUserId = 0;
                p.UpdatedUserName = "system";

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        public class PermisoRequest
        {
            public string? Codigo { get; set; }
            public string? Modulo { get; set; }
            public string? Nombre { get; set; }
            public string? Descripcion { get; set; }
        }
    }
}
