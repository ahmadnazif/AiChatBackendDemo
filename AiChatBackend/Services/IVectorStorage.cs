using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;

namespace AiChatBackend.Services;

[Obsolete("Use service directly")]
public interface IVectorStorage
{
    //Task<Embedding<float>> GenerateEmbeddingAsync(string text, CancellationToken ct = default);
    //IAsyncEnumerable<string> ListCollectionNamesAsync(CancellationToken ct = default);

    #region Recipe
    Task<ResponseBase> UpsertRecipesAsync(List<RecipeVectorModelBase> data, CancellationToken ct = default);
    Task<string> QueryRecipeV1Async(EmbeddingQueryRequest req, CancellationToken ct = default);
    Task<string> QueryRecipeV2Async(string userPrompt, CancellationToken ct = default);
    #endregion
}
