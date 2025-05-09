using Microsoft.Extensions.VectorData;
namespace AiChatBackend.VectorModels;

public class TextVector
{
    [VectorStoreRecordKey]
    public ulong Id { get; set; }

    //[VectorStoreRecordData(IsIndexed = true, StoragePropertyName = "text_label")]
    [VectorStoreRecordData] public string Label { get; set; }

    //[VectorStoreRecordData(IsFullTextIndexed = true, StoragePropertyName = "text_remarks")]
    [VectorStoreRecordData]    public string Remarks { get; set; }

    //[VectorStoreRecordVector(4, DistanceFunction = DistanceFunction.CosineSimilarity, IndexKind = IndexKind.Hnsw, StoragePropertyName = "text_vector")]
    [VectorStoreRecordVector(384, DistanceFunction = DistanceFunction.CosineDistance)]
    public ReadOnlyMemory<float> Vector { get; set; }
}
