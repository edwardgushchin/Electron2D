using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;

namespace Electron2D;

public sealed partial class OS
{
    [SupportedOSPlatform("windows")]
    internal static unsafe partial class WindowsTrash
    {
        private static readonly Guid FileOperationClass = new("3AD05575-8857-4850-9277-11B85BDB8E09"), FileOperationInterface = new("947AAB5F-0A5C-4C13-B4D6-4BF7836FC9F8"), ShellItemInterface = new("43826D1E-E718-42EE-BC55-A1E261C37BFE");
        internal static void Move(string path)
        {
            // Shell operations require an STA even when a game calls from its ordinary MTA owner.
            ExceptionDispatchInfo? failure = null;
            var worker = new System.Threading.Thread(() => { try { Recycle(path); } catch (Exception error) { failure = ExceptionDispatchInfo.Capture(error); } }) { IsBackground = true, Name = "Electron2D recycle" };
            worker.SetApartmentState(ApartmentState.STA); worker.Start(); worker.Join(); failure?.Throw();
        }
        private static void Check(int result)
        { if (result < 0) throw new IOException($"The Windows recycle operation failed (HRESULT 0x{result:X8}).", result); }
        private static void Release(nint value)
        { if (value != 0) ((delegate* unmanaged[Stdcall]<nint, uint>)(*(nint**)value)[2])(value); }
        private static void Recycle(string path)
        {
            path = path.Replace('/', '\\');
            if (System.IO.Path.TrimEndingDirectorySeparator(path) == System.IO.Path.GetPathRoot(path)) throw new IOException("A filesystem root cannot be recycled.");
            Check(CoInitializeEx(0, 6));
            nint operation = 0, item = 0, sink = 0;
            try
            {
                var creation = CoCreateInstance(in FileOperationClass, 0, 1, in FileOperationInterface, out operation);
                if (creation == unchecked((int)0x80040154) || creation == unchecked((int)0x80004002)) throw new NotSupportedException("The Windows Shell recycle service is unavailable.");
                Check(creation);
                var methods = *(nint**)operation;
                // Recycle-only, undo record, no connected HTML companions/UI/elevation, fail at the first error.
                Check(((delegate* unmanaged[Stdcall]<nint, uint, int>)methods[5])(operation, 0x20182614));
                Check(SHCreateItemFromParsingName(path, 0, in ShellItemInterface, out item));
                var progress = new RecycleProgress(); sink = (nint)ComInterfaceMarshaller<IRecycleProgress>.ConvertToUnmanaged(progress);
                Check(((delegate* unmanaged[Stdcall]<nint, nint, nint, int>)methods[18])(operation, item, sink));
                var result = ((delegate* unmanaged[Stdcall]<nint, int>)methods[21])(operation);
                int aborted = 0; var status = ((delegate* unmanaged[Stdcall]<nint, int*, int>)methods[22])(operation, &aborted);
                if (progress.RefusedPermanentDeletion) throw new IOException("The Windows Shell offered permanent deletion; the source was not recycled.");
                Check(result); Check(status); Check(progress.Error);
                if (aborted != 0 || !progress.Completed) throw new IOException("The Windows recycle operation did not complete; permanent deletion is not permitted.");
            }
            finally
            {
                Release(item); Release(operation);
                if (sink != 0) ComInterfaceMarshaller<IRecycleProgress>.Free((void*)sink);
                CoUninitialize();
            }
        }
        [LibraryImport("ole32.dll"), DefaultDllImportSearchPaths(DllImportSearchPath.System32)] private static partial int CoInitializeEx(nint reserved, uint mode);
        [LibraryImport("ole32.dll"), DefaultDllImportSearchPaths(DllImportSearchPath.System32)] private static partial void CoUninitialize();
        [LibraryImport("ole32.dll"), DefaultDllImportSearchPaths(DllImportSearchPath.System32)] private static partial int CoCreateInstance(in Guid classID, nint outer, uint context, in Guid interfaceID, out nint value);
        [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16), DefaultDllImportSearchPaths(DllImportSearchPath.System32)] private static partial int SHCreateItemFromParsingName(string path, nint binding, in Guid interfaceID, out nint value);

        [GeneratedComInterface, Guid("04B0F1A7-9490-44BC-96E1-4296A31252E2")]
        internal partial interface IRecycleProgress
        {
            [PreserveSig] int StartOperations();
            [PreserveSig] int FinishOperations(int result);
            [PreserveSig] int PreRenameItem(uint flags, nint item, nint name);
            [PreserveSig] int PostRenameItem(uint flags, nint item, nint name, int result, nint created);
            [PreserveSig] int PreMoveItem(uint flags, nint item, nint destination, nint name);
            [PreserveSig] int PostMoveItem(uint flags, nint item, nint destination, nint name, int result, nint created);
            [PreserveSig] int PreCopyItem(uint flags, nint item, nint destination, nint name);
            [PreserveSig] int PostCopyItem(uint flags, nint item, nint destination, nint name, int result, nint created);
            [PreserveSig] int PreDeleteItem(uint flags, nint item);
            [PreserveSig] int PostDeleteItem(uint flags, nint item, int result, nint created);
            [PreserveSig] int PreNewItem(uint flags, nint destination, nint name);
            [PreserveSig] int PostNewItem(uint flags, nint destination, nint name, nint template, uint attributes, int result, nint created);
            [PreserveSig] int UpdateProgress(uint total, uint completed);
            [PreserveSig] int ResetTimer();
            [PreserveSig] int PauseTimer();
            [PreserveSig] int ResumeTimer();
        }
        [GeneratedComClass]
        internal sealed partial class RecycleProgress : IRecycleProgress
        {
            internal int Error;
            internal bool Completed;
            internal bool RefusedPermanentDeletion;
            public int StartOperations() => 0;
            public int FinishOperations(int result) => 0;
            public int PreRenameItem(uint flags, nint item, nint name) => 0;
            public int PostRenameItem(uint flags, nint item, nint name, int result, nint created) => 0;
            public int PreMoveItem(uint flags, nint item, nint destination, nint name) => 0;
            public int PostMoveItem(uint flags, nint item, nint destination, nint name, int result, nint created) => 0;
            public int PreCopyItem(uint flags, nint item, nint destination, nint name) => 0;
            public int PostCopyItem(uint flags, nint item, nint destination, nint name, int result, nint created) => 0;
            public int PreDeleteItem(uint flags, nint item)
            { if ((flags & 0x80) != 0) return 0; RefusedPermanentDeletion = true; return Error = unchecked((int)0x80004004); }
            public int PostDeleteItem(uint flags, nint item, int result, nint created)
            { if (result < 0) Error = result; else if (created == 0) Error = unchecked((int)0x80004005); else Completed = true; return 0; }
            public int PreNewItem(uint flags, nint destination, nint name) => 0;
            public int PostNewItem(uint flags, nint destination, nint name, nint template, uint attributes, int result, nint created) => 0;
            public int UpdateProgress(uint total, uint completed) => 0;
            public int ResetTimer() => 0;
            public int PauseTimer() => 0;
            public int ResumeTimer() => 0;
        }
    }
}
