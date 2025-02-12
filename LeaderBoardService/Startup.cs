using LeaderBoardService.Common.Events;
using LeaderBoardService.Common.Messaging;
using LeaderBoardService.Domain.Model;
using LeaderBoardService.Domain.Persistence;
using LeaderBoardService.Domain.Persistence.Repositories;
using LeaderBoardService.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using RabbitMQ.Client;

namespace LeaderBoardService;

public class Startup
{
    public Startup(IConfiguration configuration, IWebHostEnvironment env)
    {
        Configuration = configuration;
        Environment = env;
    }

    public IConfiguration Configuration { get; }
    public IWebHostEnvironment Environment { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        var connectionString = Configuration.GetValue<string>($"ConnectionString:LeaderBoard");
        services.AddDbContext<LeaderBoardDbContext>(x => x.UseSqlServer(connectionString));
        services.AddScoped<IDataRepository<Game>, GameRepository>();
        services.AddScoped<ICustomerScoreRepository<CustomerScore>, CustomerScoreRepository>();
        services.AddScoped<ILeaderBoardService, Service.LeaderBoardService>();
        services.AddControllers().AddNewtonsoftJson(o =>
        {
            o.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
        });
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "LeaderBoardService", Version = "v1" });
        });

        var rabbitConfig = Configuration.GetSection("RabbitMQ");
        var exchangeName = rabbitConfig["Exchange"];
        var queueName = rabbitConfig["Queue"];
        var deadLetterExchange = rabbitConfig["DeadLetterExchange"];
        var deadLetterQueue = rabbitConfig["DeadLetterQueue"];
        var subscriberOptions = new RabbitMqSubscriberOptions(exchangeName, queueName, deadLetterExchange, deadLetterQueue);
        services.AddSingleton(subscriberOptions);

        var connectionFactory = new ConnectionFactory()
        {
            HostName = rabbitConfig["HostName"],
            UserName = rabbitConfig["UserName"],
            Password = rabbitConfig["Password"],
            Port = rabbitConfig.GetValue<int>("Port"),
            DispatchConsumersAsync = true // this is mandatory to have Async Subscribers
        };
        services.AddSingleton<IConnectionFactory>(connectionFactory);

        services.AddSingleton<IMessageBusConnection, RabbitMqMessageBusConnection>();
        services.AddSingleton<ISubscriber, RabbitMqSubscriber>();
        services.AddSingleton<RabbitMqPublisher>();

        var channel = System.Threading.Channels.Channel.CreateBounded<Message>(100);
        services.AddSingleton(channel);

        services.AddHostedService<GameEventSubscriberWorker>();
        services.AddTransient<IMessageHandler<GameEventMessage>, GameEventMessageHandler>();
        services.AddTransient<IMessageHandler<CustomerScoreEventMessage>, CustomerScoreEventMessageHandler>();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (!env.IsProduction())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "LeaderBoardService v1"));
        }

        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });

        app.UseWelcomePage();
    }
}