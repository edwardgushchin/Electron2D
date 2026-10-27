using SDL3;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private sealed record FramebufferPassLayout(DataFormat[] Formats, TextureUsageBits[] Usage, TextureSamples[] AttachmentSamples,
        int[] Colors, int[] ColorSlots, int DepthIndex, int[] Resolves, TextureSamples Samples, int[] Inputs, int[] Preserves);
    private sealed record FramebufferLayout(FramebufferPassLayout[] Passes, int ViewCount);
    private FramebufferLayout FramebufferLayouts(int format) => _framebufferFormats.TryGetValue(format, out var layout) ? layout : throw new ArgumentException("Unknown framebuffer format.", nameof(format));
    private sealed record Framebuffer(RID[] Textures, int Format, Vector2i Size)
    { internal bool[] Touched = []; internal bool[] Visited = []; }
    private readonly Dictionary<int, FramebufferLayout> _framebufferFormats = [];
    /// <summary>Creates a copied single-pass color and depth/stencil attachment layout.</summary>
    /// <param name="attachments">Up to eight render color targets and one depth/stencil target with the same exact sample count.</param>
    /// <param name="viewCount">One through 256 ordered views, each targeting the corresponding image layer.</param>
    /// <returns>A device-local framebuffer format identifier.</returns>
    /// <exception cref="NotSupportedException">A format or exact sample count is unavailable.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The view count is outside one through 256.</exception>
    public int FramebufferFormatCreate(ReadOnlySpan<RDAttachmentFormat> attachments, int viewCount = 1) => CreateFramebufferFormat(attachments, default, viewCount);
    /// <summary>Creates a copied explicit pass layout, including per-color resolve destinations.</summary>
    /// <param name="attachments">Render and resolve attachment formats in caller-defined order.</param>
    /// <param name="passes">Ordered copied color/depth/resolve passes and their input/preservation roles.</param>
    /// <param name="viewCount">One through 256 ordered views.</param>
    /// <returns>A device-local layout retaining copied indices and formats.</returns>
    /// <exception cref="ArgumentException">Indices, formats, roles or sample counts disagree.</exception>
    /// <exception cref="NotSupportedException">An exact sample count or attachment role is unavailable.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The view count is outside one through 256.</exception>
    public int FramebufferFormatCreateMultipass(ReadOnlySpan<RDAttachmentFormat> attachments, ReadOnlySpan<RDFramebufferPass> passes, int viewCount = 1)
    { if (passes.IsEmpty) throw new ArgumentException("Provide one or more passes.", nameof(passes)); return CreateFramebufferFormat(attachments, passes, viewCount); }
    private int CreateFramebufferFormat(ReadOnlySpan<RDAttachmentFormat> attachments, ReadOnlySpan<RDFramebufferPass> passes, int viewCount)
    {
        IdleList(); ValidateViewCount(viewCount); if (attachments.IsEmpty) throw new ArgumentOutOfRangeException(nameof(attachments));
        var formats = new DataFormat[attachments.Length]; var usage = new TextureUsageBits[attachments.Length]; var samples = new TextureSamples[attachments.Length];
        for (var i = 0; i < attachments.Length; i++)
        {
            var source = attachments[i]; ArgumentNullException.ThrowIfNull(source); formats[i] = source.Format; usage[i] = source.UsageFlags; samples[i] = source.Samples;
            if (!TextureIsFormatSupportedForUsage(formats[i], samples[i] != TextureSamples.Samples1 ? usage[i] & ~(TextureUsageBits.CanCopyFrom | TextureUsageBits.CanCopyTo) : usage[i])) throw new NotSupportedException("Unsupported attachment format/usage.");
            ExactSamples(formats[i], samples[i]);
        }
        return RetainFramebufferFormat(AttachmentLayouts(formats, usage, samples, passes, viewCount));
    }
    private int RetainFramebufferFormat(FramebufferLayout layout)
    {
        foreach (var pair in _framebufferFormats)
        {
            var prior = pair.Value;
            if (prior.ViewCount != layout.ViewCount || prior.Passes.Length != layout.Passes.Length) continue;
            var a = prior.Passes[0]; var b = layout.Passes[0];
            if (!a.Formats.AsSpan().SequenceEqual(b.Formats) || !a.Usage.AsSpan().SequenceEqual(b.Usage) || !a.AttachmentSamples.AsSpan().SequenceEqual(b.AttachmentSamples)) continue;
            var same = true;
            for (var i = 0; i < prior.Passes.Length; i++) if (!SamePass(prior.Passes[i], layout.Passes[i])) { same = false; break; }
            if (same) return pair.Key;
        }
        var id = NextFormatID(ref _nextGraphicsFormat); _framebufferFormats.Add(id, layout); return id;
    }
    private static FramebufferPassLayout AttachmentLayout(DataFormat[] formats, TextureUsageBits[] usage, TextureSamples[] samples, RDFramebufferPass? pass)
    {
        var depth = -1; int[] colors, resolves;
        if (pass is null)
        {
            var indices = new List<int>();
            for (var i = 0; i < formats.Length; i++)
                if (DepthFormat(formats[i])) { if (depth >= 0) throw new ArgumentException("Provide at most one depth/stencil target."); depth = i; } else if ((usage[i] & TextureUsageBits.ColorAttachment) != 0) indices.Add(i);
            colors = indices.ToArray(); resolves = new int[colors.Length]; Array.Fill(resolves, RDFramebufferPass.AttachmentUnused);
        }
        else
        {
            colors = pass.ColorAttachments; depth = pass.DepthAttachment; resolves = pass.ResolveAttachments;
            if (resolves.Length == 0) { resolves = new int[colors.Length]; Array.Fill(resolves, RDFramebufferPass.AttachmentUnused); }
        }
        if (colors.Length > 8 || resolves.Length != colors.Length || colors.Length == 0 && depth == -1) throw new ArgumentException("Provide one or more render targets and a resolve slot per color target.");
        var slots = Enumerable.Range(0, colors.Length).Where(i => colors[i] != RDFramebufferPass.AttachmentUnused).ToArray();
        if (slots.Length == 0 && depth == -1) throw new ArgumentException("Provide an active color or depth/stencil target.");
        var used = new HashSet<int>(); var rasterSamples = samples[slots.Length > 0 ? Index(colors[slots[0]], formats.Length) : Index(depth, formats.Length)];
        foreach (var color in colors)
        {
            if (color == RDFramebufferPass.AttachmentUnused) continue;
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
            if (colors[i] == RDFramebufferPass.AttachmentUnused) throw new ArgumentException("An unused color slot cannot resolve.");
            if (!used.Add(resolve) || rasterSamples == TextureSamples.Samples1 || samples[resolve] != TextureSamples.Samples1 || formats[resolve] != formats[colors[i]] || (usage[resolve] & TextureUsageBits.ColorAttachment) == 0) throw new ArgumentException("Resolve slots require distinct matching single-sample color targets.");
        }
        var inputs = pass?.InputAttachments ?? []; var preserves = pass?.PreserveAttachments ?? [];
        foreach (var input in inputs)
        {
            if (input == RDFramebufferPass.AttachmentUnused) continue;
            Index(input, formats.Length);
            if (!used.Add(input) || DepthFormat(formats[input]) || (usage[input] & TextureUsageBits.InputAttachment) == 0)
                throw new ArgumentException("Inputs require distinct color input attachments, separate from the pass outputs.");
        }
        foreach (var preserve in preserves) Index(preserve, formats.Length);
        return new(formats, usage, samples, colors, slots, depth, resolves, rasterSamples, inputs, preserves);
    }
    private static void ValidateViewCount(int viewCount) { if (viewCount is < 1 or > 256) throw new ArgumentOutOfRangeException(nameof(viewCount), "A framebuffer supports one through 256 views."); }
    private static int Index(int index, int length) => index >= 0 && index < length ? index : throw new ArgumentOutOfRangeException(nameof(index));
    private static FramebufferLayout AttachmentLayouts(DataFormat[] formats, TextureUsageBits[] usage, TextureSamples[] samples, ReadOnlySpan<RDFramebufferPass> descriptions, int viewCount)
    {
        var passes = new FramebufferPassLayout[Math.Max(1, descriptions.Length)]; var used = new HashSet<int>();
        for (var i = 0; i < passes.Length; i++)
        {
            RDFramebufferPass? description = null;
            if (!descriptions.IsEmpty) { description = descriptions[i]; ArgumentNullException.ThrowIfNull(description); }
            var pass = passes[i] = AttachmentLayout(formats, usage, samples, description);
            foreach (var color in pass.Colors) if (color >= 0) used.Add(color);
            if (pass.DepthIndex >= 0) used.Add(pass.DepthIndex);
            foreach (var resolve in pass.Resolves) if (resolve >= 0) used.Add(resolve);
            foreach (var input in pass.Inputs) if (input >= 0) used.Add(input);
        }
        for (var i = 0; i < formats.Length; i++)
            if (formats[i] != DataFormat.Max && !used.Contains(i)) throw new ArgumentException("Every live attachment must have an executing render, resolve or input role.");
        return new(passes, viewCount);
    }
    private FramebufferPassLayout FramebufferFormat(int format, uint renderPass = 0)
    { var passes = FramebufferLayouts(format).Passes; if (renderPass >= passes.Length) throw new ArgumentOutOfRangeException(nameof(renderPass)); return passes[renderPass]; }
    private static bool SamePass(FramebufferPassLayout a, FramebufferPassLayout b) => a.DepthIndex == b.DepthIndex && a.Colors.AsSpan().SequenceEqual(b.Colors) && a.Resolves.AsSpan().SequenceEqual(b.Resolves) && a.Inputs.AsSpan().SequenceEqual(b.Inputs) && a.Preserves.AsSpan().SequenceEqual(b.Preserves);

    /// <summary>Reports the exact raster sample count of a framebuffer pass.</summary>
    /// <param name="framebufferFormat">A live framebuffer format identifier.</param>
    /// <param name="renderPass">The zero-based pass index.</param>
    /// <returns>The copied raster sample count.</returns>
    public TextureSamples FramebufferFormatGetTextureSamples(int framebufferFormat, uint renderPass = 0)
    { Access(); return FramebufferFormat(framebufferFormat, renderPass).Samples; }
    /// <summary>Creates a framebuffer borrowing device-owned color and depth/stencil images.</summary>
    /// <param name="textures">Distinct single-mip images with enough layers for all views with matching dimensions; empty RID slots must remain unused by every pass.</param>
    /// <param name="validateWithFormat">An existing matching layout including resolve roles, or -1 for an implicit render-only pass.</param>
    /// <param name="viewCount">One through 256 views; attachments must supply at least that many layers.</param>
    /// <returns>A framebuffer invalidated when any borrowed image is freed.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The view count is outside one through 256.</exception>
    public RID FramebufferCreate(ReadOnlySpan<RID> textures, int validateWithFormat = -1, int viewCount = 1) => CreateFramebuffer(textures, default, validateWithFormat, viewCount);
    /// <summary>Creates a framebuffer borrowing the images selected by ordered copied passes.</summary>
    /// <param name="textures">Render, marked resolve and input images in caller-defined attachment order; empty RID slots must remain unused by every pass.</param>
    /// <param name="passes">One or more ordered executable passes.</param>
    /// <param name="validateWithFormat">An existing matching layout, or -1 to create its copied format.</param>
    /// <param name="viewCount">One through 256 views, matching any explicitly selected format.</param>
    /// <returns>A framebuffer borrowing all images used by its ordered passes.</returns>
    /// <exception cref="ArgumentException">Images, sample counts, roles or requested layout disagree.</exception>
    /// <exception cref="NotSupportedException">An attachment role requires its native adapter.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The view count is outside one through 256.</exception>
    public RID FramebufferCreateMultipass(ReadOnlySpan<RID> textures, ReadOnlySpan<RDFramebufferPass> passes, int validateWithFormat = -1, int viewCount = 1)
    { if (passes.IsEmpty) throw new ArgumentException("Provide one or more passes.", nameof(passes)); return CreateFramebuffer(textures, passes, validateWithFormat, viewCount); }
    private RID CreateFramebuffer(ReadOnlySpan<RID> textures, ReadOnlySpan<RDFramebufferPass> passes, int validateWithFormat, int viewCount)
    {
        IdleList(); ValidateViewCount(viewCount); if (textures.IsEmpty) throw new ArgumentOutOfRangeException(nameof(textures));
        var formats = new DataFormat[textures.Length]; var usage = new TextureUsageBits[textures.Length]; var samples = new TextureSamples[textures.Length]; var unique = new HashSet<nint>(); Vector2i size = default;
        for (var i = 0; i < textures.Length; i++)
        {
            if (!textures[i].IsValid()) { formats[i] = DataFormat.Max; continue; }
            var image = Get(textures[i], Kind.Texture); var info = image.Texture!;
            if (info.Swizzle != 0) throw new ArgumentException("Framebuffer attachments require identity component mappings.", nameof(textures));
            if (!unique.Add(image.NativeHandle) || info.Layers < viewCount || info.Mips != 1) throw new ArgumentException("Framebuffer images require distinct single-mip targets with enough layers for every view.", nameof(textures));
            var current = new Vector2i(checked((int)info.Width), checked((int)info.Height)); if (size == default) size = current; else if (size != current) throw new ArgumentException("Attachment dimensions must match.", nameof(textures));
            formats[i] = info.Format; usage[i] = info.Usage; samples[i] = info.Samples;
        }
        if (size == default) throw new ArgumentException("Provide at least one live attachment.", nameof(textures));
        var format = validateWithFormat; FramebufferLayout layout;
        if (format == -1) layout = AttachmentLayouts(formats, usage, samples, passes, viewCount);
        else
        {
            layout = FramebufferLayouts(format); if (layout.ViewCount != viewCount) throw new ArgumentException("Framebuffer view counts must match.", nameof(viewCount)); var first = layout.Passes[0];
            if (!first.Formats.AsSpan().SequenceEqual(formats) || !first.AttachmentSamples.AsSpan().SequenceEqual(samples) || first.Usage.Where((u, i) => (usage[i] & u) != u).Any()) throw new ArgumentException("Images do not match the requested layout.", nameof(validateWithFormat));
            if (!passes.IsEmpty)
            {
                var actual = AttachmentLayouts(formats, usage, samples, passes, viewCount);
                if (actual.Passes.Length != layout.Passes.Length || actual.Passes.Where((pass, i) => !SamePass(pass, layout.Passes[i])).Any()) throw new ArgumentException("Pass indices do not match the requested layout.", nameof(validateWithFormat));
            }
        }
        foreach (var pass in layout.Passes)
        {
            foreach (var preserved in pass.Preserves) Get(textures[preserved], Kind.Texture);
            for (var i = 0; i < pass.Resolves.Length; i++)
                if (pass.Resolves[i] >= 0) ResolvePair(Get(textures[pass.Colors[i]], Kind.Texture).Texture!, Get(textures[pass.Resolves[i]], Kind.Texture).Texture!);
            foreach (var color in pass.Colors) if (color >= 0 && Get(textures[color], Kind.Texture).Texture!.Resolve) throw new ArgumentException("Marked resolve images must occupy resolve slots, not render slots.", nameof(textures));
        }
        if (validateWithFormat == -1) format = RetainFramebufferFormat(layout);
        return Add(new Entry { Kind = Kind.Framebuffer, Target = new(textures.ToArray(), format, size) { Touched = new bool[checked(textures.Length * viewCount)], Visited = new bool[viewCount] } });
    }
    private static bool SameRasterLayout(FramebufferPassLayout a, FramebufferPassLayout b)
    {
        if (a.Samples != b.Samples || a.Colors.Length != b.Colors.Length || (a.DepthIndex < 0) != (b.DepthIndex < 0)) return false;
        for (var i = 0; i < a.Colors.Length; i++)
            if (a.Colors[i] < 0 ? b.Colors[i] >= 0 : b.Colors[i] < 0 || a.Formats[a.Colors[i]] != b.Formats[b.Colors[i]]) return false;
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
        Access(); return CachedFramebufferIsValid(framebuffer);
    }
    internal bool CachedFramebufferIsValid(RID framebuffer)
    {
        if (!_entries.TryGetValue(framebuffer, out var entry) || entry.Kind != Kind.Framebuffer) return false;
        foreach (var image in entry.Target!.Textures) if (image.IsValid() && (!_entries.TryGetValue(image, out var texture) || !texture.Alive)) return false;
        return true;
    }
    private Framebuffer GetFramebuffer(RID rid)
    {
        var target = Get(rid, Kind.Framebuffer).Target!;
        foreach (var image in target.Textures) if (image.IsValid()) Get(image, Kind.Texture);
        return target;
    }
}
