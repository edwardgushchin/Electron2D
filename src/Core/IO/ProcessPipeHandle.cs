using Microsoft.Win32.SafeHandles;
using SDL3;

namespace Electron2D;

internal sealed class ProcessPipeHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    private readonly OSProcess _owner;
    internal ProcessPipeHandle(OSProcess owner, nint pipe) : base(true) { _owner = owner; SetHandle(pipe); owner.AcquirePipe(); }
    protected override bool ReleaseHandle()
    {
        lock (_owner.Gate) { var closed = SDL.CloseIO(handle); _owner.ReleasePipe(); return closed; }
    }
}
