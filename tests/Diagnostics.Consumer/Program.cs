using Electron2D;
using EOs = Electron2D.OS;

switch (args.FirstOrDefault() ?? "text")
{
    case "managed": DiagnosticsTests.Run(); break;
    case "portable": DiagnosticsTests.RunPortable(); break;
    case "gpu": case "compatibility": DiagnosticsFlowTests.Run(args[0]); break;
    case "output": DiagnosticsTests.RunOutputPolicy("", null); break;
    case "verbose": DiagnosticsTests.RunVerbose(args[1..]); break;
    case "text": TextChecks.Run(); break;
    case "wire":
        EOs.LogMessage(string.Concat("A", "Б"));
        EOs.LogMessage(string.Concat("A", "Б"), error: true);
        EOs.LogMessage(string.Join(" ", "A", "Б"));
        EOs.LogMessage(string.Join("\t", "A", "Б"));
        EOs.LogError("AБ", LoggerErrorType.Error, function: "Origin", file: "wire.cs", line: 42);
        EOs.LogError("AБ", LoggerErrorType.Warning, function: "Origin", file: "wire.cs", line: 42);
        break;
    default: throw new ArgumentException("Unknown diagnostics profile.");
}

internal static class TextChecks
{
    private sealed class Writer : StringWriter
    {
        internal bool FailWrite, FailFlush, Disposed;
        public override void WriteLine(string? value)
        {
            if (FailWrite) throw new IOException("writer failure");
            base.WriteLine(value);
        }
        public override void Flush() { if (FailFlush) throw new IOException("flush failure"); }
        protected override void Dispose(bool disposing) { Disposed = true; base.Dispose(disposing); }
    }

    private sealed class Receiver : Logger
    {
        internal readonly List<(string, bool)> Messages = [];
        internal readonly List<(string Function, string File, int Line, string Text, LoggerErrorType Type)> Errors = [];
        protected override void OnLogMessage(string message, bool error) => Messages.Add((message, error));
        protected override void OnLogError(string function, string file, int line, string code, string rationale,
            bool editorNotify, LoggerErrorType errorType, ReadOnlySpan<ScriptBacktrace> scriptBacktraces) =>
            Errors.Add((function, file, line, rationale, errorType));
    }

    private static void Check(bool value) { if (!value) throw new InvalidOperationException("Text diagnostic contract failed."); }

    internal static void Run()
    {
        var originalOut = Console.Out; var originalError = Console.Error;
        var oldOut = Engine.PrintToSTDOUT; var oldError = Engine.PrintErrorMessages;
        using var output = new Writer(); using var errors = new Writer(); using var receiver = new Receiver();
        Console.SetOut(output); Console.SetError(errors);
        var installedOut = Console.Out; var installedError = Console.Error;
        try
        {
            EOs.AddLogger(receiver);
            foreach (var stdout in new[] { false, true })
                foreach (var stderr in new[] { false, true })
                {
                    Engine.PrintToSTDOUT = stdout; Engine.PrintErrorMessages = stderr;
                    foreach (var parts in new[] { Array.Empty<string>(), new[] { "one" }, new[] { "A", "", "Б\nC" } })
                        foreach (var separator in new[] { "", " ", "\t" })
                            foreach (var error in new[] { false, true })
                            {
                                var text = string.Join(separator, parts);
                                output.GetStringBuilder().Clear(); errors.GetStringBuilder().Clear();
                                var count = receiver.Messages.Count;
                                EOs.LogMessage(text, error);
                                Check(receiver.Messages.Count == count + 1 && receiver.Messages[^1] == (text, error));
                                Check(output.ToString() == (!error && stdout ? text + output.NewLine : ""));
                                Check(errors.ToString() == (error && stderr ? text + errors.NewLine : ""));
                            }
                    foreach (var type in new[] { LoggerErrorType.Error, LoggerErrorType.Warning })
                        foreach (var text in new[] { "", "AБ", string.Concat("A", "", "Б\nC") })
                        {
                            errors.GetStringBuilder().Clear(); var count = receiver.Errors.Count;
                            EOs.LogError(text, type);
                            var detail = receiver.Errors[^1];
                            Check(receiver.Errors.Count == count + 1 && detail.Text == text && detail.Type == type);
                            Check(detail.Function == nameof(Run) && detail.File.EndsWith("Program.cs") && detail.Line > 0);
                            Check(errors.ToString() == (stderr ? $"{type}: {text} [{detail.File}:{detail.Line} @ Run]" + errors.NewLine : ""));
                        }
                }
            Engine.PrintToSTDOUT = true; Engine.PrintErrorMessages = true;
            foreach (var flush in new[] { false, true })
                foreach (var structured in new[] { false, true })
                {
                    errors.FailWrite = !flush; errors.FailFlush = flush;
                    var count = structured ? receiver.Errors.Count : receiver.Messages.Count;
                    Exception? caught = null;
                    try { if (structured) EOs.LogError("failed", LoggerErrorType.Warning); else EOs.LogMessage("failed", true); }
                    catch (IOException failure) { caught = failure; }
                    Check(caught is not null && (structured ? receiver.Errors.Count : receiver.Messages.Count) == count + 1);
                }
            errors.FailWrite = errors.FailFlush = false;
            output.FailWrite = true;
            var messages = receiver.Messages.Count;
            try { EOs.LogMessage("stdout failure"); throw new InvalidOperationException("Missing writer error"); }
            catch (IOException) { Check(receiver.Messages.Count == messages + 1); }
            output.FailWrite = false;
            EOs.RemoveLogger(receiver);
            Check(!receiver.IsDisposed && !output.Disposed && !errors.Disposed);
            Check(ReferenceEquals(Console.Out, installedOut) && ReferenceEquals(Console.Error, installedError));
        }
        finally
        {
            EOs.RemoveLogger(receiver);
            Engine.PrintToSTDOUT = oldOut; Engine.PrintErrorMessages = oldError;
            Console.SetOut(originalOut); Console.SetError(originalError);
        }
        Console.WriteLine("Text diagnostics: 72 text/gate combinations, 24 structured/gate combinations and 5 writer failures passed.");
    }
}
