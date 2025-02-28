using static System.Net.WebRequestMethods;

namespace AiChatBackend.Services;

public class ApiClient(ILogger<ApiClient> logger, IHttpClientFactory fac)
{
    private readonly ILogger<ApiClient> logger = logger;
    private readonly IHttpClientFactory fac = fac;
    private readonly TimeSpan httpClientTimeout = TimeSpan.FromSeconds(3);

    public async Task<OllamaTagsResponse> GetOllamaTagsAsync(string baseurl)
    {
        try
        {
            var httpClient = fac.CreateClient();
            httpClient.BaseAddress = new(baseurl);
            httpClient.Timeout = httpClientTimeout;
            var resp = await httpClient.GetAsync("api/tags");
            return resp.IsSuccessStatusCode ? await resp.Content.ReadFromJsonAsync<OllamaTagsResponse>() : null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
            return null;
        }
    }

    public async Task<OllamaPsResponse> GetOllamaPsAsync(string baseurl)
    {
        try
        {
            var httpClient = fac.CreateClient();
            httpClient.BaseAddress = new(baseurl);
            httpClient.Timeout = httpClientTimeout;
            var resp = await httpClient.GetAsync("api/ps");
            return resp.IsSuccessStatusCode ? await resp.Content.ReadFromJsonAsync<OllamaPsResponse>() : null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
            return null;
        }
    }

    public async Task<string> GetServerStatusAsync(string baseurl)
    {
        try
        {
            var httpClient = fac.CreateClient();
            httpClient.BaseAddress = new(baseurl);
            httpClient.Timeout = httpClientTimeout;
            var resp = await httpClient.GetAsync(string.Empty);
            return resp.IsSuccessStatusCode ? "Running" : "Not running";
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
            return "Error";
        }
    }
}
