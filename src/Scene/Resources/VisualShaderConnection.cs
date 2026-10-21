namespace Electron2D;

/// <summary>A copied directed connection between two visual shader ports.</summary>
/// <param name="FromNode">The source node ID.</param>
/// <param name="FromPort">The zero-based source output.</param>
/// <param name="ToNode">The destination node ID, including the reserved output.</param>
/// <param name="ToPort">The zero-based destination input.</param>
public readonly record struct VisualShaderConnection(int FromNode, int FromPort, int ToNode, int ToPort);
