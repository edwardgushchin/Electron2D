using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Electron2D;

public sealed partial class OS
{
    [SupportedOSPlatform("windows")]
    private static partial class WindowsFileManager
    {
        internal static void Show(string path, bool openFolder)
        {
            // A private STA preserves the caller's apartment and supports Shell extensions requiring one.
            ExceptionDispatchInfo? failure = null;
            var worker = new System.Threading.Thread(() => { try { Navigate(path, openFolder); } catch (Exception error) { failure = ExceptionDispatchInfo.Capture(error); } }) { IsBackground = true, Name = "Electron2D file manager" };
            worker.SetApartmentState(ApartmentState.STA); worker.Start(); worker.Join(); failure?.Throw();
        }

        private static void Check(int result)
        {
            if (result < 0) throw new IOException($"The Windows file-manager request failed (HRESULT 0x{result:X8}).", result);
        }

        private static unsafe void Navigate(string path, bool openFolder)
        {
            Check(CoInitializeEx(0, 6));
            nint item = 0;
            try
            {
                Check(SHParseDisplayName(path.Replace('/', '\\'), 0, out item, 0, out _));
                if (item == 0) throw new IOException("The Windows Shell did not return a file identity.");
                if (openFolder && Directory.Exists(path))
                {
                    // Complete the Shell handoff before freeing its PIDL, with native error UI suppressed.
                    var launch = new ShellExecuteInfo { Size = (uint)sizeof(ShellExecuteInfo), Mask = 0x504, IDList = item, Show = 1 };
                    if (ShellExecuteEx(ref launch) == 0) throw new IOException("The Windows Shell rejected the folder request.", new System.ComponentModel.Win32Exception(Marshal.GetLastPInvokeError()));
                }
                else Check(SHOpenFolderAndSelectItems(item, 0, 0, 0));
            }
            finally
            {
                if (item != 0) CoTaskMemFree(item);
                CoUninitialize();
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ShellExecuteInfo
        {
            internal uint Size, Mask;
            internal nint Window, Verb, File, Parameters, Directory;
            internal int Show;
            internal nint Instance, IDList, Class, ClassKey;
            internal uint HotKey;
            internal nint Icon, Process;
        }

        [LibraryImport("ole32.dll"), DefaultDllImportSearchPaths(DllImportSearchPath.System32)] private static partial int CoInitializeEx(nint reserved, uint mode);
        [LibraryImport("ole32.dll"), DefaultDllImportSearchPaths(DllImportSearchPath.System32)] private static partial void CoUninitialize();
        [LibraryImport("ole32.dll"), DefaultDllImportSearchPaths(DllImportSearchPath.System32)] private static partial void CoTaskMemFree(nint value);
        [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16), DefaultDllImportSearchPaths(DllImportSearchPath.System32)] private static partial int SHParseDisplayName(string name, nint binding, out nint item, uint requestedAttributes, out uint attributes);
        [LibraryImport("shell32.dll"), DefaultDllImportSearchPaths(DllImportSearchPath.System32)] private static partial int SHOpenFolderAndSelectItems(nint item, uint count, nint children, uint flags);
        [LibraryImport("shell32.dll", EntryPoint = "ShellExecuteExW", SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)] private static partial int ShellExecuteEx(ref ShellExecuteInfo info);
    }
}
