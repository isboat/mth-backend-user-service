using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UserService.Models
{
    [BsonIgnoreExtraElements]
    public class UserModel
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("userId")]
        public string UserId { get; set; } = null!;

        [BsonElement("username")]
        public string Username { get; set; } = null!;

        [BsonElement("email")]
        public string? Email { get; set; }

        [BsonElement("walletAddress")]
        public string? WalletAddress { get; set; }

        [BsonElement("role")]
        public UserRole Role { get; set; } = UserRole.Authenticated;

        [BsonElement("profile")]
        public UserProfile? Profile { get; set; }

        [BsonElement("preferences")]
        public UserPreferences? Preferences { get; set; }

        [BsonElement("isVerified")]
        public bool IsVerified { get; set; } = false;

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class UserProfile
    {
        [BsonElement("bio")]
        public string? Bio { get; set; }

        [BsonElement("avatarUrl")]
        public string? AvatarUrl { get; set; }

        [BsonElement("socialLinks")]
        public List<string> SocialLinks { get; set; } = new();

        [BsonElement("reputationScore")]
        public int ReputationScore { get; set; } = 0;

        [BsonElement("badges")]
        public List<string> Badges { get; set; } = new();
    }

    public class UserPreferences
    {
        [BsonElement("notificationsEnabled")]
        public bool NotificationsEnabled { get; set; } = true;

        [BsonElement("theme")]
        public string Theme { get; set; } = "light";
    }

    public enum UserRole
    {
        Anonymous = 0,
        Authenticated = 1,
        Creator = 2,
        Collector = 3,
        Moderator = 4
    }
}
