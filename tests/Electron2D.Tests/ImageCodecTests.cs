using IOPath = System.IO.Path;
using System.Buffers.Binary;
using System.Text;
using Electron2D;

internal static class ImageCodecTests
{
    private static readonly byte[] Pixels = [255, 0, 0, 128, 0, 255, 0, 255, 0, 0, 255, 255, 255, 255, 0, 255, 255, 0, 255, 255, 0, 255, 255, 255];
    private const string WEBP = "UklGRjQAAABXRUJQVlA4TCgAAAAvAkAAEC8gEEjaH3qN+RcQFPk/moCg6Lrlgh9EMgoCATJEjBIR/Y9Y";
    // libwebpmux fixture: a red 2x2 first frame at (2, 2) on a transparent 4x4 canvas, then a blue frame.
    private const string AnimatedWEBP = "UklGRoQAAABXRUJQVlA4WAoAAAACAAAAAwAAAwAAQU5JTQYAAAAAAAAAAABBTk1GKAAAAAEAAAEAAAEAAAEAAGQAAAJWUDhMDwAAAC8BQAAABxD9j/4HIqL/AQBBTk1GKAAAAAAAAAAAAAMAAAMAAGQAAAJWUDhMDwAAAC8DwAAABxDR//4HIqL/AQA=";
    private const string SVG = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"3px\" height=\"2px\"><rect width=\"3\" height=\"2\" fill=\"#ff0000\"/></svg>";

    internal static void Run()
    {
        VerifyNativeIOCounts();
        using var source = Image.CreateFromData(3, 2, false, Image.Format.Rgba8, Pixels);
        source.GenerateMIPMaps();
        var before = source.GetData();
        var changes = 0;
        source.Changed += _ => changes++;
        var png = source.SavePNGToBuffer();
        Check(source.GetData().SequenceEqual(before) && source.HasMIPMaps && changes == 0, "Encoding uses a snapshot without changing the source.");
        using var image = new Image();
        var loaded = 0;
        image.Changed += _ => loaded++;
        image.LoadPNGFromBuffer(png);
        CheckPixels(image, Pixels);
        Check(!image.HasMIPMaps && loaded == 1, "Load replaces the whole image and emits Changed once.");
        image.LoadWebPFromBuffer(Convert.FromBase64String(WEBP));
        CheckPixels(image, Pixels);
        VerifyAnimatedWebP();
        VerifySVG();
        VerifyBMP();
        VerifyTGA();
        VerifyPNG();

        var jpg = VerifyJPEG();

        image.LoadPNGFromBuffer(png);
        var count = loaded;
        var badDimensions = (byte[])png.Clone();
        BinaryPrimitives.WriteUInt32BigEndian(badDimensions.AsSpan(16), 0x7fffffff);
        foreach (var bytes in new[] { Array.Empty<byte>(), png[..12], png[..^1], badDimensions, jpg, new byte[] { 0, 1, 2, 3 } })
            Reject<InvalidDataException>(() => image.LoadPNGFromBuffer(bytes));
        Reject<InvalidDataException>(() => image.LoadJPGFromBuffer(png));
        Reject<InvalidDataException>(() => image.LoadWebPFromBuffer(png));
        Reject<InvalidDataException>(() => image.LoadBMPFromBuffer(png));
        Reject<InvalidDataException>(() => image.LoadTGAFromBuffer(png));
        Reject<InvalidDataException>(() => image.LoadSVGFromBuffer(png));
        var corrupt = (byte[])png.Clone();
        Array.Fill(corrupt, (byte)255, 41, 8);
        Reject<InvalidDataException>(() => image.LoadPNGFromBuffer(corrupt));
        CheckPixels(image, Pixels);
        Check(loaded == count, "Header and decoder failures leave pixels and notifications unchanged.");
        foreach (var svg in new[] { "<svg width=\"50000\" height=\"2\"/>", "<!DOCTYPE svg [<!ENTITY x SYSTEM 'file:///etc/passwd'>]><svg>&x;</svg>", "<svg width=\"3\" height=\"2\"><rect" })
            Reject<InvalidDataException>(() => image.LoadSVGFromString(svg));
        foreach (var scale in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
            Reject<ArgumentOutOfRangeException>(() => image.LoadSVGFromString(SVG, scale));
        CheckPixels(image, Pixels);
        Check(loaded == count, "SVG preflight failures preserve image state and notifications.");
        Reject<InvalidDataException>(() => image.LoadPNGFromBuffer(new byte[64 * 1024 * 1024 + 1]));

        using var empty = new Image();
        Reject<InvalidOperationException>(() => empty.SavePNGToBuffer());
        VerifyFiles(source, png, jpg);
        using var callback = new Image();
        callback.Changed += _ => throw new ApplicationException("codec callback");
        Reject<ApplicationException>(() => callback.LoadPNGFromBuffer(png));
        CheckPixels(callback, Pixels);
        image.Dispose();
        Reject<ObjectDisposedException>(() => image.LoadPNGFromBuffer(png));
        Reject<ObjectDisposedException>(() => image.LoadSVGFromString(SVG));
        Reject<ObjectDisposedException>(() => image.SavePNGToBuffer());
        if (!OperatingSystem.IsBrowser())
            Parallel.For(0, 12, _ => { using var copy = new Image(); copy.LoadPNGFromBuffer(png); copy.LoadPNGFromBuffer(copy.SavePNGToBuffer()); CheckPixels(copy, Pixels); });
        Console.WriteLine("Image codec checks passed (PNG/JPEG/WebP/BMP/TGA/SVG; buffers, files, failures, ownership).");
    }

    internal static void VerifySVG()
    {
        using var image = new Image();
        image.LoadSVGFromString(SVG);
        Check(image.Size == new Vector2i(3, 2) && image.PixelFormat == Image.Format.Rgba8 && image.GetPixel(1, 1) == Colors.Red,
            "SVG string rasterizes at intrinsic size and color.");
        image.LoadSVGFromBuffer(Encoding.UTF8.GetBytes(SVG), 2);
        Check(image.Size == new Vector2i(6, 4) && image.GetPixel(3, 2) == Colors.Red,
            "SVG buffer scale changes raster dimensions and preserves fill.");
        image.LoadSVGFromString("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 4 3\"><rect width=\"4\" height=\"3\" fill=\"#00ff00\"/></svg>");
        Check(image.Size == new Vector2i(4, 3) && image.GetPixel(1, 1) == Colors.Green,
            "SVG viewBox supplies intrinsic dimensions.");

        const string styled = """<svg xmlns="http://www.w3.org/2000/svg" width="2" height="2" style="width:6px; height:3px"><rect width="4" height="3" fill="red"/></svg>""";
        var changes = 0;
        image.Changed += _ => changes++;
        image.LoadSVGFromString(styled);
        Check(image.Size == new Vector2i(6, 3) && image.GetPixel(3, 2) == Colors.Red &&
            image.GetPixel(5, 2).A == 0 && changes == 1, "Inline dimensions override the attributes and set the actual viewport.");
        image.LoadSVGFromBuffer(Encoding.UTF8.GetBytes(styled), 2);
        Check(image.Size == new Vector2i(12, 6) && image.GetPixel(7, 4) == Colors.Red &&
            image.GetPixel(11, 4).A == 0 && changes == 2, "The inline viewport scales without stretching its contents.");

        const string viewBox = """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 4 4" style="--ignored:'; height:100px'; width:2px; WIDTH:8px !important; width:9px; height:/**/100%"><rect width="4" height="4" fill="#00ff00"/></svg>""";
        image.LoadSVGFromString(viewBox);
        Check(image.Size == new Vector2i(8, 4) && image.GetPixel(0, 1).A == 0 &&
            image.GetPixel(3, 1) == Colors.Lime && image.GetPixel(7, 1).A == 0, "Inline declaration precedence and viewBox alignment determine raster pixels.");
        image.LoadSVGFromString("""<svg style="width:5.2px;height:2.1px"><rect width="2" height="2" fill="red"/></svg>""", 1.5f);
        Check(image.Size == new Vector2i(8, 3) && image.GetPixel(1, 1) == Colors.Red,
            "Fractional lengths round once at the requested scale.");

        image.LoadSVGFromString("""<svg width="99" viewBox="0 0 4 3" preserveAspectRatio="none" style="width:auto;height:6px"><rect width="4" height="3" fill="#00ff00"/></svg>""");
        Check(image.Size == new Vector2i(4, 6) && image.GetPixel(3, 5) == Colors.Lime,
            "Auto overrides the attribute and explicit aspect-ratio behavior survives rasterization.");

        var before = image.GetData();
        var beforeChanges = changes;
        foreach (var style in new[] { "width:20000px;height:2px", "width:2px;height:-1px", "width:1e-100px;height:1px" })
            Reject<InvalidDataException>(() => image.LoadSVGFromString($"<svg style=\"{style}\"/>"));
        Reject<InvalidDataException>(() => image.LoadSVGFromString("""<svg style="width:6px;height:3px"><rect"""));
        Check(image.GetData().SequenceEqual(before) && changes == beforeChanges,
            "Invalid styled SVG leaves previous pixels and the notification count intact.");

        var path = IOPath.Combine(IOPath.GetTempPath(), "electron2d-svg-" + Guid.NewGuid().ToString("N") + ".svg");
        try
        {
            File.WriteAllText(path, styled);
            using var file = Image.LoadFromFile(path);
            using var texture = ResourceLoader.Load<ImageTexture>(path, ResourceLoader.CacheMode.Ignore);
            using var pixels = texture.GetImage()!;
            Check(file.Size == new Vector2i(6, 3) && pixels.GetData().SequenceEqual(file.GetData()),
                "File and texture loading share the styled rasterizer.");
        }
        finally { File.Delete(path); }

        VerifySVGStylesheets();
        VerifySVGMath();
        VerifySVGMathWhitespace();
        VerifySVGVariables();
        VerifySVGVariableDepthAcrossAxes();
        Console.WriteLine("SVG checks passed (viewport dimensions, stylesheet cascade, CSS math, raster pixels, scaling, file and resource loading, failure rollback).");
    }

    private static void VerifySVGStylesheets()
    {
        const string cascaded = """
            <svg xmlns="http://www.w3.org/2000/svg" id="Viewport" class="tile panel" width="2" height="3"
                 viewBox="0 0 4 4" style="width:31px; height:6px !important">
                <style>
                    /* Only the matching branch contributes specificity. */
                    #Viewport.tile.panel.missing, svg#Viewport.panel:root {
                        width: 8px !important;
                    }
                    svg, svg/**/.tile#Viewport:root {
                        --ignored: "}; width:90px !important";
                        width: /* } { ; , ! [ ( " ' */ 12px !important;
                        height: 9px !important;
                    }
                    #Viewport.tile.panel.missing, svg {
                        width: 99px !important;
                    }
                </style>
                <rect width="2" height="4" fill="red"/>
                <rect x="2" width="2" height="4" fill="blue"/>
            </svg>
            """;

        using var image = new Image();
        image.LoadSVGFromString(cascaded);
        CheckCascadePixels(image, 1, "SVG string");

        image.LoadSVGFromBuffer(Encoding.UTF8.GetBytes(cascaded));
        CheckCascadePixels(image, 1, "SVG buffer");
        image.LoadSVGFromBuffer(Encoding.UTF8.GetBytes(cascaded), 2);
        CheckCascadePixels(image, 2, "Scaled SVG buffer");

        var path = IOPath.Combine(IOPath.GetTempPath(), "electron2d-svg-cascade-" + Guid.NewGuid().ToString("N") + ".svg");
        try
        {
            File.WriteAllText(path, cascaded);
            using var file = Image.LoadFromFile(path);
            CheckCascadePixels(file, 1, "SVG file");
            using var texture = ResourceLoader.Load<ImageTexture>(path, ResourceLoader.CacheMode.Ignore);
            using var pixels = texture.GetImage()!;
            Check(texture.GetSize() == new Vector2(12, 6), "ImageTexture reports the cascaded SVG dimensions.");
            CheckCascadePixels(pixels, 1, "ImageTexture file");
            Check(pixels.GetData().SequenceEqual(file.GetData()), "The image and texture file decoders return identical SVG pixels.");
        }
        finally
        {
            File.Delete(path);
        }

        using var dpi = DPITexture.CreateFromString(cascaded);
        Check(dpi.GetSize() == new Vector2(12, 6), "DPITexture preflight reports the cascaded SVG dimensions.");
        using (var pixels = dpi.GetImage()!)
        {
            CheckCascadePixels(pixels, 1, "DPITexture base");
        }
        dpi.BaseScale = 2;
        Check(dpi.GetSize() == new Vector2(24, 12), "DPITexture logical dimensions include BaseScale.");
        using (var pixels = dpi.GetImage()!)
        {
            CheckCascadePixels(pixels, 2, "DPITexture with BaseScale");
        }
        var scaleReference = CanvasRasterScale.Quantize(1);
        var oversampled = CanvasRasterScale.Quantize(2);
        CanvasRasterScale.ChangeReference(ref scaleReference, oversampled);
        try
        {
            using var pixels = dpi.ForScale(oversampled)!.GetImage()!;
            CheckCascadePixels(pixels, 4, "DPITexture with BaseScale and twofold oversampling");
        }
        finally
        {
            CanvasRasterScale.ChangeReference(ref scaleReference, CanvasRasterScale.Quantize(1));
        }

        image.LoadSVGFromString("""
            <svg xmlns="http://www.w3.org/2000/svg" id="Viewport" class="tile panel" width="2" height="3"
                 style="height:4px">
                <style>
                    .tile.panel { width: 7px; height: 5px; }
                    .tile { width: 11px; }
                    #viewport, .Tile, rect:root { width: 99px !important; }
                </style>
                <rect width="2" height="3" fill="red"/>
                <style>
                    .tile.panel { width: 9px; }
                    .panel { width: 21px; }
                </style>
            </svg>
            """);
        Check(image.Size == new Vector2i(9, 4) && image.GetPixel(1, 1) == Colors.Red &&
            image.GetPixel(8, 1).A == 0, "Normal rules honor document order and specificity; inline wins on height.");

        image.LoadSVGFromString("""
            <svg xmlns="http://www.w3.org/2000/svg" xmlns:other="urn:other" id="Viewport" class="tile" width="4" height="3">
                <style>
                    #viewport, .Tile, rect:root { width: 99px; height: 99px; }
                    body svg, :is(rect, svg, circle) { width: 98px; height: 98px; }
                    svg/**/tile { width: 94px !important; height: 94px !important; }
                    sv/**/g, .ti/**/le { width: 93px !important; height: 93px !important; }
                    svg .tile, svg/**/ .tile { width: 92px !important; height: 92px !important; }
                    @media all {
                        svg { width: 97px; height: 97px; }
                    }
                </style>
                <style media="print">svg { width: 96px; height: 96px; }</style>
                <other:style>svg { width: 95px; height: 95px; }</other:style>
                <rect width="2" height="3" fill="red"/>
            </svg>
            """);
        Check(image.Size == new Vector2i(4, 3) && image.GetPixel(1, 1) == Colors.Red &&
            image.GetPixel(3, 1).A == 0, "Nonmatching and unsupported rules leave attributes in effect.");

        var beforeSize = image.Size;
        var beforePixels = image.GetData();
        foreach (var invalidRule in new[]
        {
            "svg, { width: 12px; }",
            ", svg { width: 12px; }",
            "svg, , rect { width: 12px; }",
            "svg, /* empty branch */ { width: 12px; }",
        })
        {
            image.LoadSVGFromString($$"""
                <svg xmlns="http://www.w3.org/2000/svg" width="4" height="3">
                    <style>{{invalidRule}}</style>
                    <rect width="2" height="3" fill="red"/>
                </svg>
                """);
            Check(image.Size == beforeSize && image.GetData().SequenceEqual(beforePixels),
                $"An empty selector branch leaves the viewport and raster unchanged: {invalidRule}");
        }

        var changes = 0;
        image.Changed += _ => changes++;
        foreach (var invalid in new[] { "16foo", "0x10px", "12 px", "1/**/2px", "12/**/px", "NaN", "-2px", "20000px" })
        {
            Reject<InvalidDataException>(() => image.LoadSVGFromString(StylesheetWithWidth(invalid)));
        }
        Reject<InvalidDataException>(() => image.LoadSVGFromString("""
            <!DOCTYPE svg [<!ENTITY size "12px">]>
            <svg><style>svg { width: &size;; height: 6px; }</style></svg>
            """));
        Reject<InvalidDataException>(() => image.LoadSVGFromString("""
            <svg><style>svg { width: 12px; height: 6px; }</style><rect></svg>
            """));
        Check(image.Size == beforeSize && image.GetData().SequenceEqual(beforePixels) && changes == 0,
            "Invalid stylesheet dimensions, a DTD and malformed XML leave the previous image intact.");
        image.LoadSVGFromString(StylesheetWithWidth("12px"));
        Check(image.Size == new Vector2i(12, 6) && image.GetPixel(1, 1) == Colors.Red && changes == 1,
            "Correcting the winning dimension allows a single successful replacement.");
    }

    private static string StylesheetWithWidth(string width) => $$"""
        <svg xmlns="http://www.w3.org/2000/svg" width="4" height="3">
            <style>
                :root { width: {{width}}; height: 6px; }
            </style>
            <rect width="2" height="3" fill="red"/>
        </svg>
        """;

    private static void CheckCascadePixels(Image image, int scale, string caller)
    {
        Check(image.Size == new Vector2i(12 * scale, 6 * scale), $"{caller}: expected {12 * scale}×{6 * scale}, got {image.Size}.");
        Check(image.GetPixel(scale, 2 * scale).A == 0 &&
            image.GetPixel(4 * scale, 2 * scale) == Colors.Red &&
            image.GetPixel(7 * scale, 2 * scale) == Colors.Blue &&
            image.GetPixel(10 * scale, 2 * scale).A == 0, $"{caller}: centered red and blue content with transparent margins.");
    }

    private static void VerifySVGMath()
    {
        // The viewBox gives percentage bases of 8 and 4. The winning dimensions are
        // 16×6, so the default meet alignment centers 12×6 of paint with two-pixel side margins.
        const string cascaded = """
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 8 4" width="5" height="7"
                 style="width:calc(9px); height:Min(8px, calc(50% + 8px/2)) !important">
                <style>
                    :root {
                        width: clamp(10px, max(50% + 2px*6, min(1pc, 12px)), 20px) !important;
                        height: max(7px, 9px) !important;
                    }
                </style>
                <rect width="4" height="4" fill="red"/>
                <rect x="4" width="4" height="4" fill="blue"/>
            </svg>
            """;
        using var image = new Image();
        VerifySVGComputedViewport(image, cascaded);
        VerifySVGMathLimits(image);
    }

    private static void VerifySVGComputedViewport(Image image, string cascaded, string invalidWidth = "calc(8px / 0)")
    {
        image.LoadSVGFromString(cascaded);
        CheckMathPixels(image, 1, "Computed SVG string");
        var expectedPixels = image.GetData();

        var bytes = Encoding.UTF8.GetBytes(cascaded);
        image.LoadSVGFromBuffer(bytes);
        CheckMathPixels(image, 1, "SVG math buffer");
        Check(image.GetData().SequenceEqual(expectedPixels), "SVG math string and buffer have identical pixels.");
        image.LoadSVGFromBuffer(bytes, 2);
        CheckMathPixels(image, 2, "Scaled SVG math buffer");

        var path = IOPath.Combine(IOPath.GetTempPath(), "electron2d-svg-math-" + Guid.NewGuid().ToString("N") + ".svg");
        try
        {
            File.WriteAllText(path, cascaded);
            image.Load(path);
            CheckMathPixels(image, 1, "SVG math file");
            Check(image.GetData().SequenceEqual(expectedPixels), "SVG math string and file have identical pixels.");
            using var texture = ResourceLoader.Load<ImageTexture>(path, ResourceLoader.CacheMode.Ignore);
            using var pixels = texture.GetImage()!;
            Check(texture.GetSize() == new Vector2(16, 6), "ImageTexture reports computed SVG dimensions.");
            CheckMathPixels(pixels, 1, "SVG math ImageTexture");
            Check(pixels.GetData().SequenceEqual(expectedPixels), "SVG math ImageTexture and Image have identical pixels.");

            var changes = 0;
            image.Changed += _ => changes++;
            File.WriteAllText(path, StylesheetWithWidth(invalidWidth));
            Reject<InvalidDataException>(() => image.Load(path));
            Check(image.Size == new Vector2i(16, 6) && image.GetData().SequenceEqual(expectedPixels) && changes == 0,
                "A failed SVG math file reload keeps the previous image and emits no Changed event.");
        }
        finally
        {
            File.Delete(path);
        }

        VerifySVGMathDPI(cascaded);
    }

    private static void VerifySVGMathLimits(Image image)
    {
        image.LoadSVGFromString("""
            <svg xmlns="http://www.w3.org/2000/svg" width="calc(8192px + 8192px)" height="1">
                <rect width="16384" height="1" fill="red"/>
            </svg>
            """);
        Check(image.Size == new Vector2i(16384, 1) && image.GetPixel(16383, 0) == Colors.Red,
            "The largest supported computed axis rasterizes through its last pixel.");

        // Without a viewBox, 10% uses the default 300×150 viewport. The unit ratio
        // is a number, and the first clamp bound wins when the bounds are reversed.
        image.LoadSVGFromString("""
            <svg xmlns="http://www.w3.org/2000/svg" width="calc(10% * (2px/2px))"
                 height="clamp(10%, 20px, 12px)">
                <rect width="10" height="15" fill="red"/>
                <rect x="10" width="10" height="15" fill="blue"/>
            </svg>
            """);
        Check(image.Size == new Vector2i(30, 15) && image.GetPixel(5, 7) == Colors.Red &&
            image.GetPixel(15, 7) == Colors.Blue && image.GetPixel(25, 7).A == 0,
            "SVG attributes use the default percentage bases and retain the computed viewport coordinates.");

        var beforeSize = image.Size;
        var beforePixels = image.GetData();
        var mathChanges = 0;
        image.Changed += _ => mathChanges++;
        foreach (var invalid in new[]
        {
            "calc(6px + 2)",
            "min(4px, 2)",
            "clamp(1px, 2, 3px)",
            "calc(6px / -0)",
            "calc(1px - 1px)",
            "calc(1px - 2px)",
            "calc(6px / 2px)",
            "calc(2px * 3px)",
            "calc(1e308px * 2)",
            "max(12px, NaN)",
            "calc(0x10px)",
            "calc(12foo)",
            "calc(1/**/2px)",
            "calc(12/**/px)",
            "calc(2px+3px)",
            "calc(2px +3px)",
            "calc(2px/**/+ 3px)",
            "calc(1.px)",
            "calc(12px)junk",
            "min()",
            "max(12px,)",
            "clamp(4px, 12px)",
            "clamp(4px, 12px, 16px, 20px)",
            "calc(16384px + 1px)",
        })
        {
            var invalidBytes = Encoding.UTF8.GetBytes(StylesheetWithWidth(invalid));
            Reject<InvalidDataException>(() => image.LoadSVGFromBuffer(invalidBytes));
            Check(image.Size == beforeSize && image.GetData().SequenceEqual(beforePixels) && mathChanges == 0,
                $"Rejected SVG math keeps the previous raster and notifications: {invalid}");
        }
        Reject<InvalidDataException>(() => image.LoadSVGFromString("""<svg style="height:6px;width:calc(12px"/>"""));
        Reject<InvalidDataException>(() => image.LoadSVGFromString(StylesheetWithWidth("calc(8192px + 1px)"), 2));
        Check(image.Size == beforeSize && image.GetData().SequenceEqual(beforePixels) && mathChanges == 0,
            "Malformed inline math and computed dimensions beyond the scaled axis limit preserve the raster.");

        var nested = new string('(', 64) + "12px" + new string(')', 64);
        Reject<InvalidDataException>(() => image.LoadSVGFromString(StylesheetWithWidth($"calc({nested})")));
        Check(image.Size == beforeSize && image.GetData().SequenceEqual(beforePixels) && mathChanges == 0,
            "Excessive nesting preserves the raster and notifications.");

        image.LoadSVGFromString(StylesheetWithWidth("calc(+1.2e1px /**/ + (2px - 2px))"));
        Check(image.Size == new Vector2i(12, 6) && image.GetPixel(1, 1) == Colors.Red && mathChanges == 1,
            "Corrected SVG math accepts signed decimal exponents and parentheses, and emits Changed once.");
    }

    private static void VerifySVGVariables()
    {
        const string source = """
            <svg xmlns="http://www.w3.org/2000/svg" id="root" class="icon" viewBox="0 0 8 4"
                 width="5" height="7" style="--Width:2px; --height:2px; --inline:3px; width:var(--chain)">
                <style>
                    #root { --Width: 16px !important; --height: 3px; --order: 1px; }
                    #root { --order: 2px; }
                    .icon { --order: 5px; --inline: 4px; }
                    :root { --width: 9px; --Width: 7px !important; --chain: VAR(/**/--Width);
                            height: calc(var(--height) / 2 + var(--order) + var(--missing, min(var(--inline), 4px))); }
                    rect { --Width: 4px !important; }
                </style>
                <rect width="4" height="4" fill="red"/>
                <rect x="4" width="4" height="4" fill="blue"/>
            </svg>
            """;
        using var image = new Image();
        VerifySVGComputedViewport(image, source, "var(--missing)");
        image.LoadSVGFromString(Document("--a:1px; width:var(--missing, /**/ var(--a) /**/); height:var(--auto); --auto:auto"));
        Check(image.Size == new Vector2i(1, 150), "A nested fallback resolves and substituted auto uses the default axis.");

        image.LoadSVGFromString(Document("--a:var(--b); --b:var(--a, var(--c)); --c:var(--a); width:var(--c, 3px)"));
        Check(image.Size == new Vector2i(3, 2), "A fallback recovers when any member of a multi-edge cycle is referenced.");
        image.LoadSVGFromString(Document("--a:3px; --b:var(--a, var(--b)); width:var(--b, 4px)"));
        Check(image.Size == new Vector2i(4, 2), "An unused fallback still contributes its self-cycle.");
        image.LoadSVGFromString(Document("--empty:; --a:3px; width:var(--a)var(--empty)var(--missing,)"));
        Check(image.Size == new Vector2i(3, 2), "Empty variables and empty fallbacks substitute no tokens.");

        var before = image.GetData();
        var changes = 0;
        image.Changed += _ => changes++;
        using var dpi = DPITexture.CreateFromString(source);
        var dpiChanges = 0;
        dpi.Changed += _ => dpiChanges++;

        var chain = string.Concat(Enumerable.Range(0, 66).Select(index => $"--v{index}:var(--v{index + 1});"));
        var expansion = "--part:" + new string('1', 9000) + "; width:var(--part)var(--part)";
        var repeated = "--part:; width:" + string.Concat(Enumerable.Repeat("var(--part)", 500)) + "3px";
        foreach (var declarations in new[]
        {
            "width:3px; width:var(--missing)",
            "--a:var(--a, 3px); width:var(--a)",
            "--a:12oops; width:var(--a, 3px)",
            "--a:NaN; width:var(--a)",
            "width:var(--bad name, 3px)",
            "width:var(width, 3px)",
            "--a:12; width:var(--a)px",
            "--a:2px; width:calc(var(--a)+ 1px)",
            "--a:px; width:calc(3var(--a) + 1px)",
            "width:var(--a)",
            chain + "width:var(--v0, 3px)",
            expansion,
            repeated,
        })
        {
            var invalid = Document(declarations);
            Reject<InvalidDataException>(() => image.LoadSVGFromString(invalid));
            Reject<InvalidDataException>(() => image.LoadSVGFromBuffer(Encoding.UTF8.GetBytes(invalid)));
            Reject<InvalidDataException>(() => dpi.SetSource(invalid));
            Check(image.Size == new Vector2i(3, 2) && image.GetData().SequenceEqual(before) && changes == 0,
                $"Invalid SVG variables preserve the image and emit no Changed event ({declarations.Length} chars).");
            Check(dpi.GetSource() == source && dpi.GetSize() == new Vector2(16, 6) && dpiChanges == 0,
                "Invalid SVG variables preserve DPITexture source, size and Changed count.");
        }

        var corrected = Document("--a:3px; width:var(--a)");
        image.LoadSVGFromString(corrected);
        dpi.SetSource(corrected);
        Check(changes == 1 && dpiChanges == 1 && dpi.GetSize() == new Vector2(3, 2),
            "A corrected SVG replaces the previous image and DPITexture source once.");

        static string Document(string declarations) => $"""
            <svg xmlns="http://www.w3.org/2000/svg" width="9" height="2" style="{declarations}">
                <rect width="1" height="2" fill="red"/>
            </svg>
            """;
    }

    private static void VerifySVGVariableDepthAcrossAxes()
    {
        var chain = string.Concat(Enumerable.Range(0, 63).Select(index => $"--v{index}:var(--v{index + 1});"));
        using var image = new Image();
        image.LoadSVGFromString(Document("--v63:3px; width:var(--v0); height:var(--v0)"));
        Check(image.Size == new Vector2i(3, 3) && image.GetPixel(0, 0) == Colors.Red,
            "Both axes can share a 64-property chain.");
        var pixels = image.GetData();
        var changes = 0;
        image.Changed += _ => changes++;

        const string extended = "--v63:3px; --extra:var(--v0);";
        foreach (var dimensions in new[]
        {
            "width:var(--v0); height:var(--extra, 3px)",
            "width:var(--extra, 3px); height:var(--v0)",
            "width:calc(var(--v0) + var(--extra, 3px)); height:3px",
        })
        {
            var invalid = Document(extended + dimensions);
            Reject<InvalidDataException>(() => image.LoadSVGFromString(invalid));
            Reject<InvalidDataException>(() => image.LoadSVGFromBuffer(Encoding.UTF8.GetBytes(invalid)));
            Check(image.Size == new Vector2i(3, 3) && image.GetData().SequenceEqual(pixels) && changes == 0,
                "A 65-property chain is rejected even after a shorter lookup populated the cache.");
        }

        image.LoadSVGFromString(Document("--v63:4px; --extra:var(--v1); width:var(--v0); height:var(--extra)"));
        Check(image.Size == new Vector2i(4, 4) && image.GetPixel(0, 0) == Colors.Red && changes == 1,
            "Shortening the extra chain to 64 properties publishes the corrected image once.");

        const string unusedFallback = "--v63:4px; --size:4px; --extra:var(--size, var(--v0));";
        const string cyclicFallback = "--v63:4px; --size:4px; --extra:var(--size, var(--b));" +
            "--a:var(--b, var(--v2)); --b:var(--a);";
        pixels = image.GetData();
        foreach (var declarations in new[]
        {
            unusedFallback + "width:var(--v0); height:var(--extra)",
            unusedFallback + "width:var(--extra); height:var(--v0)",
            unusedFallback + "width:calc(var(--v0) + var(--extra)); height:4px",
            cyclicFallback + "width:var(--a, 4px); height:var(--extra)",
            cyclicFallback + "width:var(--extra); height:var(--a, 4px)",
        })
        {
            var invalid = Document(declarations);
            Reject<InvalidDataException>(() => image.LoadSVGFromString(invalid));
            Reject<InvalidDataException>(() => image.LoadSVGFromBuffer(Encoding.UTF8.GetBytes(invalid)));
            Check(image.Size == new Vector2i(4, 4) && image.GetData().SequenceEqual(pixels) && changes == 1,
                "An over-depth dependency in an unused fallback preserves the raster regardless of lookup order.");
        }

        image.LoadSVGFromString(Document(
            "--v63:4px; --size:5px; --extra:var(--size, var(--v1)); width:var(--v0); height:var(--extra)"));
        Check(image.Size == new Vector2i(4, 5) && image.GetPixel(0, 0) == Colors.Red && changes == 2,
            "A fallback dependency within the limit allows the main value and emits Changed once.");

        image.LoadSVGFromString(Document(
            cyclicFallback.Replace("var(--v2)", "var(--v3)", StringComparison.Ordinal) +
            "width:var(--extra); height:var(--a, 5px)"));
        Check(image.Size == new Vector2i(4, 5) && changes == 3,
            "A cycle within the depth limit still allows its caller to select a fallback.");

        string Document(string declarations) => $"""
            <svg xmlns="http://www.w3.org/2000/svg" style="{chain}{declarations}">
                <rect width="1" height="1" fill="red"/>
            </svg>
            """;
    }

    private static void VerifySVGMathWhitespace()
    {
        foreach (var inline in new[] { false, true })
        {
            var location = inline ? "inline style" : "attribute";
            using var image = new Image();
            image.LoadSVGFromString(SVG);
            var beforePixels = image.GetData();
            var changes = 0;
            image.Changed += _ => changes++;

            foreach (var (position, expression) in new[]
            {
                ("before", "\u00A0calc(8px + 4px)"),
                ("after", "calc(8px + 4px)\u00A0"),
                ("inside", "calc(8px\u00A0 + 4px)"),
            })
            {
                Reject<InvalidDataException>(() => image.LoadSVGFromString(Document(expression)));
                Check(image.Size == new Vector2i(3, 2) && image.GetData().SequenceEqual(beforePixels) && changes == 0,
                    $"A non-CSS space {position} calc in an SVG {location} leaves the raster and notifications unchanged.");
            }

            // XML character references preserve the actual CSS whitespace characters in each attribute value.
            image.LoadSVGFromString(Document(" &#x9;calc(8px + 4px)&#xD;&#xA; "));
            Check(image.Size == new Vector2i(12, 6) && image.GetPixel(1, 1) == Colors.Red &&
                image.GetPixel(11, 5).A == 0 && changes == 1,
                $"CSS whitespace at the boundaries of calc in an SVG {location} allows one successful replacement.");

            string Document(string expression)
            {
                var dimensions = inline
                    ? $"style=\"height:6px; width:{expression}\""
                    : $"height=\"6\" width=\"{expression}\"";
                return $$"""
                    <svg xmlns="http://www.w3.org/2000/svg" {{dimensions}}>
                        <rect width="4" height="6" fill="red"/>
                    </svg>
                    """;
            }
        }
    }

    private static void VerifySVGMathDPI(string source)
    {
        using var dpi = DPITexture.CreateFromString(source);
        Check(dpi.GetSize() == new Vector2(16, 6), "DPITexture preflight uses the computed viewport.");
        using (var pixels = dpi.GetImage()!)
        {
            CheckMathPixels(pixels, 1, "SVG math DPITexture base");
        }

        dpi.BaseScale = 2;
        Check(dpi.GetSize() == new Vector2(32, 12), "DPITexture applies BaseScale to the computed viewport.");
        using (var pixels = dpi.GetImage()!)
        {
            CheckMathPixels(pixels, 2, "SVG math DPITexture BaseScale");
        }

        var scaleReference = CanvasRasterScale.Quantize(1);
        var oversampled = CanvasRasterScale.Quantize(2);
        CanvasRasterScale.ChangeReference(ref scaleReference, oversampled);
        try
        {
            var variant = dpi.ForScale(oversampled)!;
            using var pixels = variant.GetImage()!;
            CheckMathPixels(pixels, 4, "SVG math DPITexture oversampled variant");

            var changes = 0;
            dpi.Changed += _ => changes++;
            Reject<InvalidDataException>(() => dpi.SetSource(StylesheetWithWidth("calc(6px + 2)")));
            Reject<InvalidDataException>(() => dpi.SetSource(StylesheetWithWidth("calc(8192px + 1px)")));
            Check(dpi.GetSource() == source && dpi.GetSize() == new Vector2(32, 12) && changes == 0 &&
                ReferenceEquals(dpi.ForScale(oversampled), variant),
                "A failed DPITexture source update keeps the source, logical size, cached variant and notifications.");
            using var retained = variant.GetImage()!;
            Check(retained.GetData().SequenceEqual(pixels.GetData()), "The retained DPI raster pixels survive a failed update.");
        }
        finally
        {
            CanvasRasterScale.ChangeReference(ref scaleReference, CanvasRasterScale.Quantize(1));
        }
    }

    private static void CheckMathPixels(Image image, int scale, string caller)
    {
        var expectedSize = new Vector2i(16 * scale, 6 * scale);
        Check(image.Size == expectedSize, $"{caller}: expected {expectedSize}, got {image.Size}.");
        Check(image.GetPixel(scale, 2 * scale).A == 0 &&
            image.GetPixel(4 * scale, 2 * scale) == Colors.Red &&
            image.GetPixel(10 * scale, 2 * scale) == Colors.Blue &&
            image.GetPixel(15 * scale, 2 * scale).A == 0, $"{caller}: expected centered red and blue content with transparent margins.");
    }

    internal static byte[] VerifyJPEG()
    {
        // cjpeg -optimize -quality 98, with -progressive -restart 1 for the RGB fixture.
        var gray = Convert.FromBase64String("/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAEBAQEBAQEBAQEBAQEBAQIBAQEBAQIBAQECAgICAgICAgIDAwQDAwMDAwICAwQDAwQEBAQEAgMFBQQEBQQEBAT/wAALCAAIAAkBAREA/8QAFQABAQAAAAAAAAAAAAAAAAAACAr/xAAYEAACAwAAAAAAAAAAAAAAAAAACUaDwv/aAAgBAQAAPwAxpcidGSzw/9k=");
        var progressive = Convert.FromBase64String("/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAEBAQEBAQEBAQEBAQEBAQIBAQEBAQIBAQECAgICAgICAgIDAwQDAwMDAwICAwQDAwQEBAQEAgMFBQQEBQQEBAT/2wBDAQEBAQEBAQIBAQIEAwIDBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAT/wgARCAAIAAkDASIAAhEBAxEB/8QAFgABAQEAAAAAAAAAAAAAAAAAAAcI/8QAFQEBAQAAAAAAAAAAAAAAAAAABAj/3QAEAAH/2gAMAwEAAhADEAAAAYNoInQ3/8QAFxABAAMAAAAAAAAAAAAAAAAAAAYHF//dAAQAAv/aAAgBAQABBQKP1Yy1/8QAGxEAAQQDAAAAAAAAAAAAAAAAUQADBgcWJHH/3QAEAAH/2gAIAQMBAT8Boe3pzhTe0COr/8QAGxEAAQQDAAAAAAAAAAAAAAAABgABBxUksdH/2gAIAQIBAT8BKJLLrJ8jfV//xAAYEAACAwAAAAAAAAAAAAAAAAAAogIkNP/dAAQAAv/aAAgBAQAGPwKFdTOp/8QAFRABAQAAAAAAAAAAAAAAAAAAEKH/2gAIAQEAAT8hln//3QAEAAH/2gAMAwEAAgADAAAAEP8A/8QAFREBAQAAAAAAAAAAAAAAAAAAAMH/2gAIAQMBAT8Qnkf/xAAXEQADAQAAAAAAAAAAAAAAAAAAQVGB/9oACAECAQE/ENiVD//EAB0QAAECBwAAAAAAAAAAAAAAAAABETFBUWFxsfD/3QAEAAL/2gAIAQEAAT8Q1JJoWH0cwf/Z");
        using var image = new Image();
        var changes = 0;
        image.Changed += _ => changes++;
        image.LoadJPGFromBuffer(gray);
        var grayscale = image.GetData();
        Check(image.PixelFormat == Image.Format.L8 && image.Size == new Vector2i(9, 8) &&
            grayscale.Select((sample, index) => Math.Abs(sample - (28 + index % 9 * 23))).Max() <= 2, "Grayscale JPEG retains one byte per pixel, including odd-width rows.");
        image.LoadJPGFromBuffer(progressive);
        var first = image.GetPixel(1, 1);
        var last = image.GetPixel(7, 6);
        Check(image.PixelFormat == Image.Format.Rgb8 && image.Size == new Vector2i(9, 8) && first.A == 1 &&
            last.R - first.R > 0.4f && last.G - first.G > 0.4f && Math.Abs(last.B - 80 / 255f) < 0.12f, "Progressive JPEG reconstructs RGB without alpha storage.");
        image.GenerateMIPMaps();
        var before = image.GetData();
        var changeCount = changes;
        var frame = gray.AsSpan().IndexOf(new byte[] { 0xff, 0xc0 });
        var highPrecision = (byte[])gray.Clone();
        highPrecision[frame + 4] = 12;
        var excessive = (byte[])gray.Clone();
        BinaryPrimitives.WriteUInt32BigEndian(excessive.AsSpan(frame + 5), 0x30003000);
        Reject<NotSupportedException>(() => image.LoadJPGFromBuffer(highPrecision));
        Reject<InvalidDataException>(() => image.LoadJPGFromBuffer(excessive));
        Reject<InvalidDataException>(() => image.LoadJPGFromBuffer(progressive[..^2]));
        Check(image.HasMIPMaps && image.GetData().SequenceEqual(before) && changes == changeCount, "JPEG preflight preserves a populated image on refusal.");

        using var source = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8);
        source.Fill(new Color(0.25f, 0.5f, 0.75f, 0.25f));
        source.GenerateMIPMaps();
        var saved = source.GetData();
        foreach (var quality in new[] { 0.01f, 0.75f, 1f })
        {
            image.LoadJPGFromBuffer(source.SaveJPGToBuffer(quality));
            var pixel = image.GetPixel(5, 5);
            Check(image.Size == source.Size && image.PixelFormat == Image.Format.Rgb8 && !image.HasMIPMaps &&
                pixel.A == 1 && Math.Abs(pixel.R - 0.25f) < 0.12f && Math.Abs(pixel.G - 0.5f) < 0.12f && Math.Abs(pixel.B - 0.75f) < 0.12f,
                "JPEG accepts its quality range, encodes the base level and discards alpha.");
        }
        Check(source.HasMIPMaps && source.GetData().SequenceEqual(saved), "JPEG encoding preserves the source mip chain.");
        Check(source.SaveJPGToBuffer(0.759f).SequenceEqual(source.SaveJPGToBuffer(0.75f)),
            "JPEG passes the same truncated quality percentage for both inputs.");
        foreach (var quality in new[] { float.NaN, float.PositiveInfinity, 0, -1, 1.01f })
            Reject<ArgumentOutOfRangeException>(() => source.SaveJPGToBuffer(quality));
        using var empty = new Image();
        Reject<InvalidOperationException>(() => empty.SaveJPGToBuffer());
        using var block = Image.CreateFromData(4, 4, false, Image.Format.Dxt1, [0, 248, 0, 0, 0, 0, 0, 0]);
        image.LoadJPGFromBuffer(block.SaveJPGToBuffer(1));
        Check(block.IsCompressed && image.GetPixel(3, 3).R > 0.95f, "JPEG accepts a supported block source without changing it.");

        var folder = Directory.CreateTempSubdirectory("Electron2D-jpeg-");
        try
        {
            var path = IOPath.Combine(folder.FullName, "gray.jpeg");
            File.WriteAllBytes(path, gray);
            using var decoded = Image.LoadFromFile(path);
            using var texture = ResourceLoader.Load<ImageTexture>(path, ResourceLoader.CacheMode.Ignore);
            using var snapshot = texture.GetImage()!;
            Check(decoded.PixelFormat == Image.Format.L8 && snapshot.PixelFormat == Image.Format.L8 &&
                snapshot.GetData().SequenceEqual(grayscale), "JPEG file and resource loaders retain the grayscale layout.");
            block.SaveJPG(path, 1);
            using var encoded = Image.LoadFromFile(path);
            Check(encoded.PixelFormat == Image.Format.Rgb8 && encoded.Size == new Vector2i(4, 4), "JPEG file save shares block encoding.");
        }
        finally { folder.Delete(recursive: true); }
        Console.WriteLine("JPEG grayscale, progressive color, block export, file/resource and failure checks passed.");
        return source.SaveJPGToBuffer(1);
    }
    internal static void VerifyPNG()
    {
        var nativeBefore = NativeAllocation.Snapshot().Current;
        using var image = new Image();
        foreach (var (encoded, width, height, format, expected) in new (string, int, int, Image.Format, byte[])[]
        {
            ("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAAAAAA6fptVAAAACklEQVR4nGP4DwABAQEAsTj2FAAAAABJRU5ErkJggg==", 1, 1, Image.Format.L8, [255]),
            ("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR4nGP43wAAAoEBgFnpL/wAAAAASUVORK5CYII=", 1, 1, Image.Format.La8, [255, 128]),
            ("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABEAIAAADA54+dAAAAD0lEQVR4nGMQMgmrmLUHAAYnAmsO3tV6AAAAAElFTkSuQmCC", 1, 1, Image.Format.Rgb8, [18, 86, 154]),
            ("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABEAYAAABPhRjKAAAAEUlEQVR4nGMQMgmrmLXn3gcADakEOVMAyboAAAAASUVORK5CYII=", 1, 1, Image.Format.Rgba8, [18, 86, 154, 222]),
            // 16-bit alpha stays linear while gAMA converts color to display samples.
            ("iVBORw0KGgoAAAANSUhEUgAAAAIAAAABEAQAAAAOu2tCAAAABGdBTUEAAYagMeiWXwAAAA9JREFUeJxjcGBoAMIDDAAIiQIBLVBEoAAAAABJRU5ErkJggg==", 2, 1, Image.Format.La8, [136, 128, 186, 191]),
            // Both colors quantize to 18. Only the exact source key is transparent.
            ("iVBORw0KGgoAAAANSUhEUgAAAAIAAAABEAAAAACB2fwVAAAAAnRSTlMSNC/TSV4AAAANSURBVHicYxAyEQoFAAFiAK6jp76HAAAAAElFTkSuQmCC", 2, 1, Image.Format.La8, [18, 0, 18, 255]),
            ("iVBORw0KGgoAAAANSUhEUgAAAAMAAAABAgMAAABmjvwnAAAACVBMVEX/AAAA/wAAAP8tSs2KAAAAAnRSTlMAgJsrThgAAAAKSURBVHicY5AAAAAaABktiPQ2AAAAAElFTkSuQmCC", 3, 1, Image.Format.Rgba8, [255, 0, 0, 0, 0, 255, 0, 128, 0, 0, 255, 255]),
            ("iVBORw0KGgoAAAANSUhEUgAAAAIAAAACEAAAAAFwSr4tAAAAE0lEQVR4nGMQMmEIq2CYtefeBwAPBQQ5DUPaigAAAABJRU5ErkJggg==", 2, 2, Image.Format.L8, [18, 86, 154, 222]),
        })
        {
            var data = Convert.FromBase64String(encoded);
            image.LoadPNGFromBuffer(data);
            Check(image.Size == new Vector2i(width, height) && image.PixelFormat == format && image.GetData().SequenceEqual(expected), "PNG channel, transfer and transparency contract.");
            image.GenerateMIPMaps();
            var before = image.GetData();
            var png = image.SavePNGToBuffer();
            var colorType = format switch { Image.Format.L8 => 0, Image.Format.La8 => 4, Image.Format.Rgb8 => 2, _ => 6 };
            Check(png[24] == 8 && png[25] == colorType && image.GetData().SequenceEqual(before), "PNG IHDR matches the source layout and encoding preserves the source.");
            image.LoadPNGFromBuffer(png);
            Check(!image.HasMIPMaps && image.PixelFormat == format && image.GetData().SequenceEqual(expected), "PNG saves the base level with exact channel bytes.");
            var invalidCRC = (byte[])data.Clone();
            invalidCRC[^1] ^= 1;
            Reject<InvalidDataException>(() => image.LoadPNGFromBuffer(invalidCRC));
            var idat = data.AsSpan().IndexOf("IDAT"u8);
            var payloadLength = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(idat - 4));
            data[idat + 4 + payloadLength] ^= 1;
            Reject<InvalidDataException>(() => image.LoadPNGFromBuffer(data));
            Check(image.GetData().SequenceEqual(expected), "A late PNG failure preserves the published pixels.");
        }

        using var block = Image.CreateFromData(4, 4, false, Image.Format.Dxt1, [0, 248, 0, 0, 0, 0, 0, 0]);
        image.LoadPNGFromBuffer(block.SavePNGToBuffer());
        Check(block.IsCompressed && image.GetPixel(3, 3) == Colors.Red, "PNG export decompresses blocks in a private snapshot.");
        foreach (var alpha in new[] { 1f, 0.5f })
        {
            using var floating = Image.CreateEmpty(1, 1, false, Image.Format.Rgbaf);
            floating.SetPixel(0, 0, new Color(0, 1, 0, alpha));
            image.LoadPNGFromBuffer(floating.SavePNGToBuffer());
            Check(image.PixelFormat == (alpha == 1f ? Image.Format.Rgb8 : Image.Format.Rgba8) &&
                image.GetPixel(0, 0).G == 1, "PNG converts other formats according to used alpha.");
        }

        // A cached texture must retain the source channel layout as well as its pixel values.
        var folder = Directory.CreateTempSubdirectory("Electron2D-png-");
        try
        {
            var path = IOPath.Combine(folder.FullName, "gray.png");
            using var source = Image.CreateFromData(2, 1, false, Image.Format.La8, [37, 0, 144, 255]);
            source.SavePNG(path);
            using var decoded = Image.LoadFromFile(path);
            using var texture = ResourceLoader.Load<ImageTexture>(path, ResourceLoader.CacheMode.Ignore);
            using var snapshot = texture.GetImage()!;
            Check(decoded.PixelFormat == Image.Format.La8 && snapshot.PixelFormat == Image.Format.La8 &&
                snapshot.GetData().SequenceEqual(source.GetData()), "PNG file and resource texture paths retain LA8 samples.");
        }
        finally { folder.Delete(recursive: true); }
        Check(NativeAllocation.Snapshot().Current == nativeBefore, "PNG temporaries drain after successful and failed operations.");
        Console.WriteLine("PNG channel, transfer, file/resource, failure and ownership checks passed.");
    }

    private static void VerifyAnimatedWebP()
    {
        var bytes = Convert.FromBase64String(AnimatedWEBP);
        using var image = new Image();
        var changes = 0;
        image.Changed += _ => changes++;
        image.LoadWebPFromBuffer(bytes);
        Check(image.Size == new Vector2i(4, 4) && !image.HasMIPMaps && changes == 1, "Animated WebP publishes one complete base canvas.");
        for (var y = 0; y < 4; y++)
            for (var x = 0; x < 4; x++)
                Check(image.GetPixel(x, y) == (x >= 2 && y >= 2 ? Colors.Red : new Color(0, 0, 0, 0)), "The first WebP frame retains its canvas offset and transparent background.");
        var before = image.GetData();
        var malformed = (byte[])bytes.Clone();
        malformed[malformed.AsSpan().IndexOf("VP8L"u8) + 8] = 0;
        foreach (var invalid in new[] { bytes[..^1], malformed })
            Reject<InvalidDataException>(() => image.LoadWebPFromBuffer(invalid));
        Check(changes == 1 && image.GetData().AsSpan().SequenceEqual(before), "Animation parse/decode failures preserve pixels and events.");
    }

    private static unsafe void VerifyNativeIOCounts()
    {
        byte[] bytes = [11, 22, 33];
        fixed (byte* data = bytes)
        {
            var stream = SDL3.SDL.IOFromConstMem((nint)data, (nuint)bytes.Length);
            if (stream == 0) throw new InvalidOperationException(SDL3.SDL.GetError());
            try
            {
                var output = stackalloc byte[4];
                Check(SDL3.SDL.ReadIO(stream, (nint)output, 4) == 3 && new ReadOnlySpan<byte>(output, 3).SequenceEqual(bytes), "Native size_t read counts and copied bytes are exact.");
                Check(SDL3.SDL.ReadIO(stream, (nint)output, 4) == 0, "Native size_t EOF count is zero.");
            }
            finally { SDL3.SDL.CloseIO(stream); }
            stream = SDL3.SDL.IOFromDynamicMem();
            if (stream == 0) throw new InvalidOperationException(SDL3.SDL.GetError());
            try
            {
                Check(SDL3.SDL.WriteIO(stream, (nint)data, (nuint)bytes.Length) == 3 && SDL3.SDL.GetIOSize(stream) == 3, "Native size_t write counts and output length are exact.");
            }
            finally { SDL3.SDL.CloseIO(stream); }
        }
    }

    private static void VerifyFiles(Image source, byte[] png, byte[] jpg)
    {
        var settings = ProjectSettings.Service;
        var roots = (ProjectSettings.ProjectRoot, ProjectSettings.UserDataRoot);
        var root = IOPath.Combine(IOPath.GetTempPath(), "Electron2D-codecs-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(IOPath.Combine(root, "user"));
        try
        {
            ProjectSettings.ConfigurePaths(root, IOPath.Combine(root, "user"));
            source.SavePNG("res://texture.PNG");
            source.SavePNG("user://texture.png");
            source.SaveJPG("user://texture.jpeg");
            foreach (var path in new[] { "res://texture.PNG", "user://texture.png", IOPath.Combine(root, "texture.PNG") })
            {
                using var decoded = Image.LoadFromFile(path);
                CheckPixels(decoded, Pixels);
                using var texture = ImageTexture.CreateFromImage(decoded);
                decoded.Fill(Colors.Black);
                using var snapshot = texture.GetImage();
                CheckPixels(snapshot!, Pixels);
            }
            var inputs = new[] { (".png", png), (".jpg", jpg), (".webp", Convert.FromBase64String(WEBP)), (".bmp", BMP(false)), (".tga", TGA(false)), (".svg", Encoding.UTF8.GetBytes(SVG)) };
            foreach (var (extension, bytes) in inputs)
            {
                var path = IOPath.Combine(root, "input" + extension);
                File.WriteAllBytes(path, bytes);
                using var decoded = Image.LoadFromFile(path);
                Check(decoded.Width > 0 && decoded.Height > 0, "Every integrated file extension reaches its decoder.");
            }
            var destination = IOPath.Combine(root, "texture.PNG");
            var old = File.ReadAllBytes(destination);
            using var empty = new Image();
            Reject<InvalidOperationException>(() => empty.SavePNG(destination));
            Reject<ArgumentOutOfRangeException>(() => source.SaveJPG(destination, float.NaN));
            Check(File.ReadAllBytes(destination).SequenceEqual(old), "Failed encoding preserves an existing destination.");
            Reject<IOException>(() => source.SavePNG(root));
            Reject<DirectoryNotFoundException>(() => source.SavePNG(IOPath.Combine(root, "absent", "out.png")));
            Reject<FileNotFoundException>(() => Image.LoadFromFile("res://missing.png"));
            Reject<NotSupportedException>(() => Image.LoadFromFile("res://texture.unknown"));
            Reject<UnauthorizedAccessException>(() => source.SavePNG("res://../escape.png"));
            Reject<UnauthorizedAccessException>(() => Image.LoadFromFile("res://../escape.png"));
            var large = IOPath.Combine(root, "large.png");
            using (var file = File.Create(large)) file.SetLength(64 * 1024 * 1024 + 1);
            Reject<InvalidDataException>(() => Image.LoadFromFile(large));
            Check(!Directory.EnumerateFiles(root).Any(path => path.EndsWith(".tmp", StringComparison.Ordinal)), "Failed atomic writes clean their temporary files.");
        }
        finally { ProjectSettings.ConfigurePaths(roots.ProjectRoot, roots.UserDataRoot); Directory.Delete(root, true); }
    }

    private static byte[] BMP(bool topDown)
    {
        var bytes = new byte[54 + 24];
        bytes[0] = (byte)'B'; bytes[1] = (byte)'M';
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(2), bytes.Length);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(10), 54);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(14), 40);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(18), 3);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(22), topDown ? -2 : 2);
        bytes[26] = 1; bytes[28] = 24;
        for (var y = 0; y < 2; y++)
            for (var x = 0; x < 3; x++)
                for (var channel = 0; channel < 3; channel++)
                    bytes[54 + y * 12 + x * 3 + channel] = Pixels[((topDown ? y : 1 - y) * 3 + x) * 4 + 2 - channel];
        return bytes;
    }

    internal static void VerifyBMP()
    {
        static void CheckBMPPixels(Image decoded, int width, int height, byte[] expected) =>
            Check(decoded.Size == new Vector2i(width, height) && decoded.PixelFormat == Image.Format.Rgba8 &&
                decoded.GetData().AsSpan().SequenceEqual(expected), $"BMP {width}x{height} dimensions and RGBA pixels are exact.");

        using var image = new Image();
        byte[] red = [255, 0, 0, 255], green = [0, 255, 0, 255], blue = [0, 0, 255, 255];
        byte[] black = [0, 0, 0, 255];
        byte[] palette = [0, 0, 0, 77, 0, 0, 255, 44, 0, 255, 0, 0, 255, 0, 0, 9];
        var indexed = BMPVector(5, -1, 2, [0x1b, 0x40, 0xaa, 0xbb], palette);
        foreach (var topDown in new[] { false, true })
        {
            image.LoadBMPFromBuffer(BMP(topDown));
            var opaque = (byte[])Pixels.Clone();
            opaque[3] = 255;
            CheckBMPPixels(image, 3, 2, opaque);
        }
        image.LoadBMPFromBuffer(indexed);
        CheckBMPPixels(image, 5, 1, [.. black, .. red, .. green, .. blue, .. red]);
        image.LoadBMPFromBuffer(BMPVector(3, 1, 1, [0xa0, 0, 0, 0], [0, 0, 255, 0, 255, 0], header: 12));
        CheckBMPPixels(image, 3, 1, [.. green, .. red, .. green]);
        image.LoadBMPFromBuffer(BMPVector(3, 1, 4, [0x12, 0x30, 0, 0], palette));
        CheckBMPPixels(image, 3, 1, [.. red, .. green, .. blue]);
        image.LoadBMPFromBuffer(BMPVector(3, 1, 8, [3, 0, 2, 0], palette));
        CheckBMPPixels(image, 3, 1, [.. blue, .. black, .. green]);
        var legacy = BMPVector(1, 1, 15, [0x0c, 0x7c, 0, 0]);
        BinaryPrimitives.WriteInt32LittleEndian(legacy.AsSpan(2), 0);
        image.LoadBMPFromBuffer(legacy);
        CheckBMPPixels(image, 1, 1, [255, 0, 99, 255]);
        image.LoadBMPFromBuffer(BMPVector(1, 1, 16, [0x0c, 0x7c, 0, 0]));
        CheckBMPPixels(image, 1, 1, [255, 0, 99, 255]);
        image.LoadBMPFromBuffer(BMPVector(1, 1, 16, [0xe0, 0x07, 0, 0], compression: 3, masks: [0xf800, 0x07e0, 0x001f]));
        CheckBMPPixels(image, 1, 1, green);
        image.LoadBMPFromBuffer(BMPVector(1, -1, 16, [0x00, 0x7c, 0, 0], compression: 6, masks: [0x7c00, 0x03e0, 0x001f, 0x8000]));
        CheckBMPPixels(image, 1, 1, [255, 0, 0, 0]);
        image.LoadBMPFromBuffer(BMPVector(1, 1, 32, [0xff, 0x83, 0x0f, 0x80], compression: 3, header: 56,
            masks: [0x000003ff, 0x000ffc00, 0x3ff00000, 0xc0000000]));
        CheckBMPPixels(image, 1, 1, [255, 247, 0, 170]);
        image.LoadBMPFromBuffer(BMPVector(2, 1, 32, [0, 0, 255, 0, 255, 0, 0, 0]));
        CheckBMPPixels(image, 2, 1, [.. red, .. blue]);
        image.LoadBMPFromBuffer(BMPVector(2, 1, 32, [0, 0, 255, 0, 255, 0, 0, 127]));
        CheckBMPPixels(image, 2, 1, [255, 0, 0, 0, 0, 0, 255, 127]);

        var rle4 = BMPVector(5, 2, 4, [5, 0x12, 0, 0, 0, 5, 0x32, 0x10, 0x30, 0xaa, 0, 1], palette, compression: 2);
        byte[] rle4Expected = [.. blue, .. green, .. red, .. black, .. blue, .. red, .. green, .. red, .. green, .. red];
        image.LoadBMPFromBuffer(rle4);
        CheckBMPPixels(image, 5, 2, rle4Expected);
        var rle8 = BMPVector(4, 2, 8, [0, 2, 1, 0, 3, 3, 0, 0, 0, 3, 1, 2, 3, 0xaa, 0, 1], palette, compression: 1);
        image.LoadBMPFromBuffer(rle8);
        CheckBMPPixels(image, 4, 2, [.. red, .. green, .. blue, .. black, .. black, .. blue, .. blue, .. blue]);

        image.GenerateMIPMaps();
        var before = image.GetData();
        var changes = 0;
        image.Changed += _ => changes++;
        foreach (var invalid in new[]
        {
            indexed[..^1], rle4[..^2],
            BMPVector(1, 1, 4, [0x40, 0, 0, 0], palette),
            BMPVector(1, 1, 8, [0, 1], compression: 1),
            BMPVector(3, 1, 8, [4, 2, 0, 1], palette, compression: 1),
            BMPVector(3, 1, 8, [0, 2, 0, 1, 0, 1], palette, compression: 1),
            BMPVector(3, -1, 8, [3, 2, 0, 1], palette, compression: 1),
            BMPVector(3, 1, 4, [3, 0x41, 0, 1], palette, compression: 2),
            BMPVector(1, 1, 16, [0, 0, 0, 0], compression: 3, masks: [0x001f, 0x03e0, 0x001f]),
            BMPVector(1, 1, 16, [0, 0, 0, 0], compression: 3, masks: [0x7c00, 0x03e0, 0x0015]),
            BMPVector(1, 1, 16, [0, 0, 0, 0], compression: 3, masks: [0x10000, 0x03e0, 0x001f]),
            BMPVector(1, 1, 16, [0, 0, 0, 0], compression: 3, masks: [0x7c00, 0x03e0]),
            BMPVector(1, int.MinValue, 32, [0, 0, 0, 0]),
            BMPVector(8192, 8192, 32, [0, 0, 0, 0]),
        })
            Reject<InvalidDataException>(() => image.LoadBMPFromBuffer(invalid));
        var boundedRLE = (byte[])rle4.Clone();
        BinaryPrimitives.WriteInt32LittleEndian(boundedRLE.AsSpan(34), 2);
        Reject<InvalidDataException>(() => image.LoadBMPFromBuffer(boundedRLE));
        var paletteOverlap = (byte[])indexed.Clone();
        BinaryPrimitives.WriteInt32LittleEndian(paletteOverlap.AsSpan(10), 54);
        Reject<InvalidDataException>(() => image.LoadBMPFromBuffer(paletteOverlap));
        var shortRows = BMPVector(2, 1, 24, [0, 0, 0, 0, 0, 0]);
        Reject<InvalidDataException>(() => image.LoadBMPFromBuffer(shortRows));
        Check(image.HasMIPMaps && image.GetData().AsSpan().SequenceEqual(before) && changes == 0,
            "Malformed BMP preserves pixels, mipmaps and event count.");

        var folder = Directory.CreateTempSubdirectory("Electron2D-bmp-");
        try
        {
            var path = IOPath.Combine(folder.FullName, "indexed.BMP");
            File.WriteAllBytes(path, rle4);
            image.Load(path);
            CheckBMPPixels(image, 5, 2, rle4Expected);
            Check(changes == 1 && !image.HasMIPMaps, "BMP file load publishes one base level.");
            using var loaded = Image.LoadFromFile(path);
            CheckBMPPixels(loaded, 5, 2, rle4Expected);
            using var texture = ResourceLoader.Load<ImageTexture>(path, ResourceLoader.CacheMode.Ignore);
            using var snapshot = texture.GetImage()!;
            CheckBMPPixels(snapshot, 5, 2, rle4Expected);
        }
        finally { folder.Delete(recursive: true); }
        Console.WriteLine("BMP depths, masks, palettes, RLE, orientation, failure and resource checks passed.");
    }

    private static byte[] BMPVector(int width, int height, ushort depth, byte[] pixels,
        byte[]? palette = null, uint compression = 0, int header = 40, uint[]? masks = null)
    {
        var maskBytes = (masks?.Length ?? 0) * 4;
        var tableOffset = 14 + Math.Max(header, maskBytes == 0 ? header : 40 + maskBytes);
        var offset = tableOffset + (palette?.Length ?? 0);
        var bytes = new byte[offset + pixels.Length];
        "BM"u8.CopyTo(bytes);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(2), bytes.Length);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(10), offset);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(14), header);
        if (header == 12)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(18), (ushort)width);
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(20), (ushort)height);
            bytes[22] = 1;
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(24), depth);
        }
        else
        {
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(18), width);
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(22), height);
            bytes[26] = 1;
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(28), depth);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(30), compression);
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(34), pixels.Length);
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(46), (palette?.Length ?? 0) / 4);
        }
        if (masks is not null)
            for (var i = 0; i < masks.Length; i++)
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(54 + 4 * i), masks[i]);
        palette?.CopyTo(bytes, tableOffset);
        pixels.CopyTo(bytes, offset);
        return bytes;
    }


    internal static void VerifyTGA()
    {
        using var image = new Image();
        foreach (var top in new[] { false, true })
            foreach (var right in new[] { false, true })
            {
                image.LoadTGAFromBuffer(TGA(top, right));
                CheckPixels(image, Pixels);
            }

        var indexed = TGAVector(9, 8, 0x30, 3, 2,
            [0x81, 6, 0x02, 7, 5, 6, 0x80, 7], palette: [0, 0, 255, 0, 255, 0, 255, 0, 0], first: 5);
        byte[] expected = [0, 0, 255, 255, 0, 255, 0, 255, 0, 255, 0, 255, 0, 0, 255, 255, 0, 255, 0, 255, 255, 0, 0, 255];
        image.LoadTGAFromBuffer(indexed);
        CheckPixels(image, expected);

        var indices16 = TGAVector(1, 16, 0x20, 2, 1, [0x00, 0x01, 0x01, 0x01],
            palette: [8, 17, 35, 64, 120, 65, 7, 192], first: 256, paletteDepth: 32);
        image.LoadTGAFromBuffer(indices16);
        Check(image.GetData().AsSpan().SequenceEqual(new byte[] { 35, 17, 8, 64, 7, 65, 120, 192 }), "Indexed TGA preserves palette origin and byte alpha.");

        image.LoadTGAFromBuffer(TGAVector(10, 16, 0x21, 2, 1, [0x01, 0x00, 0x7c, 0xe0, 0x83]));
        Check(image.GetData().AsSpan().SequenceEqual(new byte[] { 248, 0, 0, 0, 0, 248, 0, 255 }), "TGA preserves the optional 16-bit alpha bit.");
        image.LoadTGAFromBuffer(TGAVector(2, 16, 0x20, 1, 1, [0x1f, 0x00]));
        Check(image.GetData().AsSpan().SequenceEqual(new byte[] { 0, 0, 248, 255 }), "TGA packed color without alpha is opaque.");
        image.LoadTGAFromBuffer(TGAVector(2, 24, 0x20, 1, 1, [3, 2, 1]));
        Check(image.GetData().AsSpan().SequenceEqual(new byte[] { 1, 2, 3, 255 }), "TGA 24-bit color uses BGR storage.");
        image.LoadTGAFromBuffer(TGAVector(11, 8, 0x20, 3, 1, [0x82, 37]));
        Check(image.GetData().AsSpan().SequenceEqual(new byte[] { 37, 37, 37, 255, 37, 37, 37, 255, 37, 37, 37, 255 }), "TGA grayscale expands each shade to RGB.");
        image.LoadTGAFromBuffer(TGAVector(3, 16, 0x28, 1, 1, [200, 17]));
        Check(image.GetData().AsSpan().SequenceEqual(new byte[] { 200, 200, 200, 17 }), "TGA grayscale alpha is stored separately.");

        var crossRow = TGAVector(10, 24, 0x20, 3, 2, [0x83, 0, 0, 255, 1, 0, 255, 0, 255, 0, 0]);
        image.LoadTGAFromBuffer(crossRow);
        Check(image.GetPixel(0, 1) == Colors.Red && image.GetPixel(1, 1) == Colors.Green && image.GetPixel(2, 1) == Colors.Blue,
            "TGA repeat and literal packets can span row boundaries.");

        var footer = new byte[26];
        "TRUEVISION-XFILE.\0"u8.CopyTo(footer.AsSpan(8));
        image.LoadTGAFromBuffer([.. indexed, .. footer]);
        CheckPixels(image, expected);
        var unsupportedIndex = (byte[])indexed.Clone();
        unsupportedIndex[^1] = 8;
        var changes = 0;
        image.GenerateMIPMaps();
        var before = image.GetData();
        image.Changed += _ => changes++;
        foreach (var invalid in new byte[][]
        {
            indexed[..17], indexed[..19], indexed[..^1], unsupportedIndex, [.. indexed[..^1], .. footer],
            TGAVector(11, 8, 0x20, 1, 1, [0x81, 33]),
            TGAVector(11, 8, 0x20, ushort.MaxValue, ushort.MaxValue, [0x80, 1])
        })
            Reject<InvalidDataException>(() => image.LoadTGAFromBuffer(invalid));
        Check(image.HasMIPMaps && image.GetData().AsSpan().SequenceEqual(before) && changes == 0, "Malformed TGA keeps pixels, mipmaps and event count.");

        var folder = Directory.CreateTempSubdirectory("Electron2D-tga-");
        try
        {
            var path = IOPath.Combine(folder.FullName, "indexed.TGA");
            File.WriteAllBytes(path, indexed);
            image.Load(path);
            CheckPixels(image, expected);
            Check(changes == 1 && !image.HasMIPMaps, "TGA file success publishes a single base level.");
            using var texture = ResourceLoader.Load<ImageTexture>(path, ResourceLoader.CacheMode.Ignore);
            using var snapshot = texture.GetImage()!;
            CheckPixels(snapshot, expected);
        }
        finally { folder.Delete(recursive: true); }
        Console.WriteLine("TGA origins, palette, RLE, alpha, failure and resource checks passed.");
    }

    private static byte[] TGAVector(byte type, byte depth, byte descriptor, ushort width, ushort height,
        byte[] pixels, byte[]? palette = null, ushort first = 0, byte paletteDepth = 24)
    {
        var header = new byte[19];
        header[0] = 1;
        header[18] = 0x41;
        header[2] = type;
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(12), width);
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(14), height);
        header[16] = depth;
        header[17] = descriptor;
        if (palette is not null)
        {
            header[1] = 1;
            BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(3), first);
            BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(5), (ushort)(palette.Length / (paletteDepth / 8)));
            header[7] = paletteDepth;
        }
        return [.. header, .. (palette ?? []), .. pixels];
    }

    private static byte[] TGA(bool topDown, bool right = false)
    {
        var bytes = new byte[18 + Pixels.Length];
        bytes[2] = 2; bytes[12] = 3; bytes[14] = 2; bytes[16] = 32; bytes[17] = (byte)((topDown ? 40 : 8) | (right ? 16 : 0));
        for (var y = 0; y < 2; y++)
            for (var x = 0; x < 3; x++)
                for (var channel = 0; channel < 4; channel++)
                    bytes[18 + (y * 3 + x) * 4 + channel] = Pixels[((topDown ? y : 1 - y) * 3 + (right ? 2 - x : x)) * 4 + (channel == 3 ? 3 : 2 - channel)];
        return bytes;
    }

    private static void CheckPixels(Image image, byte[] expected) => Check(image.Size == new Vector2i(3, 2) && image.PixelFormat == Image.Format.Rgba8 && image.GetData().SequenceEqual(expected), "Decoded color, alpha, orientation and row pitch are exact.");
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
