using Electron2D;

internal static partial class TileCustomDataTests
{
    private static void StorageBoundaries()
    {
        using var library = new TilePatternTests.Library(TileShape.Square, TileLayout.Stacked, TileOffsetAxis.Horizontal);
        var set = library.Set; set.AddCustomDataLayer(); set.SetCustomDataLayerName(0, "Value"); set.SetCustomDataLayerType<long>(0);
        var data = library.Atlas.GetTileData(default, 0); data.SetCustomData("Value", double.PositiveInfinity);
        using var copy = (TileSet)set.Duplicate(true);
        Check(double.IsPositiveInfinity(((TileSetAtlasSource)copy.GetSource(0)).GetTileData(default, 0).GetCustomData<double>("Value")), "Copy preserves actual type differing from declared schema without conversion");
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "custom-set-" + Guid.NewGuid() + ".e2dres");
        try
        {
            ResourceSaver.Save(set, path);
            using var loaded = ResourceLoader.Load<TileSet>(path, ResourceLoader.CacheMode.Ignore);
            var restored = ((TileSetAtlasSource)loaded.GetSource(0)).GetTileData(default, 0);
            Check(loaded.GetCustomDataLayerType(0) == typeof(long) && double.IsPositiveInfinity(restored.GetCustomData<double>("Value")), "Portable graph preserves independent declared and actual types");
            using var cached = ResourceLoader.Load<TileSet>(path, ResourceLoader.CacheMode.Reuse);
            data.SetCustomData("Value", "changed"); ResourceSaver.Save(set, path);
            var replaced = ResourceLoader.Load<TileSet>(path, ResourceLoader.CacheMode.Replace);
            Check(ReferenceEquals(cached, replaced) && ((TileSetAtlasSource)replaced.GetSource(0)).GetTileData(default, 0).GetCustomData<string>("Value") == "changed", "Cache replacement preserves actual typed values and root identity");
            library.Set.RemoveSource(0); ResourceSaver.Save(library.Atlas, path);
            using var standalone = ResourceLoader.Load<TileSetAtlasSource>(path, ResourceLoader.CacheMode.Ignore);
            Check(standalone.GetTileData(default, 0).GetCustomDataByLayerID<string>(0) == "changed", "Standalone source retains indexed typed data without a set");
            Reject<InvalidOperationException>(() => standalone.GetTileData(default, 0).HasCustomData("Value"));
        }
        finally { System.IO.File.Delete(path); }
        data.SetCustomDataByLayerID(0, 42L);
        var descriptor = (PropertyDescriptor<TileData, string[]>)data.GetPropertyList().Single(p => p.Name == "CustomDataTypes");
        Reject<NotSupportedException>(() => descriptor.SetValue(data, ["unprepared.type"]));
        Check(data.GetCustomDataByLayerID<long>(0) == 42, "Unknown portable actual type rejects before replacing values");
        Reject<ArgumentException>(() => descriptor.SetValue(data, [null!]));
        Check(data.GetCustomDataByLayerID<long>(0) == 42, "Null portable type rejects before replacing values");
        var count = (PropertyDescriptor<TileSet, int>)set.GetPropertyList().Single(p => p.Name == "CustomLayerCount");
        Reject<ArgumentOutOfRangeException>(() => count.SetValue(set, int.MaxValue));
        Check(set.GetCustomDataLayersCount() == 1, "Stored count rejects excessive allocations before editing");
        set.AddSource(library.Atlas, 0);
        Reject<NotSupportedException>(() => set.SetCustomDataLayerType(0, typeof(AppValue)));
        PrepareCodec(); set.SetCustomDataLayerType(0, typeof(AppValue)); Check(data.GetCustomDataByLayerID<AppValue>(0) == default, "Application codec prepares a direct typed factory/default");
        Conversions();
    }
    private static void Conversions()
    {
        using var library = new TilePatternTests.Library(TileShape.Square, TileLayout.Stacked, TileOffsetAxis.Horizontal);
        var set = library.Set; set.AddCustomDataLayer(); var data = library.Atlas.GetTileData(default, 0);
        data.SetCustomDataByLayerID(0, new Rect2(2.75f, -3.25f, 4.5f, 5.9f)); set.SetCustomDataLayerType<Rect2i>(0);
        Check(data.GetCustomDataByLayerID<Rect2i>(0) == new Rect2i(2, -3, 4, 5), "Rectangle conversion truncates each component");
        set.SetCustomDataLayerType<Rect2>(0); Check(data.GetCustomDataByLayerID<Rect2>(0) == new Rect2(2, -3, 4, 5), "Rectangle reverse conversion");
        data.SetCustomDataByLayerID(0, new Vector4(2.75f, -3.25f, 4.5f, 5.9f)); set.SetCustomDataLayerType<Vector4i>(0);
        Check(data.GetCustomDataByLayerID<Vector4i>(0) == new Vector4i(2, -3, 4, 5), "Vector4 conversion");
        data.SetCustomDataByLayerID(0, "999999999999999999999999999"); set.SetCustomDataLayerType<long>(0); Check(data.GetCustomDataByLayerID<long>(0) == long.MaxValue, "Integer text saturates");
        data.SetCustomDataByLayerID(0, "--001dog2.8"); set.SetCustomDataLayerType<long>(0); Check(data.GetCustomDataByLayerID<long>(0) == 12, "Integer text sign and ignored characters");
        data.SetCustomDataByLayerID(0, "1.25e+suffix"); set.SetCustomDataLayerType<double>(0); Check(data.GetCustomDataByLayerID<double>(0) == 1.25, "Incomplete exponent preserves prefix");
        data.SetCustomDataByLayerID(0, 0x204080ffL); set.SetCustomDataLayerType<Color>(0); Check(data.GetCustomDataByLayerID<Color>(0).IsEqualApprox(new Color(32f / 255, 64f / 255, 128f / 255)), "Integer color conversion");
        data.SetCustomDataByLayerID(0, new[] { 1, 2 }); set.SetCustomDataLayerType<double[]>(0); Check(data.GetCustomDataByLayerID<double[]>(0).Length == 0, "Different packed array types reset rather than elementwise conversion");
        data.SetCustomDataByLayerID(0, new[] { 1, 2 }); set.SetCustomDataLayerType<string>(0); Check(data.GetCustomDataByLayerID<string>(0) == "[1, 2]", "Array text includes values");
    }
}
