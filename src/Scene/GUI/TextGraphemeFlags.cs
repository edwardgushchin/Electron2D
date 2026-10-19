namespace Electron2D;

/// <summary>Describes shaped grapheme properties independently of native shaping flags.</summary>
[Flags]
public enum TextGraphemeFlags
{
    /// <summary>No grapheme properties.</summary>
    None = 0,
    /// <summary>A mandatory line separator.</summary>
    BreakHard = 16,
    /// <summary>A soft line opportunity on whitespace, a soft hyphen or a virtual word boundary.</summary>
    BreakSoft = 32,
    /// <summary>The native shaper marks this cluster as unsafe to break.</summary>
    Connected = 1024,
    /// <summary>An actual or virtual Arabic elongation opportunity.</summary>
    Elongation = 128,
    /// <summary>A complete caller-authored inline object.</summary>
    EmbeddedObject = 4096,
    /// <summary>Punctuation under the active custom or pinned Unicode policy.</summary>
    Punctuation = 256,
    /// <summary>The resolved run advances right to left.</summary>
    RTL = 2,
    /// <summary>The native shaper permits insertion of an Arabic elongation glyph.</summary>
    SafeToInsertTatweel = 2048,
    /// <summary>A source soft-hyphen cluster.</summary>
    SoftHyphen = 8192,
    /// <summary>Source whitespace or a virtual word-space opportunity.</summary>
    Space = 8,
    /// <summary>A horizontal or vertical tabulation source unit.</summary>
    Tab = 64,
    /// <summary>A source underscore, independently of punctuation.</summary>
    Underscore = 512,
    /// <summary>A resolved native glyph or an explicit inline object.</summary>
    Valid = 1,
    /// <summary>A synthesized shaping or justification record.</summary>
    Virtual = 4,
}
