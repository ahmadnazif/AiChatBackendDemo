using AiChatBackend.Caches;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;
using System.Runtime.CompilerServices;
using System.Text;

namespace AiChatBackend.Services;

public class InMemoryVectorDb(ILogger<InMemoryVectorDb> logger, LlmService llm, InMemoryVectorStore store, TextSimilarityCache cache)
{
    private readonly ILogger<InMemoryVectorDb> logger = logger;
    private readonly LlmService llm = llm;
    private readonly InMemoryVectorStore store = store;
    private readonly TextSimilarityCache cache = cache;
    private const string COLL_TEXT = "text";

    public async Task<ResponseBase> UpsertTextAsync(string text, CancellationToken ct)
    {
        try
        {
            var coll = store.GetCollection<Guid, TextVector>(COLL_TEXT);
            await coll.EnsureCollectionExistsAsync(ct);

            TextVector record = new()
            {
                Id = Guid.NewGuid(),
                Text = text,
                Vector = await llm.GenerateVectorAsync(text, ct)
            };

            await coll.UpsertAsync(record, ct);
            cache.Add(record);

            return new()
            {
                IsSuccess = true,
                Message = record.Id.ToString()
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
            return new()
            {
                IsSuccess = false,
                Message = ex.Message
            };
        }
    }

    public async Task<TextVector> GetTextAsync(Guid key, CancellationToken ct)
    {
        try
        {
            var coll = store.GetCollection<Guid, TextVector>(COLL_TEXT);
            var x = store.GetCollection<Guid, TextVector>("");
            await coll.EnsureCollectionExistsAsync(ct);

            return await coll.GetAsync(key, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
            return null;
        }
    }

    public async Task<ResponseBase> DeleteTextAsync(Guid key, CancellationToken ct)
    {
        try
        {
            var coll = store.GetCollection<Guid, TextVector>(COLL_TEXT);
            await coll.EnsureCollectionExistsAsync(ct);

            await coll.DeleteAsync(key, ct);
            cache.Remove(key);

            return new()
            {
                IsSuccess = true,
                Message = key.ToString()
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
            return new()
            {
                IsSuccess = false,
                Message = ex.Message
            };
        }
    }

    public async IAsyncEnumerable<TextSimilarityResult> QueryTextSimilarityAsync(string text, int top, [EnumeratorCancellation] CancellationToken ct)
    {
        var coll = store.GetCollection<Guid, TextVector>(COLL_TEXT);
        await coll.EnsureCollectionExistsAsync(ct);

        var vector = await llm.GenerateVectorAsync(text, ct);

        var result = coll.SearchAsync(vector, top, cancellationToken: ct);

        await foreach (var r in result)
        {
            yield return new()
            {
                Guid = r.Record.Id.ToString(),
                Text = r.Record.Text,
                Score = r.Score.Value
            };
        }
    }

    //public async IAsyncEnumerable<string> QueryToLlmAsync(string userPrompt, List<string> resultFromDb, string? modelId, [EnumeratorCancellation] CancellationToken ct)
    //{
    //    // 1: Build context
    //    // -----------------
        
    //    logger.LogInformation("Building context from result..");
    //    StringBuilder sb = new();

    //    foreach (var item in resultFromDb)
    //    {
    //        sb.AppendLine($"- {item}");
    //        sb.AppendLine();
    //    }

    //    // 2: Compose prompt to LLM
    //    // -------------------------

    //    var prompt = $"""
    //        You are a helpful text analyzer.

    //        A user asked: "{userPrompt}"

    //        Based on the internal search, here are some relevant info:
    //        {sb}

    //        Using the above information, answer the user's question as helpfully as possible.
    //        """;

    //    logger.LogInformation("Sending to LLM for processing..");
    //    await foreach(var item in llm.StreamResponseAsync(prompt, modelId, ct))
    //    {
    //        //logger.LogInformation(item.Message.Text);
    //        yield return item.Message.Text;
    //    }
    //}

    public async IAsyncEnumerable<StreamingChatResponse> QueryToLlmAsync(string userPrompt, List<string> resultFromDb, string? modelId, [EnumeratorCancellation] CancellationToken ct)
    {
        // 1: Build context
        // -----------------

        logger.LogInformation("Building context from result..");
        StringBuilder sb = new();

        foreach (var item in resultFromDb)
        {
            sb.AppendLine($"- {item}");
            sb.AppendLine();
        }

        // 2: Compose prompt to LLM
        // -------------------------

        var prompt = $"""
            You are a helpful text analyzer.

            A user asked: "{userPrompt}"

            Based on the internal search, here are some relevant info:
            {sb}

            Using the above information, answer the user's question as helpfully as possible.
            """;

        logger.LogInformation("Sending to LLM for processing..");
        await foreach (var item in llm.StreamResponseAsync(prompt, modelId, ct))
        {
            yield return item;
        }
    }

}
