using System.Reflection;
using Electron2D;
using SDL3;
using static TextureArrayTests;

internal static class GPUFrameQueueTests
{
    private const BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;
    private delegate bool ConfigureQueue(nint device, uint frames);
    private delegate nint SubmitFence(nint command);
    private delegate bool WaitFences(nint device, bool all, nint fences, uint count);
    private delegate void ReleaseFence(nint device, nint fence);

    private static readonly List<(nint Device, uint Depth)> Configurations = [];
    private static readonly List<nint> Submitted = [];
    private static readonly List<nint> Waited = [];
    private static readonly List<nint> Released = [];
    private static bool _measure;

    private sealed class Hook : IDisposable
    {
        private readonly FieldInfo _field;
        private readonly object _original;

        internal Hook(string field, string replacement)
        {
            _field = typeof(SDL).GetField(field, PrivateStatic)!;
            _original = _field.GetValue(null)!;
            var method = typeof(GPUFrameQueueTests).GetMethod(replacement, PrivateStatic)!;
            _field.SetValue(null, method.CreateDelegate(_field.FieldType));
        }

        public void Dispose() => _field.SetValue(null, _original);
    }

    private static readonly ConfigureQueue ConfigureNative = Native<ConfigureQueue>("SDL_SetGPUAllowedFramesInFlight", typeof(nint), typeof(uint));
    private static readonly SubmitFence SubmitNative = Native<SubmitFence>("SDL_SubmitGPUCommandBufferAndAcquireFence", typeof(nint));
    private static readonly WaitFences WaitNative = Native<WaitFences>("SDL_WaitForGPUFences", typeof(nint), typeof(bool), typeof(nint), typeof(uint));
    private static readonly ReleaseFence ReleaseNative = Native<ReleaseFence>("SDL_ReleaseGPUFence", typeof(nint), typeof(nint));

    private static T Native<T>(string name, params Type[] parameters) where T : Delegate =>
        typeof(SDL).GetMethod(name, PrivateStatic, null, parameters, null)!.CreateDelegate<T>();

    private static bool Configure(nint device, uint frames)
    {
        var result = ConfigureNative(device, frames);
        if (result) Configurations.Add((device, frames));
        return result;
    }

    private static nint Submit(nint command)
    {
        var fence = SubmitNative(command);
        if (_measure) Submitted.Add(fence);
        return fence;
    }

    private static unsafe bool Wait(nint device, bool all, nint fences, uint count)
    {
        var result = WaitNative(device, all, fences, count);
        if (_measure)
        {
            Check(result && all && count == 1, "The renderer waits for one oldest frame fence.");
            Waited.Add(*(nint*)fences);
        }
        return result;
    }

    private static void Release(nint device, nint fence)
    {
        ReleaseNative(device, fence);
        if (_measure) Released.Add(fence);
    }

    private sealed class Patch : Entity
    {
        protected override void OnDraw() => DrawRect(new(4, 4, 20, 20), Colors.Red);
    }

    internal static void Run(string driver)
    {
        var setting = ProjectSettings.FrameQueueSize;
        Check(ProjectSettings.Get(setting) == 2, "The default frame queue size is two.");
        foreach (var invalid in new[] { int.MinValue, 0, 4, int.MaxValue })
            Reject<ArgumentOutOfRangeException>(() => ProjectSettings.Set(setting, invalid));
        Check(ProjectSettings.Get(setting) == 2, "Invalid depth leaves the previous setting intact.");
        if (driver == "managed")
        {
            Console.WriteLine("GPU frame queue setting validation passed.");
            return;
        }

        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var configure = new Hook("SetGPUAllowedFramesInFlightNativeFunction", nameof(Configure));
        using var submit = new Hook("SubmitGPUCommandBufferAndAcquireFenceNativeFunction", nameof(Submit));
        using var wait = new Hook("WaitForGPUFencesPointerNativeFunction", nameof(Wait));
        using var release = new Hook("ReleaseGPUFenceNativeFunction", nameof(Release));
        RenderingDevice? retained = null;
        try
        {
            foreach (var depth in new[] { 1, 3, 2 })
            {
                ProjectSettings.Set(setting, depth);
                RenderingDevice? renderer = null;
                var root = new Window { Size = new(64, 64) };
                root.AddChild(new Patch());
                root.Ready += _ =>
                {
                    renderer = RenderingServer.GetRenderingDevice()!;
                    retained ??= renderer.CreateLocalDevice();
                    Check(RenderingServer.GetCurrentRenderingDriverName() == driver, "The requested GPU driver is active.");
                    Check(renderer.GetFrameDelay() == depth && retained.GetFrameDelay() == 1, "Renderer and local capacities belong to their submission paths.");
                    Check(Configurations[^1] == (Configurations[0].Device, (uint)depth), "SDL accepted the current session policy on the retained physical device.");
                    Reject<InvalidOperationException>(() => Task.Run(renderer.GetFrameDelay).GetAwaiter().GetResult());
                    ProjectSettings.Set(setting, depth == 3 ? 1 : 3);
                    Check(renderer.GetFrameDelay() == depth, "A running renderer uses its startup snapshot.");

                    Submitted.Clear(); Waited.Clear(); Released.Clear();
                    _measure = true;
                    try
                    {
                        var interrupted = (Action)(() => throw new InvalidOperationException("test interrupted frame"));
                        for (var frame = 0; frame < depth + 4; frame++)
                        {
                            if (frame == 1)
                            {
                                RenderingServer.FramePreDraw += interrupted;
                                try { Reject<InvalidOperationException>(() => RenderingServer.ForceDraw(swapBuffers: false)); }
                                finally { RenderingServer.FramePreDraw -= interrupted; }
                            }
                            else RenderingServer.ForceDraw(swapBuffers: frame >= depth + 1);
                            Check(Submitted.Count == frame + 1 && Submitted[^1] != 0, "Each frame owns one native fence.");
                            var retired = Math.Max(0, frame + 1 - depth);
                            Check(Waited.Count == retired && Released.Count == retired &&
                                Waited.SequenceEqual(Submitted.Take(retired)) && Released.SequenceEqual(Waited),
                                "The renderer waits and releases exactly the oldest fence at capacity.");
                        }
                    }
                    finally { _measure = false; }

                    using var texture = root.GetTexture();
                    using var pixels = texture.GetImage()!;
                    Check(pixels.GetPixel(12, 12) == Colors.Red, "Window pixels survive queued, interrupted and presented frames.");

                    var buffer = retained.StorageBufferCreate(4, [1, 2, 3, 4]);
                    retained.Submit();
                    Reject<InvalidOperationException>(retained.Submit);
                    retained.Sync();
                    Check(retained.BufferGetData(buffer).AsSpan().SequenceEqual(new byte[] { 1, 2, 3, 4 }), "The local device completes its own submission.");
                    retained.FreeRID(buffer);
                    root.Tree!.Quit();
                };
                Check(Engine.Run(root) == 0 && renderer is not null, "The renderer session completed.");
                Reject<ObjectDisposedException>(() => renderer!.GetFrameDelay());
                Check(retained!.GetFrameDelay() == 1, "The local device survives renderer retirement.");
            }
            Check(Configurations.Select(c => c.Depth).SequenceEqual(new uint[] { 1, 3, 2 }), "Each startup applies its policy once.");
        }
        finally { retained?.Dispose(); }
        Console.WriteLine($"GPU frame queue verified ({driver}, depths 1, 3 and 2).");
    }
}
