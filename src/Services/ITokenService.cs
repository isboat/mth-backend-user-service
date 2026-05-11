using UserService.Models;

namespace UserService.Services
{
    public interface ITokenService
    {
        string GenerateToken(string userId, UserRole role);
    }
}
