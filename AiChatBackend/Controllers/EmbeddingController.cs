using AiChatBackend.Caches;
using AiChatBackend.Services;
using Microsoft.AspNetCore.Mvc;
using OllamaSharp.Models;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AiChatBackend.Controllers;

[Route($"{BASE_ROUTE}/embedding")]
[ApiController]
public class EmbeddingController(
    ILogger<EmbeddingController> logger, LlmService llm) : ControllerBase
{
    private readonly ILogger<EmbeddingController> logger = logger;
    private readonly LlmService llm = llm;

    [HttpPost("generate-embedding")]
    public async Task<ActionResult<ReadOnlyMemory<float>>> GenerateEmbedding([FromBody] string text, CancellationToken ct)
    {
        return await llm.GenerateVectorAsync(text, ct);
    }
}
