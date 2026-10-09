namespace DualBackendChat;

public class AppSettings
{
    public ProviderSettings Ollama { get; set; } = new()
    {
        BaseUrl = "http://localhost:11434/v1",
        ModelId = "gemma4-dev:latest",
        ApiKey = "ollama"
    };

    public ProviderSettings Llama { get; set; } = new()
    {
        BaseUrl = "http://localhost:8080/v1",
        //ModelId = "llama-model",
        ModelId = "unsloth/gemma-4-31B-it-GGUF",
        ApiKey = "llama"
    };

    public string SandboxRoot { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "sandbox");
    public bool EnableRiskyTools { get; set; } = false;
}

public class ProviderSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}
