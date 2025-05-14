using Qdrant.Client;
using Qdrant.Client.Grpc;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using System.Runtime.CompilerServices;

namespace AiChatBackend.Services;

public class QdrantDb(ILogger<QdrantDb> logger, IVectorStore store, OllamaEmbeddingGenerator gen) : IVectorStorage
{
    private readonly ILogger<QdrantDb> logger = logger;
    private readonly IVectorStore store = store;
    private readonly OllamaEmbeddingGenerator gen = gen;
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
                Vector = await gen.GenerateVectorAsync(food.Remarks, cancellationToken: ct)
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
    //        var coll = store.GetCollection<Guid, FoodVectorModel>(COLL_FOOD);
    //        await coll.CreateCollectionIfNotExistsAsync(ct);

    //        List<Guid> ids = [];

    //        foreach (var food in foods)
    //        {
    //            var id = await coll.UpsertAsync(new FoodVectorModel
    //            {
    //                Id = Guid.NewGuid(),
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

    public async IAsyncEnumerable<string> QueryFoodAsync(EmbeddingQueryRequest req, [EnumeratorCancellation] CancellationToken ct)
    {
        var coll = store.GetCollection<Guid, FoodVectorModel>(COLL_FOOD);
        await coll.CreateCollectionIfNotExistsAsync(ct);

        var vector = await gen.GenerateVectorAsync(req.Prompt, cancellationToken: ct);

        var result = coll.SearchEmbeddingAsync(vector, req.Top, cancellationToken: ct);

        await foreach (var r in result)
        {
            var val = $"{r.Record.FoodName} [{r.Score}]";
            yield return val;
        }
    }

    public async IAsyncEnumerable<string> ListCollectionNamesAsync([EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var name in store.ListCollectionNamesAsync(ct))
        {
            logger.LogInformation($"{name}");
            yield return name;
        }
    }
}
