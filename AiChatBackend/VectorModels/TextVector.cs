using Microsoft.Extensions.VectorData;
namespace AiChatBackend.VectorModels;

public class TextVector : TextVectorBase
{
    [VectorStoreKey] public Guid Id { get; set; }
    [VectorStoreVector(348, DistanceFunction = DistanceFunction.CosineSimilarity, IndexKind = IndexKind.Hnsw)] public ReadOnlyMemory<float> Vector { get; set; }
}

public class TextVectorBase
{
    [VectorStoreData] public string Text { get; set; }
}
