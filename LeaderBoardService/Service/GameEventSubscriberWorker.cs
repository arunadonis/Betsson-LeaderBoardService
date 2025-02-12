using LeaderBoardService.Common.Events;
using LeaderBoardService.Common.Messaging;

namespace LeaderBoardService.Service;

public class GameEventSubscriberWorker : BackgroundService
{
    private readonly ISubscriber _subscriber;
    private readonly ILogger<GameEventSubscriberWorker> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IMessageHandler<GameEventMessage> _gameEventMessageHandler;
    private readonly IMessageHandler<CustomerScoreEventMessage> _customerScoreEventMessageHandler;

    public GameEventSubscriberWorker(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
        var scope = _serviceScopeFactory.CreateScope();
        _logger = scope.ServiceProvider.GetRequiredService<ILogger<GameEventSubscriberWorker>>();
        _subscriber = scope.ServiceProvider.GetRequiredService<ISubscriber>();
        _gameEventMessageHandler = scope.ServiceProvider.GetRequiredService<IMessageHandler<GameEventMessage>>();
        _customerScoreEventMessageHandler = scope.ServiceProvider.GetRequiredService<IMessageHandler<CustomerScoreEventMessage>>();

        _subscriber.OnMessage += OnMessageAsync;
    }

    private async Task OnMessageAsync(object sender, RabbitMqSubscriberEventArgs args)
    {
        _logger.LogInformation($"Got a new message: {args.Message.GetType().Name}");

        switch (args.Message)
        {
            case GameEventMessage gameEvent:
                await _gameEventMessageHandler.Handle(gameEvent);
                break;
            case CustomerScoreEventMessage customerScoreEvent:
                await _customerScoreEventMessageHandler.Handle(customerScoreEvent);
                break;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _subscriber.StartAsync(stoppingToken);

    }
}