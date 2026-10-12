using SDL3;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private sealed record FramebufferLayout(DataFormat[] Formats, TextureUsageBits[] Usage, TextureSamples[] AttachmentSamples, int[] Colors, int DepthIndex, int[] Resolves, TextureSamples Samples);
    private sealed record Framebuffer(RID[] Textures, int Format, Vector2i Size);
    private readonly Dictionary<int, FramebufferLayout> _framebufferFormats = [];
    /// <summary>Creates a copied single-pass color and depth/stencil attachment layout.</summary>
    /// <param name="attachments">Up to eight render color targets and one depth/stencil target with the same exact sample count.</param>
    /// <returns>A device-local framebuffer format identifier.</returns>
    /// <exception cref="NotSupportedException">A format or exact sample count is unavailable.</exception>
    public int FramebufferFormatCreate(ReadOnlySpan<RDAttachmentFormat> attachments) => CreateFramebufferFormat(attachments, null);
    /// <summary>Creates a copied explicit pass layout, including per-color resolve destinations.</summary>
    /// <param name="attachments">Render and resolve attachment formats in caller-defined order.</param>
    /// <param name="passes">Exactly one executable pass; additional subpasses require their native input/preservation adapters.</param>
    /// <returns>A device-local layout retaining copied indices and formats.</returns>
    /// <exception cref="ArgumentException">Indices, formats, roles or sample counts disagree.</exception>
    /// <exception cref="NotSupportedException">Multiple native subpasses or an exact sample count are unavailable.</exception>
    public int FramebufferFormatCreateMultipass(ReadOnlySpan<RDAttachmentFormat> attachments, ReadOnlySpan<RDFramebufferPass> passes)
    { if (passes.Length != 1) throw new NotSupportedException("Multiple subpasses require native input/preservation integration."); ArgumentNullException.ThrowIfNull(passes[0]); return CreateFramebufferFormat(attachments, passes[0]); }
    private int CreateFramebufferFormat(ReadOnlySpan<RDAttachmentFormat> attachments, RDFramebufferPass? pass)
    {
        IdleList(); if (attachments.Length is < 1 or > 17) throw new ArgumentOutOfRangeException(nameof(attachments));
        var formats = new DataFormat[attachments.Length]; var usage = new TextureUsageBits[attachments.Length]; var samples = new TextureSamples[attachments.Length];
        for (var i = 0; i < attachments.Length; i++)
        {
            var source = attachments[i]; ArgumentNullException.ThrowIfNull(source); formats[i] = source.Format; usage[i] = source.UsageFlags; samples[i] = source.Samples;
            if (!TextureIsFormatSupportedForUsage(formats[i], samples[i] != TextureSamples.Samples1 ? usage[i] & ~(TextureUsageBits.CanCopyFrom | TextureUsageBits.CanCopyTo) : usage[i])) throw new NotSupportedException("Unsupported attachment format/usage.");
            ExactSamples(formats[i], samples[i]);
        }
        var layout = AttachmentLayout(formats, usage, samples, pass); var id = NextFormatID(ref _nextGraphicsFormat); _framebufferFormats.Add(id, layout); return id;
    }
    private static FramebufferLayout AttachmentLayout(DataFormat[] formats, TextureUsageBits[] usage, TextureSamples[] samples, RDFramebufferPass? pass)
    {
        var depth = -1; int[] colors, resolves;
        if (pass is null)
        {
            var indices = new List<int>();
            for (var i = 0; i < formats.Length; i++)
                if (DepthFormat(formats[i])) { if (depth >= 0) throw new ArgumentException("Provide at most one depth/stencil target."); depth = i; } else indices.Add(i);
            colors = indices.ToArray(); resolves = new int[colors.Length]; Array.Fill(resolves, RDFramebufferPass.AttachmentUnused);
        }
        else
        {
            colors = pass.ColorAttachments; depth = pass.DepthAttachment; resolves = pass.ResolveAttachments;
            if (resolves.Length == 0) { resolves = new int[colors.Length]; Array.Fill(resolves, RDFramebufferPass.AttachmentUnused); }
        }
        if (colors.Length > 8 || resolves.Length != colors.Length || colors.Length == 0 && depth == -1) throw new ArgumentException("Provide one or more render targets and a resolve slot per color target.");
        var used = new HashSet<int>(); var rasterSamples = samples[colors.Length > 0 ? Index(colors[0], formats.Length) : Index(depth, formats.Length)];
        foreach (var color in colors)
        {
            Index(color, formats.Length);
            if (!used.Add(color) || DepthFormat(formats[color]) || (usage[color] & TextureUsageBits.ColorAttachment) == 0 || samples[color] != rasterSamples) throw new ArgumentException("Color targets require distinct matching raster sample counts and color usage.");
        }
        if (depth != -1)
        {
            Index(depth, formats.Length);
            if (!used.Add(depth) || !DepthFormat(formats[depth]) || (usage[depth] & TextureUsageBits.DepthStencilAttachment) == 0 || samples[depth] != rasterSamples) throw new ArgumentException("Depth target must match the raster sample count and depth role.");
        }
        for (var i = 0; i < resolves.Length; i++)
        {
            var resolve = resolves[i]; if (resolve == -1) continue; Index(resolve, formats.Length);
            if (!used.Add(resolve) || rasterSamples == TextureSamples.Samples1 || samples[resolve] != TextureSamples.Samples1 || formats[resolve] != formats[colors[i]] || (usage[resolve] & TextureUsageBits.ColorAttachment) == 0) throw new ArgumentException("Resolve slots require distinct matching single-sample color targets.");
        }
        if (used.Count != formats.Length) throw new ArgumentException("Every attachment must have an executing render or resolve role in this pass.");
        return new(formats, usage, samples, colors, depth, resolves, rasterSamples);
    }
    private static int Index(int index, int length) => index >= 0 && index < length ? index : throw new ArgumentOutOfRangeException(nameof(index));
    private FramebufferLayout FramebufferFormat(int format) => _framebufferFormats.TryGetValue(format, out var layout) ? layout : throw new ArgumentException("Unknown framebuffer format.", nameof(format));
    /// <summary>Reports the exact raster sample count of a framebuffer pass.</summary>
    /// <param name="framebufferFormat">A live single-pass format identifier.</param>
    /// <param name="renderPass">Zero for the supported single pass.</param>
    /// <returns>The copied raster sample count.</returns>
    public TextureSamples FramebufferFormatGetTextureSamples(int framebufferFormat, uint renderPass = 0)
    { Access(); var layout = FramebufferFormat(framebufferFormat); if (renderPass != 0) throw new ArgumentOutOfRangeException(nameof(renderPass)); return layout.Samples; }
    /// <summary>Creates a framebuffer borrowing device-owned color and depth/stencil images.</summary>
    /// <param name="textures">Distinct ordinary single-layer, single-mip images with matching dimensions.</param>
    /// <param name="validateWithFormat">An existing matching layout including resolve roles, or -1 for an implicit render-only pass.</param>
    /// <returns>A framebuffer invalidated when any borrowed image is freed.</returns>
    public RID FramebufferCreate(ReadOnlySpan<RID> textures, int validateWithFormat = -1) => CreateFramebuffer(textures, null, validateWithFormat);
    /// <summary>Creates a framebuffer borrowing the images selected by a copied explicit pass.</summary>
    /// <param name="textures">Ordinary render and marked resolve images, in the pass's index order.</param>
    /// <param name="passes">Exactly one executable color/depth/resolve pass.</param>
    /// <param name="validateWithFormat">An existing matching layout, or -1 to create its copied format.</param>
    /// <returns>A framebuffer borrowing all render and resolve images.</returns>
    /// <exception cref="ArgumentException">Images, sample counts, roles or requested layout disagree.</exception>
    /// <exception cref="NotSupportedException">Multiple subpasses require their native adapters.</exception>
    public RID FramebufferCreateMultipass(ReadOnlySpan<RID> textures, ReadOnlySpan<RDFramebufferPass> passes, int validateWithFormat = -1)
    { if (passes.Length != 1) throw new NotSupportedException("Multiple subpasses require native input/preservation integration."); ArgumentNullException.ThrowIfNull(passes[0]); return CreateFramebuffer(textures, passes[0], validateWithFormat); }
    private RID CreateFramebuffer(ReadOnlySpan<RID> textures, RDFramebufferPass? pass, int validateWithFormat)
    {
        IdleList(); if (textures.Length is < 1 or > 17) throw new ArgumentOutOfRangeException(nameof(textures));
        var formats = new DataFormat[textures.Length]; var usage = new TextureUsageBits[textures.Length]; var samples = new TextureSamples[textures.Length]; var unique = new HashSet<RID>(); Vector2i size = default;
        for (var i = 0; i < textures.Length; i++)
        {
            var info = Get(textures[i], Kind.Texture).Texture!;
            if (!unique.Add(textures[i]) || info.Type != TextureType.Texture || info.Layers != 1 || info.Mips != 1) throw new ArgumentException("Framebuffer images require distinct ordinary single-mip targets.", nameof(textures));
            var current = new Vector2i(checked((int)info.Width), checked((int)info.Height)); if (i == 0) size = current; else if (size != current) throw new ArgumentException("Attachment dimensions must match.", nameof(textures));
            formats[i] = info.Format; usage[i] = info.Usage; samples[i] = info.Samples;
        }
        var format = validateWithFormat; FramebufferLayout layout;
        if (format == -1) { layout = AttachmentLayout(formats, usage, samples, pass); format = NextFormatID(ref _nextGraphicsFormat); }
        else
        {
            layout = FramebufferFormat(format);
            if (!layout.Formats.AsSpan().SequenceEqual(formats) || !layout.AttachmentSamples.AsSpan().SequenceEqual(samples) || layout.Usage.Where((u, i) => (usage[i] & u) != u).Any()) throw new ArgumentException("Images do not match the requested layout.", nameof(validateWithFormat));
            if (pass is not null)
            {
                var actual = AttachmentLayout(formats, usage, samples, pass);
                if (actual.DepthIndex != layout.DepthIndex || !actual.Colors.AsSpan().SequenceEqual(layout.Colors) || !actual.Resolves.AsSpan().SequenceEqual(layout.Resolves)) throw new ArgumentException("Pass indices do not match the requested layout.", nameof(validateWithFormat));
            }
        }
        for (var i = 0; i < layout.Resolves.Length; i++)
            if (layout.Resolves[i] >= 0) ResolvePair(Get(textures[layout.Colors[i]], Kind.Texture).Texture!, Get(textures[layout.Resolves[i]], Kind.Texture).Texture!);
        foreach (var color in layout.Colors) if (Get(textures[color], Kind.Texture).Texture!.Resolve) throw new ArgumentException("Marked resolve images must occupy resolve slots, not render slots.", nameof(textures));
        if (validateWithFormat == -1) _framebufferFormats.Add(format, layout);
        return Add(new Entry { Kind = Kind.Framebuffer, Target = new(textures.ToArray(), format, size) });
    }
    private static bool SameRasterLayout(FramebufferLayout a, FramebufferLayout b)
    {
        if (a.Samples != b.Samples || a.Colors.Length != b.Colors.Length || (a.DepthIndex < 0) != (b.DepthIndex < 0)) return false;
        for (var i = 0; i < a.Colors.Length; i++) if (a.Formats[a.Colors[i]] != b.Formats[b.Colors[i]]) return false;
        return a.DepthIndex < 0 || a.Formats[a.DepthIndex] == b.Formats[b.DepthIndex];
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
