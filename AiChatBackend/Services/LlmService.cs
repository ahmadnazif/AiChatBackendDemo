using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;

namespace AiChatBackend.Services;

public class LlmService(ILogger<LlmService> logger, IChatClient client)
{
    private readonly ILogger<LlmService> logger = logger;
    private readonly IChatClient client = client;

    public async Task<string> SendAsync(ChatRole role, string prompt)
    {
        List<ChatMessage> msg = [];

        msg.Add(new(role, prompt));

        var r = await client.GetResponseAsync(msg);
        return r.Text;
    }

    //public async Task<ChatResponse> SendAsync(ChatRole role, string prompt, [EnumeratorCancellation] CancellationToken ct = default)
    //{
    //    List<ChatMessage> msg = [];

    //    msg.Add(new(role, prompt));

    //  await foreach(var resp in client.GetStreamingResponseAsync(msg, cancellationToken: ct))
    //    {
    //        resp.
    //    }
    //}
}
