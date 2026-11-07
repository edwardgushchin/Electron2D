using System.Runtime.InteropServices;
using System.Text;

namespace Electron2D;

internal sealed unsafe partial class WaylandInput
{
    private static class CaptureProtocol
    {
        internal static readonly ProtocolInterface Source = new("ext_image_capture_source_v1", [("destroy", "")]);
        internal static readonly ProtocolInterface SourceManager = new("ext_output_image_capture_source_manager_v1",
            [("create_source", "no"), ("destroy", "")]);
        internal static readonly ProtocolInterface Frame = new("ext_image_copy_capture_frame_v1",
            [("destroy", ""), ("attach_buffer", "o"), ("damage_buffer", "iiii"), ("capture", "")],
            [("transform", "u"), ("damage", "iiii"), ("presentation_time", "uuu"), ("ready", ""), ("failed", "u")]);
        internal static readonly ProtocolInterface Session = new("ext_image_copy_capture_session_v1",
            [("create_frame", "n"), ("destroy", "")],
            [("buffer_size", "uu"), ("shm_format", "u"), ("dmabuf_device", "a"), ("dmabuf_format", "ua"), ("done", ""), ("stopped", "")]);
        internal static readonly ProtocolInterface Manager = new("ext_image_copy_capture_manager_v1",
            [("create_session", "nou"), ("create_pointer_cursor_session", "noo"), ("destroy", "")]);

        internal static void Link(nint library)
        {
            SourceManager.SetType(0, 0, Source.Handle);
            SourceManager.SetType(0, 1, NativeLibrary.GetExport(library, "wl_output_interface"));
            Manager.SetType(0, 0, Session.Handle);
            Manager.SetType(0, 1, Source.Handle);
            Session.SetType(0, 0, Frame.Handle);
            Frame.SetType(1, 0, NativeLibrary.GetExport(library, "wl_buffer_interface"));
        }
    }

    // libwayland may still inspect a retired proxy's interface while draining queued messages.
    private sealed class ProtocolInterface
    {
        private readonly byte[] _text;
        private readonly Message[] _messages;
        private readonly nint[] _types;
        private readonly Interface[] _interface = GC.AllocateArray<Interface>(1, pinned: true);

        [StructLayout(LayoutKind.Sequential)]
        private struct Message
        {
            internal byte* Name, Signature;
            internal nint* Types;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Interface
        {
            internal byte* Name;
            internal int Version, RequestCount;
            internal Message* Requests;
            internal int EventCount;
            internal Message* Events;
        }

        internal ProtocolInterface(string name, (string Name, string Signature)[] requests, (string Name, string Signature)[]? events = null)
        {
            var entries = events is null ? requests : [.. requests, .. events];
            var bytes = Encoding.ASCII.GetBytes(name + "\0" + string.Concat(entries.Select(static entry => entry.Name + "\0" + entry.Signature + "\0")));
            _text = GC.AllocateUninitializedArray<byte>(bytes.Length, pinned: true);
            bytes.CopyTo(_text, 0);
            _messages = GC.AllocateArray<Message>(entries.Length, pinned: true);
            _types = GC.AllocateArray<nint>(entries.Length * 4, pinned: true);

            fixed (byte* text = _text)
            fixed (Message* messages = _messages)
            fixed (nint* types = _types)
            {
                var offset = name.Length + 1;
                for (var i = 0; i < entries.Length; i++)
                {
                    var entry = entries[i];
                    var signature = offset + entry.Name.Length + 1;
                    _messages[i] = new() { Name = text + offset, Signature = text + signature, Types = types + i * 4 };
                    offset = signature + entry.Signature.Length + 1;
                }
                _interface[0] = new()
                {
                    Name = text,
                    Version = 1,
                    RequestCount = requests.Length,
                    Requests = messages,
                    EventCount = events?.Length ?? 0,
                    Events = events is null ? null : messages + requests.Length,
                };
            }
        }

        internal nint Handle { get { fixed (Interface* value = _interface) return (nint)value; } }
        internal void SetType(int request, int argument, nint type) => _types[request * 4 + argument] = type;
    }
}
