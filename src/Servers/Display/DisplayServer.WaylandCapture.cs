using SDL3;

namespace Electron2D;

public sealed partial class DisplayServer
{
    private readonly record struct WaylandCaptureOutput(uint ID, Rect2i Bounds)
    {
        internal void Copy(WaylandInput wayland, Rect2i crop, Span<byte> destination, int stride)
        {
            var properties = SDL.GetDisplayProperties(ID);
            var output = SDL.GetPointerProperty(properties, SDL.Props.DisplayWaylandWLOutputPointer, 0);
            wayland.CaptureOutput(output, Bounds.Size, new(crop.Position - Bounds.Position, crop.Size), destination, stride);
        }
    }

    private static WaylandCaptureOutput GetWaylandCaptureOutput(uint id, SDL.Rect bounds)
    {
        var size = WaylandPhysicalScreenSize(id, bounds);
        if (size.X <= 0 || size.Y <= 0 || (long)bounds.X + size.X > int.MaxValue || (long)bounds.Y + size.Y > int.MaxValue)
            throw new IOException("The Wayland output bounds are unavailable.");
        return new(id, new(new Vector2i(bounds.X, bounds.Y), size));
    }

    private WaylandCaptureOutput[] GetWaylandCaptureOutputs()
    {
        var displays = GetDisplays();
        if (displays.Length == 0)
            throw new IOException("Wayland did not report any outputs.");
        var outputs = new WaylandCaptureOutput[displays.Length];
        for (var i = 0; i < displays.Length; i++)
        {
            if (!SDL.GetDisplayBounds(displays[i], out var bounds))
                throw SDLFailure("query the Wayland output bounds");
            outputs[i] = GetWaylandCaptureOutput(displays[i], bounds);
        }
        return outputs;
    }

    private static Image CaptureWaylandScreen(WaylandInput wayland, WaylandCaptureOutput output)
    {
        var bounds = output.Bounds;
        var bytes = GC.AllocateUninitializedArray<byte>(WaylandImageSize(bounds));
        output.Copy(wayland, bounds, bytes, bounds.Size.X * 4);
        return WaylandImage(bounds, bytes);
    }

    private Image CaptureWaylandRectangle(WaylandInput wayland, Rect2i rectangle)
    {
        var outputs = GetWaylandCaptureOutputs();
        ValidateWaylandCaptureBounds(outputs, rectangle, nameof(rectangle));
        var bytes = new byte[WaylandImageSize(rectangle)];
        var stride = rectangle.Size.X * 4;

        foreach (var output in outputs)
        {
            if (!output.Bounds.Intersects(rectangle)) continue;
            if (output.Bounds.Encloses(rectangle))
            {
                output.Copy(wayland, rectangle, bytes, stride);
                return WaylandImage(rectangle, bytes);
            }
            break;
        }

        for (var i = 3; i < bytes.Length; i += 4)
            bytes[i] = 255;

        // Composite in reverse order so overlapping physical rectangles use the first screen index.
        for (var i = outputs.Length - 1; i >= 0; i--)
        {
            var output = outputs[i];
            var crop = rectangle.Intersection(output.Bounds);
            if (crop.Size.X <= 0 || crop.Size.Y <= 0) continue;
            var offset = crop.Position - rectangle.Position;
            output.Copy(wayland, crop, bytes.AsSpan(offset.Y * stride + offset.X * 4), stride);
        }
        return WaylandImage(rectangle, bytes);
    }

    private Color CaptureWaylandPixel(WaylandInput wayland, Vector2i position)
    {
        var outputs = GetWaylandCaptureOutputs();
        var crop = new Rect2i(position, Vector2i.One);
        ValidateWaylandCaptureBounds(outputs, crop, nameof(position));
        foreach (var output in outputs)
        {
            if (!output.Bounds.HasPoint(position)) continue;
            Span<byte> pixel = stackalloc byte[4];
            output.Copy(wayland, crop, pixel, 4);
            return new Color(pixel[0] / 255f, pixel[1] / 255f, pixel[2] / 255f);
        }
        return Colors.Black;
    }

    private static void ValidateWaylandCaptureBounds(WaylandCaptureOutput[] outputs, Rect2i rectangle, string argument)
    {
        var start = outputs[0].Bounds.Position;
        var end = outputs[0].Bounds.End;
        foreach (var output in outputs.AsSpan(1))
        {
            start = start.Min(output.Bounds.Position);
            end = end.Max(output.Bounds.End);
        }
        if (rectangle.Size.X <= 0 || rectangle.Size.Y <= 0 ||
            rectangle.Position.X < start.X || rectangle.Position.Y < start.Y ||
            (long)rectangle.Position.X + rectangle.Size.X > end.X || (long)rectangle.Position.Y + rectangle.Size.Y > end.Y)
            throw new ArgumentOutOfRangeException(argument, "The capture rectangle must fit inside the Wayland desktop bounds.");
    }

    private static int WaylandImageSize(Rect2i bounds) =>
        Image.GetRequiredDataSize(bounds.Size.X, bounds.Size.Y, Image.Format.Rgba8, mipmaps: false);

    private static Image WaylandImage(Rect2i bounds, byte[] bytes) =>
        Image.FromImportedState(new Image.State(bounds.Size.X, bounds.Size.Y, Image.Format.Rgba8, false, bytes));
}
