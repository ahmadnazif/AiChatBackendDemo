using Microsoft.Extensions.AI;
using OllamaSharp.Models;
using System.Runtime.CompilerServices;

namespace AiChatBackend.Services;

public class LlmService(ILogger<LlmService> logger, IChatClient client, OllamaEmbeddingGenerator gen)
{
    private readonly ILogger<LlmService> logger = logger;
    private readonly IChatClient client = client;
    private readonly OllamaEmbeddingGenerator gen = gen;

    public async Task<ReadOnlyMemory<float>> GenerateVectorAsync(string text, CancellationToken ct = default)
    {
        return await gen.GenerateVectorAsync(text, cancellationToken: ct);
    }

    public async Task<string> GeneralizeUserPromptAsJsonAsync(string userPrompt, string modelId = null)
    {
        List<ChatMessage> msg = [];

        //msg.Add(new(ChatRole.System, """
        //    You are a smart query interpreter. Analyze the user query and return:

        //    - The cleaned query text that captures what the user is looking for
        //    - Any filters or constraints that can be used for structured search
        //    - Keep your response strictly in JSON format
        //    """));

        msg.Add(new(ChatRole.User, $"""
            You are a smart query interpreter. Analyze the user query and return:
            - Do not add any explanation, labels, markdown, or surrounding text
            - The cleaned query text that captures what the user is looking for
            - Any filters or constraints that can be used for structured search
            - Keep your response strictly in JSON format.
            User prompt: {userPrompt}
            """));

        //msg.Add(new(ChatRole.User, userPrompt));

        ChatOptions options = new()
        {
             ModelId = modelId
        };

        var r = await client.GetResponseAsync(msg, options);
        return r.Text;
    }

    public async Task<string> GetResponseTextAsync(string userPrompt, string systemPrompt = null, string modelId = null)
    {
        List<ChatMessage> msg = [];

        if (!string.IsNullOrWhiteSpace(systemPrompt))
            msg.Add(new(ChatRole.System, systemPrompt));

        msg.Add(new(ChatRole.User, userPrompt));

        ChatOptions opt = new()
        {
             ModelId = modelId
        };

        var r = await client.GetResponseAsync(msg, opt);
        return r.Text;
    }

    public async Task<ChatResponse> GetResponseAsync(List<ChatMessage> chatMessages)
    {
        return await client.GetResponseAsync(chatMessages);
    }

    public async Task<List<string>> GenerateRandomSentencesAsync(int number, TextGenerationLength length, string modelId = null)
    {
        List<ChatMessage> msg = [];

        //msg.Add(new(ChatRole.User, $"""
        //    Generate a raw JSON array containing exactly {number} distinct, concise, and interesting text statements on random topics.
        //    The length of text should be {length.ToString().ToLower()}. 
        //    The output should ONLY be the JSON array with no additional surrounding text or explanations.
        //    """));

        // NEW
        msg.Add(new(ChatRole.System, """          
            You are a JSON generator. Your task is to return only a valid raw JSON array string. 
            Do not add any explanation, labels, markdown, or surrounding text. 
            The format must be like: ["Sentence one.", "Sentence two.", "Sentence three."]          
            """));

        msg.Add(new(ChatRole.User, $"""
            Generate a raw JSON array containing exactly {number} distinct, concise, and interesting sentences on random topics.
            Each sentence must be surrounded by double quotes, and each item must be separated by commas.
            The length of text should be {length.ToString().ToLower()}.
            The response must be strictly valid JSON: just the array, no explanations, no markdown, no headers.
            """));

        try
        {
            ChatOptions opt = new()
            {
                ModelId = modelId
            };

            var r = await client.GetResponseAsync(msg, opt);
            logger.LogInformation($"RESPONSE = {r.Text}");

            return JsonSerializer.Deserialize<List<string>>(r.Text);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
            return [];
        }
    }

    public IAsyncEnumerable<StreamingChatResponse> StreamResponseAsync(string userPrompt, CancellationToken ct = default)
    {
        List<ChatMessage> msg = [];

        msg.Add(new(ChatRole.User, userPrompt));

        return StreamResponseAsync(msg, ct);
    }

    public IAsyncEnumerable<StreamingChatResponse> StreamResponseAsync(ChatMessage chatMessage, CancellationToken ct = default)
    {
        List<ChatMessage> msg = [chatMessage];
        return StreamResponseAsync(msg, ct);
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
