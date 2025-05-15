namespace AiChatBackend.Caches;

public class TextSimilarityCache
{
    private readonly Dictionary<Guid, TextVector> dict = [];

    public void Add(TextVector data) => dict.Add(data.Id, data);

    public void Remove(Guid key) => dict.Remove(key);

    public void Clear() => dict.Clear();

    public List<TextVector> ListAll() => [.. dict.Values];
}
