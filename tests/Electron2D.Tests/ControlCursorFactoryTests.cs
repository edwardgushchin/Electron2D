using System.Reflection;
using Electron2D;
using SDL3;
using Image = Electron2D.Image;

internal static class ControlCursorFactoryTests
{
    private static readonly BindingFlags PrivateStatic = BindingFlags.Static | BindingFlags.NonPublic;
    private static Delegate? _destroy;
    private static int _destroyCalls;
    private static readonly HashSet<nint> Destroyed = [];
    private static nint FailCreate(SDL.SystemCursor shape) => 0;
    private static void Destroy(nint handle) { _destroyCalls++; Destroyed.Add(handle); _destroy!.DynamicInvoke(handle); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    internal static void Run()
    {
        var createField = typeof(SDL).GetField("CreateSystemCursorNativeFunction", PrivateStatic)!;
        var destroyField = typeof(SDL).GetField("DestroyCursorNativeFunction", PrivateStatic)!;
        var create = createField.GetValue(null);
        _destroy = (Delegate)destroyField.GetValue(null)!;
        var installed = new HashSet<nint>(); Destroyed.Clear(); _destroyCalls = 0;
        destroyField.SetValue(null, Delegate.CreateDelegate(destroyField.FieldType, typeof(ControlCursorFactoryTests).GetMethod(nameof(Destroy), PrivateStatic)!));
        try
        {
            using (var display = DisplayServer.Open("Cursor factory failure/lifetime", new(96, 64)))
            {
                DisplayServer.CursorSetShape(CursorShape.Arrow);
                var previous = SDL.GetCursor(); installed.Add(previous);
                createField.SetValue(null, Delegate.CreateDelegate(createField.FieldType, typeof(ControlCursorFactoryTests).GetMethod(nameof(FailCreate), PrivateStatic)!));
                try
                {
                    try { DisplayServer.CursorSetShape(CursorShape.IBeam); throw new Exception("Expected system creation failure."); }
                    catch (InvalidOperationException) { }
                    Check(SDL.GetCursor() == previous && DisplayServer.CursorGetShape() == CursorShape.Arrow, "Failed system creation preserves native handle and shape.");
                    using var image = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8);
                    image.Fill(Colors.Red);
                    DisplayServer.CursorSetCustomImage(image, CursorShape.IBeam);
                    DisplayServer.CursorSetShape(CursorShape.IBeam);
                    var custom = SDL.GetCursor(); installed.Add(custom);
                    try { DisplayServer.CursorSetCustomImage(null, CursorShape.IBeam); throw new Exception("Expected restore failure."); }
                    catch (InvalidOperationException) { }
                    Check(SDL.GetCursor() == custom && !Destroyed.Contains(custom), "Failed system restoration preserves the active custom handle.");
                }
                finally { createField.SetValue(null, create); }
                DisplayServer.CursorSetCustomImage(null, CursorShape.IBeam);
                for (var index = 0; index < (int)CursorShape.Max; index++)
                {
                    var shape = (CursorShape)index;
                    DisplayServer.CursorSetShape(shape); installed.Add(SDL.GetCursor());
                    Check(SDL.GetCursor() != 0 && DisplayServer.CursorGetShape() == shape, "Every standard cursor installs natively.");
                    using var image = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8);
                    DisplayServer.CursorSetCustomImage(image, shape); installed.Add(SDL.GetCursor());
                    DisplayServer.CursorSetCustomImage(null, shape);
                    Check(DisplayServer.CursorGetShape() == shape && SDL.GetCursor() != 0, "Every custom slot restores through the shared factory.");
                }
            }
            Check(installed.All(Destroyed.Contains), "Display shutdown destroys all installed owned SDL cursor handles.");
            Check(_destroyCalls == 35, $"Exactly 17 system and 18 custom cursor lifetimes retire; actual {_destroyCalls}.");
        }
        finally { createField.SetValue(null, create); destroyField.SetValue(null, _destroy); _destroy = null; }
        Console.WriteLine($"Cursor factory: 17 standard/custom slots, create/restore failures and {installed.Count} owned handle retirements passed.");
    }
}
