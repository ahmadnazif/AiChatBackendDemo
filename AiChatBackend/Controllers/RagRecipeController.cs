using AiChatBackend.Services;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.AccessControl;

namespace AiChatBackend.Controllers;

[Route("rag/recipe")]
[ApiController]
public class RagRecipeController(ILogger<RagRecipeController> logger, IConfiguration config, ApiClient api, QdrantDb qdrant, LlmService llm) : ControllerBase
{
    private readonly ILogger<RagRecipeController> logger = logger;
    private readonly IConfiguration config = config;
    private readonly ApiClient api = api;
    private readonly QdrantDb qdrant = qdrant;
    private readonly LlmService llm = llm;

    [HttpGet("is-qdrant-running")]
    public async Task<ActionResult<bool>> IsQdrantRunning(CancellationToken ct)
    {
        return await api.IsQdrantRunningAsync(ct);
    }

    [HttpGet("external-api/list-all-recipe")]
    public async Task<ActionResult<List<RecipeVectorModelBase>>> RecipeListAllFromExternalApi([FromQuery] int limit, CancellationToken ct)
    {
        return await api.ListRecipesAsync(limit, ct);
    }

    [HttpPost("external-api/feed-all-recipe-to-qdrant")]
    public async Task<ActionResult<ResponseBase>> RecipeFeedAll(CancellationToken ct)
    {
        Stopwatch sw0 = Stopwatch.StartNew();
        logger.LogInformation("[1] Obtaining recipes..");
        var data = await api.ListRecipesAsync(50, ct);
        sw0.Stop();
        logger.LogInformation($"[1] {data.Count} data obtained ({sw0.Elapsed} elapsed)");

        Stopwatch sw1 = Stopwatch.StartNew();
        logger.LogInformation($"[2] Upserting {data.Count}..");
        var resp = await qdrant.UpsertRecipesAsync(data, ct);
        sw1.Stop();
        logger.LogInformation($"[2] Upsert finished ({sw1.Elapsed} elapsed)..");
        logger.LogInformation($"[2] Resp = {JsonSerializer.Serialize(resp)}");

        return resp;
    }

    [HttpGet("count-all")]
    public async Task<ActionResult<ulong>> CountAllInDb(CancellationToken ct)
    {
        return await qdrant.CountRecipeAsync(ct);
    }

    [HttpPost("query-v1")]
    public async Task<ActionResult<string>> QueryV1([FromBody] EmbeddingQueryRequest req, CancellationToken ct)
    {
        if (req.Top < 1)
            req.Top = 1;

        return await qdrant.QueryRecipeV1Async(req, ct);
    }

    [HttpPost("query-v2")]
    public async Task<ActionResult<string>> QueryV2([FromBody] string userPrompt, CancellationToken ct)
    {
        return await qdrant.QueryRecipeV2Async(userPrompt, ct);
    }

    [HttpPost("query-vector-db")]
    public IAsyncEnumerable<RecipeVdbQueryResult> QueryVectorDb([FromBody] VdbRequest req, CancellationToken ct)
    {
        return qdrant.QueryAsync(req.Prompt, req.Top, ct);
    }
}
