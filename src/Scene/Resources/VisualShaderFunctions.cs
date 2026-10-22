namespace Electron2D;

internal static class VisualShaderFunctions
{
    internal static string IntegerOperation(VisualShaderIntegerOperator operation, IReadOnlyList<string> inputs) => operation switch
    {
        VisualShaderIntegerOperator.Add => $"({inputs[0]} + {inputs[1]})",
        VisualShaderIntegerOperator.Subtract => $"({inputs[0]} - {inputs[1]})",
        VisualShaderIntegerOperator.Multiply => $"({inputs[0]} * {inputs[1]})",
        VisualShaderIntegerOperator.Divide => $"({inputs[0]} / {inputs[1]})",
        VisualShaderIntegerOperator.Modulo => $"({inputs[0]} % {inputs[1]})",
        VisualShaderIntegerOperator.Max => $"max({inputs[0]}, {inputs[1]})",
        VisualShaderIntegerOperator.Min => $"min({inputs[0]}, {inputs[1]})",
        VisualShaderIntegerOperator.BitwiseAnd => $"({inputs[0]} & {inputs[1]})",
        VisualShaderIntegerOperator.BitwiseOr => $"({inputs[0]} | {inputs[1]})",
        VisualShaderIntegerOperator.BitwiseXor => $"({inputs[0]} ^ {inputs[1]})",
        VisualShaderIntegerOperator.BitwiseLeftShift => $"({inputs[0]} << {inputs[1]})",
        VisualShaderIntegerOperator.BitwiseRightShift => $"({inputs[0]} >> {inputs[1]})",
        _ => throw new InvalidOperationException("Invalid integer operation.")
    };

    internal static string GenerateExpression(string function, string input) => function switch
    {
        "Negate" => $"(-({input}))",
        "Reciprocal" => $"(1.0 / ({input}))",
        "OneMinus" => $"(1.0 - ({input}))",
        "Saturate" => $"clamp({input}, 0.0, 1.0)",
        "RoundEven" => $"roundEven({input})",
        _ => $"{function.ToLowerInvariant()}({input})"
    };
}
