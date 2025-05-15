namespace AiChatBackend.Models;

public class AutoPopulateStatementRequest
{
    public int Number { get; set; }
    public TextGenerationDifficultyLevel Difficulty { get; set; }
}
