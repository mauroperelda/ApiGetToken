using System;
using System.Collections.Generic;

namespace ApiGetToken.Models;

/// <summary>
/// Tabla join N:M entre <see cref="Rol"/> y <see cref="Permiso"/>.
/// PK compuesta (RolId, PermisoId). Se permite navegación a Rol y Permiso
/// para que EF pueda hidratar el grafo. Las FKs se setean con
/// <c>OnDelete(DeleteBehavior.Cascade)</c> en el DbContext.
/// </summary>
public partial class RolPermiso
{
    public int RolId { get; set; }
    public int PermisoId { get; set; }
    public int CreatedUserId { get; set; }
    public string CreatedUserName { get; set; } = null!;
    public DateTime CreatedDate { get; set; }

    public virtual Rol? Rol { get; set; }
    public virtual Permiso? Permiso { get; set; }
}
