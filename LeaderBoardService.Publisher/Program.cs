using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using LeaderBoardService.Common.Events;
using LeaderBoardService.Common.Messaging;
using LeaderBoardService.Domain.Model;
using LeaderBoardService.Domain.Persistence;
using LeaderBoardService.Domain.Persistence.Repositories;
using LeaderBoardService.Publisher;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

class Program
{
    static void Main(string[] args)
    {
        var builder = new ConfigurationBuilder();
        builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        var config = builder.Build();

        var rabbitMqConfig = config.GetSection("RabbitMQ");
        var connectionFactory = new ConnectionFactory()
        {
            HostName = rabbitMqConfig["HostName"],
            UserName = rabbitMqConfig["UserName"],
            Password = rabbitMqConfig["Password"],
            Port = Convert.ToInt32(rabbitMqConfig["Port"])
        };

        var connection = new RabbitMqMessageBusConnection(connectionFactory);
        var exchangeName = rabbitMqConfig["Exchange"];
        var queueName = rabbitMqConfig["Queue"];
        var deadLetterExchange = rabbitMqConfig["DeadLetterExchange"];
        var deadLetterQueue = rabbitMqConfig["DeadLetterQueue"];
        var subscriberOptions = new RabbitMqSubscriberOptions(exchangeName, queueName, deadLetterExchange, deadLetterQueue);

        var publisher = new RabbitMqPublisher(connection, subscriberOptions);

        var services = new ServiceCollection();
        var connectionString = config.GetSection("ConnectionString")["LeaderBoard"];
        services.AddDbContext<LeaderBoardDbContext>(x => x.UseSqlServer(connectionString));
        services.AddScoped<IDataRepository<Game>, GameRepository>();

        var serviceProvider = services.BuildServiceProvider();
        var gameRepo = serviceProvider.GetService<IDataRepository<Game>>();

        var games = gameRepo.GetAllAsync(CancellationToken.None).Result.ToList();

        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            foreach (var game in games)
            {
                Console.WriteLine($"Name: {game.GameName} Id: {game.GameId}");
            }

            Console.WriteLine("Enter game id to start/end: ");
            var id = Console.ReadLine();
            Console.WriteLine("Enter START or END the game:");
            var gameStatus = Console.ReadLine();

            try
            {
                var gameId = Guid.Parse(id);
                var message = new GameEventMessage()
                {
                    GameName = "Cricket",
                    GameId = gameId,
                    EventType = gameStatus
                };
                publisher.Publish(message);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Game Event message sent!");

                if (!gameStatus.Equals("END", StringComparison.InvariantCultureIgnoreCase))
                {
                    Console.WriteLine("Enter Customer Id (GUID): ");
                    var custId = Guid.Parse(Console.ReadLine());
                    Console.WriteLine("Enter Customer Name: ");
                    var custName = Console.ReadLine();
                    Console.WriteLine("Enter Customer Score: ");
                    var custScore = Convert.ToInt32(Console.ReadLine());

                    var scoreMessage = new CustomerScoreEventMessage
                    {
                        GameId = gameId,
                        CustomerId = custId,
                        CustomerName = custName,
                        Score = custScore
                    };

                    Console.WriteLine($"Customer Game Id: {scoreMessage.GameId}");
                    Console.WriteLine($"Customer Name: {scoreMessage.CustomerName}");
                    Console.WriteLine($"Customer Id: {scoreMessage.CustomerId}");
                    Console.WriteLine($"Customer Score: {scoreMessage.Score}");
                    publisher.Publish(scoreMessage);

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Customer Score Event message sent!");
                }

                var mqConnection = connectionFactory.CreateConnection();

                var channel = mqConnection.CreateModel();

                // accept only one unack-ed message at a time
                // uint prefetchSize, ushort prefetchCount, bool global
                channel.BasicQos(0, 1, false);

                MessageReceiver messageReceiver = new MessageReceiver(channel);

                channel.BasicConsume(queueName, false, messageReceiver);
                Console.ReadLine();

            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"an error has occurred while sending the message: {ex.Message}");
            }

            Console.ResetColor();
        }
    }
}