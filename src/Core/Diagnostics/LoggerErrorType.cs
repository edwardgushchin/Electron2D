namespace Electron2D;

/// <summary>Identifies the origin and severity of a structured engine diagnostic.</summary>
public enum LoggerErrorType
{
    /// <summary>A general runtime error.</summary>
    Error = 0,
    /// <summary>A warning.</summary>
    Warning = 1,
    /// <summary>An error in compiled gameplay code.</summary>
    Script = 2,
    /// <summary>A shader error.</summary>
    Shader = 3
}
