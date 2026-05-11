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

        public PrivyService(HttpClient httpClient, IConfiguration configuration, ILogger<PrivyService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<PrivyUser> VerifyTokenAsync(string privyToken)
        {
            try
            {
                var apiKey = _configuration.GetSection("Privy:ApiKey").Value;
                var apiUrl = _configuration.GetSection("Privy:ApiUrl").Value;

                var request = new HttpRequestMessage(HttpMethod.Post, $"{apiUrl}/api/v1/verify");
                request.Headers.Add("Authorization", $"Bearer {apiKey}");

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
    }
}
