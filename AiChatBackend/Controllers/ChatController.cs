using AiChatBackend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace AiChatBackend.Controllers;

[Route($"{BASE_ROUTE}/chat")]
[ApiController]
public class ChatController(ILogger<ChatController> logger, IChatClient chatClient, IVectorStorage vector) : ControllerBase
{
    private readonly IChatClient chatClient = chatClient;
    private readonly ILogger<ChatController> logger = logger;
    private readonly IVectorStorage vector = vector;

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
            return resp.Messages[0].Text;
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
            ChoiceCount = resp.Messages.Count, //resp.Choices.Count,
            resp.ModelId,
            resp.Usage.InputTokenCount,
            resp.Usage.OutputTokenCount,
            Duration = duration
        };

        logger.LogInformation(JsonSerializer.Serialize(r));
    }
}
