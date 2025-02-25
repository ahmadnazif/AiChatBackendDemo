using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;

namespace AiChatBackend.Controllers;

[Route("api/chat")]
public class ChatController(IChatClient chatClient) : ControllerBase
{
    private readonly IChatClient chatClient = chatClient;

    [HttpGet("send")]
    public async Task<ActionResult<string>> Send([FromQuery] string? prompt, CancellationToken ct)
    {
        try
        {
            List<ChatMessage> msg = [];
            msg.Add(new ChatMessage(ChatRole.User, prompt));

            var resp = await chatClient.GetResponseAsync(msg, cancellationToken: ct);
            return resp.Message.Text;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }        
    }
}
