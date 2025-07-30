// U Gateway.Api/Controllers/ChatController.cs
using Microsoft.AspNetCore.Mvc;
using MassTransit;
using Contracts;

[ApiController]
[Route("[controller]")]
public class ChatController : ControllerBase
{
    private readonly IPublishEndpoint _publishEndpoint;

    public ChatController(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    [HttpPost("ask")]
    public async Task<IActionResult> Post([FromBody] string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return BadRequest();
        }

        // Objavljujemo poruku na sabirnicu!
        await _publishEndpoint.Publish(new ChatQueryReceived(
            Guid.NewGuid(),
            query,
            "user123")); // UserID ćemo kasnije riješiti

        return Ok(new { Message = "Hvala, vaš upit se obrađuje." });
    }
}