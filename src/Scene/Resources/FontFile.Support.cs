namespace Electron2D;

public partial class FontFile
{
    internal bool SupportsSimpleScript(string script)
    { FontSupportPolicy.Validate(script); lock (FontGate) { ThrowIfDisposed(); return _fontData!.SupportPolicy.SupportsSimpleScript(script); } }
    private static readonly PropertyDescriptor SupportProperty = new PropertyDescriptor<FontFile, byte[]>("_font_support", font => font.Read(static data => data.SupportPolicy.Save()), (font, bytes) => font.SetSupportPolicy(FontSupportPolicy.Load(bytes)), _ => [], stored: true);
    /// <summary>Overrides language support used to rank fallback faces.</summary>
    /// <param name="language">An exact ordinal language key or * wildcard; empty keys are preserved.</param>
    /// <param name="supported">True selects the script-supported priority; false lowers language priority.</param>
    /// <remarks>The primary face remains first. This preference never creates glyphs or forbids a last-resort face.
    /// Equal writes are ignored; committed changes notify layouts after releasing storage locks.</remarks>
    /// <exception cref="ArgumentException">The key contains NUL, invalid UTF16 or exceeds 65536 units.</exception>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public void SetLanguageSupportOverride(string language, bool supported) => ChangeSupport(false, language, supported);
    /// <summary>Gets an explicit language override without creating a missing entry.</summary>
    /// <param name="language">The exact ordinal stored key.</param>
    /// <returns>True for an explicit true value; false for false or absent keys.</returns>
    /// <exception cref="ArgumentException">The key contains NUL, invalid UTF16 or exceeds 65536 units.</exception>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public bool GetLanguageSupportOverride(string language) { FontSupportPolicy.Validate(language); return Read(data => data.SupportPolicy.Get(false, language)); }
    /// <summary>Removes a language override and restores automatic/wildcard ranking.</summary>
    /// <param name="language">The exact ordinal stored key; missing keys are ignored.</param>
    /// <exception cref="ArgumentException">The key contains NUL, invalid UTF16 or exceeds 65536 units.</exception>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public void RemoveLanguageSupportOverride(string language) => ChangeSupport(false, language, null);
    /// <summary>Copies every explicit language key, including false values, in insertion order.</summary>
    /// <returns>An independent array of exact stored keys.</returns>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public string[] GetLanguageSupportOverrides() => Read(static data => data.SupportPolicy.Keys(false));
    /// <summary>Overrides writing-system support used to rank fallback faces.</summary>
    /// <param name="script">An exact ISO15924 tag or * wildcard; other stored keys remain queryable.</param>
    /// <param name="supported">True gives script-supported priority; false moves the face to the last tier.</param>
    /// <remarks>The primary face remains first; unsupported fallback faces remain available as last resort.</remarks>
    /// <exception cref="ArgumentException">The key contains NUL, invalid UTF16 or exceeds 65536 units.</exception>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public void SetScriptSupportOverride(string script, bool supported) => ChangeSupport(true, script, supported);
    /// <summary>Gets an explicit script override without creating a missing entry.</summary>
    /// <param name="script">The exact ordinal stored key.</param>
    /// <returns>True for an explicit true value; false for false or absent keys.</returns>
    /// <exception cref="ArgumentException">The key contains NUL, invalid UTF16 or exceeds 65536 units.</exception>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public bool GetScriptSupportOverride(string script) { FontSupportPolicy.Validate(script); return Read(data => data.SupportPolicy.Get(true, script)); }
    /// <summary>Removes a script override and restores automatic/wildcard ranking.</summary>
    /// <param name="script">The exact ordinal stored key; missing keys are ignored.</param>
    /// <exception cref="ArgumentException">The key contains NUL, invalid UTF16 or exceeds 65536 units.</exception>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public void RemoveScriptSupportOverride(string script) => ChangeSupport(true, script, null);
    /// <summary>Copies every explicit script key, including false values, in insertion order.</summary>
    /// <returns>An independent array of exact stored keys.</returns>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public string[] GetScriptSupportOverrides() => Read(static data => data.SupportPolicy.Keys(true));
    private void ChangeSupport(bool script, string key, bool? value)
    {
        FontSupportPolicy.Validate(key);
        lock (FontGate) { ThrowIfDisposed(); var policy = _fontData!.SupportPolicy.With(script, key, value); if (ReferenceEquals(policy, _fontData.SupportPolicy)) return; ApplySupportLocked(policy); }
        EmitFontChanged();
    }
    private void SetSupportPolicy(FontSupportPolicy policy)
    {
        lock (FontGate) { ThrowIfDisposed(); ApplySupportLocked(policy); }
        EmitFontChanged();
    }
    private void ApplySupportLocked(FontSupportPolicy policy)
    {
        lock (_fontData!.Gate) _fontData.SupportPolicy = policy;
        foreach (var data in _cacheData.Values) lock (data.Gate) data.SupportPolicy = policy;
        InvalidateFontStateLocked();
    }
}
