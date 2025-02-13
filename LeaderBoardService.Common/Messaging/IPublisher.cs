using LeaderBoardService.Common.Events;

namespace LeaderBoardService.Common.Messaging;

public interface IPublisher
{
    void Publish<T>(T @message) where T : Message;
}