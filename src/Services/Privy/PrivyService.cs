using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using UserService.Services.Privy;

namespace UserService.Services.Privy
{
    public class PrivyService : IPrivyService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PrivyService> _logger;


        private readonly JwtSecurityTokenHandler _jwtSecurityTokenHandler;
        private List<SecurityKey>? _privySigningKeys; // Cache Privy's public keys
        private DateTime _lastJwksFetchTime = DateTime.MinValue;
        private readonly TimeSpan _jwksCacheDuration = TimeSpan.FromHours(6); // Cache duration

        public PrivyService(HttpClient httpClient, IConfiguration configuration, ILogger<PrivyService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            _jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
        }

        public async Task<PrivyUser> VerifyTokenAsync(string privyToken, string expectedPrivyId)
        {
            try
            {
                var isPrivyTokenValid = await VerifyPrivyAccessToken(privyToken, expectedPrivyId);
                if (!isPrivyTokenValid)
                {
                    throw new UnauthorizedAccessException("Invalid or expired Privy access token.");
                }

                var appId = _configuration.GetSection("Privy:AppId").Value;
                var apiUrl = _configuration.GetSection("Privy:ApiUrl").Value;
                var appSecret = _configuration.GetSection("Privy:AppSecret").Value;

                // Build the Basic Auth header
                var authBytes = Encoding.ASCII.GetBytes($"{appId}:{appSecret}");
                var authHeader = Convert.ToBase64String(authBytes);

                var request = new HttpRequestMessage(HttpMethod.Get, $"{apiUrl}/users/{expectedPrivyId}");
                request.Headers.Add("privy-app-id", $"{appId}");

                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

                var content = new StringContent(
                    JsonSerializer.Serialize(new { token = privyToken }),
                    Encoding.UTF8,
                    "application/json"
                );
                request.Content = content;

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Privy token verification failed with status: {response.StatusCode}");
                    throw new UnauthorizedAccessException("Invalid Privy token");
                }

                var jsonContent = await response.Content.ReadAsStringAsync();
                var privyUser = JsonSerializer.Deserialize<PrivyUser>(jsonContent, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (privyUser == null)
                {
                    throw new UnauthorizedAccessException("Failed to parse Privy response");
                }

                return privyUser;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"HTTP error during Privy verification: {ex.Message}");
                throw new UnauthorizedAccessException("Privy service unavailable", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error verifying Privy token: {ex.Message}");
                throw;
            }
        }


        private async Task<bool> VerifyPrivyAccessToken(string privyAccessToken, string expectedPrivyId)
        {
            // 1. Fetch and Cache Privy's JWKS
            // Replace with the actual Privy JWKS URL. You can typically find this in Privy's docs
            // or by looking for a `.well-known/jwks.json` endpoint related to your Privy app.
            // Example: "https://your-privy-app-id.privy.io/.well-known/jwks.json" or similar.
            // Check Privy's documentation for the exact JWKS endpoint.
            var privyJwksUrl = _configuration["Privy:JwksUrl"] ??
                                throw new InvalidOperationException("Privy JWKS URL is not configured.");

            if (_privySigningKeys == null || (DateTime.UtcNow - _lastJwksFetchTime) > _jwksCacheDuration)
            {
                try
                {
                    var jwksResponse = await _httpClient.GetStringAsync(privyJwksUrl);
                    var jwks = new JsonWebKeySet(jwksResponse);
                    _privySigningKeys = jwks.Keys.Select(k => (SecurityKey)k).ToList();
                    _lastJwksFetchTime = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching Privy JWKS: {ex.Message}");
                    // If JWKS cannot be fetched, we cannot validate, so treat as invalid.
                    return false;
                }
            }

            if (_privySigningKeys == null || !_privySigningKeys.Any())
            {
                Console.WriteLine("No Privy signing keys available.");
                return false;
            }

            // 2. Define Token Validation Parameters
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = _privySigningKeys, // Use the fetched keys

                ValidateIssuer = true,
                ValidIssuer = _configuration["Privy:Issuer"], // Privy's issuer (e.g., "privy.io")

                ValidateAudience = true,
                ValidAudience = _configuration["Privy:Audience"], // Your Privy app ID

                ValidateLifetime = true, // Check expiration and not-before times
                ClockSkew = TimeSpan.FromMinutes(5) // Allow for slight clock differences (e.g., 5 minutes)
            };

            try
            {
                // 3. Validate the token
                SecurityToken validatedToken;
                var principal = _jwtSecurityTokenHandler.ValidateToken(
                    privyAccessToken,
                    tokenValidationParameters,
                    out validatedToken
                );

                // 4. Verify Subject (Privy ID) matches the expected ID from the request
                var privyIdClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (privyIdClaim != expectedPrivyId)
                {
                    Console.WriteLine($"Privy ID mismatch: Expected {expectedPrivyId}, Got {privyIdClaim}");
                    return false;
                }

                Console.WriteLine($"Privy access token for {expectedPrivyId} successfully validated.");
                return true;
            }
            catch (SecurityTokenExpiredException)
            {
                Console.WriteLine("Privy access token expired.");
                return false;
            }
            catch (SecurityTokenValidationException ex)
            {
                Console.WriteLine($"Privy access token validation failed: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred during Privy token validation: {ex.Message}");
                return false;
            }
        }

    }
}
