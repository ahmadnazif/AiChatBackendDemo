namespace AiChatBackend.Models;

public class DummyjsonRecipeResponse
{
    public List<RecipeVectorModelBase> Recipes { get; set; }
    public int Total { get; set; }
    public int Skip { get; set; }
    public int Limit { get; set; }
}
