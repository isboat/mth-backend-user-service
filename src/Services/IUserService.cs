using UserService.Models;

namespace UserService.Services
{
    public interface IUserService
    {
        Task<UserDto?> GetUserByIdAsync(string userId);
        Task<UserDto?> GetOrCreateUserAsync(string privyUserId, Privy.PrivyUser privyUser);
        Task<UserDto> CreateUserAsync(CreateUserRequest request);
        Task<UserDto?> UpdateUserAsync(string userId, UpdateUserRequest request);
        Task<bool> DeleteUserAsync(string userId);
        Task<List<UserSearchResult>> SearchUsersAsync(string query, int limit = 10);
        Task<UserDto?> UpdateUserRoleAsync(string userId, UserRole role);
        Task<bool> VerifyWalletAsync(string userId, VerifyWalletRequest request);
    }
}
