using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Electron2D;
using Memory = Electron2D.NativeFontPrecision.FTMemory;

internal static unsafe partial class NativeFontAllocationTests
{
    private struct FaultState { internal Memory Normal; internal int FailAt, Calls, Failures; }
    internal static void Run()
    {
        using var source = typeof(Font).Assembly.GetManifestResourceStream("Electron2D.Fonts.OpenSans_SemiBold.woff2")!;
        var data = new byte[checked((int)source.Length)]; source.ReadExactly(data);
        using (var warm = new NativeFontPrecision(data))
            Check(warm.Rasterize(warm.GetGlyphIndex('A'), FontHinting.Normal, forceAutohinter: true).Width > 0, "Prepare process-lifetime automatic-hinting caches.");
        var baseline = NativeAllocation.Snapshot();
        using (var font = new NativeFontPrecision(data))
        {
            Check(OwnedMemory(font) != null && OwnedMemory(font)->Allocate != null, "Production face retains its own live allocator record.");
            Check(NativeAllocation.Snapshot().Current > baseline.Current + data.Length, "Actual FreeType-owned storage is counted beside the source bytes.");
            var glyph = font.GetGlyphIndex('A'); Check(glyph != 0, "The normal module set retains character mapping."); font.SetSize(24);
            Check(font.Rasterize(glyph, FontHinting.Normal, 0, 0).Width > 0, "The supplied allocator retains real glyph rendering.");
            var beforeAuto = NativeAllocation.Snapshot();
            Check(font.Rasterize(glyph, FontHinting.Normal, forceAutohinter: true).Width > 0, "Accounted automatic hinting retains real glyph rendering.");
            Check(NativeAllocation.Snapshot().Current > beforeAuto.Current, "Automatic hinting retains accounted native state with the face.");
            uint[] text = ['A', 'f', 'f', 'i'];
            for (var i = 0; i < 128; i++) _ = font.Shape(text, NativeTextDirection.LTR);
            AllocationWarmupCheck.Collect(); for (var i = 0; i < 128; i++) _ = font.Shape(text, NativeTextDirection.LTR);
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Check(font.Shape(text, NativeTextDirection.LTR).Length > 0, "Warmed native shaping remains executable.");
            Check(GC.GetAllocatedBytesForCurrentThread() == own && GC.GetTotalAllocatedBytes(true) == all, "128 warmed actual shaping calls allocate zero managed bytes.");
        }
        Check(NativeAllocation.Snapshot().Current == baseline.Current, "Production face/library/allocator teardown drains all tracked storage.");
        try { using var invalid = new NativeFontPrecision([0, 1, 2, 3]); throw new Exception("Invalid font unexpectedly opened."); }
        catch (InvalidOperationException) { }
        Check(NativeAllocation.Snapshot().Current == baseline.Current, "Failed production face construction retires the initialized modules and allocator.");
        var unhinted = ExerciseLibrary(data, 0, false);
        var hinted = ExerciseLibrary(data, 0, true);
        Check(unhinted > 0 && hinted > unhinted, "Both render modes use the allocator; hinting also allocates its interpreter state.");
        for (var failAt = 1; failAt <= unhinted; failAt++) _ = ExerciseLibrary(data, failAt, false);
        for (var failAt = 1; failAt <= hinted; failAt++) _ = ExerciseLibrary(data, failAt, true);
        Check(NativeAllocation.Snapshot().Current == baseline.Current, "All injected native allocation failures drain their accepted ownership.");
        var abandoned = Abandon(data); AllocationWarmupCheck.Collect();
        Check(!abandoned.IsAlive && NativeAllocation.Snapshot().Current == baseline.Current, "Finalizer teardown retains the callback record until library retirement completes.");
        Console.WriteLine($"FreeType allocation ownership: production lifetime/errors/finalization, {unhinted}/{hinted} unhinted/hinted native failure positions and 128 zero-managed-allocation shaping calls passed.");
    }
    private static int ExerciseLibrary(byte[] data, int failAt, bool hinted)
    {
        var before = NativeAllocation.Snapshot(); var state = new FaultState { Normal = Memory.Create(), FailAt = failAt };
        var memory = state.Normal; memory.User = &state; memory.Allocate = &Allocate; memory.Reallocate = &Reallocate;
        nint library = 0, face = 0;
        fixed (byte* bytes = data)
        {
            try
            {
                var error = NewLibrary(null, &memory, out library);
                if (error == 0)
                {
                    AddModules(null, library); SetProperties(null, library);
                    error = NewFace(null, library, bytes, new CLong(data.Length), new CLong(0), out face);
                    if (error == 0) error = SetSize(null, face, new CLong(0), new CLong(24 * 64), 72, 72);
                    if (error == 0) error = LoadGlyph(null, face, GlyphIndex(null, face, new CULong('A')), hinted ? 4 : 4 | 2);
                }
                if (failAt == 0) Check(error == 0 && face != 0, "Unrestricted native initialization, decoding and rendering succeeds.");
                else Check(state.Failures == 1, "Each requested native allocation failure actually occurred.");
            }
            finally
            {
                if (face != 0) Check(DoneFace(null, face) == 0, "Accepted face closes after native allocation failure.");
                if (library != 0) Check(DoneLibrary(null, library) == 0, "Modules/library close while their caller-owned callback record is live.");
            }
        }
        Check(NativeAllocation.Snapshot().Current == before.Current, $"Native allocation failure {failAt}, hinted={hinted}, leaves no tracked allocation.");
        return state.Calls;
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void* Allocate(Memory* memory, CLong size)
    {
        var state = (FaultState*)memory->User;
        if (++state->Calls == state->FailAt) { state->Failures++; return null; }
        return state->Normal.Allocate(memory, size);
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void* Reallocate(Memory* memory, CLong current, CLong size, void* block)
    {
        var state = (FaultState*)memory->User;
        if (++state->Calls == state->FailAt) { state->Failures++; return null; }
        return state->Normal.Reallocate(memory, current, size, block);
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference Abandon(byte[] data) => new(new NativeFontPrecision(data));
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_ftMemory")]
    private static extern ref Memory* OwnedMemory(NativeFontPrecision font);
    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "FTNewLibrary")] private static extern int NewLibrary(NativeFontPrecision? owner, Memory* memory, out nint library);
    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "FTAddDefaultModules")] private static extern void AddModules(NativeFontPrecision? owner, nint library);
    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "FTSetDefaultProperties")] private static extern void SetProperties(NativeFontPrecision? owner, nint library);
    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "FTNewMemoryFace")] private static extern int NewFace(NativeFontPrecision? owner, nint library, byte* data, CLong size, CLong index, out nint face);
    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "FTSetCharSize")] private static extern int SetSize(NativeFontPrecision? owner, nint face, CLong width, CLong height, uint horizontalDPI, uint verticalDPI);
    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "FTGetCharIndex")] private static extern uint GlyphIndex(NativeFontPrecision? owner, nint face, CULong code);
    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "FTLoadGlyph")] private static extern int LoadGlyph(NativeFontPrecision? owner, nint face, uint glyph, int flags);
    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "FTDoneFace")] private static extern int DoneFace(NativeFontPrecision? owner, nint face);
    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "FTDone")] private static extern int DoneLibrary(NativeFontPrecision? owner, nint library);
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
