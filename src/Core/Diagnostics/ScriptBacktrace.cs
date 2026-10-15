using System.Diagnostics;
using System.Text;

namespace Electron2D;

/// <summary>An immutable snapshot of available compiled C# call-stack metadata.</summary>
/// <remarks>Snapshots own strings and have ordinary managed lifetime. Source locations require debug symbols;
/// filenames may be empty and line numbers zero. Trimmed runtimes may omit function metadata. No live variables,
/// reflected game objects, exception instances or native stack handles are retained.</remarks>
public sealed class ScriptBacktrace
{
    private readonly record struct Frame(string File, string Function, int Line);
    private readonly Frame[] _frames;
    private ScriptBacktrace(Frame[] frames) => _frames = frames;

    internal static ScriptBacktrace Capture(StackTrace stack)
    {
        var frames = stack.GetFrames();
        var result = new Frame[frames.Length];
        for (var i = 0; i < frames.Length; i++)
        {
            var frame = frames[i]; var function = "<unavailable>";
            var method = DiagnosticMethodInfo.Create(frame);
            if (method is not null) function = (method.DeclaringTypeName is { } type ? type + "." : "") + method.Name;
            result[i] = new(frame.GetFileName() ?? "", function, frame.GetFileLineNumber());
        }
        return new(result);
    }

    /// <summary>Gets the language represented by this snapshot.</summary>
    /// <returns>C#.</returns>
    public string GetLanguageName() => "C#";
    /// <summary>Gets the captured frame count.</summary>
    /// <returns>The number of retained metadata frames.</returns>
    public int GetFrameCount() => _frames.Length;
    /// <summary>Reports whether no frames were captured.</summary>
    /// <returns>True for an empty snapshot.</returns>
    public bool IsEmpty() => _frames.Length == 0;
    /// <summary>Gets a frame's source file.</summary>
    /// <param name="index">Zero-based frame index.</param>
    /// <returns>A source filename or an empty string when symbols are unavailable.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Index is outside this snapshot.</exception>
    public string GetFrameFile(int index) => Get(index).File;
    /// <summary>Gets a frame's available function identity.</summary>
    /// <param name="index">Zero-based frame index.</param>
    /// <returns>A function name, or an explicit unavailable marker.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Index is outside this snapshot.</exception>
    public string GetFrameFunction(int index) => Get(index).Function;
    /// <summary>Gets a frame's source line.</summary>
    /// <param name="index">Zero-based frame index.</param>
    /// <returns>A source line, or zero when symbols are unavailable.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Index is outside this snapshot.</exception>
    public int GetFrameLine(int index) => Get(index).Line;
    private Frame Get(int index) => (uint)index < (uint)_frames.Length ? _frames[index] : throw new ArgumentOutOfRangeException(nameof(index));

    /// <summary>Formats the captured language and frame metadata.</summary>
    /// <param name="indentAll">Spaces before every output line, from zero through 1024.</param>
    /// <param name="indentFrames">Additional spaces before frame lines, from zero through 1024.</param>
    /// <returns>A deterministic snapshot representation.</returns>
    /// <exception cref="ArgumentOutOfRangeException">An indentation is negative or greater than 1024.</exception>
    public string Format(int indentAll = 0, int indentFrames = 4)
    {
        if ((uint)indentAll > 1024) throw new ArgumentOutOfRangeException(nameof(indentAll));
        if ((uint)indentFrames > 1024) throw new ArgumentOutOfRangeException(nameof(indentFrames));
        var prefix = new string(' ', indentAll); var frames = prefix + new string(' ', indentFrames);
        var text = new StringBuilder(prefix).Append("C# backtrace:");
        for (var i = 0; i < _frames.Length; i++)
        {
            var f = _frames[i]; text.Append('\n').Append(frames).Append(i).Append(": ").Append(f.Function);
            if (f.File.Length != 0) text.Append(" (").Append(f.File).Append(':').Append(f.Line).Append(')');
        }
        return text.ToString();
    }
    /// <summary>Formats this snapshot with the default indentation.</summary>
    /// <returns>The same string as Format().</returns>
    public override string ToString() => Format();
}
