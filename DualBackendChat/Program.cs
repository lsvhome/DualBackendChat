using System;
using System.Threading.Tasks;
using DualBackendChat;
using DualBackendChat.Services;
using DualBackendChat.Tools;

namespace DualBackendChat;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Welcome to Dual Backend Chat!");
        Console.WriteLine("Commands: /provider <name>, /model <provider> <id>, /clear, /exit");
        Console.WriteLine("--------------------------------------------------");

        var settings = new AppSettings();
        var toolInstance = new FileSystemTools(settings);
        var toolRegistrationService = new ToolRegistrationService();
        var tools = toolRegistrationService.RegisterTools(toolInstance);

        var chatEngine = new ChatEngine(settings, tools);

        while (true)
        {
            Console.Write("\nUser > ");
            string input = Console.ReadLine() ?? "";

            if (string.IsNullOrWhiteSpace(input)) continue;

            if (input.StartsWith("/"))
            {
                var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var command = parts[0].ToLower();

                switch (command)
                {
                    case "/provider":
                        if (parts.Length < 2)
                        {
                            Console.WriteLine("Usage: /provider <ollama|llama>");
                        }
                        else
                        {
                            chatEngine.SetProvider(parts[1]);
                        }
                        break;

                    case "/model":
                        if (parts.Length < 3)
                        {
                            Console.WriteLine("Usage: /model <ollama|llama> <model_id>");
                        }
                        else
                        {
                            chatEngine.SetModel(parts[1], parts[2]);
                        }
                        break;

                    case "/clear":
                        chatEngine.ClearHistory();
                        break;

                    case "/exit":
                        Console.WriteLine("Exiting...");
                        return;

                    default:
                        Console.WriteLine($"Unknown command: {command}");
                        break;
                }
                continue;
            }

            try
            {
                string response = await chatEngine.SendMessageAsync(input);
                Console.WriteLine($"\nAssistant > {response}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[Error] {ex.Message}");
            }
        }
    }
}
