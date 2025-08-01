#pragma warning disable SKEXP0001

using Contracts;
using MassTransit;
using MassTransit.Internals;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Memory;

namespace AI.Worker;

public class ChatQueryConsumer : IConsumer<ChatQueryReceived>
{
    private readonly Kernel _kernel;
    private readonly ISemanticTextMemory _semanticTextMemory;
    private readonly ILogger<ChatQueryConsumer> _logger;
    private readonly IConfiguration _configuration;
    private readonly IPublishEndpoint _publishEndpoint; // <-- DODANO

    public ChatQueryConsumer(Kernel kernel, IMemoryStore memoryStore, ITextEmbeddingGenerationService embeddingService,
                             ILogger<ChatQueryConsumer> logger, IConfiguration configuration, IPublishEndpoint publishEndpoint) // <-- DODANO
    {
        _kernel = kernel;
        _logger = logger;
        _configuration = configuration;
        _publishEndpoint = publishEndpoint; // <-- DODANO
        _semanticTextMemory = new SemanticTextMemory(memoryStore, embeddingService);
    }

    public async Task Consume(ConsumeContext<ChatQueryReceived> context)
    {
        var userQuery = context.Message.QueryText;
        _logger.LogInformation("PRIMLJEN UPIT: '{Query}'", userQuery);

        var collectionName = _configuration["AI:CollectionName"]!;

        var searchResults = await _semanticTextMemory.SearchAsync(
            collectionName,
            userQuery,
            limit: 2,
            minRelevanceScore: 0.5).ToListAsync();

        var contextText = string.Join("\n\n", searchResults.Select(r => r.Metadata.Text));
        _logger.LogInformation("Pronađen kontekst: {Context}", string.IsNullOrWhiteSpace(contextText) ? "NEMA" : contextText);

        var prompt = $"""
            Ti si ljubazni asistent turističke agencije Adriatic Dreams.
            Odgovori na pitanje korisnika isključivo na temelju priloženog konteksta.
            Ako kontekst ne sadrži odgovor, reci da nemaš tu informaciju. Budi kratak i precizan.

            Kontekst:
            {contextText}

            Pitanje korisnika:
            {userQuery}

            Odgovor:
            """;

        var result = await _kernel.InvokePromptAsync(prompt, cancellationToken: context.CancellationToken);
        var finalAnswer = result.GetValue<string>() ?? "Došlo je do greške.";

        _logger.LogInformation("GENERIRAN ODGOVOR: {Answer}", finalAnswer);

        // <-- DODANO: Objavljujemo poruku da je odgovor spreman
        await _publishEndpoint.Publish(new AnswerGenerated(
            context.Message.UserId,
            finalAnswer,
            context.Message.CorrelationId), context.CancellationToken);
    }
}