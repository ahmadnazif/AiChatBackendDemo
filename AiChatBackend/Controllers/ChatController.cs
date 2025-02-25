using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;

namespace AiChatBackend.Controllers;

[Route("api/chat")]
public class ChatController(IChatClient chatClient, ILogger<ChatController> logger) : ControllerBase
{
    private readonly IChatClient chatClient = chatClient;
    private readonly ILogger<ChatController> logger = logger;

    [HttpGet("send")]
    public async Task<ActionResult<string>> Send([FromQuery] string? prompt, CancellationToken ct)
    {
        try
        {
            List<ChatMessage> msg = [];
            msg.Add(new ChatMessage(ChatRole.User, prompt));

            var resp = await chatClient.GetResponseAsync(msg, cancellationToken: ct);
            logger.LogInformation(JsonSerializer.Serialize(resp));
            return resp.Message.Text;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

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
