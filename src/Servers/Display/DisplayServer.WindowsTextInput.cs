using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Electron2D;

public sealed partial class DisplayServer
{
    private sealed unsafe partial class WindowsTextInput(string description, string existingText)
    {
        private const ushort Accept = 1, Cancel = 2, Input = 100, Description = 101, Label = 102;
        private const ushort ButtonClass = 0x80, EditClass = 0x81, StaticClass = 0x82;
        private const uint ChildVisible = 0x50000000, TabStop = 0x00010000;
        private const uint DialogStyle = 0x80C808C0;
        private const uint ScrollableDescription = 0x00200844; // WS_VSCROLL, ES_READONLY, ES_AUTOVSCROLL, ES_MULTILINE.
        private const uint SingleLineInput = 0x00800080; // WS_BORDER, ES_AUTOHSCROLL.
        private const uint LiteralLabel = 0x80, DefaultButton = 1;

        // DialogBox calls the procedure on the retained owner until the modal call returns.
        [ThreadStatic] private static WindowsTextInput? _current;
        private readonly string _description = description;
        private readonly string _initialText = existingText;
        private string? _result;
        private Exception? _error;

        internal static string? Show(nint parent, string title, string description, string existingText)
        {
            if (parent == 0)
                throw new InvalidOperationException("The native text dialog requires a Windows parent window.");
            ValidateInitialText(existingText);
            description = description.ReplaceLineEndings("\r\n");
            var template = CreateTemplate(title, description.Length != 0);
            var request = new WindowsTextInput(description, existingText);
            _current = request;
            nint result;
            try
            {
                fixed (byte* data = template)
                    result = DialogBoxIndirectParamW(0, data, parent, &DialogProcedure, 0);
            }
            finally { _current = null; }
            if (request._error != null)
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw(request._error);
            return result switch
            {
                Accept => request._result ?? throw new InvalidOperationException("The native text dialog did not return confirmed text."),
                Cancel => null,
                _ => throw Failure("create or run the dialog")
            };
        }

        private static void ValidateInitialText(string existingText)
        {
            for (var index = 0; index < existingText.Length; index++)
            {
                var ch = existingText[index];
                if (ch is '\r' or '\n' ||
                    (char.IsSurrogate(ch) && !char.IsSurrogatePair(existingText, index)))
                    throw new ArgumentException("The native input requires one line of valid UTF-16 text.", nameof(existingText));
                if (char.IsHighSurrogate(ch))
                    index++;
            }
        }

        private static byte[] CreateTemplate(string title, bool hasDescription)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            var offset = hasDescription ? 38 : 0;
            // DLGTEMPLATE with caption, system menu, modal frame, centered placement and dialog font.
            writer.Write(DialogStyle);
            writer.Write(0u);
            writer.Write((ushort)(hasDescription ? 5 : 4));
            Bounds(0, 0, 278, (short)(103 - offset));
            writer.Write((ushort)0);
            writer.Write((ushort)0);
            Text(title);
            writer.Write((ushort)9);
            Text("Segoe UI");
            if (hasDescription)
                Control(ChildVisible | TabStop | ScrollableDescription, 8, 8, 262, 31, Description, EditClass, "");
            Control(ChildVisible | LiteralLabel, 8, (short)(46 - offset), 262, 9, Label, StaticClass, Caption("Input"));
            Control(ChildVisible | TabStop | SingleLineInput, 8, (short)(57 - offset), 262, 14, Input, EditClass, "");
            Control(ChildVisible | TabStop | DefaultButton, 148, (short)(81 - offset), 58, 14, Accept, ButtonClass, Caption("OK"));
            Control(ChildVisible | TabStop, 212, (short)(81 - offset), 58, 14, Cancel, ButtonClass, Caption("Cancel"));
            return stream.ToArray();

            static string Caption(string message)
            {
                var translated = TranslationServer.Translate("", message, "native text dialog");
                OS.ValidateNativeMessage(translated, nameof(translated));
                return translated.Length == 0 ? message : translated;
            }
            void Bounds(short x, short y, short width, short height)
            {
                writer.Write(x);
                writer.Write(y);
                writer.Write(width);
                writer.Write(height);
            }
            void Text(string value)
            {
                foreach (var ch in value)
                    writer.Write((ushort)ch);
                writer.Write((ushort)0);
            }
            void Control(uint style, short x, short y, short width, short height, ushort id, ushort kind, string text)
            {
                while ((stream.Position & 3) != 0)
                    writer.Write((byte)0);
                writer.Write(style);
                writer.Write(0u);
                Bounds(x, y, width, height);
                writer.Write(id);
                writer.Write(ushort.MaxValue);
                writer.Write(kind);
                Text(text);
                writer.Write((ushort)0);
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static nint DialogProcedure(nint dialog, uint message, nuint wParam, nint lParam)
        {
            var request = _current;
            if (request == null)
                return 0;
            try
            {
                if (message == 0x0110) // WM_INITDIALOG, before the template is presented.
                {
                    var input = RequireControl(dialog, Input);
                    if (request._description.Length != 0 && SetWindowTextW(RequireControl(dialog, Description), request._description) == 0)
                        throw Failure("initialize the description");
                    SendMessageW(input, 0x00C5, 0, 0); // EM_SETLIMITTEXT: use the native single-line maximum.
                    if (SetWindowTextW(input, request._initialText) == 0)
                        throw Failure("initialize the text field");
                    if (!string.Equals(ReadText(input), request._initialText, StringComparison.Ordinal))
                        throw new ArgumentException("The native entry cannot represent the initial text exactly.", "existingText");
                    SetFocus(input);
                    SendMessageW(input, 0x00B1, 0, -1); // EM_SETSEL.
                    return 0;
                }
                if (message == 0x0111 && (ushort)wParam == Accept) // WM_COMMAND.
                {
                    request._result = ReadText(RequireControl(dialog, Input));
                    if (EndDialog(dialog, Accept) == 0)
                        throw Failure("confirm the dialog");
                    return 1;
                }
                if (message == 0x0010 || (message == 0x0111 && (ushort)wParam == Cancel))
                {
                    if (EndDialog(dialog, Cancel) == 0)
                        throw Failure("dismiss the dialog");
                    return 1;
                }
                return 0;
            }
            catch (Exception error)
            {
                request._error = error;
                EndDialog(dialog, 0);
                return 1;
            }
        }

        private static nint RequireControl(nint dialog, int id)
        {
            var control = GetDlgItem(dialog, id);
            return control != 0 ? control : throw Failure("find the native control");
        }

        private static string ReadText(nint edit)
        {
            var length = GetWindowTextLengthW(edit);
            if (length == 0 && Marshal.GetLastPInvokeError() != 0)
                throw Failure("measure the text field");
            var buffer = new char[checked(length + 1)];
            fixed (char* chars = buffer)
            {
                var copied = GetWindowTextW(edit, chars, buffer.Length);
                if (copied != length || (copied == 0 && Marshal.GetLastPInvokeError() != 0))
                    throw Failure("copy the text field");
            }
            return new string(buffer, 0, length);
        }

        private static InvalidOperationException Failure(string operation) =>
            new($"Could not {operation} for Windows native text input (Win32 error {Marshal.GetLastPInvokeError()}).");

        [LibraryImport("user32.dll", SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static partial nint DialogBoxIndirectParamW(nint instance, byte* template, nint parent,
            delegate* unmanaged[Stdcall]<nint, uint, nuint, nint, nint> procedure, nint parameter);
        [LibraryImport("user32.dll", SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static partial int EndDialog(nint dialog, nint result);
        [LibraryImport("user32.dll", SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static partial nint GetDlgItem(nint dialog, int id);
        [LibraryImport("user32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static partial int SetWindowTextW(nint window, string value);
        [LibraryImport("user32.dll", SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static partial int GetWindowTextLengthW(nint window);
        [LibraryImport("user32.dll", SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static partial int GetWindowTextW(nint window, char* buffer, int size);
        [LibraryImport("user32.dll"), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static partial nint SendMessageW(nint window, uint message, nuint wParam, nint lParam);
        [LibraryImport("user32.dll"), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static partial nint SetFocus(nint window);
    }
}
