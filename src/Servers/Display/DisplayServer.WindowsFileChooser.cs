using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

public sealed partial class DisplayServer
{
    private void ShowWindowsFileDialog(nint window, int parentWindowId, string title, string directory, string filename, bool hidden, FileDialogMode mode, IReadOnlyList<string> filters, Action<bool, IReadOnlyList<string>, int>? callback, FileDialogOption[]? options, Action<bool, IReadOnlyList<string>, int, IReadOnlyDictionary<string, FileDialogOptionValue>>? optionsCallback)
    {
        var parsed = mode == FileDialogMode.OpenDirectory ? [] : filters.Select(ParseDialogFilter).ToArray();
        var handle = SDL.GetPointerProperty(SDL.GetWindowProperties(window), SDL.Props.WindowWin32HWNDPointer, 0);
        if (handle == 0) throw new InvalidOperationException("The native chooser requires a Windows parent window.");
        IReadOnlyDictionary<string, FileDialogOptionValue> selected = new ReadOnlyDictionary<string, FileDialogOptionValue>(new Dictionary<string, FileDialogOptionValue>());
        var request = new DialogRequest(this, ResolveWindow(parentWindowId), optionsCallback == null ? callback! : (ok, paths, filter) => optionsCallback(ok, paths, filter, selected), []);
        var worker = new Thread(() =>
        {
            string[] paths = []; var filter = -1; string? error = null;
            try { (paths, filter, selected) = WindowsFileChooser.Show(handle, title, directory, filename, hidden, mode, parsed, options ?? []); }
            catch (Exception failure) { error = failure.Message; }
            finally { request.DisposeFilters(); QueueDialogCompletion(new(request.Callback, paths, filter, error)); }
        })
        { IsBackground = true, Name = "Electron2D file chooser" };
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        worker.SetApartmentState(ApartmentState.STA);
        request.RetainParent(); lock (_dialogGate) _pendingNativeDialogs++;
        try { worker.Start(); }
        catch { request.DisposeFilters(); lock (_dialogGate) _pendingNativeDialogs--; throw; }
    }

    private static unsafe partial class WindowsFileChooser
    {
        private static readonly Guid OpenClass = new("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7"), SaveClass = new("C0B4E2F3-BA21-4773-8DBA-335EC946EB8B"), OpenInterface = new("D57C7288-D4AD-4768-BE02-9D969532D960"), DialogInterface = new("42F85136-DB7E-439C-85F1-E4075D135FC8"), CustomizeInterface = new("E6FDD21A-163F-4975-9C8C-A69F1BA37034"), ItemInterface = new("43826D1E-E718-42EE-BC55-A1E261C37BFE");
        private static void* Method(nint value, int slot) => (*(void***)value)[slot];
        private static void Check(int result) { if (result < 0) throw new IOException($"The Windows file chooser failed (HRESULT 0x{result:X8}).", result); }
        private static void Release(ref nint value) { var owned = value; value = 0; if (owned != 0) ((delegate* unmanaged[Stdcall]<nint, uint>)Method(owned, 2))(owned); }
        private static void String(nint value, int slot, string text) { fixed (char* buffer = text) Check(((delegate* unmanaged[Stdcall]<nint, char*, int>)Method(value, slot))(value, buffer)); }

        internal static (string[] Paths, int Filter, IReadOnlyDictionary<string, FileDialogOptionValue> Options) Show(nint parent, string title, string directory, string filename, bool hidden, FileDialogMode mode, (string Label, string Pattern)[] filters, FileDialogOption[] options)
        {
            var empty = new ReadOnlyDictionary<string, FileDialogOptionValue>(new Dictionary<string, FileDialogOptionValue>());
            Check(CoInitializeEx(0, 6)); nint dialog = 0, customize = 0, folder = 0;
            try
            {
                var classID = mode == FileDialogMode.SaveFile ? SaveClass : OpenClass; var iid = mode == FileDialogMode.SaveFile ? DialogInterface : OpenInterface;
                Check(CoCreateInstance(in classID, 0, 1, in iid, out dialog));
                uint flags = 0; Check(((delegate* unmanaged[Stdcall]<nint, uint*, int>)Method(dialog, 10))(dialog, &flags));
                flags |= 0x40 | 0x8 | 0x800;
                if (mode == FileDialogMode.OpenDirectory) flags |= 0x20;
                if (mode == FileDialogMode.OpenFiles) flags |= 0x200;
                if (mode is FileDialogMode.OpenFile or FileDialogMode.OpenFiles) flags |= 0x1000;
                if (mode == FileDialogMode.SaveFile) flags |= 0x2;
                if (hidden) flags |= 0x10000000;
                Check(((delegate* unmanaged[Stdcall]<nint, uint, int>)Method(dialog, 9))(dialog, flags));
                String(dialog, 17, title);
                if (directory.Length != 0)
                {
                    Check(SHCreateItemFromParsingName(System.IO.Path.GetFullPath(directory).Replace('/', '\\'), 0, in ItemInterface, out folder));
                    Check(((delegate* unmanaged[Stdcall]<nint, nint, int>)Method(dialog, 12))(dialog, folder));
                }
                if (filename.Length != 0 && mode != FileDialogMode.OpenDirectory) String(dialog, 15, filename.Replace('/', '\\'));
                SetFilters(dialog, filters);
                if (mode == FileDialogMode.SaveFile && filters.Length != 0)
                {
                    var extension = filters[0].Pattern.Split(';')[0]; if (extension != "*") String(dialog, 22, extension);
                }
                if (options.Length != 0)
                {
                    var customizeID = CustomizeInterface;
                    Check(((delegate* unmanaged[Stdcall]<nint, Guid*, nint*, int>)Method(dialog, 0))(dialog, &customizeID, &customize));
                    AddOptions(customize, options);
                }
                var status = ((delegate* unmanaged[Stdcall]<nint, nint, int>)Method(dialog, 3))(dialog, parent);
                if (status == unchecked((int)0x800704C7)) return ([], -1, empty);
                Check(status);
                var paths = Paths(dialog, mode == FileDialogMode.OpenFiles);
                uint filter = 0; if (filters.Length != 0) Check(((delegate* unmanaged[Stdcall]<nint, uint*, int>)Method(dialog, 6))(dialog, &filter));
                var selected = new Dictionary<string, FileDialogOptionValue>(StringComparer.Ordinal);
                for (var i = 0; i < options.Length; i++)
                {
                    uint value = 0; var checkbox = options[i].Values.Length == 0;
                    Check(((delegate* unmanaged[Stdcall]<nint, uint, uint*, int>)Method(customize, checkbox ? 17 : 24))(customize, (uint)i + 1, &value));
                    if (value > (checkbox ? 1 : (uint)options[i].Values.Length - 1)) throw new InvalidDataException("The native chooser returned an invalid option value.");
                    selected[options[i].Name] = new(checkbox, (int)value);
                }
                return (paths, filter >= 1 && filter <= filters.Length ? (int)filter - 1 : -1, new ReadOnlyDictionary<string, FileDialogOptionValue>(selected));
            }
            finally { Release(ref folder); Release(ref customize); Release(ref dialog); CoUninitialize(); }
        }

        private static void SetFilters(nint dialog, (string Label, string Pattern)[] filters)
        {
            if (filters.Length == 0) return;
            var native = new Filter[filters.Length];
            try
            {
                for (var i = 0; i < native.Length; i++)
                {
                    native[i].Label = Marshal.StringToCoTaskMemUni(filters[i].Label);
                    native[i].Pattern = Marshal.StringToCoTaskMemUni(string.Join(';', filters[i].Pattern.Split(';').Select(extension => extension == "*" ? "*" : "*." + extension)));
                }
                fixed (Filter* values = native) Check(((delegate* unmanaged[Stdcall]<nint, uint, Filter*, int>)Method(dialog, 4))(dialog, (uint)native.Length, values));
            }
            finally { foreach (var filter in native) { if (filter.Label != 0) Marshal.FreeCoTaskMem(filter.Label); if (filter.Pattern != 0) Marshal.FreeCoTaskMem(filter.Pattern); } }
        }

        private static void AddOptions(nint customize, FileDialogOption[] options)
        {
            for (var i = 0; i < options.Length; i++)
            {
                var option = options[i]; var id = (uint)i + 1; var label = option.Name.Replace("&", "&&");
                if (option.Values.Length == 0)
                { fixed (char* text = label) Check(((delegate* unmanaged[Stdcall]<nint, uint, char*, int, int>)Method(customize, 8))(customize, id, text, option.DefaultValueIndex)); }
                else
                {
                    fixed (char* text = label) Check(((delegate* unmanaged[Stdcall]<nint, uint, char*, int>)Method(customize, 26))(customize, (uint)(options.Length + i + 1), text));
                    Check(((delegate* unmanaged[Stdcall]<nint, uint, int>)Method(customize, 6))(customize, id));
                    for (var choice = 0; choice < option.Values.Length; choice++)
                    { fixed (char* text = option.Values[choice]) Check(((delegate* unmanaged[Stdcall]<nint, uint, uint, char*, int>)Method(customize, 19))(customize, id, (uint)choice, text)); }
                    Check(((delegate* unmanaged[Stdcall]<nint, uint, uint, int>)Method(customize, 25))(customize, id, (uint)option.DefaultValueIndex));
                    Check(((delegate* unmanaged[Stdcall]<nint, int>)Method(customize, 27))(customize));
                }
            }
        }

        private static string[] Paths(nint dialog, bool multiple)
        {
            nint items = 0, item = 0;
            try
            {
                if (!multiple)
                { Check(((delegate* unmanaged[Stdcall]<nint, nint*, int>)Method(dialog, 20))(dialog, &item)); return [Path(item)]; }
                Check(((delegate* unmanaged[Stdcall]<nint, nint*, int>)Method(dialog, 27))(dialog, &items));
                uint count = 0; Check(((delegate* unmanaged[Stdcall]<nint, uint*, int>)Method(items, 7))(items, &count));
                if (count == 0 || count > 65536) throw new InvalidDataException("The native chooser returned an invalid selection count.");
                var paths = new string[count];
                for (uint i = 0; i < count; i++) { Check(((delegate* unmanaged[Stdcall]<nint, uint, nint*, int>)Method(items, 8))(items, i, &item)); try { paths[i] = Path(item); } finally { Release(ref item); } }
                return paths;
            }
            finally { Release(ref item); Release(ref items); }
        }
        private static string Path(nint item)
        {
            nint text = 0;
            try
            {
                Check(((delegate* unmanaged[Stdcall]<nint, uint, nint*, int>)Method(item, 5))(item, 0x80058000, &text));
                var path = Marshal.PtrToStringUni(text); if (string.IsNullOrEmpty(path)) throw new InvalidDataException("The native chooser returned an empty filesystem path.");
                return path;
            }
            finally { if (text != 0) Marshal.FreeCoTaskMem(text); }
        }

        [StructLayout(LayoutKind.Sequential)] private struct Filter { internal nint Label, Pattern; }
        [LibraryImport("ole32.dll"), DefaultDllImportSearchPaths(DllImportSearchPath.System32)] private static partial int CoInitializeEx(nint reserved, uint mode);
        [LibraryImport("ole32.dll"), DefaultDllImportSearchPaths(DllImportSearchPath.System32)] private static partial void CoUninitialize();
        [LibraryImport("ole32.dll"), DefaultDllImportSearchPaths(DllImportSearchPath.System32)] private static partial int CoCreateInstance(in Guid classID, nint outer, uint context, in Guid interfaceID, out nint value);
        [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16), DefaultDllImportSearchPaths(DllImportSearchPath.System32)] private static partial int SHCreateItemFromParsingName(string path, nint binding, in Guid iid, out nint value);
    }
}
