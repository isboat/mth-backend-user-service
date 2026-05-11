using UserService.Models;
using UserService.Repositories;
using UserService.Events;

namespace UserService.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserEventPublisher _eventPublisher;
        private readonly ILogger<UserService> _logger;

        public UserService(IUserRepository userRepository, IUserEventPublisher eventPublisher, ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _eventPublisher = eventPublisher;
            _logger = logger;
        }

        public async Task<UserDto?> GetUserByIdAsync(string userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            return user != null ? MapToDto(user) : null;
        }

        public async Task<UserDto?> GetOrCreateUserAsync(string privyUserId, Privy.PrivyUser privyUser)
        {
            var existingUser = await _userRepository.GetUserByIdAsync(privyUserId);
            
            if (existingUser != null)
            {
                return MapToDto(existingUser);
            }

            // Create new user from Privy data
            var newUser = new UserModel
            {
                UserId = privyUserId,
                Username = privyUser.Email ?? $"user_{privyUserId[..8]}",
                Email = privyUser.Email,
                WalletAddress = privyUser.Wallets?.FirstOrDefault()?.Address,
                Role = UserRole.Authenticated,
                Profile = new UserProfile(),
                Preferences = new UserPreferences()
            };

            var createdUser = await _userRepository.CreateUserAsync(newUser);
            _logger.LogInformation($"Created new user: {createdUser.UserId}");

            // Publish user created event
            await _eventPublisher.PublishUserCreatedEventAsync(MapToDto(createdUser));

            return MapToDto(createdUser);
        }

        public async Task<UserDto> CreateUserAsync(CreateUserRequest request)
        {
            // Check if user already exists
            if (await _userRepository.UserExistsAsync(request.UserId))
            {
                throw new InvalidOperationException($"User {request.UserId} already exists");
            }

            var user = new UserModel
            {
                UserId = request.UserId,
                Username = request.Username,
                Email = request.Email,
                WalletAddress = request.WalletAddress,
                Role = UserRole.Authenticated,
                Profile = new UserProfile(),
                Preferences = new UserPreferences()
            };

            var createdUser = await _userRepository.CreateUserAsync(user);
            _logger.LogInformation($"Created user: {createdUser.UserId}");

            // Publish event
            await _eventPublisher.PublishUserCreatedEventAsync(MapToDto(createdUser));

            return MapToDto(createdUser);
        }

        public async Task<UserDto?> UpdateUserAsync(string userId, UpdateUserRequest request)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(request.Username))
                user.Username = request.Username;
            
            if (!string.IsNullOrEmpty(request.Email))
                user.Email = request.Email;
            
            if (request.Profile != null)
                user.Profile = request.Profile;
            
            if (request.Preferences != null)
                user.Preferences = request.Preferences;

            var updatedUser = await _userRepository.UpdateUserAsync(userId, user);
            _logger.LogInformation($"Updated user: {userId}");

            // Publish event
            if (updatedUser != null)
            {
                await _eventPublisher.PublishUserUpdatedEventAsync(MapToDto(updatedUser));
            }

            return updatedUser != null ? MapToDto(updatedUser) : null;
        }

        public async Task<bool> DeleteUserAsync(string userId)
        {
            var result = await _userRepository.DeleteUserAsync(userId);
            if (result)
            {
                _logger.LogInformation($"Deleted user: {userId}");
                await _eventPublisher.PublishUserDeletedEventAsync(userId);
            }
            return result;
        }

        public async Task<List<UserSearchResult>> SearchUsersAsync(string query, int limit = 10)
        {
            return await _userRepository.SearchUsersAsync(query, limit);
        }

        public async Task<UserDto?> UpdateUserRoleAsync(string userId, UserRole role)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return null;
            }

            user.Role = role;
            var updatedUser = await _userRepository.UpdateUserAsync(userId, user);
            _logger.LogInformation($"Updated user role: {userId} -> {role}");

            if (updatedUser != null)
            {
                await _eventPublisher.PublishUserUpdatedEventAsync(MapToDto(updatedUser));
            }

            return updatedUser != null ? MapToDto(updatedUser) : null;
        }

        public async Task<bool> VerifyWalletAsync(string userId, VerifyWalletRequest request)
        {
            // TODO: Implement wallet verification logic (signature verification, on-chain check)
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            user.IsVerified = true;
            var updatedUser = await _userRepository.UpdateUserAsync(userId, user);
            
            if (updatedUser != null)
            {
                await _eventPublisher.PublishUserUpdatedEventAsync(MapToDto(updatedUser));
                return true;
            }

            return false;
        }

        private UserDto MapToDto(UserModel user)
        {
            return new UserDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                WalletAddress = user.WalletAddress,
                Role = user.Role,
                Profile = user.Profile,
                Preferences = user.Preferences,
                IsVerified = user.IsVerified,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }
    }
}
