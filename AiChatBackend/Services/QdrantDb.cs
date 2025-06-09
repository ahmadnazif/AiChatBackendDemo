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
using System.Text;

namespace AiChatBackend.Services;

public class QdrantDb(ILogger<QdrantDb> logger, QdrantVectorStore store, QdrantClient qdrant, LlmService llm)
{
    private readonly ILogger<QdrantDb> logger = logger;
    private readonly QdrantVectorStore store = store;
    private readonly QdrantClient qdrant = qdrant;
    private readonly LlmService llm = llm;

    private const string COLL_RECIPE = "recipe";

    #region Recipe

    private static string GenerateEmbeddingInputString(RecipeVectorModelBase model)
    {
        return $"""
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
    }

    public async Task<ResponseBase> UpsertRecipesAsync(List<RecipeVectorModelBase> data, CancellationToken ct)
    {
        try
        {
            var coll = store.GetCollection<ulong, RecipeVectorModel>(COLL_RECIPE);
            await coll.EnsureCollectionExistsAsync(ct);

            List<ulong> ids = [];

            Stopwatch sw = Stopwatch.StartNew();
            foreach (var model in data)
            {
                Stopwatch swi = Stopwatch.StartNew();
                await coll.UpsertAsync(new RecipeVectorModel
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
                    Vector = await llm.GenerateVectorAsync(GenerateEmbeddingInputString(model), ct),
                }, ct);

                swi.Stop();
                logger.LogInformation($"Data '{model.Id}' upserted ({swi.Elapsed} elapsed)");
                ids.Add(model.Id);
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

    public async IAsyncEnumerable<RecipeVdbQueryResult> QueryAsyncAsync(string text, int top, [EnumeratorCancellation] CancellationToken ct)
    {
        var coll = store.GetCollection<ulong, RecipeVectorModel>(COLL_RECIPE);
        await coll.EnsureCollectionExistsAsync(ct);

        logger.LogInformation("Generalize prompt as JSON via LLM..");
        var json = await llm.GeneralizeUserPromptAsJsonAsync(text);
        logger.LogInformation($"OUT = {json}");

        logger.LogInformation("Generating prompt as vector..");
        var vector = await llm.GenerateVectorAsync(json, ct);

        logger.LogInformation("Search in DB..");
        var result = coll.SearchAsync(vector, top, cancellationToken: ct);

        await foreach(var item in result)
        {
            logger.LogInformation($"{item.Record.Name} ({item.Score})");
            yield return new()
            {
                 Id = item.Record.Id,
                 Text = item.Record.Name,
                 Score = item.Score.Value
            };
        }
    }

    public async Task<string> QueryRecipeV1Async(EmbeddingQueryRequest req, CancellationToken ct)
    {
        var coll = store.GetCollection<ulong, RecipeVectorModel>(COLL_RECIPE);
        await coll.EnsureCollectionExistsAsync(ct);

        logger.LogInformation("Generating prompt as vector..");
        var vector = await llm.GenerateVectorAsync(req.Prompt, ct);

        logger.LogInformation("Search in DB..");
        var result = coll.SearchAsync(vector, req.Top, cancellationToken: ct);

        // 1: Build context
        // -----------------
        logger.LogInformation("Building context from result..");
        StringBuilder sb = new();
        await foreach (var r in result)
        {
            sb.AppendLine(GenerateEmbeddingInputString(r.Record));
            sb.AppendLine();
        }

        // 2: Compose prompt to LLM
        // -------------------------

        var prompt = $"""
            You are a helpful recipe assistant.
            Here are some recipes from the database:
            {sb}
            Answer this user query using information above:
            "{req.Prompt}"
            """;

        logger.LogInformation("Sending to LLM for processing..");
        var resp = await llm.GetResponseTextAsync(prompt);
        logger.LogInformation($"Response generated");

        return resp;

        //// KIV
        //await foreach (var r in result)
        //{
        //    var val = $"[{r.Record.Id}] {r.Record.Name}, {r.Record.Difficulty} [{r.Score}]";
        //    yield return val;
        //}
    }

    public async Task<string> QueryRecipeV2Async(string userPrompt, CancellationToken ct)
    {
        var coll = store.GetCollection<ulong, RecipeVectorModel>(COLL_RECIPE);
        await coll.EnsureCollectionExistsAsync(ct);

        // 1: Generalize prompt as JSON
        // -----------------------------
        logger.LogInformation("Generalize prompt as JSON via LLM..");
        var json = await llm.GeneralizeUserPromptAsJsonAsync(userPrompt);
        logger.LogInformation($"OUT = {json}");

        logger.LogInformation("Generating prompt as vector..");
        var vector = await llm.GenerateVectorAsync(json, ct);

        logger.LogInformation("Search in DB..");
        var result = coll.SearchAsync(vector, 5, cancellationToken: ct);

        // 1: Build context
        // -----------------
        logger.LogInformation("Building context from result..");
        StringBuilder sb = new();
        await foreach (var r in result)
        {
            logger.LogInformation($"IN-DB: {r.Record.Name} (Cuisine: {r.Record.Cuisine}, Cal: {r.Record.CaloriesPerServing})");
            sb.AppendLine(GenerateEmbeddingInputString(r.Record));
            sb.AppendLine();
        }

        // 2: Compose prompt to LLM
        // -------------------------

        var prompt = $"""
            You are a helpful cooking assistant.

            A user asked: "{userPrompt}"

            Based on the internal search, here are some relevant recipes:
            {sb}

            Using the above information, answer the user's question as helpfully as possible.
            If none of the recipes fit, say so.
            """;

        logger.LogInformation("Sending to LLM for processing..");
        var resp = await llm.GetResponseTextAsync(prompt);
        logger.LogInformation($"Response generated");

        return resp;
    }

    public async Task<ulong> CountRecipeAsync(CancellationToken ct)
    {
        var coll = store.GetCollection<ulong, RecipeVectorModel>(COLL_RECIPE);
        await coll.EnsureCollectionExistsAsync(ct);

        return await qdrant.CountAsync(COLL_RECIPE, cancellationToken: ct);
    }

    #endregion
}
