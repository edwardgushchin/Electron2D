using System.Globalization;

namespace Electron2D;

/// <summary>A numeric text field with stepped arrow buttons, press repeat and accelerated vertical dragging.</summary>
/// <remarks>Owns one borrowed LineEdit. Range supplies shared double values, bounds, step and typed events.
/// Numeric expressions are evaluated on submission; malformed input restores the formatted value.
/// Editing/formula parsing and first-seen text are cold operations. Retained rendering and prepared text reuse avoid allocation.</remarks>
public partial class SpinBox : Range
{
    private readonly SpinBoxLineEdit _line;
    private string _prefix = "", _suffix = "", _numberLocale = "";
    private bool _updateOnTextChanged, _accepted = true, _formatting, _disposing;
    private double _arrowStep;
    private bool _arrowRound;
    private bool _upHover, _downHover, _upPressed, _downPressed, _dragAllowed, _dragging;
    private bool _nativeCapture;
    private MouseMode _priorMouseMode;
    private Vector2 _capturePosition, _mousePosition;
    private double _dragValue, _dragDifference, _repeatRemaining;
    private bool _repeat;
    private Rect2 _upRect, _downRect, _fieldSeparator, _buttonSeparator;
    private int _buttonBlock;
    private readonly Action<string> _applyDeferred;
    private readonly Action<bool> _editDeferred;
    private readonly Action _changeDeferred;
    private bool _changeQueued;
    private string _queuedText = "";
    // ponytail: 64 formatted states bound retained text; new values are cold, use span-backed text if continuous novel formatting becomes a hot contract.
    private readonly (double Value, int Decimals, bool Editing, string Text)[] _textCache = new (double, int, bool, string)[64];
    private int _cacheCount, _cacheNext;
    /// <summary>Creates an editable field with Step=1, empty affixes and vertical Fill sizing.</summary>
    public SpinBox()
    {
        Step = 1; SizeFlagsVertical = SizeFlags.Fill;
        _applyDeferred = DeferredApply; _editDeferred = DeferredEditing; _changeDeferred = DeferredChanged;
        _line = new SpinBoxLineEdit(this) { Name = "_spinbox_line_edit", ThemeTypeVariation = "SpinBoxInnerLineEdit", MouseFilter = MouseFilter.Pass, UseParentMaterial = true };
        AddChild(_line, InternalMode.Front);
        _line.TextSubmitted += TextSubmitted; _line.EditingToggled += EditingToggled; _line.TextChanged += TextChanged;
        _line.FocusExited += EndInteraction; Changed += ConfigurationChanged;
        RefreshText(); ComputeSizes();
    }
    /// <summary>Gets or sets the borrowed field's text alignment.</summary><value>Left initially.</value>
    public HorizontalAlignment Alignment { get { CheckSpinBox(); return _line.Alignment; } set { EnsureMutable(); _line.Alignment = value; } }
    /// <summary>Gets or sets editing and pointer-step availability; disabling cancels repeat and capture.</summary><value>True initially.</value>
    public bool Editable { get { CheckSpinBox(); return _line.Editable; } set { EnsureMutable(); if (!value) EndInteraction(); _line.Editable = value; QueueRedraw(); } }
    /// <summary>Gets or sets deferred evaluation after user text changes.</summary><value>False initially. Intermediate formulas can be replaced by their current result.</value>
    public bool UpdateOnTextChanged { get { CheckSpinBox(); return _updateOnTextChanged; } set { EnsureMutable(); _updateOnTextChanged = value; } }
    /// <summary>Gets or sets a prefix shown with a separating space outside editing.</summary><value>Empty initially.</value>
    public string Prefix { get { CheckSpinBox(); return _prefix; } set { EnsureMutable(); ArgumentNullException.ThrowIfNull(value); if (_prefix == value) return; _prefix = value; ClearTextCache(); RefreshText(); } }
    /// <summary>Gets or sets a suffix shown with a separating space outside editing.</summary><value>Empty initially.</value>
    public string Suffix { get { CheckSpinBox(); return _suffix; } set { EnsureMutable(); ArgumentNullException.ThrowIfNull(value); if (_suffix == value) return; _suffix = value; ClearTextCache(); RefreshText(); } }
    /// <summary>Gets or sets the arrow increment; zero uses Range.Step.</summary><value>Zero initially; finite signed increments are retained.</value>
    /// <exception cref="ArgumentOutOfRangeException">The increment is nonfinite.</exception>
    public double CustomArrowStep { get { CheckSpinBox(); return _arrowStep; } set { EnsureMutable(); if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); _arrowStep = value; } }
    /// <summary>Gets or sets arrow-grid snapping before the ordinary Range snap.</summary><value>False initially.</value>
    public bool CustomArrowRound { get { CheckSpinBox(); return _arrowRound; } set { EnsureMutable(); _arrowRound = value; } }
    /// <summary>Gets or sets selecting all numeric text when the field gains editing focus.</summary><value>False initially.</value>
    public bool SelectAllOnFocus { get { CheckSpinBox(); return _line.SelectAllOnFocus; } set { EnsureMutable(); _line.SelectAllOnFocus = value; } }
    /// <summary>Returns the stable required numeric input control.</summary><returns>A borrowed owned LineEdit; preserve its parent and lifetime.</returns>
    public LineEdit GetLineEdit() { CheckSpinBox(); return _line; }
    /// <summary>Evaluates the current field text and refreshes it, preserving value after malformed expressions.</summary>
    public void Apply() { EnsureMutable(); Submit(_line.Text); }
    private void CheckSpinBox() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); if (_line.IsDisposed || _line.Parent != this) throw new InvalidOperationException("The required SpinBox input control has left its owner."); }
    private void ConfigurationChanged() { if (_disposing) return; ClearTextCache(); RefreshText(); QueueRedraw(); }
    private void ClearTextCache() { Array.Clear(_textCache); _cacheCount = _cacheNext = 0; }
    private static int Decimals(double step)
    {
        var decimals = Mathf.StepDecimals(step); if (decimals != 0 || step == 0) return decimals;
        var number = Math.Abs(step); while (number >= 10 && double.IsFinite(number)) number /= 10; return Mathf.StepDecimals(number);
    }
    private void RefreshText(bool onlyChanged = false)
    {
        if (_line == null || _line.IsDisposed || _disposing || _formatting) return;
        var culture = NumericCulture(); if (_numberLocale != culture.Name) { _numberLocale = culture.Name; ClearTextCache(); }
        var value = Value; var editing = _line.IsEditing(); var decimals = Decimals(Step);
        if (!_accepted && _updateOnTextChanged && !_line.Text.Replace(',', '.').Contains('.')) decimals = 0;
        string? text = null;
        for (var i = 0; i < _cacheCount; i++) if (_textCache[i].Value.Equals(value) && _textCache[i].Decimals == decimals && _textCache[i].Editing == editing) { text = _textCache[i].Text; break; }
        if (text == null)
        {
            text = double.IsNaN(value) ? "nan" : double.IsPositiveInfinity(value) ? "inf" : double.IsNegativeInfinity(value) ? "-inf" : value.ToString("F" + decimals, CultureInfo.InvariantCulture);
            if (LocalizeNumeralSystem) text = TranslationServer.FormatNumber(text, culture.Name.Length == 0 ? "en" : culture.Name);
            if (!editing) { if (_prefix.Length != 0) text = _prefix + " " + text; if (_suffix.Length != 0) text += " " + _suffix; }
            _textCache[_cacheNext] = (value, decimals, editing, text); _cacheNext = (_cacheNext + 1) % _textCache.Length; _cacheCount = Math.Min(_cacheCount + 1, _textCache.Length);
        }
        if (_line.Text == text) return;
        _formatting = true;
        try { _line.SetTextPreservingSelection(text); }
        finally { _formatting = false; }
    }
    private void Submit(string text)
    {
        if (text.Length == 0) return;
        if (_updateOnTextChanged)
        {
            var normalized = text.Replace(',', '.');
            if (!normalized.StartsWith('.') && normalized.EndsWith('.')) return;
            if (normalized.StartsWith('.')) { _line.Text = "0."; _line.CaretColumn = 2; return; }
        }
        var culture = NumericCulture(); var expression = TranslationServer.ParseNumber(StripAffixes(text.Replace(';', ',')), culture.Name.Length == 0 ? "en" : culture.Name);
        if (_updateOnTextChanged) expression = expression.Replace(',', '.');
        if (!NumericExpression.TryEvaluate(expression, out var value) && !NumericExpression.TryEvaluate(TranslationServer.ParseNumber(StripAffixes(text), culture.Name.Length == 0 ? "en" : culture.Name), out value)) { RefreshText(); return; }
        List<Exception>? errors = null;
        try { Value = value; } catch (Exception error) { CollectException(ref errors, error); }
        try { if (!IsDisposed) RefreshText(); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("SpinBox submission callbacks failed.", errors);
    }
    private CultureInfo NumericCulture()
    {
        var locale = TranslationServer.GetOrAddDomain(TranslationDomain).LocaleOverride;
        return locale.Length == 0 ? TranslationServer.Culture : CultureInfo.GetCultureInfo(locale.Replace('_', '-'));
    }
    private string StripAffixes(string text) { if (_prefix.Length != 0 && text.StartsWith(_prefix + " ", StringComparison.Ordinal)) text = text[(_prefix.Length + 1)..]; if (_suffix.Length != 0 && text.EndsWith(" " + _suffix, StringComparison.Ordinal)) text = text[..^(_suffix.Length + 1)]; return text; }
    private void TextSubmitted(string text) { if (Tree is { } tree) { if (!tree.IsClosing) tree.SetDeferred(_applyDeferred, text); } else Submit(text); }
    private void DeferredApply(string text) { if (!IsDisposed && !_line.IsDisposed) Submit(text); }
    private void EditingToggled(bool editing) { if (Tree is { } tree) { if (!tree.IsClosing) tree.SetDeferred(_editDeferred, editing); } else DeferredEditing(editing); }
    private void DeferredEditing(bool editing)
    {
        if (IsDisposed || _line.IsDisposed) return;
        var caret = _line.CaretColumn;
        if (editing) { RefreshText(); _line.CaretColumn = caret; if (SelectAllOnFocus && !Input.IsMouseButtonPressed(MouseButton.Left)) _line.SelectAll(); }
        else { _accepted = true; if (Input.IsActionPressed("ui_cancel") || _line.Text.Length == 0) RefreshText(); else { Submit(_line.Text.TrimEnd('.', ',')); RefreshText(); } }
    }
    private void TextChanged(string text) { if (!_updateOnTextChanged || _formatting) return; _queuedText = text; if (_changeQueued) return; _changeQueued = true; if (Tree is { } tree) { if (!tree.IsClosing) tree.Defer(_changeDeferred); } else DeferredChanged(); }
    private void DeferredChanged() { _changeQueued = false; if (IsDisposed || !_updateOnTextChanged) return; _accepted = false; var caret = _line.CaretColumn; Submit(_queuedText); if (!IsDisposed && !_queuedText.StartsWith('.')) _line.CaretColumn = caret; }
    /// <inheritdoc />
    protected override Vector2 OnGetMinimumSize() { if (_line == null) return Vector2.Zero; ComputeSizes(); return _line.GetBoundMinimumSize() + new Vector2(_buttonBlock, 0); }
    /// <inheritdoc />
    protected override void OnValueChanged(double newValue) { RefreshText(true); QueueRedraw(); base.OnValueChanged(newValue); }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(SpinBox) ? CreateSpinBox : base.CreateSceneInstanceFactory();
    private static Node CreateSpinBox() => new SpinBox();
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) { _disposing = true; EndInteraction(); Changed -= ConfigurationChanged; ClearTextCache(); } base.Dispose(disposing); }
    private sealed class SpinBoxLineEdit(SpinBox owner) : LineEdit
    {
        protected override void OnGUIInput(InputEvent input) { if (owner._dragging) { AcceptEvent(); return; } base.OnGUIInput(input); }
        protected override void ValidateDisposal() { if (!owner.IsDisposed && !owner._disposing) throw new InvalidOperationException("The required numeric input is owned by SpinBox."); base.ValidateDisposal(); }
    }
}
