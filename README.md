# AI Chat Backend Demo
This is backend API powered by new `Microsoft.Extensions.AI`, with Ollama as generative AI runtime.

> Go to the [frontend app](https://github.com/ahmadnazif/AiChatFrontendDemo)

## Requirements
#### Ollama runtime
- Ollama runtime must be installed.
  - You can directly install on Windows, MacOS or Linux [here](https://ollama.com/download)
  - You can also pull from Docker Hub using Docker Desktop. Instruction [here](https://hub.docker.com/r/ollama/ollama)
- Once done, verify by going to http://localhost:11434
#### Models
- This demo uses different kind of models & must be installed:
  -  Text model: Llama3.2 `ollama pull llama3.2`
  -  Vision model: llava `ollama pull llava`
  -  Embedding model: nomic-embed-text `ollama pull nomic-embed-text`
- Run the models by using `ollama run <model_name>` to test the model, or the model will automatically run when you use the `LlmService` service in this API.
- Verify the running model with command `ollama ps` (in CMD, PS, terminal or Docker Containter "Exec" tab).
#### Vector database
Vector database is needed for RAG (Retrieval-Augmented Generation). We use In-Memory vector database & awesome [Qdrant](https://qdrant.tech/) database for this.
- For RAG that experiments on text similarity analysis, in-memory database will be used via [Semantic Kernel In-Memory connector](https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/out-of-the-box-connectors/inmemory-connector?pivots=programming-language-csharp). It automatically running when application starts.
- For RAG that experiments on more advanced data (list of recipe), Qdrant database will be used via [Semantic Kernel Qdrant connector](https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/out-of-the-box-connectors/qdrant-connector?pivots=programming-language-csharp). This library built on top of official [Qdrant .NET client](https://github.com/qdrant/qdrant-dotnet). This needs running Qdrant instance, and you can install it in your local machine using [Docker](https://qdrant.tech/documentation/quickstart/) -- The easiest way to running Qdrant locally.
  - Qdrant typically run on:
    - API: [http://localhost:6333](http://localhost:6333),
    - Portal: [http://localhost:6333/dashboard](http://localhost:6333/dashboard)

## Running the app
- .NET 8 SDK must be installed to debug the code.
- Pull the source code, then open it using Visual Studio 2022.
- This app is host using Kestrel server on port 4444 and you can change it on `appsettings.json`
- Navigate inside the project directory
- Run the app: `dotnet run`
- Navigate to http://localhost:4444 in your browser. The default page should be Swagger UI 
  
## App technology & dependency
- ASP.NET Core API (.NET 8 LTS)
- SignalR
- MsgPack serialization for SignalR
- Microsoft.Extensions.AI (preview)
- Microsoft.SemanticKernel storage connectors (preview)
- Qdrant.Client (official Qdrant client)

## Misc
- To test the chat, you can directly call `api/chat` endpoint in Swagger UI or in your browser
- Checkout the other project: *AiChatFrontend* that connected to this API, that built using Blazor WebAssembly with SignalR
