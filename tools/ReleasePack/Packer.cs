using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json.Nodes;

namespace ReleasePack;

/// <summary>A plugin as it was packed, for the release notes.</summary>
internal sealed record PackedPlugin(
    string Name, string Version, string Description, IReadOnlyList<string> Skills,
    string ZipName, string? BundledFrom, string? BundledTag, bool Pinned);

/// <summary>Builds the release files from the tag vX.Y.Z.</summary>
internal sealed class Packer(string repoRoot, string version, string outDir, IReadOnlyDictionary<string, string> pluginRefs, bool force)
{
    public const string ChecksumFile = "SHA256SUMS.txt";
    public const string NotesFile = "notes.md";

    private readonly string _tag = $"v{version}";
    private string _buildDir = "";

    public void Run()
    {
        if (!Repository.TagExists(repoRoot, _tag))
            throw new ReleaseException($"Tag {_tag} does not exist. Finish the release merge and tag first.");

        CheckOutDir();
        var work = Path.Combine(Path.GetTempPath(), $"releasepack-{Guid.NewGuid():N}");
        try
        {
            // Build in the work folder; an earlier pack is replaced only when this one succeeds.
            _buildDir = Path.Combine(work, "out");
            Directory.CreateDirectory(_buildDir);
            Pack(work);
            ReplaceOutDir();
            PrintSummary();
        }
        finally
        {
            DeleteDirectory(work);
        }
    }

    private void Pack(string work)
    {
        // Stage the whole marketplace from the tag.
        var stageParent = Path.Combine(work, "stage");
        var tagTree = Path.Combine(stageParent, "tag");
        Console.WriteLine($"Extracting {_tag}...");
        Repository.Extract(repoRoot, _tag, tagTree);

        var marketplacePath = Path.Combine(tagTree, Repository.MarketplacePath);
        var marketplace = Marketplace.Load(marketplacePath);
        if (marketplace.Version != version)
            throw new ReleaseException($"metadata.version in marketplace.json at {_tag} is '{marketplace.Version}', not '{version}'.");

        var collectionName = $"{marketplace.Name}-v{version}";
        var stage = Path.Combine(stageParent, collectionName);
        Directory.Move(tagTree, stage);
        marketplacePath = Path.Combine(stage, Repository.MarketplacePath);

        var plugins = marketplace.Plugins;
        var unknownRefs = pluginRefs.Keys.Except(plugins.Where(p => p.IsRemote).Select(p => p.Name)).ToList();
        if (unknownRefs.Count > 0)
            throw new ReleaseException($"--plugin-ref names no URL-sourced plugin: {string.Join(", ", unknownRefs)}.");

        // Pack each plugin; bundle remote ones into the stage.
        var packed = new List<PackedPlugin>();
        foreach (var plugin in plugins)
            packed.Add(plugin.IsRemote ? PackRemote(plugin, stage, work, marketplace) : PackLocal(plugin, stage));

        marketplace.Save(marketplacePath);

        // Pack the whole marketplace and check that it installs offline.
        var collectionZip = $"{collectionName}.zip";
        Console.WriteLine($"Packing {collectionZip}...");
        Zip(stage, Path.Combine(_buildDir, collectionZip), collectionName);
        Validate(Path.Combine(_buildDir, collectionZip), Path.Combine(work, "verify"), collectionName);

        WriteChecksums();
        var notes = NotesWriter.Write(marketplace.Name, version, collectionZip, packed);
        File.WriteAllText(Path.Combine(_buildDir, NotesFile), notes);
    }

    private void PrintSummary()
    {
        Console.WriteLine();
        Console.WriteLine($"Packed into {outDir}:");
        foreach (var file in Directory.GetFiles(outDir).Order(StringComparer.Ordinal))
            Console.WriteLine($"  {Path.GetFileName(file),-32} {new FileInfo(file).Length,10:N0} bytes");
        Console.WriteLine();
        Console.WriteLine($"Next: complete the Changes section of {NotesFile}, then run 'upload --version {version}'.");
    }

    private PackedPlugin PackLocal(PluginEntry plugin, string stage)
    {
        var folder = Path.GetFullPath(Path.Combine(stage, plugin.LocalPath!));
        if (!Directory.Exists(folder))
            throw new ReleaseException($"Plugin {plugin.Name}: folder {plugin.LocalPath} does not exist at {_tag}.");

        var pluginVersion = ReadPluginVersion(plugin.Name, folder);
        if (!Repository.TagExists(repoRoot, $"{plugin.Name}/v{pluginVersion}"))
            Console.WriteLine($"warning: tag {plugin.Name}/v{pluginVersion} does not exist.");

        var zipName = $"{plugin.Name}-v{pluginVersion}.zip";
        Console.WriteLine($"Packing {zipName}...");
        Zip(folder, Path.Combine(_buildDir, zipName), plugin.Name);
        return new PackedPlugin(plugin.Name, pluginVersion, plugin.Description, ReadSkills(folder), zipName, null, null, false);
    }

    private PackedPlugin PackRemote(PluginEntry plugin, string stage, string work, Marketplace marketplace)
    {
        var pinned = plugin.Ref != null;
        var tag = pluginRefs.GetValueOrDefault(plugin.Name) ?? plugin.Ref ?? Repository.LatestReleaseTag(plugin.Url!);
        Console.WriteLine($"Bundling {plugin.Name} {tag} from {plugin.Url}...");

        var clone = Path.Combine(work, "remote", plugin.Name);
        Repository.CloneTag(plugin.Url!, tag, clone);

        var relativePath = $"./plugins/{plugin.Name}";
        var folder = Path.GetFullPath(Path.Combine(stage, relativePath));
        if (Directory.Exists(folder))
            throw new ReleaseException($"Plugin {plugin.Name}: {relativePath} already exists in the marketplace.");
        Repository.Extract(clone, tag, folder);
        marketplace.SetLocalSource(plugin.Name, relativePath);

        var pluginVersion = ReadPluginVersion(plugin.Name, folder);
        if ($"v{pluginVersion}" != tag)
            Console.WriteLine($"warning: {plugin.Name} {tag} carries version {pluginVersion} in plugin.json.");

        var zipName = $"{plugin.Name}-v{pluginVersion}.zip";
        Console.WriteLine($"Packing {zipName}...");
        Zip(folder, Path.Combine(_buildDir, zipName), plugin.Name);
        return new PackedPlugin(plugin.Name, pluginVersion, plugin.Description, ReadSkills(folder), zipName, plugin.Url, tag, pinned);
    }

    private static string ReadPluginVersion(string pluginName, string folder)
    {
        var path = Path.Combine(folder, ".claude-plugin", "plugin.json");
        if (!File.Exists(path))
            throw new ReleaseException($"Plugin {pluginName}: .claude-plugin/plugin.json is missing.");
        return JsonNode.Parse(File.ReadAllText(path))?["version"]?.GetValue<string>()
               ?? throw new ReleaseException($"Plugin {pluginName}: plugin.json has no version.");
    }

    private static IReadOnlyList<string> ReadSkills(string folder)
    {
        var skills = Path.Combine(folder, "skills");
        if (!Directory.Exists(skills))
            return [];
        return Directory.GetDirectories(skills)
            .Where(d => File.Exists(Path.Combine(d, "SKILL.md")))
            .Select(d => Path.GetFileName(d)!)
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Zips a folder with every entry under the given prefix folder.</summary>
    private static void Zip(string source, string zipPath, string prefix)
    {
        using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        var files = Directory.GetFiles(source, "*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(source, f).Replace('\\', '/'))
            .Order(StringComparer.Ordinal);
        foreach (var relative in files)
            zip.CreateEntryFromFile(Path.Combine(source, relative), $"{prefix}/{relative}", CompressionLevel.Optimal);
    }

    /// <summary>Extracts the collection zip and validates the marketplace and its plugins with the claude CLI.</summary>
    private static void Validate(string zipPath, string verifyDir, string collectionName)
    {
        if (!ProcessRunner.Exists("claude"))
        {
            Console.WriteLine("warning: claude CLI not found; the marketplace was not validated.");
            return;
        }

        ZipFile.ExtractToDirectory(zipPath, verifyDir);
        var root = Path.Combine(verifyDir, collectionName);
        var targets = new List<string> { root };
        targets.AddRange(Marketplace.Load(Path.Combine(root, Repository.MarketplacePath)).Plugins
            .Select(p => Path.GetFullPath(Path.Combine(root, p.LocalPath!))));

        foreach (var target in targets)
        {
            var result = ProcessRunner.Run("claude", ["plugin", "validate", target], allowFailure: true);
            var label = Path.GetRelativePath(verifyDir, target);
            if (result.ExitCode != 0)
                throw new ReleaseException($"Validation of {label} failed:\n{result.Output}{result.Error}");
            Console.WriteLine($"Validated {label}");
        }
    }

    private void WriteChecksums()
    {
        var lines = Directory.GetFiles(_buildDir, "*.zip")
            .Order(StringComparer.Ordinal)
            .Select(f => $"{Sha256(f)}  {Path.GetFileName(f)}");
        File.WriteAllText(Path.Combine(_buildDir, ChecksumFile), string.Join("\n", lines) + "\n");
    }

    public static string Sha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }

    /// <summary>Fails early when the output folder holds an earlier pack (without --force) or foreign files.</summary>
    private void CheckOutDir()
    {
        if (!Directory.Exists(outDir) || !Directory.EnumerateFileSystemEntries(outDir).Any())
            return;

        if (!force)
            throw new ReleaseException($"{outDir} is not empty. Use --force to replace an earlier pack.");

        var others = Directory.EnumerateFileSystemEntries(outDir)
            .Where(e => !IsPackFile(Path.GetFileName(e)) || Directory.Exists(e))
            .ToList();
        if (others.Count > 0)
            throw new ReleaseException($"{outDir} holds files that pack did not make; remove them first: {string.Join(", ", others.Select(Path.GetFileName))}.");
    }

    /// <summary>Replaces the files of an earlier pack with the new build.</summary>
    private void ReplaceOutDir()
    {
        Directory.CreateDirectory(outDir);
        foreach (var file in Directory.GetFiles(outDir))
            File.Delete(file);
        foreach (var file in Directory.GetFiles(_buildDir))
            File.Move(file, Path.Combine(outDir, Path.GetFileName(file)));
    }

    private static bool IsPackFile(string name) =>
        name.EndsWith(".zip", StringComparison.Ordinal) || name is ChecksumFile or NotesFile;

    private static void DeleteDirectory(string path)
    {
        if (!Directory.Exists(path))
            return;
        // Git checkouts contain read-only files that Directory.Delete refuses on Windows.
        foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            File.SetAttributes(file, FileAttributes.Normal);
        Directory.Delete(path, recursive: true);
    }
}
