using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

namespace Electron2D;

/// <summary>Owns temporary UTF8 input buffers until the native call completes.</summary>
[CustomMarshaller(typeof(string), MarshalMode.ManagedToUnmanagedIn, typeof(NativeUTF8StringMarshaller.In))]
internal static unsafe class NativeUTF8StringMarshaller
{
    internal static byte* Allocate(string? value) => value is null ? null : Allocate(value, checked(Encoding.UTF8.GetByteCount(value) + 1));
    private static byte* Allocate(string value, int size)
    {
        var pointer = (byte*)NativeAllocation.Alloc((nuint)size);
        try { Encode(value, pointer, size); return pointer; }
        catch { NativeAllocation.Free(pointer); throw; }
    }
    private static void Encode(string value, byte* pointer, int size)
    {
        var written = Encoding.UTF8.GetBytes(value, new Span<byte>(pointer, size));
        pointer[written] = 0;
    }
    public ref struct In
    {
        public static int BufferSize => 256;
        private byte* _value;
        private bool _owned;
        public void FromManaged(string? value, Span<byte> buffer)
        {
            _value = null; _owned = false;
            if (value is null) return;
            var size = checked(Encoding.UTF8.GetByteCount(value) + 1);
            if (size > buffer.Length) { _value = Allocate(value, size); _owned = true; }
            else
            {
                // The source-generated caller supplies pinned scratch for the complete native call.
                _value = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(buffer));
                Encode(value, _value, size);
            }
        }
        public byte* ToUnmanaged() => _value;
        public void Free()
        {
            if (_owned) NativeAllocation.Free(_value);
            _value = null; _owned = false;
        }
    }
}
