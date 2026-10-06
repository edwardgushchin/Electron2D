using System.Collections.ObjectModel;

namespace Electron2D;

/// <summary>Caches typed scalar-column color transitions for one borrowed multiline editor.</summary>
/// <remarks>Each instance binds one live TextEdit. The editor borrows this resource; custom subclasses provide
/// transitions through OnGetLineSyntaxHighlighting. Cache computation is cold owner-thread work.</remarks>
public class SyntaxHighlighter : Resource
{
    private WeakReference<TextEdit>? _editor;
    private readonly Dictionary<int, IReadOnlyDictionary<int, Color>> _cache = [];
    private static readonly IReadOnlyDictionary<int, Color> Empty = new ReadOnlyDictionary<int, Color>(new Dictionary<int, Color>());
    private bool _computing;
    private int _version = -1;
    /// <summary>Creates an unbound highlighter with no color overrides.</summary>
    public SyntaxHighlighter() { }
    /// <summary>Returns the cached immutable color transitions for a logical line.</summary>
    /// <param name="line">A nonnegative logical line index.</param><returns>Scalar column to color; entries continue until the next transition.</returns>
    public IReadOnlyDictionary<int, Color> GetLineSyntaxHighlighting(int line)
    {
        CheckHighlighter(); if (line < 0) throw new ArgumentOutOfRangeException(nameof(line)); var editor = GetTextEdit(); if (editor == null) return Empty;
        if (_version != editor.GetVersion()) { _version = editor.GetVersion(); _cache.Clear(); OnClearHighlightingCache(); }
        if (line >= editor.GetLineCount()) throw new ArgumentOutOfRangeException(nameof(line)); if (_cache.TryGetValue(line, out var cached)) return cached;
        if (_computing) throw new InvalidOperationException("Syntax highlighting cannot recursively compute the same resource.");
        _computing = true;
        try
        {
            var transitions = OnGetLineSyntaxHighlighting(line) ?? throw new InvalidOperationException("The highlighter returned null."); var copy = new SortedDictionary<int, Color>();
            foreach (var pair in transitions) { if (pair.Key < 0 || !pair.Value.IsFinite()) throw new InvalidOperationException("Invalid syntax color transition."); copy.Add(pair.Key, pair.Value); }
            var result = new ReadOnlyDictionary<int, Color>(copy); _cache[line] = result; return result;
        }
        finally { _computing = false; }
    }
    /// <summary>Calculates scalar-column color transitions for an uncached logical line.</summary><param name="line">An existing line.</param><returns>Readonly typed transition data; empty means ordinary font color.</returns>
    protected virtual IReadOnlyDictionary<int, Color> OnGetLineSyntaxHighlighting(int line) => Empty;
    /// <summary>Clears retained transitions and invokes the subclass cache-clear hook.</summary>
    public void ClearHighlightingCache() { CheckHighlighter(); if (_computing) throw new InvalidOperationException("An active highlighter cannot clear its cache."); _cache.Clear(); GetTextEdit()?.InvalidateTextLayout(); OnClearHighlightingCache(); }
    /// <summary>Clears subclass caches after the base cache has been cleared.</summary>
    protected virtual void OnClearHighlightingCache() { }
    /// <summary>Clears all cached transitions, then updates subclass state when bound.</summary>
    public void UpdateCache() { ClearHighlightingCache(); if (GetTextEdit() != null) OnUpdateCache(); }
    /// <summary>Updates subclass state using the bound editor.</summary>
    protected virtual void OnUpdateCache() { }
    /// <summary>Returns the live borrowed editor or null.</summary><returns>The bound TextEdit; its lifetime is independent of the resource.</returns>
    public TextEdit? GetTextEdit() { ThrowIfDisposed(); return _editor?.TryGetTarget(out var editor) == true && !editor.IsDisposed ? editor : null; }
    private void CheckHighlighter() { ThrowIfDisposed(); GetTextEdit()?.Tree?.EnsureOwnerThread(); }
    internal void Bind(TextEdit? editor)
    {
        CheckHighlighter(); var old = GetTextEdit(); if (old == editor) return; if (old != null && editor != null) throw new InvalidOperationException("A SyntaxHighlighter cannot be shared by live editors.");
        if (old != null) old.LinesEditedFrom -= LinesEdited; _editor = editor == null ? null : new(editor); if (editor != null) editor.LinesEditedFrom += LinesEdited; UpdateCache();
    }
    private void LinesEdited(int from, int to)
    { if (IsDisposed) return; var first = Math.Max(0, Math.Min(from, to) - 1); foreach (var key in _cache.Keys.Where(k => k >= first).ToArray()) _cache.Remove(key); OnClearHighlightingCache(); }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => GetType() == typeof(SyntaxHighlighter) ? new SyntaxHighlighter() : base.CreateDuplicateInstance();
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) { if (GetTextEdit() is { } editor) editor.LinesEditedFrom -= LinesEdited; _editor = null; _cache.Clear(); } base.Dispose(disposing); }
}
