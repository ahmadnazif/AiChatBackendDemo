﻿namespace AiChatBackend.Models;

[Obsolete("Use VdbRequest")]
public class TextAnalysisVdbRequest
{
    public string? Prompt { get; set; }
    public int Top { get; set; }
}
