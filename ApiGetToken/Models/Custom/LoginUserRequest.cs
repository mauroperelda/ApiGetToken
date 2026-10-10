namespace ApiGetToken.Models.Custom
{
    public class LoginUserRequest
    {
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}
