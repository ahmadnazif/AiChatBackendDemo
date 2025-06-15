using AiChatBackend.Caches;
using AiChatBackend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace AiChatBackend.Controllers;

[Route($"{BASE_ROUTE}/rag/text-analysis")]
[ApiController]
public class RagTextAnalysisController(ILogger<RagTextAnalysisController> logger, InMemoryVectorDb imvDb,
    LlmService llm, TextAnalysisCache tsCache) : ControllerBase
{
    private readonly ILogger<RagTextAnalysisController> logger = logger;
    private readonly InMemoryVectorDb imvDb = imvDb;
    private readonly LlmService llm = llm;
    private readonly TextAnalysisCache tsCache = tsCache;

    [HttpGet("list-all-from-cache")]
    public ActionResult<List<TextVector>> TextListAllFromCache(CancellationToken ct)
    {
        return tsCache.ListAll();
    }

    [HttpGet("get")]
    public async Task<ActionResult<TextVector>> TextGet([FromBody] string key, CancellationToken ct)
    {
        var succ = Guid.TryParse(key, out var guid);
        if (!succ)
            return null;

        return await imvDb.GetTextAsync(guid, ct);
    }

    [HttpPost("feed")]
    public async Task<ActionResult<ResponseBase>> TextFeed([FromBody] string text, CancellationToken ct)
    {
        return await imvDb.UpsertTextAsync(text, ct);
    }

    [HttpPost("auto-populate")]
    public async Task<ActionResult<ResponseBase>> TextAutoPopulate([FromBody] AutoPopulateStatementRequest req, CancellationToken ct)
    {
        if (req.Number < 1)
            req.Number = 5;

        Stopwatch sw = Stopwatch.StartNew();
        var statements = await llm.GenerateRandomSentencesAsync(req.Number, req.Length, req.ModelId, req.Topic, req.Language);

        List<ResponseBase> resps = [];
        foreach (var statement in statements)
        {
            var resp = await imvDb.UpsertTextAsync(statement, ct);
            resps.Add(resp);
        }
        sw.Stop();

        var isSuccess = resps.Exists(x => x.IsSuccess);

        return new ResponseBase
        {
            IsSuccess = isSuccess,
            Message = !isSuccess ? "Failed to generate statement. Please check API console log" : $"{resps.Count(x => x.IsSuccess)} statement populated ({sw.Elapsed} elapsed)"
        };
    }

    [HttpDelete("delete")]
    public async Task<ActionResult<ResponseBase>> TextDelete([FromQuery] string key, CancellationToken ct)
    {
        var succ = Guid.TryParse(key, out var guid);
        if (!succ)
            return new ResponseBase { IsSuccess = false, Message = "Unable to parse key as GUID" };

        return await imvDb.DeleteTextAsync(guid, ct);
    }

    [HttpPost("query-vector-db")]
    public IAsyncEnumerable<TextAnalysisVdbQueryResult> TextQueryVectorDb([FromBody] VdbRequest req, CancellationToken ct)
    {
        return imvDb.QueryTextSimilarityAsync(req.Prompt, req.Top, ct);
    }

    [HttpPost("query-llm")]
    public IAsyncEnumerable<StreamingChatResponse> TextQueryLlm([FromBody] LlmRequest req, CancellationToken ct)
    {
        // 1: Build context
        // -----------------

        logger.LogInformation("Building context from result..");
        StringBuilder sb = new();

        foreach (var item in req.Results)
        {
            sb.AppendLine($"- {item}");
            sb.AppendLine();
        }

        // 2: Compose prompt to LLM
        // -------------------------

        var prompt = $"""
            You are a helpful text analyzer.

            A user asked: "{req.OriginalPrompt}"

            Based on the internal search, here are some relevant info:
            {sb}

            Using the above information, answer the user's question as helpfully as possible.
            """;

        logger.LogInformation("Sending to LLM for processing..");
        return llm.StreamResponseAsync(prompt, req.ModelId, ct);
    }


    [HttpPost("stream-post")]
    public async IAsyncEnumerable<string> StreamPost([FromBody] int max, [EnumeratorCancellation] CancellationToken ct)
    {
        for (int i = 0; i < max; i++)
        {
            if (i == max)
                yield break;

            await Task.Delay(1000, ct);
            yield return i.ToString();
        }
    }

    [HttpGet("stream-get")]
    public async IAsyncEnumerable<string> StreamGet([FromQuery] int max, [EnumeratorCancellation] CancellationToken ct)
    {
        for (int i = 0; i < max; i++)
        {
            if (i == max)
                yield break;

            await Task.Delay(1000, ct);
            yield return i.ToString();
        }
    }
}
