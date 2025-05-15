using AiChatBackend.Caches;
using AiChatBackend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace AiChatBackend.Controllers;

[Route($"{BASE_ROUTE}/embedding")]
[ApiController]
public class EmbeddingController(
    ILogger<EmbeddingController> logger, IConfiguration config, IVectorStorage vector,
    InMemoryVectorDb imvDb, ApiClient api, LlmService llm, TextSimilarityCache tsCache) : ControllerBase
{
    private readonly ILogger<EmbeddingController> logger = logger;
    private readonly IConfiguration config = config;
    private readonly IVectorStorage vector = vector;
    private readonly InMemoryVectorDb imvDb = imvDb;
    private readonly ApiClient api = api;
    private readonly LlmService llm = llm;
    private readonly TextSimilarityCache tsCache = tsCache;

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

    [HttpGet("get-model-name")]
    public ActionResult<string> GetModelName([FromQuery] LlmModelType type)
    {
        var ollama = "Ollama";
        return type switch
        {
            LlmModelType.Embedding => config[$"{ollama}:EmbeddingModel"],
            LlmModelType.Text => config[$"{ollama}:TextModel"],
            LlmModelType.Vision => config[$"{ollama}:Vision"],
            _ => "Unknown",
        };
    }

    #region Text
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
        var statements = await llm.GenerateRandomStatementsAsync(req.Number, req.Length);

        List<ResponseBase> resps = [];
        foreach (var statement in statements)
        {
            var resp = await imvDb.UpsertTextAsync(statement, ct);
            resps.Add(resp);
        }
        sw.Stop();

        return new ResponseBase
        {
            IsSuccess = resps.Exists(x => x.IsSuccess),
            Message = $"{resps.Count(x => x.IsSuccess)} statement populated ({sw.Elapsed} elapsed)"
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

    [HttpGet("text/query-similarity")]
    public IAsyncEnumerable<TextSimilarityResult> TextQuery([FromQuery] string text, CancellationToken ct)
    {
        return imvDb.QueryTextSimilarityAsync(text, ct);
    }
    #endregion

    #region Recipe
    [HttpGet("recipe/list-all-from-external-api")]
    public async Task<ActionResult<List<RecipeVectorModelBase>>> RecipeListAllFromExternalApi([FromQuery] int limit, CancellationToken ct)
    {
        return await api.ListRecipesAsync(limit, ct);
    }

    [HttpPost("recipe/feed-all-from-external-api")]
    public async Task<ActionResult<ResponseBase>> RecipeFeedAll(CancellationToken ct)
    {
        Stopwatch sw0 = Stopwatch.StartNew();
        logger.LogInformation("[1] Obtaining recipes..");
        var data = await api.ListRecipesAsync(50, ct);
        sw0.Stop();
        logger.LogInformation($"[1] {data.Count} data obtained ({sw0.Elapsed} elapsed)");

        Stopwatch sw1 = Stopwatch.StartNew();
        logger.LogInformation($"[2] Upserting {data.Count}..");
        var resp = await vector.UpsertRecipesAsync(data, ct);
        sw1.Stop();
        logger.LogInformation($"[2] Upsert finished ({sw1.Elapsed} elapsed)..");
        logger.LogInformation($"[2] Resp = {JsonSerializer.Serialize(resp)}");

        return resp;
    }

    [HttpPost("recipe/query")]
    public async Task<ActionResult<string>> RecipeQuery([FromBody] EmbeddingQueryRequest req, CancellationToken ct)
    {
        if (req.Top < 1)
            req.Top = 1;

        //await foreach (var r in vector.QueryRecipeAsync(req, ct))
        //{
        //    yield return r;
        //}

        return await vector.QueryRecipeV1Async(req, ct);
    }

    [HttpPost("recipe/query-v2")]
    public async Task<ActionResult<string>> RecipeQueryV2([FromBody] string userPrompt, CancellationToken ct)
    {
        return await vector.QueryRecipeV2Async(userPrompt, ct);
    }

    [HttpPost("test")]
    public async Task<ActionResult<List<string>>> Test([FromBody] AutoPopulateStatementRequest req, CancellationToken ct)
    {
        return await llm.GenerateRandomStatementsAsync(req.Number, req.Length);
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
