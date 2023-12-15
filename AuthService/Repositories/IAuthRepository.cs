using AuthService.Models;

namespace AuthService.Repositories
{
    public interface IAuthRepository
    {
        Task<string?> Login(AuthDTO authDTO);
        Task<string?> Register(UserDTO userDTO);
    }
}
