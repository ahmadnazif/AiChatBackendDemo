global using System.Text.Json;
global using AiChatBackend.Enums;
global using AiChatBackend.Models;
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

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.WebHost.ConfigureKestrel(x =>
{
    var port = int.Parse(config["Port"]);
    x.ListenAnyIP(port);
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
