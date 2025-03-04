namespace AiChatBackend.Helpers;

public static class Generator
{
    private static readonly Random random;

    static Generator() => random = new Random();

    public static string NextId() => Guid.NewGuid().ToString("N").ToUpper();
    public static string NextStreamingId() => $"A-{NextId()}";
}
