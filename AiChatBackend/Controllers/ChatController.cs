using AiChatBackend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace AiChatBackend.Controllers;

[Route($"{BASE_ROUTE}/chat")]
[ApiController]
public class ChatController(ILogger<ChatController> logger, LlmService llm) : ControllerBase
{
    private readonly ILogger<ChatController> logger = logger;
    private readonly LlmService llm = llm;

    [HttpPost("send")]
    public async Task<ActionResult<string>> Send([FromBody] string prompt, CancellationToken ct)
    {
        try
        {
            return await llm.GetResponseTextAsync(prompt);
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }
}
