using System.Globalization;
using System.Runtime.InteropServices;

namespace Electron2D;

internal sealed unsafe partial class WindowsSpeech : INativeSpeech
{
    internal const ushort StartedEvent = 1, EndedEvent = 2, WordEvent = 5;
    private static readonly Guid VoiceClass = new("96749377-3391-11d2-9ee3-00c04f797396"), VoiceInterface = new("6c44df74-72b9-4992-a1ec-ef996e0422d4");
    private static readonly Guid CategoryClass = new("a910187f-0c7a-45ac-92cc-59edafb77b53"), CategoryInterface = new("2d3d3845-39af-4850-bbf9-40b49780011d");
    private const string VoiceCategory = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Speech\Voices";
    private nint _voice;
    private bool _ownsCOM;
    [StructLayout(LayoutKind.Sequential)]
    internal struct SpeechEvent
    {
        internal ushort ID, ParameterType;
        internal uint Stream;
        internal ulong AudioOffset;
        internal nuint WordLength;
        internal nint Position;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct SpeechStatus
    {
        internal uint CurrentStream, LastStream;
        internal int Result;
        internal uint State, WordPosition, WordLength, SentencePosition, SentenceLength;
        internal int Bookmark;
        internal ushort Phoneme;
        internal uint Viseme, Reserved1, Reserved2;
    }
    internal WindowsSpeech()
    {
        var result = CoInitializeEx(0, 0);
        _ownsCOM = result >= 0;
        if (!_ownsCOM && result != unchecked((int)0x80010106)) Check(result, "initialize speech COM");
        try
        {
            result = CoCreateInstance(in VoiceClass, 0, 1, in VoiceInterface, out _voice);
            if (result is unchecked((int)0x80040154) or unchecked((int)0x80004002)) throw new NotSupportedException("The Windows speech provider is not installed.");
            Check(result, "create the speech synthesizer");
            // The SDK SPFEI mask includes its flag-check bits as well as start/end/word events.
            Check(((delegate* unmanaged[Stdcall]<nint, ulong, ulong, int>)Methods(_voice)[10])(_voice, 0x240000026, 0x240000026), "select speech events");
        }
        catch { Dispose(); throw; }
    }
    public TTSVoice[] GetVoices()
    {
        var voices = new List<TTSVoice>(); var enumeration = OpenEnumeration();
        try
        {
            while (Next(enumeration, out var token))
            {
                nint attributes = 0;
                try
                {
                    var id = TokenID(token); var name = ReadString(token, null);
                    fixed (char* key = "Attributes") Check(((delegate* unmanaged[Stdcall]<nint, char*, nint*, int>)Methods(token)[9])(token, key, &attributes), "read voice attributes");
                    var localeText = ReadString(attributes, "Language"); var separator = localeText.IndexOf(';');
                    var locale = localeText.AsSpan(0, separator < 0 ? localeText.Length : separator);
                    var language = uint.TryParse(locale, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var code) && code != 0 ? LocalePart(code, 0x59) : "";
                    var region = code != 0 ? LocalePart(code, 0x5a) : "";
                    if (region.Length != 0 && language.Length != 0) language += "_" + region;
                    voices.Add(new(id, name.Length == 0 ? id : name, language));
                }
                finally { Release(attributes); Release(token); }
            }
            return voices.ToArray();
        }
        finally { Release(enumeration); }
    }
    private static string LocalePart(uint locale, uint part)
    {
        var text = stackalloc char[85]; var length = GetLocaleInfo(locale, part, text, 85);
        return length > 1 ? new(text, 0, length - 1) : "";
    }
    private static nint OpenEnumeration()
    {
        nint category = 0, enumeration = 0;
        try
        {
            Check(CoCreateInstance(in CategoryClass, 0, 1, in CategoryInterface, out category), "enumerate speech voices");
            fixed (char* id = VoiceCategory) Check(((delegate* unmanaged[Stdcall]<nint, char*, int, int>)Methods(category)[15])(category, id, 0), "open the voice category");
            Check(((delegate* unmanaged[Stdcall]<nint, nint, nint, nint*, int>)Methods(category)[18])(category, 0, 0, &enumeration), "read the voice collection");
            return enumeration;
        }
        catch { Release(enumeration); throw; }
        finally { Release(category); }
    }
    private static bool Next(nint enumeration, out nint token)
    {
        nint value = 0; uint count = 0;
        var result = ((delegate* unmanaged[Stdcall]<nint, uint, nint*, uint*, int>)Methods(enumeration)[3])(enumeration, 1, &value, &count);
        if (result < 0) { Release(value); Check(result, "advance the voice collection"); }
        if (count != 1) { Release(value); value = 0; }
        token = value; return count == 1;
    }
    private static string TokenID(nint token)
    {
        nint text = 0;
        try { Check(((delegate* unmanaged[Stdcall]<nint, nint*, int>)Methods(token)[16])(token, &text), "read a voice identifier"); return Marshal.PtrToStringUni(text) ?? ""; }
        finally { CoTaskMemFree(text); }
    }
    private static string ReadString(nint token, string? name)
    {
        nint text = 0;
        try
        {
            int result; fixed (char* key = name) result = ((delegate* unmanaged[Stdcall]<nint, char*, nint*, int>)Methods(token)[6])(token, key, &text);
            if (result == unchecked((int)0x80070002)) return "";
            Check(result, "read voice metadata"); return Marshal.PtrToStringUni(text) ?? "";
        }
        finally { CoTaskMemFree(text); }
    }
    public NativeSpeechText Prepare(string text, float pitch, string language) => NativeSpeechText.Windows(text, pitch);
    public bool IsAvailable => _voice != 0;
    public uint Speak(string voiceID, NativeSpeechText text, int volume, float pitch, float rate)
    {
        var enumeration = OpenEnumeration(); nint selected = 0;
        try
        {
            while (Next(enumeration, out var token))
            {
                try { if (TokenID(token) == voiceID) { selected = token; token = 0; break; } }
                finally { Release(token); }
            }
            if (selected == 0) throw new ArgumentException("The speech voice is no longer installed.", nameof(voiceID));
            Check(((delegate* unmanaged[Stdcall]<nint, nint, int>)Methods(_voice)[18])(_voice, selected), "select a speech voice");
            Check(((delegate* unmanaged[Stdcall]<nint, ushort, int>)Methods(_voice)[30])(_voice, (ushort)volume), "set speech volume");
            var nativeRate = Math.Clamp((int)(10 * Math.Log(rate) / Math.Log(3)), -10, 10);
            Check(((delegate* unmanaged[Stdcall]<nint, int, int>)Methods(_voice)[28])(_voice, nativeRate), "set speech rate");
            uint stream = 0;
            fixed (char* input = text.Content) Check(((delegate* unmanaged[Stdcall]<nint, char*, uint, uint*, int>)Methods(_voice)[20])(_voice, input, 9, &stream), "start speech synthesis");
            return stream;
        }
        finally { Release(selected); Release(enumeration); }
    }
    public bool IsSpeaking
    {
        get { var status = GetStatus(); return status.State is 0 or 2; }
    }
    public int LastResult => GetStatus().Result;
    private SpeechStatus GetStatus()
    {
        SpeechStatus status = default;
        Check(((delegate* unmanaged[Stdcall]<nint, SpeechStatus*, nint, int>)Methods(_voice)[22])(_voice, &status, 0), "query speech state");
        return status;
    }
    public void Pause() => Check(((delegate* unmanaged[Stdcall]<nint, int>)Methods(_voice)[16])(_voice), "pause speech");
    public void Resume() => Check(((delegate* unmanaged[Stdcall]<nint, int>)Methods(_voice)[17])(_voice), "resume speech");
    public void Purge() => Check(((delegate* unmanaged[Stdcall]<nint, nint, uint, nint, int>)Methods(_voice)[20])(_voice, 0, 2, 0), "stop speech");
    public bool TryGetEvent(out NativeSpeechEvent value)
    {
        uint count = 0; SpeechEvent raw = default; value = default;
        Check(((delegate* unmanaged[Stdcall]<nint, uint, SpeechEvent*, uint*, int>)Methods(_voice)[11])(_voice, 1, &raw, &count), "read speech events");
        if (count == 0) return false;
        try
        {
            var kind = raw.ID switch { StartedEvent => DisplayServer.TTSUtteranceEvent.Started, EndedEvent => DisplayServer.TTSUtteranceEvent.Ended, WordEvent => DisplayServer.TTSUtteranceEvent.Boundary, _ => (DisplayServer.TTSUtteranceEvent)(-1) };
            value = new(kind, raw.Stream, (long)raw.Position); return true;
        }
        finally { ClearEvent(raw); }
    }
    internal static void ClearEvent(SpeechEvent value)
    {
        if (value.ParameterType is 1 or 2) Release(value.Position);
        else if (value.ParameterType is 3 or 4) CoTaskMemFree(value.Position);
    }
    private static nint* Methods(nint value) => *(nint**)value;
    private static void Release(nint value) { if (value != 0) ((delegate* unmanaged[Stdcall]<nint, uint>)Methods(value)[2])(value); }
    private static void Check(int result, string operation) { if (result < 0) throw new IOException($"Windows could not {operation} (HRESULT 0x{result:X8}).", result); }
    public void Dispose()
    {
        var voice = _voice; _voice = 0;
        try { Release(voice); }
        finally { if (_ownsCOM) { _ownsCOM = false; CoUninitialize(); } }
    }
    [LibraryImport("ole32.dll"), DefaultDllImportSearchPaths(DllImportSearchPath.System32)] private static partial int CoInitializeEx(nint reserved, uint mode);
    [LibraryImport("ole32.dll"), DefaultDllImportSearchPaths(DllImportSearchPath.System32)] private static partial void CoUninitialize();
    [LibraryImport("ole32.dll"), DefaultDllImportSearchPaths(DllImportSearchPath.System32)] private static partial int CoCreateInstance(in Guid classID, nint outer, uint context, in Guid interfaceID, out nint value);
    [LibraryImport("ole32.dll"), DefaultDllImportSearchPaths(DllImportSearchPath.System32)] private static partial void CoTaskMemFree(nint value);
    [LibraryImport("kernel32.dll", EntryPoint = "GetLocaleInfoW"), DefaultDllImportSearchPaths(DllImportSearchPath.System32)] private static partial int GetLocaleInfo(uint locale, uint part, char* buffer, int count);
}
