using static Electron2D.GIONative;
using Element = Electron2D.AccessibilityServer.Element;
namespace Electron2D;

internal sealed unsafe partial class ATSPIServer
{
    private static void AddScrollAttributes(nint builder, Element e)
    {
        void Add(string name, float value) => g_variant_builder_add_value(builder, g_variant_new_dict_entry(S(name), S(value.ToString("R", System.Globalization.CultureInfo.InvariantCulture))));
        if ((e.ScrollMask & 1) != 0) Add("scroll-x", e.ScrollOffset.X);
        if ((e.ScrollMask & 2) != 0) Add("scroll-y", e.ScrollOffset.Y);
        if ((e.ScrollMask & 4) != 0) { Add("scroll-x-min", e.ScrollMinimum.X); Add("scroll-x-max", e.ScrollMaximum.X); }
        if ((e.ScrollMask & 8) != 0) { Add("scroll-y-min", e.ScrollMinimum.Y); Add("scroll-y-max", e.ScrollMaximum.Y); }
    }
    internal void ScrollChanged(Element e, bool position)
    {
        Emit(e.Path, "org.a11y.atspi.Event.Object", "AttributesChanged", Event("", 0, 0, I(0), e));
        if (position)
        {
            Emit(e.Path, "org.a11y.atspi.Event.Object", "VisibleDataChanged", Event("", 0, 0, I(0), e));
            VisibilityChanged(e);
        }
    }
    private static Vector2? ScrollWindowPoint(Element e, uint kind, int x, int y)
    {
        if (kind > 2) throw new InvalidRequestException("The coordinate type is not supported.");
        if (kind == 0) return WindowRoot(e) is { ScreenOriginKnown: true } root ? new Vector2(x, y) - root.InnerRect.Position : null;
        if (kind == 2 && e.Parent is { } parent) return new Vector2(x, y) + Bounds(parent).Position;
        return new Vector2(x, y);
    }
    private static bool InvokeScrollPoint(Element e, Vector2 point, bool placement)
    {
        if (!point.IsFinite() || !CanInvoke(e)) return false;
        var callback = placement ? e.ScrollToPointAction : e.SetScrollOffsetAction;
        if (callback is null) return false;
        try { callback(point); } catch (Exception error) { throw new CallbackException(error); }
        return true;
    }
    private static double RevealDelta(double from, double to, double low, double high, int edge)
    {
        if (edge < 0) return from - low;
        if (edge > 0) return to - high;
        if (from >= low && to <= high || from <= low && to >= high) return 0;
        if (to - from > high - low) return Math.Abs(from - low) < Math.Abs(to - high) ? from - low : to - high;
        return from < low ? from - low : to - high;
    }
    private static float Offset(float current, double delta, float min, float max, bool bounded) => (float)Math.Clamp(current + delta, bounded ? min : -float.MaxValue, bounded ? max : float.MaxValue);
    private static bool ScrollMoves(Element parent, Element target)
    {
        if (parent.SetScrollOffsetAction != parent.SourceSetScrollOffsetAction) return true;
        if (target.TextSource == parent.Source && target.TextSource is not null) return true;
        if ((target.Source ?? target.TextSource) is not CanvasItem item) return target.Source is null && target.TextSource is null;
        for (var child = item; child.RenderParent is { } owner; child = owner)
            if (owner == parent.Source) return owner switch
            {
                ScrollContainer container => container.AccessibilityScrollsChild(child),
                GraphEdit => child is GraphElement,
                _ => false,
            };
        return false;
    }
    private static bool ScrollTo(Element e, uint hint, Vector2? point)
    {
        if (hint > 6) throw new InvalidRequestException("The scroll placement is not supported.");
        if (!CanInvoke(e)) return false;
        if (point is { } destination && e.ScrollToPointAction is not null) return InvokeScrollPoint(e, destination, true);
        if (point is null && e.ScrollIntoViewAction is { } reveal)
        {
            try { reveal((AccessibilityServer.AccessibilityScrollHint)(hint == 6 ? 0 : hint)); } catch (Exception error) { throw new CallbackException(error); }
            return true;
        }
        var target = Bounds(e); var accepted = false;
        for (var parent = e.Parent; parent is not null; parent = parent.Parent)
        {
            if (parent.SetScrollOffsetAction is null || !ScrollMoves(parent, e)) continue;
            if (!CanInvoke(parent)) return false;
            var frame = AccessibilityServer.SourceScrollFrame(parent) ?? (parent.Transform, parent.Bounds, parent.ScrollOffset, parent.ScrollMinimum, parent.ScrollMaximum, (byte)(parent.ScrollMask & 12));
            var (world, view, before, min, max, ranges) = frame;
            if (world.Determinant() == 0 || !float.IsFinite(world.Determinant()) || !world.IsFinite() || view.Size.X <= 0 || view.Size.Y <= 0) return false;
            var inverse = world.AffineInverse(); if (!inverse.IsFinite()) return false;
            var local = inverse * target;
            if (!local.Position.IsFinite() || !local.End.IsFinite() || !view.End.IsFinite()) return false;
            double dx, dy;
            if (point is { } at)
            {
                var displacement = inverse.BasisXform(target.Position - at);
                var position = local.Position - displacement;
                if (!position.IsFinite()) return false;
                var x = Math.Clamp(position.X, view.Position.X, Math.Max(view.Position.X, view.End.X - local.Size.X));
                var y = Math.Clamp(position.Y, view.Position.Y, Math.Max(view.Position.Y, view.End.Y - local.Size.Y));
                dx = (double)local.Position.X - x; dy = (double)local.Position.Y - y;
            }
            else
            {
                var horizontal = hint is 0 or 4 ? -1 : hint is 1 or 5 ? 1 : 0;
                var vertical = hint is 0 or 2 ? -1 : hint is 1 or 3 ? 1 : 0;
                dx = RevealDelta(local.Position.X, local.End.X, view.Position.X, view.End.X, horizontal);
                dy = RevealDelta(local.Position.Y, local.End.Y, view.Position.Y, view.End.Y, vertical);
            }
            var requested = new Vector2(Offset(before.X, dx, min.X, max.X, (ranges & 4) != 0), Offset(before.Y, dy, min.Y, max.Y, (ranges & 8) != 0));
            if (!InvokeScrollPoint(parent, requested, false)) return false;
            accepted = true; if (e.Retired || parent.Retired) return true;
            var after = AccessibilityServer.SourceScrollFrame(parent)?.Offset ?? parent.ScrollOffset;
            // The producer may publish on the next frame; its accepted request still defines the pending displacement.
            if (after == before) after = requested;
            var movement = world.BasisXform(after - before);
            if (!movement.IsFinite()) return false;
            target.Position -= movement;
            if (parent.Flag(AccessibilityServer.AccessibilityFlags.ClipsChildren)) target = target.Intersection(world * view);
        }
        return accepted;
    }
}
