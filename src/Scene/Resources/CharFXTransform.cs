namespace Electron2D;

/// <summary>Describes and transforms one rich-text glyph during an effect callback.</summary>
/// <remarks>Font, range, flags, count, outline and relative index are descriptive; changing them does not
/// redirect drawing. Color, glyph index, offset, transform and visibility affect the current glyph.</remarks>
public class CharFXTransform : ElectronObject
{
    private Color _color = Colors.Black;
    private Vector2 _offset;
    private Transform _transform = Transform.Identity;
    private double _elapsed;
    private RichTextEffectEnvironment _env = new();
    private Font? _font = null;
    private int _glyphCount = 0;
    private TextGraphemeFlags _glyphFlags = 0;
    private int _glyphIndex = 0;
    private bool _outline = false;
    private Vector2i _range = default;
    private int _relativeIndex = 0;
    private bool _visible = true;
    internal FontData? GlyphFace;
    /// <summary>Creates independent default glyph state.</summary>
    public CharFXTransform() { }
    /// <summary>Gets or sets the finite glyph color.</summary><value>Opaque black initially.</value>
    public Color Color { get { ThrowIfDisposed(); return _color; } set { ThrowIfDisposed(); if (!value.IsFinite()) throw new ArgumentException("Color must be finite.", nameof(value)); _color = value; } }
    /// <summary>Gets or sets the effect's elapsed seconds.</summary><value>Zero initially; the label supplies its paused-aware block clock.</value>
    public double ElapsedTime { get { ThrowIfDisposed(); return _elapsed; } set { ThrowIfDisposed(); if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); _elapsed = value; } }
    /// <summary>Gets or assigns the borrowed typed argument context.</summary><value>Independent empty context initially.</value>
    public RichTextEffectEnvironment Env { get { ThrowIfDisposed(); return _env; } set { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(value); _env = value; } }
    /// <summary>Gets or assigns descriptive borrowed font ownership.</summary><value>Null initially; writes do not change the actual glyph face.</value>
    public Font? Font { get { ThrowIfDisposed(); return _font; } set { ThrowIfDisposed(); _font = value; } }
    /// <summary>Gets or sets descriptive glyph count for the cluster.</summary><value>Zero initially; writes do not change drawing.</value>
    public int GlyphCount { get { ThrowIfDisposed(); return _glyphCount; } set { ThrowIfDisposed(); _glyphCount = value; } }
    /// <summary>Gets or sets descriptive grapheme flags.</summary><value>None initially; writes do not change drawing.</value>
    public TextGraphemeFlags GlyphFlags { get { ThrowIfDisposed(); return _glyphFlags; } set { ThrowIfDisposed(); _glyphFlags = value; } }
    /// <summary>Gets or sets the actual font-local glyph index.</summary><value>Zero initially.</value>
    public int GlyphIndex { get { ThrowIfDisposed(); return _glyphIndex; } set { ThrowIfDisposed(); if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)); _glyphIndex = value; } }
    /// <summary>Gets or sets finite pixel translation.</summary><value>Zero initially.</value>
    public Vector2 Offset { get { ThrowIfDisposed(); return _offset; } set { ThrowIfDisposed(); if (!value.IsFinite()) throw new ArgumentException("Offset must be finite.", nameof(value)); _offset = value; } }
    /// <summary>Gets or sets descriptive outline-pass state.</summary><value>False initially; writes do not change drawing.</value>
    public bool Outline { get { ThrowIfDisposed(); return _outline; } set { ThrowIfDisposed(); _outline = value; } }
    /// <summary>Gets or sets the descriptive absolute scalar interval.</summary><value>Zero initially; writes do not change drawing.</value>
    public Vector2i Range { get { ThrowIfDisposed(); return _range; } set { ThrowIfDisposed(); _range = value; } }
    /// <summary>Gets or sets the descriptive scalar offset into the effect block.</summary><value>Zero initially; writes do not change drawing.</value>
    public int RelativeIndex { get { ThrowIfDisposed(); return _relativeIndex; } set { ThrowIfDisposed(); _relativeIndex = value; } }
    /// <summary>Gets or sets the finite glyph transform.</summary><value>Identity initially.</value>
    public Transform Transform { get { ThrowIfDisposed(); return _transform; } set { ThrowIfDisposed(); if (!value.IsFinite()) throw new ArgumentException("Transform must be finite.", nameof(value)); _transform = value; } }
    /// <summary>Gets or sets whether this glyph is drawn and occupies visual advance.</summary><value>True initially.</value>
    public bool Visible { get { ThrowIfDisposed(); return _visible; } set { ThrowIfDisposed(); _visible = value; } }
    /// <summary>Finds a replacement scalar's glyph index in the actual borrowed face of this callback.</summary><param name="character">Valid Unicode scalar.</param><returns>Font-local index or zero if missing/unbound.</returns>
    public int GetGlyphIndex(int character) { ThrowIfDisposed(); if (!System.Text.Rune.IsValid(character)) throw new ArgumentOutOfRangeException(nameof(character)); return checked((int)(GlyphFace?.GetGlyphIndex((uint)character) ?? 0)); }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var descriptor in base.GetPropertyDescriptors()) yield return descriptor;
        yield return new PropertyDescriptor<CharFXTransform, Color>(nameof(Color), s => s.Color, (s, v) => s.Color = v, _ => Colors.Black);
        yield return new PropertyDescriptor<CharFXTransform, double>(nameof(ElapsedTime), s => s.ElapsedTime, (s, v) => s.ElapsedTime = v, _ => 0);
        yield return new PropertyDescriptor<CharFXTransform, RichTextEffectEnvironment>(nameof(Env), s => s.Env, (s, v) => s.Env = v, _ => new());
        yield return new PropertyDescriptor<CharFXTransform, Font?>(nameof(Font), s => s.Font, (s, v) => s.Font = v, _ => null);
        yield return new PropertyDescriptor<CharFXTransform, int>(nameof(GlyphCount), s => s.GlyphCount, (s, v) => s.GlyphCount = v, _ => 0);
        yield return new PropertyDescriptor<CharFXTransform, TextGraphemeFlags>(nameof(GlyphFlags), s => s.GlyphFlags, (s, v) => s.GlyphFlags = v, _ => 0);
        yield return new PropertyDescriptor<CharFXTransform, int>(nameof(GlyphIndex), s => s.GlyphIndex, (s, v) => s.GlyphIndex = v, _ => 0);
        yield return new PropertyDescriptor<CharFXTransform, Vector2>(nameof(Offset), s => s.Offset, (s, v) => s.Offset = v, _ => Vector2.Zero);
        yield return new PropertyDescriptor<CharFXTransform, bool>(nameof(Outline), s => s.Outline, (s, v) => s.Outline = v, _ => false);
        yield return new PropertyDescriptor<CharFXTransform, Vector2i>(nameof(Range), s => s.Range, (s, v) => s.Range = v, _ => Vector2i.Zero);
        yield return new PropertyDescriptor<CharFXTransform, int>(nameof(RelativeIndex), s => s.RelativeIndex, (s, v) => s.RelativeIndex = v, _ => 0);
        yield return new PropertyDescriptor<CharFXTransform, Transform>(nameof(Transform), s => s.Transform, (s, v) => s.Transform = v, _ => Transform.Identity);
        yield return new PropertyDescriptor<CharFXTransform, bool>(nameof(Visible), s => s.Visible, (s, v) => s.Visible = v, _ => true);
    }
}
