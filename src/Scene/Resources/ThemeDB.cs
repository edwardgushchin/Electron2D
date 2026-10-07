using System.Collections.Concurrent;

namespace Electron2D;

/// <summary>Owns the built-in theme for implemented controls and the universal typed fallback values.</summary>
/// <remarks>Public static operations delegate to the retained service object; state, identity and ownership remain object-scoped.
/// The singleton and its built-in resources are borrowed. Project theme-file loading
/// and skins for unimplemented controls remain separate integrations. Resource changes notify attached theme
/// owners through their scene queues. Universal fallback assignments are synchronous and suppress equal values.
/// Initial service construction decodes the built-in slider, button, text-field and scroll-hint icons through the SVG image codec at scale one.
/// Native type-name queries use compiled names and inheritance chains without initializing the native theme service.</remarks>
public sealed partial class ThemeDB : ElectronObject
{
    private static readonly ConcurrentDictionary<Type, string[]> TypeChains = new();
    private static readonly ConcurrentDictionary<string, string[]> NameChains = new(StringComparer.Ordinal);
    private static class ServiceHolder
    {
        internal static readonly Lazy<ThemeDB> Singleton = new(() => new ThemeDB());
    }
    private readonly object _gate = new();
    private readonly Theme _defaultTheme;
    private readonly List<Resource> _owned = [];
    private float _baseScale = 1;
    private int _fontSize = 16;
    private Font? _font;
    private Texture? _icon;
    private StyleBox? _style;
    private bool _iconInitialized;
    private ThemeDB()
    {
        _defaultTheme = new Theme { DefaultBaseScale = 1, DefaultFontSize = 16 };
        foreach (var name in new[] { "Panel", "PanelContainer", "PopupPanel" })
        {
            var style = new StyleBoxFlat { BGColor = new(.1f, .1f, .1f, .6f), CornerDetail = 5 };
            style.SetContentMarginAll(0); style.SetCornerRadiusAll(3); _owned.Add(style);
            _defaultTheme.SetStyleBox("panel", name, style);
        }
        foreach (var name in new[] { "BoxContainer", "HBoxContainer", "VBoxContainer" }) _defaultTheme.SetConstant("separation", name, 4);
        foreach (var name in new[] { "GridContainer", "FlowContainer", "HFlowContainer", "VFlowContainer" })
        { _defaultTheme.SetConstant("h_separation", name, 4); _defaultTheme.SetConstant("v_separation", name, 4); }
        foreach (var side in new[] { "left", "top", "right", "bottom" }) _defaultTheme.SetConstant("margin_" + side, "MarginContainer", 0);
        var windowBorder = new StyleBoxFlat { BGColor = new(.13f, .13f, .13f) }; windowBorder.SetContentMarginAll(4); _owned.Add(windowBorder);
        _defaultTheme.SetStyleBox("embedded_border", "Window", windowBorder);
        _defaultTheme.SetConstant("title_height", "Window", 36); _defaultTheme.SetConstant("resize_margin", "Window", 4);
        _defaultTheme.SetColor("title_color", "Window", new(.875f, .875f, .875f)); _defaultTheme.SetFontSize("title_font_size", "Window", 16);
        var fallback = new StyleBoxFlat { BGColor = new(1, .365f, .365f), DrawCenter = false, CornerDetail = 1 };
        fallback.SetContentMarginAll(4); fallback.SetBorderWidthAll(2); _style = fallback; _owned.Add(fallback);
        try { AddSliderDefaults(); AddTextDefaults(); AddButtonDefaults(); AddScrollDefaults(); AddItemListDefaults(); AddSplitDefaults(); AddLineEditDefaults(); AddTextEditDefaults(); AddCodeEditDefaults(); AddTreeDefaults(); AddTabDefaults(); AddMenuDefaults(); AddDialogDefaults(); AddFileDialogDefaults(); AddSpinBoxDefaults(); AddColorPickerDefaults(); }
        catch
        {
            _defaultTheme.Dispose(); foreach (var owned in _owned) owned.Dispose(); _owned.Clear();
            throw;
        }
        _defaultTheme.Changed += DefaultChanged; _defaultTheme.Disposed += DefaultDisposed;
    }
    private void AddTextDefaults()
    {
        using var stream = typeof(ThemeDB).Assembly.GetManifestResourceStream("Electron2D.Fonts.OpenSans_SemiBold.woff2")
            ?? throw new InvalidOperationException("The built-in font is missing.");
        var bytes = new byte[checked((int)stream.Length)]; stream.ReadExactly(bytes);
        _font = new FontFile(bytes); _owned.Add(_font); _defaultTheme.DefaultFont = _font;
        var normal = new StyleBoxEmpty(); _owned.Add(normal);
        var focus = new StyleBoxFlat { BGColor = new(1, 1, 1, .75f), DrawCenter = false, CornerDetail = 5 };
        focus.SetContentMarginAll(4); focus.SetCornerRadiusAll(3); focus.SetBorderWidthAll(2); focus.SetExpandMarginAll(2); _owned.Add(focus);
        _defaultTheme.SetStyleBox("normal", "Label", normal); _defaultTheme.SetStyleBox("focus", "Label", focus);
        _defaultTheme.SetFont("font", "Label", null); _defaultTheme.SetFontSize("font_size", "Label", -1);
        _defaultTheme.SetColor("font_color", "Label", Colors.White);
        _defaultTheme.SetColor("font_shadow_color", "Label", new(0, 0, 0, 0));
        _defaultTheme.SetColor("font_outline_color", "Label", Colors.Black);
        _defaultTheme.SetConstant("shadow_offset_x", "Label", 1); _defaultTheme.SetConstant("shadow_offset_y", "Label", 1);
        _defaultTheme.SetConstant("outline_size", "Label", 0); _defaultTheme.SetConstant("shadow_outline_size", "Label", 1);
        _defaultTheme.SetConstant("line_spacing", "Label", 3);
    }
    private void AddSliderDefaults()
    {
        var track = CreateSliderStyle(new(.1f, .1f, .1f, .6f));
        var fill = CreateSliderStyle(new(1, 1, 1, .4f));
        var activeFill = CreateSliderStyle(new(1, 1, 1, .75f));
        var grabber = CreateIcon("""<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16"><circle cx="8" cy="8" r="7" fill="#fefefe" fill-opacity=".75"/></svg>"""u8);
        var highlight = CreateIcon("""<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16"><circle cx="8" cy="8" r="7" fill="#fefefe"/></svg>"""u8);
        var disabled = CreateIcon("""<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16"><circle cx="8" cy="8" r="7" fill="#fefefe" fill-opacity=".37"/></svg>"""u8);
        var horizontalTick = CreateIcon("""<svg xmlns="http://www.w3.org/2000/svg" width="4" height="8"><path fill="#fff" fill-opacity=".25" d="M1 0h2v16H1z"/></svg>"""u8);
        var verticalTick = CreateIcon("""<svg xmlns="http://www.w3.org/2000/svg" width="8" height="4"><path fill="#fff" fill-opacity=".25" d="M0 3V1h16v2z"/></svg>"""u8);
        foreach (var name in new[] { "HSlider", "VSlider" })
        {
            _defaultTheme.SetStyleBox("slider", name, track);
            _defaultTheme.SetStyleBox("grabber_area", name, fill);
            _defaultTheme.SetStyleBox("grabber_area_highlight", name, activeFill);
            _defaultTheme.SetIcon("grabber", name, grabber);
            _defaultTheme.SetIcon("grabber_highlight", name, highlight);
            _defaultTheme.SetIcon("grabber_disabled", name, disabled);
            _defaultTheme.SetIcon("tick", name, name == "HSlider" ? horizontalTick : verticalTick);
            _defaultTheme.SetConstant("center_grabber", name, 0);
            _defaultTheme.SetConstant("grabber_offset", name, 0);
            _defaultTheme.SetConstant("tick_offset", name, 0);
        }
    }
    private StyleBoxFlat CreateSliderStyle(Color color)
    {
        var style = new StyleBoxFlat { BGColor = color, CornerDetail = 6 };
        style.SetContentMarginAll(4); style.SetCornerRadiusAll(4); _owned.Add(style);
        return style;
    }
    private ImageTexture CreateIcon(ReadOnlySpan<byte> svg)
    {
        using var image = new Image(); image.LoadSVGFromBuffer(svg);
        var texture = ImageTexture.CreateFromImage(image); _owned.Add(texture); return texture;
    }
    internal static ThemeDB Service => ServiceHolder.Singleton.Value;
    internal Theme GetDefaultThemeCore() { ThrowIfDisposed(); if (_defaultTheme.IsDisposed) throw new ObjectDisposedException(nameof(Theme)); return _defaultTheme; }
    internal event Action? FallbackChangedCore;
    internal event Action? ContextChanged;
    private void DefaultChanged(Resource _) => ContextChanged?.Invoke();
    private void DefaultDisposed(ElectronObject _) => ContextChanged?.Invoke();
    private void NotifyFallback()
    {
        Exception? contextError = null, fallbackError = null;
        try { ContextChanged?.Invoke(); } catch (Exception error) { contextError = error; }
        try { FallbackChangedCore?.Invoke(); } catch (Exception error) { fallbackError = error; }
        Resource.ThrowCombined(contextError, fallbackError);
    }
    internal float FallbackBaseScaleCore
    {
        get { lock (_gate) { ThrowIfDisposed(); return _baseScale; } }
        set { lock (_gate) { ThrowIfDisposed(); if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); if (_baseScale == value) return; _baseScale = value; } NotifyFallback(); }
    }
    internal Font? FallbackFontCore
    {
        get { lock (_gate) { ThrowIfDisposed(); return _font; } }
        set { lock (_gate) { ThrowIfDisposed(); if (value is { IsDisposed: true }) throw new ObjectDisposedException(nameof(value)); if (ReferenceEquals(_font, value)) return; _font = value; } NotifyFallback(); }
    }
    internal int FallbackFontSizeCore
    {
        get { lock (_gate) { ThrowIfDisposed(); return _fontSize; } }
        set { lock (_gate) { ThrowIfDisposed(); if (_fontSize == value) return; _fontSize = value; } NotifyFallback(); }
    }
    internal Texture? FallbackIconCore
    {
        get
        {
            lock (_gate)
            {
                ThrowIfDisposed();
                if (!_iconInitialized)
                {
                    using var image = new Image();
                    image.LoadSVGFromBuffer("""<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16"><path fill="#ff5d5d" d="M2 1v8.586l1.293-1.293a1 1 0 0 1 1.414 0L7 10.587l2.293-2.293a1 1 0 0 1 1.414 0L13 10.586l1-1V6H9V1H2zm8 0v4h4zm-6 9.414-2 2V15h12v-2.586l-.293.293a1 1 0 0 1-1.414 0L10 10.414l-2.293 2.293a1 1 0 0 1-1.414 0L4 10.414z"/></svg>"""u8);
                    _icon = ImageTexture.CreateFromImage(image); _owned.Add(_icon); _iconInitialized = true;
                }
                return _icon;
            }
        }
        set { lock (_gate) { ThrowIfDisposed(); if (value is { IsDisposed: true }) throw new ObjectDisposedException(nameof(value)); if (_iconInitialized && ReferenceEquals(_icon, value)) return; _iconInitialized = true; _icon = value; } NotifyFallback(); }
    }
    internal StyleBox? FallbackStyleBoxCore
    {
        get { lock (_gate) { ThrowIfDisposed(); return _style; } }
        set { lock (_gate) { ThrowIfDisposed(); if (value is { IsDisposed: true }) throw new ObjectDisposedException(nameof(value)); if (ReferenceEquals(_style, value)) return; _style = value; } NotifyFallback(); }
    }
    internal static bool IsNativeType(string name) => NativeTypes.ContainsKey(name);
    internal static string[] NativeDependencies(Type type) => TypeChains.GetOrAdd(type, static current =>
    {
        var result = new List<string>();
        for (var ancestor = current; ancestor is not null && typeof(ElectronObject).IsAssignableFrom(ancestor); ancestor = ancestor.BaseType) result.Add(ancestor.Name);
        return result.ToArray();
    });
    internal static string[] NativeDependencies(string name) => NativeTypes.TryGetValue(name, out var chain) ? chain : NameChains.GetOrAdd(name, static key => [key]);
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ContextChanged = null; FallbackChangedCore = null;
            _defaultTheme.Changed -= DefaultChanged; _defaultTheme.Disposed -= DefaultDisposed;
            _defaultTheme.Dispose(); foreach (var owned in _owned) owned.Dispose(); _owned.Clear();
        }
        base.Dispose(disposing);
    }
}
