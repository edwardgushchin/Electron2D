namespace Electron2D;

/// <summary>Owns font sources and caches, retained shaped-text buffers and shared text utilities.</summary>
/// <remarks>Obtain the retained interface from TextServerManager. Each buffer belongs to its creating thread.
/// Created fonts and buffers are retained until FreeRID; font projections are borrowed live views.
/// Caller fonts, text-object buffers, inline keys and metadata remain borrowed. Advanced and Fallback own operational
/// resources; the explicit Dummy profile creates no identities and ignores resource requests. Capabilities govern
/// contextual layout, boundaries and case behavior; consult each concrete implementation before requesting them.</remarks>
public abstract partial class TextServer : ElectronObject
{
    private readonly object _buffersGate = new();
    private readonly Dictionary<RID, TextLine> _owned = [];
    private protected TextServer() { }
    internal sealed class Builtin : TextServer { protected override string OnGetName() => "FreeType/HarfBuzz"; }

    /// <summary>Returns the stable name of this text implementation.</summary><returns>The implementation name.</returns>
    public string GetName()
    {
        var implementationCall = EnterImplementation();
        Exception? callbackFailure = null;
        try
        {
            return OnGetName();
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }


    /// <summary>Returns the stable name of this text implementation.</summary><returns>The implementation name.</returns>
    protected abstract string OnGetName();
    /// <summary>Creates an empty owned text buffer.</summary><param name="direction">Base writing direction.</param><param name="orientation">Advance axis.</param><returns>A caller-owned identity; release with FreeRID.</returns>
    public RID CreateShapedText(TextDirection direction = TextDirection.Auto, TextOrientation orientation = TextOrientation.Horizontal)
    {
        var implementationCall = EnterImplementation();
        Exception? callbackFailure = null;
        try
        {
            if (!Enum.IsDefined(direction) || direction == TextDirection.Inherited) throw new ArgumentOutOfRangeException(nameof(direction));
            if (!Enum.IsDefined(orientation)) throw new ArgumentOutOfRangeException(nameof(orientation));
            return CreateOwnedRID(false, () => OnCreateShapedText(direction, orientation));
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Creates an empty owned text buffer.</summary><param name="direction">Base writing direction.</param><param name="orientation">Advance axis.</param><returns>A caller-owned identity; release with FreeRID.</returns>
    protected virtual RID OnCreateShapedText(TextDirection direction = TextDirection.Auto, TextOrientation orientation = TextOrientation.Horizontal)
    {
        using var nativeTextScope = EnterNativeBackend();
        if (!Enum.IsDefined(direction) || direction == TextDirection.Inherited) throw new ArgumentOutOfRangeException(nameof(direction));
        if (!Enum.IsDefined(orientation)) throw new ArgumentOutOfRangeException(nameof(orientation));
        var line = new TextLine { Direction = direction, Orientation = orientation, TextOverrunBehavior = TextOverrunBehavior.NoTrimming };
        return Own(line);
    }
    private RID Own(TextLine line)
    {
        try { CheckFactoryAllocation(); line.ServerInterfaceOwner = this; line.ServerSimpleLayout = this is TextServerFallback; line.ServerNativeBackend = IsNativeBackendCall; if (TextServerManager.IsCreatingConsumerBuffer(this)) line.BindConsumerOwner(this); var rid = line.GetRID(); lock (_buffersGate) _owned.Add(rid, line); RecordAllocation(rid, false); return rid; }
        catch { line.Dispose(); throw; }
    }
    /// <summary>Reports whether an identity resolves to a live font entry or shaped buffer.</summary><param name="rid">Font or buffer identity.</param><returns>False for empty, retired or foreign identities. A surviving link remains present after its source retires.</returns>
    public bool Has(RID rid)
    {
        var implementationCall = EnterImplementation();
        Exception? callbackFailure = null;
        try
        {
            return OnHas(rid);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Reports whether an identity resolves to a live font entry or shaped buffer.</summary><param name="rid">Font or buffer identity.</param><returns>False for empty, retired or foreign identities. A surviving link remains present after its source retires.</returns>
    protected virtual bool OnHas(RID rid) { using var nativeTextScope = EnterNativeBackend(); return HasExternalRID(rid) || HasNativeFont(rid) || FontFile.TryResolveSource(rid, out _) || TextBuffers.TryResolve(rid, out _); }
    /// <summary>Releases an owned font or buffer, preserving borrowed text-object lifetimes.</summary><param name="rid">Identity returned by this interface's creation, duplication or substring operations.</param>
    /// <exception cref="InvalidOperationException">The identity is borrowed, unknown, off-owner or active.</exception>
    public void FreeRID(RID rid)
    {
        var implementationCall = EnterImplementation();
        Exception? callbackFailure = null;
        try
        {
            if (this is TextServerDummy) return;
            var external = BeginExternalRetirement(rid);
            TextLine? buffer = null; ServerFont? font = null;
            if (external is null)
            {
                lock (_buffersGate) _owned.TryGetValue(rid, out buffer);
                if (buffer is not null) buffer.BeginServerRetirement();
                else font = BeginOwnedFontRetirement(rid);
            }
            try { FinishFreeRID(rid, external); }
            finally { buffer?.EndServerRetirement(); if (font is not null) lock (NativeFontsGate) font.Retiring = false; }
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }
    private void FinishFreeRID(RID rid, ExternalEntry? external)
    {
        Exception? failure = null; var dispatcher = this;
        if (external is null) lock (_buffersGate) if (_owned.TryGetValue(rid, out var buffer) && !buffer.ServerNativeBackend) dispatcher = TextServerManager.NativeInterface;
        try { dispatcher.OnFreeRID(rid); } catch (Exception error) { failure = error; }
        try
        {
            if (external is not null) CompleteExternalRetirement(rid);
            else ReleaseNativeRID(rid);
        }
        catch (Exception error) { failure = failure is null ? error : new AggregateException(failure, error); }
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }
    private void ReleaseNativeRID(RID rid)
    {
        if (FreeFont(rid)) return;
        TextLine? buffer; lock (_buffersGate) _owned.TryGetValue(rid, out buffer);
        if (buffer is null) return;
        try { buffer.Dispose(); if (!buffer.IsDisposed) throw new InvalidOperationException("Text buffer retirement was canceled."); }
        finally { if (buffer.IsDisposalCommitted || buffer.IsDisposed) lock (_buffersGate) _owned.Remove(rid); }
    }
    internal bool IsOwnedRID(RID rid)
    {
        lock (_implementationGate) if (_external.ContainsKey(rid)) return true;
        lock (_fontsGate) if (_fonts.ContainsKey(rid)) return true;
        lock (_buffersGate) return _owned.ContainsKey(rid);
    }

    /// <summary>Releases an owned font or buffer, preserving borrowed text-object lifetimes.</summary><param name="rid">Identity returned by this interface's creation, duplication or substring operations.</param>
    /// <exception cref="InvalidOperationException">The identity is borrowed, unknown, off-owner or active.</exception>
    protected virtual void OnFreeRID(RID rid) { }
    /// <summary>Copies a complete buffer into an independent owned identity.</summary><param name="shaped">Live source identity.</param><returns>A new identity sharing borrowed fonts, keys and metadata values.</returns>
    public RID ShapedTextDuplicate(RID shaped)
    {
        var implementationCall = EnterImplementation(shaped, false);
        Exception? callbackFailure = null;
        try
        {
            var dispatcher = implementationCall.Dispatcher; return CreateOwnedRID(false, () => AdoptBuffer(dispatcher, dispatcher.OnShapedTextDuplicate(shaped)));
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Copies a complete buffer into an independent owned identity.</summary><param name="shaped">Live source identity.</param><returns>A new identity sharing borrowed fonts, keys and metadata values.</returns>
    protected virtual RID OnShapedTextDuplicate(RID shaped)
    {
        using var nativeTextScope = EnterNativeBackend(shaped);
        var v = View(shaped);
        return Own(v.Start == v.Offset && v.End == v.Offset + v.Source.ScalarCount ? v.Source.ServerDuplicate() : v.Source.ServerSlice(v.Start - v.Offset, v.End - v.Offset));
    }
    /// <summary>Appends copied text with ordered borrowed fonts and copied feature overrides.</summary>
    /// <param name="shaped">Live buffer.</param><param name="text">Copied source; empty succeeds without a new span.</param><param name="fonts">One to sixty-four live ordered font resources.</param><param name="size">Positive logical font size.</param><param name="openTypeFeatures">Copied named or four-character OpenType selectors.</param><param name="language">Language tag.</param><returns>True after successful authoring.</returns>
    public bool ShapedTextAddString(RID shaped, string text, ReadOnlySpan<Font> fonts, int size, IReadOnlyDictionary<string, int>? openTypeFeatures = null, string language = "")
    {
        var implementationCall = EnterImplementation(shaped, false, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnShapedTextAddString(shaped, text, fonts, size, openTypeFeatures, language);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Appends copied text with ordered borrowed fonts and copied feature overrides.</summary>
    /// <param name="shaped">Live buffer.</param><param name="text">Copied source; empty succeeds without a new span.</param><param name="fonts">One to sixty-four live ordered font resources.</param><param name="size">Positive logical font size.</param><param name="openTypeFeatures">Copied named or four-character OpenType selectors.</param><param name="language">Language tag.</param><returns>True after successful authoring.</returns>
    protected virtual bool OnShapedTextAddString(RID shaped, string text, ReadOnlySpan<Font> fonts, int size, IReadOnlyDictionary<string, int>? openTypeFeatures = null, string language = "")
    { using var nativeTextScope = EnterNativeBackend(shaped); using var mutation = Mutation(shaped); var result = mutation.Buffer.ServerAdd(text, fonts, size, openTypeFeatures, language, null); if (text.Length != 0) mutation.Commit(); return result; }
    /// <summary>Appends text retaining metadata under its exact supplied type.</summary><typeparam name="T">Metadata type.</typeparam>
    /// <param name="shaped">Buffer identity.</param><param name="text">Copied source.</param><param name="fonts">Ordered borrowed fonts.</param><param name="size">Logical font size.</param><param name="openTypeFeatures">Copied feature overrides.</param><param name="language">Language tag.</param><param name="metadata">Borrowed typed value.</param><returns>True after successful authoring.</returns>
    public bool ShapedTextAddString<T>(RID shaped, string text, ReadOnlySpan<Font> fonts, int size, IReadOnlyDictionary<string, int>? openTypeFeatures, string language, T metadata)
    {
        var implementationCall = EnterImplementation(shaped, false, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnShapedTextAddString<T>(shaped, text, fonts, size, openTypeFeatures, language, metadata);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Appends text retaining metadata under its exact supplied type.</summary><typeparam name="T">Metadata type.</typeparam>
    /// <param name="shaped">Buffer identity.</param><param name="text">Copied source.</param><param name="fonts">Ordered borrowed fonts.</param><param name="size">Logical font size.</param><param name="openTypeFeatures">Copied feature overrides.</param><param name="language">Language tag.</param><param name="metadata">Borrowed typed value.</param><returns>True after successful authoring.</returns>
    protected virtual bool OnShapedTextAddString<T>(RID shaped, string text, ReadOnlySpan<Font> fonts, int size, IReadOnlyDictionary<string, int>? openTypeFeatures, string language, T metadata)
    { using var nativeTextScope = EnterNativeBackend(shaped); using var mutation = Mutation(shaped); var result = mutation.Buffer.ServerAdd(text, fonts, size, openTypeFeatures, language, RichTextMetadata.Create(metadata)); if (text.Length != 0) mutation.Commit(); return result; }
    /// <summary>Appends one identity-keyed inline object.</summary><param name="shaped">Buffer identity.</param><param name="key">Unique borrowed token.</param><param name="size">Nonnegative finite dimensions.</param><param name="inlineAlign">Inline alignment mask.</param><param name="length">Positive replacement scalar count.</param><param name="baseline">Finite signed baseline offset.</param><returns>False for a duplicate key.</returns>
    public bool ShapedTextAddObject(RID shaped, TextObjectKey key, Vector2 size, InlineAlignment inlineAlign = InlineAlignment.Center, int length = 1, float baseline = 0)
    {
        var implementationCall = EnterImplementation(shaped, false, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnShapedTextAddObject(shaped, key, size, inlineAlign, length, baseline);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Appends one identity-keyed inline object.</summary><param name="shaped">Buffer identity.</param><param name="key">Unique borrowed token.</param><param name="size">Nonnegative finite dimensions.</param><param name="inlineAlign">Inline alignment mask.</param><param name="length">Positive replacement scalar count.</param><param name="baseline">Finite signed baseline offset.</param><returns>False for a duplicate key.</returns>
    protected virtual bool OnShapedTextAddObject(RID shaped, TextObjectKey key, Vector2 size, InlineAlignment inlineAlign = InlineAlignment.Center, int length = 1, float baseline = 0)
    { using var nativeTextScope = EnterNativeBackend(shaped); using var mutation = Mutation(shaped); var b = mutation.Buffer; var result = b.AddObject(key, size, inlineAlign, length, baseline); if (result) { b.ServerChanged?.Invoke(); mutation.Commit(); } return result; }
    /// <summary>Updates an existing inline object.</summary><param name="shaped">Buffer identity.</param><param name="key">Borrowed token.</param><param name="size">Finite nonnegative dimensions.</param><param name="inlineAlign">Alignment mask.</param><param name="baseline">Finite signed baseline.</param><returns>False for a missing token.</returns>
    public bool ShapedTextResizeObject(RID shaped, TextObjectKey key, Vector2 size, InlineAlignment inlineAlign = InlineAlignment.Center, float baseline = 0)
    {
        var implementationCall = EnterImplementation(shaped, false, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnShapedTextResizeObject(shaped, key, size, inlineAlign, baseline);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Updates an existing inline object.</summary><param name="shaped">Buffer identity.</param><param name="key">Borrowed token.</param><param name="size">Finite nonnegative dimensions.</param><param name="inlineAlign">Alignment mask.</param><param name="baseline">Finite signed baseline.</param><returns>False for a missing token.</returns>
    protected virtual bool OnShapedTextResizeObject(RID shaped, TextObjectKey key, Vector2 size, InlineAlignment inlineAlign = InlineAlignment.Center, float baseline = 0)
    { using var nativeTextScope = EnterNativeBackend(shaped); using var mutation = Mutation(shaped); var b = mutation.Buffer; var result = b.ResizeObject(key, size, inlineAlign, baseline); if (result) { b.ServerChanged?.Invoke(); mutation.Commit(); } return result; }
    /// <summary>Clears authored text, objects and BIDI overrides while retaining policies and identity.</summary><param name="shaped">Live buffer.</param>
    public void ShapedTextClear(RID shaped)
    {
        var implementationCall = EnterImplementation(shaped, false, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            implementationCall.Dispatcher.OnShapedTextClear(shaped);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Clears authored text, objects and BIDI overrides while retaining policies and identity.</summary><param name="shaped">Live buffer.</param>
    protected virtual void OnShapedTextClear(RID shaped) { using var nativeTextScope = EnterNativeBackend(shaped); using var mutation = Mutation(shaped); var b = mutation.Buffer; b.Clear(); b.ServerOffset = 0; b.ServerPrefix = b.ServerSuffix = default; b.ServerChanged?.Invoke(); mutation.Commit(); }
    /// <summary>Prepares the real native layout and metrics.</summary><param name="shaped">Buffer identity.</param><returns>True on successful preparation; errors throw.</returns>
    public bool ShapedTextShape(RID shaped)
    {
        var implementationCall = EnterImplementation(shaped, false, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnShapedTextShape(shaped);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Prepares the real native layout and metrics.</summary><param name="shaped">Buffer identity.</param><returns>True on successful preparation; errors throw.</returns>
    protected virtual bool OnShapedTextShape(RID shaped) { using var nativeTextScope = EnterNativeBackend(shaped); _ = View(shaped); return true; }
    /// <summary>Reports whether a prepared buffer remains current without forcing a rebuild.</summary><param name="shaped">Buffer identity.</param><returns>Whether the authored content and borrowed font generations match.</returns>
    public bool ShapedTextIsReady(RID shaped)
    {
        var implementationCall = EnterImplementation(shaped, false);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnShapedTextIsReady(shaped);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Reports whether a prepared buffer remains current without forcing a rebuild.</summary><param name="shaped">Buffer identity.</param><returns>Whether the authored content and borrowed font generations match.</returns>
    protected virtual bool OnShapedTextIsReady(RID shaped)
    {
        using var nativeTextScope = EnterNativeBackend(shaped);
        var owner = TextBuffers.Resolve(shaped);
        if (owner.Owner is TextParagraph paragraph) return paragraph.ServerIsReady(owner.Line, shaped);
        var source = (TextLine)owner.Owner; source.ServerAccess(); return source.ServerReady;
    }
    /// <summary>Returns complete underlying source text, including replacement scalars and the surrounding source of a line view.</summary><param name="shaped">Buffer identity.</param><returns>An immutable source snapshot.</returns>
    public string ShapedGetText(RID shaped)
    {
        var implementationCall = EnterImplementation(shaped, false);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnShapedGetText(shaped);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Returns complete underlying source text, including replacement scalars and the surrounding source of a line view.</summary><param name="shaped">Buffer identity.</param><returns>An immutable source snapshot.</returns>
    protected virtual string OnShapedGetText(RID shaped) { using var nativeTextScope = EnterNativeBackend(shaped); var v = AuthoringView(shaped); return v.Source.ServerFullText; }
    /// <summary>Returns the number of source text and inline-object spans intersecting this buffer.</summary><param name="shaped">Buffer identity.</param><returns>Authored span count.</returns>
    public int ShapedGetSpanCount(RID shaped)
    {
        var implementationCall = EnterImplementation(shaped, false);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnShapedGetSpanCount(shaped);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Returns the number of source text and inline-object spans intersecting this buffer.</summary><param name="shaped">Buffer identity.</param><returns>Authored span count.</returns>
    protected virtual int OnShapedGetSpanCount(RID shaped)
    { using var nativeTextScope = EnterNativeBackend(shaped); var v = AuthoringView(shaped); return v.Source.ServerSpanCountInRange(v.Start - v.Offset, v.End - v.Offset); }
    private static (BufferView View, int At, int End, TextObjectKey? Key, RichTextMetadata? Metadata) Span(RID shaped, int index)
    {
        var v = AuthoringView(shaped); var span = v.Source.ServerSpan(index, v.Start - v.Offset, v.End - v.Offset);
        return (v, span.At, span.At + span.Length, span.Key, span.Metadata);
    }
    /// <summary>Returns the source text of an authored span.</summary><param name="shaped">Buffer identity.</param><param name="index">Text-or-object span index.</param><returns>The source substring, including object replacement scalars.</returns>
    public string ShapedGetSpanText(RID shaped, int index)
    {
        var implementationCall = EnterImplementation(shaped, false);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnShapedGetSpanText(shaped, index);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Returns the source text of an authored span.</summary><param name="shaped">Buffer identity.</param><param name="index">Text-or-object span index.</param><returns>The source substring, including object replacement scalars.</returns>
    protected virtual string OnShapedGetSpanText(RID shaped, int index) { using var nativeTextScope = EnterNativeBackend(shaped); var s = Span(shaped, index); return s.View.Source.ServerSliceText(s.At, s.End); }
    /// <summary>Returns a span's inline-object key.</summary><param name="shaped">Buffer identity.</param><param name="index">Text-or-object span index.</param><returns>The borrowed token, or null for a text span.</returns>
    public TextObjectKey? ShapedGetSpanObject(RID shaped, int index)
    {
        var implementationCall = EnterImplementation(shaped, false);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnShapedGetSpanObject(shaped, index);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Returns a span's inline-object key.</summary><param name="shaped">Buffer identity.</param><param name="index">Text-or-object span index.</param><returns>The borrowed token, or null for a text span.</returns>
    protected virtual TextObjectKey? OnShapedGetSpanObject(RID shaped, int index) { using var nativeTextScope = EnterNativeBackend(shaped); return Span(shaped, index).Key; }
    /// <summary>Returns a span's embedded inline-object key.</summary><param name="shaped">Buffer identity.</param><param name="index">Text-or-object span index.</param><returns>The borrowed token, or null for a text span.</returns>
    public TextObjectKey? ShapedGetSpanEmbeddedObject(RID shaped, int index)
    {
        var implementationCall = EnterImplementation(shaped, false);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnShapedGetSpanEmbeddedObject(shaped, index);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Returns a span's embedded inline-object key.</summary><param name="shaped">Buffer identity.</param><param name="index">Text-or-object span index.</param><returns>The borrowed token, or null for a text span.</returns>
    protected virtual TextObjectKey? OnShapedGetSpanEmbeddedObject(RID shaped, int index) { using var nativeTextScope = EnterNativeBackend(shaped); return Span(shaped, index).Key; }
    /// <summary>Reads span metadata under its exact original type.</summary><typeparam name="T">Original metadata type.</typeparam><param name="shaped">Buffer identity.</param><param name="index">Text-or-object span index.</param><param name="metadata">Borrowed value, or default.</param><returns>Whether the exact type matches.</returns>
    public bool TryGetSpanMetadata<T>(RID shaped, int index, out T metadata)
    {
        var implementationCall = EnterImplementation(shaped, false);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnTryGetSpanMetadata<T>(shaped, index, out metadata);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Reads span metadata under its exact original type.</summary><typeparam name="T">Original metadata type.</typeparam><param name="shaped">Buffer identity.</param><param name="index">Text-or-object span index.</param><param name="metadata">Borrowed value, or default.</param><returns>Whether the exact type matches.</returns>
    protected virtual bool OnTryGetSpanMetadata<T>(RID shaped, int index, out T metadata)
    { using var nativeTextScope = EnterNativeBackend(shaped); var span = Span(shaped, index); if (span.Metadata is { } value) return value.TryGet(out metadata); metadata = default!; return false; }
    /// <summary>Returns the buffer's half-open Unicode scalar range.</summary><param name="shaped">Buffer identity.</param><returns>Start and exclusive end.</returns>
    public Vector2i ShapedTextGetRange(RID shaped)
    {
        var implementationCall = EnterImplementation(shaped, false);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnShapedTextGetRange(shaped);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Returns the buffer's half-open Unicode scalar range.</summary><param name="shaped">Buffer identity.</param><returns>Start and exclusive end.</returns>
    protected virtual Vector2i OnShapedTextGetRange(RID shaped) { using var nativeTextScope = EnterNativeBackend(shaped); var v = AuthoringView(shaped); return new(v.Start, v.End); }
    /// <summary>Returns the number of glyph records in visual order.</summary><param name="shaped">Buffer identity.</param><returns>The prepared glyph count.</returns>
    public int ShapedTextGetGlyphCount(RID shaped)
    {
        var implementationCall = EnterImplementation(shaped, false);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnShapedTextGetGlyphCount(shaped);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Returns the number of glyph records in visual order.</summary><param name="shaped">Buffer identity.</param><returns>The prepared glyph count.</returns>
    protected virtual int OnShapedTextGetGlyphCount(RID shaped) { using var nativeTextScope = EnterNativeBackend(shaped); return View(shaped).Metrics.GlyphCount; }
    /// <summary>Returns the ceiled advance-axis width.</summary><param name="shaped">Buffer identity.</param><returns>Logical pixels.</returns>
    public float ShapedTextGetWidth(RID shaped)
    {
        var implementationCall = EnterImplementation(shaped, false);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnShapedTextGetWidth(shaped);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Returns the ceiled advance-axis width.</summary><param name="shaped">Buffer identity.</param><returns>Logical pixels.</returns>
    protected virtual float OnShapedTextGetWidth(RID shaped) { using var nativeTextScope = EnterNativeBackend(shaped); return MathF.Ceiling(View(shaped).Advance); }
    /// <summary>Returns fractional ascent above the baseline.</summary><param name="shaped">Buffer identity.</param><returns>Logical pixels.</returns>
    public float ShapedTextGetAscent(RID shaped)
    {
        var implementationCall = EnterImplementation(shaped, false);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnShapedTextGetAscent(shaped);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Returns fractional ascent above the baseline.</summary><param name="shaped">Buffer identity.</param><returns>Logical pixels.</returns>
    protected virtual float OnShapedTextGetAscent(RID shaped) { using var nativeTextScope = EnterNativeBackend(shaped); return View(shaped).Metrics.Ascent; }
    /// <summary>Returns fractional descent below the baseline.</summary><param name="shaped">Buffer identity.</param><returns>Logical pixels.</returns>
    public float ShapedTextGetDescent(RID shaped)
    {
        var implementationCall = EnterImplementation(shaped, false);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnShapedTextGetDescent(shaped);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Returns fractional descent below the baseline.</summary><param name="shaped">Buffer identity.</param><returns>Logical pixels.</returns>
    protected virtual float OnShapedTextGetDescent(RID shaped) { using var nativeTextScope = EnterNativeBackend(shaped); return View(shaped).Metrics.Descent; }
    /// <summary>Returns ceiled advance and cross-axis dimensions.</summary><param name="shaped">Buffer identity.</param><returns>Dimensions in the buffer's orientation.</returns>
    public Vector2 ShapedTextGetSize(RID shaped)
    {
        var implementationCall = EnterImplementation(shaped, false);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnShapedTextGetSize(shaped);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Returns ceiled advance and cross-axis dimensions.</summary><param name="shaped">Buffer identity.</param><returns>Dimensions in the buffer's orientation.</returns>
    protected virtual Vector2 OnShapedTextGetSize(RID shaped) { using var nativeTextScope = EnterNativeBackend(shaped); var v = View(shaped); var m = v.Metrics; return (v.Source.Orientation == TextOrientation.Horizontal ? new Vector2(v.Advance, m.Height) : new Vector2(m.Height, v.Advance)).Ceil(); }
    /// <summary>Returns underline offset for the buffer's authored fonts.</summary><param name="shaped">Buffer identity.</param><returns>Logical pixels below the baseline.</returns>
    public float ShapedTextGetUnderlinePosition(RID shaped)
    {
        var implementationCall = EnterImplementation(shaped, false);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnShapedTextGetUnderlinePosition(shaped);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Returns underline offset for the buffer's authored fonts.</summary><param name="shaped">Buffer identity.</param><returns>Logical pixels below the baseline.</returns>
    protected virtual float OnShapedTextGetUnderlinePosition(RID shaped) { using var nativeTextScope = EnterNativeBackend(shaped); var v = View(shaped); return v.Layout.ServerUnderline(v.Line, false); }
    /// <summary>Returns maximum authored underline thickness.</summary><param name="shaped">Buffer identity.</param><returns>Logical pixels.</returns>
    public float ShapedTextGetUnderlineThickness(RID shaped)
    {
        var implementationCall = EnterImplementation(shaped, false);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnShapedTextGetUnderlineThickness(shaped);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Returns maximum authored underline thickness.</summary><param name="shaped">Buffer identity.</param><returns>Logical pixels.</returns>
    protected virtual float OnShapedTextGetUnderlineThickness(RID shaped) { using var nativeTextScope = EnterNativeBackend(shaped); var v = View(shaped); return v.Layout.ServerUnderline(v.Line, true); }
    /// <summary>Returns the first paragraph's inferred direction.</summary><param name="shaped">Buffer identity.</param><returns>LTR or RTL.</returns>
    public TextDirection ShapedTextGetInferredDirection(RID shaped)
    {
        var implementationCall = EnterImplementation(shaped, false);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnShapedTextGetInferredDirection(shaped);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Returns the first paragraph's inferred direction.</summary><param name="shaped">Buffer identity.</param><returns>LTR or RTL.</returns>
    protected virtual TextDirection OnShapedTextGetInferredDirection(RID shaped) { using var nativeTextScope = EnterNativeBackend(shaped); return View(shaped).Metrics.ParagraphLevel == 1 ? TextDirection.RTL : TextDirection.LTR; }
    /// <summary>Returns the authored base direction.</summary><param name="shaped">Buffer identity.</param><returns>The direction policy.</returns>
    public TextDirection ShapedTextGetDirection(RID shaped)
    {
        var implementationCall = EnterImplementation(shaped, false);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnShapedTextGetDirection(shaped);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Returns the authored base direction.</summary><param name="shaped">Buffer identity.</param><returns>The direction policy.</returns>
    protected virtual TextDirection OnShapedTextGetDirection(RID shaped) { using var nativeTextScope = EnterNativeBackend(shaped); return ReadSource(shaped).Direction; }
    /// <summary>Replaces the base direction.</summary><param name="shaped">Buffer identity.</param><param name="direction">Defined direction.</param>
    public void ShapedTextSetDirection(RID shaped, TextDirection direction = TextDirection.Auto)
    {
        var implementationCall = EnterImplementation(shaped, false, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            implementationCall.Dispatcher.OnShapedTextSetDirection(shaped, direction);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Replaces the base direction.</summary><param name="shaped">Buffer identity.</param><param name="direction">Defined direction.</param>
    protected virtual void OnShapedTextSetDirection(RID shaped, TextDirection direction = TextDirection.Auto) { using var nativeTextScope = EnterNativeBackend(shaped); if (direction == TextDirection.Inherited) throw new ArgumentOutOfRangeException(nameof(direction)); if (ReadSource(shaped).Direction == direction) return; using var mutation = Mutation(shaped); var b = mutation.Buffer; b.Direction = direction; b.ServerChanged?.Invoke(); mutation.Commit(); }
    /// <summary>Returns the advance orientation.</summary><param name="shaped">Buffer identity.</param><returns>Horizontal or Vertical.</returns>
    public TextOrientation ShapedTextGetOrientation(RID shaped)
    {
        var implementationCall = EnterImplementation(shaped, false);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnShapedTextGetOrientation(shaped);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Returns the advance orientation.</summary><param name="shaped">Buffer identity.</param><returns>Horizontal or Vertical.</returns>
    protected virtual TextOrientation OnShapedTextGetOrientation(RID shaped) { using var nativeTextScope = EnterNativeBackend(shaped); return ReadSource(shaped).Orientation; }
    /// <summary>Replaces the advance orientation.</summary><param name="shaped">Buffer identity.</param><param name="orientation">Defined orientation.</param>
    public void ShapedTextSetOrientation(RID shaped, TextOrientation orientation = TextOrientation.Horizontal)
    {
        var implementationCall = EnterImplementation(shaped, false, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            implementationCall.Dispatcher.OnShapedTextSetOrientation(shaped, orientation);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Replaces the advance orientation.</summary><param name="shaped">Buffer identity.</param><param name="orientation">Defined orientation.</param>
    protected virtual void OnShapedTextSetOrientation(RID shaped, TextOrientation orientation = TextOrientation.Horizontal) { using var nativeTextScope = EnterNativeBackend(shaped); if (ReadSource(shaped).Orientation == orientation) return; using var mutation = Mutation(shaped); var b = mutation.Buffer; b.Orientation = orientation; b.ServerChanged?.Invoke(); mutation.Commit(); }
    /// <summary>Returns the missing-character display policy.</summary><param name="shaped">Buffer identity.</param><returns>Whether invalid characters display hexadecimal boxes.</returns>
    public bool ShapedTextGetPreserveInvalid(RID shaped)
    {
        var implementationCall = EnterImplementation(shaped, false);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnShapedTextGetPreserveInvalid(shaped);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Returns the missing-character display policy.</summary><param name="shaped">Buffer identity.</param><returns>Whether invalid characters display hexadecimal boxes.</returns>
    protected virtual bool OnShapedTextGetPreserveInvalid(RID shaped) { using var nativeTextScope = EnterNativeBackend(shaped); return ReadSource(shaped).PreserveInvalid; }
    /// <summary>Replaces the missing-character display policy.</summary><param name="shaped">Buffer identity.</param><param name="enabled">Whether to display missing characters.</param>
    public void ShapedTextSetPreserveInvalid(RID shaped, bool enabled)
    {
        var implementationCall = EnterImplementation(shaped, false, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            implementationCall.Dispatcher.OnShapedTextSetPreserveInvalid(shaped, enabled);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Replaces the missing-character display policy.</summary><param name="shaped">Buffer identity.</param><param name="enabled">Whether to display missing characters.</param>
    protected virtual void OnShapedTextSetPreserveInvalid(RID shaped, bool enabled) { using var nativeTextScope = EnterNativeBackend(shaped); if (ReadSource(shaped).PreserveInvalid == enabled) return; using var mutation = Mutation(shaped); var b = mutation.Buffer; b.PreserveInvalid = enabled; b.ServerChanged?.Invoke(); mutation.Commit(); }
    /// <summary>Returns the nonprinting-control display policy.</summary><param name="shaped">Buffer identity.</param><returns>The control display preference.</returns>
    public bool ShapedTextGetPreserveControl(RID shaped)
    {
        var implementationCall = EnterImplementation(shaped, false);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnShapedTextGetPreserveControl(shaped);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Returns the nonprinting-control display policy.</summary><param name="shaped">Buffer identity.</param><returns>The control display preference.</returns>
    protected virtual bool OnShapedTextGetPreserveControl(RID shaped) { using var nativeTextScope = EnterNativeBackend(shaped); return ReadSource(shaped).PreserveControl; }
    /// <summary>Replaces the nonprinting-control display policy.</summary><param name="shaped">Buffer identity.</param><param name="enabled">Whether controls display glyphs.</param>
    public void ShapedTextSetPreserveControl(RID shaped, bool enabled)
    {
        var implementationCall = EnterImplementation(shaped, false, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            implementationCall.Dispatcher.OnShapedTextSetPreserveControl(shaped, enabled);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Replaces the nonprinting-control display policy.</summary><param name="shaped">Buffer identity.</param><param name="enabled">Whether controls display glyphs.</param>
    protected virtual void OnShapedTextSetPreserveControl(RID shaped, bool enabled) { using var nativeTextScope = EnterNativeBackend(shaped); if (ReadSource(shaped).PreserveControl == enabled) return; using var mutation = Mutation(shaped); var b = mutation.Buffer; b.PreserveControl = enabled; b.ServerChanged?.Invoke(); mutation.Commit(); }
    /// <summary>Replaces contiguous scalar BIDI override ranges.</summary><param name="shaped">Buffer identity.</param><param name="ranges">Copied full-buffer contexts, or empty to clear.</param>
    public void ShapedTextSetBIDIOverride(RID shaped, ReadOnlySpan<TextBIDIRange> ranges)
    {
        var implementationCall = EnterImplementation(shaped, false, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            implementationCall.Dispatcher.OnShapedTextSetBIDIOverride(shaped, ranges);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Replaces contiguous scalar BIDI override ranges.</summary><param name="shaped">Buffer identity.</param><param name="ranges">Copied full-buffer contexts, or empty to clear.</param>
    protected virtual void OnShapedTextSetBIDIOverride(RID shaped, ReadOnlySpan<TextBIDIRange> ranges)
    {
        using var nativeTextScope = EnterNativeBackend(shaped);
        using var mutation = Mutation(shaped); var b = mutation.Buffer;
        if (b.ServerOffset == 0 || ranges.IsEmpty) b.ServerSetBIDIOverride(ranges);
        else { var copy = new TextBIDIRange[ranges.Length]; for (var i = 0; i < copy.Length; i++) copy[i] = new(checked(ranges[i].Start - b.ServerOffset), checked(ranges[i].End - b.ServerOffset), ranges[i].Direction); b.ServerSetBIDIOverride(copy); }
        b.ServerChanged?.Invoke(); mutation.Commit();
    }
    /// <summary>Aligns tabs to repeating positive interval lengths.</summary><param name="shaped">Buffer identity.</param><param name="tabStops">Copied finite intervals.</param><returns>The resulting ceiled width.</returns>
    public float ShapedTextTabAlign(RID shaped, ReadOnlySpan<float> tabStops)
    {
        var implementationCall = EnterImplementation(shaped, false, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnShapedTextTabAlign(shaped, tabStops);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Aligns tabs to repeating positive interval lengths.</summary><param name="shaped">Buffer identity.</param><param name="tabStops">Copied finite intervals.</param><returns>The resulting ceiled width.</returns>
    protected virtual float OnShapedTextTabAlign(RID shaped, ReadOnlySpan<float> tabStops) { using var nativeTextScope = EnterNativeBackend(shaped); using var mutation = Mutation(shaped); var b = mutation.Buffer; b.TabAlign(tabStops); b.ServerChanged?.Invoke(); mutation.Commit(); return ShapedTextGetWidth(shaped); }
    /// <summary>Tests for an inline token in the buffer.</summary><param name="shaped">Buffer identity.</param><param name="key">Borrowed token.</param><returns>Whether present.</returns>
    public bool ShapedTextHasObject(RID shaped, TextObjectKey key)
    {
        var implementationCall = EnterImplementation(shaped, false);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnShapedTextHasObject(shaped, key);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Tests for an inline token in the buffer.</summary><param name="shaped">Buffer identity.</param><param name="key">Borrowed token.</param><returns>Whether present.</returns>
    protected virtual bool OnShapedTextHasObject(RID shaped, TextObjectKey key)
    { using var nativeTextScope = EnterNativeBackend(shaped); var v = AuthoringView(shaped); var range = v.Source.ServerObjectRange(key); return range.X >= 0 && range.X + v.Offset >= v.Start && range.X + v.Offset < v.End; }
    /// <summary>Returns inline tokens in source order.</summary><param name="shaped">Buffer identity.</param><returns>An independent array of borrowed tokens.</returns>
    public TextObjectKey[] ShapedTextGetObjects(RID shaped)
    {
        var implementationCall = EnterImplementation(shaped, false);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnShapedTextGetObjects(shaped);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Returns inline tokens in source order.</summary><param name="shaped">Buffer identity.</param><returns>An independent array of borrowed tokens.</returns>
    protected virtual TextObjectKey[] OnShapedTextGetObjects(RID shaped)
    {
        using var nativeTextScope = EnterNativeBackend(shaped);
        var v = AuthoringView(shaped); var output = new List<TextObjectKey>();
        for (var i = 0; i < v.Source.ObjectCount; i++) { var item = v.Source.ObjectAt(i); if (item.At + v.Offset >= v.Start && item.At + v.Offset < v.End) output.Add(item.Key); }
        return output.ToArray();
    }
    /// <summary>Returns an object's half-open scalar range.</summary><param name="shaped">Buffer identity.</param><param name="key">Borrowed token.</param><returns>Start and exclusive end.</returns><exception cref="KeyNotFoundException">The token is absent.</exception>
    public Vector2i ShapedTextGetObjectRange(RID shaped, TextObjectKey key)
    {
        var implementationCall = EnterImplementation(shaped, false);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnShapedTextGetObjectRange(shaped, key);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Returns an object's half-open scalar range.</summary><param name="shaped">Buffer identity.</param><param name="key">Borrowed token.</param><returns>Start and exclusive end.</returns><exception cref="KeyNotFoundException">The token is absent.</exception>
    protected virtual Vector2i OnShapedTextGetObjectRange(RID shaped, TextObjectKey key)
    {
        using var nativeTextScope = EnterNativeBackend(shaped);
        var v = AuthoringView(shaped); var range = v.Source.ServerObjectRange(key);
        if (range.X < 0 || range.X + v.Offset < v.Start || range.X + v.Offset >= v.End) throw new KeyNotFoundException("The inline object is absent from this buffer.");
        return range + new Vector2i(v.Offset, v.Offset);
    }
    /// <summary>Returns an inline rectangle relative to the buffer baseline.</summary><param name="shaped">Buffer identity.</param><param name="key">Borrowed token.</param><returns>Full shaped geometry.</returns><exception cref="KeyNotFoundException">The token is absent.</exception>
    public Rect2 ShapedTextGetObjectRect(RID shaped, TextObjectKey key)
    {
        var implementationCall = EnterImplementation(shaped, false);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnShapedTextGetObjectRect(shaped, key);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Returns an inline rectangle relative to the buffer baseline.</summary><param name="shaped">Buffer identity.</param><param name="key">Borrowed token.</param><returns>Full shaped geometry.</returns><exception cref="KeyNotFoundException">The token is absent.</exception>
    protected virtual Rect2 OnShapedTextGetObjectRect(RID shaped, TextObjectKey key)
    {
        using var nativeTextScope = EnterNativeBackend(shaped);
        var v = View(shaped); ArgumentNullException.ThrowIfNull(key);
        for (var i = 0; i < v.Source.ObjectCount; i++) { var o = v.Source.ObjectAt(i); if (ReferenceEquals(o.Key, key) && o.At + v.Offset >= v.Start && o.At + v.Offset < v.End) return v.Layout.ServerObjectRect(i, o.Size, v.Line); }
        throw new KeyNotFoundException("The inline object is absent from this buffer.");
    }
    /// <summary>Finds the closest scalar caret, interpolating inside shaped ligatures.</summary><param name="shaped">Buffer identity.</param><param name="coords">Finite baseline advance offset.</param><returns>A Unicode scalar position.</returns>
    public int ShapedTextHitTestPosition(RID shaped, float coords)
    {
        var implementationCall = EnterImplementation(shaped, false);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnShapedTextHitTestPosition(shaped, coords);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Finds the closest scalar caret, interpolating inside shaped ligatures.</summary><param name="shaped">Buffer identity.</param><param name="coords">Finite baseline advance offset.</param><returns>A Unicode scalar position.</returns>
    protected virtual int OnShapedTextHitTestPosition(RID shaped, float coords)
    { using var nativeTextScope = EnterNativeBackend(shaped); if (!float.IsFinite(coords)) throw new ArgumentOutOfRangeException(nameof(coords)); var v = View(shaped); return v.Metrics.GlyphCount == 0 ? 0 : v.Layout.ServerHitPosition(v.Line, coords, MathF.Ceiling(v.Advance)) + v.Offset; }
    /// <summary>Records the current shaped display at a baseline origin.</summary><param name="shaped">Buffer identity.</param><param name="canvas">Recording canvas.</param><param name="position">Finite baseline origin.</param><param name="clipL">Negative disables the leading advance bound.</param><param name="clipR">Negative disables the trailing advance bound.</param><param name="color">Modulation, or white.</param><param name="oversampling">Positive raster scale, or automatic.</param>
    public void ShapedTextDraw(RID shaped, CanvasItem canvas, Vector2 position, float clipL = -1, float clipR = -1, Color? color = null, float oversampling = 0)
    {
        var implementationCall = EnterImplementation(shaped, false);
        Exception? callbackFailure = null;
        try
        {
            implementationCall.Dispatcher.OnShapedTextDraw(shaped, canvas, position, clipL, clipR, color, oversampling);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Records the current shaped display at a baseline origin.</summary><param name="shaped">Buffer identity.</param><param name="canvas">Recording canvas.</param><param name="position">Finite baseline origin.</param><param name="clipL">Negative disables the leading advance bound.</param><param name="clipR">Negative disables the trailing advance bound.</param><param name="color">Modulation, or white.</param><param name="oversampling">Positive raster scale, or automatic.</param>
    protected virtual void OnShapedTextDraw(RID shaped, CanvasItem canvas, Vector2 position, float clipL = -1, float clipR = -1, Color? color = null, float oversampling = 0)
    { if (UsesCallbackConsumers) { DrawCallbackBuffer(shaped, canvas, position, clipL, clipR, 0, color ?? Colors.White, oversampling, false); return; } using var nativeTextScope = EnterNativeBackend(shaped); DrawBuffer(shaped, canvas, position, clipL, clipR, 0, color ?? Colors.White, oversampling, false); }
    /// <summary>Records a shaped glyph outline at a baseline origin.</summary><param name="shaped">Buffer identity.</param><param name="canvas">Recording canvas.</param><param name="position">Baseline origin.</param><param name="clipL">Leading advance bound, or negative.</param><param name="clipR">Trailing advance bound, or negative.</param><param name="outlineSize">Outline radius; nonpositive uses unexpanded glyphs.</param><param name="color">Modulation, or white.</param><param name="oversampling">Positive raster scale, or automatic.</param>
    public void ShapedTextDrawOutline(RID shaped, CanvasItem canvas, Vector2 position, float clipL = -1, float clipR = -1, int outlineSize = 1, Color? color = null, float oversampling = 0)
    {
        var implementationCall = EnterImplementation(shaped, false);
        Exception? callbackFailure = null;
        try
        {
            implementationCall.Dispatcher.OnShapedTextDrawOutline(shaped, canvas, position, clipL, clipR, outlineSize, color, oversampling);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Records a shaped glyph outline at a baseline origin.</summary><param name="shaped">Buffer identity.</param><param name="canvas">Recording canvas.</param><param name="position">Baseline origin.</param><param name="clipL">Leading advance bound, or negative.</param><param name="clipR">Trailing advance bound, or negative.</param><param name="outlineSize">Outline radius; nonpositive uses unexpanded glyphs.</param><param name="color">Modulation, or white.</param><param name="oversampling">Positive raster scale, or automatic.</param>
    protected virtual void OnShapedTextDrawOutline(RID shaped, CanvasItem canvas, Vector2 position, float clipL = -1, float clipR = -1, int outlineSize = 1, Color? color = null, float oversampling = 0)
    { if (UsesCallbackConsumers) { DrawCallbackBuffer(shaped, canvas, position, clipL, clipR, Math.Max(0, outlineSize), color ?? Colors.White, oversampling, true); return; } using var nativeTextScope = EnterNativeBackend(shaped); DrawBuffer(shaped, canvas, position, clipL, clipR, Math.Max(0, outlineSize), color ?? Colors.White, oversampling, true); }
    private static void DrawBuffer(RID rid, CanvasItem canvas, Vector2 baseline, float left, float right, int outline, Color color, float oversampling, bool outlinePass)
    {
        var owner = TextBuffers.Resolve(rid);
        if (owner.Owner is TextParagraph paragraph) paragraph.ServerDraw(owner.Line, rid, canvas, baseline, left, right, outline, color, oversampling, outlinePass);
        else { var source = (TextLine)owner.Owner; source.ServerAccess(); source.ServerDraw(canvas, baseline, left, right, outline, color, oversampling, outlinePass); }
    }
    private sealed class BufferMutation(TextLine buffer, TextParagraph? parent = null, int line = -1) : IDisposable
    {
        internal TextLine Buffer { get; } = buffer;
        private bool _pending = parent is not null;
        internal void Commit(bool detachSubstring = true) { if (detachSubstring) Buffer.ServerDetachSubstring(); if (!_pending) return; parent!.ServerPublishLine(line, Buffer); _pending = false; }
        public void Dispose() { if (_pending) Buffer.Dispose(); }
    }
    private static BufferMutation Mutation(RID rid)
    {
        var owner = TextBuffers.Resolve(rid);
        if (owner.Owner is TextLine line) { line.ServerAccess(); return new(line); }
        var parent = (TextParagraph)owner.Owner; var buffer = parent.ServerMutableLine(owner.Line, rid, out var pending);
        return new(buffer, pending ? parent : null, owner.Line);
    }
    private static TextLine ReadSource(RID rid)
    {
        var owner = TextBuffers.Resolve(rid);
        if (owner.Owner is TextLine line) { line.ServerAccess(); return line; }
        var source = ((TextParagraph)owner.Owner).ServerAuthoringView(owner.Line, rid).Source; source.ServerAccess(); return source;
    }
    private static BufferView AuthoringView(RID rid)
    {
        var owner = TextBuffers.Resolve(rid);
        if (owner.Owner is TextParagraph paragraph) return paragraph.ServerAuthoringView(owner.Line, rid);
        var source = (TextLine)owner.Owner; source.ServerAccess();
        return new(source, source.ServerUnpreparedLayout, 0, source.ServerOffset, source.ServerOffset + source.ScalarCount, source.ServerOffset);
    }
    private static BufferView View(RID rid)
    {
        var owner = TextBuffers.Resolve(rid);
        if (owner.Owner is TextParagraph paragraph) return paragraph.ServerView(owner.Line, rid);
        var line = (TextLine)owner.Owner; line.ServerAccess(); var layout = line.ServerLayout;
        return new(line, layout, 0, line.ServerOffset, line.ServerOffset + line.ScalarCount, line.ServerOffset, line.ServerDisplayWidth);
    }
    internal readonly record struct BufferView(TextLine Source, TextLayout Layout, int Line, int Start, int End, int Offset = 0, float? DisplayWidth = null)
    {
        internal float Advance => DisplayWidth ?? Metrics.Width;
        internal TextLayoutLine Metrics => Layout.LineCount == 0 ? default : Layout.Lines[Line];
    }
    /// <inheritdoc />
    protected override void ValidateDisposal() => throw new InvalidOperationException("A retained text interface cannot be disposed.");
}
