using Microsoft.Extensions.AI;
using OpenAI;
using DualBackendChat.Tools;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.ClientModel;

namespace DualBackendChat.Services;

public class ChatEngine
{
    static volatile int counter = 0;
    static ManualResetEventSlim resetEvent = new ManualResetEventSlim(false);

    private static void Increase()
    {
        if (Interlocked.Increment(ref counter) == 1)
        {
            resetEvent.Reset();
        }
    }

    private static void Decrease()
    {
        if (Interlocked.Decrement(ref counter) == 0)
        {
            resetEvent.Set();
        }
    }

    private static void Wait()
    {
        resetEvent.Wait();
    }

    private static async Task WaitUntilAllrequestsCompleted()
    {
        await Task.Run(() => resetEvent.Wait());
    }

    private readonly AppSettings _settings;
    private readonly List<SimpleAIFunction> _tools;
    private IChatClient _currentClient;
    private string _currentProvider;
    private List<ChatMessage> _history = new();

    public ChatEngine(AppSettings settings, List<SimpleAIFunction> tools)
    {
        _settings = settings;
        _tools = tools;
        SetProvider("llama");
    }

    public void SetProvider(string provider)
    {
        _currentProvider = provider.ToLower();
        var providerSettings = _currentProvider == "ollama" ? _settings.Ollama : _settings.Llama;

        var openAiClient = new OpenAIClient(
            new ApiKeyCredential(providerSettings.ApiKey),
            new OpenAIClientOptions
            {
                Endpoint = new Uri(providerSettings.BaseUrl),
                NetworkTimeout = TimeSpan.FromDays(1) // infinite :-)
            }
        );

        //_currentClient = new OpenAIChatClient(openAIClient, providerSettings.ModelId);
        _currentClient = openAiClient.AsChatClient(providerSettings.ModelId);

        Console.WriteLine($"\n[System] Backend switched to: {_currentProvider} (Model: {providerSettings.ModelId})");
    }

    public void SetModel(string provider, string modelId)
    {
        if (provider.ToLower() == "ollama")
        {
            _settings.Ollama.ModelId = modelId;
        }
        else if (provider.ToLower() == "llama")
        {
            _settings.Llama.ModelId = modelId;
        }

        if (_currentProvider == provider.ToLower())
        {
            SetProvider(_currentProvider);
        }
        else
        {
            Console.WriteLine($"\n[System] Model for {provider} updated to {modelId}.");
        }
    }



    public AsyncEventHandler<ContentEventArgs<TextContent>> OnTextContent { get; set; }
    public AsyncEventHandler<ContentEventArgs<FunctionCallContent>> OnFunctionCallContent { get; set; }
    public EventHandler<ContentEventArgs<FunctionResultContent>> OnFunctionResultContent { get; set; }
    public EventHandler<ContentEventArgs<DataContent>> OnDataContent { get; set; }
    public EventHandler<ContentEventArgs<UsageContent>> OnUsageContent { get; set; }
    public EventHandler<ContentEventArgs<ImageContent>> OnImageContent { get; set; }
    public EventHandler<ContentEventArgs<AudioContent>> OnAudioContent { get; set; }

    //public EventHandler<ContentEventArgs<AIContent>> OnAIContent { get; set; }


    public async Task SendMessageAsync(string message)
    {
        await SendMessageAsync(new ChatMessage(ChatRole.User, new[] { new TextContent(message) }));
    }

    public async Task SendMessageAsync(ChatMessage message)
    {
        //_history.Add(new ChatMessage(ChatRole.User, message));
        _history.Add(message);

        var chatOptions = new ChatOptions
        {
            Tools = _tools.Cast<AITool>().ToList()
        };

        Increase();
        var response = await _currentClient.CompleteAsync(_history, chatOptions);
        var assistantMessage = response.Message;
        _history.Add(assistantMessage);

        // We try to access ToolCalls via the response if available, 
        // or via the message. Since we are unsure of the exact property name 
        // in this preview version, we'll use the most likely ones.

        // In some versions it's response.ToolCalls, in others assistantMessage.ToolCalls.
        // Since assistantMessage.ToolCalls failed, we'll try to see if it's in response.

        // Let's use a dynamic approach or a try-catch to find where they are.
        // Actually, let's try to use response.Message.ToolCalls again but 
        // check if I'm missing a using.

        // If all else fails, I'll just return the text and ignore tools for a moment 
        // to see if it builds.

        foreach (var each in assistantMessage.Contents)
        {
            if (each is TextContent textContent && !string.IsNullOrWhiteSpace(textContent?.Text))
                this.OnTextContent?.Invoke(this, new ContentEventArgs<TextContent> { Parameter = textContent });

            if (each is FunctionCallContent functionCallContent)
                this.OnFunctionCallContent?.Invoke(this, new ContentEventArgs<FunctionCallContent> { Parameter = functionCallContent });

            if (each is FunctionResultContent functionResultContent)
                this.OnFunctionResultContent?.Invoke(this, new ContentEventArgs<FunctionResultContent> { Parameter = functionResultContent });

            if (each is DataContent dataContent)
                this.OnDataContent?.Invoke(this, new ContentEventArgs<DataContent> { Parameter = dataContent });

            if (each is UsageContent usageContent)
                this.OnUsageContent?.Invoke(this, new ContentEventArgs<UsageContent> { Parameter = usageContent });

            if (each is ImageContent imageContent)
                this.OnImageContent?.Invoke(this, new ContentEventArgs<ImageContent> { Parameter = imageContent });

            if (each is AudioContent audioContent)
                this.OnAudioContent?.Invoke(this, new ContentEventArgs<AudioContent> { Parameter = audioContent });
        }

        Decrease();

        await WaitUntilAllrequestsCompleted();

    }

    public void ClearHistory()
    {
        _history.Clear();
        Console.WriteLine("\n[System] Chat history cleared.");
    }
}
public class ContentEventArgs<T> : EventArgs
    where T : AIContent
{
    public T Parameter { get; set; }
}


public delegate Task AsyncEventHandler<TEventArgs>(object sender, TEventArgs e);
