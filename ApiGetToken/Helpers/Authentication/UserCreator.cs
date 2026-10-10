using ApiGetToken.DATA;
using ApiGetToken.Models;
using ApiGetToken.Models.Custom;
using Microsoft.EntityFrameworkCore;

namespace ApiGetToken.Helpers.Authentication
{
    public class UserCreator : IUserCreator
    {
        private readonly ManagerOdontosoftContext _context;

        public UserCreator(ManagerOdontosoftContext context)
        {
            _context = context;
        }

        public User Create(RegisterUserRequest request, string createdBy)
        {
            var username = request.Username.Trim().ToLower();
            var email = request.Email.Trim().ToLower();

            // Validar unicidad (case-insensitive gracias al .ToLower)
            if (_context.Users.Any(u => u.Username == username))
                throw new Exception("El nombre de usuario ya está registrado.");
            if (_context.Users.Any(u => u.Email == email))
                throw new Exception("El email ya está registrado.");

            var now = DateTime.UtcNow;
            var user = new User
            {
                Username = username,
                // BCrypt con workFactor=11. Cada hash es único (salt aleatorio),
                // por lo que dos usuarios con la misma password tienen hashes
                // distintos. El login usa BCrypt.Verify, así que el salt vive
                // embebido en el propio string del hash.
                Password = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 11),
                Name = request.Name?.Trim() ?? string.Empty,
                LastName = request.LastName?.Trim() ?? string.Empty,
                Email = email,
                IsAdministrator = request.IsAdministrator,
                IsSuperUser = request.IsSuperUser,
                Activated = request.Activated,
                ActivatedDate = request.Activated ? now : (DateTime?)null,
                Disabled = request.Disabled,
                ChangePasswordRequired = request.ChangePasswordRequired,
                CreatedUserId = 0,
                CreatedUserName = createdBy,
                CreatedDate = now,
            };

            _context.Users.Add(user);
            _context.SaveChanges();
            return user;
        }
    }
}
