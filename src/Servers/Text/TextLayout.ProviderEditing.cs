namespace Electron2D;

internal sealed partial class TextLayout
{
    internal bool UsesProvider => _providerLayout is not null;
    private RID ProviderRangeBuffer(ProviderLayout state, int start, int end)
    {
        for (var i = 0; i < _lines.Count; i++) if (_lines[i].Start == start && _lines[i].End == end) return state.Lines[i];
        return state.Full;
    }
    private static int ProviderPosition(int value, ProviderLayout state, int start, int end)
    {
        var result = (long)value - state.ScalarOffset;
        if (result < start || result > end) throw new InvalidOperationException("The text implementation returned a position outside its scalar buffer range.");
        return (int)result;
    }
    private int[] ProviderCharacterBreaks(int start, int end, int offset)
    {
        var state = _providerLayout!;
        using var consumer = TextServerManager.BeginConsumerRead(state.Owner); var use = state.BeginUse(); Exception? useFailure = null;
        try
        {
            var breaks = state.Owner.ShapedTextGetCharacterBreaks(ProviderRangeBuffer(state, start, end)); var result = new List<int>(); var previous = -1;
            foreach (var value in breaks)
            {
                var local = ProviderPosition(value, state, 0, _count);
                if (local <= previous) throw new InvalidOperationException("The text implementation returned unordered character boundaries."); previous = local;
                if (local > start && local <= end) result.Add(checked(local + offset));
            }
            return result.ToArray();

        }
        catch (Exception scopeError) { useFailure = scopeError; throw; }
        finally { use.Dispose(useFailure); }
    }
    private int ProviderCharacterPosition(int start, int end, int offset, int position, int mode)
    {
        var state = _providerLayout!;
        using var consumer = TextServerManager.BeginConsumerRead(state.Owner); var use = state.BeginUse(); Exception? useFailure = null;
        try
        {
            var rid = ProviderRangeBuffer(state, start, end); var global = (int)Math.Clamp((long)position - offset + state.ScalarOffset, int.MinValue, int.MaxValue);
            var result = mode < 0 ? state.Owner.ShapedTextPrevCharacterPos(rid, global) : mode > 0 ? state.Owner.ShapedTextNextCharacterPos(rid, global) : state.Owner.ShapedTextClosestCharacterPos(rid, global);
            return checked(ProviderPosition(result, state, start, end) + offset);

        }
        catch (Exception scopeError) { useFailure = scopeError; throw; }
        finally { use.Dispose(useFailure); }
    }
    private int[] ProviderBreakRanges(int[] ranges, ProviderLayout state, int start, int end, int offset)
    {
        if ((ranges.Length & 1) != 0 || ranges.Length > 2097152) throw new InvalidOperationException("The text implementation returned invalid paired scalar ranges.");
        var result = new List<int>(); var previous = 0;
        for (var i = 0; i < ranges.Length; i += 2)
        {
            var from = ProviderPosition(ranges[i], state, 0, _count); var to = ProviderPosition(ranges[i + 1], state, 0, _count);
            if (to < from || from < previous) throw new InvalidOperationException("The text implementation returned unordered paired scalar ranges."); previous = to;
            if (to < start || from > end) continue;
            result.Add(checked(Math.Max(start, from) + offset)); result.Add(checked(Math.Min(end, to) + offset));
        }
        return result.ToArray();
    }
    private float ProviderAdvanceOffset(int line)
    {
        var metrics = _lines[line]; if (metrics.GlyphCount == 0) return 0;
        var first = _glyphs[metrics.GlyphStart]; var origin = first.Position - first.Offset;
        return Key.Orientation == TextOrientation.Horizontal ? origin.X : origin.Y;
    }
    private (float? Leading, float? Trailing, TextDirection LeadingDirection, TextDirection TrailingDirection) ProviderCarets(int column)
    {
        var state = _providerLayout!; column = Math.Clamp(column, 0, _count);
        using var consumer = TextServerManager.BeginConsumerRead(state.Owner); var use = state.BeginUse(); Exception? useFailure = null;
        try
        {
            float? leading = null, trailing = null; var leadingDirection = TextDirection.Auto; var trailingDirection = TextDirection.Auto;
            for (var i = 0; i < _lines.Count; i++)
            {
                var line = _lines[i]; if (column < line.Start || column > line.End) continue;
                var caret = state.Owner.ShapedTextGetCarets(state.Lines[i], checked(column + state.ScalarOffset)); var offset = ProviderAdvanceOffset(i);
                if ((column > line.Start || i == 0) && caret.LeadingRect != default)
                { leading = (Key.Orientation == TextOrientation.Horizontal ? caret.LeadingRect.Position.X : caret.LeadingRect.Position.Y) + offset; leadingDirection = caret.LeadingDirection; }
                if ((column < line.End || i + 1 == _lines.Count) && caret.TrailingRect != default)
                { trailing = (Key.Orientation == TextOrientation.Horizontal ? caret.TrailingRect.Position.X : caret.TrailingRect.Position.Y) + offset; trailingDirection = caret.TrailingDirection; }
            }
            return (leading, trailing, leadingDirection, trailingDirection);

        }
        catch (Exception scopeError) { useFailure = scopeError; throw; }
        finally { use.Dispose(useFailure); }
    }
    private int ProviderHitLine(int line, float coordinate, bool midGrapheme)
    {
        if ((uint)line >= (uint)_lines.Count) return 0; var state = _providerLayout!;
        using var consumer = TextServerManager.BeginConsumerRead(state.Owner); var use = state.BeginUse(); Exception? useFailure = null;
        try
        {
            var result = checked(state.Owner.ShapedTextHitTestPosition(state.Lines[line], coordinate - ProviderAdvanceOffset(line)) - state.ScalarOffset);
            var metrics = _lines[line];
            if (result < metrics.Start || result > metrics.End) throw new InvalidOperationException("The text implementation returned a hit outside its scalar line range.");
            return midGrapheme ? result : Math.Clamp(ClosestGrapheme(result), metrics.Start, metrics.End);

        }
        catch (Exception scopeError) { useFailure = scopeError; throw; }
        finally { use.Dispose(useFailure); }
    }
    private void ProviderSelectionRanges(int from, int to, List<Vector2> output)
    {
        var state = _providerLayout!; if (from > to) (from, to) = (to, from);
        using var consumer = TextServerManager.BeginConsumerRead(state.Owner); var use = state.BeginUse(); Exception? useFailure = null;
        try
        {
            for (var i = 0; i < _lines.Count; i++)
            {
                var line = _lines[i]; if (from >= line.End || to <= line.Start) continue;
                using var selection = state.Owner.ReadSelection(state.Lines[i], checked(Math.Max(from, line.Start) + state.ScalarOffset), checked(Math.Min(to, line.End) + state.ScalarOffset));
                var offset = ProviderAdvanceOffset(i);
                foreach (var range in selection.Ranges) output.Add(range + new Vector2(offset, offset));
            }

        }
        catch (Exception scopeError) { useFailure = scopeError; throw; }
        finally { use.Dispose(useFailure); }
    }
}
