using LeaderBoardService.Common.Events;
using LeaderBoardService.Common.Messaging;
using LeaderBoardService.Domain.Model;
using LeaderBoardService.Domain.Persistence.Repositories;

namespace LeaderBoardService.Service;

public class GameEventMessageHandler : IMessageHandler<GameEventMessage>
{
    private readonly IDataRepository<Game> _gameRepository;
    public GameEventMessageHandler(IDataRepository<Game> gameRepository)
    {
        _gameRepository = gameRepository;
    }

    public async Task Handle(GameEventMessage @event)
    {
        var game = await _gameRepository.GetByGuidAsync(@event.GameId, CancellationToken.None);
        if (game == null)
            throw new InvalidOperationException($"Game not found for the given id {@event.GameId}");
        game.IsActive = game.IsActive switch
        {
            false when @event.EventType?.Equals("START", StringComparison.InvariantCultureIgnoreCase) ??
                       false => true,
            true when @event.EventType?.Equals("END", StringComparison.InvariantCultureIgnoreCase) ??
                      false => false,
            _ => game.IsActive
        };

        await _gameRepository.SaveChangesAsync(CancellationToken.None);
    }
}