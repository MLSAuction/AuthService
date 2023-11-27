namespace AuthService.Repositories
{
    public class AuthRepository
    {
        private readonly ILogger<AuthRepository> _logger;
        private readonly IConfiguration _configuration;

        public AuthRepository (ILogger<AuthRepository> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }
    }
}
