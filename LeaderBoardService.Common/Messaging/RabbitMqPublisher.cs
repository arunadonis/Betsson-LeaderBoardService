using System.Text;
using System.Text.Json;
using LeaderBoardService.Common.Events;
using RabbitMQ.Client;

namespace LeaderBoardService.Common.Messaging;

public class RabbitMqPublisher : IPublisher, IDisposable
{
    private readonly string _exchangeName;

    private readonly IMessageBusConnection _connection;
    private IModel _channel;
    private readonly IBasicProperties _properties;

    public RabbitMqPublisher(IMessageBusConnection connection, RabbitMqSubscriberOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ExchangeName))
            throw new ArgumentException($"'{nameof(options.ExchangeName)}' cannot be null or whitespace", nameof(options.ExchangeName));
        _exchangeName = options.ExchangeName;

        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _channel = _connection.CreateChannel();
        _channel.ExchangeDeclare(exchange: _exchangeName, type: ExchangeType.Direct);
        _properties = _channel.CreateBasicProperties();
    }

    public void Publish<T>(T @message) where T : Message
    {

        _properties.Headers ??= new Dictionary<string, object>();
        _properties.Headers.Clear();
        _properties.Headers.Add("Timestamp", DateTime.Now.ToString());
        _properties.Headers.Add("MessageType", @message.GetType().Name);

        var jsonData = JsonSerializer.Serialize(@message);

        var body = Encoding.UTF8.GetBytes(jsonData);

        _channel.BasicPublish(
            exchange: _exchangeName,
            routingKey: "GameEvents",
            mandatory: true,
            basicProperties: _properties,
            body: body);
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _channel = null;
    }
}