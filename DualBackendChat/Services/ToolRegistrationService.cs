using System.Reflection;
using Microsoft.Extensions.AI;
using DualBackendChat.Tools;

namespace DualBackendChat.Services;

public class SimpleAIFunction : AIFunction
{
    private readonly Func<string, Task<string>> _func;
    private readonly string _description;

    public SimpleAIFunction(string description, Func<string, Task<string>> func)
    {
        _description = description;
        _func = func;
        this.Metadata = new AIFunctionMetadata(_description)
        {
            Name = _description,
            Parameters = new List<AIFunctionParameterMetadata>(),
            ReturnParameter = new AIFunctionReturnParameterMetadata() { ParameterType = typeof(string) },
            Description = _description
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
                var func = new SimpleAIFunction(
                    method.Name,
                    async (args) =>
                    {
                        var parameters = method.GetParameters();
                        object[] methodArgs = new object[parameters.Length];
                        if (parameters.Length > 0)
                        {
                            methodArgs[0] = args.Trim('"');
                        }
                        var result = method.Invoke(toolInstance, methodArgs);
                        return result?.ToString() ?? "";
                    }
                )
                {
                    Metadata = { Parameters = method.GetParameters().Select(p => new AIFunctionParameterMetadata(p.Name) { IsRequired = true }).ToList() }


                };

                method.GetParameters().Select(p => new AIFunctionParameterMetadata(p.Name) { IsRequired = true });

                System.Diagnostics.Debug.Assert(func != null);
                System.Diagnostics.Debug.Assert(func.Metadata != null);
                System.Diagnostics.Debug.Assert(func.Metadata.Parameters != null);
                System.Diagnostics.Debug.Assert(func.Metadata.Parameters. != null);
                functions.Add(func);
            }
        }

        return functions;
    }
}
