using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace AiChatBackend.Controllers;

[Route("rest-api/chat")]
[ApiController]
public class ChatController(IChatClient chatClient, ILogger<ChatController> logger) : ControllerBase
{
    private readonly IChatClient chatClient = chatClient;
    private readonly ILogger<ChatController> logger = logger;

    [HttpPost("send")]
    public async Task<ActionResult<string>> Send([FromBody] string prompt, CancellationToken ct)
    {
        try
        {
            List<ChatMessage> msg = [];
            msg.Add(new(ChatRole.User, prompt));

            Stopwatch sw = Stopwatch.StartNew();
            var resp = await chatClient.GetResponseAsync(msg, cancellationToken: ct);
            sw.Stop();

            Log(resp, sw.Elapsed);
            return resp.Message.Text;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    private void Log(ChatResponse resp, TimeSpan duration)
    {
        var r = new
        {
            ChoiceCount = resp.Choices.Count,
            resp.ModelId,
            resp.Usage.InputTokenCount,
            resp.Usage.OutputTokenCount,
            Duration = duration
        };

        logger.LogInformation(JsonSerializer.Serialize(r));
    }

    [NonAction]
    [HttpGet("stream")]
    public async IAsyncEnumerable<ChatResponseUpdate> Stream([FromQuery] string? prompt, [EnumeratorCancellation] CancellationToken ct)
    {
        List<ChatMessage> msg = [];
        msg.Add(new ChatMessage(ChatRole.User, prompt));

        await foreach (var x in chatClient.GetStreamingResponseAsync(msg, cancellationToken: ct))
        {
            yield return x;
        }
    }
}
