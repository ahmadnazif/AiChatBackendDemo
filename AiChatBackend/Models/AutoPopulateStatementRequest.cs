namespace AiChatBackend.Models;

public class AutoPopulateStatementRequest
{
    public string ModelId { get; set; }
    public int Number { get; set; }
    public TextGenerationLength Length { get; set; }
    public string Topic { get; set; }
    public string Language { get; set; }
}
