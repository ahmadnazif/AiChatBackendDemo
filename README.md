# AI Chat Backend Demo
This is backend API powered by new `Microsoft.Extensions.AI`, with Ollama as generative AI runtime & Llama 3.2 model with 3.2B paramters. You can always use [other](https://ollama.com/search) powerful model depending on your PC specification.

## Requirements
#### Ollama runtime & LLama3.2 model
- Ollama runtime must be installed.
  - You can directly install on Windows, MacOS or Linux [here](https://ollama.com/download)
  - You can also pull from Docker Hub using Docker Desktop. Instruction [here](https://hub.docker.com/r/ollama/ollama)
- Once done, verify by going to http://localhost:11434
- Run the "Llama3.2" model by command `ollama run llama3.2`. This will pull the model to your PC (or Docker)
- Verify the model with command `ollama ps` (in CMD, PS, terminal or Docker Containter "Exec" tab.

## Testing the app
- .NET 8 SDK must be installed to debug the code.
- Pull the source code, then open it using Visual Studio 2022.
- This app is host using Kestrel server on port 4444 and you can change it on `appsettings.json`
- Run the app: `dotnet run`
- Navigate to http://localhost:4444 in your browser. The default page should be Swagger UI 
  
## App technology & dependency
- ASP.NET Core API (.NET 8 LTS)
- SignalR
- MsgPack serialization for SignalR
- Microsoft.Extensions.AI (preview)

## Misc
- To test the chat, you can directly call `api/chat` endpoint in Swagger UI or in your browser
- Checkout the other project: *AiChatFrontend* that connected to this API, that built using Blazor WebAssembly with SignalR
