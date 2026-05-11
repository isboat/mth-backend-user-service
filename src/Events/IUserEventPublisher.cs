using UserService.Models;

namespace UserService.Events
{
    public interface IUserEventPublisher
    {
        Task PublishUserCreatedEventAsync(UserDto user);
        Task PublishUserUpdatedEventAsync(UserDto user);
        Task PublishUserDeletedEventAsync(string userId);
    }
}
