using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ReleasePack;

/// <summary>A plugin entry of marketplace.json.</summary>
internal sealed record PluginEntry(string Name, string Description, string? LocalPath, string? Url, string? Ref)
{
    public bool IsRemote => Url != null;
}

/// <summary>Reads and rewrites .claude-plugin/marketplace.json.</summary>
internal sealed class Marketplace
{
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        NewLine = "\n",
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly JsonObject _root;

    private Marketplace(JsonObject root) => _root = root;

    public static Marketplace Load(string path)
    {
        var node = JsonNode.Parse(File.ReadAllText(path)) as JsonObject
                   ?? throw new ReleaseException($"{path} is not a JSON object.");
        return new Marketplace(node);
    }

    public static Marketplace Parse(string json) =>
        new(JsonNode.Parse(json) as JsonObject ?? throw new ReleaseException("marketplace.json is not a JSON object."));

    public string Name => _root["name"]?.GetValue<string>() ?? throw new ReleaseException("marketplace.json has no name.");

    public string? Version => _root["metadata"]?["version"]?.GetValue<string>();

    public IReadOnlyList<PluginEntry> Plugins =>
        (_root["plugins"] as JsonArray ?? throw new ReleaseException("marketplace.json has no plugins array."))
        .Select(node => ToEntry(node as JsonObject ?? throw new ReleaseException("A plugin entry is not an object.")))
        .ToList();

    /// <summary>Points a plugin entry at a folder inside the marketplace.</summary>
    public void SetLocalSource(string pluginName, string relativePath)
    {
        var entry = (_root["plugins"] as JsonArray)!
            .OfType<JsonObject>()
            .Single(p => p["name"]?.GetValue<string>() == pluginName);
        entry["source"] = relativePath;
    }

    public void Save(string path) => File.WriteAllText(path, _root.ToJsonString(WriteOptions) + "\n");

    private static PluginEntry ToEntry(JsonObject plugin)
    {
        var name = plugin["name"]?.GetValue<string>() ?? throw new ReleaseException("A plugin entry has no name.");
        var description = plugin["description"]?.GetValue<string>() ?? "";
        var source = plugin["source"];

        if (source is JsonValue value)
            return new PluginEntry(name, description, value.GetValue<string>(), null, null);

        if (source is JsonObject obj)
        {
            var kind = obj["source"]?.GetValue<string>();
            var gitRef = obj["ref"]?.GetValue<string>();
            return kind switch
            {
                "url" => new PluginEntry(name, description, null,
                    obj["url"]?.GetValue<string>() ?? throw new ReleaseException($"Plugin {name}: url source without url."), gitRef),
                "github" => new PluginEntry(name, description, null,
                    $"https://github.com/{obj["repo"]?.GetValue<string>() ?? throw new ReleaseException($"Plugin {name}: github source without repo.")}.git", gitRef),
                _ => throw new ReleaseException($"Plugin {name}: source type '{kind}' cannot be bundled."),
            };
        }

        throw new ReleaseException($"Plugin {name} has no source.");
    }
}
