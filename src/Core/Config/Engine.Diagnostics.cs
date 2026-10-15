using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Electron2D;

public sealed partial class Engine
{
    /// <summary>Gets or sets whether physical engine stdout messages are printed.</summary>
    /// <value>True initially. Registered loggers still receive messages when false.</value>
    /// <remarks>Applies to engine ingress and an attached native log stream; arbitrary host Console writes
    /// retain ordinary C# behavior. Changing the flag does not initialize a native window.</remarks>
    public static bool PrintToSTDOUT { get => RuntimeDiagnostics.Instance.PrintToSTDOUT; set => RuntimeDiagnostics.Instance.PrintToSTDOUT = value; }

    /// <summary>Gets or sets whether physical engine stderr diagnostics are printed.</summary>
    /// <value>True initially. Registered loggers still receive errors when false.</value>
    public static bool PrintErrorMessages { get => RuntimeDiagnostics.Instance.PrintErrorMessages; set => RuntimeDiagnostics.Instance.PrintErrorMessages = value; }

    /// <summary>Captures available C# stack metadata for the calling thread.</summary>
    /// <returns>A caller-owned array containing an immutable snapshot.</returns>
    /// <remarks>Capture allocates and may block. Source locations require symbols, and trimmed metadata may be
    /// unavailable. Live locals/member/global values require a CLR debugger adapter and are not captured.</remarks>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ScriptBacktrace[] CaptureScriptBacktraces() => [ScriptBacktrace.Capture(new StackTrace(1, true))];
}
