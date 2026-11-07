using static Electron2D.LinuxPortalDBus;

namespace Electron2D;

internal sealed class LinuxPortalColorPicker : IDisposable
{
    internal const string Interface = "org.freedesktop.portal.Screenshot";
    internal const uint RequiredVersion = 1;
    private readonly LinuxPortalRequest _request;
    internal Action<Color?> Completed { get; }
    internal bool Canceled { get; private set; }

    internal LinuxPortalColorPicker(string parent, Action<Color?> completed)
    {
        Completed = completed;
        _request = new(Interface, RequiredVersion);
        try { _request.Launch("PickColor", (message, token) => WriteArguments(message, parent, token)); }
        catch { _request.Dispose(); throw; }
    }

    private static void WriteArguments(nint message, string parent, string token)
    {
        InitAppend(message, out var args);
        AppendString(ref args, parent);
        var options = Open(ref args, ArrayType, "{sv}");
        var entry = Open(ref options, DictEntryType, null);
        AppendString(ref entry, "handle_token");
        var value = Open(ref entry, VariantType, "s");
        AppendString(ref value, token);
        Close(ref entry, ref value);
        Close(ref options, ref entry);
        Close(ref args, ref options);
    }

    internal bool TryComplete(out Color? color)
    {
        color = null;
        try
        {
            var message = _request.TakeResponse();
            if (message == 0) return false;
            try { color = ReadResponse(message); }
            finally { MessageUnref(message); }
        }
        catch (InvalidOperationException) { return true; }
        return true;
    }

    private static Color? ReadResponse(nint message)
    {
        if (Init(message, out var args) == 0 || ReadUInt32(ref args) != 0)
            return null;
        Next(ref args);
        Require(ref args, ArrayType);
        Recurse(ref args, out var entries);
        Color? color = null;
        while (GetArgType(ref entries) != 0)
        {
            Require(ref entries, DictEntryType);
            Recurse(ref entries, out var entry);
            if (ReadString(ref entry) == "color")
            {
                if (color.HasValue)
                    throw new InvalidOperationException("The portal returned multiple colors.");
                Next(ref entry);
                Require(ref entry, VariantType);
                Recurse(ref entry, out var value);
                Require(ref value, StructType);
                Recurse(ref value, out var channels);
                var red = ReadChannel(ref channels);
                var green = ReadChannel(ref channels);
                var blue = ReadChannel(ref channels);
                if (GetArgType(ref channels) != 0)
                    throw new InvalidOperationException("The portal returned too many color channels.");
                color = new Color(red, green, blue);
            }
            Next(ref entries);
        }
        return color;
    }

    private static float ReadChannel(ref MessageIter channel)
    {
        var value = ReadDouble(ref channel);
        Next(ref channel);
        if (!double.IsFinite(value) || value is < 0 or > 1)
            throw new InvalidOperationException("The portal returned an invalid color channel.");
        return (float)value;
    }

    internal void Cancel()
    {
        Canceled = true;
        Dispose();
    }

    public void Dispose() => _request.Dispose();
}
