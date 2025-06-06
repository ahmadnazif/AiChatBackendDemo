global using System.Text.Json;
global using AiChatBackend.Enums;
global using AiChatBackend.Models;
global using AiChatBackend.VectorModels;
global using static AiChatBackend.Constants;
global using AiChatBackend.Helpers;
using AiChatBackend.Caches;
using AiChatBackend.Hubs;
using AiChatBackend.ServiceExtensions;
using Microsoft.Extensions.AI;
using AiChatBackend.Services;
using Microsoft.SemanticKernel;
using Qdrant.Client;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

builder.Services.AddSingleton<TextAnalysisCache>();

builder.Services.AddChatClient(x =>
{
    const string ollama = "Ollama";
    var endpoint = config[$"{ollama}:Endpoint"];
    //var textModel = config[$"{ollama}:TextModel"];
    //var visionModel = config[$"{ollama}:VisionModel"];
    //var multimodal = config[$"{ollama}:MultimodalModel"];

    var textModel = LlmModelHelper.GetModel(config, LlmModelType.Text);
    return new OllamaChatClient(endpoint, textModel.DefaultModelId);
});

// Vector DB: Qdrant
builder.Services.AddQdrantVectorStore(config["Qdrant:Host"], int.Parse(config["Qdrant:Port"]));
//builder.Services.AddSingleton(sp => new QdrantClient(config["Qdrant:Host"], int.Parse(config["Qdrant:Port"])));
//builder.Services.AddQdrantVectorStore();
builder.Services.AddScoped<QdrantDb>();

// Vector DB: InMemory
builder.Services.AddInMemoryVectorStore();
builder.Services.AddScoped<InMemoryVectorDb>();

//#pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
//builder.Services.AddOllamaTextEmbeddingGeneration(config["Ollama:EmbeddingModel"], new Uri(config[$"Ollama:Endpoint"]));
//builder.Services.AddTransient(sp => new Kernel(sp));

builder.Services.AddSingleton(x =>
{
    const string ollama = "Ollama";
    var endpoint = config[$"{ollama}:Endpoint"];
    var model = config[$"{ollama}:EmbeddingModel"];

    return new OllamaEmbeddingGenerator(endpoint, model);
});

builder.Services.AddScoped<LlmService>();

builder.Services.AddHttpClient();
builder.Services.AddScoped<ApiClient>();

builder.Services.AddSingleton<IHubUserCache, HubUserCache>();
builder.Services.AddSignalR(x =>
{
    x.MaximumReceiveMessageSize = null;
}).AddMessagePackProtocol();
builder.Services.AddSignalrUserIdentifier();

builder.Services.AddCors(x => x.AddDefaultPolicy(y => y.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.WebHost.ConfigureKestrel(x =>
{
    var port = int.Parse(config["Port"]);
    x.ListenAnyIP(port);
});

var app = builder.Build();

app.UseCors();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();

app.MapControllers();

app.MapHub<ChatHub>($"/{BASE_ROUTE}/chat-hub");

await app.RunAsync();
