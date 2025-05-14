using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;

namespace AiChatBackend.Services;

public class InMemoryVectorDb(ILogger<InMemoryVectorDb> logger, OllamaEmbeddingGenerator gen, InMemoryVectorStore store)
{
    private readonly ILogger<InMemoryVectorDb> logger = logger;
    private readonly OllamaEmbeddingGenerator gen = gen;
    private readonly InMemoryVectorStore store = store;
    private const string COLL_TEXT = "text";

    public async Task<ResponseBase> UpsertTextAsync(TextVectorBase data, CancellationToken ct)
    {
        try
        {
            var coll = store.GetCollection<Guid, TextVectorBase>(COLL_TEXT);
            await coll.CreateCollectionIfNotExistsAsync(ct);

            var id = await coll.UpsertAsync(new TextVector
            {
                Id = Guid.NewGuid(),
                Text = data.Text,
                Vector = await gen.GenerateVectorAsync(data.Text)
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

    public async IAsyncEnumerable<TextSimilarityResult> QueryTextSimilarityAsync(string text, CancellationToken ct)
    {
        var coll = store.GetCollection<Guid, TextVectorBase>(COLL_TEXT);
        await coll.CreateCollectionIfNotExistsAsync(ct);

        var vector = await gen.GenerateVectorAsync(text, cancellationToken: ct);

        var result = coll.SearchEmbeddingAsync(vector, 5, cancellationToken: ct);

        await foreach(var r in result)
        {
            r.
        }
    }
}
