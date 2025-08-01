#pragma warning disable SKEXP0011, SKEXP0010, SKEXP0001, SKEXP0020 // <-- Dodan je SKEXP0011

using AI.Worker;
using MassTransit;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Connectors.AzureAISearch;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        var config = hostContext.Configuration;
        var azureOpenAIConfig = config.GetSection("AI:AzureOpenAI");
        var azureSearchConfig = config.GetSection("AI:AzureAISearch");

        // 1. Registracija Azure OpenAI konektora
        services.AddAzureOpenAIChatCompletion(
            deploymentName: azureOpenAIConfig["ChatDeploymentName"]!,
            endpoint: azureOpenAIConfig["Endpoint"]!,
            apiKey: azureOpenAIConfig["ApiKey"]!);

        services.AddAzureOpenAITextEmbeddingGeneration(
            deploymentName: azureOpenAIConfig["EmbeddingDeploymentName"]!,
            endpoint: azureOpenAIConfig["Endpoint"]!,
            apiKey: azureOpenAIConfig["ApiKey"]!);

        // 2. Registracija Azure AI Search kao Memory Store
        services.AddSingleton<IMemoryStore>(sp =>
        {
            return new AzureAISearchMemoryStore(azureSearchConfig["Endpoint"]!, azureSearchConfig["ApiKey"]!);
        });

        // 3. Registracija Kernela
        services.AddKernel();

        // 4. Registracija MassTransita
        services.AddMassTransit(x =>
        {
            x.AddConsumer<ChatQueryConsumer>();
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host("rabbitmq", "/", h => {
                    h.Username("guest");
                    h.Password("guest");
                });
                cfg.ReceiveEndpoint("ai-query-queue", e =>
                {
                    e.ConfigureConsumer<ChatQueryConsumer>(context);
                });
            });
        });

        // 5. Registracija servisa za unos podataka
        services.AddHostedService<DataIngestionService>();
    })
    .Build();

await host.RunAsync();