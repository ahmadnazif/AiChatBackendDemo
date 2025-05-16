using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;

namespace AiChatBackend.Services;

public class LlmService(ILogger<LlmService> logger, IChatClient client)
{
    private readonly ILogger<LlmService> logger = logger;
    private readonly IChatClient client = client;

    public async Task<string> GeneralizeUserPromptAsJsonAsync(string userPrompt)
    {
        List<ChatMessage> msg = [];

        msg.Add(new(ChatRole.System, """
            You are a smart query interpreter. Analyze the user query and return:
            
            - The cleaned query text that captures what the user is looking for
            - Any filters or constraints that can be used for structured search
            - Keep your response strictly in JSON format
            """));

        msg.Add(new(ChatRole.User, userPrompt));

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

    public async Task<List<string>> GenerateRandomStatementsAsync(int number, TextGenerationLength length)
    {
        List<ChatMessage> msg = [];

        msg.Add(new(ChatRole.System, """
            You are a smart JSON generator. Your response must be a valid raw JSON string only,
            without any explanation, markdown, or extra text.
            """));

        msg.Add(new(ChatRole.User, $"""
            Generate {number} distinct, concise, and interesting sentences on random topics. 
            The length of text should be {length.ToString().ToLower()}. 
            """));

        //var prompt = $"""
        //    Generate a raw JSON array containing exactly {number} distinct, concise, and interesting text statements on random topics. 
        //    The length of text should be {length.ToString().ToLower()}. 
        //    The output should ONLY be the JSON array with no additional surrounding text or explanations.
        //    """;

        //msg.Add(new(ChatRole.User, prompt));

        try
        {
            logger.LogInformation($"PROMPT = {prompt}");
            var r = await client.GetResponseAsync(msg);
            logger.LogInformation($"RESPONSE = {r.Text}");

            return JsonSerializer.Deserialize<List<string>>(r.Text);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
            return [];
        }
    }

    public async IAsyncEnumerable<StreamingChatResponse> StreamResponseAsync(string prompt, [EnumeratorCancellation] CancellationToken ct = default)
    {
        List<ChatMessage> msg = [];

        msg.Add(new(ChatRole.User, prompt));

        return StreamResponseAsync(msg, ct);

        //var id = Generator.NextStreamingId();
        //logger.LogInformation($"Streaming {id} started");

        //await foreach (var resp in client.GetStreamingResponseAsync(msg, cancellationToken: ct))
        //{
        //    var hasFinished = resp.FinishReason.HasValue;
        //    yield return new()
        //    {
        //        StreamingId = id,
        //        HasFinished = hasFinished,
        //        Message = new(ChatSender.Assistant, resp.Text),
        //        ModelId = resp.ModelId,
        //        CreatedAt = resp.CreatedAt ?? DateTime.UtcNow
        //    };

        //    if (hasFinished)
        //    {
        //        logger.LogInformation($"Streaming {id} completed");
        //    }
        //}
    }

    public async IAsyncEnumerable<StreamingChatResponse> StreamResponseAsync(List<ChatMessage> chatMessages, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var id = Generator.NextStreamingId();
        logger.LogInformation($"Streaming {id} started");

        await foreach (var resp in client.GetStreamingResponseAsync(chatMessages, cancellationToken: ct))
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
