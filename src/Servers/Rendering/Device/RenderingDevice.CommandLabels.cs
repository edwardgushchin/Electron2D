using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private bool _commandLabelOpen;

    /// <summary>Starts a native debug region for commands recorded on this device.</summary>
    /// <param name="name">A UTF-8 region name without null characters.</param>
    /// <param name="color">The diagnostic RGBA color, with channels in the range zero to one.</param>
    /// <remarks>A new label replaces the current label. The region can span draw, compute and copy passes and ends at submission.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> contains a null character.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="color"/> contains a nonfinite or out-of-range channel.</exception>
    /// <exception cref="NotSupportedException">The current backend does not expose colored command labels.</exception>
    /// <exception cref="InvalidOperationException">The call is outside the device's allowed recording context.</exception>
    public void DrawCommandBeginLabel(string name, Color color)
    {
        Access();
        ArgumentNullException.ThrowIfNull(name);
        if (name.Contains('\0'))
            throw new ArgumentException("A debug label cannot contain null characters.", nameof(name));
        if (!color.IsFinite() || color.R is < 0 or > 1 || color.G is < 0 or > 1 ||
            color.B is < 0 or > 1 || color.A is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(color));

        int supported;
        try { supported = LabelNativeCommands(Device, 1, 0, null, 0, 0, 0, 0, 0); }
        catch (EntryPointNotFoundException) { supported = 0; }
        if (supported != 1)
            throw new NotSupportedException("The active GPU does not expose colored command labels.");

        var command = _command != 0 ? _command : Commands();
        if (LabelNativeCommands(Device, 1, command, name, color.R, color.G, color.B, color.A, _commandLabelOpen ? 1u : 0u) != 1)
            throw new InvalidOperationException("The native command buffer could not record the debug label.");
        _commandLabelOpen = true;
    }

    /// <summary>Ends the current debug region, if one is open.</summary>
    /// <exception cref="InvalidOperationException">The call is outside the device's allowed recording context.</exception>
    public void DrawCommandEndLabel()
    {
        Access();
        CloseCommandLabel();
    }

    private void CloseCommandLabel()
    {
        if (!_commandLabelOpen)
            return;
        if (LabelNativeCommands(Device, 1, _command, null, 0, 0, 0, 0, 0) != 1)
            throw new InvalidOperationException("The native command buffer could not end the debug label.");
        _commandLabelOpen = false;
    }

    [LibraryImport(NativeLibraries.EngineLibrary, EntryPoint = "e2d_render_command_label",
        StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(NativeUTF8StringMarshaller))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int LabelNativeCommands(nint device, uint abi, nint command, string? name, float r, float g, float b, float a, uint replace);
}
