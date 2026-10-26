using System.Runtime.InteropServices;
using System.Text;
using static Electron2D.GIONative;
using Element = Electron2D.AccessibilityServer.Element;
namespace Electron2D;

internal sealed unsafe partial class ATSPIServer
{
    private static string CustomDescription(Element e, int id)
    {
        if (e.CustomActions is { } actions) foreach (var action in actions) if (action.ID == id) return action.Description;
        throw new InvalidRequestException("The custom action is absent.");
    }
    private static bool InvokeCustom(Element e, int id)
    {
        if (!CanInvoke(e) || e.CustomAction is not { } callback || e.CustomActions is null) return false;
        foreach (var action in e.CustomActions)
            if (action.ID == id)
            { try { callback(id); } catch (Exception error) { throw new CallbackException(error); } return true; }
        return false;
    }
    private static nint NameVariant(Element e)
    {
        if ((!e.IncludeContainerContext || e.ContainerContext.Length == 0) && e.ExtraInfo.Length == 0 && e.Name.AsSpan().Trim().Length == e.Name.Length) return S(e.Name);
        var name = e.Name.AsSpan().TrimStart(); var container = e.IncludeContainerContext ? e.ContainerContext.AsSpan() : default; var extra = e.ExtraInfo.AsSpan().TrimEnd();
        if (name.IsEmpty) { container = container.TrimStart(); if (container.IsEmpty) extra = extra.TrimStart(); }
        if (extra.IsEmpty) { container = container.TrimEnd(); if (container.IsEmpty) name = name.TrimEnd(); }
        var capacity = checked(Encoding.UTF8.GetByteCount(name) + Encoding.UTF8.GetByteCount(container) + Encoding.UTF8.GetByteCount(extra) + 2 * (name.Count('\0') + container.Count('\0') + extra.Count('\0')) + 3);
        byte* allocated = null;
        Span<byte> buffer = capacity <= 1024 ? stackalloc byte[capacity] : new(allocated = (byte*)NativeMemory.Alloc((nuint)capacity), capacity);
        try
        {
            var at = Write(name, buffer); if (at != 0 && !container.IsEmpty) buffer[at++] = (byte)' ';
            at += Write(container, buffer[at..]); if (at != 0 && !extra.IsEmpty) buffer[at++] = (byte)' ';
            at += Write(extra, buffer[at..]); buffer[at] = 0;
            fixed (byte* p = buffer) return g_variant_new_string(p);
        }
        finally { NativeMemory.Free(allocated); }
        static int Write(ReadOnlySpan<char> text, Span<byte> destination)
        {
            var written = 0; int nul;
            while ((nul = text.IndexOf('\0')) >= 0)
            {
                written += Encoding.UTF8.GetBytes(text[..nul], destination[written..]);
                destination[written++] = 0xef; destination[written++] = 0xbf; destination[written++] = 0xbd; text = text[(nul + 1)..];
            }
            return written + Encoding.UTF8.GetBytes(text, destination[written..]);
        }
    }
}
