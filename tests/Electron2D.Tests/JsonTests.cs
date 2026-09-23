using System.Text.Json;
using System.Text.Json.Nodes;
using Electron2D;

using EngineJSON = Electron2D.JSON;

internal static class JsonTests
{
    public static void Run()
    {
        using var json = new EngineJSON();
        Check(json.Data is null && json.GetErrorLine() == 0 && json.GetErrorMessage() == string.Empty &&
              json.GetParsedText() == string.Empty, "An empty JSON resource has no value or diagnostics.");

        const string source = "{\"z\":1,\"a\":[true,null,],}";
        Check(json.Parse(source, keepText: true) && json.GetParsedText() == source &&
              json.GetErrorLine() == 0 && json.GetErrorMessage() == string.Empty &&
              json.Data is JsonObject root && root["z"]!.GetValue<int>() == 1 &&
              root["a"] is JsonArray { Count: 2 }, "A JSON resource parses objects and trailing commas while retaining source text.");
        Check(EngineJSON.Stringify(json.Data) == "{\"a\":[true,null],\"z\":1}" &&
              EngineJSON.Stringify(json.Data, sortKeys: false) == "{\"z\":1,\"a\":[true,null]}" &&
              EngineJSON.Stringify(json.Data, "..") == "{\n..\"a\": [\n....true,\n....null\n..],\n..\"z\": 1\n}",
            "JSON formatting honors sorting, insertion order, and arbitrary indentation.");

        using var duplicate = (EngineJSON)json.Duplicate();
        ((JsonObject)json.Data!)["z"] = 9;
        Check(duplicate.GetParsedText() == source && duplicate.Data!["z"]!.GetValue<int>() == 1,
            "Resource duplication retains source text and owns an independent JSON tree.");

        var supplied = JsonNode.Parse("{\"value\":3}")!;
        json.Data = supplied;
        ((JsonObject)supplied)["value"] = 4;
        Check(json.Data!["value"]!.GetValue<int>() == 3 && json.GetParsedText() == string.Empty,
            "Assigning Data snapshots the supplied tree and clears retained text.");
        Check(json.Parse("[1,\n broken]") == false && json.Data is null &&
              json.GetErrorLine() == 1 && json.GetErrorMessage().Length != 0 && json.GetParsedText() == string.Empty,
            "Parse errors update line diagnostics and clear stale data.");
        Check(json.Parse("false") && json.Data!.GetValue<bool>() == false && json.GetErrorMessage() == string.Empty &&
              EngineJSON.ParseString("\"text\"")!.GetValue<string>() == "text" &&
              EngineJSON.ParseString("{") is null && EngineJSON.ParseString("null") is null,
            "Scalar roots, error recovery, and the static shortcut behave consistently.");

        var native = EngineJSON.FromNative(new Vector2(2, 5));
        Check(EngineJSON.ToNative<Vector2>(native) == new Vector2(2, 5) &&
              EngineJSON.ToNative<int>(EngineJSON.FromNative(7)) == 7,
            "Typed native conversion reuses the engine's existing vector and scalar JSON schemas.");
        var precise = JsonValue.Create(1.2345678901234567d)!;
        Check(EngineJSON.Stringify(precise) != EngineJSON.Stringify(precise, fullPrecision: true) &&
              JsonSerializer.Deserialize<double>(EngineJSON.Stringify(precise, fullPrecision: true)) == 1.2345678901234567d,
            "FullPrecision retains all round-trip digits of a native double.");
        Check(EngineJSON.Stringify(JsonValue.Create(double.NaN)) == "null" &&
              EngineJSON.Stringify(JsonValue.Create(double.PositiveInfinity)) == "1e99999",
            "Nonfinite floating values use the documented JSON fallback forms.");

        Reject<NotSupportedException>(() => EngineJSON.FromNative<object>(new object()));
        Reject<NotSupportedException>(() => EngineJSON.ToNative<object>(native));
        using (var owned = new Resource())
            Reject<NotSupportedException>(() => EngineJSON.FromNative(new { Owned = owned }));
        Reject<NotSupportedException>(() => EngineJSON.FromNative(new { Untyped = (object)7 }));
        Reject<JsonException>(() => EngineJSON.ToNative<Vector2>(JsonNode.Parse("\"wrong\"")));
        Reject<ArgumentNullException>(() => json.Parse(null!));
        Reject<ArgumentNullException>(() => EngineJSON.Stringify(null, null!));
        json.Dispose();
        Reject<ObjectDisposedException>(() => json.Parse("1"));
        Console.WriteLine("JSON parsing, formatting, typed conversion, duplication and failures passed.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
