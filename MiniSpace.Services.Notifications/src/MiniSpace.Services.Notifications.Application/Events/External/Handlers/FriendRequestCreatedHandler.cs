using Convey.CQRS.Events;
using MiniSpace.Services.Notifications.Application.Services;
using MiniSpace.Services.Notifications.Core.Entities;
using MiniSpace.Services.Notifications.Core.Events;
using MiniSpace.Services.Notifications.Core.Repositories;

namespace MiniSpace.Services.Notifications.Application.Events.External.Handlers
{
    public class FriendRequestCreatedHandler : IEventHandler<FriendRequestCreated>
    {
        private readonly IFriendEventRepository _friendEventRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IEventMapper _eventMapper;
        private readonly IMessageBroker _messageBroker;

        public FriendRequestCreatedHandler(IFriendEventRepository friendEventRepository, IEventMapper eventMapper, IMessageBroker messageBroker)
        {
            _friendEventRepository = friendEventRepository;
            _eventMapper = eventMapper;
            _messageBroker = messageBroker;
        }

        public async Task HandleAsync(FriendRequestCreated friendEvent, CancellationToken cancellationToken)
        {
            var newFriendEvent = new FriendEvent(
                id: Guid.NewGuid(),
                eventId: Guid.NewGuid(),
                userId: friendEvent.RequesterId, 
                friendId: friendEvent.FriendId,
                eventType: "FriendRequestCreated",
                details: $"A new friend request created from {friendEvent.RequesterId} to {friendEvent.FriendId}",
                createdAt: DateTime.UtcNow
            );

             var notification = new Notification(
                notificationId: Guid.NewGuid(),
                userId: friendEvent.RequesterId, 
                message: $"You have received a friend request from userId: {friendEvent.RequesterId}",
                status: NotificationStatus.Unread,
                createdAt: DateTime.UtcNow,
                updatedAt: null
            );

            await _friendEventRepository.AddAsync(newFriendEvent);
            
            await _messageBroker.PublishAsync(friendEvent);

            await _notificationRepository.AddAsync(notification);

            var notificationCreated = new NotificationCreated(
                notificationId: notification.NotificationId,
                userId: notification.UserId,
                message: notification.Message,
                createdAt: notification.CreatedAt
            );

            await _messageBroker.PublishAsync(notificationCreated);
        }
    }
}
