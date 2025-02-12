using LeaderBoardService.Common.Events;

namespace LeaderBoardService.Common.Messaging;

public interface IMessageHandler<in TEvent> : IMessageHandler
    where TEvent : Message
{
    Task Handle(TEvent @event);
}

public interface IMessageHandler
{

}