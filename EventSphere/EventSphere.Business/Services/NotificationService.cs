using EventSphere.API.Hubs;
using EventSphere.Business.Services.Interfaces;
using EventSphere.Domain.Entities;
using EventSphere.Infrastructure.Repositories;
using Microsoft.AspNetCore.SignalR;

namespace EventSphere.Business.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _repository;
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationService(INotificationRepository repository, IHubContext<NotificationHub> hubContext)
    {
        _repository = repository;
        _hubContext = hubContext;
    }


    public async Task SendNotificationAsync(int userId, string message)
    {
        var notification = new Notification { UserId = userId, Message = message, IsRead = false };
        await _repository.AddNotification(notification);
            
        // Find all connection IDs associated with the user
        var connectionIds = NotificationHub._connections.Where(x => x.Value == userId.ToString()).Select(x => x.Key);
            
        foreach (var connectionId in connectionIds)
        {
            await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveNotification", message);
        }
    }

    public async Task MarkAsReadAsync(int notificationId)
    {
        var notification = await _repository.GetNotificationById(notificationId);
        if (notification != null)
        {
            notification.IsRead = true;
            await _repository.UpdateNotification(notification);
        }
        else
        {
            throw new Exception("Notification not found");
        }
    }

    public async Task<IEnumerable<Notification>> GetUnreadNotificationsAsync(int userId)
    {
        return await _repository.GetUnReadNotificationsByUserId(userId);
    }
    
    public async Task MarkAllAsReadAsync(int userId)
    {
        await _repository.MarkAllAsReadAsync(userId);
    }
}