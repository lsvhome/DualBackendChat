using System.IO;
using DualBackendChat.Tools;

namespace DualBackendChat.Tools;

public class FileSystemTools
{
    private readonly string _sandboxRoot;
    private readonly bool _enableRiskyTools;

    public FileSystemTools(AppSettings settings)
    {
        _sandboxRoot = settings.SandboxRoot;
        _enableRiskyTools = settings.EnableRiskyTools;
        
        if (!Directory.Exists(_sandboxRoot))
        {
            Directory.CreateDirectory(_sandboxRoot);
        }
    }

    private string ResolvePath(string path)
    {
        // Simple sandbox check: ensure the path is within SandboxRoot
        var fullPath = Path.GetFullPath(Path.Combine(_sandboxRoot, path));
        if (!fullPath.StartsWith(Path.GetFullPath(_sandboxRoot)))
        {
            throw new UnauthorizedAccessException($"Access to path {path} is denied. It is outside the sandbox root.");
        }
        return fullPath;
    }

    [OllamaTool]
    public string ReadFile(string path)
    {
        var fullPath = ResolvePath(path);
        if (!File.Exists(fullPath)) return $"File {path} not found.";
        return File.ReadAllText(fullPath);
    }

    [OllamaTool]
    public string WriteFile(string path, string content)
    {
        var fullPath = ResolvePath(path);
        File.WriteAllText(fullPath, content);
        return $"Successfully wrote to {path}.";
    }

    [OllamaTool]
    public string DeleteFile(string path)
    {
        var fullPath = ResolvePath(path);
        if (!File.Exists(fullPath)) return $"File {path} not found.";
        File.Delete(fullPath);
        return $"Successfully deleted {path}.";
    }

    [OllamaTool]
    public string ListDirectory(string path)
    {
        var fullPath = ResolvePath(path);
        if (!Directory.Exists(fullPath)) return $"Directory {path} not found.";
        var files = Directory.GetFileSystemEntries(fullPath);
        return string.Join(Environment.NewLine, files.Select(f => Path.GetFileName(f)));
    }

    [OllamaTool]
    public string MoveFile(string sourcePath, string destinationPath)
    {
        var src = ResolvePath(sourcePath);
        var dest = ResolvePath(destinationPath);
        File.Move(src, dest, true);
        return $"Successfully moved {sourcePath} to {destinationPath}.";
    }

    [OllamaTool]
    public string CreateDirectory(string path)
    {
        var fullPath = ResolvePath(path);
        Directory.CreateDirectory(fullPath);
        return $"Successfully created directory {path}.";
    }

    // Example of a risky tool
    [OllamaTool]
    public string ExecuteShellCommand(string command)
    {
        if (!_enableRiskyTools)
        {
            return "Error: Shell execution is disabled by policy. Enable 'EnableRiskyTools' in settings to use this.";
        }

        // This is simplified and potentially dangerous, but for the sake of the exercise
        // we are implementing the "risky tool" as requested.
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{command}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            return string.IsNullOrEmpty(error) ? output : $"Error: {error}\nOutput: {output}";
        }
        catch (Exception ex)
        {
            return $"Exception: {ex.Message}";
        }
    }
}
