using ApiGetToken.Helpers.Authentication;
using ApiGetToken.Helpers.GenerateToken;
using ApiGetToken.Models.Custom;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ApiGetToken.Controllers
{
	[Route("api/Account")]
	[ApiController]
	public class AccountController : ControllerBase
	{
		private readonly IAuthenticateUser _authenticateUser;
		private readonly IUserCreator _userCreator;
		private readonly IGenerateToken _generateToken;

		public AccountController(
				IAuthenticateUser authenticateUser,
				IUserCreator userCreator,
				IGenerateToken generateToken)
		{
			_authenticateUser = authenticateUser;
			_userCreator = userCreator;
			_generateToken = generateToken;
		}

		/// <summary>
		/// POST: api/Account/Login
		/// </summary>
		[HttpPost]
		[Route("Login")]
		public IActionResult Login([FromBody] LoginUserRequest request)
		{
			try
			{
				var userResponse = _authenticateUser.Authenticate(request);
				if (userResponse == null)
				{
					return Ok(new LoginUserResponse { Result = false, Message = "Credenciales inválidas." });
				}
				userResponse.Token = _generateToken.Generate(userResponse);
				return Ok(userResponse);
			}
			catch (Exception ex)
			{
				return BadRequest(new LoginUserResponse { Result = false, Message = ex.Message });
			}
		}

		/// <summary>
		/// POST: api/Account/Register
		/// Crea un nuevo usuario. Si el registro es exitoso, devuelve un
		/// LoginUserResponse con el token JWT (logueo automático).
		/// </summary>
		[HttpPost]
		[Route("Register")]
		public IActionResult Register([FromBody] RegisterUserRequest request)
		{
			try
			{
				// El registro self-service delega la creación al helper
				// compartido. Si el body trae RolIds, se ignoran a propósito:
				// un usuario público no debe poder autoasignarse roles.
				var user = _userCreator.Create(request, "self-register");
				var userResponse = new LoginUserResponse
				{
					Id = user.Id,
					Username = user.Username,
					Password = user.Password,
					Name = user.Name,
					LastName = user.LastName,
					Email = user.Email,
					IsAdministrator = user.IsAdministrator,
					IsSuperUser = user.IsSuperUser,
					Disabled = user.Disabled,
					Activated = user.Activated,
					ActivatedDate = user.ActivatedDate,
					Result = true,
					Message = "Usuario registrado correctamente.",
				};
				userResponse.Token = _generateToken.Generate(userResponse);
				return Ok(userResponse);
			}
			catch (Exception ex)
			{
				return BadRequest(new LoginUserResponse { Result = false, Message = ex.Message });
			}
		}
	}
}
