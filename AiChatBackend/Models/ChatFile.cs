namespace AiChatBackend.Models;

public record ChatFile(string Filename, byte[] FileStream, string MediaType);
