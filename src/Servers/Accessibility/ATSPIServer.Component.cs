using static Electron2D.GIONative;
using Element = Electron2D.AccessibilityServer.Element;
using AFlag = Electron2D.AccessibilityServer.AccessibilityFlags;
using AAction = Electron2D.AccessibilityServer.AccessibilityAction;
namespace Electron2D;

internal sealed unsafe partial class ATSPIServer
{
    private static Rect2 Bounds(Element e)
    {
        if (e.TextSource is { IsDisposed: false, IsInsideTree: true } source)
        {
            var local = source is LineEdit line ? line.AccessibilityLineRect() : ((TextEdit)source).AccessibilityLineRect(e.TextLine);
            return (source.GetViewport()!.GetScreenTransform() * source.GetGlobalTransformWithCanvas()) * local;
        }
        var rect = AccessibilityServer.TransformBounds(e.Bounds, e.Transform);
        if (e.Parent is { IsTextLine: true } parent && e.TextValue is not null) rect = AccessibilityServer.TransformBounds(new(rect.Position + parent.Bounds.Position, rect.Size), parent.Transform);
        return rect;
    }
    private static Element? WindowRoot(Element e) { for (var p = e; p is not null; p = p.Parent) if (p.IsWindow) return p; return null; }
    private static Rect2 Coordinates(Element e, uint kind)
    {
        var rect = Bounds(e); var root = WindowRoot(e);
        if (kind == 1) return rect;
        if (kind == 2) return e.Parent is { } p ? new(rect.Position - Bounds(p).Position, rect.Size) : rect;
        if (kind != 0) throw new InvalidRequestException("The coordinate type is not supported.");
        return root is { ScreenOriginKnown: true } ? e.IsWindow ? root.OuterRect : new(rect.Position + root.InnerRect.Position, rect.Size) : new(new Vector2(int.MinValue, int.MinValue), rect.Size);
    }
    private static bool Showing(Element e)
    {
        var connected = false;
        for (var p = e; p is not null; p = p.Parent)
        {
            if (p.Retired || p.Flag(AFlag.Hidden)) return false;
            if (p.Source is { } source && (source.IsDisposed || !source.IsInsideTree || source.IsDepartingTree || source.IsQueuedForDeletion || source is CanvasItem { IsVisibleInTree: false } || source is Window { Visible: false })) return false;
            connected |= p.IsApplication;
        }
        return connected;
    }
    private static bool Contains(Element e, int x, int y, uint kind)
    {
        if (!Showing(e) || kind == 0 && WindowRoot(e) is not { ScreenOriginKnown: true }) return false;
        return Coordinates(e, kind).HasPoint(new(x, y));
    }
    private static int Pixel(double value) => (int)Math.Clamp(value, int.MinValue, int.MaxValue);
    private static nint RectValue(Rect2 rect)
    {
        var x = Math.Floor(rect.Position.X); var y = Math.Floor(rect.Position.Y);
        var fractionX = double.IsFinite(x) ? rect.Position.X - x : 0;
        var fractionY = double.IsFinite(y) ? rect.Position.Y - y : 0;
        return Tuple([I(Pixel(x)), I(Pixel(y)), I(Pixel(Math.Ceiling(rect.Size.X + fractionX))), I(Pixel(Math.Ceiling(rect.Size.Y + fractionY)))]);
    }
    private nint Hit(Element e, int x, int y, uint kind)
    {
        var todo = new Stack<(Element Node, bool After)>(); todo.Push((e, false));
        while (todo.Count != 0)
        {
            var (node, after) = todo.Pop(); if (!Showing(node)) continue;
            var inside = Contains(node, x, y, kind);
            if (after) { if (inside && !node.Flag(AFlag.TouchPassthrough)) return ObjectRef(node); continue; }
            if (!inside && node.Flag(AFlag.ClipsChildren)) continue;
            todo.Push((node, true));
            foreach (var child in node.Children) todo.Push((child, false));
        }
        return ObjectRef(null);
    }
    private nint ComponentCall(Element e, string method, nint args)
    {
        switch (method)
        {
            case "Contains": return Tuple([B(Contains(e, IntArg(args, 0), IntArg(args, 1), UIntArg(args, 2)))]);
            case "GetAccessibleAtPoint": return Tuple([Hit(e, IntArg(args, 0), IntArg(args, 1), UIntArg(args, 2))]);
            case "GetExtents": return Tuple([RectValue(Coordinates(e, UIntArg(args, 0)))]);
            case "GetPosition": { var rect = Coordinates(e, UIntArg(args, 0)); return Tuple([I(Pixel(Math.Floor(rect.Position.X))), I(Pixel(Math.Floor(rect.Position.Y)))]); }
            case "GetSize": { var rect = e.IsWindow && e.ScreenOriginKnown ? e.OuterRect : Bounds(e); return Tuple([I(Pixel(Math.Ceiling(rect.Size.X))), I(Pixel(Math.Ceiling(rect.Size.Y)))]); }
            case "GetLayer": return Tuple([U(e.IsWindow ? 7u : 3u)]);
            case "GetMDIZOrder": return Tuple([g_variant_new_int16(0)]);
            case "GrabFocus": return Tuple([B(Invoke(e, AAction.Focus))]);
            case "GetAlpha": return Tuple([g_variant_new_double(1)]);
            case "SetExtents": case "SetPosition": case "SetSize": case "ScrollTo": case "ScrollToPoint": return Tuple([B(false)]);
            default: throw new InvalidOperationException("Unknown component method.");
        }
    }
    internal void BoundsChanged(Element e) => Emit(e.Path, "org.a11y.atspi.Event.Object", "BoundsChanged", Event("", 0, 0, RectValue(Coordinates(e, 0)), e));
}
