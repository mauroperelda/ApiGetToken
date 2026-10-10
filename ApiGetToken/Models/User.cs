using System;
using System.Collections.Generic;

namespace ApiGetToken.Models;

public partial class User
{
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public bool IsAdministrator { get; set; }
    public bool IsSuperUser { get; set; }
    public DateTime? PasswordExpirationDate { get; set; }
    public bool Activated { get; set; }
    public DateTime? ActivatedDate { get; set; }
    public int CreatedUserId { get; set; }
    public string CreatedUserName { get; set; } = null!;
    public DateTime CreatedDate { get; set; }
    public int? UpdatedUserId { get; set; }
    public string? UpdatedUserName { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public bool Disabled { get; set; }
    public bool ChangePasswordRequired { get; set; }

    /// <summary>
    /// Roles asignados al usuario vía la tabla join <see cref="UsuarioRol"/>.
    /// La propiedad de navegación se llama <c>UsuariosRoles</c> para que EF
    /// la infiera automáticamente a partir del nombre de la entidad join.
    /// </summary>
    public virtual ICollection<UsuarioRol> UsuariosRoles { get; set; } = new List<UsuarioRol>();
}
