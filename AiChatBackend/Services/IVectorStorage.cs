using Microsoft.Extensions.AI;

namespace AiChatBackend.Services;

public interface IVectorStorage
{
    //Task<Embedding<float>> GenerateEmbeddingAsync(string text, CancellationToken ct = default);
    Task<ResponseBase> UpsertFoodAsync(FoodVectorModelBase food, CancellationToken ct = default);
    //Task<ResponseBase> UpsertFoodsAsync(List<FoodVectorModelBase> foods, CancellationToken ct = default);
    Task QueryAsync(string prompt, CancellationToken ct = default);
}
