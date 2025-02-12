using System.Data;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using LeaderBoardService.Common.Events;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace LeaderBoardService.Common.Messaging
{
    public record RabbitMqSubscriberOptions(string ExchangeName, string QueueName, string DeadLetterExchangeName, string DeadLetterQueue);

    public class RabbitMqSubscriber : ISubscriber, IDisposable
    {
        public event AsyncEventHandler<RabbitMqSubscriberEventArgs> OnMessage;

        private readonly IMessageBusConnection _connection;
        private readonly ILogger<RabbitMqSubscriber> _logger;
        private readonly RabbitMqSubscriberOptions _options;

        private IModel _channel;

        public RabbitMqSubscriber(IMessageBusConnection connection, RabbitMqSubscriberOptions options, ILogger<RabbitMqSubscriber> logger)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private void InitChannel()
        {
            _channel?.Dispose();

            _channel = _connection.CreateChannel();

            _channel.ExchangeDeclare(exchange: _options.DeadLetterExchangeName, type: ExchangeType.Direct);
            _channel.QueueDeclare(queue: _options.DeadLetterQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
            _channel.QueueBind(_options.DeadLetterQueue, _options.DeadLetterExchangeName, routingKey: string.Empty, arguments: null);

            _channel.ExchangeDeclare(exchange: _options.ExchangeName, type: ExchangeType.Direct);

            _channel.QueueDeclare(queue: _options.QueueName,
                durable: false,
                exclusive: false,
                autoDelete: true,
                arguments: null);

            _channel.QueueBind(_options.QueueName, _options.ExchangeName, "GameEvents", null);

            _channel.CallbackException += (sender, ea) =>
            {
                InitChannel();
                InitSubscription();
            };
        }

        private void InitSubscription()
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.Received += OnMessageReceivedAsync;

            _channel.BasicQos(0, 1, false);

            _channel.BasicConsume(queue: _options.QueueName, autoAck: false, consumer: consumer);
        }

        private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs eventArgs)
        {
            var consumer = sender as IBasicConsumer;
            var channel = consumer?.Model ?? _channel;
            

            Message message = null;
            try
            {
                var body = Encoding.UTF8.GetString(eventArgs.Body.Span);
                if (eventArgs.BasicProperties.IsHeadersPresent())
                {
                    var time = Convert.ToDateTime(Encoding.UTF8.GetString(eventArgs.BasicProperties.Headers["Timestamp"] as byte[]));
                    var type = Encoding.UTF8.GetString(eventArgs.BasicProperties.Headers["MessageType"] as byte[]);

                    if (!string.IsNullOrWhiteSpace(type))
                    {
                        switch (type)
                        {
                            case nameof(GameEventMessage):
                                message = JsonSerializer.Deserialize<GameEventMessage>(body);
                                break;
                            case nameof(CustomerScoreEventMessage):
                                message = JsonSerializer.Deserialize<CustomerScoreEventMessage>(body);
                                break;
                            default:
                                _channel.BasicNack(eventArgs.DeliveryTag, false, true);
                                break;
                        }
                    }
                }
                else
                {
                    _channel.BasicNack(eventArgs.DeliveryTag, false, true);
                }

                if (message != null)
                {
                    await this.OnMessage(this, new RabbitMqSubscriberEventArgs(message));

                    channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
                }
            }
            catch (Exception ex)
            {
                var errMsg = (message is null) ? $"an error has occurred while processing a message: {ex.Message}"
                    : $"an error has occurred while processing message '{message.GetType().Name}': {ex.Message}";
                _logger.LogError(ex, errMsg);

                if (eventArgs.Redelivered)
                    channel.BasicReject(eventArgs.DeliveryTag, requeue: false);
                else
                    channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: true);
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            InitChannel();
            InitSubscription();

            await Task.CompletedTask;
        }

        public void Dispose()
        {
            _channel?.Dispose();
        }
    }
}
