using RabbitMQ.Client;

namespace LeaderBoardService.Common.Messaging;

public interface IMessageBusConnection
{
    bool IsConnected { get; }

    IModel CreateChannel();
}