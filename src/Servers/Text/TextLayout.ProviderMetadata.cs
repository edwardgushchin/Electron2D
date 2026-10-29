namespace Electron2D;

internal sealed partial class TextLayout
{
    private static int ReadProviderRunCount(ProviderLayout state, RID rid)
    {
        var count = state.Owner.ShapedGetRunCount(rid);
        if (count < 0 || count > state.Owner.ShapedTextGetGlyphCount(rid)) throw new InvalidOperationException("The text implementation returned an invalid visual run count.");
        return count;
    }
    internal bool TryProviderRunCount(int line, out int count)
    {
        if (_providerLayout is not { } state) { count = 0; return false; }
        using var consumer = TextServerManager.BeginConsumerRead(state.Owner); var use = state.BeginUse(); Exception? useFailure = null;
        try
        {
            count = ReadProviderRunCount(state, state.Lines[line]); return true;

        }
        catch (Exception scopeError) { useFailure = scopeError; throw; }
        finally { use.Dispose(useFailure); }
    }
    internal bool TryProviderRun<T>(int line, int index, Func<TextServer, RID, int, T> query, out T value)
    {
        if (_providerLayout is not { } state) { value = default!; return false; }
        using var consumer = TextServerManager.BeginConsumerRead(state.Owner); var use = state.BeginUse(); Exception? useFailure = null;
        try
        {
            var rid = state.Lines[line];
            if ((uint)index >= (uint)ReadProviderRunCount(state, rid)) throw new ArgumentOutOfRangeException(nameof(index));
            value = query(state.Owner, rid, index); return true;

        }
        catch (Exception scopeError) { useFailure = scopeError; throw; }
        finally { use.Dispose(useFailure); }
    }
    internal Vector2i ProviderRunRange(Vector2i range, int offset)
    {
        var state = _providerLayout!; var start = ProviderPosition(range.X, state, 0, _count); var end = ProviderPosition(range.Y, state, start, _count);
        return new(checked(start + offset), checked(end + offset));
    }
    private Rect2 ProviderObjectRect(ProviderLayout state, int index, int line)
    {
        if ((uint)index >= (uint)state.Objects.Count) throw new KeyNotFoundException("The inline object is absent from the producer buffer.");
        using var consumer = TextServerManager.BeginConsumerRead(state.Owner); var use = state.BeginUse(); Exception? useFailure = null;
        try
        {
            var rect = state.Owner.ShapedTextGetObjectRect(state.Lines[line], state.Objects[index]);
            if (!rect.Position.IsFinite() || !rect.Size.IsFinite() || !rect.End.IsFinite()) throw new InvalidOperationException("The text implementation returned nonfinite inline object geometry.");
            return rect;

        }
        catch (Exception scopeError) { useFailure = scopeError; throw; }
        finally { use.Dispose(useFailure); }
    }
}
