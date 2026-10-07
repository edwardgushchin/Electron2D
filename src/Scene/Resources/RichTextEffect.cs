namespace Electron2D;

/// <summary>Transforms borrowed glyph state during rich-text recording through a typed C# hook.</summary>
/// <remarks>Labels borrow effects. A custom subtype implements the hook; BBCode names identify installed
/// effects without per-glyph source lookup; compiled Script assets can construct exact typed effect subclasses. Processing takes place on the label's owner thread.</remarks>
public class RichTextEffect : Resource
{
    private string _bbcode = "";
    /// <summary>Creates an unnamed effect whose default hook leaves text untransformed.</summary>
    public RichTextEffect() { }
    /// <summary>Gets or sets this effect's tag identifier.</summary><value>Empty initially; unnamed effects can still be pushed directly.</value>
    public string BBCode { get { ThrowIfDisposed(); return _bbcode; } set { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(value); if (_bbcode == value) return; _bbcode = value; EmitChanged(); } }
    /// <summary>Transforms the current glyph state.</summary><param name="charFX">Borrowed state, reset for subsequent glyphs.</param><returns>True when this transform succeeds; false stops later custom effects for that glyph.</returns>
    protected virtual bool OnProcessCustomFX(CharFXTransform charFX) => false;
    internal bool ProcessEffect(CharFXTransform state) { ThrowIfDisposed(); return OnProcessCustomFX(state); }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => GetType() == typeof(RichTextEffect) ? new RichTextEffect() : base.CreateDuplicateInstance();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicate, Func<Resource?, Resource?> force) { ((RichTextEffect)target)._bbcode = _bbcode; }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Append(new PropertyDescriptor<RichTextEffect, string>(nameof(BBCode), e => e.BBCode, (e, v) => e.BBCode = v, _ => "", stored: true));
}
