using System.Text;

namespace ReleasePack;

/// <summary>Writes the draft release notes.</summary>
internal static class NotesWriter
{
    /// <summary>Marker that upload refuses, so the Changes section is filled in before publishing.</summary>
    public const string TodoMarker = "<!-- TODO:";

    public static string Write(string marketplaceName, string version, string collectionZip, IReadOnlyList<PackedPlugin> plugins)
    {
        var sb = new StringBuilder();
        sb.Append("## Changes\n\n");
        sb.Append($"{TodoMarker} summarize what changed in this release, then delete this line. -->\n\n");

        sb.Append("## Plugins\n\n");
        sb.Append("| Plugin | Version | Skills |\n");
        sb.Append("|--------|---------|--------|\n");
        foreach (var p in plugins)
        {
            var skills = p.Skills.Count == 0 ? "" : string.Join(", ", p.Skills.Select(s => $"`{p.Name}:{s}`"));
            var origin = p.BundledFrom == null ? "" : $" Bundled from {RepoLink(p.BundledFrom)} at {p.BundledRef}.";
            var description = Escape(p.Description.TrimEnd());
            if (origin.Length > 0 && description.Length > 0 && !description.EndsWith('.'))
                description += ".";
            sb.Append($"| `{p.Name}` | {p.Version} | {skills}<br>{description}{origin} |\n");
        }

        var installs = string.Concat(plugins.Select(p => $"/plugin install {p.Name}@{marketplaceName}\n"));

        sb.Append("\n## Install online\n\n");
        sb.Append($"```\n/plugin marketplace add {OwnerRepo()}\n{installs}```\n");
        // Plugins of one repository share one remark.
        foreach (var repo in plugins.Where(p => p.BundledFrom != null && !p.Pinned).GroupBy(p => p.BundledFrom))
        {
            var names = string.Join(", ", repo.Select(p => $"`{p.Name}`"));
            sb.Append($"\nOnline installation of {names} follows its repository's default branch, not the bundled version.\n");
        }

        sb.Append("\n## Install offline\n\n");
        sb.Append($"Download `{collectionZip}`, extract it, and add the extracted folder as a local marketplace:\n\n");
        sb.Append($"```\n/plugin marketplace add C:\\path\\to\\{Path.GetFileNameWithoutExtension(collectionZip)}\n{installs}```\n\n");
        sb.Append("Every plugin is inside the zip under `plugins/`, so no network access is needed.\n");

        sb.Append("\n## Assets\n\n");
        sb.Append($"- `{collectionZip}`: the whole marketplace at tag `v{version}`, for offline installation");
        var bundled = plugins.Where(p => p.BundledFrom != null).ToList();
        if (bundled.Count > 0)
            sb.Append($", with {string.Join(", ", bundled.Select(p => $"`{p.Name}` {p.BundledRef}"))} bundled");
        sb.Append('\n');
        // Plugins that share one repository folder share one zip.
        foreach (var zip in plugins.GroupBy(p => p.ZipName))
        {
            var names = zip.Select(p => $"`{p.Name}`").ToList();
            var what = names.Count == 1
                ? $"the {names[0]} plugin only"
                : $"the shared repository folder of {string.Join(", ", names)}";
            sb.Append($"- `{zip.Key}`: {what}\n");
        }
        sb.Append($"- `{Packer.ChecksumFile}`: SHA-256 checksums of the zip files\n");
        return sb.ToString();
    }

    private static string OwnerRepo()
    {
        var url = ProcessRunner.Run("git", ["remote", "get-url", "origin"], allowFailure: true).Output.Trim();
        var path = url.Replace("git@github.com:", "https://github.com/");
        const string prefix = "https://github.com/";
        return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? path[prefix.Length..].TrimEnd('/').Replace(".git", "")
            : url;
    }

    private static string RepoLink(string url)
    {
        var display = url.Replace("https://github.com/", "").Replace(".git", "");
        return $"[{display}]({url.Replace(".git", "")})";
    }

    private static string Escape(string text) => text.Replace("|", "\\|");
}
