namespace ApiGetToken.Models.Custom
{
    public class LoginUserResponse
    {
        public int Id { get; set; }
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public bool IsAdministrator { get; set; }
        public bool IsSuperUser { get; set; }
        public bool Disabled { get; set; }
        public bool ChangePasswordRequired { get; set; }
        public bool Activated { get; set; }
        public DateTime? ActivatedDate { get; set; }
        public string Token { get; set; } = string.Empty;
        public bool Result { get; set; }
        public string Message { get; set; } = null!;

    }
}
