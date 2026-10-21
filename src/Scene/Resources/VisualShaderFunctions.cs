namespace Electron2D;

internal static class VisualShaderFunctions
{
    internal static string Expression(string function, string input) => function switch
    {
        "Negate" => $"(-({input}))",
        "Reciprocal" => $"(1.0 / ({input}))",
        "OneMinus" => $"(1.0 - ({input}))",
        "Saturate" => $"clamp({input}, 0.0, 1.0)",
        "RoundEven" => $"roundEven({input})",
        _ => $"{function.ToLowerInvariant()}({input})"
    };
}
