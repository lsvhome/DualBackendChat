using System.Reflection;
using Microsoft.Extensions.AI;
using DualBackendChat.Tools;

namespace DualBackendChat.Services;

public class SimpleAIFunction : AIFunction
{
    private readonly Func<string, Task<string>> _func;

    public SimpleAIFunction(MethodInfo method, object toolInstance)
    {
        _func = async (args) =>
        {
            var parameters = method.GetParameters();
            object[] methodArgs = new object[parameters.Length];
            if (parameters.Length > 0)
            {
                methodArgs[0] = args.Trim('"');
            }

            var result = method.Invoke(toolInstance, methodArgs);
            return result?.ToString() ?? "";
        };

        this.Metadata = new AIFunctionMetadata(method.Name)
        {
            Name = method.Name,
            Description = method.Name,
            Parameters = method.GetParameters().Select(p => new AIFunctionParameterMetadata(p.Name) { IsRequired = true, ParameterType = p.ParameterType }).ToList(),
            ReturnParameter = new AIFunctionReturnParameterMetadata() { ParameterType = typeof(string) },
        };
    }

    public override AIFunctionMetadata Metadata { get; }

    protected override async Task<object?> InvokeCoreAsync(IEnumerable<KeyValuePair<string, object?>> arguments, CancellationToken cancellationToken = default)
    {
        // We take the first argument as the input string
        var firstArg = arguments.FirstOrDefault().Value?.ToString() ?? "";
        return await _func(firstArg);
    }
}

public class ToolRegistrationService
{
    public List<SimpleAIFunction> RegisterTools(object toolInstance)
    {
        var functions = new List<SimpleAIFunction>();
        var methods = toolInstance.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);

        foreach (var method in methods)
        {
            if (method.GetCustomAttribute<OllamaToolAttribute>() != null)
            {
                functions.Add(new SimpleAIFunction(method, toolInstance));
            }
        }

        return functions;
    }
}
