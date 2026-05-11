using MongoDB.Driver;
using UserService.Models;

namespace UserService.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<UserModel> _usersCollection;

        public UserRepository(IMongoDatabase database)
        {
            _database = database;
            _usersCollection = _database.GetCollection<UserModel>("Users");
            
            // Ensure indexes
            CreateIndexes();
        }

        private void CreateIndexes()
        {
            var userIdIndexModel = new CreateIndexModel<UserModel>(
                Builders<UserModel>.IndexKeys.Ascending(u => u.UserId),
                new CreateIndexOptions { Unique = true }
            );

            var usernameIndexModel = new CreateIndexModel<UserModel>(
                Builders<UserModel>.IndexKeys.Ascending(u => u.Username),
                new CreateIndexOptions { Unique = true }
            );

            var walletIndexModel = new CreateIndexModel<UserModel>(
                Builders<UserModel>.IndexKeys.Ascending(u => u.WalletAddress),
                new CreateIndexOptions { Unique = true, Sparse = true }
            );

            var roleIndexModel = new CreateIndexModel<UserModel>(
                Builders<UserModel>.IndexKeys.Ascending(u => u.Role)
            );

            try
            {
                _usersCollection.Indexes.CreateMany(new[] 
                { 
                    userIdIndexModel, 
                    usernameIndexModel, 
                    walletIndexModel, 
                    roleIndexModel 
                });
            }
            catch (MongoCommandException ex) when (ex.Code == 48)
            {
                // Index already exists
            }
        }

        public async Task<UserModel?> GetUserByIdAsync(string userId)
        {
            return await _usersCollection.Find(u => u.UserId == userId).FirstOrDefaultAsync();
        }

        public async Task<UserModel?> GetUserByUsernameAsync(string username)
        {
            return await _usersCollection.Find(u => u.Username == username).FirstOrDefaultAsync();
        }

        public async Task<UserModel?> GetUserByWalletAsync(string walletAddress)
        {
            return await _usersCollection.Find(u => u.WalletAddress == walletAddress).FirstOrDefaultAsync();
        }

        public async Task<UserModel> CreateUserAsync(UserModel user)
        {
            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _usersCollection.InsertOneAsync(user);
            return user;
        }

        public async Task<UserModel?> UpdateUserAsync(string userId, UserModel user)
        {
            user.UpdatedAt = DateTime.UtcNow;
            var result = await _usersCollection.ReplaceOneAsync(
                u => u.UserId == userId,
                user
            );
            return result.ModifiedCount > 0 ? user : null;
        }

        public async Task<bool> DeleteUserAsync(string userId)
        {
            var result = await _usersCollection.DeleteOneAsync(u => u.UserId == userId);
            return result.DeletedCount > 0;
        }

        public async Task<List<UserSearchResult>> SearchUsersAsync(string query, int limit = 10)
        {
            var filter = Builders<UserModel>.Filter.Or(
                Builders<UserModel>.Filter.Regex(u => u.Username, new MongoDB.Bson.BsonRegularExpression(query, "i")),
                Builders<UserModel>.Filter.Regex(u => u.WalletAddress, new MongoDB.Bson.BsonRegularExpression(query, "i"))
            );

            var users = await _usersCollection
                .Find(filter)
                .Limit(limit)
                .ToListAsync();

            return users.Select(u => new UserSearchResult
            {
                UserId = u.UserId,
                Username = u.Username,
                WalletAddress = u.WalletAddress,
                Role = u.Role,
                Profile = u.Profile,
                IsVerified = u.IsVerified
            }).ToList();
        }

        public async Task<bool> UserExistsAsync(string userId)
        {
            return await _usersCollection.Find(u => u.UserId == userId).AnyAsync();
        }
    }
}
