namespace Electron2D;

public partial class RichTextLabel
{
    /// <summary>Gets or sets whether Text assignments interpret markup.</summary><value>false initially.</value>
    public bool BBCodeEnabled { get { CheckRich(); return _bbcode; } set { MutableRich(); if (_bbcode == value) return; _bbcode = value; if (value) ParseBBCode(Atr(_text)); else { Clear(); AddText(Atr(_text)); } _externalStack = false; Changed(); } }
    /// <summary>Gets or sets whether pointer and keyboard selection is available.</summary><value>false initially.</value>
    public bool SelectionEnabled { get { CheckRich(); return _selectionEnabled; } set { MutableRich(); if (_selectionEnabled == value) return; _selectionEnabled = value; if (!value) Deselect(); FocusMode = value ? FocusMode.All : FocusMode.Accessibility; QueueRedraw(); } }
    /// <summary>Gets or sets whether right click opens the owned context menu.</summary><value>false initially.</value>
    public bool ContextMenuEnabled { get { CheckRich(); return _contextMenu; } set { MutableRich(); if (_contextMenu == value) return; _contextMenu = value; QueueRedraw(); } }
    /// <summary>Gets or sets whether focus loss clears selection.</summary><value>true initially.</value>
    public bool DeselectOnFocusLossEnabled { get { CheckRich(); return _deselectFocus; } set { MutableRich(); if (_deselectFocus == value) return; _deselectFocus = value; QueueRedraw(); } }
    /// <summary>Gets or sets whether selected text can be dragged.</summary><value>true initially.</value>
    public bool DragAndDropSelectionEnabled { get { CheckRich(); return _dragSelection; } set { MutableRich(); if (_dragSelection == value) return; _dragSelection = value; QueueRedraw(); } }
    /// <summary>Gets or sets whether shaped content contributes to minimum size.</summary><value>false initially.</value>
    public bool FitContent { get { CheckRich(); return _fitContent; } set { MutableRich(); if (_fitContent == value) return; _fitContent = value; UpdateMinimumSize(); Changed(); } }
    /// <summary>Gets or sets whether hover hints receive underline styling.</summary><value>true initially.</value>
    public bool HintUnderlined { get { CheckRich(); return _hintUnderlined; } set { MutableRich(); if (_hintUnderlined == value) return; _hintUnderlined = value; QueueRedraw(); } }
    /// <summary>Gets or sets whether link underline policies are applied.</summary><value>true initially.</value>
    public bool MetaUnderlined { get { CheckRich(); return _metaUnderlined; } set { MutableRich(); if (_metaUnderlined == value) return; _metaUnderlined = value; QueueRedraw(); } }
    /// <summary>Gets or sets whether content can scroll through its owned scrollbar.</summary><value>true initially.</value>
    public bool ScrollActive { get { CheckRich(); return _scrollActive; } set { MutableRich(); if (_scrollActive == value) return; _scrollActive = value; Changed(); } }
    /// <summary>Gets or sets whether layout follows the content bottom.</summary><value>false initially.</value>
    public bool ScrollFollowing { get { CheckRich(); return _scrollFollowing; } set { MutableRich(); if (_scrollFollowing == value) return; _scrollFollowing = value; Changed(); } }
    /// <summary>Gets or sets whether scrolling follows the reveal position.</summary><value>false initially.</value>
    public bool ScrollFollowingVisibleCharacters { get { CheckRich(); return _scrollFollowingVisible; } set { MutableRich(); if (_scrollFollowingVisible == value) return; _scrollFollowingVisible = value; Changed(); } }
    /// <summary>Gets or sets whether remapped copy and selection actions are handled.</summary><value>true initially.</value>
    public bool ShortcutKeysEnabled { get { CheckRich(); return _shortcutKeys; } set { MutableRich(); if (_shortcutKeys == value) return; _shortcutKeys = value; QueueRedraw(); } }
    /// <summary>Gets or sets whether paragraph shaping runs asynchronously.</summary><value>false initially.</value>
    public bool Threaded { get { CheckRich(); return _threaded; } set { MutableRich(); if (_threaded == value) return; _threaded = value; Changed(); } }
    /// <summary>Gets or sets the paragraph wrapping policy.</summary><value>TextAutowrapMode.WordSmart initially.</value>
    public TextAutowrapMode AutowrapMode { get { CheckRich(); return _autowrap; } set { MutableRich(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); if (_autowrap == value) return; _autowrap = value; Changed(); } }
    /// <summary>Gets or sets the whitespace trimming flags at wrapped edges.</summary><value>TextLineBreakFlags.TrimStartEdgeSpaces | TextLineBreakFlags.TrimEndEdgeSpaces initially.</value>
    public TextLineBreakFlags AutowrapTrimFlags { get { CheckRich(); return _trim; } set { MutableRich(); if (_trim == value) return; _trim = value; Changed(); } }
    /// <summary>Gets or sets the paragraph fill rules.</summary><value>(TextJustificationFlags)163 initially.</value>
    public TextJustificationFlags JustificationFlags { get { CheckRich(); return _justification; } set { MutableRich(); if (_justification == value) return; _justification = value; Changed(); } }
    /// <summary>Gets or sets the default paragraph alignment.</summary><value>HorizontalAlignment.Left initially.</value>
    public HorizontalAlignment HorizontalAlignment { get { CheckRich(); return _horizontal; } set { MutableRich(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); if (_horizontal == value) return; _horizontal = value; Changed(); } }
    /// <summary>Gets or sets the placement of shorter content in its viewport.</summary><value>VerticalAlignment.Top initially.</value>
    public VerticalAlignment VerticalAlignment { get { CheckRich(); return _vertical; } set { MutableRich(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); if (_vertical == value) return; _vertical = value; QueueRedraw(); } }
    /// <summary>Gets or sets the default bidirectional paragraph base.</summary><value>TextDirection.Auto initially.</value>
    public TextDirection TextDirection { get { CheckRich(); return _direction; } set { MutableRich(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); if (_direction == value) return; _direction = value; Changed(); } }
    /// <summary>Gets or sets the scalar or glyph reveal policy.</summary><value>TextVisibleCharactersBehavior.CharsBeforeShaping initially.</value>
    public TextVisibleCharactersBehavior VisibleCharactersBehavior { get { CheckRich(); return _visibleBehavior; } set { MutableRich(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); if (_visibleBehavior == value) return; _visibleBehavior = value; Changed(); } }
    /// <summary>Gets or sets the structured text context parser.</summary><value>StructuredTextParser.Default initially.</value>
    public StructuredTextParser StructuredTextBIDIOverride { get { CheckRich(); return _parser; } set { MutableRich(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); if (_parser == value) return; _parser = value; Changed(); } }
    /// <summary>Gets or sets untranslated source text; assignment replaces manual stack edits.</summary><value>Empty initially.</value>
    public string Text { get { CheckRich(); return _text; } set { MutableRich(); ArgumentNullException.ThrowIfNull(value); _text = value; var translated = Atr(value); if (_bbcode) ParseBBCode(translated); else { Clear(); AddText(translated); } _externalStack = false; if (_visibleRatio < 1) _visibleCharacters = (int)Math.Floor(GetTotalCharacterCount() * _visibleRatio); Changed(); } }
    /// <summary>Gets or sets the shaping language.</summary><value>Empty initially.</value>
    public string Language { get { CheckRich(); return _language; } set { MutableRich(); ArgumentNullException.ThrowIfNull(value); _language = value; Changed(); } }
    /// <summary>Gets or sets a positive default tab width.</summary><value>Four initially.</value>
    public int TabSize { get { CheckRich(); return _tabSize; } set { MutableRich(); if (value < 1) throw new ArgumentOutOfRangeException(nameof(value)); _tabSize = value; Changed(); } }
    /// <summary>Gets or sets the nonnegative threaded progress delay in milliseconds.</summary><value>1000 initially.</value>
    public int ProgressBarDelay { get { CheckRich(); return _progressDelay; } set { MutableRich(); if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)); _progressDelay = value; } }
    /// <summary>Gets or sets the scalar/glyph reveal budget.</summary><value>-1 shows all.</value>
    public int VisibleCharacters { get { CheckRich(); return _visibleCharacters; } set { MutableRich(); if (value < -1) throw new ArgumentOutOfRangeException(nameof(value)); if (value == _visibleCharacters) return; _visibleCharacters = value; var total = GetTotalCharacterCount(); _visibleRatio = value < 0 || total == 0 ? 1 : (float)value / total; RevealChanged(); } }
    /// <summary>Gets or sets the reveal fraction.</summary><value>One initially; zero through one reveals a proportional scalar budget.</value>
    public float VisibleRatio { get { CheckRich(); return _visibleRatio; } set { MutableRich(); if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); _visibleRatio = value; _visibleCharacters = value < 0 || value >= 1 ? -1 : (int)Math.Floor(GetTotalCharacterCount() * value); RevealChanged(); } }
    /// <summary>Gets or sets copied finite nonnegative tab positions.</summary><value>Empty initially.</value>
    public float[] TabStops { get { CheckRich(); return (float[])_tabs.Clone(); } set { MutableRich(); ArgumentNullException.ThrowIfNull(value); if (value.Any(v => !float.IsFinite(v) || v < 0)) throw new ArgumentException("Invalid tab positions.", nameof(value)); _tabs = (float[])value.Clone(); Changed(); } }
    /// <summary>Gets or sets copied structured-text parser options.</summary><value>Empty initially.</value>
    public string[] StructuredTextBIDIOverrideOptions { get { CheckRich(); return (string[])_parserOptions.Clone(); } set { MutableRich(); ArgumentNullException.ThrowIfNull(value); if (value.Any(v => v == null)) throw new ArgumentException("Parser options cannot contain null.", nameof(value)); _parserOptions = (string[])value.Clone(); Changed(); } }
    /// <summary>Gets or replaces copied membership of borrowed custom effects.</summary><value>Empty initially; effect resources remain borrowed.</value>
    public RichTextEffect[] CustomEffects { get { CheckRich(); return (RichTextEffect[])_effects.Clone(); } set { MutableRich(); ArgumentNullException.ThrowIfNull(value); if (value.Any(e => e == null || e.IsDisposed)) throw new ArgumentException("Effects must be live.", nameof(value)); _effects = (RichTextEffect[])value.Clone(); ReloadEffects(); } }
    /// <summary>Installs one borrowed effect for tag lookup.</summary><param name="effect">Live effect resource.</param>
    public void InstallEffect(RichTextEffect effect) { MutableRich(); ArgumentNullException.ThrowIfNull(effect); if (effect.IsDisposed) throw new ObjectDisposedException(nameof(effect)); _effects = [.. _effects, effect]; ReloadEffects(); }
    /// <summary>Rebuilds source tags using the current installed effects.</summary>
    public void ReloadEffects() { MutableRich(); if (_bbcode) { ParseBBCode(Atr(_text)); _externalStack = false; } else Changed(); }
}
