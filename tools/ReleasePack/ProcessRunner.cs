using System.Diagnostics;
using System.Text;

namespace ReleasePack;

/// <summary>Error that stops the tool with a message for the user.</summary>
internal sealed class ReleaseException(string message) : Exception(message);

/// <summary>Runs external tools (git, gh, claude) without a shell.</summary>
internal static class ProcessRunner
{
    public sealed record Result(int ExitCode, string Output, string Error);

    /// <summary>Runs a tool and returns its text output; throws when it fails unless allowed.</summary>
    public static Result Run(string fileName, IEnumerable<string> args, string? workDir = null, bool allowFailure = false)
    {
        using var process = Start(fileName, args, workDir);
        var errorTask = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        var result = new Result(process.ExitCode, output, errorTask.Result);

        if (result.ExitCode != 0 && !allowFailure)
            throw new ReleaseException($"{Describe(fileName, args)} failed ({result.ExitCode}):\n{result.Error.Trim()}");
        return result;
    }

    /// <summary>Runs a tool and hands its binary output stream to a consumer.</summary>
    public static void RunWithStream(string fileName, IEnumerable<string> args, string? workDir, Action<Stream> consume)
    {
        using var process = Start(fileName, args, workDir);
        var errorTask = process.StandardError.ReadToEndAsync();
        consume(process.StandardOutput.BaseStream);
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new ReleaseException($"{Describe(fileName, args)} failed ({process.ExitCode}):\n{errorTask.Result.Trim()}");
    }

    /// <summary>Returns true when the tool can be started from PATH.</summary>
    public static bool Exists(string fileName)
    {
        try
        {
            Run(fileName, ["--version"], allowFailure: true);
            return true;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }

    private static Process Start(string fileName, IEnumerable<string> args, string? workDir)
    {
        var info = new ProcessStartInfo(fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        if (workDir != null)
            info.WorkingDirectory = workDir;
        foreach (var arg in args)
            info.ArgumentList.Add(arg);

        return Process.Start(info) ?? throw new ReleaseException($"Cannot start {fileName}.");
    }

    private static string Describe(string fileName, IEnumerable<string> args) =>
        $"'{fileName} {string.Join(' ', args)}'";
}
