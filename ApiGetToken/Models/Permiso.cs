using System;
using System.Collections.Generic;

namespace ApiGetToken.Models;

public partial class Permiso
{
    public int Id { get; set; }
    public string Codigo { get; set; } = null!;
    public string Modulo { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public int CreatedUserId { get; set; }
    public string CreatedUserName { get; set; } = null!;
    public DateTime CreatedDate { get; set; }
    public int? UpdatedUserId { get; set; }
    public string? UpdatedUserName { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public bool Disabled { get; set; }

    public virtual ICollection<RolPermiso> RolesPermisos { get; set; } = new List<RolPermiso>();
}
