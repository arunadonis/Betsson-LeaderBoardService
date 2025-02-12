using System.Text.Json;
using LeaderBoardService.Common.Events;
using LeaderBoardService.Common.Messaging;
using LeaderBoardService.Domain.Model;
using LeaderBoardService.Domain.Persistence.Repositories;

namespace LeaderBoardService.Service;

public class CustomerScoreEventMessageHandler : IMessageHandler<CustomerScoreEventMessage>
{
    private readonly IDataRepository<Game> _gameRepository;
    private readonly ICustomerScoreRepository<CustomerScore> _customerScoreRepository;
    private readonly RabbitMqPublisher _publisher;

    public CustomerScoreEventMessageHandler(IDataRepository<Game> gameRepository, ICustomerScoreRepository<CustomerScore> customerScoreRepository, RabbitMqPublisher publisher)
    {
        _gameRepository = gameRepository;
        _customerScoreRepository = customerScoreRepository;
        _publisher = publisher;
    }

    public async Task Handle(CustomerScoreEventMessage @event)
    {
        var customerGame = await _gameRepository.GetByGuidAsync(@event.GameId, CancellationToken.None);
        if (customerGame == null)
            throw new InvalidOperationException($"Game not found for the given id {@event.GameId}");

        var customerScore = await _customerScoreRepository.GetByGuidAsync(@event.CustomerId, CancellationToken.None);
        if (customerScore == null) // new customer score
        {
            await _customerScoreRepository.AddAsync(new CustomerScore
            {
                CustomerId = @event.CustomerId,
                CustomerName = @event.CustomerName,
                GameId = @event.GameId,
                Score = @event.Score
            }, CancellationToken.None);
        }
        else if (customerScore.GameId == @event.GameId && customerScore.Score < @event.Score) // update only if new high score
        {
            customerScore.Score = @event.Score;
        }

        // Update leaders board based on customer score
        if (customerGame.Leaders == null)
        {
            // looks like this is first customer score!!! hurray....
            // customer gets to leader board straight away
            var leaders = new List<Leaders>()
            {
                new()
                {
                    CustomerId = @event.CustomerId,
                    CustomerName = @event.CustomerName,
                    Score = @event.Score,
                    Rank = 1
                }
            };

            customerGame.Leaders = JsonSerializer.SerializeToUtf8Bytes(leaders, JsonSerializerOptions.Default);

            // broadcast leader board update
            _publisher.Publish(new LeaderBoardEventMessage()
            {
                GameName = customerGame.GameName,
                CustomerName = @event.CustomerName,
                Score = @event.Score,
                Rank = 1
            });
        }
        else
        {
            // there are already leaders for this game...
            var leaders = JsonSerializer.Deserialize<List<Leaders>>(customerGame.Leaders);
            if (leaders is { Count: > 0 })
            {
                var isExistingLeader = false;
                var isNewLeader = leaders.Any(x => x.Score < @event.Score);
                if (customerScore != null) // existing customer
                {
                    // existing leader?
                    var existingLeader = leaders.FirstOrDefault(x => x.CustomerId == @event.CustomerId);
                    if (existingLeader != null)
                    {
                        isExistingLeader = true;
                        // new high score?
                        if (@event.Score <= customerScore.Score)
                            isNewLeader = false;
                    }
                }

                if (isNewLeader || (leaders.Count < 20 && !isExistingLeader))
                {
                    leaders.Clear();
                    var customerScores = await _customerScoreRepository.GetAllByGameIdAsync(@event.GameId, CancellationToken.None);

                    var leads = customerScores.Count > 20
                        ? customerScores.OrderByDescending(x => x.Score).ToList()[..20]
                        : customerScores.OrderByDescending(x => x.Score).ToList();

                    leaders.AddRange(leads.Select((lead, i) => new Leaders()
                    {
                        CustomerId = lead.CustomerId,
                        CustomerName = lead.CustomerName,
                        Score = lead.Score,
                        Rank = i + 1
                    }));

                    customerGame.Leaders = JsonSerializer.SerializeToUtf8Bytes(leaders, JsonSerializerOptions.Default);

                    // broadcast leader board change
                    _publisher.Publish(new LeaderBoardEventMessage()
                    {
                        GameName = customerGame.GameName,
                        CustomerName = @event.CustomerName,
                        Score = @event.Score,
                        Rank = leaders.First(x => x.CustomerId == @event.CustomerId).Rank
                    });
                }
            }
        }
        await _gameRepository.SaveChangesAsync(CancellationToken.None);
    }
}