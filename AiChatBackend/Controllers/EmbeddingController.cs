using AiChatBackend.Caches;
using AiChatBackend.Services;
using Microsoft.AspNetCore.Mvc;
using OllamaSharp.Models;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AiChatBackend.Controllers;

[Route($"{BASE_ROUTE}/embedding")]
[ApiController]
public class EmbeddingController(
    ILogger<EmbeddingController> logger, IConfiguration config, QdrantDb qdrant, InMemoryVectorDb imvDb,
    LlmService llm, TextAnalysisCache tsCache) : ControllerBase
{
    private readonly ILogger<EmbeddingController> logger = logger;
    private readonly IConfiguration config = config;
    private readonly QdrantDb qdrant = qdrant;
    private readonly InMemoryVectorDb imvDb = imvDb;
    private readonly LlmService llm = llm;
    private readonly TextAnalysisCache tsCache = tsCache;

    //[HttpPost("generate-embedding")]
    //public async Task<ActionResult<string>> GenerateEmbedding([FromBody] string text, CancellationToken ct)
    //{
    //    var result = await vector.GenerateEmbeddingAsync(text, ct);
    //    if (result == null)
    //    {
    //        return "Error";
    //    }

    //    return JsonSerializer.Serialize(new
    //    {
    //        result.Vector.Length,
    //        result.ModelId,
    //        result.CreatedAt,
    //        Vector = result.Vector.ToArray()
    //    });
    //}

    //[Obsolete]
    //[HttpGet("get-model-name")]
    //public ActionResult<string> GetModelName([FromQuery] LlmModelType type)
    //{
    //    var succ = llm.Models.TryGetValue(type, out string model);
    //    return succ ? model : "Unknown";
    //}    

    //[Obsolete]
    //[HttpGet("get-models-dictionary")]
    //public ActionResult<Dictionary<LlmModelType, string>> GetModelsDictionary()
    //{
    //    return llm.Models;
    //}

    #region Text anaysis
    [HttpGet("text/list-all-from-cache")]
    public ActionResult<List<TextVector>> TextListAllFromCache(CancellationToken ct)
    {
        return tsCache.ListAll();
    }

    [HttpGet("text/get")]
    public async Task<ActionResult<TextVector>> TextGet([FromBody] string key, CancellationToken ct)
    {
        var succ = Guid.TryParse(key, out var guid);
        if (!succ)
            return null;

        return await imvDb.GetTextAsync(guid, ct);
    }

    [HttpPost("text/feed")]
    public async Task<ActionResult<ResponseBase>> TextFeed([FromBody] string text, CancellationToken ct)
    {
        return await imvDb.UpsertTextAsync(text, ct);
    }

    [HttpPost("text/auto-populate")]
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

    [HttpDelete("text/delete")]
    public async Task<ActionResult<ResponseBase>> TextDelete([FromQuery] string key, CancellationToken ct)
    {
        var succ = Guid.TryParse(key, out var guid);
        if (!succ)
            return new ResponseBase { IsSuccess = false, Message = "Unable to parse key as GUID" };

        return await imvDb.DeleteTextAsync(guid, ct);
    }

    [HttpPost("text/query-vector-db")]
    public IAsyncEnumerable<TextAnalysisSimilarityResult> TextQueryVectorDb([FromBody] TextAnalysisVdbRequest req, CancellationToken ct)
    {
        return imvDb.QueryTextSimilarityAsync(req.Prompt, req.Top, ct);
    }

    [HttpPost("text/query-llm")]
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


    [HttpPost("text/stream-post")]
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

    [HttpGet("text/stream-get")]
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
    #endregion

    #region Recipe
    //[HttpGet("recipe/list-all-from-external-api")]
    //public async Task<ActionResult<List<RecipeVectorModelBase>>> RecipeListAllFromExternalApi([FromQuery] int limit, CancellationToken ct)
    //{
    //    return await api.ListRecipesAsync(limit, ct);
    //}

    //[HttpPost("recipe/feed-all-from-external-api")]
    //public async Task<ActionResult<ResponseBase>> RecipeFeedAll(CancellationToken ct)
    //{
    //    Stopwatch sw0 = Stopwatch.StartNew();
    //    logger.LogInformation("[1] Obtaining recipes..");
    //    var data = await api.ListRecipesAsync(50, ct);
    //    sw0.Stop();
    //    logger.LogInformation($"[1] {data.Count} data obtained ({sw0.Elapsed} elapsed)");

    //    Stopwatch sw1 = Stopwatch.StartNew();
    //    logger.LogInformation($"[2] Upserting {data.Count}..");
    //    var resp = await qdrant.UpsertRecipesAsync(data, ct);
    //    sw1.Stop();
    //    logger.LogInformation($"[2] Upsert finished ({sw1.Elapsed} elapsed)..");
    //    logger.LogInformation($"[2] Resp = {JsonSerializer.Serialize(resp)}");

    //    return resp;
    //}

    [HttpPost("test1")]
    public async Task<ActionResult<List<string>>> Test([FromBody] AutoPopulateStatementRequest req, CancellationToken ct)
    {
        return await llm.GenerateRandomSentencesAsync(req.Number, req.Length);
    }

    [HttpPost("test2")]
    public async Task<ActionResult<string>> Test2([FromBody] string userPrompt, CancellationToken ct)
    {
        return await llm.GeneralizeUserPromptAsJsonAsync(userPrompt);
    }

    [HttpPost("test3")]
    public ActionResult Test3()
    {
        var model = LlmModelHelper.GetModel(config, LlmModelType.Text);
        logger.LogInformation(JsonSerializer.Serialize(model));

        return Ok();
    }
    #endregion

    //[HttpGet("list-collection-names")]
    //public async IAsyncEnumerable<string> ListCollectionNames([EnumeratorCancellation] CancellationToken ct)
    //{
    //    await foreach (var cn in vector.ListCollectionNamesAsync(ct))
    //    {
    //        yield return cn;
    //    }
    //}
}
