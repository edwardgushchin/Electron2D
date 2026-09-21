namespace Electron2D;

/// <summary>Specifies which scene-tree frame lane advances a <see cref="Timer"/>.</summary>
public enum TimerProcessCallback
{
    /// <summary>Advances the timer during fixed-step physics-process frames.</summary>
    Physics = 0,

    /// <summary>Advances the timer during variable-step process frames.</summary>
    Idle = 1
}
