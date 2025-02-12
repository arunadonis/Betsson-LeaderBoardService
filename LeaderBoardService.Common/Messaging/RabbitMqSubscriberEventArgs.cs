using LeaderBoardService.Common.Events;

namespace LeaderBoardService.Common.Messaging;

public class RabbitMqSubscriberEventArgs : EventArgs
{
    public RabbitMqSubscriberEventArgs(Message message)
    {
        this.Message = message;
    }

    public Message Message { get; }
}