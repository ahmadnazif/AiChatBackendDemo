using Microsoft.Extensions.VectorData;

namespace AiChatBackend.VectorModels;

public class FoodVectorModel : FoodVectorModelBase
{

    [VectorStoreRecordVector(384, DistanceFunction = DistanceFunction.CosineDistance)]
    public ReadOnlyMemory<float> Vector { get; set; }}

public class FoodVectorModelBase
{
    [VectorStoreRecordKey] public int Id { get; set; }

    [VectorStoreRecordData] public string FoodName { get; set; }

    [VectorStoreRecordData] public string Remarks { get; set; }
}
