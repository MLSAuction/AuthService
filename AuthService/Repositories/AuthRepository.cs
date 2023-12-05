using AuthService.Models;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using VaultSharp.V1.SecretsEngines.Transit;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace AuthService.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly ILogger<AuthRepository> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _userService;

        public AuthRepository (ILogger<AuthRepository> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;

            _userService = new HttpClient();
            _userService.BaseAddress = new Uri(_configuration["UserService"]);
        }

        public async Task<string?> Login(AuthDTO authDTO)
        {
            authDTO.Password = HashPassword(authDTO.Password);

            var requestBody = new StringContent(JsonSerializer.Serialize(authDTO), Encoding.UTF8, "application/json");

            var response = await _userService.PostAsync("/login", requestBody);

            if (response.IsSuccessStatusCode)
            {
                return GenerateJwt(authDTO.Username);
            }
            else
            {
                return null;
            }
        }

        public async Task<string?> Register(UserDTO userDTO)
        {
            userDTO.Password = HashPassword(userDTO.Password);

            var requestBody = new StringContent(JsonSerializer.Serialize(userDTO), Encoding.UTF8, "application/json");

            //post new user to userservice
            var response = await _userService.PostAsync("", requestBody);

            if (response.IsSuccessStatusCode)
            {
                return GenerateJwt(userDTO.Username);
            }
            else
            {
                return null;
            }
        }

        private string HashPassword(string password, string? salt = null)
        {
            if (salt == null)
            {
                salt = "REPLACE THIS WITH SALT FROM VAULT"; // <---
            }
            
            byte[] saltByteArray = Encoding.ASCII.GetBytes(salt);

            string hashedPassword = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password!,
                salt: saltByteArray,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8));

            return hashedPassword;
        }

        private string GenerateJwt(string username)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Secret"])); // skal ændres til vault
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, username)
            };

            var token = new JwtSecurityToken(_configuration["Issuer"],
                                             "http://localhost", //skal ændres til at trække fra nginx
                                             claims,
                                             expires: DateTime.Now.AddHours(1),
                                             signingCredentials: credentials);
            
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
