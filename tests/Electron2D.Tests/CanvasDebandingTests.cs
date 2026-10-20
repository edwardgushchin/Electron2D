using System.Diagnostics;
using Electron2D;
using static TextureArrayTests;

internal static class CanvasDebandingTests
{
    internal static void Managed()
    {
        using var view = new SubViewport();
        Check(!view.UseDebanding, "Debanding defaults off.");
        view.UseDebanding = true;
        using var packed = new PackedScene(); packed.Pack(view);
        using var copy = (SubViewport)packed.Instantiate();
        Check(copy.UseDebanding && copy.RenderingOwner is null, "Scene copy keeps authoring without renderer state.");
        var previous = ProjectSettings.Get(ProjectSettings.UseDebanding);
        try
        {
            ProjectSettings.Set(ProjectSettings.UseDebanding, true);
            using var root = new Window(); using var offscreen = new SubViewport(); root.ApplyProjectDebanding();
            Check(root.UseDebanding && !offscreen.UseDebanding, "Root project default is independent of offscreen authoring.");
            root.UseDebanding = false; root.ApplyProjectDebanding();
            Check(!root.UseDebanding, "Explicit root selection wins.");
        }
        finally { ProjectSettings.Set(ProjectSettings.UseDebanding, previous); }
        Reject<InvalidOperationException>(() => RenderingServer.ViewportSetUseDebanding(default, true));
        view.Dispose(); Reject<ObjectDisposedException>(() => view.UseDebanding = true);
        Reject<ObjectDisposedException>(() => _ = view.UseDebanding);
        var folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "electron-debanding-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            ResourceSaver.Save(packed, System.IO.Path.Combine(folder, "scene.e2dscene"));
            using (var settings = new ProjectSettingsRegistry(folder))
            { settings.Set(ProjectSettings.UseDebanding, false); settings.SetFeatureOverride(ProjectSettings.UseDebanding, "dotnet", true); settings.Save(); }
            var info = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true };
            info.ArgumentList.Add(typeof(CanvasDebandingTests).Assembly.Location); info.ArgumentList.Add("--debanding-storage"); info.ArgumentList.Add(folder);
            info.Environment.Remove("ELECTRON2D_TEST_DEBANDING");
            using var child = Process.Start(info)!; var output = child.StandardOutput.ReadToEnd(); var error = child.StandardError.ReadToEnd();
            Check(child.WaitForExit(30000) && child.ExitCode == 0, $"Fresh debanding storage: {output} {error}");
        }
        finally { Directory.Delete(folder, true); }
        Console.WriteLine("Debanding managed: defaults, packed scenes, root precedence, fresh scene/project feature files, inactive/disposed guards passed.");
    }

    internal static void ReadStorage(string folder)
    {
        using var scene = ResourceLoader.Load<PackedScene>(System.IO.Path.Combine(folder, "scene.e2dscene"), ResourceLoader.CacheMode.Ignore);
        using var view = (SubViewport)scene.Instantiate();
        using var settings = new ProjectSettingsRegistry(folder); settings.Load();
        Check(view.UseDebanding && !settings.Get(ProjectSettings.UseDebanding) && settings.GetWithOverride(ProjectSettings.UseDebanding), "Fresh files retain typed bool and active feature override.");
    }

    private sealed class Guard(Window root) : Entity
    {
        internal bool Armed;
        protected override void OnDraw()
        {
            if (!Armed) return;
            root.UseDebanding = root.UseDebanding;
            Reject<InvalidOperationException>(() => RenderingServer.ViewportSetUseDebanding(root.GetViewportRID(), true));
        }
    }

    internal static void Native(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        var root = new Window { Size = new(128, 64), UseHDR2D = backend == "gpu", TransparentBG = true };
        if (backend == "compatibility")
        {
            root.UseDebanding = true;
            Reject<NotSupportedException>(() => Engine.Run(root));
            Check(root.IsDisposed && RenderingServer.Service is null, "Unsupported dithering tears down native state.");
            Console.WriteLine("Compatibility debanding capability rejection passed."); return;
        }
        using var image = Image.CreateEmpty(128, 64, false, Image.Format.Rgbaf);
        for (var y = 0; y < 64; y++) for (var x = 0; x < 128; x++)
                image.SetPixel(x, y, new(x < 112 ? .012f + x * .0001f : x < 120 ? -1 : 2, .031f, .12f, y < 32 ? 1 : .4f));
        using var source = ImageTexture.CreateFromImage(image);
        root.AddChild(new Sprite { Texture = source, Centered = false });
        var guard = new Guard(root); root.AddChild(guard);
        using var child = new SubViewport { Size = new(16, 16), UseHDR2D = true, UseDebanding = true, RenderTargetUpdateMode = ViewportUpdateMode.Always };
        root.AddChild(child); child.AddChild(new Sprite { Texture = source, Centered = false });
        using var texture = root.GetTexture(); using var childTexture = child.GetTexture();
        var completed = false;
        root.Ready += _ =>
        {
            Check(root.UseDebanding, "Engine startup applies active project debanding defaults."); root.UseDebanding = false;
            var rid = root.GetViewportRID(); var renderer = RenderingServer.Service!;
            Reject<ArgumentException>(() => RenderingServer.ViewportSetUseDebanding(default, true));
            using (var foreign = new SubViewport()) Reject<InvalidOperationException>(() => RenderingServer.ViewportSetUseDebanding(foreign.GetViewportRID(), true));
            Reject<InvalidOperationException>(() => Task.Run(() => root.UseDebanding = true).GetAwaiter().GetResult());
            Reject<InvalidOperationException>(() => Task.Run(() => RenderingServer.ViewportSetUseDebanding(rid, true)).GetAwaiter().GetResult());
            var owned = RenderingServer.ViewportCreate(); RenderingServer.ViewportSetUseDebanding(owned, true); RenderingServer.FreeRID(owned);
            Reject<ArgumentException>(() => RenderingServer.ViewportSetUseDebanding(owned, false));
            RenderingServer.ForceDraw(); using var baseline = renderer.Readback(); using var linear = texture.GetImage()!; using var childPixels = childTexture.GetImage()!;
            var baselineBytes = baseline.GetData(); var linearBytes = linear.GetData(); var childBytes = childPixels.GetData();
            RenderingServer.ViewportSetUseDebanding(rid, true);
            Check(!root.UseDebanding && renderer.ViewportDebanding(root), "Server override preserves authored getter.");
            RenderingServer.ForceDraw(); using var dithered = renderer.Readback();
            Verify(dithered, linear, baseline);
            using (var actual = texture.GetImage()!) Check(actual.GetData().AsSpan().SequenceEqual(linearBytes), "Linear root image is unchanged.");
            using (var actual = childTexture.GetImage()!) Check(actual.GetData().AsSpan().SequenceEqual(childBytes), "Offscreen HDR image is unchanged.");
            RenderingServer.ForceDraw(); using (var stable = renderer.Readback()) Check(stable.GetData().AsSpan().SequenceEqual(dithered.GetData()), "Stationary dithering does not shimmer between frames.");
            root.UseDebanding = false; Check(!renderer.ViewportDebanding(root), "Equal authored value republishes over server override.");
            RenderingServer.ForceDraw(); using (var restored = renderer.Readback()) Check(restored.GetData().AsSpan().SequenceEqual(baselineBytes), "Disabling restores exact presentation bytes.");
            root.UseHDR2D = false; RenderingServer.ForceDraw(); using var byteBaseline = renderer.Readback();
            root.UseDebanding = true; RenderingServer.ForceDraw();
            using (var actual = renderer.Readback()) Check(actual.GetData().AsSpan().SequenceEqual(byteBaseline.GetData()), "SDR canvases are not dithered.");
            root.UseHDR2D = true; root.MSAA2D = Viewport.MSAA.Samples4X;
            RenderingServer.ViewportAttachToScreen(rid, new(-13, -7, 154, 78));
            RenderingServer.ForceDraw(); using var scaled = renderer.Readback();
            Verify(scaled, linear, baseline, region: new(-13, -7, 154, 78));
            Check(scaled.Width == baseline.Width && scaled.Height == baseline.Height, "Fitted/clipped presentation retains screen size.");
            RenderingServer.ViewportAttachToScreen(rid, new(0, 0, 128, 64));
            guard.Armed = true; guard.QueueRedraw(); RenderingServer.ForceDraw(); guard.Armed = false;
            var device = RenderingServer.GetRenderingDevice()!; var alias = RenderingServer.TextureGetRDTexture(texture.GetRID());
            for (var i = 0; i < 24; i++) { root.UseDebanding = (i & 1) != 0; RenderingServer.ForceDraw(); }
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 64; i++) { RenderingServer.ViewportSetUseDebanding(rid, (i & 1) != 0); RenderingServer.ForceDraw(); }
            var active = GC.GetAllocatedBytesForCurrentThread() - before;
            before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 64; i++) RenderingServer.ForceDraw();
            var idle = GC.GetAllocatedBytesForCurrentThread() - before;
            Check(active == 0 && idle == 0, $"Prepared presentation allocated active={active}, unchanged={idle} owner bytes.");
            Check(RenderingServer.TextureGetRDTexture(texture.GetRID()) == alias && device.TextureIsValid(alias), "Dither toggles preserve native HDR storage.");
            root.UseDebanding = true; RenderingServer.ForceDraw();
            using var final = renderer.Readback(); Verify(final, linear, baseline);
            var folder = Environment.GetEnvironmentVariable("ELECTRON2D_DEBANDING_OUTPUT");
            if (folder is not null) { Directory.CreateDirectory(folder); baseline.SavePNG(System.IO.Path.Combine(folder, "disabled.png")); final.SavePNG(System.IO.Path.Combine(folder, "enabled.png")); scaled.SavePNG(System.IO.Path.Combine(folder, "scaled.png")); }
            Console.WriteLine($"Debanding GPU: real SDR/linear/offscreen pixels, stable RGB dither, clipped scaling/MSAA, author/server/identity/thread/capture guards, 24 warm + 64 changed + 64 unchanged ForceDraw, active={active}, unchanged={idle} owner bytes.");
            completed = true; root.Tree!.Quit();
        };
        var projectDefault = ProjectSettings.Get(ProjectSettings.UseDebanding);
        try { ProjectSettings.Set(ProjectSettings.UseDebanding, true); Check(Engine.Run(root) == 0 && completed && root.IsDisposed, "Debanding native host completed and cleaned up."); }
        finally { ProjectSettings.Set(ProjectSettings.UseDebanding, projectDefault); }
    }

    private static void Verify(Image screen, Image linear, Image baseline, Rect2? region = null)
    {
        var changed = 0; var exact = 0;
        for (var y = 0; y < screen.Height; y++) for (var x = 0; x < screen.Width; x++)
            {
                var rect = region ?? new Rect2(0, 0, screen.Width, screen.Height);
                var sx = Math.Clamp((int)((x + .5f - rect.Position.X) / rect.Size.X * linear.Width), 0, linear.Width - 1);
                var sy = Math.Clamp((int)((y + .5f - rect.Position.Y) / rect.Size.Y * linear.Height), 0, linear.Height - 1);
                var value = linear.GetPixel(sx, sy); var actual = screen.GetPixel(x, y); var original = baseline.GetPixel(x, y);
                var phase = 171f * (x + .5f) + 231f * (y + .5f);
                float Expected(float v, float divisor)
                {
                    var encoded = v <= .0031308f ? v * 12.92f : 1.055f * MathF.Pow(v, 1 / 2.4f) - .055f;
                    var noise = (phase / divisor - MathF.Floor(phase / divisor) - .5f) / 255;
                    return Math.Clamp(MathF.Round((encoded + noise) * 255), 0, 255);
                }
                var errorR = MathF.Abs(actual.R * 255 - Expected(value.R, 103));
                var errorG = MathF.Abs(actual.G * 255 - Expected(value.G, 71));
                var errorB = MathF.Abs(actual.B * 255 - Expected(value.B, 97));
                Check(errorR <= 1 && errorG <= 1 && errorB <= 1, "Presentation follows an independent final-quantization RGB oracle.");
                if (errorR < .01f && errorG < .01f && errorB < .01f) exact++;
                Check(MathF.Abs(actual.A - value.A) <= 1f / 255, "Dithering preserves source alpha.");
                if (actual != original) changed++;
            }
        Check(exact >= screen.Width * screen.Height * .99, $"At least 99% of pixels exactly match all RGB oracle channels: {exact} / {screen.Width * screen.Height}.");
        Check(changed > screen.Width * screen.Height / 4, $"Dithering changes enough quantization decisions: {changed}.");
    }
}
