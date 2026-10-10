using System.ComponentModel.DataAnnotations;

namespace ApiGetToken.Models.Custom
{
    public class RegisterUserRequest
    {
        [Required, StringLength(50, MinimumLength = 3)]
        [RegularExpression("^[a-zA-Z0-9._-]+$",
            ErrorMessage = "El usuario solo puede contener letras, números, . _ -")]
        public string Username { get; set; } = null!;

        [Required, StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = null!;

        [Required, StringLength(100, MinimumLength = 1)]
        public string LastName { get; set; } = null!;

        [Required, EmailAddress, StringLength(200)]
        public string Email { get; set; } = null!;

        [Required, StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = null!;

        public bool IsAdministrator { get; set; } = false;
        public bool IsSuperUser { get; set; } = false;
        public bool Activated { get; set; } = true;
        public bool Disabled { get; set; } = false;
        public bool ChangePasswordRequired { get; set; } = false;
        public List<int>? RolIds { get; set; }
    }
}
