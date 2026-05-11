using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Models;
using UserService.Services;
using UserService.Services.Privy;

namespace UserService.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ITokenService _tokenService;
        private readonly IPrivyService _privyService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            IUserService userService,
            ITokenService tokenService,
            IPrivyService privyService,
            ILogger<UsersController> logger)
        {
            _userService = userService;
            _tokenService = tokenService;
            _privyService = privyService;
            _logger = logger;
        }

        /// <summary>
        /// Exchange Privy token for backend JWT
        /// </summary>
        [HttpPost("auth/exchange")]
        public async Task<IActionResult> ExchangeToken([FromBody] ExchangeTokenRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.PrivyToken))
                {
                    return BadRequest(new { message = "Privy token is required" });
                }

                // Verify Privy token
                var privyUser = await _privyService.VerifyTokenAsync(request.PrivyToken, request.PrivyUserId);

                // Get or create user in MongoDB
                var user = await _userService.GetOrCreateUserAsync(privyUser.Id, privyUser);

                if (user == null)
                {
                    return StatusCode(500, new { message = "Failed to create or retrieve user" });
                }

                // Issue JWT token
                var jwtToken = _tokenService.GenerateToken(user.UserId, user.Role);

                return Ok(new ExchangeTokenResponse
                {
                    JwtToken = jwtToken,
                    User = user
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning($"Unauthorized token exchange: {ex.Message}");
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error exchanging token: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get user profile
        /// </summary>
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUser(string userId)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = $"User {userId} not found" });
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting user {userId}: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Create new user
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.Username))
                {
                    return BadRequest(new { message = "UserId and Username are required" });
                }

                var user = await _userService.CreateUserAsync(request);
                return CreatedAtAction(nameof(GetUser), new { userId = user.UserId }, user);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating user: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Update user profile
        /// </summary>
        [HttpPut("{userId}")]
        [Authorize]
        public async Task<IActionResult> UpdateUser(string userId, [FromBody] UpdateUserRequest request)
        {
            try
            {
                // Verify that the user is updating their own profile or is an admin
                var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (currentUserId != userId && !User.IsInRole("Moderator"))
                {
                    return Forbid();
                }

                var user = await _userService.UpdateUserAsync(userId, request);
                if (user == null)
                {
                    return NotFound(new { message = $"User {userId} not found" });
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating user {userId}: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Delete user account
        /// </summary>
        [HttpDelete("{userId}")]
        [Authorize]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            try
            {
                // Verify that the user is deleting their own account or is an admin
                var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (currentUserId != userId && !User.IsInRole("Moderator"))
                {
                    return Forbid();
                }

                var result = await _userService.DeleteUserAsync(userId);
                if (!result)
                {
                    return NotFound(new { message = $"User {userId} not found" });
                }

                return Ok(new { message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting user {userId}: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Search users by username or wallet
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers([FromQuery] string query, [FromQuery] int limit = 10)
        {
            try
            {
                if (string.IsNullOrEmpty(query))
                {
                    return BadRequest(new { message = "Search query is required" });
                }

                var results = await _userService.SearchUsersAsync(query, limit);
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error searching users: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Verify wallet address
        /// </summary>
        [HttpPost("{userId}/verify-wallet")]
        [Authorize]
        public async Task<IActionResult> VerifyWallet(string userId, [FromBody] VerifyWalletRequest request)
        {
            try
            {
                // Verify that the user is verifying their own wallet
                var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (currentUserId != userId)
                {
                    return Forbid();
                }

                var result = await _userService.VerifyWalletAsync(userId, request);
                if (!result)
                {
                    return StatusCode(400, new { message = "Wallet verification failed" });
                }

                return Ok(new { message = "Wallet verified successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error verifying wallet for user {userId}: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Update user role (admin only)
        /// </summary>
        [HttpPut("{userId}/role")]
        [Authorize(Roles = "Moderator")]
        public async Task<IActionResult> UpdateUserRole(string userId, [FromBody] UpdateRoleRequest request)
        {
            try
            {
                var user = await _userService.UpdateUserRoleAsync(userId, request.Role);
                if (user == null)
                {
                    return NotFound(new { message = $"User {userId} not found" });
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating role for user {userId}: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
