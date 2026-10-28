using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using static Electron2D.LinuxPortalDBus;

namespace Electron2D;

internal sealed class LinuxPortalFileChooser : IDisposable
{
    internal const string Interface = "org.freedesktop.portal.FileChooser";
    private const string RequestInterface = "org.freedesktop.portal.Request";
    private readonly record struct Rule(uint Kind, string Pattern);
    private sealed record Filter(string Label, Rule[] Rules);
    private nint _connection;
    private string _path = "", _owner = "";
    private bool _completed;
    private readonly bool _multiple;
    private readonly Filter[] _filters;
    private readonly FileDialogOption[] _options;
    private static readonly IReadOnlyDictionary<string, FileDialogOptionValue> EmptyOptions = new ReadOnlyDictionary<string, FileDialogOptionValue>(new Dictionary<string, FileDialogOptionValue>());
    internal IReadOnlyDictionary<string, FileDialogOptionValue> SelectedOptions { get; private set; } = EmptyOptions;

    internal LinuxPortalFileChooser(string parent, string title, string directory, string filename, FileDialogMode mode, IReadOnlyList<string> filters, FileDialogOption[] options)
    {
        OS.ValidateNativeMessage(title, nameof(title)); OS.ValidateNativeMessage(directory, nameof(directory)); OS.ValidateNativeMessage(filename, nameof(filename));
        _options = options;
        _multiple = mode == FileDialogMode.OpenFiles;
        _filters = mode == FileDialogMode.OpenDirectory ? [] : ParseFilters(filters);
        try
        {
            Check(ThreadsInitDefault()); _connection = BusGetPrivate(0, 0);
            if (_connection == 0) throw new NotSupportedException("The session bus is unavailable.");
            SetExitOnDisconnect(_connection, 0);
            var version = QueryVersion(_connection, Interface, out _owner);
            if (version == 0 || _owner.Length == 0) throw new NotSupportedException("The FileChooser portal is unavailable.");
            if (mode == FileDialogMode.OpenDirectory && version < 3) throw new NotSupportedException("This FileChooser portal cannot select directories.");
            var sender = Marshal.PtrToStringUTF8(BusGetUniqueName(_connection)) ?? throw new InvalidOperationException("The portal connection has no sender identity.");
            var token = "electron2d_" + Guid.NewGuid().ToString("N");
            _path = DesktopPath + "/request/" + sender[1..].Replace('.', '_') + "/" + token;
            // Subscribe before calling: a portal may finish before its method reply arrives.
            BusAddMatch(_connection, $"type='signal',sender='{_owner}',interface='{RequestInterface}',member='Response'", 0);
            BusAddMatch(_connection, $"type='signal',sender='org.freedesktop.DBus',interface='org.freedesktop.DBus',member='NameOwnerChanged',arg0='{_owner}'", 0);
            Launch(parent, title, directory, filename, mode, token);
        }
        catch { Dispose(); throw; }
    }

    private void Launch(string parent, string title, string directory, string filename, FileDialogMode mode, string token)
    {
        var message = NewMethodCall(_owner, DesktopPath, Interface, mode == FileDialogMode.SaveFile ? "SaveFile" : "OpenFile");
        if (message == 0) throw new OutOfMemoryException();
        try
        {
            InitAppend(message, out var args); AppendString(ref args, parent); AppendString(ref args, title);
            var options = Open(ref args, ArrayType, "{sv}");
            StringOption(ref options, "handle_token", token);
            BoolOption(ref options, "modal", true);
            if (mode != FileDialogMode.SaveFile)
            { BoolOption(ref options, "multiple", mode == FileDialogMode.OpenFiles); if (mode == FileDialogMode.OpenDirectory) BoolOption(ref options, "directory", true); }
            if (directory.Length != 0) PathOption(ref options, "current_folder", directory);
            if (mode == FileDialogMode.SaveFile && filename.Length != 0) StringOption(ref options, "current_name", filename);
            if (_filters.Length != 0)
            {
                var entry = Option(ref options, "filters", "a(sa(us))", out var variant);
                var list = Open(ref variant, ArrayType, "(sa(us))");
                foreach (var filter in _filters) WriteFilter(ref list, filter);
                Close(ref variant, ref list); Close(ref entry, ref variant); Close(ref options, ref entry);
            }
            if (_options.Length != 0) WriteChoices(ref options);
            Close(ref args, ref options);
            var reply = SendWithReplyAndBlock(_connection, message, 5000, 0);
            if (reply == 0) throw new InvalidOperationException("The FileChooser portal could not launch the request.");
            try
            {
                if (MessageType(reply) != 2 || Init(reply, out var value) == 0) throw new InvalidOperationException("The FileChooser portal returned no request.");
                var actual = ReadString(ref value, PathType);
                if (!actual.StartsWith(DesktopPath + "/request/", StringComparison.Ordinal)) throw new InvalidOperationException("The FileChooser portal returned an invalid request path.");
                _path = actual;
            }
            finally { MessageUnref(reply); }
        }
        finally { MessageUnref(message); }
    }

    private static MessageIter Option(ref MessageIter options, string name, string signature, out MessageIter value)
    { var entry = Open(ref options, DictEntryType, null); AppendString(ref entry, name); value = Open(ref entry, VariantType, signature); return entry; }
    private static void StringOption(ref MessageIter options, string name, string text)
    { var entry = Option(ref options, name, "s", out var value); AppendString(ref value, text); Close(ref entry, ref value); Close(ref options, ref entry); }
    private static void BoolOption(ref MessageIter options, string name, bool enabled)
    { var entry = Option(ref options, name, "b", out var value); AppendUInt32(ref value, enabled ? 1u : 0u, BooleanType); Close(ref entry, ref value); Close(ref options, ref entry); }
    private static void PathOption(ref MessageIter options, string name, string path)
    {
        var entry = Option(ref options, name, "ay", out var value); var bytes = Open(ref value, ArrayType, "y");
        foreach (var octet in Encoding.UTF8.GetBytes(path + '\0')) AppendByte(ref bytes, octet);
        Close(ref value, ref bytes); Close(ref entry, ref value); Close(ref options, ref entry);
    }
    private static void WriteFilter(ref MessageIter list, Filter filter)
    {
        var item = Open(ref list, StructType, null); AppendString(ref item, filter.Label); var rules = Open(ref item, ArrayType, "(us)");
        foreach (var rule in filter.Rules)
        { var pair = Open(ref rules, StructType, null); AppendUInt32(ref pair, rule.Kind); AppendString(ref pair, rule.Pattern); Close(ref rules, ref pair); }
        Close(ref item, ref rules); Close(ref list, ref item);
    }

    private void WriteChoices(ref MessageIter options)
    {
        var entry = Option(ref options, "choices", "a(ssa(ss)s)", out var variant); var list = Open(ref variant, ArrayType, "(ssa(ss)s)");
        for (var i = 0; i < _options.Length; i++)
        {
            var option = _options[i]; var item = Open(ref list, StructType, null);
            AppendString(ref item, "option_" + i.ToString(CultureInfo.InvariantCulture)); AppendString(ref item, option.Name);
            var values = Open(ref item, ArrayType, "(ss)");
            for (var j = 0; j < option.Values.Length; j++)
            { var pair = Open(ref values, StructType, null); AppendString(ref pair, j.ToString(CultureInfo.InvariantCulture)); AppendString(ref pair, option.Values[j]); Close(ref values, ref pair); }
            Close(ref item, ref values);
            AppendString(ref item, option.Values.Length == 0 ? option.DefaultValueIndex == 0 ? "false" : "true" : option.DefaultValueIndex.ToString(CultureInfo.InvariantCulture));
            Close(ref list, ref item);
        }
        Close(ref variant, ref list); Close(ref entry, ref variant); Close(ref options, ref entry);
    }

    private IReadOnlyDictionary<string, FileDialogOptionValue> ReadChoices(ref MessageIter value)
    {
        Require(ref value, ArrayType); Recurse(ref value, out var entries); var selected = new FileDialogOptionValue?[_options.Length];
        while (GetArgType(ref entries) != 0)
        {
            Require(ref entries, StructType); Recurse(ref entries, out var pair); var id = ReadString(ref pair); Next(ref pair); var text = ReadString(ref pair);
            if (Next(ref pair) != 0) throw new InvalidOperationException("The portal returned an invalid option pair.");
            if (id.StartsWith("option_", StringComparison.Ordinal) && int.TryParse(id.AsSpan(7), NumberStyles.None, CultureInfo.InvariantCulture, out var index) &&
                (uint)index < _options.Length && id == "option_" + index.ToString(CultureInfo.InvariantCulture))
            {
                if (selected[index].HasValue) throw new InvalidOperationException("The portal returned a duplicate option.");
                var checkbox = _options[index].Values.Length == 0;
                int choice;
                if (checkbox) choice = text == "true" ? 1 : text == "false" ? 0 : throw new InvalidOperationException("The portal returned an invalid checkbox value.");
                else if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out choice) || (uint)choice >= _options[index].Values.Length || text != choice.ToString(CultureInfo.InvariantCulture))
                    throw new InvalidOperationException("The portal returned an invalid choice index.");
                selected[index] = new(checkbox, choice);
            }
            Next(ref entries);
        }
        var result = new Dictionary<string, FileDialogOptionValue>(StringComparer.Ordinal);
        for (var i = 0; i < _options.Length; i++) result[_options[i].Name] = selected[i] ?? throw new InvalidOperationException("The portal omitted an option result.");
        return new ReadOnlyDictionary<string, FileDialogOptionValue>(result);
    }

    private static Filter[] ParseFilters(IReadOnlyList<string> filters)
    {
        var result = new Filter[filters.Count];
        for (var i = 0; i < result.Length; i++)
        {
            var text = filters[i] ?? throw new ArgumentException("A filter is null.", nameof(filters)); OS.ValidateNativeMessage(text, nameof(filters));
            var parts = text.Split(';'); if (parts.Length > 3) throw new ArgumentException("A filter has too many fields.", nameof(filters));
            var rules = new List<Rule>(); var patterns = parts[0].Split(',');
            if (parts[0].Trim().Length != 0)
                foreach (var raw in patterns)
                {
                    var pattern = raw.Trim();
                    if (pattern is "*" or "*.*")
                    { if (patterns.Length != 1) throw new ArgumentException("The all-files filter must stand alone.", nameof(filters)); pattern = "*"; }
                    else if (!pattern.StartsWith("*.", StringComparison.Ordinal) || pattern.Length <= 2 || !pattern.AsSpan(2).ToString().All(static c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.'))
                        throw new ArgumentException("A file extension filter is invalid.", nameof(filters));
                    rules.Add(new(0, pattern));
                }
            if (parts.Length == 3 && parts[2].Trim().Length != 0)
                foreach (var raw in parts[2].Split(','))
                {
                    var mime = raw.Trim(); var slash = mime.IndexOf('/');
                    if (slash <= 0 || slash == mime.Length - 1 || mime.LastIndexOf('/') != slash ||
                        !mime.AsSpan(0, slash).ToString().All(MIMEToken) || mime[(slash + 1)..] != "*" && !mime.AsSpan(slash + 1).ToString().All(MIMEToken))
                        throw new ArgumentException("A MIME filter is invalid.", nameof(filters));
                    rules.Add(new(1, mime));
                }
            if (rules.Count == 0) throw new ArgumentException("A filter needs an extension or MIME type.", nameof(filters));
            result[i] = new(parts.Length > 1 && parts[1].Length != 0 ? parts[1] : parts[0].Trim().Length != 0 ? parts[0] : parts[2], [.. rules]);
        }
        return result;
    }
    private static bool MIMEToken(char c) => char.IsAsciiLetterOrDigit(c) || c is '!' or '#' or '$' or '&' or '^' or '_' or '.' or '+' or '-';

    internal bool TryComplete(out string[] paths, out int filter, out string? error)
    {
        paths = []; filter = -1; error = null;
        try
        {
            var connected = ReadWrite(_connection, 0) != 0;
            nint message;
            while ((message = PopMessage(_connection)) != 0)
            {
                try
                {
                    var sender = Marshal.PtrToStringUTF8(MessageSender(message));
                    if (sender == _owner && IsSignal(message, RequestInterface, "Response") != 0 && Marshal.PtrToStringUTF8(MessagePath(message)) == _path)
                    { ReadResponse(message, out paths, out filter); _completed = true; return true; }
                    if (sender == "org.freedesktop.DBus" && IsSignal(message, "org.freedesktop.DBus", "NameOwnerChanged") != 0 && Init(message, out var changed) != 0 && ReadString(ref changed) == _owner)
                    { Next(ref changed); _ = ReadString(ref changed); Next(ref changed); if (ReadString(ref changed).Length == 0) throw new InvalidOperationException("The FileChooser portal disconnected."); }
                }
                finally { MessageUnref(message); }
            }
            if (!connected) throw new InvalidOperationException("The FileChooser session bus disconnected.");
            return false;
        }
        catch (Exception failure) { SelectedOptions = EmptyOptions; paths = []; filter = -1; error = failure.Message; _completed = true; return true; }
    }

    private void ReadResponse(nint message, out string[] paths, out int filter)
    {
        paths = []; filter = -1;
        if (Init(message, out var args) == 0) throw new InvalidOperationException("The portal response is empty.");
        var response = ReadUInt32(ref args); if (response == 1) return;
        if (response != 0) throw new InvalidOperationException("The FileChooser portal ended the request unsuccessfully.");
        Next(ref args); Require(ref args, ArrayType); Recurse(ref args, out var entries);
        var foundPaths = false; var foundFilter = false;
        IReadOnlyDictionary<string, FileDialogOptionValue>? selections = null;
        while (GetArgType(ref entries) != 0)
        {
            Require(ref entries, DictEntryType); Recurse(ref entries, out var entry); var key = ReadString(ref entry); Next(ref entry); Require(ref entry, VariantType); Recurse(ref entry, out var value);
            if (key == "uris")
            {
                if (foundPaths) throw new InvalidOperationException("The portal returned duplicate file results."); foundPaths = true;
                Require(ref value, ArrayType); Recurse(ref value, out var uris); var selected = new List<string>();
                while (GetArgType(ref uris) != 0)
                {
                    var text = ReadString(ref uris);
                    if (!IsFileURI(text) || !Uri.TryCreate(text, UriKind.Absolute, out var uri) || !uri.IsFile || uri.Query.Length != 0 || uri.Fragment.Length != 0 || uri.Host.Length != 0 && uri.Host != "localhost" || uri.LocalPath.Contains('\0') || !System.IO.Path.IsPathFullyQualified(uri.LocalPath))
                        throw new InvalidOperationException("The portal returned a nonlocal or invalid file URI.");
                    selected.Add(uri.LocalPath); Next(ref uris);
                }
                paths = [.. selected];
            }
            else if (key == "current_filter")
            {
                if (foundFilter) throw new InvalidOperationException("The portal returned duplicate filter results."); foundFilter = true;
                var selected = ReadFilter(ref value);
                for (var i = 0; i < _filters.Length; i++) if (selected.Label == _filters[i].Label && selected.Rules.AsSpan().SequenceEqual(_filters[i].Rules)) { filter = i; break; }
            }
            else if (key == "choices" && _options.Length != 0)
            {
                if (selections != null) throw new InvalidOperationException("The portal returned duplicate option results.");
                selections = ReadChoices(ref value);
            }
            Next(ref entries);
        }
        if (!foundPaths || paths.Length == 0 || !_multiple && paths.Length != 1) throw new InvalidOperationException("The successful portal response contains an invalid file count.");
        SelectedOptions = selections ?? (_options.Length == 0 ? EmptyOptions : throw new InvalidOperationException("The portal omitted option results."));
    }
    private static bool IsFileURI(string text)
    {
        if (!text.StartsWith("file://", StringComparison.OrdinalIgnoreCase)) return false;
        for (var i = 0; i < text.Length; i++)
        {
            if (char.IsControl(text[i]) || text[i] is ' ' or '\\') return false;
            if (text[i] != '%') continue;
            if (i + 2 >= text.Length || !char.IsAsciiHexDigit(text[i + 1]) || !char.IsAsciiHexDigit(text[i + 2])) return false;
            i += 2;
        }
        return true;
    }

    private static Filter ReadFilter(ref MessageIter value)
    {
        Require(ref value, StructType); Recurse(ref value, out var item); var label = ReadString(ref item); Next(ref item); Require(ref item, ArrayType); Recurse(ref item, out var rules); var result = new List<Rule>();
        while (GetArgType(ref rules) != 0)
        { Require(ref rules, StructType); Recurse(ref rules, out var pair); var kind = ReadUInt32(ref pair); Next(ref pair); result.Add(new(kind, ReadString(ref pair))); Next(ref rules); }
        return new(label, [.. result]);
    }

    public void Dispose()
    {
        if (_connection == 0) return;
        if (!_completed && _path.Length != 0)
        {
            var message = NewMethodCall(_owner, _path, RequestInterface, "Close");
            if (message != 0) { try { Send(_connection, message, 0); ReadWrite(_connection, 0); } finally { MessageUnref(message); } }
        }
        ConnectionClose(_connection); ConnectionUnref(_connection); _connection = 0;
    }
}
