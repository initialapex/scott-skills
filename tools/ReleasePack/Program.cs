using ReleasePack;

try
{
    var options = Options.Parse(args);
    if (options.ShowHelp)
    {
        Console.WriteLine(Options.Usage);
        return 0;
    }

    var repoRoot = Repository.FindRoot();
    var outDir = options.OutDir ?? Path.Combine(repoRoot, "artifacts", $"release-v{options.Version}");

    switch (options.Command)
    {
        case "pack":
            new Packer(repoRoot, options.Version, outDir, options.PluginRefs, options.Force).Run();
            break;
        case "upload":
            new Uploader(repoRoot, options.Version, outDir, options.Clobber, options.DryRun).Run();
            break;
        default:
            throw new ReleaseException($"Unknown command '{options.Command}'.\n\n{Options.Usage}");
    }

    return 0;
}
catch (ReleaseException ex)
{
    Console.Error.WriteLine($"error: {ex.Message}");
    return 1;
}
