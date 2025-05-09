using AiChatBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace AiChatBackend.Controllers;

[Route($"{BASE_ROUTE}/embedding")]
[ApiController]
public class EmbeddingController(IVectorStorage vector) : ControllerBase
{
    private readonly IVectorStorage vector = vector;


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

    [HttpPost("feed-data")]
    public async Task<ActionResult<ResponseBase>> Feed([FromBody] FoodVectorModelBase food, CancellationToken ct)
    {
        return await vector.UpsertFoodAsync(food, ct);
    }

    [HttpPost("query")]
    public async Task<ActionResult> Query([FromBody] string prompt, CancellationToken ct)
    {
        await vector.QueryFoodsAsync(prompt, ct);
        return Ok();
    }
}
