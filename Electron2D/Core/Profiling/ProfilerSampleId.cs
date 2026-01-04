namespace Electron2D;

/// <summary>
/// Идентификаторы CPU-сэмплов (длительности), накапливаемых внутри одного кадра.
/// </summary>
public enum ProfilerSampleId : int
{
    Frame = 0,

    // Engine pipeline
    EventsPump,
    InputPoll,
    EventsSwap,
    HandleQuitClose,
    SceneDispatchInput,
    SceneFixedStep,
    SceneProcess,
    SceneFlushFreeQueue,

    // Render pipeline
    RenderBeginFrame,
    RenderBuildQueue,
    RenderSort,
    RenderFlush,
    RenderPresent,
    
    Animation,

    /// <summary>
    /// Служебный маркер: количество элементов enum (не является реальным сэмплом).
    /// </summary>
    Count
}