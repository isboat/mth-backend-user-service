using Azure.Messaging.ServiceBus;
using System.Text.Json;
using UserService.Models;

namespace UserService.Events
{
    public class UserEventPublisher : IUserEventPublisher
    {
        private readonly ServiceBusClient _serviceBusClient;
        private readonly ILogger<UserEventPublisher> _logger;

        public UserEventPublisher(ServiceBusClient serviceBusClient, ILogger<UserEventPublisher> logger)
        {
            _serviceBusClient = serviceBusClient;
            _logger = logger;
        }

        public async Task PublishUserCreatedEventAsync(UserDto user)
        {
            await PublishEventAsync("user-created", new
            {
                userId = user.UserId,
                username = user.Username,
                email = user.Email,
                walletAddress = user.WalletAddress,
                role = user.Role.ToString(),
                createdAt = user.CreatedAt
            });
        }

        public async Task PublishUserUpdatedEventAsync(UserDto user)
        {
            await PublishEventAsync("user-updated", new
            {
                userId = user.UserId,
                username = user.Username,
                email = user.Email,
                role = user.Role.ToString(),
                isVerified = user.IsVerified,
                updatedAt = user.UpdatedAt
            });
        }

        public async Task PublishUserDeletedEventAsync(string userId)
        {
            await PublishEventAsync("user-deleted", new
            {
                userId = userId,
                deletedAt = DateTime.UtcNow
            });
        }

        private async Task PublishEventAsync(string eventType, object eventData)
        {
            try
            {
                var sender = _serviceBusClient.CreateSender("user-events");
                var message = new ServiceBusMessage(JsonSerializer.Serialize(eventData))
                {
                    Subject = eventType,
                    ContentType = "application/json"
                };

                await sender.SendMessageAsync(message);
                _logger.LogInformation($"Published event: {eventType}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error publishing event {eventType}: {ex.Message}");
                // In production, implement retry logic or dead-letter handling
            }
        }
    }
}
