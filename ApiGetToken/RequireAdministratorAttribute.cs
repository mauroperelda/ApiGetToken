using Microsoft.AspNetCore.Authorization;

namespace ApiGetToken;

/// <summary>
/// Equivalente a [Authorize(Roles = "Administrador")] pero chequea el claim
/// custom "administrator" (boolean como string) que emite GenerateToken en
/// lugar de ClaimTypes.Role. Necesario porque .NET solo mapea automáticamente
/// el claim "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
/// no un claim custom "Rol" o "administrator".
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class RequireAdministratorAttribute : AuthorizeAttribute
{
  public RequireAdministratorAttribute() : base(policy: "RequireAdministrator") { }
}
