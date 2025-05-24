namespace AiChatBackend.Helpers;

public static class LlmModelHelper
{
    public static LlmModel GetModel(IConfiguration config, LlmModelType modelType)
    {
        var runtime = "Ollama";
        var name = $"{runtime}:{modelType}Models";
        var defaultModel = config[$"{name}:Default"];
        var models = config.GetSection($"{name}:Models").Get<string[]>();

        return new()
        {
            ModelType = modelType,
            Default = defaultModel,
            Models = [.. models]
        };
    }
}
