using ApiGetToken.Models.Custom;

namespace ApiGetToken.Helpers.GenerateToken
{
    public interface IGenerateToken
    {
        public string Generate(LoginUserResponse user);
    }
}
