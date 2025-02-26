global using System.Text.Json;
global using AiChatBackend.Enums;
global using AiChatBackend.Models;
using AiChatBackend.Caches;
using AiChatBackend.Hubs;
using AiChatBackend.ServiceExtensions;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

builder.Services.AddChatClient(x =>
{
    const string ollama = "Ollama";
    var endpoint = config[$"{ollama}:Endpoint"];
    var model = config[$"{ollama}:Model"];

    return new OllamaChatClient(endpoint, model);
});

builder.Services.AddSingleton<IHubUserCache, HubUserCache>();
builder.Services.AddSignalR().AddMessagePackProtocol();
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

app.MapHub<ChatHub>("/chat-hub");

await app.RunAsync();
