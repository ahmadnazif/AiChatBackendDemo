using Microsoft.Extensions.VectorData;

namespace AiChatBackend.VectorModels;

public class RecipeVectorModel : RecipeVectorModelBase
{
    [VectorStoreRecordVector(768, DistanceFunction = DistanceFunction.CosineSimilarity, IndexKind = IndexKind.Hnsw)] public ReadOnlyMemory<float> Vector { get; set; }
}

public class RecipeVectorModelBase
{
    [VectorStoreRecordKey] public ulong Id { get; set; }
    [VectorStoreRecordData] public string Name { get; set; }
    [VectorStoreRecordData] public List<string> Ingredients { get; set; }
    [VectorStoreRecordData] public List<string> Instructions { get; set; }
    [VectorStoreRecordData] public int PrepTimeMinutes { get; set; }
    [VectorStoreRecordData] public int CookTimeMinutes { get; set; }
    [VectorStoreRecordData] public string Difficulty { get; set; }
    [VectorStoreRecordData] public string Cuisine { get; set; }
    [VectorStoreRecordData] public int CaloriesPerServing { get; set; }
    [VectorStoreRecordData] public List<string> Tags { get; set; }
    [VectorStoreRecordData] public string Image { get; set; }
    [VectorStoreRecordData] public float Rating { get; set; }
    [VectorStoreRecordData] public List<string> MealType { get; set; }
}
