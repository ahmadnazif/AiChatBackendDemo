using Qdrant.Client;
using Qdrant.Client.Grpc;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using OllamaSharp.Models;

namespace AiChatBackend.Services;

public class QdrantDb(ILogger<QdrantDb> logger, IVectorStore store, OllamaEmbeddingGenerator gen) : IVectorStorage
{
    private readonly ILogger<QdrantDb> logger = logger;
    private readonly IVectorStore store = store;
    private readonly OllamaEmbeddingGenerator gen = gen;
    private const string COLL_FOOD = "food";
    private const string COLL_RECIPE = "recipe";

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

    #region Recipe

    public async Task<ResponseBase> UpsertRecipesAsync(List<RecipeVectorModelBase> data, CancellationToken ct)
    {
        try
        {
            var coll = store.GetCollection<ulong, RecipeVectorModel>(COLL_RECIPE);
            await coll.CreateCollectionIfNotExistsAsync(ct);

            List<ulong> ids = [];

            Stopwatch sw = Stopwatch.StartNew();
            foreach (var model in data)
            {
                var input = $"""
                        Name: {model.Name}
                        Ingredients: {string.Join(", ", model.Ingredients.Where(i => !string.IsNullOrWhiteSpace(i)))}
                        Instructions: {string.Join(" ", model.Instructions.Where(i => !string.IsNullOrWhiteSpace(i)))}
                        Tags: {string.Join(", ", model.Tags.Where(t => !string.IsNullOrWhiteSpace(t)))}
                        Meal Type: {string.Join(", ", model.MealType.Where(t => !string.IsNullOrWhiteSpace(t)))}
                        Difficulty: {model.Difficulty}
                        Cuisine: {model.Cuisine}
                        Rating: {model.Rating}
                        Prep Time: {model.PrepTimeMinutes} minutes
                        Cook Time: {model.CookTimeMinutes} minutes
                        Calories: {model.CaloriesPerServing}
                        """;

                Stopwatch swi = Stopwatch.StartNew();
                var id = await coll.UpsertAsync(new RecipeVectorModel
                {
                    Id = model.Id,
                    Name = model.Name,
                    CaloriesPerServing = model.CaloriesPerServing,
                    CookTimeMinutes = model.CookTimeMinutes,
                    Cuisine = model.Cuisine,
                    Difficulty = model.Difficulty,
                    Image = model.Image,
                    Ingredients = model.Ingredients,
                    Instructions = model.Instructions,
                    MealType = model.MealType,
                    PrepTimeMinutes = model.PrepTimeMinutes,
                    Rating = model.Rating,
                    Tags = model.Tags,
                    Vector = await gen.GenerateVectorAsync(input, cancellationToken: ct),
                }, ct);

                swi.Stop();
                logger.LogInformation($"Data '{id}' upserted ({swi.Elapsed} elapsed)");
                ids.Add(id);
            }

            sw.Stop();
            return new()
            {
                IsSuccess = ids.Count > 0,
                Message = $"{ids.Count} data upserted ({sw.Elapsed} elapsed)"
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

    public async IAsyncEnumerable<string> QueryRecipeAsync(EmbeddingQueryRequest req, [EnumeratorCancellation] CancellationToken ct)
    {
        var coll = store.GetCollection<ulong, RecipeVectorModel>(COLL_RECIPE);
        await coll.CreateCollectionIfNotExistsAsync(ct);

        var vector = await gen.GenerateVectorAsync(req.Prompt, cancellationToken: ct);

        var result = coll.SearchEmbeddingAsync(vector, req.Top, cancellationToken: ct);

        await foreach (var r in result)
        {
            var val = $"[{r.Record.Id}] {r.Record.Name}, {r.Record.Difficulty} [{r.Score}]";
            yield return val;
        }
    }


    #endregion
}
