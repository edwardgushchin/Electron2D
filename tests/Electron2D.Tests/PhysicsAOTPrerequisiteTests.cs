using Electron2D;

internal static class PhysicsAOTPrerequisiteTests
{
    private readonly record struct ApplicationValue(int Value);
    private class CurvedGraph : GraphEdit
    {
        internal int Calls;
        private readonly Vector2[] _points = [new(1, 2), new(4, 5)];
        protected override Vector2[] OnGetConnectionLine(Vector2 from, Vector2 to) { Calls++; return _points; }
    }
    private sealed class InheritedGraph : CurvedGraph { }
    private class HiddenGraph : GraphEdit
    {
        protected new Vector2[] OnGetConnectionLine(Vector2 from, Vector2 to) => throw new InvalidOperationException("A hidden method is not a virtual callback.");
    }
    private class FilterCode : CodeEdit
    {
        internal int Calls;
        protected override CodeCompletionOption[] OnFilterCodeCompletionCandidates(CodeCompletionOption[] candidates) { Calls++; return candidates; }
    }
    private sealed class InheritedCode : FilterCode { }
    private class HiddenCode : CodeEdit
    {
        protected new CodeCompletionOption[] OnFilterCodeCompletionCandidates(CodeCompletionOption[] candidates) => throw new InvalidOperationException("Hidden filter is not an override.");
    }
    internal static void Run()
    {
        using var graph = new InheritedGraph(); var points = graph.GetConnectionLine(Vector2.Zero, Vector2.One); points[0] = new(9, 9);
        Check(graph.Calls == 1 && graph.GetConnectionLine(Vector2.Zero, Vector2.One)[0] == new Vector2(1, 2), "Inherited graph callback runs and results are copied");
        using var hiddenGraph = new HiddenGraph(); Check(hiddenGraph.GetConnectionLine(Vector2.Zero, Vector2.One).Length == 65, "Hidden graph method preserves ordinary curve");
        using var code = new InheritedCode { CodeCompletionEnabled = true }; code.AddCodeCompletionOption(CodeEdit.CodeCompletionKind.Variable, "candidate", "candidate"); code.UpdateCodeCompletionOptions(true);
        Check(code.Calls == 1 && code.GetCodeCompletionOptions().Length == 1, "Inherited completion callback operates with no text context");
        using var hiddenCode = new HiddenCode { CodeCompletionEnabled = true }; hiddenCode.AddCodeCompletionOption(CodeEdit.CodeCompletionKind.Variable, "candidate", "candidate"); hiddenCode.UpdateCodeCompletionOptions(true);
        Check(hiddenCode.GetCodeCompletionOptions().Length == 0, "Hidden completion method preserves ordinary context filter");
        using var set = new TileSet(); using var atlas = new TileSetAtlasSource(); using var image = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8);
        using var texture = ImageTexture.CreateFromImage(image); atlas.Texture = texture; atlas.TextureRegionSize = new(16, 16); atlas.CreateTile(Vector2i.Zero); set.AddSource(atlas, 0); set.AddCustomDataLayer(); var data = atlas.GetTileData(Vector2i.Zero, 0);
        Default<int[]>(); Default<string[]>(); Default<Vector2[]>(); Default<PhysicsMaterial[]>(); Default<ApplicationValue[]>(); Default<ApplicationValue[][]>(); Default<ApplicationValue[,]>(); Default<ApplicationValue[,,]>();
        data.SetCustomDataByLayerID(0, new[] { "a\"b", "x\\y", "line\nnext" }); set.SetCustomDataLayerType<string>(0);
        Check(data.GetCustomDataByLayerID<string>(0) == "[\"a\\u0022b\", \"x\\\\y\", \"line\\nnext\"]", "Array text retains escaped JSON strings");
        Console.WriteLine("AOT prerequisites: inherited/hidden typed GUI callbacks, copied curves, empty typed arrays and escaped custom data passed.");
        void Default<T>() where T : class
        {
            data.SetCustomDataByLayerID<string?>(0, null); set.SetCustomDataLayerType<T>(0);
            Check(data.GetCustomDataByLayerID<T>(0) is Array { Length: 0 } array && array.GetType() == typeof(T) && array.Rank == typeof(T).GetArrayRank(), "Typed custom array has an empty default");
        }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
