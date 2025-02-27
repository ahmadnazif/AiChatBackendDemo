using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;

namespace AiChatBackend.Controllers;

[Route("rest-api/info")]
[ApiController]
public class InfoController(IConfiguration config, IHttpClientFactory http) : ControllerBase
{
    private readonly IConfiguration config = config;
    private readonly IHttpClientFactory http = http;
    private readonly TimeSpan httpClientTimeout = TimeSpan.FromSeconds(3);

    [HttpGet("get-ai-runtime-info")]
    public async Task<ActionResult<object>> GetAiRuntimeInfo()
    {
        const string Ollama = "Ollama";
        var ollamaEndpoint = config[$"{Ollama}:Endpoint"];

        return new
        {
            OllamaEndpoint = ollamaEndpoint,
            LocalModel = await CallApiAsync(ollamaEndpoint, "api/tags"), //config[$"{Ollama}:Model"],
            RunningModel = await CallApiAsync(ollamaEndpoint, "api/ps"),
            ServerStatus = await GetServerStatusAsync(ollamaEndpoint)
        };
    }

    private async Task<object> CallApiAsync(string ollamaEndpoint, string route)
    {
        var httpClient = http.CreateClient();
        httpClient.BaseAddress = new(ollamaEndpoint);
        httpClient.Timeout = httpClientTimeout;
        var resp = await httpClient.GetAsync(route);
        return resp.IsSuccessStatusCode ? await resp.Content.ReadAsStringAsync() : "Server not running";
    }

    private async Task<string> GetServerStatusAsync(string ollamaEndpoint)
    {
        var httpClient = http.CreateClient();
        httpClient.BaseAddress = new(ollamaEndpoint);
        httpClient.Timeout = httpClientTimeout;
        var resp = await httpClient.GetAsync(string.Empty);
        return resp.IsSuccessStatusCode ? "Running": "Not running";
    }
}
