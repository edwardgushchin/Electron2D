namespace Electron2D;

/// <summary>Advisory property metadata for typed tooling consumers.</summary>
/// <remarks>Hints describe presentation and do not validate or clamp runtime values. Providers and consumers
/// retain their own executing controls; the enum does not create an editor or a resource selector.</remarks>
public enum PropertyHint
{
    /// <summary>Advisory array type metadata.</summary>
    ArrayType = 31,
    /// <summary>Advisory color no alpha metadata.</summary>
    ColorNoAlpha = 21,
    /// <summary>Advisory dictionary type metadata.</summary>
    DictionaryType = 38,
    /// <summary>Advisory dir metadata.</summary>
    Dir = 14,
    /// <summary>Advisory enum metadata.</summary>
    Enum = 2,
    /// <summary>Advisory enum suggestion metadata.</summary>
    EnumSuggestion = 3,
    /// <summary>Advisory expression metadata.</summary>
    Expression = 19,
    /// <summary>Advisory exp easing metadata.</summary>
    ExpEasing = 4,
    /// <summary>Advisory file metadata.</summary>
    File = 13,
    /// <summary>Advisory file path metadata.</summary>
    FilePath = 44,
    /// <summary>Advisory flags metadata.</summary>
    Flags = 6,
    /// <summary>Advisory global dir metadata.</summary>
    GlobalDir = 16,
    /// <summary>Advisory global file metadata.</summary>
    GlobalFile = 15,
    /// <summary>Advisory global save file metadata.</summary>
    GlobalSaveFile = 28,
    /// <summary>Advisory group enable metadata.</summary>
    GroupEnable = 42,
    /// <summary>Advisory input name metadata.</summary>
    InputName = 43,
    /// <summary>Advisory quaternion-value presentation for caller-provided typed data.</summary>
    HideQuaternionEdit = 35,
    /// <summary>Advisory int is pointer metadata.</summary>
    IntIsPointer = 30,
    /// <summary>Advisory layers 2d navigation metadata.</summary>
    Layers2DNavigation = 9,
    /// <summary>Advisory layers 2d physics metadata.</summary>
    Layers2DPhysics = 8,
    /// <summary>Advisory layers 2d render metadata.</summary>
    Layers2DRender = 7,
    /// <summary>Advisory layers avoidance metadata.</summary>
    LayersAvoidance = 37,
    /// <summary>Advisory link metadata.</summary>
    Link = 5,
    /// <summary>Advisory locale id metadata.</summary>
    LocaleID = 32,
    /// <summary>Advisory localizable string metadata.</summary>
    LocalizableString = 33,
    /// <summary>The nonselectable hint count.</summary>
    Max = 45,
    /// <summary>Advisory multiline text metadata.</summary>
    MultilineText = 18,
    /// <summary>Advisory node path valid types metadata.</summary>
    NodePathValidTypes = 26,
    /// <summary>Advisory node type metadata.</summary>
    NodeType = 34,
    /// <summary>Advisory none metadata.</summary>
    None = 0,
    /// <summary>Advisory object id metadata.</summary>
    ObjectID = 22,
    /// <summary>Advisory object too big metadata.</summary>
    ObjectTooBig = 25,
    /// <summary>Advisory oneshot metadata.</summary>
    OneShot = 40,
    /// <summary>Advisory password metadata.</summary>
    Password = 36,
    /// <summary>Advisory placeholder text metadata.</summary>
    PlaceholderText = 20,
    /// <summary>Advisory range metadata.</summary>
    Range = 1,
    /// <summary>Advisory resource type metadata.</summary>
    ResourceType = 17,
    /// <summary>Advisory save file metadata.</summary>
    SaveFile = 27,
    /// <summary>Advisory tool button metadata.</summary>
    ToolButton = 39,
    /// <summary>Advisory type string metadata.</summary>
    TypeString = 23,
}
