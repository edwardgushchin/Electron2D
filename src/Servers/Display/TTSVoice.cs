namespace Electron2D;

/// <summary>Describes one installed native speech voice.</summary>
/// <remarks>Returned by DisplayServer.TTSGetVoices. The default value has empty fields.</remarks>
public readonly struct TTSVoice
{
    private readonly string? _id, _name, _language;
    internal TTSVoice(string id, string name, string language) { _id = id; _name = name; _language = language; }
    /// <summary>Gets the native identifier accepted by DisplayServer.TTSSpeak.</summary>
    /// <value>A provider-owned identifier, or empty for a default descriptor.</value>
    public string ID => _id ?? "";
    /// <summary>Gets the installed voice's display name.</summary>
    /// <value>Native display text, with its identifier used if no name is available.</value>
    public string Name => _name ?? "";
    /// <summary>Gets the native ISO language and optional variant.</summary>
    /// <value>A code such as en_US, or empty when the provider has no locale metadata.</value>
    public string Language => _language ?? "";
}
