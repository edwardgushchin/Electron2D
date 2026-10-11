using System.Text.Json;
using Electron2D;

internal static partial class TileCustomDataTests
{
    internal static void Run() { Reference(); Definitions(); Failures(); ArraysAndResources(); StorageBoundaries(); Consumers(); Console.WriteLine("Custom tile data passed: typed values/schema edits, conversions, aliases, failures, copies/files and executable consumers."); }
    private static Type? NamedType(string name) => name switch { "nil" => null, "bool" => typeof(bool), "int" => typeof(long), "float" => typeof(double), "string" => typeof(string), "vector" => typeof(Vector2), "vector_i" => typeof(Vector2i), "rect" => typeof(Rect2), "rect_i" => typeof(Rect2i), "vector4" => typeof(Vector4), "vector4_i" => typeof(Vector4i), "color" => typeof(Color), "transform" => typeof(Transform), _ => throw new InvalidOperationException(name) };
    private static void Reference()
    {
        using var library = new TilePatternTests.Library(TileShape.Square, TileLayout.Stacked, TileOffsetAxis.Horizontal); var set = library.Set; var data = library.Atlas.GetTileData(default, 0);
        using var fixture = JsonDocument.Parse(typeof(TileCustomDataTests).Assembly.GetManifestResourceStream("TestTiles.custom-data-reference.json")!); var root = fixture.RootElement;
        var types = root.GetProperty("types").EnumerateObject().ToDictionary(p => p.Value.GetInt32(), p => NamedType(p.Name)); var stages = root.GetProperty("stages"); Check(stages.GetArrayLength() == 30, "Pinned custom-data phases");
        foreach (var stage in stages.EnumerateArray())
        {
            var action = stage.GetProperty("action"); var name = action[0].GetString(); var a = action[1];
            switch (name)
            {
                case "add": set.AddCustomDataLayer(a[0].GetInt32()); break;
                case "name": set.SetCustomDataLayerName(a[0].GetInt32(), a[1].GetString()!); break;
                case "move": set.MoveCustomDataLayer(a[0].GetInt32(), a[1].GetInt32()); break;
                case "remove": set.RemoveCustomDataLayer(a[0].GetInt32()); break;
                case "type": set.SetCustomDataLayerType(a[0].GetInt32(), NamedType(a[1].GetString()!)); break;
                case "value_vector": data.SetCustomDataByLayerID(a[0].GetInt32(), new Vector2(2.75f, -3.25f)); break;
                case "set":
                    if (a[1].ValueKind is JsonValueKind.True or JsonValueKind.False) data.SetCustomDataByLayerID(a[0].GetInt32(), a[1].GetBoolean());
                    else if (a[1].ValueKind == JsonValueKind.String) data.SetCustomDataByLayerID(a[0].GetInt32(), a[1].GetString()!);
                    else if (a[1].TryGetInt64(out var integer)) data.SetCustomDataByLayerID(a[0].GetInt32(), integer);
                    else data.SetCustomDataByLayerID(a[0].GetInt32(), a[1].GetDouble());
                    break;
            }
            var expected = stage.GetProperty("layers"); Check(set.GetCustomDataLayersCount() == expected.GetArrayLength(), "Layer count"); var index = 0;
            foreach (var layer in expected.EnumerateArray())
            {
                var layerName = layer.GetProperty("name").GetString()!; Check(set.GetCustomDataLayerName(index) == layerName && set.GetCustomDataLayerType(index) == types[layer.GetProperty("schema").GetInt32()], "Layer schema/name");
                if (layerName.Length > 0) Check(set.GetCustomDataLayerByName(layerName) == index && data.HasCustomData(layerName), "Current name lookup follows moves/rename");
                var actual = layer.GetProperty("actual").GetInt32(); var value = layer.GetProperty("value");
                if (name == "type" && a[1].GetString() == "string") Check(data.GetCustomDataByLayerID<string>(index) == "12", "Declared scalar-to-string conversion produces typed string instead of failed-constructor nil");
                else switch (actual)
                    {
                        case 0: Check(data.CustomValues[index] is null, "Nil value"); break;
                        case 1: Check(data.GetCustomDataByLayerID<bool>(index) == value.GetBoolean(), "Bool value"); break;
                        case 2: Check(data.GetCustomDataByLayerID<long>(index) == value.GetInt64(), "Integer value"); break;
                        case 3: Check(Math.Abs(data.GetCustomDataByLayerID<double>(index) - value.GetDouble()) < 1e-10, "Floating value"); break;
                        case 4: Check(data.GetCustomDataByLayerID<string>(index) == value.GetString(), "String value"); break;
                        case 5: Check(data.GetCustomDataByLayerID<Vector2>(index).IsEqualApprox(new(value[0].GetSingle(), value[1].GetSingle())), "Vector value"); break;
                        case 6: Check(data.GetCustomDataByLayerID<Vector2i>(index) == new Vector2i(value[0].GetInt32(), value[1].GetInt32()), "Integer vector value"); break;
                        case 11: Check(data.GetCustomDataByLayerID<Transform>(index) == Transform.Identity, "Identity transform default"); break;
                        case 20: Check(data.GetCustomDataByLayerID<Color>(index).IsEqualApprox(new(value[0].GetSingle(), value[1].GetSingle(), value[2].GetSingle(), value[3].GetSingle())), "Color default/conversion"); break;
                    }
                index++;
            }
        }
        Check(!set.HasCustomDataLayerByName("B") && !set.HasCustomDataLayerByName("C") && !set.HasCustomDataLayerByName(string.Empty), "Renamed/removed/empty aliases do not survive");
    }
    private static void Definitions()
    {
        using var library = new TilePatternTests.Library(TileShape.Square, TileLayout.Stacked, TileOffsetAxis.Horizontal); var set = library.Set; var first = library.Atlas.GetTileData(default, 0); var alternative = library.Atlas.CreateAlternativeTile(default); var second = library.Atlas.GetTileData(default, alternative);
        for (var i = 0; i < 3; i++) { set.AddCustomDataLayer(); set.SetCustomDataLayerName(i, "L" + i); set.SetCustomDataLayerType<long>(i); first.SetCustomDataByLayerID(i, 10L + i); second.SetCustomDataByLayerID(i, 20L + i); }
        set.MoveCustomDataLayer(0, 3); Check(first.GetCustomData<long>("L0") == 10 && second.GetCustomData<long>("L0") == 20 && set.GetCustomDataLayerByName("L0") == 2, "Forward move preserves values and lookup");
        set.MoveCustomDataLayer(2, 0); set.AddCustomDataLayer(1); Check(first.GetCustomData<long>("L1") == 11 && first.GetCustomDataByLayerID<int>(1) == 0, "Insertion preserves identity and nil"); set.RemoveCustomDataLayer(1);
        Reject<ArgumentException>(() => set.SetCustomDataLayerName(0, "L1")); Reject<KeyNotFoundException>(() => first.GetCustomData<string>("L0")); Reject<KeyNotFoundException>(() => first.SetCustomData("Unknown", false)); Reject<ArgumentOutOfRangeException>(() => first.GetCustomDataByLayerID<int>(-1));
        using var clone = (TileSet)set.Duplicate(); var clonedData = ((TileSetAtlasSource)clone.GetSource(0)).GetTileData(default, 0); clonedData.SetCustomData("L0", 99L); Check(first.GetCustomData<long>("L0") == 10 && clonedData.GetCustomData<long>("L0") == 99, "Independent value holders in set/source copies");
        var sum = 0L; for (var i = 0; i < 32; i++) { sum += first.GetCustomData<long>("L0"); first.SetCustomData("L0", 10L); }
        var bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 1024; i++) { sum += first.GetCustomDataByLayerID<long>(0) + (first.HasCustomData("L0") ? 1 : 0); first.SetCustomData("L0", 10L); }
        Check(GC.GetAllocatedBytesForCurrentThread() == bytes && sum > 0, "Warmed exact typed reads/writes and name lookup allocate zero owner bytes");
    }
    private static void Failures()
    {
        using var library = new TilePatternTests.Library(TileShape.Square, TileLayout.Stacked, TileOffsetAxis.Horizontal); var set = library.Set; var alt = library.Atlas.CreateAlternativeTile(default); var sibling = library.Atlas.GetTileData(default, alt); set.AddCustomDataLayer(); set.SetCustomDataLayerType<double>(0); sibling.SetCustomDataByLayerID(0, 2d); var data = library.Atlas.GetTileData(default, 0); data.SetCustomDataByLayerID(0, double.PositiveInfinity);
        Reject<OverflowException>(() => set.SetCustomDataLayerType<long>(0)); Check(set.GetCustomDataLayerType(0) == typeof(double) && double.IsPositiveInfinity(data.GetCustomDataByLayerID<double>(0)), "Conversion failure leaves definitions and all values unchanged"); data.SetCustomDataByLayerID(0, 1d);
        var siblingChanges = 0; sibling.Changed += () => siblingChanges++;
        Action<ElectronObject> fail = _ => throw new InvalidOperationException("Expected custom-data listener failure."); data.PropertyListChanged += fail; Reject<InvalidOperationException>(() => set.AddCustomDataLayer(0)); data.PropertyListChanged -= fail;
        Check(set.GetCustomDataLayersCount() == 2 && data.GetCustomDataByLayerID<double>(1) == 1d, "Complete insertion before callback failure");
        Check(sibling.GetCustomDataByLayerID<double>(1) == 2d && siblingChanges > 0, "Later alternatives commit and notify despite first observer failure");
        Action<ElectronObject> reenter = _ => set.AddCustomDataLayer(); data.PropertyListChanged += reenter; Reject<InvalidOperationException>(() => set.MoveCustomDataLayer(1, 0)); data.PropertyListChanged -= reenter; Check(set.GetCustomDataLayersCount() == 2 && data.GetCustomDataByLayerID<double>(0) == 1d, "Reentrant schema edit rejects after coherent commitment");
        set.AddCustomDataLayer(); Check(set.GetCustomDataLayersCount() == 3, "Edit guard recovers");
        using var target = new TileSet(); target.AddCustomDataLayer(); target.SetCustomDataLayerType<long>(0); data.SetCustomDataByLayerID(0, double.PositiveInfinity); Reject<OverflowException>(() => target.AddSource(library.Atlas, 0)); Check(set.HasSource(0) && !target.HasSource(0), "Transfer preflight preserves original source ownership on failed conversion");
    }
    private static void ArraysAndResources()
    {
        using var library = new TilePatternTests.Library(TileShape.Square, TileLayout.Stacked, TileOffsetAxis.Horizontal); var set = library.Set; set.AddCustomDataLayer(); set.SetCustomDataLayerName(0, "Array"); set.SetCustomDataLayerType<int[]>(0); var data = library.Atlas.GetTileData(default, 0); int[] values = [1, 2]; data.SetCustomData("Array", values);
        using var shallow = (TileSet)set.Duplicate(); using var deep = (TileSet)set.Duplicate(true); var shallowData = ((TileSetAtlasSource)shallow.GetSource(0)).GetTileData(default, 0); var deepData = ((TileSetAtlasSource)deep.GetSource(0)).GetTileData(default, 0);
        Check(ReferenceEquals(shallowData.GetCustomData<int[]>("Array"), values) && !ReferenceEquals(deepData.GetCustomData<int[]>("Array"), values), "Array sharing/deep copies follow graph policy");
        set.AddCustomDataLayer(); set.SetCustomDataLayerName(1, "Material"); set.SetCustomDataLayerType<Resource>(1); using var material = new PhysicsMaterial { Friction = .5f }; data.SetCustomData<Resource>("Material", material);
        using var resourceDeep = (TileSet)set.Duplicate(true); var copy = ((TileSetAtlasSource)resourceDeep.GetSource(0)).GetTileData(default, 0).GetCustomData<Resource>("Material"); Check(copy is PhysicsMaterial && copy != material, "Nested resources participate in deep duplication");
        var runtime = library.Atlas.GetTileData(default, 0).Copy();
        try { runtime.SetCustomData("Array", new[] { 5, 6 }); Check(data.GetCustomData<int[]>("Array")[0] == 1, "Runtime value replacement does not replace authored value"); }
        finally { runtime.Release(); }
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
