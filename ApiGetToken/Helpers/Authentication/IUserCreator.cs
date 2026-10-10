using ApiGetToken.Models;
using ApiGetToken.Models.Custom;

namespace ApiGetToken.Helpers.Authentication
{
    /// <summary>
    /// Lógica core para crear un <see cref="User"/>. La usan tanto el
    /// registro self-service (<c>POST /api/Account/Register</c>, con
    /// auto-login) como el alta administrativa
    /// (<c>POST /api/Usuarios</c>, con asignación de roles).
    /// Centraliza: validación de unicidad, normalización de strings,
    /// armado del entity y campos de auditoría.
    /// </summary>
    public interface IUserCreator
    {
        /// <summary>
        /// Crea el usuario y lo persiste. Lanza <see cref="Exception"/>
        /// con mensaje en español si el username o el email ya existen.
        /// La contraseña se guarda tal cual llega (deuda: hashing).
        /// </summary>
        /// <param name="request">Body del registro.</param>
        /// <param name="createdBy">
        /// Nombre del sistema que crea (ej. "self-register" o
        /// "admin" según de dónde se llame). Sólo se loguea en
        /// auditoría.
        /// </param>
        User Create(RegisterUserRequest request, string createdBy);
    }
}
