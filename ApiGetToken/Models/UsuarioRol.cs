using System;
using System.Collections.Generic;

namespace ApiGetToken.Models;

/// <summary>
/// Tabla join N:M entre <see cref="User"/> y <see cref="Rol"/>. PK
/// compuesta (UsuarioId, RolId). Reemplaza la FK simple
/// <c>User.ProfileId</c> que existía con la vieja tabla Profile.
/// </summary>
public partial class UsuarioRol
{
    public int UsuarioId { get; set; }
    public int RolId { get; set; }
    public int CreatedUserId { get; set; }
    public string CreatedUserName { get; set; } = null!;
    public DateTime CreatedDate { get; set; }

    public virtual User? Usuario { get; set; }
    public virtual Rol? Rol { get; set; }
}
