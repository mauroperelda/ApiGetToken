using ApiGetToken.Models.Custom;

namespace ApiGetToken.Helpers.Authentication
{
  public interface IAuthenticateUser
  {
    public LoginUserResponse Authenticate(LoginUserRequest request);
  }
}
