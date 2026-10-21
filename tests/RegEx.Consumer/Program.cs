using Electron2D;

var profile = args.Length == 0 ? "managed" : args[0];
const string source = "😀 actor=hero; hp=(40+(2));";
using var expression = RegEx.CreateFromString(@"hp=(?<expr>\((?:[^()]++|(?&expr))*\))");
using var parsed = expression.Search(source) ?? throw new InvalidOperationException("Recursive configuration parse failed.");
using var digits = RegEx.CreateFromString(@"\d++");
var captures = digits.SearchAll(parsed.GetString("expr"));
int total;
try { total = captures.Sum(item => int.Parse(item.GetString(), System.Globalization.CultureInfo.InvariantCulture)); }
finally { foreach (var capture in captures) capture.Dispose(); }
if (total != 42 || parsed.GetStart("expr") != 17) throw new InvalidOperationException("Public code-point capture contract failed.");
expression.Clear();
if (parsed.GetString() != "hp=(40+(2))") throw new InvalidOperationException("Captured text did not survive program retirement.");
if (profile == "managed") { Console.WriteLine($"Public PCRE2 consumer: {profile}, total={total}."); return; }
ProjectSettings.Set(ProjectSettings.RenderingMethod, profile); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
var window = new Window { Size = new(128, 64), ContentScaleMode = ContentScaleMode.Viewport, ContentScaleSize = new(128, 64), Title = "Electron2D pattern consumer" };
window.AddChild(new Label { Text = "hero: " + total, Position = new(4, 4), Size = new(120, 56) });
window.AddChild(new Capture(window, args.Length > 1 ? args[1] : null));
Engine.Run(window);
Console.WriteLine($"Public PCRE2 consumer: {profile}, total={total}.");

sealed class Capture(Window window, string? output) : Node
{
    private int _frames;
    protected override void OnProcess(double delta)
    {
        if (++_frames < 3) return;
        using var image = window.GetTexture().GetImage() ?? throw new InvalidOperationException("Missing public rendered output.");
        var pixels = 0;
        for (var y = 2; y < 42; y++) for (var x = 2; x < 120; x++) { var c = image.GetPixel(x, y); if (c.R > .5f && c.G > .5f && c.B > .5f) pixels++; }
        if (pixels < 20) throw new InvalidOperationException("Parsed configuration did not produce text pixels.");
        if (output is not null) image.SavePNG(output);
        Tree!.Quit();
    }
}
