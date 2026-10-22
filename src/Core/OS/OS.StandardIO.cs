using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Electron2D;

public sealed partial class OS
{
    private readonly object _standardInputGate = new();

    /// <summary>Identifies the native device behind a process standard handle.</summary>
    public enum StdHandleType
    {
        /// <summary>The native handle is absent or invalid.</summary>
        Invalid = 0,
        /// <summary>An interactive terminal or console.</summary>
        Console = 1,
        /// <summary>A regular file.</summary>
        File = 2,
        /// <summary>A pipe or FIFO.</summary>
        Pipe = 3,
        /// <summary>A valid device of another or unrecognized kind.</summary>
        Unknown = 4
    }

    /// <summary>Gets the current native standard-input device type without reading it.</summary>
    /// <returns>The device type; an absent handle is Invalid.</returns>
    /// <exception cref="PlatformNotSupportedException">The host lacks a standard-handle adapter.</exception>
    public static StdHandleType GetSTDINType() => Service.GetStandardHandleType(0);

    /// <summary>Gets the current native standard-output device type without writing to it.</summary>
    /// <returns>The device type; an absent handle is Invalid.</returns>
    /// <exception cref="PlatformNotSupportedException">The host lacks a standard-handle adapter.</exception>
    public static StdHandleType GetSTDOUTType() => Service.GetStandardHandleType(1);

    /// <summary>Gets the current native standard-error device type without writing to it.</summary>
    /// <returns>The device type; an absent handle is Invalid.</returns>
    /// <exception cref="PlatformNotSupportedException">The host lacks a standard-handle adapter.</exception>
    public static StdHandleType GetSTDERRType() => Service.GetStandardHandleType(2);

    /// <summary>Reads an owned raw byte buffer from process standard input.</summary>
    /// <param name="bufferSize">Maximum byte count, positive and no larger than the managed array limit.</param>
    /// <returns>Bytes actually read, or an empty array at end-of-input.</returns>
    /// <remarks>Pipe/file reads wait for the requested count or EOF. Terminal reads return an available input line or prefix.
    /// This blocking, allocating operation belongs on a worker when the application must keep rendering.
    /// Engine reads are serialized; external Console/native readers and handle replacement must be coordinated by the application.
    /// The process handle is borrowed and is never closed by this service.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The requested count is invalid.</exception>
    /// <exception cref="IOException">The input handle is invalid or reading fails.</exception>
    /// <exception cref="PlatformNotSupportedException">The host lacks a standard-handle adapter.</exception>
    public static byte[] ReadBufferFromSTDIN(int bufferSize = 1024)
    {
        ValidateInputSize(bufferSize, 1);
        lock (Service._standardInputGate)
        {
            var type = GetSTDINType();
            if (type == StdHandleType.Invalid) throw new IOException("Standard input has no valid native handle.");
            using var input = OpenStandardInput();
            var buffer = new byte[bufferSize];
            var count = 0;
            do
            {
                var read = input.Read(buffer.AsSpan(count));
                if (read == 0) break;
                count += read;
            } while (count < bufferSize && type != StdHandleType.Console);
            if (count != buffer.Length) Array.Resize(ref buffer, count);
            return buffer;
        }
    }

    /// <summary>Reads a bounded UTF-8 input line without consuming the following line.</summary>
    /// <param name="bufferSize">Byte capacity including the terminating boundary; at least two. At most bufferSize minus one bytes are consumed.</param>
    /// <returns>The decoded prefix, with CRLF normalized to LF and trailing LF removed, or empty text at EOF.</returns>
    /// <remarks>Reads stop at LF, the byte limit or EOF. Bytes consumed before an I/O or UTF-8 decoding failure are not restored.
    /// A split UTF-8 sequence is rejected; use raw buffers and a caller-owned decoder for arbitrary chunk framing.
    /// This is a cold blocking operation. The process handle remains borrowed.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The capacity is below two or exceeds the managed array limit.</exception>
    /// <exception cref="System.Text.DecoderFallbackException">Consumed bytes are not complete valid UTF-8.</exception>
    /// <exception cref="IOException">The input handle is invalid or reading fails.</exception>
    /// <exception cref="PlatformNotSupportedException">The host lacks a standard-handle adapter.</exception>
    public static string ReadStringFromSTDIN(int bufferSize = 1024)
    {
        ValidateInputSize(bufferSize, 2);
        lock (Service._standardInputGate)
        {
            if (GetSTDINType() == StdHandleType.Invalid) throw new IOException("Standard input has no valid native handle.");
            using var input = OpenStandardInput();
            var buffer = new byte[bufferSize - 1];
            var count = 0;
            while (count < buffer.Length)
            {
                var value = input.ReadByte();
                if (value < 0) break;
                buffer[count++] = (byte)value;
                if (value == '\n') break;
            }
            return ProcessUTF8.GetString(buffer, 0, count).Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd('\n');
        }
    }

    private static Stream OpenStandardInput()
    {
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() || OperatingSystem.IsTvOS() || OperatingSystem.IsMacCatalyst() || OperatingSystem.IsBrowser()) throw new PlatformNotSupportedException("This host has no standard-handle adapter.");
        return System.Console.OpenStandardInput();
    }

    private static void ValidateInputSize(int bufferSize, int minimum)
    {
        if (bufferSize < minimum || bufferSize > Array.MaxLength) throw new ArgumentOutOfRangeException(nameof(bufferSize));
    }

    private unsafe StdHandleType GetStandardHandleType(int descriptor)
    {
        if (OperatingSystem.IsWindows())
        {
            var handle = GetNativeStandardHandle(-10 - descriptor);
            if (handle == 0 || handle == -1) return StdHandleType.Invalid;
            if (GetNativeConsoleMode(handle, out _)) return StdHandleType.Console;
            var kind = GetNativeFileType(handle);
            if (kind == 0 && Marshal.GetLastPInvokeError() == 6) return StdHandleType.Invalid;
            return kind switch { 1 => StdHandleType.File, 3 => StdHandleType.Pipe, _ => StdHandleType.Unknown };
        }
        if (OperatingSystem.IsLinux() && !OperatingSystem.IsAndroid())
        {
            if (IsNativeTerminal(descriptor) == 1) return StdHandleType.Console;
            // The fixed statx ABI places the native mode at byte 28.
            Span<byte> status = stackalloc byte[256];
            status.Clear();
            fixed (byte* data = status)
            {
                int result;
                try { result = GetLinuxHandleStatus(descriptor, "", 0x1000, 1, data); }
                catch (EntryPointNotFoundException error) { throw new PlatformNotSupportedException("The host libc has no statx descriptor adapter.", error); }
                if (result != 0)
                {
                    var error = Marshal.GetLastPInvokeError();
                    if (error == 9) return StdHandleType.Invalid;
                    if (error == 38) throw new PlatformNotSupportedException("The host kernel has no statx descriptor adapter.");
                    throw new IOException("Cannot inspect the standard handle.", new Win32Exception(error));
                }
            }
            return (MemoryMarshal.Read<ushort>(status[28..]) & 0xf000) switch
            {
                0x8000 or 0xa000 => StdHandleType.File,
                0x1000 => StdHandleType.Pipe,
                _ => StdHandleType.Unknown
            };
        }
        throw new PlatformNotSupportedException("This host has no standard-handle adapter.");
    }

    [LibraryImport("libc", EntryPoint = "isatty")]
    private static partial int IsNativeTerminal(int descriptor);
    [LibraryImport("libc", EntryPoint = "statx", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    private static unsafe partial int GetLinuxHandleStatus(int descriptor, string path, int flags, uint mask, byte* status);
    [LibraryImport("kernel32.dll", EntryPoint = "GetStdHandle")]
    private static partial nint GetNativeStandardHandle(int kind);
    [LibraryImport("kernel32.dll", EntryPoint = "GetFileType", SetLastError = true)]
    private static partial uint GetNativeFileType(nint handle);
    [LibraryImport("kernel32.dll", EntryPoint = "GetConsoleMode")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetNativeConsoleMode(nint handle, out uint mode);
}
