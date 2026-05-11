using UserService.Models;

namespace UserService.Repositories
{
    public interface IUserRepository
    {
        Task<UserModel?> GetUserByIdAsync(string userId);
        Task<UserModel?> GetUserByUsernameAsync(string username);
        Task<UserModel?> GetUserByWalletAsync(string walletAddress);
        Task<UserModel> CreateUserAsync(UserModel user);
        Task<UserModel?> UpdateUserAsync(string userId, UserModel user);
        Task<bool> DeleteUserAsync(string userId);
        Task<List<UserSearchResult>> SearchUsersAsync(string query, int limit = 10);
        Task<bool> UserExistsAsync(string userId);
    }
}
