namespace Electron2D;

/// <summary>Describes shaped grapheme properties independently of native shaping flags.</summary>
[Flags]
public enum TextGraphemeFlags
{
    /// <summary>No grapheme properties.</summary>
    None = 0,
    /// <summary>break hard.</summary>
    BreakHard = 16,
    /// <summary>break soft.</summary>
    BreakSoft = 32,
    /// <summary>connected.</summary>
    Connected = 1024,
    /// <summary>elongation.</summary>
    Elongation = 128,
    /// <summary>embedded object.</summary>
    EmbeddedObject = 4096,
    /// <summary>punctuation.</summary>
    Punctuation = 256,
    /// <summary>rtl.</summary>
    RTL = 2,
    /// <summary>safe to insert tatweel.</summary>
    SafeToInsertTatweel = 2048,
    /// <summary>soft hyphen.</summary>
    SoftHyphen = 8192,
    /// <summary>space.</summary>
    Space = 8,
    /// <summary>tab.</summary>
    Tab = 64,
    /// <summary>underscore.</summary>
    Underscore = 512,
    /// <summary>valid.</summary>
    Valid = 1,
    /// <summary>virtual.</summary>
    Virtual = 4,
}
