using Electron2D;
using EOs = Electron2D.OS;

internal static partial class TextChecks
{
    private sealed class DiagnosticsLoop(Action initialize) : MainLoop
    {
        protected override void OnInitialize() => initialize();
    }

    internal static void RunRawRich()
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var oldOut = Engine.PrintToSTDOUT;
        var oldError = Engine.PrintErrorMessages;
        var forceColor = Environment.GetEnvironmentVariable("FORCE_COLOR");
        var noColor = Environment.GetEnvironmentVariable("NO_COLOR");
        using var output = new Writer { NewLine = "<NL>" };
        using var errors = new Writer();
        using var receiver = new Receiver();
        Console.SetOut(output);
        Console.SetError(errors);
        var installedOut = Console.Out;
        var installedError = Console.Error;
        try
        {
            EOs.AddLogger(receiver);
            Environment.SetEnvironmentVariable("FORCE_COLOR", "1");
            foreach (var ansi in new[] { false, true })
            {
                Environment.SetEnvironmentVariable("NO_COLOR", ansi ? null : "1");
                foreach (var stdout in new[] { false, true })
                {
                    foreach (var stderr in new[] { false, true })
                    {
                        Engine.PrintToSTDOUT = stdout;
                        Engine.PrintErrorMessages = stderr;
                        CheckRawOutput(output, receiver, stdout);
                        CheckRichOutput(output, receiver, stdout, ansi);
                        RequireEqual("", errors.ToString(), "stderr");
                    }
                }
            }

            CheckWriterFailures(receiver, output);
            CheckNullMessages(receiver, output);
            CheckFlushPolicy(output);
            EOs.RemoveLogger(receiver);
            Check(!receiver.IsDisposed && !output.Disposed && !errors.Disposed);
            Check(ReferenceEquals(Console.Out, installedOut) && ReferenceEquals(Console.Error, installedError));
        }
        finally
        {
            EOs.RemoveLogger(receiver);
            Engine.PrintToSTDOUT = oldOut;
            Engine.PrintErrorMessages = oldError;
            Environment.SetEnvironmentVariable("FORCE_COLOR", forceColor);
            Environment.SetEnvironmentVariable("NO_COLOR", noColor);
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }

        Console.WriteLine("Raw/rich diagnostics: exact text, 80 rich/gate combinations, writer failures, null validation and flush policy passed.");
    }

    private static void CheckRawOutput(Writer output, Receiver receiver, bool stdout)
    {
        const string raw = " A\tБ😀\r\n[b]raw[/b]\0";
        output.GetStringBuilder().Clear();
        receiver.Messages.Clear();
        EOs.LogRaw(raw);
        EOs.LogRaw("");
        RequireEqual(stdout ? raw : "", output.ToString(), "raw output");
        EOs.LogRaw("continue ");
        EOs.LogMessage("line");
        RequireEqual(stdout ? raw + "continue line" + output.NewLine : "", output.ToString(), "continued line");
        (string, bool)[] expected = [(raw, false), ("", false), ("continue ", false), ("line", false)];
        Check(receiver.Messages.SequenceEqual(expected));
    }

    private static void CheckRichOutput(Writer output, Receiver receiver, bool stdout, bool ansi)
    {
        (string Input, string Plain, string Formatted)[] cases =
        [
            ("", "", ""),
            ("AБ😀\t\r\n", "AБ😀\t\r\n", "AБ😀\t\r\n"),
            ("[b]A[i]Б[/i]😀[/b]", "AБ😀", "\u001b[1mA\u001b[3mБ\u001b[23m😀\u001b[22m"),
            (
                "[u]U[/u][s]S[/s][code]C[/code]",
                "USC",
                "\u001b[4mU\u001b[24m\u001b[9mS\u001b[29m\u001b[2mC\u001b[22m"
            ),
            (
                "[indent]A[/indent][center]B[/center][right]C[/right]",
                "    A\n\t\t\tB\n\t\t\t\t\t\tC",
                "    A\n\t\t\tB\n\t\t\t\t\t\tC"
            ),
            (
                "[url]https://example.com[/url] [url=/]X[/url]",
                "https://example.com [url=/]X",
                "https://example.com [url=/]X"
            ),
            ("[b]unclosed [i", "unclosed [i", "\u001b[1munclosed [i"),
            ("[/b][new][[i]X[/i][/new]", "[new][X[/new]", "\u001b[22m[new][\u001b[3mX\u001b[23m[/new]"),
            (
                "[color=green]G[bgcolor=pink]B[/bgcolor][/color][fgcolor=#123456]F[/fgcolor]",
                "GBF",
                "\u001b[92mG\u001b[48;5;218mB\u001b[49m\u001b[39m\u001b[38;2;18;52;86;48;2;18;52;86mF\u001b[39;49m"
            ),
            (
                "[color=#12Z]X[/color][color=#0f0]Y[/color]",
                "XY",
                "\u001b[38;2;0;0;0mX\u001b[39m\u001b[38;2;0;255;0mY\u001b[39m"
            ),
        ];

        foreach (var (input, plain, formatted) in cases)
        {
            output.GetStringBuilder().Clear();
            receiver.Messages.Clear();
            EOs.LogRich(input);
            EOs.LogMessage("following");
            var line = ansi ? formatted + "\u001b[0m" : plain;
            RequireEqual(stdout ? line + output.NewLine + "following" + output.NewLine : "", output.ToString(), input);
            Check(receiver.Messages.SequenceEqual(new[] { (input, false), ("following", false) }));
        }
    }

    private static void CheckWriterFailures(Receiver receiver, Writer output)
    {
        Engine.PrintToSTDOUT = true;
        output.FailWrite = true;
        foreach (Action<string> write in new Action<string>[] { EOs.LogRaw, EOs.LogRich })
        {
            receiver.Messages.Clear();
            try
            {
                write("[b]write failure[/b]");
                throw new InvalidOperationException("Missing writer error.");
            }
            catch (IOException failure) when (failure.Message == "writer failure")
            {
                Check(receiver.Messages.SequenceEqual(new[] { ("[b]write failure[/b]", false) }));
            }
        }

        output.FailWrite = false;
    }

    private static void CheckNullMessages(Receiver receiver, Writer output)
    {
        output.GetStringBuilder().Clear();
        receiver.Messages.Clear();
        foreach (Action<string> write in new Action<string>[] { EOs.LogRaw, EOs.LogRich })
        {
            try
            {
                write(null!);
                throw new InvalidOperationException("Missing null argument validation.");
            }
            catch (ArgumentNullException failure) when (failure.ParamName == "message")
            {
                Check(receiver.Messages.Count == 0 && output.GetStringBuilder().Length == 0);
            }
        }
    }

    private static void CheckFlushPolicy(Writer output)
    {
        const string feature = "raw_rich_flush_fixture";
        ProjectSettings.AddCustomFeature(feature);
        try
        {
            foreach (var flush in new[] { false, true })
            {
                ProjectSettings.SetFeatureOverride(ProjectSettings.FlushSTDOUTOnPrint, feature, flush);
                using var loop = new DiagnosticsLoop(() =>
                {
                    var count = output.Flushes;
                    EOs.LogRaw("flush raw");
                    Check(output.Flushes == count + (flush ? 1 : 0));
                    EOs.LogRich("[b]flush rich[/b]");
                    Check(output.Flushes == count + (flush ? 2 : 0));
                });
                Engine.Start(loop);
                Engine.Stop();
            }
        }
        finally
        {
            ProjectSettings.ClearFeatureOverride(ProjectSettings.FlushSTDOUTOnPrint, feature);
            ProjectSettings.RemoveCustomFeature(feature);
        }
    }

    private static void RequireEqual(string expected, string actual, string context)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Unexpected output for {context}. Expected {Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(expected))}, " +
                $"got {Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(actual))}.");
        }
    }
}
