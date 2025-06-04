using Microsoft.Extensions.VectorData;

namespace AiChatBackend.VectorModels;

public class RecipeVectorModel : RecipeVectorModelBase
{
    [VectorStoreVector(768, DistanceFunction = DistanceFunction.CosineSimilarity, IndexKind = IndexKind.Hnsw)] public ReadOnlyMemory<float> Vector { get; set; }
}

public class RecipeVectorModelBase
{
    [VectorStoreKey] public ulong Id { get; set; }
    [VectorStoreData] public string Name { get; set; }
    [VectorStoreData] public List<string> Ingredients { get; set; }
    [VectorStoreData] public List<string> Instructions { get; set; }
    [VectorStoreData] public int PrepTimeMinutes { get; set; }
    [VectorStoreData] public int CookTimeMinutes { get; set; }
    [VectorStoreData] public string Difficulty { get; set; }
    [VectorStoreData] public string Cuisine { get; set; }
    [VectorStoreData] public int CaloriesPerServing { get; set; }
    [VectorStoreData] public List<string> Tags { get; set; }
    [VectorStoreData] public string Image { get; set; }
    [VectorStoreData] public float Rating { get; set; }
    [VectorStoreData] public List<string> MealType { get; set; }
}
