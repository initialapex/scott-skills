using System.Text.RegularExpressions;

namespace ReleasePack;

/// <summary>Command line options of the tool.</summary>
internal sealed partial class Options
{
    public const string Usage = """
        Package a released version of the marketplace and upload it to a GitHub Release.

        Usage:
          dotnet run --project tools/ReleasePack -- pack   --version X.Y.Z [--plugin-ref <plugin>=<tag>]... [--out <dir>] [--force]
          dotnet run --project tools/ReleasePack -- upload --version X.Y.Z [--out <dir>] [--clobber] [--dry-run]

        Commands:
          pack     Build the zips, SHA256SUMS.txt, and a notes.md draft from the tag vX.Y.Z.
          upload   Upload the packed files to the GitHub Release vX.Y.Z with gh: create a draft, upload each file
                   (a failed one is retried), check every file on GitHub, then publish. When a run fails, the draft
                   stays: run the same command again to resume it (complete files are skipped).

        Options:
          --version X.Y.Z               Collection version; the tag vX.Y.Z must exist.
          --plugin-ref <plugin>=<tag>   Tag to bundle for a URL-sourced plugin instead of its latest release tag.
          --out <dir>                   Output folder (default: artifacts/release-vX.Y.Z in the repository).
          --force                       pack: replace the files of an earlier pack in the output folder.
          --clobber                     upload: update a release that is already published (replace assets and notes).
          --dry-run                     upload: print the gh commands without running them.
        """;

    public string Command { get; private set; } = "";
    public string Version { get; private set; } = "";
    public string? OutDir { get; private set; }
    public Dictionary<string, string> PluginRefs { get; } = new(StringComparer.Ordinal);
    public bool Force { get; private set; }
    public bool Clobber { get; private set; }
    public bool DryRun { get; private set; }
    public bool ShowHelp { get; private set; }

    public static Options Parse(string[] args)
    {
        var options = new Options();
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            options.ShowHelp = true;
            return options;
        }

        options.Command = args[0];
        for (var i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--version":
                    options.Version = Value(args, ref i);
                    break;
                case "--out":
                    options.OutDir = Path.GetFullPath(Value(args, ref i));
                    break;
                case "--plugin-ref":
                    var pair = Value(args, ref i).Split('=', 2);
                    if (pair.Length != 2 || pair[0].Length == 0 || pair[1].Length == 0)
                        throw new ReleaseException("--plugin-ref expects <plugin>=<tag>.");
                    options.PluginRefs[pair[0]] = pair[1];
                    break;
                case "--force":
                    options.Force = true;
                    break;
                case "--clobber":
                    options.Clobber = true;
                    break;
                case "--dry-run":
                    options.DryRun = true;
                    break;
                case "-h" or "--help":
                    options.ShowHelp = true;
                    break;
                default:
                    throw new ReleaseException($"Unknown option '{args[i]}'.\n\n{Usage}");
            }
        }

        if (!options.ShowHelp && !SemVerPattern().IsMatch(options.Version))
            throw new ReleaseException("--version X.Y.Z is required (for example --version 0.2.0).");

        return options;
    }

    private static string Value(string[] args, ref int i)
    {
        if (i + 1 >= args.Length)
            throw new ReleaseException($"{args[i]} needs a value.");
        return args[++i];
    }

    [GeneratedRegex(@"^\d+\.\d+\.\d+$")]
    private static partial Regex SemVerPattern();
}
