namespace UserService.Models
{
    public class UserDto
    {
        public string UserId { get; set; } = null!;
        public string Username { get; set; } = null!;
        public string? Email { get; set; }
        public string? WalletAddress { get; set; }
        public UserRole Role { get; set; }
        public UserProfile? Profile { get; set; }
        public UserPreferences? Preferences { get; set; }
        public bool IsVerified { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateUserRequest
    {
        public string UserId { get; set; } = null!;
        public string Username { get; set; } = null!;
        public string? Email { get; set; }
        public string? WalletAddress { get; set; }
    }

    public class UpdateUserRequest
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public UserProfile? Profile { get; set; }
        public UserPreferences? Preferences { get; set; }
    }

    public class ExchangeTokenRequest
    {
        public string PrivyToken { get; set; } = null!;
        public string PrivyUserId { get; set; } = null!;
    }

    public class ExchangeTokenResponse
    {
        public string JwtToken { get; set; } = null!;
        public UserDto User { get; set; } = null!;
    }

    public class UpdateRoleRequest
    {
        public UserRole Role { get; set; }
    }

    public class VerifyWalletRequest
    {
        public string Signature { get; set; } = null!;
        public string Message { get; set; } = null!;
    }

    public class UserSearchResult
    {
        public string UserId { get; set; } = null!;
        public string Username { get; set; } = null!;
        public string? WalletAddress { get; set; }
        public UserRole Role { get; set; }
        public UserProfile? Profile { get; set; }
        public bool IsVerified { get; set; }
    }
}
