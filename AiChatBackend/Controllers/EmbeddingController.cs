using AiChatBackend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;

namespace AiChatBackend.Controllers;

[Route($"{BASE_ROUTE}/embedding")]
[ApiController]
public class EmbeddingController(IVectorStorage vector, ApiClient api) : ControllerBase
{
    private readonly IVectorStorage vector = vector;
    private readonly ApiClient api = api;

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
    [HttpGet("recipe/list-all")]
    public async Task<ActionResult<DummyjsonRecipeResponse>> RecipeListAllFromExternalApi(CancellationToken ct)
    {
        return await api.ListRecipesAsync(ct);
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
