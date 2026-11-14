using System.Globalization;
using Electron2D;

internal static partial class ControlLayoutTests
{
    private static void VerifyAnchorAndOffsetSides()
    {
        using var stream = typeof(ControlLayoutTests).Assembly.GetManifestResourceStream("TestFixtures.ControlAnchorLayout")
            ?? throw new InvalidOperationException("Missing anchor and offset oracle fixture.");
        using var reader = new StreamReader(stream);
        var expected = new Dictionary<string, string[]>();
        while (reader.ReadLine() is { } line)
        {
            if (line.StartsWith('#')) continue;
            var fields = line.Split('|');
            expected.Add(fields[0], fields);
        }

        var viewport = new TestViewport { VisibleSize = new(160, 96) };
        using var tree = new SceneTree(viewport);
        var parent = new Control { Size = new(160, 96) };
        var neutral = new Node();
        var entity = new Entity();
        viewport.AddChild(parent);
        parent.AddChild(neutral);
        parent.AddChild(entity);
        float[] ordinary = [.125f, -.25f, 1.125f, 1.25f];
        float[] crossed = [.875f, .875f, .125f, .125f];
        for (var index = 0; index < 4; index++)
        {
            var side = (Side)index;
            for (var flags = 0; flags < 8; flags++)
            {
                using var control = new Control();
                var events = new List<char>();
                parent.AddChild(control);
                Seed(control);
                Observe(control, events);
                control.SetAnchor(side, flags >= 4 ? crossed[index] : ordinary[index],
                    keepOffset: (flags & 2) != 0, pushOppositeAnchor: (flags & 1) != 0);
                Verify($"method-{index}-{flags}", control, events);
            }

            using var property = new Control();
            var propertyEvents = new List<char>();
            parent.AddChild(property);
            Seed(property);
            Observe(property, propertyEvents);
            SetAnchorProperty(property, side, crossed[index]);
            Verify($"property-anchor-{index}", property, propertyEvents);
            var offset = new[] { 8f, 12f, -16f, -20f }[index] + 4;
            SetOffsetProperty(property, side, offset);
            Verify($"property-offset-{index}", property, propertyEvents);
            property.SetOffset(side, offset);
            Verify($"offset-noop-{index}", property, propertyEvents);
        }

        Node[] contexts = [parent, neutral, entity, viewport];
        for (var index = 1; index <= contexts.Length; index++)
        {
            using var control = new Control();
            Seed(control);
            control.CustomMinimumSize = new(40, 36);
            control.CustomMaximumSize = new(75, 58);
            control.GrowHorizontal = GrowDirection.Both;
            control.GrowVertical = GrowDirection.Begin;
            control.LayoutDirection = LayoutDirection.RTL;
            contexts[index - 1].AddChild(control);
            var events = new List<char>();
            Observe(control, events);
            control.SetAnchor(Side.Left, .875f, keepOffset: true);
            Verify($"context-{index}", control, events);
            control.SetOffset(Side.Bottom, 14f);
            Verify($"context-offset-{index}", control, events);
            parent.Size = viewport.VisibleSize = new(200, 120);
            viewport.NotifySizeChanged();
            Verify($"context-resize-{index}", control, events);
            parent.Size = viewport.VisibleSize = new(160, 96);
            viewport.NotifySizeChanged();
        }

        using var detached = new Control();
        parent.AddChild(detached);
        Seed(detached);
        parent.RemoveChild(detached);
        var detachedEvents = new List<char>();
        Observe(detached, detachedEvents);
        detached.SetAnchor(Side.Right, 1, keepOffset: true);
        Verify("detached-anchor", detached, detachedEvents);
        detached.SetOffset(Side.Right, 48);
        Verify("detached-offset", detached, detachedEvents);

        using var precise = new Control { Position = new(80, 48), Size = new(40, 24) };
        parent.AddChild(precise);
        var preciseEvents = new List<char>();
        Observe(precise, preciseEvents);
        precise.SetOffset(Side.Right, 120.0001f);
        Verify("subepsilon-offset", precise, preciseEvents);
        Check(precise.Size.X > 40, "A small unsignaled edit still commits the exact size.");

        Check(expected.Count == 0, "Every reference row was exercised.");
        VerifyRejectedSideEdits(precise, preciseEvents);
        VerifyNamedAnchors(parent);
        Console.WriteLine("Control anchors and offsets: 59 pinned states and event sequences, plus validation and callback failure.");
        return;

        static void Seed(Control control)
        {
            control.SetAnchor(Side.Right, .75f, keepOffset: true);
            control.SetAnchor(Side.Bottom, .75f, keepOffset: true);
            control.SetAnchor(Side.Left, .25f, keepOffset: true);
            control.SetAnchor(Side.Top, .25f, keepOffset: true);
            control.SetOffset(Side.Left, 8);
            control.SetOffset(Side.Top, 12);
            control.SetOffset(Side.Right, -16);
            control.SetOffset(Side.Bottom, -20);
        }

        static void Observe(Control control, List<char> events)
        {
            control.ItemRectChanged += _ => events.Add('R');
            control.Resized += () => events.Add('S');
        }

        void Verify(string key, Control control, List<char> events)
        {
            Check(expected.Remove(key, out var row), "Missing reference row " + key);
            var actual = Capture(control);
            for (var i = 0; i < actual.Length; i++)
            {
                var value = float.Parse(row![i + 1], CultureInfo.InvariantCulture);
                Check(Math.Abs(actual[i] - value) < .00002f, $"{key}, field {i}: expected {value}, got {actual[i]}.");
            }
            Check(events.SequenceEqual(row![^1]), $"{key}: expected events {row[^1]}, got {new string([.. events])}.");
            events.Clear();
        }
    }

    private static float[] Capture(Control control)
    {
        float[] anchors = [control.AnchorLeft, control.AnchorTop, control.AnchorRight, control.AnchorBottom];
        float[] offsets = [control.OffsetLeft, control.OffsetTop, control.OffsetRight, control.OffsetBottom];
        for (var index = 0; index < anchors.Length; index++)
        {
            Check(anchors[index] == control.GetAnchor((Side)index), "Anchor property and method agree.");
            Check(offsets[index] == control.GetOffset((Side)index), "Offset property and method agree.");
        }
        return [.. anchors, .. offsets, control.Position.X, control.Position.Y, control.Size.X, control.Size.Y];
    }

    private static void VerifyRejectedSideEdits(Control control, List<char> events)
    {
        var before = Capture(control);
        Reject<ArgumentOutOfRangeException>(() => control.GetAnchor((Side)(-1)));
        Reject<ArgumentOutOfRangeException>(() => control.GetOffset((Side)4));
        Reject<ArgumentOutOfRangeException>(() => control.SetAnchor((Side)4, 1));
        Reject<ArgumentOutOfRangeException>(() => control.SetOffset((Side)(-1), 2));
        Reject<ArgumentOutOfRangeException>(() => control.AnchorLeft = float.NaN);
        Reject<ArgumentOutOfRangeException>(() => control.OffsetTop = float.NegativeInfinity);
        Reject<ArgumentOutOfRangeException>(() => control.SetAnchor(Side.Left, float.MaxValue));
        Reject<ArgumentOutOfRangeException>(() => control.SetAnchor(Side.Right, float.MaxValue, keepOffset: true));
        Reject<InvalidOperationException>(() => Task.Run(() => control.OffsetTop = 9).GetAwaiter().GetResult());
        Reject<InvalidOperationException>(() => Task.Run(() => control.AnchorLeft).GetAwaiter().GetResult());
        Check(Capture(control).SequenceEqual(before) && events.Count == 0, "Rejected writes preserve edges, geometry and events.");

        var failure = new InvalidOperationException("layout callback");
        Action<CanvasItem> fail = _ =>
        {
            Check(control.OffsetRight == 124 && control.Size.X == 44, "Callback sees committed state.");
            throw failure;
        };
        control.ItemRectChanged += fail;
        try
        {
            control.SetOffset(Side.Right, 124);
            throw new Exception("Expected a layout callback failure.");
        }
        catch (InvalidOperationException error) when (ReferenceEquals(error, failure)) { }
        control.ItemRectChanged -= fail;
        Check(events.SequenceEqual("R"), "Exception stops the remaining synchronous event delivery.");
        control.SetOffset(Side.Right, 128);
        Check(events.SequenceEqual("RRS") && control.Size.X == 48, "Next edit after a callback failure reflows.");

        var disposed = new Control();
        disposed.Dispose();
        Reject<ObjectDisposedException>(() => disposed.GetAnchor(Side.Left));
        Reject<ObjectDisposedException>(() => disposed.GetOffset(Side.Left));
        Reject<ObjectDisposedException>(() => disposed.AnchorTop = 0);
        Reject<ObjectDisposedException>(() => disposed.OffsetBottom = 0);
    }

    private static void VerifyNamedAnchors(Control parent)
    {
        using var control = new Control { Position = new(8, 12), Size = new(50, 24) };
        parent.AddChild(control);
        Check((int)Control.Anchor.Begin == 0 && (int)Control.Anchor.End == 1, "Named endpoint identity.");
        control.SetAnchorEndpoint(Side.Right, Control.Anchor.End, keepOffset: true);
        control.SetAnchor(Side.Left, .25f);
        control.SetAnchorEndpoint(Side.Left, Control.Anchor.Begin, keepOffset: true);
        Check(control.GetAnchor(Side.Left) == 0 && control.AnchorRight == 1 && control.OffsetLeft == -32 &&
              control.Position == new Vector2(-32, 12) && control.Size == new Vector2(250, 24), "Named endpoints participate in live layout.");
        var before = Capture(control);
        Reject<ArgumentOutOfRangeException>(() => control.SetAnchorEndpoint(Side.Top, (Control.Anchor)2));
        Check(Capture(control).SequenceEqual(before), "An undefined named endpoint leaves the rectangle unchanged.");
    }

    private static void SetAnchorProperty(Control control, Side side, float value)
    {
        switch (side)
        {
            case Side.Left: control.AnchorLeft = value; break;
            case Side.Top: control.AnchorTop = value; break;
            case Side.Right: control.AnchorRight = value; break;
            case Side.Bottom: control.AnchorBottom = value; break;
        }
    }

    private static void SetOffsetProperty(Control control, Side side, float value)
    {
        switch (side)
        {
            case Side.Left: control.OffsetLeft = value; break;
            case Side.Top: control.OffsetTop = value; break;
            case Side.Right: control.OffsetRight = value; break;
            case Side.Bottom: control.OffsetBottom = value; break;
        }
    }
}
