using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using Electron2D;
using Microsoft.Win32.SafeHandles;
using Path = System.IO.Path;

[SupportedOSPlatform("windows")]
internal static class WindowsSandboxChecks
{
    private const uint CreateSuspended = 0x00000004;
    private const uint ExtendedStartupInfoPresent = 0x00080000;
    private const uint CreateNoWindow = 0x08000000;
    private const int SecurityCapabilitiesAttribute = 0x00020009;
    private const int InsufficientBuffer = 122;
    private const uint TokenQuery = 0x0008;
    private const int TokenAppContainerSid = 31;
    private const uint WaitObject = 0;
    private const uint WaitTimeout = 258;
    private const uint ChildTimeout = 30000;

    internal static void Run()
    {
        var profile = $"Electron2D.Sandbox.{Guid.NewGuid():N}";
        var directory = Directory.CreateTempSubdirectory("Electron2D sandbox ");
        uint ordinary;
        uint sandboxed;
        try
        {
            Marshal.ThrowExceptionForHR(CreateAppContainerProfile(profile, profile, profile, 0, 0, out var sid));
            try
            {
                var package = new SecurityIdentifier(sid);
                var acl = directory.GetAccessControl();
                acl.AddAccessRule(new FileSystemAccessRule(package, FileSystemRights.ReadAndExecute,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None,
                    AccessControlType.Allow));
                directory.SetAccessControl(acl);

                const string exeName = "HostPlatform.Consumer.exe";
                var executable = Path.Combine(directory.FullName, exeName);
                foreach (var file in Directory.EnumerateFiles(AppContext.BaseDirectory))
                {
                    File.Copy(file, Path.Combine(directory.FullName, Path.GetFileName(file)));
                }

                ordinary = RunChild(executable, directory.FullName, 0, null);
                sandboxed = RunAppContainer(executable, directory.FullName, sid, package);
            }
            finally
            {
                FreeSid(sid);
                Marshal.ThrowExceptionForHR(DeleteAppContainerProfile(profile));
                Console.WriteLine("Temporary AppContainer profile deleted.");
            }
        }
        finally
        {
            directory.Delete(recursive: true);
            Console.WriteLine("Temporary test directory deleted.");
        }

        if (ordinary != 0 || sandboxed != 1)
        {
            throw new InvalidOperationException(
                $"Unexpected child results (ordinary=0x{ordinary:X8}, AppContainer=0x{sandboxed:X8}). " +
                "Expected 0 and 1. Exit 2 means display or renderer was available, exit 3 means the query threw.");
        }
    }

    internal static int Probe()
    {
        try
        {
            if (DisplayServer.IsAvailable || RenderingServer.IsAvailable)
            {
                return 2;
            }

            var sandboxed = OS.IsSandboxed();

            if (DisplayServer.IsAvailable || RenderingServer.IsAvailable)
            {
                return 2;
            }

            return sandboxed ? 1 : 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 3;
        }
    }

    private static uint RunAppContainer(string executable, string directory, nint sid, SecurityIdentifier package)
    {
        nuint size = 0;
        if (InitializeProcThreadAttributeList(0, 1, 0, ref size) ||
            Marshal.GetLastPInvokeError() != InsufficientBuffer)
        {
            throw NativeFailure(nameof(InitializeProcThreadAttributeList));
        }

        var capabilitySize = Marshal.SizeOf<SecurityCapabilities>();
        var capabilityMemory = Marshal.AllocHGlobal(capabilitySize);
        try
        {
            var capabilities = new SecurityCapabilities { AppContainerSid = sid };
            Marshal.StructureToPtr(capabilities, capabilityMemory, false);

            var attributes = Marshal.AllocHGlobal(checked((nint)size));
            try
            {
                if (!InitializeProcThreadAttributeList(attributes, 1, 0, ref size))
                {
                    throw NativeFailure(nameof(InitializeProcThreadAttributeList));
                }

                try
                {
                    if (!UpdateProcThreadAttribute(attributes, 0, SecurityCapabilitiesAttribute, capabilityMemory,
                            (nuint)capabilitySize, 0, 0))
                    {
                        throw NativeFailure(nameof(UpdateProcThreadAttribute));
                    }

                    return RunChild(executable, directory, attributes, package);
                }
                finally
                {
                    DeleteProcThreadAttributeList(attributes);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(attributes);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(capabilityMemory);
        }
    }

    private static uint RunChild(string executable, string directory, nint attributes, SecurityIdentifier? package)
    {
        var flags = CreateSuspended | CreateNoWindow;
        var startup = new StartupInfoEx { Attributes = attributes };
        startup.Info.Size = (uint)Marshal.SizeOf<StartupInfo>();
        if (attributes != 0)
        {
            flags |= ExtendedStartupInfoPresent;
            startup.Info.Size = (uint)Marshal.SizeOf<StartupInfoEx>();
        }

        var command = new StringBuilder($"\"{executable}\" sandbox-child");
        if (!CreateProcess(executable, command, 0, 0, false, flags, 0, directory, ref startup, out var processInfo))
        {
            throw NativeFailure(nameof(CreateProcess));
        }

        using var process = new SafeWaitHandle(processInfo.Process, ownsHandle: true);
        using var thread = new SafeWaitHandle(processInfo.Thread, ownsHandle: true);
        var exited = false;
        try
        {
            var actual = ObservePackage(process);
            if (!Equals(actual, package))
            {
                throw new InvalidOperationException($"Child token has AppContainer SID {actual}; expected {package}.");
            }

            if (ResumeThread(thread) == uint.MaxValue)
            {
                throw NativeFailure(nameof(ResumeThread));
            }

            var wait = WaitForSingleObject(process, ChildTimeout);
            if (wait == WaitTimeout)
            {
                throw new TimeoutException("The sandbox child did not exit.");
            }

            if (wait != WaitObject)
            {
                throw NativeFailure(nameof(WaitForSingleObject));
            }

            exited = true;
            if (!GetExitCodeProcess(process, out var exit))
            {
                throw NativeFailure(nameof(GetExitCodeProcess));
            }

            Console.WriteLine($"Child token: {actual?.Value ?? "ordinary"}; public probe exit: 0x{exit:X8}.");
            return exit;
        }
        finally
        {
            if (!exited && WaitForSingleObject(process, 0) != WaitObject)
            {
                // Only this fixture's child is ever terminated.
                if (!TerminateProcess(process, 4))
                {
                    var error = NativeFailure(nameof(TerminateProcess));
                    if (WaitForSingleObject(process, 0) != WaitObject)
                    {
                        throw error;
                    }
                }

                if (WaitForSingleObject(process, ChildTimeout) != WaitObject)
                {
                    throw new InvalidOperationException("Could not wait for the terminated sandbox child.");
                }
            }
        }
    }

    private static SecurityIdentifier? ObservePackage(SafeWaitHandle process)
    {
        // Read the suspended child's package identity without calling the engine's boolean query.
        if (!OpenProcessToken(process, TokenQuery, out var token))
        {
            throw NativeFailure(nameof(OpenProcessToken));
        }

        using (token)
        {
            if (GetTokenInformation(token, TokenAppContainerSid, 0, 0, out var size) ||
                Marshal.GetLastPInvokeError() != InsufficientBuffer)
            {
                throw NativeFailure(nameof(GetTokenInformation));
            }

            var buffer = Marshal.AllocHGlobal(checked((int)size));
            try
            {
                if (!GetTokenInformation(token, TokenAppContainerSid, buffer, size, out _))
                {
                    throw NativeFailure(nameof(GetTokenInformation));
                }

                var sid = Marshal.ReadIntPtr(buffer);
                return sid == 0 ? null : new SecurityIdentifier(sid);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
    }

    private static Win32Exception NativeFailure(string call)
    {
        var code = Marshal.GetLastPInvokeError();
        return new Win32Exception(code, $"{call}: {new Win32Exception(code).Message} ({code})");
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SecurityCapabilities
    {
        public nint AppContainerSid;
        public nint Capabilities;
        public uint CapabilityCount;
        public uint Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct StartupInfo
    {
        public uint Size;
        public nint Reserved;
        public nint Desktop;
        public nint Title;
        public uint X;
        public uint Y;
        public uint Width;
        public uint Height;
        public uint BufferWidth;
        public uint BufferHeight;
        public uint FillAttribute;
        public uint Flags;
        public ushort ShowWindow;
        public ushort ReservedSize;
        public nint ReservedData;
        public nint StandardInput;
        public nint StandardOutput;
        public nint StandardError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct StartupInfoEx
    {
        public StartupInfo Info;
        public nint Attributes;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessInformation
    {
        public nint Process;
        public nint Thread;
        public uint ProcessId;
        public uint ThreadId;
    }

    [DllImport("userenv.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int CreateAppContainerProfile(string name, string displayName, string description,
        nint capabilities, uint capabilityCount, out nint sid);

    [DllImport("userenv.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int DeleteAppContainerProfile(string name);

    [DllImport("advapi32.dll")]
    private static extern nint FreeSid(nint sid);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool InitializeProcThreadAttributeList(nint attributes, int count, uint flags, ref nuint size);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool UpdateProcThreadAttribute(nint attributes, uint flags, nuint attribute, nint value,
        nuint size, nint previous, nint returnSize);

    [DllImport("kernel32.dll")]
    private static extern void DeleteProcThreadAttributeList(nint attributes);

    [DllImport("kernel32.dll", EntryPoint = "CreateProcessW", CharSet = CharSet.Unicode, ExactSpelling = true,
        SetLastError = true)]
    private static extern bool CreateProcess(string executable, StringBuilder command, nint processAttributes,
        nint threadAttributes, bool inheritHandles, uint flags, nint environment, string directory,
        ref StartupInfoEx startup, out ProcessInformation process);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenProcessToken(SafeWaitHandle process, uint access, out SafeAccessTokenHandle token);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool GetTokenInformation(SafeAccessTokenHandle token, int informationClass,
        nint buffer, uint size, out uint required);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint ResumeThread(SafeWaitHandle thread);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WaitForSingleObject(SafeWaitHandle process, uint milliseconds);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetExitCodeProcess(SafeWaitHandle process, out uint exitCode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool TerminateProcess(SafeWaitHandle process, uint exitCode);
}
