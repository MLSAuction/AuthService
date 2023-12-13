using Microsoft.AspNetCore.Mvc;
using AuthService.Repositories;
using AuthService.Models;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using VaultSharp.V1.Commons;

namespace AuthService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : Controller
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly IAuthRepository _repository;
        private readonly Secret<SecretData> _secret;

        public AuthController(ILogger<AuthController> logger, IConfiguration configuration, IAuthRepository repository, Secret<SecretData> secret)
        {
            _logger = logger;
            _configuration = configuration;
            _repository = repository;
            _secret = secret;
        }

        /// <summary>
        /// Login
        /// </summary>
        /// <param name="authDTO"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AuthDTO authDTO)
        {
            _logger.LogInformation("");

            string? loginResultJWT = await _repository.Login(authDTO);

            if (loginResultJWT != null)
            {
                return Ok(loginResultJWT);
            }
            else
            {
                return Unauthorized("Unauthorized");
            }
        }

        /// <summary>
        /// Register user
        /// </summary>
        /// <param name="userDTO"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserDTO userDTO)
        {
            _logger.LogInformation("");

            string? loginResultJWT = await _repository.Register(userDTO);

            if (loginResultJWT != null)
            {
                return Ok(loginResultJWT);
            }
            else
            {
                return BadRequest("Downstream issue");
            }
        }

        /// <summary>
        /// Validate the JWT (Returning username)
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost("validate")]
        public IActionResult ValidateToken([FromBody] string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secret.Data.Data["jwtSecret"].ToString()); //jwtIssuer før

            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false, // skal potentielt være true? (idk google det)
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var claim = jwtToken.Claims.First(t => t.Type == ClaimTypes.NameIdentifier).Value;

                return Ok(claim);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return BadRequest();
            }
        }
    }
}
