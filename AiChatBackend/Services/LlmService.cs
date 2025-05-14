using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;

namespace AiChatBackend.Services;

public class LlmService(ILogger<LlmService> logger, IChatClient client)
{
    private readonly ILogger<LlmService> logger = logger;
    private readonly IChatClient client = client;

    public async Task<string> GeneralizeUserPromptAsJsonAsync(string userPrompt)
    {
        var txt = $"""
            You are a smart query interpreter. Analyze the user query and return:
            
            - The cleaned query text that captures what the user is looking for
            - Any filters or constraints that can be used for structured search
            - Keep your response strictly in JSON format

            User query: "{userPrompt}"            
            """;

        List<ChatMessage> msg = [];

        msg.Add(new(ChatRole.User, txt));

        var r = await client.GetResponseAsync(msg);
        return r.Text;
    }

    public async Task<string> GetResponseAsync(string prompt)
    {
        List<ChatMessage> msg = [];

        msg.Add(new(ChatRole.User, prompt));

        var r = await client.GetResponseAsync(msg);
        return r.Text;
    }

    public async IAsyncEnumerable<StreamingChatResponse> StreamResponseAsync(string prompt, [EnumeratorCancellation] CancellationToken ct = default)
    {
        List<ChatMessage> msg = [];

        msg.Add(new(ChatRole.User, prompt));

        var id = Generator.NextStreamingId();
        logger.LogInformation($"Streaming {id} started");

        await foreach (var resp in client.GetStreamingResponseAsync(msg, cancellationToken: ct))
        {
            var hasFinished = resp.FinishReason.HasValue;
            yield return new()
            {
                StreamingId = id,
                HasFinished = hasFinished,
                Message = new(ChatSender.Assistant, resp.Text),
                ModelId = resp.ModelId,
                CreatedAt = resp.CreatedAt ?? DateTime.UtcNow
            };

            if (hasFinished)
            {
                logger.LogInformation($"Streaming {id} completed");
            }
        }
    }
}
