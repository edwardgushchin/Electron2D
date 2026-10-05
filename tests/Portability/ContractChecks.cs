using System.Text.Json;
using System.Text.Json.Serialization;
using Electron2D;
using EngineJSON = Electron2D.JSON;

internal static class ContractChecks
{
    internal static void Run()
    {
        var model = new Model { Number = 17, Values = [3, 5] };
        var metadata = TestJSONContext.Default.Model;
        var json = EngineJSON.FromNative(model, metadata)!;
        model.Values[0] = 99;
        var decoded = EngineJSON.ToNative(json, metadata)!;
        Check(decoded.Number == 17 && decoded.Values.SequenceEqual(new[] { 3, 5 }), "Compiled JSON model snapshots.");
        Check(EngineJSON.ToNative<int>(EngineJSON.FromNative(7)) == 7, "Built-in scalar metadata.");
        Check(EngineJSON.ToNative<Vector2>(EngineJSON.FromNative(new Vector2(2, 4))) == new Vector2(2, 4), "Built-in numeric converters.");
        Reject<NotSupportedException>(() => EngineJSON.FromNative(new UnsafeModel(), TestJSONContext.Default.UnsafeModel));
        Reject<ArgumentNullException>(() => EngineJSON.FromNative(model, null!));
        Reject<JsonException>(() => EngineJSON.ToNative(System.Text.Json.Nodes.JsonNode.Parse("\"wrong\""), metadata));
        Reject<NotSupportedException>(() => EngineJSON.ToNative(null, TestJSONContext.Default.UnsafeModel));
        var indented = new TestJSONContext(new JsonSerializerOptions { IncludeFields = true, WriteIndented = true });
        Reject<ArgumentException>(() => new ConfigKey<Model>("custom", "model", indented.Model));
        Reject<ArgumentException>(() => new ProjectSetting<Model>("custom/model", model, null, indented.Model));

        using var config = new ConfigFile();
        var key = new ConfigKey<Model>("custom", "model", metadata);
        config.SetValue(key, decoded);
        var text = config.EncodeToText();
        decoded.Values[1] = 88;
        Check(config.GetValue(key).Values[1] == 5, "Compiled configuration snapshot isolation.");
        using var reloaded = new ConfigFile();
        reloaded.Parse(text);
        Check(reloaded.GetValue(key).Number == 17, "Compiled configuration decode.");
        var bounds = new ConfigKey<Rect2i>("custom", "bounds");
        config.SetValue(bounds, new Rect2i(1, 2, 3, 4));
        Check(config.GetValue(bounds) == new Rect2i(1, 2, 3, 4), "Nested integer rectangle converters.");

        var setting = new ProjectSetting<Model>("custom/model", new Model { Number = 5, Values = [1] },
            value => value.Number >= 0, metadata);
        using var settings = new ProjectSettingsRegistry(Environment.CurrentDirectory,
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-portability"));
        settings.Register(setting);
        settings.Set(setting, new Model { Number = 8, Values = [2] });
        settings.SetFeatureOverride(setting, "fixture", new Model { Number = 9, Values = [3] });
        Check(settings.Get(setting).Number == 8 && settings.GetWithOverride(setting, ["fixture"]).Number == 9,
            "Compiled setting metadata reaches base and override keys.");
        Reject<ArgumentOutOfRangeException>(() => settings.Set(setting, new Model { Number = -1 }));
        Check(settings.Get(setting).Number == 8, "Compiled validation rollback.");

        using var theme = new Theme();
        foreach (var name in new[] { "Label", "Control", "AudioEffectNotchFilter", "Window", "HTTPClient" })
            Reject<ArgumentException>(() => theme.SetTypeVariation(name, "Control"));
        theme.SetTypeVariation("CustomLabel", "Label");
        Check(theme.GetTypeVariationBase("CustomLabel") == "Label", "Compiled native theme catalog.");

        using var floorShape = new RectangleShape { Size = new(200, 20) };
        using var bodyShape = new RectangleShape { Size = new(20, 20) };
        var root = new Node();
        var floor = new StaticBody { Position = new(0, 100) };
        floor.AddChild(new CollisionShape { Shape = floorShape });
        var body = new RigidBody();
        body.AddChild(new CollisionShape { Shape = bodyShape });
        root.AddChild(floor);
        root.AddChild(body);
        using var tree = new SceneTree(root);
        for (var frame = 0; frame < 120; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.Y is > 75 and < 85 && MathF.Abs(body.LinearVelocity.Y) < 2, "Public scene physics lifecycle.");
        Console.WriteLine("PORTABILITY PASS: JSON, configuration, settings, theme catalog and headless scene physics.");
    }

    private static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}

internal sealed class Model
{
    public int Number;
    public int[] Values = [];
}

internal sealed class UnsafeModel
{
    public object Value { get; set; } = new();
}

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata, IncludeFields = true)]
[JsonSerializable(typeof(Model))]
[JsonSerializable(typeof(UnsafeModel))]
internal sealed partial class TestJSONContext : JsonSerializerContext;
