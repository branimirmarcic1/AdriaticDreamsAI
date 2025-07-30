using MassTransit;
using AI.Worker;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddMassTransit(x =>
        {
            // Registriraj našeg consumer-a
            x.AddConsumer<ChatQueryConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host("rabbitmq", "/", h => {
                    h.Username("guest");
                    h.Password("guest");
                });

                // Ovo automatski kreira red (queue) za naš consumer
                cfg.ReceiveEndpoint("ai-query-queue", e =>
                {
                    e.ConfigureConsumer<ChatQueryConsumer>(context);
                });
            });
        });
    })
    .Build();

host.Run();