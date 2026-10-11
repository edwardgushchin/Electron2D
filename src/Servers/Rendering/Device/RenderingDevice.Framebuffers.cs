using SDL3;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private sealed record FramebufferLayout(DataFormat[] Formats, TextureUsageBits[] Usage);
    private sealed record Framebuffer(RID[] Textures, int Format, Vector2i Size);
    private readonly Dictionary<int, FramebufferLayout> _framebufferFormats = [];
    /// <summary>Creates a copied single-pass color attachment layout.</summary>
    /// <param name="attachments">One to eight single-sample color target formats.</param>
    /// <returns>A device-local framebuffer format identifier.</returns>
    /// <exception cref="NotSupportedException">An attachment role or sample count is unavailable.</exception>
    public int FramebufferFormatCreate(ReadOnlySpan<RDAttachmentFormat> attachments)
    {
        IdleList(); if (attachments.Length is < 1 or > 8) throw new ArgumentOutOfRangeException(nameof(attachments));
        var formats = new DataFormat[attachments.Length]; var usage = new TextureUsageBits[attachments.Length];
        for (var i = 0; i < attachments.Length; i++)
        {
            var source = attachments[i]; ArgumentNullException.ThrowIfNull(source); formats[i] = source.Format; usage[i] = source.UsageFlags;
            if (source.Samples != TextureSamples.Samples1 || (usage[i] & TextureUsageBits.ColorAttachment) == 0 || !TextureIsFormatSupportedForUsage(formats[i], usage[i])) throw new NotSupportedException("Attachments require supported single-sample color target usage.");
        }
        var id = NextFormatID(ref _nextGraphicsFormat); _framebufferFormats.Add(id, new(formats, usage)); return id;
    }
    private FramebufferLayout FramebufferFormat(int format) => _framebufferFormats.TryGetValue(format, out var layout) ? layout : throw new ArgumentException("Unknown framebuffer format.", nameof(format));
    /// <summary>Reports the sample count of a framebuffer pass.</summary>
    /// <param name="framebufferFormat">A live single-pass format identifier.</param>
    /// <param name="renderPass">Zero for the supported single pass.</param>
    /// <returns>The format sample count.</returns>
    public TextureSamples FramebufferFormatGetTextureSamples(int framebufferFormat, uint renderPass = 0)
    { Access(); FramebufferFormat(framebufferFormat); if (renderPass != 0) throw new ArgumentOutOfRangeException(nameof(renderPass)); return TextureSamples.Samples1; }
    /// <summary>Creates a framebuffer borrowing device-owned color images.</summary>
    /// <param name="textures">One to eight ordinary single-layer, single-mip color images.</param>
    /// <param name="validateWithFormat">An existing matching format, or -1 to create its layout.</param>
    /// <returns>A framebuffer identity invalidated when an attachment is freed.</returns>
    public RID FramebufferCreate(ReadOnlySpan<RID> textures, int validateWithFormat = -1)
    {
        IdleList(); if (textures.Length is < 1 or > 8) throw new ArgumentOutOfRangeException(nameof(textures));
        var formats = new DataFormat[textures.Length]; var usage = new TextureUsageBits[textures.Length]; var unique = new HashSet<RID>(); Vector2i size = default;
        for (var i = 0; i < textures.Length; i++)
        {
            var info = Get(textures[i], Kind.Texture).Texture!;
            if (!unique.Add(textures[i]) || info.Type != TextureType.Texture || info.Layers != 1 || info.Mips != 1 || (info.Usage & TextureUsageBits.ColorAttachment) == 0) throw new ArgumentException("Framebuffer attachments require distinct ordinary color target images.", nameof(textures));
            var current = new Vector2i(checked((int)info.Width), checked((int)info.Height)); if (i == 0) size = current; else if (size != current) throw new ArgumentException("Attachment dimensions must match.", nameof(textures));
            formats[i] = info.Format; usage[i] = info.Usage;
        }
        var format = validateWithFormat;
        if (format == -1) { format = NextFormatID(ref _nextGraphicsFormat); _framebufferFormats.Add(format, new(formats, usage)); }
        else
        {
            var expected = FramebufferFormat(format);
            if (!expected.Formats.AsSpan().SequenceEqual(formats) || expected.Usage.Where((u, i) => (usage[i] & u) != u).Any()) throw new ArgumentException("Framebuffer attachments do not match the requested layout.", nameof(validateWithFormat));
        }
        return Add(new Entry { Kind = Kind.Framebuffer, Target = new(textures.ToArray(), format, size) });
    }
    /// <summary>Reads the framebuffer's matching layout identity.</summary>
    /// <param name="framebuffer">A live framebuffer with live borrowed attachments.</param>
    /// <returns>A device-local format identifier.</returns>
    public int FramebufferGetFormat(RID framebuffer) => GetFramebuffer(framebuffer).Format;
    /// <summary>Reports whether a framebuffer and all its borrowed attachments remain alive.</summary>
    /// <param name="framebuffer">The identity to inspect.</param>
    /// <returns>True for a live framebuffer with intact dependencies on this device.</returns>
    public bool FramebufferIsValid(RID framebuffer)
    {
        Access(); if (!_entries.TryGetValue(framebuffer, out var entry) || entry.Kind != Kind.Framebuffer) return false;
        foreach (var image in entry.Target!.Textures) if (!_entries.ContainsKey(image)) return false;
        return true;
    }
    private Framebuffer GetFramebuffer(RID rid)
    {
        var target = Get(rid, Kind.Framebuffer).Target!;
        foreach (var image in target.Textures) Get(image, Kind.Texture);
        return target;
    }
}
