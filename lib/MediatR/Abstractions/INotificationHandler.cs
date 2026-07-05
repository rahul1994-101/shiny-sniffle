namespace MediatR.Abstractions;

public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    ValueTask HandleAsync(TNotification notification, CancellationToken cancellationToken = default);
}
