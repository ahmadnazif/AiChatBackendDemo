using Microsoft.Extensions.VectorData;

namespace AiChatBackend.VectorModels;

public class FoodVectorModel : FoodVectorModelBase
{
    [VectorStoreRecordKey] public Guid Id { get; set; }

    [VectorStoreRecordVector(768, DistanceFunction = DistanceFunction.CosineSimilarity, IndexKind = IndexKind.Hnsw)]
    public ReadOnlyMemory<float> Vector { get; set; }}

public class FoodVectorModelBase
{

    [VectorStoreRecordData] public string FoodName { get; set; }

    [VectorStoreRecordData] public string Remarks { get; set; }
}
