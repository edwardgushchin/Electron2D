namespace Electron2D;

/// <summary>Selects how a dynamic body checks its path for collisions between discrete physics poses.</summary>
public enum CCDMode
{
    /// <summary>Uses discrete collision detection without a continuous trajectory test.</summary>
    Disabled = 0,
    /// <summary>Tests a leading support ray; narrow off-ray features and the body's own rotation can be missed.</summary>
    CastRay = 1,
    /// <summary>Tests the complete shape trajectory, including its rotation.</summary>
    CastShape = 2
}
