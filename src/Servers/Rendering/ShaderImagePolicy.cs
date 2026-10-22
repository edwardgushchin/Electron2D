namespace Electron2D;

internal readonly record struct ShaderImagePolicy(bool SourceColor, int Fallback, int Filter, int Repeat, bool Screen, string? GlobalName = null);
