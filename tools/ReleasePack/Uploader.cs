namespace ReleasePack;

/// <summary>Uploads packed files to the GitHub Release vX.Y.Z with gh.</summary>
internal sealed class Uploader(string repoRoot, string version, string outDir, bool clobber, bool dryRun)
{
    private readonly string _tag = $"v{version}";

    public void Run()
    {
        var notesPath = Path.Combine(outDir, Packer.NotesFile);
        var checksumPath = Path.Combine(outDir, Packer.ChecksumFile);
        if (!File.Exists(notesPath) || !File.Exists(checksumPath))
            throw new ReleaseException($"{outDir} has no packed release. Run 'pack --version {version}' first.");

        if (File.ReadAllText(notesPath).Contains(NotesWriter.TodoMarker, StringComparison.Ordinal))
            throw new ReleaseException($"{Packer.NotesFile} still has the TODO line; complete the Changes section first.");

        var zips = VerifyChecksums(checksumPath);
        var assets = zips.Append(checksumPath).ToList();

        if (!Repository.TagExists(repoRoot, _tag))
            throw new ReleaseException($"Tag {_tag} does not exist.");

        var marketplace = Marketplace.Parse(ProcessRunner.Run("git", ["show", $"{_tag}:{Repository.MarketplacePath}"], repoRoot).Output);
        var exists = ProcessRunner.Run("gh", ["release", "view", _tag], repoRoot, allowFailure: true).ExitCode == 0;

        if (exists && !clobber)
            throw new ReleaseException($"Release {_tag} already exists. Use --clobber to replace its assets and notes.");

        if (exists)
        {
            Gh(["release", "upload", _tag, "--clobber", .. assets]);
            Gh(["release", "edit", _tag, "--notes-file", notesPath]);
        }
        else
        {
            Gh(["release", "create", _tag, "--verify-tag", "--title", $"{marketplace.Name} {_tag}", "--notes-file", notesPath, .. assets]);
        }

        if (!dryRun)
            Console.WriteLine(ProcessRunner.Run("gh", ["release", "view", _tag, "--json", "url", "--jq", ".url"], repoRoot).Output.Trim());
    }

    /// <summary>Checks that SHA256SUMS.txt matches every zip in the folder and returns the zip paths.</summary>
    private List<string> VerifyChecksums(string checksumPath)
    {
        var expected = File.ReadAllLines(checksumPath)
            .Where(l => l.Length > 0)
            .Select(l => l.Split("  ", 2))
            .ToDictionary(p => p[1], p => p[0], StringComparer.Ordinal);

        var zips = Directory.GetFiles(outDir, "*.zip").Order(StringComparer.Ordinal).ToList();
        var names = zips.Select(Path.GetFileName).ToHashSet(StringComparer.Ordinal);
        if (zips.Count == 0 || !names.SetEquals(expected.Keys))
            throw new ReleaseException($"{Packer.ChecksumFile} does not list exactly the zips in {outDir}; pack again.");

        foreach (var zip in zips)
        {
            if (Packer.Sha256(zip) != expected[Path.GetFileName(zip)])
                throw new ReleaseException($"{Path.GetFileName(zip)} does not match {Packer.ChecksumFile}; pack again.");
        }

        return zips;
    }

    private void Gh(string[] args)
    {
        Console.WriteLine($"gh {string.Join(' ', args.Select(Quote))}");
        if (!dryRun)
            ProcessRunner.Run("gh", args, repoRoot);
    }

    private static string Quote(string arg) => arg.Contains(' ') ? $"\"{arg}\"" : arg;
}
