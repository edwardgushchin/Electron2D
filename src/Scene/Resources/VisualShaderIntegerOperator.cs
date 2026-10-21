namespace Electron2D;

/// <summary>The integer operation shared by signed and unsigned numeric graph definitions.</summary>
/// <remarks>The input node determines signed interpretation. Division, ordering and right shifts retain that domain's native rules.</remarks>
public enum VisualShaderIntegerOperator
{
    /// <summary>Adds the two integer inputs.</summary>
    Add = 0,
    /// <summary>Subtracts the second integer input from the first.</summary>
    Subtract = 1,
    /// <summary>Multiplies the two integer inputs.</summary>
    Multiply = 2,
    /// <summary>Divides the first input by the second; signed division truncates toward zero.</summary>
    Divide = 3,
    /// <summary>Computes the native integer remainder; portable canvas execution requires nonnegative operands and a nonzero divisor.</summary>
    Modulo = 4,
    /// <summary>Selects the larger integer input.</summary>
    Max = 5,
    /// <summary>Selects the smaller integer input.</summary>
    Min = 6,
    /// <summary>Keeps bits set in both inputs.</summary>
    BitwiseAnd = 7,
    /// <summary>Keeps bits set in either input.</summary>
    BitwiseOr = 8,
    /// <summary>Keeps bits set in exactly one input.</summary>
    BitwiseXor = 9,
    /// <summary>Shifts the first input left by the second input, discarding high bits.</summary>
    BitwiseLeftShift = 10,
    /// <summary>Shifts right with sign extension for signed inputs and zero fill for unsigned inputs.</summary>
    BitwiseRightShift = 11,
    /// <summary>The nonselectable operator-count sentinel.</summary>
    EnumSize = 12,
}
