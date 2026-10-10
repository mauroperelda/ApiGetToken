using ApiGetToken.DATA;
using ApiGetToken.Models.Custom;

namespace ApiGetToken.Helpers.Authentication
{
    public class AuthenticateUser : IAuthenticateUser
    {
        private readonly ManagerOdontosoftContext _managerOdontosoftContext;
        private readonly IConfiguration _configuration;

        public AuthenticateUser(ManagerOdontosoftContext managerOdontosoftContext, IConfiguration configuration)
        {
            _managerOdontosoftContext = managerOdontosoftContext;
            _configuration = configuration;
        }

        public LoginUserResponse Authenticate(LoginUserRequest request)
        {
            try
            {
                // Identificador: username o email. Normalizamos a minúsculas
                // para que el match sea case-insensitive (la columna Username
                // se guarda en minúsculas desde IUserCreator).
                var identifier = (request.Username ?? string.Empty).Trim().ToLower();

                // La password NO se normaliza: se compara contra el hash
                // BCrypt tal cual llega. Forzar ToLower acá sería un bug de
                // seguridad (las passwords son case-sensitive).
                var password = request.Password ?? string.Empty;

                var user = _managerOdontosoftContext.Users.FirstOrDefault(x =>
                    (x.Username == identifier || x.Email == identifier) &&
                    !x.Disabled);

                if (user == null)
                    throw new Exception("Usuario o contraseña incorrectos.");

                if (!VerifyAndMigratePassword(user, password))
                    throw new Exception("Usuario o contraseña incorrectos.");

                if (!user.Activated)
                    throw new Exception("El usuario aún no fue activado.");

                return new LoginUserResponse
                {
                    Username = user.Username,
                    Password = user.Password,
                    Id = user.Id,
                    Email = user.Email,
                    Name = user.Name,
                    LastName = user.LastName,
                    IsAdministrator = user.IsAdministrator,
                    IsSuperUser = user.IsSuperUser,
                    Disabled = user.Disabled,
                    Activated = user.Activated,
                    ActivatedDate = user.ActivatedDate,
                    ChangePasswordRequired = user.ChangePasswordRequired,
                    Result = true,
                    Message = "OK",
                };
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// Verifica la password contra el hash almacenado. Si el hash
        /// almacenado NO es un hash BCrypt (no empieza con <c>$2</c>), se
        /// trata de una password plana legacy: si matchea exacto, se
        /// re-hashea y persiste en el mismo login (lazy upgrade) para
        /// que el próximo login ya vaya por el camino BCrypt.
        /// </summary>
        private bool VerifyAndMigratePassword(Models.User user, string submittedPassword)
        {
            var stored = user.Password;

            // Hash BCrypt siempre empieza con "$2a$", "$2b$" o "$2y$".
            if (stored.StartsWith("$2", StringComparison.Ordinal))
            {
                return BCrypt.Net.BCrypt.Verify(submittedPassword, stored);
            }

            // Legacy: password en plano. Si matchea, migrar a BCrypt
            // antes de devolver true.
            if (!string.Equals(stored, submittedPassword, StringComparison.Ordinal))
                return false;

            user.Password = BCrypt.Net.BCrypt.HashPassword(submittedPassword, workFactor: 11);
            user.UpdatedDate = DateTime.UtcNow;
            user.UpdatedUserId = 0;
            user.UpdatedUserName = "lazy-upgrade";
            _managerOdontosoftContext.SaveChanges();
            return true;
        }
    }
}
