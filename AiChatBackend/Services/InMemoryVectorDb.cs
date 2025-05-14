using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;

namespace AiChatBackend.Services;

public class InMemoryVectorDb(ILogger<InMemoryVectorDb> logger, OllamaEmbeddingGenerator gen, InMemoryVectorStore store) : IVectorStorage
{
    private readonly ILogger<InMemoryVectorDb> logger = logger;
    private readonly OllamaEmbeddingGenerator gen = gen;
    private readonly InMemoryVectorStore store = store;
    private const string COLL_FOOD = "food";

    public async Task<ResponseBase> UpsertFoodAsync(FoodVectorModelBase food, CancellationToken ct)
    {
        try
        {
            var coll = store.GetCollection<Guid, FoodVectorModel>(COLL_FOOD);
            await coll.CreateCollectionIfNotExistsAsync(ct);

            var id = await coll.UpsertAsync(new FoodVectorModel
            {
                Id = Guid.NewGuid(),
                FoodName = food.FoodName,
                Remarks = food.Remarks,
                Vector = await gen.GenerateVectorAsync(food.Remarks)
            }, ct);

            return new()
            {
                IsSuccess = true,
                Message = id.ToString()
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


    //public async Task<ResponseBase> UpsertFoodsAsync(List<FoodVectorModelBase> foods, CancellationToken ct)
    //{
    //    try
    //    {
    //        var coll = store.GetCollection<ulong, FoodVectorModel>(COLL_FOOD);
    //        await coll.CreateCollectionIfNotExistsAsync(ct);

    //        List<ulong> ids = [];

    //        foreach (var food in foods)
    //        {
    //            var id = await coll.UpsertAsync(new FoodVectorModel
    //            {
    //                Id = 1, //Guid.NewGuid(),
    //                FoodName = food.FoodName,
    //                Remarks = food.Remarks,
    //                Vector = await gen.GenerateVectorAsync(food.Remarks)
    //            }, ct);

    //            ids.Add(id);
    //        }

    //        return new()
    //        {
    //            IsSuccess = true,
    //            Message = JsonSerializer.Serialize(ids)
    //        };
    //    }
    //    catch (Exception ex)
    //    {
    //        return new()
    //        {
    //            IsSuccess = false,
    //            Message = ex.Message
    //        };
    //    }
    //}

    public async Task QueryAsync(string prompt, CancellationToken ct)
    {
        try
        {
            var coll = store.GetCollection<Guid, FoodVectorModel>(COLL_FOOD);
            await coll.CreateCollectionIfNotExistsAsync(ct);

            var vector = await gen.GenerateVectorAsync(prompt, cancellationToken: ct);

            var result = coll.SearchEmbeddingAsync(vector, 1, cancellationToken: ct);

            await foreach(var r in result)
            {
                logger.LogInformation($"{r.Record.FoodName} [{r.Score}]");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
        }
    }

}
