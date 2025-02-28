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
        var baseurl = config[$"{Ollama}:Endpoint"];    

        return new
        {
            Ollama = baseurl,
            OllamaStatus = await GetServerStatusAsync(baseurl),
            TagsEndpoint = await CallOllamaTagsAsync(baseurl),
            PsEndpoint = await CallOllamaPsAsync(baseurl),
        };
    }

    private async Task<OllamaTagsResponse> CallOllamaTagsAsync(string baseurl)
    {
        var httpClient = http.CreateClient();
        httpClient.BaseAddress = new(baseurl);
        httpClient.Timeout = httpClientTimeout;
        var resp = await httpClient.GetAsync("api/tags");
        return resp.IsSuccessStatusCode ? await resp.Content.ReadFromJsonAsync<OllamaTagsResponse>() : null;
    }

    private async Task<OllamaPsResponse> CallOllamaPsAsync(string baseurl)
    {
        var httpClient = http.CreateClient();
        httpClient.BaseAddress = new(baseurl);
        httpClient.Timeout = httpClientTimeout;
        var resp = await httpClient.GetAsync("api/ps");
        return resp.IsSuccessStatusCode ? await resp.Content.ReadFromJsonAsync<OllamaPsResponse>() : null;
    }

    private async Task<string> GetServerStatusAsync(string baseurl)
    {
        var httpClient = http.CreateClient();
        httpClient.BaseAddress = new(baseurl);
        httpClient.Timeout = httpClientTimeout;
        var resp = await httpClient.GetAsync(string.Empty);
        return resp.IsSuccessStatusCode ? "Running": "Not running";
    }
}
