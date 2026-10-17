using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ApiGetToken;

/// <summary>
/// Marca con el security requirement "Bearer" SOLO las operaciones que
/// en runtime requieren [Authorize] (o cualquier derivado como [Authorize(Roles=...)]
/// o [Authorize(Policy=...)]). Las públicas (Login, Register, etc.) quedan
/// sin candado para que Swagger UI no pida un token que el propio endpoint
/// no exige.
/// </summary>
/// <remarks>
/// El chequeo es por action: si CUALQUIER method-level [Authorize] está presente
/// o el controller tiene [Authorize] a nivel de clase Y la action no tiene
/// [AllowAnonymous], entonces se agrega el requisito. Esto refleja 1:1 el
/// comportamiento real del middleware de autenticación.
/// </remarks>
public class AuthOperationFilter : IOperationFilter
{
  public void Apply(OpenApiOperation operation, OperationFilterContext context)
  {
    var actionAttrs = context.MethodInfo.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true);
    var controllerAttrs = context.MethodInfo.DeclaringType?.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true);
    var allowAnonymousAttrs = context.MethodInfo.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true);

    bool requiresAuth =
      (actionAttrs.Length > 0) ||
      ((controllerAttrs?.Length ?? 0) > 0 && allowAnonymousAttrs.Length == 0);

    if (!requiresAuth) return;

    // En Microsoft.OpenApi 2.x, las referencias a security schemes se construyen
    // con OpenApiSecuritySchemeReference (que hereda de OpenApiSecurityScheme) y
    // se le pasan directamente al requirement. El ReferenceType va implícito.
    operation.Security ??= new List<OpenApiSecurityRequirement>();
    operation.Security.Add(new OpenApiSecurityRequirement
    {
      {
        new OpenApiSecuritySchemeReference("Bearer", context.Document),
        new List<string>()
      },
    });
  }
}
