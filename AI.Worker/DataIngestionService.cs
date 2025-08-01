using MassTransit.Internals;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Memory;
using System.Text.Json;

namespace AI.Worker;

#pragma warning disable SKEXP0001, SKEXP0011, SKEXP0052
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
        _logger.LogInformation("Pokrećem unos podataka u vektorsku bazu iz JSON-a...");

        using var scope = _serviceProvider.CreateScope();
        var memoryStore = scope.ServiceProvider.GetRequiredService<IMemoryStore>();
        var embeddingService = scope.ServiceProvider.GetRequiredService<ITextEmbeddingGenerationService>();
        var collectionName = _configuration["AI:CollectionName"]!;

        try
        {
            var collections = await memoryStore.GetCollectionsAsync(cancellationToken).ToListAsync(cancellationToken);
            if (collections.Contains(collectionName))
            {
                _logger.LogInformation("Kolekcija '{collection}' već postoji. Preskačem unos.", collectionName);
                return;
            }

            var filePath = Path.Combine(AppContext.BaseDirectory, "KnowledgeBase", "apartments.json");
            var jsonContent = await File.ReadAllTextAsync(filePath, cancellationToken);
            var apartments = JsonSerializer.Deserialize<List<Apartment>>(jsonContent);

            if (apartments is null || apartments.Count == 0)
            {
                _logger.LogWarning("Nije pronađen nijedan apartman u JSON datoteci.");
                return;
            }

            var memory = new SemanticTextMemory(memoryStore, embeddingService);

            foreach (var apartment in apartments)
            {
                // Stvaramo lijepo formatiran tekst za svaki apartman
                var textToEmbed = $"Ime: {apartment.Name}. Lokacija: {apartment.Location}. Kapacitet: {apartment.Capacity} osoba. Cijena: {apartment.PricePerNight} EUR po noći. Detalji: {apartment.Details}. Ljubimci dozvoljeni: {(apartment.PetsAllowed ? "Da" : "Ne")}.";

                await memory.SaveInformationAsync(collectionName, textToEmbed, apartment.Id, cancellationToken: cancellationToken);
                _logger.LogInformation("Spremljen apartman '{Name}' u kolekciju '{collection}'", apartment.Name, collectionName);
            }
            _logger.LogInformation("Unos podataka iz JSON-a završen.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Greška prilikom unosa podataka iz JSON-a.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}