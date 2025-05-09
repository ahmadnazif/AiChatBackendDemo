using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AiChatBackend.Services;

public class InMemoryVectorDb(ILogger<InMemoryVectorDb> logger, OllamaEmbeddingGenerator gen) : IVectorStorage
{
    private readonly ILogger<InMemoryVectorDb> logger = logger;
    private readonly OllamaEmbeddingGenerator gen = gen;

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

    public async Task<> List()
    {
        //var store = new QdrantVectorStore(client);
        //var coll = store.GetCollection<ulong, TextVector>("text");
        //await coll.CreateCollectionIfNotExistsAsync(ct);

    }
}
