using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;

namespace AiChatBackend.Controllers;

[Route("rest-api/info")]
[ApiController]
public class InfoController(IConfiguration config, IHttpClientFactory http) : ControllerBase
{
    private readonly IConfiguration config = config;
    private readonly IHttpClientFactory http = http;

    [HttpGet("get-ai-runtime-info")]
    public async Task<ActionResult<object>> GetAiRuntimeInfo()
    {
        const string Ollama = "Ollama";
        var ollamaEndpoint = config[$"{Ollama}:Endpoint"];

        var httpClient = http.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(3);
        var resp = await httpClient.GetAsync(ollamaEndpoint);
        var serverStatus = resp.IsSuccessStatusCode ? "Running" : "Not running";

        return new
        {
            OllamaEndpoint = ollamaEndpoint,
            Model = config[$"{Ollama}:Model"],
            ServerStatus = serverStatus
        };
    }
}
