using AiChatBackend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace AiChatBackend.Controllers;

[Route($"{BASE_ROUTE}/embedding")]
[ApiController]
public class EmbeddingController(ILogger<EmbeddingController> logger, IVectorStorage vector, ApiClient api, LlmService llm) : ControllerBase
{
    private readonly ILogger<EmbeddingController> logger = logger;
    private readonly IVectorStorage vector = vector;
    private readonly ApiClient api = api;
    private readonly LlmService llm = llm;

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

    [HttpPost("food/feed-data")]
    public async Task<ActionResult<ResponseBase>> FoodFeed([FromBody] FoodVectorModelBase food, CancellationToken ct)
    {
        return await vector.UpsertFoodAsync(food, ct);
    }

    [HttpPost("food/query")]
    public async IAsyncEnumerable<string> FoodQuery([FromBody] EmbeddingQueryRequest req, [EnumeratorCancellation] CancellationToken ct)
    {
        if (req.Top < 1)
            req.Top = 1;

        await foreach(var r in vector.QueryFoodAsync(req, ct))
        {
            yield return r;
        }
    }

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

        return await vector.QueryRecipeAsync(req, ct);
    }

    [HttpPost("recipe/query-v2")]
    public async Task<ActionResult<string>> RecipeQueryV2([FromBody] string userPrompt, CancellationToken ct)
    {
        return await vector.QueryRecipeV2Async(userPrompt, ct);
    }

    [HttpPost("recipe/test")]
    public async Task<ActionResult<string>> Test([FromBody] string raw, CancellationToken ct)
    {
        return await llm.GeneralizeUserPromptAsJsonAsync(raw);
    }
    #endregion

    [HttpGet("list-collection-names")]
    public async IAsyncEnumerable<string> ListCollectionNames([EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var cn in vector.ListCollectionNamesAsync(ct))
        {
            yield return cn;
        }
    }
}
