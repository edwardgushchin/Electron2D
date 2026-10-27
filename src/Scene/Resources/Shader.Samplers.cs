using System.Buffers.Binary;
using System.Text;

namespace Electron2D;

public partial class Shader
{
    private readonly Dictionary<(string Name, int Index), SamplerConfiguration> _samplerStates = [];

    /// <summary>Copies explicit filtering, addressing and mip selection for a reflected sampled-image element.</summary>
    /// <param name="name">The exact reflected binding name, including renderer-owned and GLOBAL_ names.</param>
    /// <param name="state">The caller-owned state to copy, or null to restore this binding's ordinary sampler policy.</param>
    /// <param name="index">The zero-based descriptor-array element, or zero for a single image.</param>
    /// <remarks>Later state edits/disposal do not change the shader. Settings belong to this shader, independently of
    /// the image bound by a material or default. They persist and copy with it. Compatible program replacement retains
    /// them; removed or differently typed bindings lose them. Native sampler creation occurs during renderer preparation.
    /// Compatibility cannot execute custom shader programs. Nonzero LOD bias remains unavailable on Metal; relaxed
    /// devices can reject anisotropy. Mirror-clamp native preparation requires its negotiated Vulkan extension.
    /// Configure the input state without concurrent mutation.</remarks>
    /// <exception cref="ArgumentException">The name is absent, the LOD interval is invalid, or an integer image requests linear filtering.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The element or a configuration value is invalid.</exception>
    /// <exception cref="NotSupportedException">Comparison sampling is unavailable for this shader binding.</exception>
    /// <exception cref="ObjectDisposedException">This shader or the supplied state is disposed.</exception>
    public void SetTextureSamplerState(string name, RDSamplerState? state, int index = 0)
    {
        var configuration = state is null ? (SamplerConfiguration?)null : SamplerConfiguration.Capture(state);
        lock (_codeGate)
        {
            ThrowIfDisposed(); var descriptor = _program.Textures[FindSampler(_program, name, index)];
            if (configuration is { } value) ValidateSampler(descriptor, value);
            if (configuration is { } settings) _samplerStates[(name, index)] = settings;
            else _samplerStates.Remove((name, index));
        }
        EmitChanged();
    }

    /// <summary>Returns an independent copy of this binding's explicit sampler state.</summary>
    /// <param name="name">The exact reflected binding name.</param><param name="index">The descriptor-array element.</param>
    /// <returns>A caller-owned disposable copy, or null when the ordinary binding policy is active.</returns>
    /// <remarks>This is an allocating authoring query. Mutating or disposing the result never changes the shader.</remarks>
    /// <exception cref="ArgumentException">The binding name is absent.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The element is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The shader is disposed.</exception>
    public RDSamplerState? GetTextureSamplerState(string name, int index = 0)
    {
        lock (_codeGate)
        {
            ThrowIfDisposed(); _ = FindSampler(_program, name, index);
            return _samplerStates.TryGetValue((name, index), out var value) ? value.CreateState() : null;
        }
    }

    private static int FindSampler(ShaderProgram program, string name, int index)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        for (var i = 0; i < program.Textures.Length; i++)
            if (program.Textures[i].Name == name && program.Textures[i].Element == 0)
            {
                if ((uint)index >= (uint)program.Textures[i].Count) throw new ArgumentOutOfRangeException(nameof(index));
                return i + index;
            }
        throw new ArgumentException($"Shader has no sampled binding named '{name}'.", nameof(name));
    }
    private static void ValidateSampler(ShaderTexture descriptor, SamplerConfiguration value)
    {
        value.Validate(); value.ValidateImage(descriptor.PixelType);
        if (value.UnnormalizedUVW && descriptor.UsesSampler && (descriptor.IsArray || descriptor.UnnormalizedUnsafe)) throw new ArgumentException("This shader binding cannot use unnormalized sampling operations or array images.");
        if (value.EnableCompare) throw new NotSupportedException("High-level shader images do not expose comparison sampling.");
        if (descriptor.PixelType != 0 && value.Linear) throw new ArgumentException("Integer shader images require nearest filtering without anisotropy.");
    }
    internal void CopySamplerStates(ShaderProgram expected, Span<SamplerConfiguration?> target)
    {
        lock (_codeGate)
        {
            ThrowIfDisposed();
            for (var i = 0; i < expected.Textures.Length; i++)
            {
                var descriptor = expected.Textures[i];
                var compatible = ReferenceEquals(expected, _program);
                if (!compatible)
                    foreach (var current in _program.Textures)
                        if (current.Name == descriptor.Name && current.Element == descriptor.Element && current.SameType(descriptor)) { compatible = true; break; }
                target[i] = compatible && _samplerStates.TryGetValue((descriptor.Name, descriptor.Element), out var value) && (!value.UnnormalizedUVW || !descriptor.UsesSampler || !descriptor.UnnormalizedUnsafe && !descriptor.IsArray) ? value : null;
            }
        }
    }
    private void PruneSamplerStates(ShaderProgram previous, ShaderProgram next)
    {
        foreach (var entry in _samplerStates)
        {
            var old = previous.Textures.FirstOrDefault(t => t.Name == entry.Key.Name && t.Element == entry.Key.Index);
            if (old is null || !next.Textures.Any(t => t.Name == entry.Key.Name && t.Element == entry.Key.Index && t.SameType(old) && (!entry.Value.UnnormalizedUVW || !t.UsesSampler || !t.UnnormalizedUnsafe && !t.IsArray)))
                _samplerStates.Remove(entry.Key);
        }
    }
    private byte[] GetStoredSamplerStates()
    {
        lock (_codeGate)
        {
            ThrowIfDisposed(); if (_samplerStates.Count == 0) return [];
            long length = 8;
            foreach (var entry in _samplerStates) length += 56L + Encoding.UTF8.GetByteCount(entry.Key.Name);
            if (length > 16 * 1024 * 1024) throw new InvalidDataException("Stored shader sampler metadata exceeds 16 MiB.");
            using var stream = new MemoryStream((int)length); using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
            writer.Write(3); writer.Write(_samplerStates.Count);
            foreach (var entry in _samplerStates.OrderBy(e => e.Key.Name, StringComparer.Ordinal).ThenBy(e => e.Key.Index))
            {
                var name = Encoding.UTF8.GetBytes(entry.Key.Name); writer.Write(name.Length); writer.Write(name); writer.Write(entry.Key.Index);
                var value = entry.Value;
                writer.Write((int)value.MinFilter); writer.Write((int)value.MagFilter); writer.Write((int)value.MIPFilter);
                writer.Write((int)value.RepeatU); writer.Write((int)value.RepeatV); writer.Write(value.LODBias);
                writer.Write(value.MinLOD); writer.Write(value.MaxLOD); writer.Write(value.AnisotropyMax);
                writer.Write((value.UseAnisotropy ? 1 : 0) | (value.EnableCompare ? 2 : 0) | (value.UnnormalizedUVW ? 4 : 0)); writer.Write((int)value.CompareOp); writer.Write((int)value.BorderColor);
            }
            return stream.ToArray();
        }
    }
    private void SetStoredSamplerStates(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length > 16 * 1024 * 1024) throw new InvalidDataException("Stored shader sampler metadata exceeds 16 MiB.");
        var values = new Dictionary<(string Name, int Index), SamplerConfiguration>();
        if (data.Length != 0)
        {
            var offset = 0;
            int Read()
            {
                if (data.Length - offset < 4) throw new InvalidDataException("Truncated shader sampler metadata.");
                var result = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(offset)); offset += 4; return result;
            }
            var version = Read();
            if (version is not (1 or 2 or 3)) throw new InvalidDataException("Unsupported shader sampler metadata version.");
            var count = Read(); if ((uint)count > 32) throw new InvalidDataException("Too many shader sampler entries.");
            for (var i = 0; i < count; i++)
            {
                var length = Read();
                if (length < 1 || length > data.Length - offset) throw new InvalidDataException("Invalid sampled binding name length.");
                string name;
                try { name = new UTF8Encoding(false, true).GetString(data.AsSpan(offset, length)); }
                catch (DecoderFallbackException error) { throw new InvalidDataException("Invalid sampled binding name encoding.", error); }
                offset += length; var index = Read();
                var min = (RenderingDevice.SamplerFilter)Read(); var mag = (RenderingDevice.SamplerFilter)Read(); var mip = (RenderingDevice.SamplerFilter)Read();
                var u = (RenderingDevice.SamplerRepeatMode)Read(); var v = (RenderingDevice.SamplerRepeatMode)Read();
                var bias = BitConverter.Int32BitsToSingle(Read()); var low = BitConverter.Int32BitsToSingle(Read());
                var high = BitConverter.Int32BitsToSingle(Read()); var anisotropy = BitConverter.Int32BitsToSingle(Read());
                var flags = Read(); if ((uint)flags > (version >= 3 ? 7u : 3u)) throw new InvalidDataException("Invalid sampler flags.");
                var compare = (RenderingDevice.CompareOperator)Read();
                var border = version >= 2 ? (RenderingDevice.SamplerBorderColor)Read() : RenderingDevice.SamplerBorderColor.FloatOpaqueBlack;
                var value = new SamplerConfiguration(min, mag, mip, u, v, bias, low, high, anisotropy, (flags & 1) != 0, (flags & 2) != 0, compare, border, (flags & 4) != 0);
                value.Validate(); if (!values.TryAdd((name, index), value)) throw new InvalidDataException("Duplicate sampled binding metadata.");
            }
            if (offset != data.Length) throw new InvalidDataException("Trailing shader sampler metadata.");
        }
        lock (_codeGate)
        {
            ThrowIfDisposed();
            foreach (var entry in values) ValidateSampler(_program.Textures[FindSampler(_program, entry.Key.Name, entry.Key.Index)], entry.Value);
            _samplerStates.Clear(); foreach (var entry in values) _samplerStates.Add(entry.Key, entry.Value);
        }
        EmitChanged();
    }
}
