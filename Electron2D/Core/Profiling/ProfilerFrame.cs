namespace Electron2D;

/// <summary>
/// Снапшот профайлера за последний завершённый кадр.
/// </summary>
public readonly struct ProfilerFrame
{
    public bool IsValid { get; init; }

    public long FrameIndex { get; init; }
    public double FrameMs { get; init; }

    // allocations / GC deltas for the frame (main thread)
    public long AllocatedBytes { get; init; }
    public int Gen0Collections { get; init; }
    public int Gen1Collections { get; init; }
    public int Gen2Collections { get; init; }

    // timings (ms)
    public double EventsPumpMs { get; init; }
    public double InputPollMs { get; init; }
    public double EventsSwapMs { get; init; }
    public double HandleQuitCloseMs { get; init; }
    public double DispatchInputMs { get; init; }
    public double FixedStepMs { get; init; }
    public double ProcessMs { get; init; }
    public double FlushFreeQueueMs { get; init; }

    public double RenderBeginFrameMs { get; init; }
    public double RenderBuildQueueMs { get; init; }
    public double RenderSortMs { get; init; }
    public double RenderFlushMs { get; init; }
    public double RenderPresentMs { get; init; }

    // counters
    public int EventsEngineRead { get; init; }
    public int EventsWindowRead { get; init; }
    public int EventsInputRead { get; init; }
    public int EventsDroppedEngine { get; init; }
    public int EventsDroppedWindow { get; init; }
    public int InputDroppedEvents { get; init; }

    public int FixedSteps { get; init; }

    public int RenderSprites { get; init; }
    public int RenderDrawCalls { get; init; }
    public int RenderDebugLines { get; init; }
    public int RenderTextureBinds { get; init; }
    public int RenderUniqueTextures { get; init; }
    public int RenderSortTriggered { get; init; }
    public int RenderSortCommands { get; init; }
    public int RenderTextureColorMods { get; init; }
    public int RenderTextureAlphaMods { get; init; }
    public int RenderClears { get; init; }
    public int RenderPresents { get; init; }

    public double RenderTotalMs =>
        RenderBeginFrameMs + RenderBuildQueueMs + RenderSortMs + RenderFlushMs + RenderPresentMs;

    public override string ToString()
    {
        if (!IsValid) return "<ProfilerFrame: invalid>";

        return
            $"Frame#{FrameIndex} {FrameMs:F2}ms | alloc={(AllocatedBytes / 1024.0):F1}KB GC(0/1/2)={Gen0Collections}/{Gen1Collections}/{Gen2Collections}\n" +
            $"Events: pump={EventsPumpMs:F2}ms swap={EventsSwapMs:F2}ms read(e/w/i)={EventsEngineRead}/{EventsWindowRead}/{EventsInputRead} drop(e/w/in)={EventsDroppedEngine}/{EventsDroppedWindow}/{InputDroppedEvents}\n" +
            $"Sim: fixedSteps={FixedSteps} fixed={FixedStepMs:F2}ms process={ProcessMs:F2}ms\n" +
            $"Render: {RenderTotalMs:F2}ms (begin={RenderBeginFrameMs:F2} build={RenderBuildQueueMs:F2} sort={RenderSortMs:F2} flush={RenderFlushMs:F2} present={RenderPresentMs:F2})\n" +
            $"       sprites={RenderSprites} drawCalls={RenderDrawCalls} binds={RenderTextureBinds} uniqTex={RenderUniqueTextures} gridLines={RenderDebugLines} sortTrig={RenderSortTriggered} sortCmds={RenderSortCommands}";
    }
}