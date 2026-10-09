using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using DualBackendChat;
using DualBackendChat.Services;
using DualBackendChat.Tools;
using Microsoft.Extensions.AI;

namespace DualBackendChat;

class Program
{
    internal const string SystemPrompt = @"You are a Senior Software Architect.
You can use available tools and functions for your work.
When source changes are requested, request is completed only when all requested changes are committed.
When source changes are requested, they should be split into small, meaningful pieces.
Each source change should be committed with a relevant message.
Build & Quality: The solution must be buildable, and all warnings must be fixed. If a warning cannot be fixed, it must be documented in the 'Issues' paragraph of technical-design.md with an explanation of why it cannot be fixed.
Testing: All tests must be passing (green).
Documentation: The technical-design.md file (if it is present) must be updated to reflect the new state of the system.
Formatting: for dotnet projects and solutions dotnet format must be executed before committing changes.
Commit Message: The commit message must clearly reflect the changes that have been implemented. After excecuting commit command git repo should be verified commit is available, commit hash is present. 
Follow repo-specific rules in file AGENTS.md when it is present.
				";
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

        await chatEngine.AddMessageAsync(new ChatMessage(ChatRole.System, new[] { new TextContent(SystemPrompt) }));

        chatEngine.OnTextContent += (sender, args) =>
        {
            Console.WriteLine($"\nAssistant > {args.Parameter.Text}");
            return Task.CompletedTask;
        };

        chatEngine.OnFunctionCallContent += async (sender, args) =>
        {
            ContentEventArgs<FunctionCallContent> eventArgs = (ContentEventArgs<FunctionCallContent>)args;

            ChatEngine senderEngine = (ChatEngine)sender;
            try
            {
                Console.WriteLine($"\nRequested function call {eventArgs.Parameter.Name}");
                var task = tools.Single(t => t.Metadata.Name == eventArgs.Parameter.Name);
                var taskResult = await task.InvokeAsync(eventArgs.Parameter.Arguments);
                var functionResultContent = new FunctionResultContent(eventArgs.Parameter, taskResult, null);
                Console.WriteLine($"\nRequested function {eventArgs.Parameter.Name} result is  {taskResult}");
                await senderEngine.SendMessageAsync(new ChatMessage(ChatRole.Tool, new[] { functionResultContent }));
            }
            catch (Exception ex)
            {
                var functionExceptionResultContent = new FunctionResultContent(eventArgs.Parameter, null, ex);
                await senderEngine.SendMessageAsync(new ChatMessage(ChatRole.Tool, new[] { functionExceptionResultContent }));
            }
        };

        while (true)
        {
            Console.Write("\nUser > ");
            string input = Console.ReadLine() ?? "";

            if (string.IsNullOrWhiteSpace(input))
            {
                //input = "show me name of current directory";
                input = "show me list of available tools/functions";
            }

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
                await chatEngine.SendMessageAsync(input);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[Error] {ex.Message}");
            }
        }
    }
}
