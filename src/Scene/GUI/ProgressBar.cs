using System.Globalization;

namespace Electron2D;

/// <summary>Specifies a themed progress bar's fill direction.</summary>
public enum ProgressFillMode
{
    /// <summary>Fills toward the end of the horizontal reading direction.</summary>
    BeginToEnd = 0,
    /// <summary>Fills toward the beginning of the horizontal reading direction.</summary>
    EndToBegin = 1,
    /// <summary>Fills downward.</summary>
    TopToBottom = 2,
    /// <summary>Fills upward.</summary>
    BottomToTop = 3
}

/// <summary>Displays a shared numeric range with themed background, fill and percentage text.</summary>
/// <remarks>Indeterminate mode animates a clipped segment instead of showing the value. Hidden controls
/// retain their animation position. An EditedSceneRoot subtree previews a centered static segment unless
/// EditorPreviewIndeterminate is enabled. Styles and font are borrowed through ordinary theme lookup.</remarks>
public class ProgressBar : Range
{
    private ProgressFillMode _fillMode;
    private bool _showPercentage = true, _indeterminate, _editorPreviewIndeterminate, _previewPaused;
    private float _fillProgress;
    private readonly (double Value, string? Text)[] _textCache = new (double, string?)[16];
    private int _cacheCount, _cacheNext;
    private string? _cachedLocale;
    private bool _cachedLocalized;
    private static readonly PropertyDescriptor[] ProgressProperties =
    [
        new PropertyDescriptor<ProgressBar, ProgressFillMode>(nameof(FillMode), node => node.FillMode, (node, value) => node.FillMode = value, _ => ProgressFillMode.BeginToEnd, stored: true),
        new PropertyDescriptor<ProgressBar, bool>(nameof(ShowPercentage), node => node.ShowPercentage, (node, value) => node.ShowPercentage = value, _ => true, stored: true),
        new PropertyDescriptor<ProgressBar, bool>(nameof(Indeterminate), node => node.Indeterminate, (node, value) => node.Indeterminate = value, _ => false, stored: true),
        new PropertyDescriptor<ProgressBar, bool>(nameof(EditorPreviewIndeterminate), node => node.EditorPreviewIndeterminate, (node, value) => node.EditorPreviewIndeterminate = value, _ => false, stored: true)
    ];

    /// <summary>Creates a determinate bar with inherited range defaults, including Step 0.01 and vertical ShrinkBegin.</summary>
    public ProgressBar() { }
    private void CheckProgressBar() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    private bool IsEdited => Tree?.EditedSceneRoot is { } root && (ReferenceEquals(root, this) || root.IsAncestorOf(this));

    /// <summary>Gets or sets the fill direction. Every assignment resets the animation position.</summary>
    /// <value>BeginToEnd initially; horizontal directions follow RTL layout.</value>
    /// <exception cref="ArgumentOutOfRangeException">The direction is undefined.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner or mutation is capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public ProgressFillMode FillMode
    {
        get { CheckProgressBar(); return _fillMode; }
        set { EnsureMutable(); if (value is < ProgressFillMode.BeginToEnd or > ProgressFillMode.BottomToTop) throw new ArgumentOutOfRangeException(nameof(value)); _fillMode = value; _fillProgress = 0; QueueRedraw(); }
    }
    /// <summary>Gets or sets percentage text. Indeterminate mode hides text without changing this setting.</summary>
    /// <value>True initially.</value>
    /// <exception cref="InvalidOperationException">Access is off-owner or mutation is capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public bool ShowPercentage
    {
        get { CheckProgressBar(); return _showPercentage; }
        set { EnsureMutable(); if (_showPercentage == value) return; _showPercentage = value; UpdateMinimumSize(); QueueRedraw(); }
    }
    /// <summary>Gets or sets a moving segment for progress whose completion ratio is unknown.</summary>
    /// <value>False initially. Changes reset the animation position.</value>
    /// <remarks>Uses internal process steps and ordinary pause/time-scale policy. Hidden controls do not advance.</remarks>
    /// <exception cref="InvalidOperationException">Access is off-owner or mutation is capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public bool Indeterminate
    {
        get { CheckProgressBar(); return _indeterminate; }
        set { EnsureMutable(); if (_indeterminate == value) return; _indeterminate = value; _fillProgress = 0; _previewPaused = false; SetInternalProcessing(value, false); UpdateMinimumSize(); QueueRedraw(); }
    }
    /// <summary>Gets or sets animation in the selected EditedSceneRoot subtree.</summary>
    /// <value>False initially. It has no effect outside the edited subtree.</value>
    /// <remarks>A disabled preview draws the segment centered. Changing this option in the edited subtree resets its position.</remarks>
    /// <exception cref="InvalidOperationException">Access is off-owner or mutation is capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public bool EditorPreviewIndeterminate
    {
        get { CheckProgressBar(); return _editorPreviewIndeterminate; }
        set { EnsureMutable(); if (_editorPreviewIndeterminate == value) return; _editorPreviewIndeterminate = value; if (IsEdited) { _fillProgress = 0; _previewPaused = false; QueueRedraw(); } }
    }
    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Percentage text has no theme font.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The resolved font size is unsupported.</exception>
    /// <exception cref="ObjectDisposedException">A borrowed font or style is disposed.</exception>
    protected override Vector2 OnGetMinimumSize()
    {
        var background = GetThemeStyleBox("background")?.GetMinimumSize() ?? Vector2.Zero;
        var minimum = background.Max(GetThemeStyleBox("fill")?.GetMinimumSize() ?? Vector2.Zero);
        if (_showPercentage)
        {
            var font = GetThemeFont("font") ?? throw new InvalidOperationException("Progress text requires a theme font.");
            minimum.Y = MathF.Max(minimum.Y, background.Y + font.GetStringSize("100%", fontSize: GetThemeFontSize("font_size")).Y);
        }
        else minimum = minimum.Max(Vector2.One);
        return minimum;
    }
    private static float Pixel(float value)
    {
        var rounded = MathF.Round(value, MidpointRounding.AwayFromZero);
        if (!float.IsFinite(rounded) || rounded < int.MinValue || rounded >= 2147483648f) throw new InvalidOperationException("Progress fill exceeds integer pixel coordinates.");
        return rounded;
    }
    internal Rect2 FillRectangle(StyleBox fill)
    {
        var size = Size; var horizontal = _fillMode is ProgressFillMode.BeginToEnd or ProgressFillMode.EndToBegin;
        var reverse = horizontal ? (_fillMode == ProgressFillMode.BeginToEnd) == IsLayoutRTL() : _fillMode == ProgressFillMode.BottomToTop;
        var length = horizontal ? size.X : size.Y;
        if (_indeterminate)
        {
            var segment = MathF.Min(size.X, size.Y) * 2;
            if (!float.IsFinite(segment)) throw new InvalidOperationException("Progress segment exceeded finite coordinates.");
            if (IsEdited && !_editorPreviewIndeterminate) _fillProgress = MathF.Max(size.X, size.Y) / 2 + segment / 2;
            if (_fillProgress > length + segment) _fillProgress = reverse ? -segment : 0;
            var offset = reverse ? length - _fillProgress : _fillProgress - segment;
            var rect = horizontal ? new Rect2(offset, 0, segment, size.Y) : new Rect2(0, offset, size.X, segment);
            return rect.Intersection(new(Vector2.Zero, size));
        }
        var ratio = (float)Ratio;
        if (!float.IsFinite(ratio)) throw new InvalidOperationException("Progress fill requires a finite range ratio.");
        var minimum = fill.GetMinimumSize(); var padding = horizontal ? minimum.X : minimum.Y;
        if (!float.IsFinite(padding) || padding < int.MinValue || padding >= 2147483648f) throw new InvalidOperationException("Progress style exceeds integer pixel coordinates.");
        var pixels = Pixel(ratio * (length - (int)padding));
        if (pixels <= 0) return default;
        var origin = reverse ? Pixel((1 - ratio) * (length - (int)padding)) : 0;
        return horizontal ? new(origin, 0, pixels + padding, size.Y) : new(0, origin, size.X, pixels + padding);
    }
    internal string PercentageText()
    {
        var minimum = MinValue; var maximum = MaxValue; var value = Value;
        double ratio;
        if (Mathf.IsEqualApprox(maximum, minimum)) ratio = 1;
        else if (ExpEdit && minimum >= 0 && value >= 0)
        {
            var logMinimum = minimum == 0 ? 0 : Math.Log2(minimum);
            ratio = ((value == 0 ? 0 : Math.Log2(value)) - logMinimum) / (Math.Log2(maximum) - logMinimum);
        }
        else ratio = (value - minimum) / (maximum - minimum);
        if (!AllowLesser) ratio = Math.Max(0, ratio);
        if (!AllowGreater) ratio = Math.Min(1, ratio);
        var percentage = Math.Round(ratio * 100, MidpointRounding.AwayFromZero);
        if (!double.IsFinite(percentage)) throw new InvalidOperationException("Progress percentage requires a finite value.");
        var locale = TranslationServer.GetOrAddDomain(TranslationDomain).LocaleOverride;
        if (locale.Length == 0) { locale = TranslationServer.Culture.Name; if (locale.Length == 0) locale = "en"; }
        if (_cachedLocalized != LocalizeNumeralSystem || _cachedLocale != locale)
        {
            Array.Clear(_textCache); _cacheCount = _cacheNext = 0; _cachedLocalized = LocalizeNumeralSystem; _cachedLocale = locale;
        }
        for (var i = 0; i < _cacheCount; i++) if (_textCache[i].Value == percentage) return _textCache[i].Text!;
        var text = percentage.ToString("F0", CultureInfo.InvariantCulture);
        text = LocalizeNumeralSystem ? TranslationServer.FormatNumber(text, locale) + TranslationServer.GetPercentSign(locale) : text + "%";
        _textCache[_cacheNext] = (percentage, text); _cacheNext = (_cacheNext + 1) % _textCache.Length; _cacheCount = Math.Min(_cacheCount + 1, _textCache.Length);
        return text;
    }
    private void DrawProgressBar()
    {
        var background = GetThemeStyleBox("background") ?? throw new InvalidOperationException("Progress drawing requires a background style.");
        var fill = GetThemeStyleBox("fill") ?? throw new InvalidOperationException("Progress drawing requires a fill style.");
        var rectangle = FillRectangle(fill);
        DrawStyleBox(background, new(Vector2.Zero, Size));
        if (rectangle.Size.X > 0 && rectangle.Size.Y > 0) DrawStyleBox(fill, rectangle);
        if (_indeterminate || !_showPercentage) return;
        var font = GetThemeFont("font") ?? throw new InvalidOperationException("Progress text requires a theme font.");
        var fontSize = GetThemeFontSize("font_size"); var text = PercentageText(); var textSize = font.GetStringSize(text, fontSize: fontSize);
        var position = ((Size - textSize) / 2).Round(); position.Y += font.GetAscent(fontSize);
        var outline = GetThemeConstant("outline_size"); var outlineColor = GetThemeColor("font_outline_color");
        if (outline > 0 && outlineColor.A > 0) DrawStringOutline(font, position, text, fontSize: fontSize, size: outline, modulate: outlineColor);
        DrawString(font, position, text, fontSize: fontSize, modulate: GetThemeColor("font_color"));
    }
    private void Advance()
    {
        if (!_indeterminate || !IsVisibleInTree) return;
        var paused = IsEdited && !_editorPreviewIndeterminate;
        if (paused) { if (!_previewPaused) QueueRedraw(); _previewPaused = true; return; }
        _previewPaused = false;
        var progress = (float)(_fillProgress + ProcessDeltaTime * MathF.Max(200, MathF.Max(Size.X, Size.Y) / 2));
        if (!float.IsFinite(progress)) throw new InvalidOperationException("Progress animation exceeded finite coordinates.");
        _fillProgress = progress; QueueRedraw();
    }
    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Drawing lacks a required theme resource or exceeds finite/pixel geometry limits.</exception>
    /// <exception cref="ObjectDisposedException">A borrowed drawing resource is disposed.</exception>
    /// <exception cref="AggregateException">Required inherited/internal notification phases report callback failures.</exception>
    protected override void OnNotification(int what)
    {
        if (what == NotificationDraw) { base.OnNotification(what); DrawProgressBar(); return; }
        List<Exception>? errors = null;
        try { base.OnNotification(what); } catch (Exception error) { CollectException(ref errors, error); }
        if (!IsDisposed)
            try
            {
                switch (what)
                {
                    case NotificationInternalProcess: Advance(); break;
                    case NotificationTranslationChanged:
                    case NotificationThemeChanged:
                    case NotificationLayoutDirectionChanged: UpdateMinimumSize(); QueueRedraw(); break;
                }
            }
            catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Progress notification callbacks failed.", errors);
    }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(ProgressProperties);
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(ProgressBar) ? CreateProgressBar : base.CreateSceneInstanceFactory();
    private static Node CreateProgressBar() => new ProgressBar();
}
