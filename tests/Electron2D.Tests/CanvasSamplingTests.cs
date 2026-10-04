using Electron2D;
using Filter = Electron2D.TextureFilter;
using Repeat = Electron2D.TextureRepeat;

internal static class CanvasSamplingTests
{
    internal static void Run()
    {
        using var source = Image.CreateFromData(2, 1, false, Image.Format.Rgba8, new byte[] { 255, 0, 0, 0, 0, 0, 255, 255 });
        using var texture = ImageTexture.CreateFromImage(source);
        var root = new TestViewport();
        var parent = new Entity { Name = "parent" };
        var child = new Probe(texture) { Name = "child" };
        var explicitChild = new Probe(texture) { Name = "explicit", TextureFilter = Filter.Nearest };
        var top = new Probe(texture) { Name = "top", TopLevel = true };
        var neutral = new Node { Name = "neutral" };
        var independent = new Probe(texture);
        root.AddChild(parent); parent.AddChild(child); parent.AddChild(explicitChild); parent.AddChild(top);
        parent.AddChild(neutral); neutral.AddChild(independent);
        Check(child.TextureFilter == Filter.ParentNode && child.TextureRepeat == Repeat.ParentNode &&
            root.CanvasItemDefaultTextureFilter == Viewport.DefaultCanvasItemTextureFilter.Linear &&
            root.CanvasItemDefaultTextureRepeat == Viewport.DefaultCanvasItemTextureRepeat.Disabled &&
            root.AnisotropicFilteringLevel == Viewport.AnisotropicFiltering.Anisotropy4X, "Sampling defaults.");
        var events = 0; parent.PropertyListChanged += _ => events++;
        using (var tree = new SceneTree(root))
        {
            var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>();
            CanvasBatch Batch(Probe item)
            {
                item.PrepareCanvas(); vertices.Clear(); batches.Clear(); item.AppendCanvas(vertices, batches, item.GetGlobalTransform());
                Check(batches.Count == 1, "One probe batch."); return batches[0];
            }
            Check(Batch(child).Filter == Filter.Linear && Batch(top).Filter == Filter.Linear && Batch(independent).Filter == Filter.Linear, "Viewport default resolves canvas boundaries.");
            Batch(explicitChild);
            parent.TextureFilter = Filter.Nearest;
            Check(events == 1 && Batch(child).Filter == Filter.Nearest && child.Draws == 2 &&
                Batch(explicitChild).Filter == Filter.Nearest && explicitChild.Draws == 1 && Batch(top).Filter == Filter.Linear &&
                Batch(independent).Filter == Filter.Linear, "Inheritance redraw stops at overrides and neutral boundaries; top-level uses viewport.");
            parent.TextureFilter = Filter.Nearest;
            Check(events == 1 && Batch(child).Filter == Filter.Nearest && child.Draws == 2, "No-op keeps retained commands.");
            root.CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.NearestWithMipmaps;
            Check(Batch(top).Filter == Filter.NearestWithMipmaps && Batch(independent).Filter == Filter.NearestWithMipmaps, "Live viewport default crosses neutral roots at submission.");
            child.TopLevel = true;
            Check(Batch(child).Filter == Filter.NearestWithMipmaps, "Top-level rebinding switches sampling inheritance.");
            child.TopLevel = false;
            Check(Batch(child).Filter == Filter.Nearest, "Returning to parent restores sampling inheritance.");
            child.Reparent(neutral);
            Check(Batch(child).Filter == Filter.NearestWithMipmaps, "Reparenting refreshes sampling caches.");
            parent.TextureRepeat = Repeat.Mirror;
            Check(events == 2 && Batch(explicitChild).Repeat == Repeat.Mirror && Batch(child).Repeat == Repeat.Disabled, "Repeat inheritance is independent of filter overrides.");
            root.CanvasItemDefaultTextureRepeat = Viewport.DefaultCanvasItemTextureRepeat.Enabled;
            Check(Batch(child).Repeat == Repeat.Enabled && Batch(top).Repeat == Repeat.Enabled, "Viewport repeat fallback.");
            explicitChild.TextureFilter = Filter.LinearWithMipmapsAnisotropic;
            root.AnisotropicFilteringLevel = Viewport.AnisotropicFiltering.Anisotropy16X;
            Check(Batch(explicitChild).MaxAnisotropy == 16, "Viewport anisotropy reaches batches.");
            root.AnisotropicFilteringLevel = Viewport.AnisotropicFiltering.Disabled;
            Check(Batch(explicitChild).MaxAnisotropy == 1, "Disabled anisotropy retains mip filtering with one sample.");
            Reject<ArgumentOutOfRangeException>(() => parent.TextureFilter = Filter.Max);
            Reject<ArgumentOutOfRangeException>(() => parent.TextureRepeat = (Repeat)(-1));
            Reject<ArgumentOutOfRangeException>(() => root.CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Max);
            Reject<ArgumentOutOfRangeException>(() => root.CanvasItemDefaultTextureRepeat = Viewport.DefaultCanvasItemTextureRepeat.Max);
            Reject<ArgumentOutOfRangeException>(() => root.AnisotropicFilteringLevel = Viewport.AnisotropicFiltering.Max);
            Reject<InvalidOperationException>(() => Task.Run(() => parent.TextureFilter = Filter.Linear).GetAwaiter().GetResult());
            Reject<InvalidOperationException>(() => Task.Run(() => root.AnisotropicFilteringLevel = Viewport.AnisotropicFiltering.Disabled).GetAwaiter().GetResult());
            Action<ElectronObject> fail = _ => throw new ApplicationException("sampling property-list callback");
            parent.PropertyListChanged += fail;
            Reject<ApplicationException>(() => parent.TextureFilter = Filter.Linear);
            Check(parent.TextureFilter == Filter.Linear && events == 3, "Callback failure preserves committed sampling state.");
            parent.PropertyListChanged -= fail;
            root.CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.ParentNode;
            root.CanvasItemDefaultTextureRepeat = Viewport.DefaultCanvasItemTextureRepeat.ParentNode;
            Check(Batch(child).Filter == Filter.Linear && Batch(child).Repeat == Repeat.Disabled, "Root inherited viewport defaults fall back to linear/clamp.");
            for (var i = 0; i < 1000; i++) { vertices.Clear(); batches.Clear(); child.AppendCanvas(vertices, batches, child.GetGlobalTransform()); }
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 1000; i++) { vertices.Clear(); batches.Clear(); child.AppendCanvas(vertices, batches, child.GetGlobalTransform()); }
            Check(GC.GetAllocatedBytesForCurrentThread() == before, "Sampling resolution and batch keys allocate nothing after warmup.");
        }
        Reject<ObjectDisposedException>(() => parent.TextureRepeat = Repeat.Disabled);
        Reject<ObjectDisposedException>(() => root.CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Linear);
        VerifyOpacity(texture);
        VerifyPacking();
        Console.WriteLine("Canvas sampling managed checks passed.");
    }

    private static void VerifyOpacity(Texture texture)
    {
        using var root = new Node();
        var sprite = new Sprite { Texture = texture, Centered = false, RegionEnabled = true, RegionRect = new(0, 0, 6, 1), TextureRepeat = Repeat.Enabled };
        root.AddChild(sprite);
        Check(sprite.IsPixelOpaque(new(2.2f, 0.2f)), "Detached repeat setting has not entered the canvas cache.");
        using (var tree = new SceneTree(root))
        {
            Check(!sprite.IsPixelOpaque(new(2.2f, 0.2f)) && sprite.IsPixelOpaque(new(3.2f, 0.2f)), "Repeat opacity wraps source coordinates.");
            sprite.TextureRepeat = Repeat.Mirror;
            Check(!sprite.IsPixelOpaque(new(2.2f, 0.2f)) && sprite.IsPixelOpaque(new(1.2f, 0.2f)), "Mirrored opacity preserves integer source-index convention.");
            sprite.RegionRect = new(-2, 0, 6, 1);
            Check(!sprite.IsPixelOpaque(new(0.2f, 0.2f)), "Negative repeated source indices are not opaque.");
            sprite.TextureRepeat = Repeat.Enabled;
            root.RemoveChild(sprite);
            sprite.TextureRepeat = Repeat.Disabled;
            sprite.RegionRect = new(0, 0, 6, 1);
            Check(!sprite.IsPixelOpaque(new(2.2f, 0.2f)), "Detached opacity keeps its last active cache.");
            root.AddChild(sprite);
            Check(sprite.IsPixelOpaque(new(2.2f, 0.2f)), "Reentry applies detached repeat edits.");
        }
    }

    private static void VerifyPacking()
    {
        using var window = new Window
        {
            CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.NearestWithMipmaps,
            CanvasItemDefaultTextureRepeat = Viewport.DefaultCanvasItemTextureRepeat.Mirror,
            AnisotropicFilteringLevel = Viewport.AnisotropicFiltering.Anisotropy8X,
        };
        window.AddChild(new Sprite { Name = "sprite", TextureFilter = Filter.LinearWithMipmapsAnisotropic, TextureRepeat = Repeat.Enabled });
        window.Children[0].Owner = window;
        using var packed = new PackedScene(); packed.Pack(window);
        using var copy = (Window)packed.Instantiate();
        Check(copy.CanvasItemDefaultTextureFilter == window.CanvasItemDefaultTextureFilter && copy.CanvasItemDefaultTextureRepeat == window.CanvasItemDefaultTextureRepeat &&
            copy.AnisotropicFilteringLevel == window.AnisotropicFilteringLevel && copy.GetNode<Sprite>("sprite").TextureFilter == Filter.LinearWithMipmapsAnisotropic &&
            copy.GetNode<Sprite>("sprite").TextureRepeat == Repeat.Enabled, "Stored sampling state reconstructs across each declaring layer.");
        var settings = ProjectSettings.Service;
        var original = ProjectSettings.Get(ProjectSettings.AnisotropicFilteringLevel);
        try
        {
            ProjectSettings.Set(ProjectSettings.AnisotropicFilteringLevel, 4);
            using var configured = new Window();
            Check(configured.AnisotropicFilteringLevel == Viewport.AnisotropicFiltering.Anisotropy16X && window.AnisotropicFilteringLevel == Viewport.AnisotropicFiltering.Anisotropy8X, "Construction samples project anisotropy without mutating live viewports.");
            Reject<ArgumentOutOfRangeException>(() => ProjectSettings.Set(ProjectSettings.AnisotropicFilteringLevel, 5));
        }
        finally { ProjectSettings.Set(ProjectSettings.AnisotropicFilteringLevel, original); }
    }

    private sealed class TestViewport : Viewport
    {
        public override Rect2 GetVisibleRect() => new(0, 0, 64, 64);
    }

    private sealed class Probe(Texture texture) : Entity
    {
        internal int Draws;
        protected override void OnDraw() { Draws++; DrawTexture(texture, Vector2.Zero); }
    }

    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
