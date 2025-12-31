namespace Electron2D;

/// <summary>
/// Идентификаторы счётчиков (целочисленных), накапливаемых внутри одного кадра.
/// </summary>
public enum ProfilerCounterId : int
{
    // Events / Input
    EventsEngineRead = 0,
    EventsWindowRead,
    EventsInputRead,
    EventsDroppedEngine,
    EventsDroppedWindow,
    InputDroppedEvents,

    // Simulation
    FixedSteps,

    // Render (frame-local)
    RenderSprites,
    RenderDrawCalls,
    RenderDebugLines,
    RenderTextureBinds,
    RenderUniqueTextures,
    RenderSortTriggered,
    RenderSortCommands,
    RenderTextureColorMods,
    RenderTextureAlphaMods,
    RenderClears,
    RenderPresents,

    Count
}