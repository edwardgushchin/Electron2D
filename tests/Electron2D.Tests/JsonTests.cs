using System.Diagnostics;
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
        Check(EngineJSON.Stringify(json.Data) == "{\"a\":[true,null],\"z\":1.0}" &&
              EngineJSON.Stringify(json.Data, sortKeys: false) == "{\"z\":1.0,\"a\":[true,null]}" &&
              EngineJSON.Stringify(json.Data, "..") == "{\n..\"a\": [\n....true,\n....null\n..],\n..\"z\": 1.0\n}",
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
        VerifyDocumentState();
        VerifyTypedConversion();
        VerifyFormatting();
        VerifyDocumentParsing();
        Console.WriteLine("JSON parsing, formatting, typed conversion, duplication and failures passed.");
    }

    private static void VerifyDocumentParsing()
    {
        using var json = new EngineJSON();
        const string permissive = "{\"line\":\"a\nb\",\"lead\":01,\"fraction\":-.5,\"end\":1.,\"same\":1,\"same\":2,}";
        Check(json.Parse(permissive, keepText: true) && json.Data is JsonObject value &&
              value["line"]!.GetValue<string>() == "a\nb" && value["lead"]!.GetValue<int>() == 1 &&
              value["fraction"]!.GetValue<double>() == -0.5 && value["end"]!.GetValue<int>() == 1 &&
              value["same"]!.GetValue<int>() == 2 && json.GetParsedText() == permissive &&
              EngineJSON.ParseString(permissive)!["same"]!.GetValue<int>() == 2,
            "Both parse entry points accept raw string lines, permissive number forms, trailing commas and last-key wins.");
        Check(json.Parse("[true,false,null,]\v") && json.Data is JsonArray { Count: 3 } &&
              json.GetErrorLine() == 0 && json.GetErrorMessage() == string.Empty,
            "Document whitespace and trailing commas follow the same token path.");
        Check(json.Parse("9007199254740993") && json.Data!.GetValue<double>() == 9007199254740992d &&
              EngineJSON.ToNative<long>(json.Data) == 9007199254740992L,
            "Document numbers use the parsed floating value instead of retaining extra integer digits.");
        Check(json.Parse("0000000000000000001") && json.Data!.GetValue<int>() == 0 &&
              json.Parse("1e4000") && double.IsPositiveInfinity(json.Data!.GetValue<double>()) &&
              json.Parse("1e-4000") && json.Data!.GetValue<int>() == 0,
            "Numeric conversion keeps the first eighteen mantissa digits and caps very large exponents.");
        Check(json.Parse("\"\\uD83D\\uDE00\"") && json.Data!.GetValue<string>() == "😀",
            "A valid escaped surrogate pair produces one Unicode scalar.");

        Check(!json.Parse("\"\\uD83D\"") && json.GetErrorMessage() ==
              "Invalid UTF-16 sequence in string, unpaired lead surrogate" && json.GetErrorLine() == 0,
            "An unpaired lead surrogate reports the document diagnostic.");
        Check(!json.Parse("\"\\uDE00\"") && json.GetErrorMessage() ==
              "Invalid UTF-16 sequence in string, unpaired trail surrogate",
            "An unpaired trail surrogate reports its own diagnostic.");
        Check(!json.Parse("\"\\u12xz\"") && json.GetErrorMessage() == "Malformed hex constant in string" &&
              EngineJSON.ParseString("\"\\v\"") is null,
            "Malformed hex and unsupported escapes fail consistently.");
        Check(!json.Parse("{\"a\" 1}") && json.GetErrorMessage() == "Expected ':'" &&
              !json.Parse("[1 2]") && json.GetErrorMessage() == "Expected ','" &&
              !json.Parse("1 trailing") && json.GetErrorMessage() == "Expected 'EOF'" &&
              !json.Parse("[") && json.GetErrorMessage() == "Expected ']'",
            "Structural failures report the offending token or missing closer.");
        Check(!json.Parse("[\n bad]") && json.GetErrorLine() == 1 && json.GetErrorMessage() ==
              "Expected 'true', 'false', or 'null', got 'bad'",
            "Diagnostic line counting follows literal newlines.");
        (string Source, string Message)[] failures =
        [
            ("", "Unknown error getting token"),
            (" \t", "Expected value, got 'EOF'"),
            ("$", "Unexpected character"),
            ("{a:1}", "Expected key"),
            ("{\"a\":1 \"b\":2}", "Expected '}' or ','"),
            ("\"unterminated", "Unterminated string"),
            ("\"\\v\"", "Invalid escape sequence"),
            ("null false", "Expected 'EOF'"),
        ];
        foreach (var (source, message) in failures)
            Check(!json.Parse(source) && json.GetErrorMessage() == message && json.GetErrorLine() == 0 &&
                  EngineJSON.ParseString(source) is null,
                $"Both parse entry points must report or reject {source} with {message}.");

        var withinLimit = new string('[', 1024) + "0" + new string(']', 1024);
        Check(json.Parse(withinLimit) && json.Data is JsonArray &&
              EngineJSON.ParseString(withinLimit) is JsonArray,
            "Both parse entry points accept the documented nesting boundary.");
        var tooDeep = '[' + withinLimit + ']';
        Check(!json.Parse(tooDeep) && json.Data is null &&
              json.GetErrorMessage() == "JSON structure is too deep" &&
              EngineJSON.ParseString(tooDeep) is null,
            "Both entry points reject the next nesting level without exposing partial data.");
    }

    private static void VerifyFormatting()
    {
        Check(EngineJSON.Stringify(JsonValue.Create(1)) == "1" &&
              EngineJSON.Stringify(JsonNode.Parse("[1,1e3]")) == "[1.0,1000.0]" &&
              EngineJSON.Stringify(JsonValue.Create(0.0)) == "0.0" &&
              EngineJSON.Stringify(JsonValue.Create(-0.0)) == "0.0",
            "Parsed numbers are floating values, while explicitly typed integer nodes stay integers.");
        Check(EngineJSON.Stringify(JsonValue.Create(1.2345678901234567d)) == "1.23456789012346" &&
              EngineJSON.Stringify(JsonValue.Create(1e-30d)) == "0.000000000000000000000000000001" &&
              EngineJSON.Stringify(JsonValue.Create(1e15d)) == "1000000000000000.0" &&
              EngineJSON.Stringify(JsonValue.Create(1e-5d), fullPrecision: true) == "1e-05" &&
              EngineJSON.Stringify(JsonValue.Create(1e15d), fullPrecision: true) == "1e+15" &&
              EngineJSON.Stringify(JsonValue.Create(0.2f), fullPrecision: true) == "0.20000000298023224",
            "Default and full precision follow the pinned fixed and shortest round-trip formats.");
        Check(EngineJSON.Stringify(JsonValue.Create(BitConverter.Int64BitsToDouble(unchecked((long)0xC31B19CD70E65EBDUL))),
                  fullPrecision: true) == "-1.9070486310808793e+15" &&
              EngineJSON.Stringify(JsonValue.Create(BitConverter.Int64BitsToDouble(unchecked((long)0xCD2B5B300586517DUL))),
                  fullPrecision: true) == "-5.6268442801468946e+63",
            "Full precision follows the pinned digit selection where managed round-trip formatting differs.");
        Check(EngineJSON.Stringify(JsonValue.Create("é<>&\v\n\"\\")) == "\"é<>&\\v\\n\\\"\\\\\"" &&
              EngineJSON.Stringify(JsonNode.Parse("\"é\"")) == "\"é\"",
            "String values retain Unicode and use the reference escapes for special characters.");

        var keys = new JsonObject { ["😀"] = 2, ["\uE000"] = 1 };
        Check(EngineJSON.Stringify(keys) == "{\"\uE000\":1,\"😀\":2}" &&
              EngineJSON.Stringify(keys, sortKeys: false) == "{\"😀\":2,\"\uE000\":1}",
            "Sorted keys use Unicode scalar order; insertion order survives when sorting is disabled.");

        JsonNode deep = JsonValue.Create(true)!;
        for (var index = 0; index < 129; index++) deep = new JsonArray(deep);
        Check(EngineJSON.Stringify(deep) == new string('[', 129) + "true" + new string(']', 129),
            "A document deeper than the former 128-level limit must format completely.");
        for (var index = 129; index < 1024; index++) deep = new JsonArray(deep);
        Check(EngineJSON.Stringify(deep) == new string('[', 1024) + "true" + new string(']', 1024),
            "The documented nesting limit must still format the final permitted level.");
        using var trace = new StringWriter();
        using var listener = new TextWriterTraceListener(trace);
        Trace.Listeners.Add(listener);
        try
        {
            deep = new JsonArray(deep);
            Check(EngineJSON.Stringify(deep) == new string('[', 1025) + "..." + new string(']', 1025),
                "An array beyond the nesting limit emits an ellipsis at the rejected value.");
            JsonNode deepObject = new JsonObject { ["key"] = true };
            for (var index = 0; index < 1024; index++) deepObject = new JsonArray(deepObject);
            Check(EngineJSON.Stringify(deepObject) == new string('[', 1024) + "{...:...}" + new string(']', 1024),
                "Object keys and values beyond the nesting limit emit ellipses at their own depth.");
            Trace.Flush();
            Check(trace.ToString().Contains("JSON structure is too deep. Bailing.", StringComparison.Ordinal),
                "Depth truncation must report its diagnostic through the managed trace listeners.");
        }
        finally { Trace.Listeners.Remove(listener); }
    }

    private static void VerifyDocumentState()
    {
        using var json = new EngineJSON();
        const string original = "{\"nested\":{\"value\":1}}";
        Check(json.Parse(original, keepText: true) && json.GetParsedText() == original,
            "Successful parsing must retain the exact requested source text.");
        using var duplicate = (EngineJSON)json.Duplicate();
        var borrowed = (JsonObject)json.Data!;
        borrowed["nested"]!["value"] = 2;
        Check(json.Data!["nested"]!["value"]!.GetValue<int>() == 2 &&
              duplicate.Data!["nested"]!["value"]!.GetValue<int>() == 1 &&
              duplicate.GetParsedText() == original,
            "The getter must expose live data while resource duplication owns an independent snapshot.");

        var supplied = JsonNode.Parse("{\"nested\":{\"value\":3}}")!;
        json.Data = supplied;
        supplied["nested"]!["value"] = 4;
        Check(json.Data!["nested"]!["value"]!.GetValue<int>() == 3 &&
              json.GetParsedText() == string.Empty && json.GetErrorMessage() == string.Empty &&
              json.GetErrorLine() == 0,
            "Data assignment must deep-copy input and clear source and diagnostic state.");
        borrowed = (JsonObject)json.Data!;
        json.Data = borrowed;
        borrowed["nested"]!["value"] = 5;
        Check(json.Data!["nested"]!["value"]!.GetValue<int>() == 3,
            "Reassigning a borrowed live tree must replace it with an independent copy.");

        const string malformed = "[1,\r\n broken]";
        const string expected = "Expected 'true', 'false', or 'null', got 'broken'";
        Check(!json.Parse(malformed, keepText: true) && json.Data is null &&
              json.GetErrorLine() == 1 && json.GetErrorMessage() == expected && json.GetParsedText() == malformed,
            "A failed parse must atomically store the document parser's line, message and requested source.");
        using var failedCopy = (EngineJSON)json.Duplicate();
        Check(failedCopy.Data is null && failedCopy.GetErrorLine() == 1 &&
              failedCopy.GetErrorMessage() == expected && failedCopy.GetParsedText() == malformed,
            "Resource duplication must copy failure diagnostics and retained source without aliases.");

        Check(json.Parse("null") && json.Data is null && json.GetErrorLine() == 0 &&
              json.GetErrorMessage() == string.Empty && json.GetParsedText() == string.Empty,
            "A valid JSON null must clear prior failure diagnostics and unrequested retained text.");
        Check(json.Parse("true", keepText: true) && json.GetParsedText() == "true",
            "Text retention must resume on the next requested parse.");
        json.Data = null;
        Check(json.Data is null && json.GetParsedText() == string.Empty &&
              json.GetErrorLine() == 0 && json.GetErrorMessage() == string.Empty,
            "Assigning a null document must reset all prior source and diagnostic state.");

        json.Dispose();
        Reject<ObjectDisposedException>(() => _ = json.Data);
        Reject<ObjectDisposedException>(() => json.Data = JsonValue.Create(1));
        Reject<ObjectDisposedException>(() => _ = json.GetErrorLine());
        Reject<ObjectDisposedException>(() => _ = json.GetErrorMessage());
        Reject<ObjectDisposedException>(() => _ = json.GetParsedText());
    }

    private static void VerifyTypedConversion()
    {
        static bool RoundTrip<T>(T value) where T : notnull =>
            EqualityComparer<T>.Default.Equals(EngineJSON.ToNative<T>(EngineJSON.FromNative(value)), value);

        Check(RoundTrip(true) && RoundTrip(123) && RoundTrip(123L) && RoundTrip(1.25f) &&
              RoundTrip(1.2345678901234567d) && RoundTrip("text") &&
              RoundTrip(new Vector2(2f, 5f)) && RoundTrip(new Vector2i(2, 5)) &&
              RoundTrip(new Vector3(1f, 2f, 3f)) && RoundTrip(new Vector3i(1, 2, 3)) &&
              RoundTrip(new Vector4(1f, 2f, 3f, 4f)) && RoundTrip(new Vector4i(1, 2, 3, 4)) &&
              RoundTrip(new Color(0.2f, 0.4f, 0.6f, 1f)) &&
              RoundTrip(new Rect(1f, 2f, 3f, 4f)) && RoundTrip(new Rect2i(1, 2, 3, 4)) &&
              RoundTrip(new Transform(0f, new Vector2(2f, 3f))),
            "Typed native conversion must round-trip every registered scalar and engine math schema.");
        Check(EngineJSON.FromNative<string?>(null) is null &&
              EngineJSON.ToNative<string>(null) is null && EngineJSON.ToNative<int>(null) == 0,
            "Typed native conversion must preserve null and default values by selected destination type.");

        var model = new NativeModel { Name = "ship", Position = new Vector2(2f, 5f), Values = [1, 2] };
        var node = EngineJSON.FromNative(model)!;
        model.Values[0] = 9;
        Check(node["Values"]![0]!.GetValue<int>() == 1,
            "FromNative must create an independent tree instead of retaining collection aliases.");
        var decoded = EngineJSON.ToNative<NativeModel>(node)!;
        ((JsonArray)node["Values"]!)[0] = 7;
        Check(decoded.Name == "ship" && decoded.Position == new Vector2(2f, 5f) &&
              decoded.Values.SequenceEqual([1, 2]),
            "ToNative must decode a caller-selected model without retaining tree aliases.");
        Reject<JsonException>(() => EngineJSON.ToNative<NativeModel>(JsonNode.Parse("\"wrong\"")));
        Reject<NotSupportedException>(() => EngineJSON.FromNative<object>(null!));
        Reject<NotSupportedException>(() => EngineJSON.ToNative<object>(node));
        using var owned = new Resource();
        Reject<NotSupportedException>(() => EngineJSON.FromNative(new { Owned = owned }));
        Reject<NotSupportedException>(() => EngineJSON.ToNative<NativeWithResource>(node));
    }

    private sealed class NativeModel
    {
        public string Name { get; set; } = string.Empty;
        public Vector2 Position { get; set; }
        public List<int> Values { get; set; } = [];
    }

    private sealed class NativeWithResource
    {
        public Resource? Owned { get; set; }
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
