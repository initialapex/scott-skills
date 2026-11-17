using System.Formats.Tar;
using System.Text.RegularExpressions;

namespace ReleasePack;

/// <summary>Git operations on this repository and on remote plugin repositories.</summary>
internal static partial class Repository
{
    public const string MarketplacePath = ".claude-plugin/marketplace.json";

    /// <summary>Finds the root of the marketplace repository from the current folder.</summary>
    public static string FindRoot()
    {
        var result = ProcessRunner.Run("git", ["rev-parse", "--show-toplevel"], allowFailure: true);
        var root = result.ExitCode == 0 ? Path.GetFullPath(result.Output.Trim()) : null;
        if (root == null || !File.Exists(Path.Combine(root, MarketplacePath)))
            throw new ReleaseException($"Run the tool inside the marketplace repository (no {MarketplacePath} found).");
        return root;
    }

    public static bool TagExists(string repoRoot, string tag) =>
        ProcessRunner.Run("git", ["rev-parse", "-q", "--verify", $"refs/tags/{tag}"], repoRoot, allowFailure: true).ExitCode == 0;

    /// <summary>Extracts the tree of a tag (git archive) into a folder.</summary>
    public static void Extract(string repoDir, string treeish, string destination)
    {
        Directory.CreateDirectory(destination);
        ProcessRunner.RunWithStream("git", ["archive", "--format=tar", treeish], repoDir, stream =>
        {
            TarFile.ExtractToDirectory(stream, destination, overwriteFiles: false);
            // The reader stops at the end-of-archive marker; drain the block padding so git can exit.
            stream.CopyTo(Stream.Null);
        });
    }

    /// <summary>Returns the highest release tag vX.Y.Z of a remote repository, skipping prereleases.</summary>
    public static string LatestReleaseTag(string url)
    {
        var output = ProcessRunner.Run("git", ["ls-remote", "--tags", "--refs", url]).Output;
        var best = output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => ReleaseTagPattern().Match(line))
            .Where(m => m.Success)
            .Select(m => (Tag: m.Groups["tag"].Value,
                          Version: new Version(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value), int.Parse(m.Groups[3].Value))))
            .OrderByDescending(t => t.Version)
            .FirstOrDefault();

        return best.Tag ?? throw new ReleaseException($"No release tag vX.Y.Z found in {url}.");
    }

    /// <summary>Shallow-clones one tag of a remote repository.</summary>
    public static void CloneTag(string url, string tag, string destination) =>
        ProcessRunner.Run("git", ["-c", "advice.detachedHead=false", "clone", "--quiet", "--depth", "1", "--branch", tag, url, destination]);

    [GeneratedRegex(@"refs/tags/(?<tag>v(\d+)\.(\d+)\.(\d+))$")]
    private static partial Regex ReleaseTagPattern();
}
