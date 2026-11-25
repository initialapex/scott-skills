using System.Text.Json;

namespace ReleasePack;

/// <summary>Uploads packed files to the GitHub Release vX.Y.Z with gh.</summary>
/// <remarks>
/// A new release is built in steps instead of with one 'gh release create' that carries every file:
/// create a draft, upload each file (retrying a failed one), check every file on GitHub, then publish.
/// A step that fails leaves a draft, and running the same command again resumes it: files that are
/// complete on GitHub are skipped, and a partial file is replaced.
/// </remarks>
internal sealed class Uploader(string repoRoot, string version, string outDir, bool clobber, bool dryRun)
{
    private const int Attempts = 3;
    private const int RetryDelaySeconds = 5;

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
        var release = GetRelease();

        if (release is { IsDraft: false })
        {
            if (!clobber)
                throw new ReleaseException($"Release {_tag} already exists. Use --clobber to replace its assets and notes.");

            Gh(["release", "upload", _tag, "--clobber", .. assets]);
            Gh(["release", "edit", _tag, "--notes-file", notesPath]);
        }
        else
        {
            CreateOrResumeAndPublish(release, $"{marketplace.Name} {_tag}", notesPath, assets);
        }

        if (!dryRun)
            Console.WriteLine(ProcessRunner.Run("gh", ["release", "view", _tag, "--json", "url", "--jq", ".url"], repoRoot).Output.Trim());
    }

    /// <summary>Creates the draft (or takes the unfinished one), uploads what is missing, checks it, and publishes.</summary>
    private void CreateOrResumeAndPublish(ReleaseState? release, string title, string notesPath, List<string> assets)
    {
        try
        {
            if (release == null)
            {
                release = CreateDraft(title, notesPath);
            }
            else
            {
                Console.WriteLine($"Release {_tag} is an unfinished draft; resuming it.");
                Gh(["release", "edit", _tag, "--notes-file", notesPath]);
            }

            UploadMissing(release, assets);
            if (!dryRun)
                VerifyUploaded(assets);
            Gh(["release", "edit", _tag, "--draft=false", "--latest"]);
        }
        catch (ReleaseException ex)
        {
            throw new ReleaseException($"{ex.Message}\n{DescribeLeftover()}");
        }
    }

    /// <summary>Says what a failed run left on GitHub and what to do next; it never hides the original error.</summary>
    private string DescribeLeftover()
    {
        if (dryRun)
            return "";

        try
        {
            return GetRelease() is { IsDraft: true }
                ? $"Release {_tag} was not published. It stays a draft: run the same command again to resume it."
                : $"Release {_tag} was not created. Run the same command again.";
        }
        catch (ReleaseException)
        {
            return $"Check release {_tag} with 'gh release view {_tag}', then run the same command again.";
        }
    }

    /// <summary>Creates a draft release without files. gh may fail after it created the draft, so a failed attempt looks again.</summary>
    private ReleaseState CreateDraft(string title, string notesPath)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                Gh(["release", "create", _tag, "--verify-tag", "--draft", "--title", title, "--notes-file", notesPath]);
                break;
            }
            catch (ReleaseException ex) when (attempt < Attempts)
            {
                Console.WriteLine($"Creating the draft failed (attempt {attempt} of {Attempts}): {FirstLine(ex.Message)}");
                var existing = GetRelease();
                if (existing != null)
                    return existing;
                Wait(attempt);
            }
        }

        if (dryRun)
            return new ReleaseState(true, []);
        return GetRelease() ?? throw new ReleaseException($"gh created release {_tag}, but it cannot be read back.");
    }

    /// <summary>Uploads every file that is not complete on GitHub yet; a partial file is replaced.</summary>
    private void UploadMissing(ReleaseState release, List<string> assets)
    {
        foreach (var asset in assets)
        {
            var name = Path.GetFileName(asset);
            var size = new FileInfo(asset).Length;
            if (release.Assets.TryGetValue(name, out var remote) && remote.Size == size && remote.State == "uploaded")
            {
                Console.WriteLine($"{name}: already uploaded, skipped");
                continue;
            }

            Retry(() => Gh(["release", "upload", _tag, "--clobber", asset]), name);
        }
    }

    /// <summary>Checks on GitHub that every file is there, complete, and has the size of the local file.</summary>
    private void VerifyUploaded(List<string> assets)
    {
        var release = GetRelease() ?? throw new ReleaseException($"Release {_tag} disappeared while it was uploaded.");
        var problems = new List<string>();
        foreach (var asset in assets)
        {
            var name = Path.GetFileName(asset);
            if (!release.Assets.TryGetValue(name, out var remote))
                problems.Add($"{name}: missing");
            else if (remote.State != "uploaded" || remote.Size != new FileInfo(asset).Length)
                problems.Add($"{name}: incomplete (state {remote.State}, {remote.Size} bytes)");
        }

        if (problems.Count > 0)
            throw new ReleaseException($"Not every file is complete on GitHub:\n  {string.Join("\n  ", problems)}");
    }

    /// <summary>Reads the release of the tag from GitHub, or returns null when there is none.</summary>
    private ReleaseState? GetRelease()
    {
        var result = ProcessRunner.Run("gh", ["release", "view", _tag, "--json", "isDraft,assets"], repoRoot, allowFailure: true);
        if (result.ExitCode != 0)
        {
            if (result.Error.Contains("release not found", StringComparison.OrdinalIgnoreCase))
                return null;
            throw new ReleaseException($"Cannot read release {_tag}:\n{result.Error.Trim()}");
        }

        using var document = JsonDocument.Parse(result.Output);
        var root = document.RootElement;
        var assets = new Dictionary<string, RemoteAsset>(StringComparer.Ordinal);
        foreach (var asset in root.GetProperty("assets").EnumerateArray())
        {
            assets[asset.GetProperty("name").GetString()!] =
                new RemoteAsset(asset.GetProperty("size").GetInt64(), asset.GetProperty("state").GetString() ?? "");
        }

        return new ReleaseState(root.GetProperty("isDraft").GetBoolean(), assets);
    }

    /// <summary>Runs an action up to three times, because the GitHub API fails now and then with a 5xx error.</summary>
    private void Retry(Action action, string what)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                action();
                return;
            }
            catch (ReleaseException ex) when (attempt < Attempts)
            {
                Console.WriteLine($"{what}: attempt {attempt} of {Attempts} failed: {FirstLine(ex.Message)}");
                Wait(attempt);
            }
        }
    }

    private static void Wait(int attempt) => Thread.Sleep(TimeSpan.FromSeconds(RetryDelaySeconds * attempt));

    private static string FirstLine(string message)
    {
        var lines = message.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return lines.Length > 1 ? lines[^1] : lines.FirstOrDefault() ?? "";
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

    private sealed record RemoteAsset(long Size, string State);

    private sealed record ReleaseState(bool IsDraft, Dictionary<string, RemoteAsset> Assets);
}
