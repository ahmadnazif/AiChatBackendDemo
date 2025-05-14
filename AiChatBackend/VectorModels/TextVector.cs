using Microsoft.Extensions.VectorData;
namespace AiChatBackend.VectorModels;

public class TextVector : TextVectorBase
{
    [VectorStoreRecordKey]
    public Guid Id { get; set; }
    [VectorStoreRecordVector(348, DistanceFunction = DistanceFunction.CosineSimilarity, IndexKind = IndexKind.Hnsw)] public ReadOnlyMemory<float> Vector { get; set; }
}

public class TextVectorBase
{
    [VectorStoreRecordData] public string Text { get; set; }
}
