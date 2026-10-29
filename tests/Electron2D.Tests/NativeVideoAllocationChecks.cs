using Electron2D;
using System.Runtime.CompilerServices;

internal static unsafe class NativeVideoAllocationChecks
{
    private static void Check(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
    internal static void ColdWindow(string backend, string path)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        var window = new Window
        {
            Size = new(66, 50),
            ContentScaleSize = new(66, 50),
            ContentScaleMode = ContentScaleMode.Viewport,
            Title = "Electron2D video cold start"
        };
        window.Ready += _ => window.Tree!.Quit();
        Check(Engine.Run(window, path, 59) == 0 && window.IsDisposed && new FileInfo(path).Length > 0,
            "Cold window/encoder startup failed.");
        Console.WriteLine("Window-first startup loaded the common native allocator before SDL and finalized OGV output.");
    }
    internal static void Run(Func<(long Current, long Peak)> read, string file,
        delegate* unmanaged[Cdecl]<nuint, void> arm, delegate* unmanaged[Cdecl]<nuint> attempts, bool old)
    {
        using var source = new VideoStreamTheora { File = file };
        if (old)
        {
            var baseline = read().Current;
            try { using var unexpected = source.InstantiatePlayback(); throw new InvalidOperationException("An older video bridge was accepted."); }
            catch (PlatformNotSupportedException) { Check(read().Current == baseline, "Rejected bridge published native storage."); }
            Console.WriteLine("Older video bridge rejected before decoder publication."); return;
        }
        long baselineCurrent;
        using (var warm = source.InstantiatePlayback()) { warm.Play(); warm.Seek(.1); warm.Update(.001); warm.Stop(); }
        baselineCurrent = read().Current;
        nuint count = 0;
        if (arm != null)
        {
            arm(0);
            using (var warm = source.InstantiatePlayback()) count = attempts();
            Check(count > 5, "Video bridge allocations are absent.");
            for (nuint position = 1; position <= count; ++position)
            {
                arm(position);
                try { using var unexpected = source.InstantiatePlayback(); throw new InvalidOperationException("Expected video storage refusal."); }
                catch (OutOfMemoryException) { }
                Check(read().Current == baselineCurrent, "Failed preparation leaked native storage.");
                arm(0); using (var retry = source.InstantiatePlayback()) Check(retry.GetLength() > 0, "Healthy preparation retry failed.");
                Check(read().Current == baselineCurrent, "Preparation retry did not drain.");
            }
            arm(0);
        }
        using (var first = source.InstantiatePlayback())
        {
            var retained = read().Current;
            Check(retained > baselineCurrent, "Decoder ownership is uncounted.");
            using (var second = source.InstantiatePlayback()) Check(read().Current > retained, "Independent decoder ownership is absent.");
            Check(read().Current == retained, "Independent decoder release changed the retained owner.");
            var texture = first.GetTexture(); first.Play(); first.Seek(.1);
            var time = first.GetPlaybackPosition();
            if (arm != null)
            {
                arm(0); first.SetAudioTrack(-1); var replacementCount = attempts(); first.SetAudioTrack(0);
                for (nuint position = 1; position <= replacementCount; ++position)
                {
                    arm(position);
                    try { first.SetAudioTrack(-1); throw new InvalidOperationException("Expected replacement storage refusal."); }
                    catch (OutOfMemoryException) { }
                    Check(first.GetPlaybackPosition() == time && ReferenceEquals(texture, first.GetTexture()) &&
                        read().Current == retained && first.GetChannels() > 0, "Failed track replacement changed live playback.");
                    arm(0);
                }
            }
            first.SetAudioTrack(-1); first.SetAudioTrack(0);
            for (int i = 0; i < 1000; ++i) Prepared(first);
            var before = read(); var allocated = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 20000; ++i) Prepared(first);
            var bytes = GC.GetAllocatedBytesForCurrentThread() - allocated; var after = read();
            Check(bytes == 0 && before.Current == after.Current && before.Peak == after.Peak, $"Prepared video allocated {bytes} managed bytes or changed native ownership.");
        }
        Check(read().Current == baselineCurrent, "Playback storage did not drain.");
        var abandoned = Abandon(source);
        for (int i = 0; i < 3; ++i) { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); }
        Check(!abandoned.IsAlive && read().Current == baselineCurrent, "Collected playback did not release its decoder.");
        Console.WriteLine($"Video bridge: {count} preparation refusal positions; independent owners, atomic track replacement, healthy retries and 20000 prepared operations passed with zero managed/native growth.");
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference Abandon(VideoStreamTheora source) => new(source.InstantiatePlayback());
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Prepared(VideoStreamPlayback playback)
    {
        playback.Seek(.05); playback.Update(1d / 120);
        _ = playback.GetTexture(); _ = playback.GetChannels(); _ = playback.GetPlaybackPosition();
    }
}
