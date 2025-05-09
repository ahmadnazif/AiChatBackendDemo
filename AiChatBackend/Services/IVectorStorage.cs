using Microsoft.Extensions.AI;

namespace AiChatBackend.Services;

public interface IVectorStorage
{
    Task<Embedding<float>> GenerateEmbeddingAsync(string text, CancellationToken ct = default);
}
