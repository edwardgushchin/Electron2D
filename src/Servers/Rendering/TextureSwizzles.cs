using static Electron2D.MaterialLightingProgram;

namespace Electron2D;

internal readonly record struct TextureSwizzleKey(ulong A, ulong B, ulong C, ulong D, ulong E, ulong F, ulong G, ulong H)
{
    internal static TextureSwizzleKey Create(ReadOnlySpan<uint> values)
    {
        if (values.Length > 32) throw new NotSupportedException("Sampled component maps exceed two native stages of sixteen textures.");
        Span<ulong> packed = stackalloc ulong[8]; packed.Clear();
        for (var i = 0; i < values.Length; i++)
        {
            if ((values[i] & ~4095u) != 0) throw new ArgumentOutOfRangeException(nameof(values));
            packed[i / 4] |= (ulong)values[i] << ((i % 4) * 16);
        }
        return new(packed[0], packed[1], packed[2], packed[3], packed[4], packed[5], packed[6], packed[7]);
    }
}

internal static class TextureSwizzles
{
    internal static uint Capture(RDTextureView view)
    {
        static uint Lane(RenderingDevice.TextureSwizzle value, int lane) => value == RenderingDevice.TextureSwizzle.Identity || (int)value == lane + 3 ? 0 : (uint)value;
        return Lane(view.SwizzleR, 0) | (Lane(view.SwizzleG, 1) << 3) | (Lane(view.SwizzleB, 2) << 6) | (Lane(view.SwizzleA, 3) << 9);
    }
    internal static uint Source(Resource? resource) => ShaderTexture.Source(resource) switch
    {
        TextureRD texture => texture.DeviceInfo?.Swizzle ?? 0,
        TextureLayeredRD texture => texture.DeviceInfo?.Swizzle ?? 0,
        _ => 0
    };
}

internal sealed class TextureSwizzleCache
{
    private sealed class Cached(byte[] code, long epoch) { internal readonly byte[] Code = code; internal long Epoch = epoch; }
    private readonly Dictionary<(byte[] Code, TextureSwizzleKey Swizzles), Cached> _modules = [];
    private long _epoch;
    internal void BeginFrame() { _epoch++; Trim(0); }
    private long _bytes;
    internal byte[] Get(byte[] code, ReadOnlySpan<uint> maps)
    {
        var swizzles = TextureSwizzleKey.Create(maps); if (swizzles == default) return code;
        var key = (code, swizzles); if (_modules.TryGetValue(key, out var existing)) { existing.Epoch = _epoch; return existing.Code; }
        var module = new Module(code); var source = module.Instructions;
        var types = source.Where(i => Op(i) is >= 19 and < 39).ToDictionary(i => i[1]);
        var decorations = source.Where(i => Op(i) == 71 && i.Length == 4 && i[2] == 33).ToDictionary(i => i[1], i => (int)i[3]);
        var images = new Dictionary<int, uint[]>();
        foreach (var variable in source.Where(i => Op(i) == 59 && i[3] == 0))
        {
            var type = types[types[variable[1]][3]]; if (Op(type) == 27) type = types[type[2]];
            if (Op(type) != 25 || type[7] != 1) continue;
            var binding = decorations[variable[2]]; if (!images.TryAdd(binding, type)) throw new ArgumentException("Native sampled image slots must be unique.", nameof(code));
        }
        var count = images.Count == 0 ? 0 : images.Keys.Max() + 1;
        if (count > maps.Length) throw new ArgumentException("Every native image requires a component map.", nameof(maps));
        var textures = new ShaderTexture[count];
        for (var slot = 0; slot < count; slot++)
        {
            var image = images.GetValueOrDefault(slot); var scalar = image is null ? null : types[image[2]];
            textures[slot] = new("swizzle_" + slot, slot, image is not null && image[5] != 0, scalar is null || Op(scalar) == 22 ? 0 : scalar[3] == 1 ? 1 : 2);
        }
        var result = CanvasSamplerArrays.Lower(code, textures, swizzles: maps[..count].ToArray(), native: true);
        var size = (long)code.Length + result.Length;
        Trim(size);
        _modules.Add(key, new(result, _epoch)); _bytes += size; return result;
    }
    private void Trim(long size)
    {
        while (_modules.Count + (size == 0 ? 0 : 1) > 64 || _bytes + size > 32 * 1024 * 1024 && _modules.Count != 0)
        {
            var candidate = default((byte[] Code, TextureSwizzleKey Swizzles));
            foreach (var pair in _modules) if (_epoch == 0 || pair.Value.Epoch < _epoch - 1) { candidate = pair.Key; break; }
            if (candidate.Code is null) break;
            _bytes -= candidate.Code.Length + _modules[candidate].Code.Length; _modules.Remove(candidate);
        }
    }
    internal void Clear() { _modules.Clear(); _bytes = 0; }
}
