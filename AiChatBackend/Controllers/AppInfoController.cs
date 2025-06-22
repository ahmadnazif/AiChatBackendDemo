using AiChatBackend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;

namespace AiChatBackend.Controllers;

[Route($"{BASE_ROUTE}/app-info")]
[ApiController]
public class AppInfoController(IConfiguration config, ApiClient api) : ControllerBase
{
    private readonly IConfiguration config = config;
    private readonly ApiClient api = api;

    [HttpGet("ok")] public ActionResult IsOk() => Ok();

    [HttpGet("get-ai-runtime-info")]
    public async Task<ActionResult<object>> GetAiRuntimeInfo()
    {
        const string Ollama = "Ollama";
        var baseurl = config[$"{Ollama}:Endpoint"];    

        return new
        {
            Ollama = baseurl,
            OllamaStatus = await api.GetServerStatusAsync(baseurl),
            TagsEndpoint = await api.GetOllamaTagsAsync(baseurl),
            PsEndpoint = await api.GetOllamaPsAsync(baseurl),
        };
    }

    
}
