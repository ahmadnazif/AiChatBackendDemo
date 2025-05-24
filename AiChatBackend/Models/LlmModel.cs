namespace AiChatBackend.Models;

public class LlmModel
{
    public LlmModelType ModelType { get; set; }
    public List<string> Models { get; set; }
    public string? Default { get; set; }
}
