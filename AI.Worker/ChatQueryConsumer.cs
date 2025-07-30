using MassTransit;
using Contracts;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace AI.Worker;

public class ChatQueryConsumer : IConsumer<ChatQueryReceived>
{
    private readonly ILogger<ChatQueryConsumer> _logger;

    public ChatQueryConsumer(ILogger<ChatQueryConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<ChatQueryReceived> context)
    {
        _logger.LogInformation("PRIMLJENA PORUKA: '{Query}' za korisnika '{UserId}'",
            context.Message.QueryText, context.Message.UserId);

        // OVDJE ĆE KASNIJE IĆI AI LOGIKA!

        return Task.CompletedTask;
    }
}