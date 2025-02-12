using RabbitMQ.Client.Events;

namespace LeaderBoardService.Common.Messaging;

public interface ISubscriber
{
    Task StartAsync(CancellationToken cancellationToken);
    event AsyncEventHandler<RabbitMqSubscriberEventArgs> OnMessage;
}