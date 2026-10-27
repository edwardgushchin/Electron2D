using Electron2D;

internal static partial class ShaderSamplerChecks
{
    internal static void Run(string[] args)
    {
        if (args.Length > 0 && args[0] == "coordinates-glyph") { CoordinatesGlyphGuard(); return; }
        if (args.Length > 0 && args[0] == "coordinates") { Coordinates(); return; }
        if (args.Length > 0 && args[0] == "coordinates-canvas") { CoordinatesCanvas(); return; }
        if (args.Length > 0 && args[0] == "borders-canvas") { BordersCanvas(); return; }
        if (args.Length > 0 && args[0] == "borders") { Borders(); return; }
        if (args.Length > 0 && args[0] == "swizzles-canvas") { SwizzleCanvas(args); return; }
        if (args.Length > 0 && args[0] == "swizzles-device") { TextureSwizzleRuntimeChecks.Run(System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", "Device")); return; }
        if (args.Length > 0 && args[0] == "compatibility") { Compatibility(); return; }
        if (args.Length > 0 && args[0] == "screen") { Screen(args[1]); return; }
        if (args.Length > 0 && args[0] == "native") { Native(args[1]); return; }
        if (args.Length > 0 && args[0] == "archive-child") { ArchiveChild(args[1]); return; }
        using var shader = new Shader();
        Check(shader.GetTextureSamplerState("TEXTURE") is null, "Unset binding retains its original policy.");
        using var state = new RDSamplerState
        {
            BorderColor = RenderingDevice.SamplerBorderColor.FloatOpaqueWhite,
            RepeatU = RenderingDevice.SamplerRepeatMode.MirroredRepeat,
            RepeatV = RenderingDevice.SamplerRepeatMode.Repeat,
            MinFilter = RenderingDevice.SamplerFilter.Linear,
            MIPFilter = RenderingDevice.SamplerFilter.Linear,
            MinLOD = 1,
            MaxLOD = 2,
            LODBias = .5f,
            UseAnisotropy = true,
            AnisotropyMax = 4,
            CompareOp = RenderingDevice.CompareOperator.Greater
        };
        var changed = 0; shader.Changed += _ => changed++;
        shader.SetTextureSamplerState("TEXTURE", state); state.MinLOD = 0; state.Dispose();
        using var first = shader.GetTextureSamplerState("TEXTURE")!;
        Check(changed == 1 && first.MinLOD == 1 && first.RepeatU == RenderingDevice.SamplerRepeatMode.MirroredRepeat, "Input is copied rather than borrowed.");
        first.RepeatU = RenderingDevice.SamplerRepeatMode.Repeat;
        using var second = shader.GetTextureSamplerState("TEXTURE")!;
        Check(second.RepeatU == RenderingDevice.SamplerRepeatMode.MirroredRepeat && !ReferenceEquals(first, second), "Getter returns an independent caller-owned state.");
        using var duplicate = (Shader)shader.Duplicate();
        duplicate.SetTextureSamplerState("TEXTURE", null); using var original = shader.GetTextureSamplerState("TEXTURE"); Check(duplicate.GetTextureSamplerState("TEXTURE") is null && original is not null, "Duplicate metadata mutates independently.");
        var descriptor = (PropertyDescriptor<Shader, byte[]>)shader.GetPropertyList().Single(p => p.Name == "SamplerSettings");
        var bytes = descriptor.GetValue(shader); using var restored = new Shader();
        var target = (PropertyDescriptor<Shader, byte[]>)restored.GetPropertyList().Single(p => p.Name == "SamplerSettings");
        target.SetValue(restored, bytes); using var roundtrip = restored.GetTextureSamplerState("TEXTURE")!;
        Check(roundtrip.MinLOD == 1 && roundtrip.LODBias == .5f && roundtrip.MaxLOD == 2 && roundtrip.MIPFilter == RenderingDevice.SamplerFilter.Linear && roundtrip.MinFilter == RenderingDevice.SamplerFilter.Linear && roundtrip.MagFilter == RenderingDevice.SamplerFilter.Nearest && roundtrip.RepeatV == RenderingDevice.SamplerRepeatMode.Repeat && roundtrip.UseAnisotropy && roundtrip.AnisotropyMax == 4 && !roundtrip.EnableCompare && roundtrip.CompareOp == RenderingDevice.CompareOperator.Greater && roundtrip.BorderColor == RenderingDevice.SamplerBorderColor.FloatOpaqueWhite, "Typed stored metadata roundtrip.");
        for (var i = 1; i < bytes.Length; i++)
        {
            var truncated = bytes.AsSpan(0, i).ToArray(); Reject<Exception>(() => target.SetValue(restored, truncated));
        }
        MalformedMetadata(restored, target, bytes);
        using var unchanged = restored.GetTextureSamplerState("TEXTURE")!; Check(unchanged.MinLOD == 1, "Malformed metadata preserves the previous table.");
        using var invalid = new RDSamplerState { MaxLOD = -1 };
        Reject<ArgumentException>(() => shader.SetTextureSamplerState("TEXTURE", invalid));
        invalid.MaxLOD = 1; invalid.RepeatU = RenderingDevice.SamplerRepeatMode.MirrorClampToEdge;
        Reject<NotSupportedException>(() => shader.SetTextureSamplerState("TEXTURE", invalid));
        invalid.RepeatU = RenderingDevice.SamplerRepeatMode.ClampToEdge; invalid.EnableCompare = true;
        Reject<NotSupportedException>(() => shader.SetTextureSamplerState("TEXTURE", invalid));
        Reject<ArgumentException>(() => shader.SetTextureSamplerState("absent", null));
        Reject<ArgumentOutOfRangeException>(() => shader.SetTextureSamplerState("TEXTURE", null, -1));
        Reject<ArgumentOutOfRangeException>(() => shader.SetTextureSamplerState("TEXTURE", null, 1));
        Reject<ObjectDisposedException>(() => shader.SetTextureSamplerState("TEXTURE", state));
        shader.SetTextureSamplerState("TEXTURE", null); Check(shader.GetTextureSamplerState("TEXTURE") is null && changed == 2, "Clearing restores policy; failures published no events.");
        Archives(restored);
        ReloadAndCapacity();
        shader.Dispose(); Reject<ObjectDisposedException>(() => shader.GetTextureSamplerState("TEXTURE"));
        Check(!RenderingServer.IsAvailable, "Authoring does not open a renderer.");
        Console.WriteLine("Sampler authoring: copied state, independent duplicates, stored roundtrip, all truncations, invalid states and disposal passed.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
