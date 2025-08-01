using MassTransit.Internals;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Memory;

namespace AI.Worker;

#pragma warning disable SKEXP0001, SKEXP0052
public class DataIngestionService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DataIngestionService> _logger;
    private readonly IConfiguration _configuration;

    public DataIngestionService(IServiceProvider serviceProvider, ILogger<DataIngestionService> logger, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Pokrećem unos podataka u vektorsku bazu...");

        using var scope = _serviceProvider.CreateScope();
        var memoryStore = scope.ServiceProvider.GetRequiredService<IMemoryStore>();
        var embeddingService = scope.ServiceProvider.GetRequiredService<ITextEmbeddingGenerationService>();

        var collectionName = _configuration["AI:CollectionName"]!;

        var collections = await memoryStore.GetCollectionsAsync(cancellationToken).ToListAsync(cancellationToken);
        if (collections.Contains(collectionName))
        {
            _logger.LogInformation("Kolekcija '{collection}' već postoji. Preskačem unos.", collectionName);
            return;
        }

        try
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "KnowledgeBase", "apartments.txt");
            var text = await File.ReadAllTextAsync(filePath, cancellationToken);
            var paragraphs = text.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            var memory = new SemanticTextMemory(memoryStore, embeddingService);

            for (int i = 0; i < paragraphs.Length; i++)
            {
                await memory.SaveInformationAsync(collectionName, paragraphs[i], $"paragraph-{i}", cancellationToken: cancellationToken);
                _logger.LogInformation("Spremljen odlomak {index} u kolekciju '{collection}'", i + 1, collectionName);
            }
            _logger.LogInformation("Unos podataka završen.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Greška prilikom unosa podataka.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}