using System;
using System.Text;
using LeaderBoardService.Common.Events;
using RabbitMQ.Client;

namespace LeaderBoardService.Publisher;

public class MessageReceiver : DefaultBasicConsumer
{

    private readonly IModel _channel;

    public MessageReceiver(IModel channel)
    {
        _channel = channel;
    }

    public override void HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, IBasicProperties properties, ReadOnlyMemory<byte> body)
    {
        if (properties.IsHeadersPresent())
        {
            var type = Encoding.UTF8.GetString(properties.Headers["MessageType"] as byte[]);

            if (!string.IsNullOrWhiteSpace(type) && type.Equals(nameof(LeaderBoardEventMessage)))
            {
                Console.WriteLine();
                Console.WriteLine("Hurray!!! We have a new leader....");
                Console.WriteLine(string.Concat("Message: ", Encoding.UTF8.GetString(body.ToArray())));
            }
            else
            {
                _channel.BasicNack(deliveryTag, false, true);
            }
        }
        else
        {
            _channel.BasicNack(deliveryTag, false, true);
        }

        _channel.BasicAck(deliveryTag, false);
    }
}