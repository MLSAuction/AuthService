using Microsoft.AspNetCore.Mvc;
using AuthService.Repositories;

namespace AuthService.Controllers
{
    public class AuthController : Controller
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly AuthRepository _repository;

        public AuthController (ILogger logger, IConfiguration configuration, AuthRepository repository)
        {
            _logger = logger;
            _configuration = configuration;
            _repository = repository;
        }
    }
}
