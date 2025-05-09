using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Connectors.InMemory;

namespace AiChatBackend.Services;

public class InMemoryVectorDb(ILogger<InMemoryVectorDb> logger, OllamaEmbeddingGenerator gen, InMemoryVectorStore store) : IVectorStorage
{
    private readonly ILogger<InMemoryVectorDb> logger = logger;
    private readonly OllamaEmbeddingGenerator gen = gen;
    private readonly InMemoryVectorStore store = store;
    private const string COLL_FOOD = "food";

    public async Task<Embedding<float>> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        try
        {
            return await gen.GenerateAsync(text, cancellationToken: ct);

        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
            return null;
        }
    }

    public async Task<ResponseBase> UpsertFoodsAsync(List<FoodVectorModelBase> foods, CancellationToken ct = default)
    {
        try
        {
            var coll = store.GetCollection<int, FoodVectorModel>(COLL_FOOD);
            await coll.CreateCollectionIfNotExistsAsync(ct);

            List<int> ids = [];

            foreach (var food in foods)
            {
                var id = await coll.UpsertAsync(new FoodVectorModel
                {
                    //Id = food.Id,
                    FoodName = food.FoodName,
                    Remarks = food.Remarks,
                    Vector = await gen.GenerateVectorAsync(food.Remarks)
                }, ct);

                ids.Add(id);
            }

            return new()
            {
                IsSuccess = true,
                Message = JsonSerializer.Serialize(ids)
            };
        }
        catch (Exception ex)
        {
            return new()
            {
                IsSuccess = false,
                Message = ex.Message
            };
        }
    }

    public async Task List()
    {
        //var store = new QdrantVectorStore(client);
        //var coll = store.GetCollection<ulong, TextVector>("text");
        //await coll.CreateCollectionIfNotExistsAsync(ct);

    }
}
