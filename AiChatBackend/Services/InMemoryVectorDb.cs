using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;
using System.Runtime.CompilerServices;

namespace AiChatBackend.Services;

public class InMemoryVectorDb(ILogger<InMemoryVectorDb> logger, OllamaEmbeddingGenerator gen, InMemoryVectorStore store)
{
    private readonly ILogger<InMemoryVectorDb> logger = logger;
    private readonly OllamaEmbeddingGenerator gen = gen;
    private readonly InMemoryVectorStore store = store;
    private const string COLL_TEXT = "text";

    public async Task<ResponseBase> UpsertTextAsync(string text, CancellationToken ct)
    {
        try
        {
            var coll = store.GetCollection<Guid, TextVector>(COLL_TEXT);
            await coll.CreateCollectionIfNotExistsAsync(ct);

            var id = await coll.UpsertAsync(new TextVector
            {
                Id = Guid.NewGuid(),
                Text = text,
                Vector = await gen.GenerateVectorAsync(text, cancellationToken: ct)
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

    public async IAsyncEnumerable<TextSimilarityResult> QueryTextSimilarityAsync(string text, [EnumeratorCancellation] CancellationToken ct)
    {
        var coll = store.GetCollection<Guid, TextVector>(COLL_TEXT);
        await coll.CreateCollectionIfNotExistsAsync(ct);

        var vector = await gen.GenerateVectorAsync(text, cancellationToken: ct);

        var result = coll.SearchEmbeddingAsync(vector, 5, cancellationToken: ct);

        await foreach (var r in result)
        {
            yield return new()
            {
                Guid = r.Record.Id.ToString(),
                Text = r.Record.Text,
                Score = r.Score.Value
            };
        }
    }
}
