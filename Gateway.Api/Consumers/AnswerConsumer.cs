using Contracts;
using Gateway.Api.Hubs;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace Gateway.Api.Consumers;

public class AnswerConsumer : IConsumer<AnswerGenerated>
{
    private readonly IHubContext<ChatHub> _hubContext;

    public AnswerConsumer(IHubContext<ChatHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task Consume(ConsumeContext<AnswerGenerated> context)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveAnswer", context.Message.AnswerText);
    }
}