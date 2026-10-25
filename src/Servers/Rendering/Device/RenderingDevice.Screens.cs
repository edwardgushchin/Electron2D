namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    /// <summary>The unavailable screen or command-list identity.</summary>
    public const int InvalidID = -1;
    private sealed class ScreenTarget(RID texture, RID framebuffer, Vector2i size, int format)
    {
        internal readonly RID Texture = texture, Framebuffer = framebuffer;
        internal readonly Vector2i Size = size;
        internal readonly int Format = format;
        internal bool Pending;
    }
    private readonly Dictionary<int, ScreenTarget> _screens = [];
    private readonly Dictionary<DataFormat, int> _screenFormats = [];
    private ScreenTarget? _screenDraw;
    /// <summary>Gets a native window's graphics client width in pixels.</summary>
    /// <param name="screen">Zero for the main window or a live native child window identity.</param>
    /// <returns>The current pixel width, or InvalidID on a local device.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The window is absent or retired.</exception>
    public int ScreenGetWidth(int screen = 0) { Access(); return _serverOwner?.DeviceScreen(screen).Size.X ?? InvalidID; }
    /// <summary>Gets a native window's graphics client height in pixels.</summary>
    /// <param name="screen">Zero for the main window or a live native child window identity.</param>
    /// <returns>The current pixel height, or InvalidID on a local device.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The window is absent or retired.</exception>
    public int ScreenGetHeight(int screen = 0) { Access(); return _serverOwner?.DeviceScreen(screen).Size.Y ?? InvalidID; }
    /// <summary>Gets the native window's single-color, single-sample framebuffer format.</summary>
    /// <param name="screen">Zero for main or a live native child window identity.</param>
    /// <returns>A reusable device-local format, or InvalidID on a local device.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The window is absent or retired.</exception>
    public int ScreenGetFramebufferFormat(int screen = 0)
    {
        Access(); if (_serverOwner is null) return InvalidID;
        return ScreenFormat(_serverOwner.DeviceScreen(screen).Format);
    }
    private int ScreenFormat(DataFormat format)
    {
        if (_screenFormats.TryGetValue(format, out var id)) return id;
        id = NextFormatID(ref _nextGraphicsFormat);
        _framebufferFormats.Add(id, AttachmentLayouts([format], [TextureUsageBits.ColorAttachment], [TextureSamples.Samples1], default, 1));
        _screenFormats.Add(format, id); return id;
    }
    /// <summary>Begins a cleared graphics list for the next presentation of a native window.</summary>
    /// <param name="screen">Zero for main or a live native child window identity.</param>
    /// <param name="clearColor">Finite attachment clear; null selects opaque black.</param>
    /// <returns>The owner-thread draw list, or InvalidID on a local device.</returns>
    /// <remarks>Use ScreenGetFramebufferFormat to create matching graphics pipelines and end the list before rendering.
    /// Completed screen drawing replaces that window's canvas presentation once; viewport textures retain their canvas images.
    /// The renderer submits before canvas drawing and presents through the same GPU queue without a CPU image copy.
    /// ForceDraw(false) and unavailable swapchain images retain pending output until native presentation; retired windows discard it.
    /// Hidden main-window output remains pending under the backend visibility policy. Native pixel size is sampled when the list begins.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The window identity is absent or retired.</exception>
    /// <exception cref="ArgumentException">The clear color is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Owner, active-list or native-submission rules reject recording.</exception>
    public long DrawListBeginForScreen(int screen = 0, Color? clearColor = null)
    {
        IdleList(); if (_serverOwner is null) return InvalidID;
        var color = clearColor ?? Colors.Black; FiniteColor(color);
        var info = _serverOwner.DeviceScreen(screen); var format = ScreenFormat(info.Format);
        if (!_screens.TryGetValue(screen, out var target) || target.Size != info.Size || target.Format != format)
        {
            using var description = new RDTextureFormat
            {
                Width = (uint)info.Size.X,
                Height = (uint)info.Size.Y,
                Format = info.Format,
                UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.Sampling | TextureUsageBits.CanCopyFrom
            };
            using var view = new RDTextureView(); var texture = TextureCreate(description, view); RID framebuffer;
            try { framebuffer = FramebufferCreate([texture], format); } catch { FreeRID(texture); throw; }
            var replacement = new ScreenTarget(texture, framebuffer, info.Size, format);
            try { ReleaseScreen(screen); } catch { try { FreeRID(framebuffer); } finally { FreeRID(texture); } throw; }
            _screens[screen] = target = replacement;
        }
        var list = DrawListBegin(target.Framebuffer, DrawFlags.ClearColor0, [color]); _screenDraw = target; return list;
    }
    internal (nint Texture, Vector2i Size) ScreenPresentation(int screen)
        => _screens.TryGetValue(screen, out var target) && target.Pending ? (_entries[target.Texture].NativeHandle, target.Size) : default;
    internal void ScreenPresented(int screen) { if (_screens.TryGetValue(screen, out var target)) target.Pending = false; }
    internal bool HasPendingScreens
    {
        get { foreach (var pair in _screens) if (pair.Value.Pending) return true; return false; }
    }
    internal bool ScreenMutationBlocked => _screens.Count != 0 && _activeList != 0;
    internal void ReleaseScreen(int screen)
    {
        if (!_screens.TryGetValue(screen, out var target)) return;
        IdleList(); _screens.Remove(screen);
        try { FreeRID(target.Framebuffer); } finally { FreeRID(target.Texture); }
    }
}
