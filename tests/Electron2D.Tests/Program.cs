using Electron2D;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EngineFileAccess = Electron2D.FileAccess;

VerifyInstanceIds();
VerifyLifetime();
VerifyNotificationsAndProperties();
VerifyEventConnections();
VerifyTranslations();
VerifyColors();
VerifyRectangles();
VerifyTransforms();
VerifyConfigFiles();
VerifyFileAccess();
VerifyDirAccess();
VerifyProjectSettings();
VerifyResources();
VerifyPackedScenes();
VerifyEngine();
VerifyMainLoop();
VerifyNodeHierarchyAndTransforms();
VerifyProcessing();
VerifySceneTree();
VerifySceneTreeGroupsEventsAndTimers();
VerifySceneTreeFailureSafety();

Console.WriteLine("Electron2D checks passed.");

static void VerifyColors()
{
    Require(Marshal.SizeOf<Color>() == 16 && typeof(Color).IsDefined(typeof(SerializableAttribute), inherit: false),
        "Color must be a serializable sequential four-float value type.");
    Require(default(Color) == new Color() && default(Color) == new Color(0f, 0f, 0f, 0f) &&
            Colors.Black == new Color(0f, 0f, 0f, 1f) && Colors.Transparent == new Color(1f, 1f, 1f, 0f),
        "Zero initialization, opaque black, and transparent white must remain distinct.");

    var rgba = new Color(0.1f, 0.2f, 0.3f, 0.4f);
    Require(new Color(rgba) == new Color(0.1f, 0.2f, 0.3f, 1f) && new Color(rgba, 0.7f).A == 0.7f,
        "The copy-and-alpha constructor must copy RGB and replace alpha.");
    Require(new Color(0x12345678u).ToRgba32() == 0x12345678u &&
            new Color(0x123456789abcdef0ul).ToRgba64() == 0x123456789abcdef0ul,
        "Packed RGBA constructors and encoders must be inverse for byte- and word-aligned values.");

    var channels = new Color();
    channels.R8 = 306;
    channels.G8 = -51;
    channels.B8 = 128;
    channels.A8 = 255;
    Require(channels.R8 == 306 && channels.G8 == -51 && channels.B8 == 128 && channels.A8 == 255 &&
            NearlyEqual(channels.R, 1.2f) && NearlyEqual(channels.G, -0.2f),
        "8-bit-scale properties must preserve typed binding overbright and negative values without clamping.");
    Require(new Color(float.NaN, float.PositiveInfinity, float.NegativeInfinity).R8 == 0 &&
            new Color(float.NaN, float.PositiveInfinity, float.NegativeInfinity).G8 == int.MaxValue &&
            new Color(float.NaN, float.PositiveInfinity, float.NegativeInfinity).B8 == int.MinValue,
        "8-bit-scale getters must define NaN and overflow deterministically.");
    channels[0] = 0.25f;
    channels[1] = 0.5f;
    channels[2] = 0.75f;
    channels[3] = 1f;
    Require(channels == new Color(0.25f, 0.5f, 0.75f, 1f),
        "The component indexer must map indices zero through three to RGBA.");
    Expect<ArgumentOutOfRangeException>(() => _ = channels[-1],
        "The color indexer getter must reject negative indices.");
    Expect<ArgumentOutOfRangeException>(() => channels[4] = 0f,
        "The color indexer setter must reject indices above three.");

    var red = Colors.Red;
    red.ToHsv(out var redHue, out var redSaturation, out var redValue);
    Require(NearlyEqual(redHue, 0f) && NearlyEqual(redSaturation, 1f) && NearlyEqual(redValue, 1f) &&
            ColorNearlyEqual(Color.FromHsv(1f / 3f, 1f, 1f, 0.25f), new Color(0f, 1f, 0f, 0.25f)) &&
            ColorNearlyEqual(Color.FromHsv(0.7f, 0f, 0.4f, 0.3f), new Color(0.4f, 0.4f, 0.4f, 0.3f)),
        "HSV conversion must cover chromatic and achromatic colors and preserve alpha.");
    var hsvMutable = new Color(1f, 0f, 0f, 0.35f) { H = 2f / 3f };
    var saturationMutable = new Color(1f, 0f, 0f, 0.35f) { S = 0f };
    var valueMutable = new Color(1f, 0f, 0f, 0.35f) { V = 0.5f };
    Require(ColorNearlyEqual(hsvMutable, new Color(0f, 0f, 1f, 0.35f)) &&
            ColorNearlyEqual(saturationMutable, new Color(1f, 1f, 1f, 0.35f)) &&
            ColorNearlyEqual(valueMutable, new Color(0.5f, 0f, 0f, 0.35f)),
        "HSV property setters must reconstruct RGB while preserving alpha.");

    Require(NearlyEqual(Colors.Red.OkHslH, 0.0812f, 0.001f) &&
            NearlyEqual(Colors.Red.OkHslS, 1f, 0.001f) &&
            NearlyEqual(Colors.Red.OkHslL, 0.5681f, 0.001f) &&
            NearlyEqual(Colors.Green.OkHslH, 0.3958f, 0.001f) &&
            NearlyEqual(Colors.Blue.OkHslH, 0.7335f, 0.001f),
        "OKHSL primary-color anchors must match the perceptual reference transform.");
    foreach (var sample in new[]
             {
                 Colors.Red, Colors.Green, Colors.Blue, new Color(0.12f, 0.47f, 0.83f, 0.6f),
                 new Color(0.2f, 0.2f, 0.2f, 0.4f), Colors.White, Colors.Black
             })
    {
        var roundTrip = Color.FromOkHsl(sample.OkHslH, sample.OkHslS, sample.OkHslL, sample.A);
        Require(ColorNearlyEqual(roundTrip, sample, 0.002f),
            "OKHSL conversion must round-trip ordinary sRGB colors.");
    }
    var okGrid = new[] { 0.1f, 0.3f, 0.5f, 0.7f, 0.9f };
    foreach (var gridRed in okGrid)
        foreach (var gridGreen in okGrid)
            foreach (var gridBlue in okGrid)
            {
                var sample = new Color(gridRed, gridGreen, gridBlue, 0.37f);
                var roundTrip = Color.FromOkHsl(sample.OkHslH, sample.OkHslS, sample.OkHslL, sample.A);
                Require(ColorNearlyEqual(roundTrip, sample, 0.003f),
                    $"OKHSL must round-trip a broad ordinary sRGB grid: {sample} -> ({sample.OkHslH}, {sample.OkHslS}, {sample.OkHslL}) -> {roundTrip}.");
            }
    var saturatedDarkBlue = new Color(0f, 0f, 0.25f, 0.37f);
    var clampedDarkBlue = Color.FromOkHsl(
        saturatedDarkBlue.OkHslH,
        saturatedDarkBlue.OkHslS,
        saturatedDarkBlue.OkHslL,
        saturatedDarkBlue.A);
    var okMutable = new Color(0.3f, 0.6f, 0.9f, 0.42f);
    okMutable.OkHslL = 0.4f;
    Require(NearlyEqual(new Color(0.25f, 0.25f, 0.25f).OkHslS, 0f) &&
            saturatedDarkBlue.OkHslS == 1f &&
            ColorNearlyEqual(clampedDarkBlue, new Color(0.000297069f, 0.022376226f, 0.216744155f, 0.37f), 0.000001f) &&
            okMutable.A == 0.42f &&
            Color.FromOkHsl(0.5f, 0.5f, 0.5f, -1f).A == 0f &&
            Color.FromOkHsl(0.5f, 0.5f, 0.5f, 2f).A == 1f &&
            Color.FromOkHsl(float.NaN, float.NaN, float.NaN, float.NaN) == default,
        "OKHSL must define achromatic saturation and clamp all constructed components.");

    Require(NearlyEqual(new Color(1f, 1f, 1f).Luminance, 1f) &&
            NearlyEqual(new Color(1f, 0f, 0f).Luminance, 0.2126f),
        "Luminance must use linear RGB coefficients and ignore alpha.");
    var blend = new Color(0f, 0f, 1f, 0.5f).Blend(new Color(1f, 0f, 0f, 0.5f));
    Require(ColorNearlyEqual(blend, new Color(2f / 3f, 0f, 1f / 3f, 0.75f)) &&
            default(Color).Blend(default) == default,
        "Blend must implement straight-alpha source-over and define the zero-alpha result.");

    Require(new Color(-1f, 0.4f, 2f, 3f).Clamp() == new Color(0f, 0.4f, 1f, 1f) &&
            new Color(0.1f, 0.4f, 0.8f, 0.9f).Clamp(
                new Color(0.2f, 0.3f, 0.4f, 0.5f), new Color(0.7f, 0.6f, 0.5f, 0.8f)) ==
            new Color(0.2f, 0.4f, 0.5f, 0.8f),
        "Clamp must apply default and custom componentwise bounds.");
    Expect<ArgumentException>(() => rgba.Clamp(new Color(1f, 0f, 0f), new Color(0f, 1f, 1f)),
        "Clamp must reject a componentwise reversed bound.");
    Require(ColorNearlyEqual(rgba.Darkened(2f), new Color(-0.1f, -0.2f, -0.3f, 0.4f)) &&
            ColorNearlyEqual(rgba.Lightened(2f), new Color(1.9f, 1.8f, 1.7f, 0.4f)) &&
            ColorNearlyEqual(rgba.Lerp(Colors.White, 2f), new Color(1.9f, 1.8f, 1.7f, 1.6f)) &&
            ColorNearlyEqual(rgba.Inverted(), new Color(0.9f, 0.8f, 0.7f, 0.4f)),
        "Color adjustment operations must preserve their documented unbounded interpolation behavior.");

    var srgbThreshold = new Color(0.04045f, 0.5f, 1f, 0.25f).SrgbToLinear();
    Require(NearlyEqual(srgbThreshold.R, 0.0031308f, 0.000001f) &&
            NearlyEqual(srgbThreshold.G, 0.214041f, 0.00001f) && srgbThreshold.A == 0.25f &&
            ColorNearlyEqual(srgbThreshold.LinearToSrgb(), new Color(0.04045f, 0.5f, 1f, 0.25f), 0.00001f),
        "sRGB transfer functions must cover their nonlinear branch and preserve alpha.");

    var packed = new Color(0.1f, 0.2f, 0.3f, 0.4f);
    Require(packed.ToRgba32() == 0x1a334c66u && packed.ToArgb32() == 0x661a334cu &&
            packed.ToAbgr32() == 0x664c331au &&
            new Color(1f, 0f, 1f, 0.5f).ToRgba64() == 0xffff0000ffff8000ul &&
            new Color(-1f, 2f, float.NaN, float.PositiveInfinity).ToRgba32() == 0x00ff00ffu,
        "Packed integer encoders must use the documented channel ordering and midpoint rounding.");
    for (var byteValue = 0; byteValue <= byte.MaxValue; byteValue++)
    {
        var byteColor = Color.Color8((byte)byteValue, (byte)byteValue, (byte)byteValue, (byte)byteValue);
        var repeated = (uint)byteValue * 0x01010101u;
        Require(byteColor.R8 == byteValue && byteColor.G8 == byteValue &&
                byteColor.B8 == byteValue && byteColor.A8 == byteValue && byteColor.ToRgba32() == repeated,
            "Every byte channel must survive Color8, integer-scale access, and RGBA packing.");
    }
    var rgbe = (24u << 27) | (3u << 18) | (2u << 9) | 1u;
    Require(Color.FromRgbe9995(rgbe) == new Color(1f, 2f, 3f, 1f),
        "RGBE9995 decoding must apply the shared exponent to all mantissas.");

    Require(Color.HtmlIsValid("#abc") && Color.HtmlIsValid("abcd") && Color.HtmlIsValid("A1b2C3") &&
            Color.HtmlIsValid("#10203040") && !Color.HtmlIsValid("") && !Color.HtmlIsValid("#") &&
            !Color.HtmlIsValid("#12xz") && !Color.HtmlIsValid("##ffffff"),
        "HTML validation must accept only optional-hash 3, 4, 6, and 8 digit hexadecimal forms.");
    Require(Color.FromHtml("#abc") == Color.Color8(0xaa, 0xbb, 0xcc) &&
            Color.FromHtml("abcd") == Color.Color8(0xaa, 0xbb, 0xcc, 0xdd) &&
            Color.FromHtml("10203040") == Color.Color8(0x10, 0x20, 0x30, 0x40) &&
            Color.FromHtml(ReadOnlySpan<char>.Empty) == Colors.Black &&
            new Color(-1f, 0.5f, 2f, 0.5f).ToHtml() == "0080ff80" &&
            new Color(1f, 0.5f, 0f).ToHtml(includeAlpha: false) == "ff8000",
        "HTML parsing and formatting must handle shorthand, alpha, the empty compatibility case, clamping, and lowercase output.");
    Expect<ArgumentOutOfRangeException>(() => Color.FromHtml("12"),
        "HTML parsing must reject an invalid length.");
    Expect<ArgumentOutOfRangeException>(() => Color.FromHtml("12xz"),
        "HTML parsing must reject a non-hexadecimal character.");
    Expect<ArgumentNullException>(() => _ = new Color((string)null!),
        "The string constructor must reject null.");

    Require(new Color("dark slate-gray") == Colors.DarkSlateGray &&
            new Color("Rebecca_Purple", 0.25f) == new Color(Colors.RebeccaPurple, 0.25f) &&
            Color.FromString("not-a-color", rgba) == rgba &&
            Colors.Aqua == Colors.Cyan && Colors.Fuchsia == Colors.Magenta && Colors.Green == Colors.Lime &&
            Colors.Gray.ToRgba32() == 0xbebebeffu && Colors.WebGray.ToRgba32() == 0x808080ffu &&
            Colors.Maroon.ToRgba32() == 0xb03060ffu && Colors.WebMaroon.ToRgba32() == 0x800000ffu,
        "Named parsing must normalize supported separators, replace alpha, provide fallback, and preserve aliases.");
    Expect<ArgumentOutOfRangeException>(() => _ = new Color("not-a-color"),
        "The string constructor must reject an unknown color name.");
    Expect<ArgumentNullException>(() => Color.FromString(null!, rgba),
        "Named fallback parsing must reject null.");

    var namedProperties = typeof(Colors).GetProperties(
        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
    Require(namedProperties.Length == 146 && namedProperties.All(property => property.PropertyType == typeof(Color)),
        "Colors must expose the complete 146-property named catalog.");
    foreach (var property in namedProperties)
    {
        var value = (Color)property.GetValue(null)!;
        Require(new Color(property.Name) == value,
            "Every public named property must be discoverable by the string constructor.");
    }
    Parallel.ForEach(namedProperties, property =>
    {
        var value = (Color)property.GetValue(null)!;
        Require(Color.FromString(property.Name, default) == value,
            "Concurrent named lookup must be deterministic.");
    });

    Require(ColorNearlyEqual(rgba + rgba, new Color(0.2f, 0.4f, 0.6f, 0.8f)) &&
            rgba - rgba == default && +rgba == rgba && ColorNearlyEqual(-rgba, new Color(0.9f, 0.8f, 0.7f, 0.6f)) &&
            rgba * 2f == 2f * rgba && ColorNearlyEqual(rgba * rgba, new Color(0.01f, 0.04f, 0.09f, 0.16f)) &&
            ColorNearlyEqual((rgba * 2f) / 2f, rgba) && ColorNearlyEqual(rgba / rgba, Colors.White),
        "Arithmetic operators must act componentwise, including alpha.");
    var divisionByZero = Colors.White / 0f;
    Require(float.IsPositiveInfinity(divisionByZero.R) && float.IsPositiveInfinity(divisionByZero.A),
        "Scalar zero division must retain IEEE 754 behavior.");
    Require(new Color(0f, 1f, 1f) < new Color(1f, 0f, 0f) &&
            new Color(0f, 1f, 1f) <= new Color(0f, 1f, 1f) &&
            new Color(1f, 0f, 0f) > new Color(0f, 1f, 1f) &&
            new Color(1f, 0f, 0f) >= new Color(1f, 0f, 0f),
        "Relational operators must compare RGBA lexicographically.");
    var nanColor = new Color(float.NaN, 0f, 0f);
    Require(nanColor != new Color(float.NaN, 0f, 0f) && !(nanColor < Colors.Black) && !(nanColor > Colors.Black) &&
            !(nanColor <= Colors.Black) && !(nanColor >= Colors.Black) &&
            new Color(float.PositiveInfinity, 0f, 0f).IsEqualApprox(new Color(float.PositiveInfinity, 0f, 0f)) &&
            !nanColor.IsEqualApprox(new Color(float.NaN, 0f, 0f)),
        "NaN and infinity comparisons must follow exact and approximate floating-point contracts.");
    Require(new Color(1f, 2f, 3f, 4f).GetHashCode() == new Color(1f, 2f, 3f, 4f).GetHashCode() &&
            new Color(1f, 2f, 3f, 4f).IsEqualApprox(new Color(1.000001f, 2f, 3f, 4f)),
        "Equal colors must hash equally and approximate equality must tolerate small relative error.");

    var previousCulture = CultureInfo.CurrentCulture;
    try
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        Require(new Color(1.5f, 2.5f, 3.5f, 4.5f).ToString("F1") == "(1.5, 2.5, 3.5, 4.5)",
            "Color formatting must use invariant culture.");
    }
    finally
    {
        CultureInfo.CurrentCulture = previousCulture;
    }

    var colorKey = new ConfigKey<Color>("graphics", "tint");
    using (var config = new ConfigFile())
    {
        var stored = new Color(1f, 0.5f, 0f, 1f);
        config.SetValue(colorKey, stored);
        Require(config.EncodeToText() == "[graphics]\n\ntint={\"R\":1,\"G\":0.5,\"B\":0,\"A\":1}\n" &&
                config.GetValue(colorKey) == stored,
            "ConfigFile must use the stable finite R/G/B/A color schema.");
        Expect<JsonException>(() => config.SetValue(colorKey, new Color(float.NaN, 0f, 0f)),
            "ConfigFile must reject non-finite color components before mutation.");
        Require(config.GetValue(colorKey) == stored,
            "Failed color serialization must preserve the prior configuration token.");
        config.Parse("[graphics]\ntint={\"R\":1,\"G\":0,\"B\":0}\n");
        Expect<InvalidDataException>(() => config.GetValue(colorKey),
            "ConfigFile must reject a color with missing fields during typed decoding.");
        config.Parse("[graphics]\ntint={\"R\":1,\"G\":0,\"B\":0,\"A\":1,\"X\":0}\n");
        Expect<InvalidDataException>(() => config.GetValue(colorKey),
            "ConfigFile must reject unknown color fields during typed decoding.");
        config.Parse("[graphics]\ntint={\"R\":1,\"R\":0,\"G\":0,\"B\":0,\"A\":1}\n");
        Expect<InvalidDataException>(() => config.GetValue(colorKey),
            "ConfigFile must reject duplicate color fields during typed decoding.");
        config.Parse("[graphics]\ntint={\"R\":\"red\",\"G\":0,\"B\":0,\"A\":1}\n");
        Expect<InvalidDataException>(() => config.GetValue(colorKey),
            "ConfigFile must reject nonnumeric color fields during typed decoding.");
    }

    using (var scene = new PackedScene())
    {
        var source = new ColorPackedNode { Name = "ColorRoot", Tint = new Color(0.2f, 0.4f, 1.5f, 0.7f) };
        scene.Pack(source);
        source.Dispose();
        using var instance = (ColorPackedNode)scene.Instantiate();
        Require(instance.Tint == new Color(0.2f, 0.4f, 1.5f, 0.7f),
            "PackedScene must preserve Color stored properties including HDR components.");
    }

    _ = ExerciseColorHotPath(32);
    var beforeAllocations = GC.GetAllocatedBytesForCurrentThread();
    var hotResult = ExerciseColorHotPath(10_000);
    var allocated = GC.GetAllocatedBytesForCurrentThread() - beforeAllocations;
    Require(allocated == 0 && float.IsFinite(hotResult.R),
        "Warmed numeric color operations must not allocate managed memory.");
}

static Color ExerciseColorHotPath(int iterations)
{
    var value = new Color(0.1f, 0.2f, 0.3f, 0.4f);
    var destination = new Color(0.9f, 0.8f, 0.7f, 0.6f);
    for (var index = 0; index < iterations; index++)
    {
        value = value.Lerp(destination, 0.0001f).SrgbToLinear().LinearToSrgb();
        value = (value * 1.00001f).Clamp(new Color(-10f, -10f, -10f, -10f), new Color(10f, 10f, 10f, 10f));
    }

    return value;
}

static bool ColorNearlyEqual(Color left, Color right, float epsilon = 0.0001f) =>
    NearlyEqual(left.R, right.R, epsilon) && NearlyEqual(left.G, right.G, epsilon) &&
    NearlyEqual(left.B, right.B, epsilon) && NearlyEqual(left.A, right.A, epsilon);

static void VerifyRectangles()
{
    Require(Marshal.SizeOf<Rect2>() == 16 && typeof(Rect2).IsDefined(typeof(SerializableAttribute), inherit: false) &&
            typeof(Rect2).StructLayoutAttribute?.Value == LayoutKind.Sequential,
        "Rect2 must be a serializable sequential four-float value type.");
    Require(default(Rect2) == new Rect2(Vector2.Zero, Vector2.Zero) &&
            new Rect2(new Vector2(1f, 2f), new Vector2(3f, 4f)) == new Rect2(1f, 2f, 3f, 4f) &&
            new Rect2(new Vector2(1f, 2f), 3f, 4f) == new Rect2(1f, 2f, new Vector2(3f, 4f)),
        "Zero initialization and every typed constructor must preserve position and size.");
    Require((int)Side.Left == 0 && (int)Side.Top == 1 && (int)Side.Right == 2 && (int)Side.Bottom == 3,
        "Side numeric values must remain stable.");

    var mutable = new Rect2(new Vector2(1f, 2f), new Vector2(3f, 4f));
    mutable.Position = new Vector2(2f, 3f);
    mutable.Size = new Vector2(5f, 6f);
    Require(mutable.End == new Vector2(7f, 9f),
        "Position and Size mutation must update the computed end.");
    mutable.End = new Vector2(10f, 12f);
    Require(mutable.Position == new Vector2(2f, 3f) && mutable.Size == new Vector2(8f, 9f),
        "Assigning End must preserve Position and derive Size.");
    Require(new Rect2(0f, 0f, 3f, 4f).Area == 12f &&
            new Rect2(0f, 0f, -3f, -4f).Area == 12f &&
            !new Rect2(0f, 0f, -3f, -4f).HasArea(),
        "Area must remain a signed product while HasArea requires two positive components.");

    var normalized = new Rect2(25f, 25f, -100f, -50f).Abs();
    Require(normalized == new Rect2(-75f, -25f, 100f, 50f),
        "Abs must move the origin and normalize both size components.");
    var outer = new Rect2(0f, 0f, 10f, 10f);
    Require(outer.Encloses(new Rect2(0f, 0f, 10f, 10f)) &&
            outer.Encloses(new Rect2(2f, 3f, 4f, 5f)) &&
            !outer.Encloses(new Rect2(-1f, 3f, 4f, 5f)),
        "Encloses must accept coincident edges and reject an escaped edge.");
    Require(new Rect2(0f, 0f, 5f, 5f).Expand(new Vector2(-2f, 7f)) == new Rect2(-2f, 0f, 7f, 7f) &&
            outer.Expand(new Vector2(10f, 10f)) == outer,
        "Expand must grow only the edges needed to include a point.");
    Require(new Rect2(1f, 2f, 3f, 4f).GetCenter() == new Vector2(2.5f, 4f) &&
            new Rect2(1f, 2f, 3f, 4f).GetSupport(new Vector2(1f, -1f)) == new Vector2(4f, 2f) &&
            new Rect2(1f, 2f, 3f, 4f).GetSupport(Vector2.Zero) == new Vector2(1f, 2f),
        "Center and support mapping must use the documented edges.");

    var baseRect = new Rect2(1f, 2f, 3f, 4f);
    Require(baseRect.Grow(2f) == new Rect2(-1f, 0f, 7f, 8f) &&
            baseRect.Grow(-1f) == new Rect2(2f, 3f, 1f, 2f) &&
            baseRect.GrowIndividual(1f, 2f, 3f, 4f) == new Rect2(0f, 0f, 7f, 10f),
        "Grow operations must move origins and add the matching side amounts.");
    Require(baseRect.GrowSide(Side.Left, 1f) == new Rect2(0f, 2f, 4f, 4f) &&
            baseRect.GrowSide(Side.Top, 1f) == new Rect2(1f, 1f, 3f, 5f) &&
            baseRect.GrowSide(Side.Right, 1f) == new Rect2(1f, 2f, 4f, 4f) &&
            baseRect.GrowSide(Side.Bottom, 1f) == new Rect2(1f, 2f, 3f, 5f) &&
            baseRect.GrowSide((Side)99, 1f) == baseRect,
        "GrowSide must cover every side and leave undefined values unchanged.");

    Require(outer.HasArea() && !new Rect2(0f, 0f, 0f, 1f).HasArea() &&
            !new Rect2(0f, 0f, 1f, -1f).HasArea(),
        "HasArea must reject zero and negative size components.");
    Require(outer.HasPoint(Vector2.Zero) && outer.HasPoint(new Vector2(9.999f, 9.999f)) &&
            !outer.HasPoint(new Vector2(10f, 5f)) && !outer.HasPoint(new Vector2(5f, 10f)) &&
            !outer.HasPoint(new Vector2(-0.001f, 5f)),
        "HasPoint must include left/top edges and exclude right/bottom edges.");

    var overlap = new Rect2(8f, 4f, 5f, 8f);
    var touching = new Rect2(10f, 2f, 4f, 3f);
    var containedEmpty = new Rect2(5f, 6f, 0f, 0f);
    Require(outer.Intersects(overlap) && outer.Intersection(overlap) == new Rect2(8f, 4f, 2f, 6f) &&
            !outer.Intersects(touching) && outer.Intersects(touching, includeBorders: true) &&
            outer.Intersection(touching) == default && !outer.Intersects(new Rect2(11f, 0f, 1f, 1f)) &&
            outer.Intersects(containedEmpty) && outer.Intersection(containedEmpty) == containedEmpty,
        "Intersection tests must distinguish positive overlap, touching borders, and separation.");
    Require(outer.Merge(overlap) == new Rect2(0f, 0f, 13f, 12f),
        "Merge must return the smallest enclosing rectangle.");
    var exact = new Rect2(1f, 2f, 3f, 4f);
    var nanRect = new Rect2(float.NaN, 2f, 3f, 4f);
    Require(exact == new Rect2(1f, 2f, 3f, 4f) && exact != new Rect2(1f, 2f, 3f, 5f) &&
            exact.Equals((object)new Rect2(1f, 2f, 3f, 4f)) &&
            exact.GetHashCode() == new Rect2(1f, 2f, 3f, 4f).GetHashCode() &&
            exact.IsEqualApprox(new Rect2(1.000001f, 2f, 3f, 4f)) &&
            new Rect2(float.PositiveInfinity, 0f, 1f, 1f).IsEqualApprox(
                new Rect2(float.PositiveInfinity, 0f, 1f, 1f)) &&
            nanRect != new Rect2(float.NaN, 2f, 3f, 4f) && !nanRect.IsEqualApprox(nanRect),
        "Exact and approximate equality must define finite, infinity, and NaN behavior.");
    Require(exact.IsFinite() && !nanRect.IsFinite() &&
            !new Rect2(0f, 0f, float.NegativeInfinity, 1f).IsFinite(),
        "IsFinite must inspect every position and size component.");

    var previousCulture = CultureInfo.CurrentCulture;
    try
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        Require(new Rect2(1.5f, 2.5f, 3.5f, 4.5f).ToString() == "<1.5, 2.5>, <3.5, 4.5>" &&
                new Rect2(1.5f, 2.5f, 3.5f, 4.5f).ToString("F1") == "<1.5, 2.5>, <3.5, 4.5>",
            "Rect2 formatting must use invariant culture.");
        Expect<FormatException>(() => _ = new Rect2(1f, 2f, 3f, 4f).ToString("Q"),
            "Rect2 formatting must surface invalid numeric formats.");
    }
    finally
    {
        CultureInfo.CurrentCulture = previousCulture;
    }

    var rectangleKey = new ConfigKey<Rect2>("geometry", "bounds");
    using (var config = new ConfigFile())
    {
        var stored = new Rect2(1f, 2f, 3f, 4f);
        config.SetValue(rectangleKey, stored);
        Require(config.EncodeToText() ==
                "[geometry]\n\nbounds={\"Position\":{\"X\":1,\"Y\":2},\"Size\":{\"X\":3,\"Y\":4}}\n" &&
                config.GetValue(rectangleKey) == stored,
            "ConfigFile must use the stable finite Position/Size rectangle schema.");
        Expect<JsonException>(() => config.SetValue(rectangleKey, new Rect2(float.NaN, 0f, 1f, 1f)),
            "ConfigFile must reject non-finite rectangle components before mutation.");
        Require(config.GetValue(rectangleKey) == stored,
            "Failed rectangle serialization must preserve the prior configuration token.");

        config.Parse("[geometry]\nbounds={\"Position\":{\"X\":1,\"Y\":2}}\n");
        Expect<InvalidDataException>(() => config.GetValue(rectangleKey),
            "ConfigFile must reject a rectangle with a missing field.");
        config.Parse("[geometry]\nbounds={\"Position\":{\"X\":1,\"Y\":2},\"Size\":{\"X\":3,\"Y\":4},\"End\":{\"X\":4,\"Y\":6}}\n");
        Expect<InvalidDataException>(() => config.GetValue(rectangleKey),
            "ConfigFile must reject unknown rectangle fields.");
        config.Parse("[geometry]\nbounds={\"Position\":{\"X\":1,\"X\":2,\"Y\":2},\"Size\":{\"X\":3,\"Y\":4}}\n");
        Expect<InvalidDataException>(() => config.GetValue(rectangleKey),
            "ConfigFile must reject duplicate rectangle vector components.");
        config.Parse("[geometry]\nbounds={\"Position\":{\"X\":1,\"Y\":2},\"Position\":{\"X\":1,\"Y\":2},\"Size\":{\"X\":3,\"Y\":4}}\n");
        Expect<InvalidDataException>(() => config.GetValue(rectangleKey),
            "ConfigFile must reject duplicate rectangle fields.");
        config.Parse("[geometry]\nbounds={\"Position\":{\"X\":1},\"Size\":{\"X\":3,\"Y\":4}}\n");
        Expect<InvalidDataException>(() => config.GetValue(rectangleKey),
            "ConfigFile must reject incomplete rectangle vectors.");
        config.Parse("[geometry]\nbounds={\"Position\":{\"X\":\"left\",\"Y\":2},\"Size\":{\"X\":3,\"Y\":4}}\n");
        Expect<InvalidDataException>(() => config.GetValue(rectangleKey),
            "ConfigFile must reject nonnumeric rectangle vector components.");
        config.Parse("[geometry]\nbounds={\"Position\":{\"X\":1e999,\"Y\":2},\"Size\":{\"X\":3,\"Y\":4}}\n");
        Expect<InvalidDataException>(() => config.GetValue(rectangleKey),
            "ConfigFile must reject non-finite numeric rectangle components.");
    }

    using (var scene = new PackedScene())
    {
        var source = new ColorPackedNode
        {
            Name = "GeometryRoot",
            Bounds = new Rect2(-2f, -3f, 8f, 9f),
        };
        scene.Pack(source);
        source.Dispose();
        using var instance = (ColorPackedNode)scene.Instantiate();
        Require(instance.Bounds == new Rect2(-2f, -3f, 8f, 9f),
            "PackedScene must preserve stored Rect2 properties.");
    }

    _ = ExerciseRect2HotPath(32);
    var beforeAllocations = GC.GetAllocatedBytesForCurrentThread();
    var hotResult = ExerciseRect2HotPath(10_000);
    var allocated = GC.GetAllocatedBytesForCurrentThread() - beforeAllocations;
    Require(allocated == 0 && hotResult.IsFinite(),
        "Warmed rectangle geometry operations must not allocate managed memory.");
}

static Rect2 ExerciseRect2HotPath(int iterations)
{
    var value = new Rect2(1f, 2f, 3f, 4f);
    var bounds = new Rect2(-100f, -100f, 200f, 200f);
    for (var index = 0; index < iterations; index++)
    {
        value = value.Grow(0.0001f).Intersection(bounds);
        value = value.Merge(new Rect2(1f, 2f, 3f, 4f));
    }

    return value;
}

static void VerifyTransforms()
{
    Require(Marshal.SizeOf<Transform2D>() == 24 &&
            typeof(Transform2D).IsDefined(typeof(SerializableAttribute), inherit: false) &&
            typeof(Transform2D).StructLayoutAttribute?.Value == LayoutKind.Sequential,
        "Transform2D must be a serializable sequential six-float value type.");
    Require(default(Transform2D) == new Transform2D(Vector2.Zero, Vector2.Zero, Vector2.Zero) &&
            default(Transform2D) != Transform2D.Identity &&
            Transform2D.Identity == new Transform2D(1f, 0f, 0f, 1f, 0f, 0f) &&
            Transform2D.FlipX == new Transform2D(-1f, 0f, 0f, 1f, 0f, 0f) &&
            Transform2D.FlipY == new Transform2D(1f, 0f, 0f, -1f, 0f, 0f),
        "Zero initialization and the three standard transforms must remain distinct and stable.");

    var indexed = new Transform2D(Vector2.UnitX, Vector2.UnitY, new Vector2(2f, 3f));
    indexed[0] = new Vector2(4f, 5f);
    indexed[1, 0] = 6f;
    indexed[2, 1] = 7f;
    Require(indexed.X == new Vector2(4f, 5f) && indexed.Y == new Vector2(6f, 1f) &&
            indexed.Origin == new Vector2(2f, 7f) && indexed[0, 1] == 5f,
        "Column and component indexers must read and mutate the same sequential storage.");
    Expect<ArgumentOutOfRangeException>(() => _ = indexed[-1],
        "The column indexer must reject negative indices.");
    Expect<ArgumentOutOfRangeException>(() => indexed[3] = Vector2.Zero,
        "The column indexer must reject indices after Origin.");
    Expect<ArgumentOutOfRangeException>(() => _ = indexed[0, 2],
        "The component indexer must reject rows after Y.");
    Expect<ArgumentOutOfRangeException>(() => indexed[3, 0] = 1f,
        "The component indexer must reject invalid columns before mutation.");

    var quarterTurn = new Transform2D(MathF.PI * 0.5f, new Vector2(3f, 4f));
    Require(VectorNearlyEqual(quarterTurn.X, Vector2.UnitY) &&
            VectorNearlyEqual(quarterTurn.Y, -Vector2.UnitX) &&
            VectorNearlyEqual(quarterTurn * new Vector2(2f, 1f), new Vector2(2f, 6f)) &&
            NearlyEqual(quarterTurn.Rotation, MathF.PI * 0.5f),
        "Rotation construction and point transformation must use clockwise screen-space columns.");

    var decomposed = new Transform2D(0.4f, new Vector2(2f, -3f), 0.2f, new Vector2(5f, 6f));
    Require(NearlyEqual(decomposed.Rotation, 0.4f) &&
            VectorNearlyEqual(decomposed.Scale, new Vector2(2f, -3f)) &&
            NearlyEqual(decomposed.Skew, 0.2f) && decomposed.Origin == new Vector2(5f, 6f) &&
            NearlyEqual(Transform2D.FlipX.Determinant(), -1f) &&
            default(Transform2D).Scale == Vector2.Zero && NearlyEqual(default(Transform2D).Skew, 0f),
        "Rotation, signed scale, skew, origin, and reflection determinant must decompose consistently.");

    var basis = new Transform2D(new Vector2(2f, 1f), new Vector2(-1f, 3f), new Vector2(100f, 200f));
    Require(basis.BasisXform(new Vector2(4f, 5f)) == new Vector2(3f, 19f) &&
            VectorNearlyEqual(quarterTurn.BasisXformInv(quarterTurn.BasisXform(new Vector2(4f, 5f))), new Vector2(4f, 5f)),
        "Basis transforms must ignore Origin and the inverse shortcut must invert orthonormal bases.");

    var affine = new Transform2D(0.35f, new Vector2(2f, 3f), 0.25f, new Vector2(4f, -2f));
    var affineInverse = affine.AffineInverse();
    var point = new Vector2(8f, -5f);
    Require(TransformNearlyEqual(affine * affineInverse, Transform2D.Identity) &&
            TransformNearlyEqual(affineInverse * affine, Transform2D.Identity) &&
            VectorNearlyEqual(affineInverse * (affine * point), point),
        "AffineInverse must invert rotation, non-uniform scale, skew, and translation.");
    Expect<InvalidOperationException>(
        () => new Transform2D(Vector2.UnitX, Vector2.UnitX, Vector2.Zero).AffineInverse(),
        "AffineInverse must reject an exactly singular basis.");

    var orthonormalInverse = quarterTurn.Inverse();
    Require(VectorNearlyEqual(orthonormalInverse * (quarterTurn * point), point) &&
            VectorNearlyEqual((quarterTurn * point) * quarterTurn, point),
        "Inverse and reverse point multiplication must invert an orthonormal transform.");

    var parent = new Transform2D(0.6f, new Vector2(4f, 5f));
    var child = new Transform2D(-0.2f, new Vector2(2f, 3f));
    Require(VectorNearlyEqual((parent * child) * point, parent * (child * point)),
        "Transform multiplication must compose parent and child in application order.");

    var localFrame = new Transform2D(new Vector2(2f, 0f), new Vector2(0f, 3f), new Vector2(1f, 2f));
    var rotatedGlobal = localFrame.Rotated(MathF.PI * 0.5f);
    var rotatedLocal = localFrame.RotatedLocal(MathF.PI * 0.5f);
    Require(VectorNearlyEqual(rotatedGlobal.X, new Vector2(0f, 2f)) &&
            VectorNearlyEqual(rotatedGlobal.Y, new Vector2(-3f, 0f)) &&
            VectorNearlyEqual(rotatedGlobal.Origin, new Vector2(-2f, 1f)) &&
            VectorNearlyEqual(rotatedLocal.X, new Vector2(0f, 3f)) &&
            VectorNearlyEqual(rotatedLocal.Y, new Vector2(-2f, 0f)) &&
            rotatedLocal.Origin == localFrame.Origin,
        "Global and local rotation must multiply on opposite sides.");

    var rotatedFrame = new Transform2D(MathF.PI * 0.5f, new Vector2(10f, 20f));
    Require(rotatedFrame.Translated(Vector2.UnitX).Origin == new Vector2(11f, 20f) &&
            VectorNearlyEqual(rotatedFrame.TranslatedLocal(Vector2.UnitX).Origin, new Vector2(10f, 21f)),
        "Global and local translation must distinguish world offsets from basis-relative offsets.");
    var scaledGlobal = rotatedFrame.Scaled(new Vector2(2f, 3f));
    var scaledLocal = rotatedFrame.ScaledLocal(new Vector2(2f, 3f));
    Require(VectorNearlyEqual(scaledGlobal.X, new Vector2(0f, 3f)) &&
            VectorNearlyEqual(scaledGlobal.Y, new Vector2(-2f, 0f)) &&
            scaledGlobal.Origin == new Vector2(20f, 60f) &&
            VectorNearlyEqual(scaledLocal.X, new Vector2(0f, 2f)) &&
            VectorNearlyEqual(scaledLocal.Y, new Vector2(-3f, 0f)) &&
            scaledLocal.Origin == rotatedFrame.Origin,
        "Global scale must scale rows and origin while local scale must scale basis columns only.");

    var start = new Transform2D(170f * MathF.PI / 180f, new Vector2(1f, -1f), 0f, Vector2.Zero);
    var finish = new Transform2D(-170f * MathF.PI / 180f, new Vector2(3f, -3f), 0.2f, new Vector2(10f, 20f));
    var midpoint = start.InterpolateWith(finish, 0.5f);
    var extrapolated = start.InterpolateWith(finish, 2f);
    Require(VectorNearlyEqual(midpoint.X, new Vector2(-2f, 0f), 0.001f) &&
            VectorNearlyEqual(midpoint.Scale, new Vector2(2f, -2f), 0.001f) &&
            midpoint.Origin == new Vector2(5f, 10f) &&
            extrapolated.Origin == new Vector2(20f, 40f) &&
            start.InterpolateWith(finish, 0f).IsEqualApprox(start) &&
            start.InterpolateWith(finish, 1f).IsEqualApprox(finish),
        "Interpolation must use the shortest angular path, preserve reflected scale, and allow extrapolation.");

    Require(Transform2D.Identity.IsConformal() &&
            new Transform2D(Vector2.One * 2f, new Vector2(-2f, 2f), Vector2.Zero).IsConformal() &&
            Transform2D.FlipX.IsConformal() &&
            !new Transform2D(new Vector2(2f, 0f), Vector2.UnitY, Vector2.Zero).IsConformal() &&
            !new Transform2D(Vector2.UnitX, new Vector2(1f, 1f), Vector2.Zero).IsConformal(),
        "Conformal checks must accept uniform rotation/reflection and reject non-uniform scale or skew.");
    Require(Transform2D.Identity.IsFinite() &&
            !new Transform2D(new Vector2(float.NaN, 0f), Vector2.UnitY, Vector2.Zero).IsFinite() &&
            !new Transform2D(Vector2.UnitX, Vector2.UnitY, new Vector2(float.PositiveInfinity, 0f)).IsFinite(),
        "IsFinite must inspect every basis and origin component.");

    var orthonormalized = basis.Orthonormalized();
    var zeroOrthonormalized = default(Transform2D).Orthonormalized();
    Require(VectorNearlyEqual(orthonormalized.X, new Vector2(0.8944272f, 0.4472136f)) &&
            NearlyEqual(Vector2.Dot(orthonormalized.X, orthonormalized.Y), 0f) &&
            NearlyEqual(orthonormalized.X.Length(), 1f) && NearlyEqual(orthonormalized.Y.Length(), 1f) &&
            orthonormalized.Origin == basis.Origin && zeroOrthonormalized == default,
        "Orthonormalized must preserve Origin and keep degenerate zero axes finite.");

    var lookingDown = Transform2D.Identity.LookingAt(Vector2.UnitY);
    var lookingScaled = affine.LookingAt(new Vector2(9f, 3f));
    Require(NearlyEqual(lookingDown.Rotation, MathF.PI * 0.5f) && lookingDown.Origin == Vector2.Zero &&
            VectorNearlyEqual(lookingDown.Scale, Vector2.One) &&
            lookingScaled.Origin == affine.Origin && VectorNearlyEqual(lookingScaled.Scale, Vector2.One) &&
            NearlyEqual(lookingScaled.Skew, 0f) && NearlyEqual(lookingScaled.Rotation, 0.7553597f),
        "LookingAt must use affine-local scale compensation while preserving Origin and removing scale and skew.");
    Expect<InvalidOperationException>(() => default(Transform2D).LookingAt(Vector2.One),
        "LookingAt must surface a singular source basis.");

    var sourcePoints = new[] { Vector2.Zero, Vector2.UnitX, new Vector2(2f, -3f) };
    var transformedPoints = quarterTurn * sourcePoints;
    var restoredPoints = transformedPoints * quarterTurn;
    Require(transformedPoints.Length == sourcePoints.Length && restoredPoints.Length == sourcePoints.Length &&
            sourcePoints.Where((source, index) => !VectorNearlyEqual(source, restoredPoints[index])).Count() == 0 &&
            (Transform2D.Identity * Array.Empty<Vector2>()).Length == 0,
        "Array operators must return complete transformed copies in source order.");
    Vector2[] nullPoints = null!;
    Expect<ArgumentNullException>(() => _ = Transform2D.Identity * nullPoints,
        "Forward array transformation must reject null explicitly.");
    Expect<ArgumentNullException>(() => _ = nullPoints * Transform2D.Identity,
        "Inverse array transformation must reject null explicitly.");

    var scalar = new Transform2D(1f, 2f, 3f, 4f, 5f, 6f);
    Require((scalar * 2f) / 2f == scalar && !(scalar / 0f).IsFinite(),
        "Scalar arithmetic must affect every component and retain IEEE division behavior.");
    var approximate = new Transform2D(1.000001f, 0f, 0f, 1f, 0f, 0f);
    var nanTransform = new Transform2D(new Vector2(float.NaN, 0f), Vector2.UnitY, Vector2.Zero);
    var signedZeroTransform = new Transform2D(-0f, 0f, 0f, -0f, 0f, -0f);
    Require(Transform2D.Identity == new Transform2D(1f, 0f, 0f, 1f, 0f, 0f) &&
            Transform2D.Identity != approximate && Transform2D.Identity.IsEqualApprox(approximate) &&
            new Transform2D(new Vector2(float.PositiveInfinity, 0f), Vector2.UnitY, Vector2.Zero).IsEqualApprox(
                new Transform2D(new Vector2(float.PositiveInfinity, 0f), Vector2.UnitY, Vector2.Zero)) &&
            nanTransform != new Transform2D(new Vector2(float.NaN, 0f), Vector2.UnitY, Vector2.Zero) &&
            !nanTransform.IsEqualApprox(new Transform2D(new Vector2(float.NaN, 0f), Vector2.UnitY, Vector2.Zero)) &&
            signedZeroTransform == default && signedZeroTransform.GetHashCode() == default(Transform2D).GetHashCode() &&
            scalar.Equals((object)new Transform2D(1f, 2f, 3f, 4f, 5f, 6f)) &&
            scalar.GetHashCode() == new Transform2D(1f, 2f, 3f, 4f, 5f, 6f).GetHashCode(),
        "Exact and approximate equality must define finite, infinity, and NaN behavior.");

    var previousCulture = CultureInfo.CurrentCulture;
    try
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        Require(new Transform2D(1.5f, 2.5f, 3.5f, 4.5f, 5.5f, 6.5f).ToString("F1") ==
                "[X: <1.5, 2.5>, Y: <3.5, 4.5>, O: <5.5, 6.5>]",
            "Transform2D formatting must use invariant culture.");
        Expect<FormatException>(() => _ = scalar.ToString("Q"),
            "Transform2D formatting must surface invalid numeric formats.");
    }
    finally
    {
        CultureInfo.CurrentCulture = previousCulture;
    }

    var transformKey = new ConfigKey<Transform2D>("geometry", "transform");
    using (var config = new ConfigFile())
    {
        config.SetValue(transformKey, scalar);
        Require(config.EncodeToText() ==
                "[geometry]\n\ntransform={\"X\":{\"X\":1,\"Y\":2},\"Y\":{\"X\":3,\"Y\":4},\"Origin\":{\"X\":5,\"Y\":6}}\n" &&
                config.GetValue(transformKey) == scalar,
            "ConfigFile must use the stable finite X/Y/Origin transform schema.");
        Expect<JsonException>(() => config.SetValue(
                transformKey,
                new Transform2D(new Vector2(float.NaN, 0f), Vector2.UnitY, Vector2.Zero)),
            "ConfigFile must reject non-finite transform components before mutation.");
        Require(config.GetValue(transformKey) == scalar,
            "Failed transform serialization must preserve the prior configuration token.");

        config.Parse("[geometry]\ntransform={\"X\":{\"X\":1,\"Y\":2},\"Y\":{\"X\":3,\"Y\":4}}\n");
        Expect<InvalidDataException>(() => config.GetValue(transformKey),
            "ConfigFile must reject a transform with a missing field.");
        config.Parse("[geometry]\ntransform={\"X\":{\"X\":1,\"Y\":2},\"Y\":{\"X\":3,\"Y\":4},\"Origin\":{\"X\":5,\"Y\":6},\"Extra\":0}\n");
        Expect<InvalidDataException>(() => config.GetValue(transformKey),
            "ConfigFile must reject unknown transform fields.");
        config.Parse("[geometry]\ntransform={\"X\":{\"X\":1,\"X\":2,\"Y\":2},\"Y\":{\"X\":3,\"Y\":4},\"Origin\":{\"X\":5,\"Y\":6}}\n");
        Expect<InvalidDataException>(() => config.GetValue(transformKey),
            "ConfigFile must reject duplicate transform vector components.");
        config.Parse("[geometry]\ntransform={\"X\":{\"X\":1,\"Y\":2},\"X\":{\"X\":1,\"Y\":2},\"Y\":{\"X\":3,\"Y\":4},\"Origin\":{\"X\":5,\"Y\":6}}\n");
        Expect<InvalidDataException>(() => config.GetValue(transformKey),
            "ConfigFile must reject duplicate transform fields.");
        config.Parse("[geometry]\ntransform={\"X\":{\"X\":1},\"Y\":{\"X\":3,\"Y\":4},\"Origin\":{\"X\":5,\"Y\":6}}\n");
        Expect<InvalidDataException>(() => config.GetValue(transformKey),
            "ConfigFile must reject incomplete transform vectors.");
        config.Parse("[geometry]\ntransform={\"X\":{\"X\":\"right\",\"Y\":2},\"Y\":{\"X\":3,\"Y\":4},\"Origin\":{\"X\":5,\"Y\":6}}\n");
        Expect<InvalidDataException>(() => config.GetValue(transformKey),
            "ConfigFile must reject nonnumeric transform vector components.");
        config.Parse("[geometry]\ntransform={\"X\":{\"X\":1,\"Y\":2,\"Z\":3},\"Y\":{\"X\":3,\"Y\":4},\"Origin\":{\"X\":5,\"Y\":6}}\n");
        Expect<InvalidDataException>(() => config.GetValue(transformKey),
            "ConfigFile must reject unknown transform vector components.");
        config.Parse("[geometry]\ntransform={\"X\":{\"X\":1e100,\"Y\":2},\"Y\":{\"X\":3,\"Y\":4},\"Origin\":{\"X\":5,\"Y\":6}}\n");
        Expect<InvalidDataException>(() => config.GetValue(transformKey),
            "ConfigFile must reject transform numbers outside the finite single-precision range.");
    }

    using (var scene = new PackedScene())
    {
        var source = new ColorPackedNode { Name = "TransformRoot", PackedTransform = affine };
        scene.Pack(source);
        source.Dispose();
        using var instance = (ColorPackedNode)scene.Instantiate();
        Require(instance.PackedTransform == affine,
            "PackedScene must preserve stored Transform2D properties.");
    }

    _ = ExerciseTransformHotPath(32);
    var beforeAllocations = GC.GetAllocatedBytesForCurrentThread();
    var hotResult = ExerciseTransformHotPath(10_000);
    var allocated = GC.GetAllocatedBytesForCurrentThread() - beforeAllocations;
    Require(allocated == 0 && hotResult.IsFinite(),
        "Warmed transform math operations must not allocate managed memory.");
}

static Transform2D ExerciseTransformHotPath(int iterations)
{
    var value = new Transform2D(0.1f, new Vector2(1.2f, 0.8f), 0.05f, new Vector2(2f, 3f));
    for (var index = 0; index < iterations; index++)
    {
        value = value.RotatedLocal(0.00001f).TranslatedLocal(new Vector2(0.00001f, -0.00001f));
        value = value.AffineInverse().AffineInverse();
    }

    return value;
}

static bool TransformNearlyEqual(Transform2D left, Transform2D right, float epsilon = 0.0001f) =>
    VectorNearlyEqual(left.X, right.X, epsilon) && VectorNearlyEqual(left.Y, right.Y, epsilon) &&
    VectorNearlyEqual(left.Origin, right.Origin, epsilon);

static void VerifyConfigFiles()
{
    var rootKey = new ConfigKey<int>(string.Empty, "root");
    var answerKey = new ConfigKey<int>("gameplay", "answer");
    var enabledKey = new ConfigKey<bool>("gameplay", "enabled");
    var nameKey = new ConfigKey<string>("display settings", "player=name");
    var positionKey = new ConfigKey<Vector2>("gameplay", "position");
    var numbersKey = new ConfigKey<List<int>>("gameplay", "numbers");
    var profileKey = new ConfigKey<ConfigProfile>("gameplay", "profile");
    var nullableKey = new ConfigKey<string>("gameplay", "nullable");
    var missingKey = new ConfigKey<int>("missing", "value");

    Expect<ArgumentNullException>(() => new ConfigKey<int>(null!, "key"),
        "Configuration key sections must reject null.");
    Expect<ArgumentNullException>(() => new ConfigKey<int>("section", null!),
        "Configuration key names must reject null.");
    Expect<ArgumentException>(() => new ConfigKey<int>("section", string.Empty),
        "Configuration key names must reject empty values.");
    Expect<NotSupportedException>(() => new ConfigKey<object>("section", "untyped"),
        "Configuration keys must reject object as an implicit universal value.");
    Expect<NotSupportedException>(() => new ConfigKey<List<object>>("section", "nested-untyped"),
        "Configuration keys must reject universal values nested in typed containers.");
    Expect<NotSupportedException>(() => new ConfigKey<TestObject>("section", "engine_object"),
        "Configuration keys must reject engine objects.");
    Require(rootKey.ToString() == "root" && answerKey.ToString() == "gameplay/answer",
        "Configuration keys must expose stable diagnostic paths.");

    using var config = new ConfigFile();
    Require(config.GetSections().Count == 0 && config.EncodeToText().Length == 0,
        "A new configuration must be empty.");
    Expect<ArgumentNullException>(() => config.Parse(null!),
        "Configuration parsing must reject null input.");
    Expect<ArgumentNullException>(() => config.HasSection(null!),
        "Section lookup must reject null names.");
    Expect<ArgumentNullException>(() => config.GetValue<int>(null!),
        "Typed configuration lookup must reject a null key.");
    Expect<ArgumentNullException>(() => config.Save(null!),
        "Configuration saving must reject a null path.");
    Expect<ArgumentException>(() => config.Save(string.Empty),
        "Configuration saving must reject an empty path.");
    Expect<ArgumentNullException>(() => config.SaveEncryptedPass("unused", null!),
        "Password encryption must reject a null password before file access.");
    Require(!config.HasSection("gameplay") && !config.HasSectionKey(answerKey) &&
            !config.TryGetValue(answerKey, out _) && config.GetValue(answerKey, 41) == 41,
        "Missing configuration entries must support lookup, try-get, and fallback semantics.");
    Expect<KeyNotFoundException>(() => config.GetValue(answerKey),
        "Required lookup must reject a missing configuration entry.");
    Expect<KeyNotFoundException>(() => config.GetSectionKeys("missing"),
        "Key enumeration must reject a missing section.");
    Expect<KeyNotFoundException>(() => config.EraseSection("missing"),
        "Section removal must reject a missing section.");
    Expect<KeyNotFoundException>(() => config.EraseSectionKey(missingKey),
        "Entry removal must reject a missing key.");

    var numbers = new List<int> { 1, 2, 3 };
    config.SetValue(answerKey, 42);
    config.SetValue(enabledKey, true);
    config.SetValue(nameKey, "Ada\nLovelace");
    config.SetValue(positionKey, new Vector2(1.5f, -2.25f));
    config.SetValue(numbersKey, numbers);
    config.SetValue(profileKey, new ConfigProfile("pilot", 3));
    config.SetValue(rootKey, 7);
    numbers.Add(4);

    var loadedNumbers = config.GetValue(numbersKey);
    loadedNumbers.Add(99);
    Require(config.GetValue(answerKey) == 42 && config.GetValue(enabledKey) &&
            config.GetValue(nameKey) == "Ada\nLovelace" &&
            VectorNearlyEqual(config.GetValue(positionKey), new Vector2(1.5f, -2.25f)) &&
            config.GetValue(numbersKey).SequenceEqual([1, 2, 3]) &&
            config.GetValue(profileKey) == new ConfigProfile("pilot", 3),
        "Typed configuration values must round-trip and remain independent serialized snapshots.");
    Require(config.TryGetValue(answerKey, out var answer) && answer == 42 &&
            config.GetSections().SequenceEqual([string.Empty, "gameplay", "display settings"]) &&
            config.GetSectionKeys("gameplay").SequenceEqual(["answer", "enabled", "position", "numbers", "profile"]),
        "Configuration enumeration must preserve insertion order and place sectionless entries first.");

    var wrongTypeKey = new ConfigKey<string>("gameplay", "answer");
    Expect<InvalidDataException>(() => config.GetValue(wrongTypeKey),
        "Configuration lookup must reject a stored token that is incompatible with the typed key.");

    config.SetValue(nullableKey, "present");
    config.SetValue(nullableKey, null);
    config.SetValue(nullableKey, null);
    Require(!config.HasSectionKey(nullableKey),
        "Assigning null must remove an entry idempotently.");
    config.EraseSectionKey(enabledKey);
    Require(!config.HasSectionKey(enabledKey) && config.HasSection("gameplay"),
        "Removing one entry must preserve a nonempty section.");

    var cyclicKey = new ConfigKey<CyclicConfigValue>("gameplay", "cycle");
    var cyclic = new CyclicConfigValue();
    cyclic.Next = cyclic;
    Expect<JsonException>(() => config.SetValue(cyclicKey, cyclic),
        "Configuration assignment must surface unsupported cyclic serialization.");
    Require(!config.HasSectionKey(cyclicKey),
        "Failed serialization must not mutate the document.");

    var beforeFailedParse = config.EncodeToText();
    Expect<FormatException>(() => config.Parse("new_value=1\nnot-an-assignment"),
        "Malformed text must fail parsing.");
    Require(config.EncodeToText() == beforeFailedParse && !config.HasSectionKey(new ConfigKey<int>(string.Empty, "new_value")),
        "Parsing must be transactional when a later line is malformed.");

    var removedByParseKey = new ConfigKey<string>("network settings", "remove_me");
    config.SetValue(removedByParseKey, "present");
    config.Parse(
        "\uFEFF; ignored comment\n" +
        "sectionless=11\n" +
        "[\"network settings\"]\n" +
        "\"host=name\"=\"localhost\"\n" +
        "port=7777\n" +
        "remove_me=null\n");
    var sectionlessKey = new ConfigKey<int>(string.Empty, "sectionless");
    var hostKey = new ConfigKey<string>("network settings", "host=name");
    var portKey = new ConfigKey<int>("network settings", "port");
    Require(config.GetValue(sectionlessKey) == 11 && config.GetValue(hostKey) == "localhost" &&
            config.GetValue(portKey) == 7777 && !config.HasSectionKey(removedByParseKey) &&
            config.GetValue(answerKey) == 42,
        "Parsing must support comments, a BOM, quoted identifiers, sectionless entries, null removal, and merge semantics.");

    var escapedSectionKey = new ConfigKey<string>("line\nbreak", "key\nname");
    config.SetValue(escapedSectionKey, "value\nwith\nlines");
    var encoded = config.EncodeToText();
    using (var decoded = new ConfigFile())
    {
        decoded.Parse(encoded);
        Require(decoded.GetValue(escapedSectionKey) == "value\nwith\nlines" && decoded.EncodeToText() == encoded,
            "Encoding must quote unsafe identifiers and produce a stable parseable document.");
    }

    config.EraseSection("network settings");
    Require(!config.HasSection("network settings"),
        "Section removal must remove all of its entries.");

    config.Clear();
    Parallel.For(0, 128, index =>
    {
        var key = new ConfigKey<int>("parallel", $"key-{index}");
        config.SetValue(key, index);
        Require(config.GetValue(key) == index, "Concurrent typed configuration access must retain assigned values.");
    });
    Require(config.GetSectionKeys("parallel").Count == 128,
        "Concurrent configuration writes must not lose entries.");

    config.Clear();
    var secretKey = new ConfigKey<string>("account", "secret");
    config.SetValue(secretKey, "classified-value");
    config.SetValue(answerKey, 42);

    var directory = Path.Combine(Path.GetTempPath(), $"electron2d-config-{Guid.NewGuid():N}");
    Directory.CreateDirectory(directory);
    try
    {
        var plainPath = Path.Combine(directory, "settings.cfg");
        config.Save(plainPath);
        config.SetValue(answerKey, 43);
        config.Save(plainPath);
        using (var loaded = new ConfigFile())
        {
            loaded.SetValue(rootKey, 9);
            loaded.Load(plainPath);
            Require(loaded.GetValue(secretKey) == "classified-value" && loaded.GetValue(answerKey) == 43 &&
                    loaded.GetValue(rootKey) == 9,
                "Plain saves must replace an existing destination, and loading must merge without clearing unrelated entries.");
        }

        config.SetValue(answerKey, 42);
        config.Save(plainPath);

        var savedBytes = File.ReadAllBytes(plainPath);
        Require(!savedBytes.AsSpan().StartsWith(Encoding.UTF8.Preamble) &&
                Encoding.UTF8.GetString(savedBytes) == config.EncodeToText(),
            "Plain saves must use UTF-8 without a BOM and replace the destination with the encoded snapshot.");

        var invalidUtf8Path = Path.Combine(directory, "invalid-utf8.cfg");
        File.WriteAllBytes(invalidUtf8Path, [0xff]);
        var beforeInvalidLoad = config.EncodeToText();
        Expect<DecoderFallbackException>(() => config.Load(invalidUtf8Path),
            "Plain loading must reject invalid UTF-8.");
        Require(config.EncodeToText() == beforeInvalidLoad,
            "A failed plain load must preserve existing state.");

        Expect<DirectoryNotFoundException>(() => config.Save(Path.Combine(directory, "missing", "settings.cfg")),
            "Saving must not create an absent destination directory implicitly.");
        var directoryTarget = Path.Combine(directory, "directory-target");
        Directory.CreateDirectory(directoryTarget);
        Expect<IOException>(() => config.Save(directoryTarget),
            "Atomic saving must surface a destination that cannot be replaced by a file.");
        Require(!Directory.EnumerateFiles(directory, "*.tmp", SearchOption.TopDirectoryOnly).Any(),
            "A failed atomic replacement must clean up its temporary file.");

        var rawKey = Enumerable.Range(0, 32).Select(value => (byte)value).ToArray();
        var rawEncryptedPath = Path.Combine(directory, "settings-key.bin");
        config.SaveEncrypted(rawEncryptedPath, rawKey);
        var encryptedBytes = File.ReadAllBytes(rawEncryptedPath);
        Require(encryptedBytes.AsSpan().IndexOf(Encoding.UTF8.GetBytes("classified-value")) < 0,
            "Encrypted saves must not expose plaintext values.");

        using (var loaded = new ConfigFile())
        {
            loaded.LoadEncrypted(rawEncryptedPath, rawKey);
            Require(loaded.GetValue(secretKey) == "classified-value" && loaded.GetValue(answerKey) == 42,
                "Raw-key encrypted configurations must authenticate, decrypt, parse, and merge.");
        }

        var wrongRawKey = Enumerable.Repeat((byte)0xff, 32).ToArray();
        using (var wrongKeyConfig = new ConfigFile())
        {
            Expect<CryptographicException>(() => wrongKeyConfig.LoadEncrypted(rawEncryptedPath, wrongRawKey),
                "Raw-key loading must reject a wrong key.");
            Require(wrongKeyConfig.GetSections().Count == 0,
                "Authentication failure must not mutate the destination configuration.");
        }

        var tamperedPath = Path.Combine(directory, "settings-tampered.bin");
        encryptedBytes[^1] ^= 0x01;
        File.WriteAllBytes(tamperedPath, encryptedBytes);
        using (var tampered = new ConfigFile())
        {
            Expect<CryptographicException>(() => tampered.LoadEncrypted(tamperedPath, rawKey),
                "Authenticated loading must reject modified ciphertext.");
        }

        Expect<ArgumentException>(() => config.SaveEncrypted(rawEncryptedPath, new byte[31]),
            "Raw-key encryption must require exactly 256 key bits.");

        var passwordPath = Path.Combine(directory, "settings-password.bin");
        var secondPasswordPath = Path.Combine(directory, "settings-password-2.bin");
        config.SaveEncryptedPass(passwordPath, "correct horse battery staple");
        config.SaveEncryptedPass(secondPasswordPath, "correct horse battery staple");
        Require(!File.ReadAllBytes(passwordPath).SequenceEqual(File.ReadAllBytes(secondPasswordPath)),
            "Password encryption must generate fresh salt and nonce material for every save.");

        using (var loaded = new ConfigFile())
        {
            loaded.LoadEncryptedPass(passwordPath, "correct horse battery staple");
            Require(loaded.GetValue(secretKey) == "classified-value",
                "Password-encrypted configurations must derive, authenticate, decrypt, and parse successfully.");
        }

        using (var wrongPassword = new ConfigFile())
        {
            Expect<CryptographicException>(() => wrongPassword.LoadEncryptedPass(passwordPath, "wrong password"),
                "Password-encrypted loading must reject a wrong password.");
            Expect<InvalidDataException>(() => wrongPassword.LoadEncrypted(passwordPath, rawKey),
                "Raw-key loading must reject a password-mode envelope.");
            Expect<InvalidDataException>(() => wrongPassword.LoadEncryptedPass(rawEncryptedPath, "password"),
                "Password loading must reject a raw-key envelope.");
        }

        Expect<ArgumentException>(() => config.SaveEncryptedPass(passwordPath, string.Empty),
            "Password encryption must reject an empty password.");

        var malformedEnvelopePath = Path.Combine(directory, "malformed.bin");
        File.WriteAllBytes(malformedEnvelopePath, [1, 2, 3]);
        using (var malformed = new ConfigFile())
        {
            Expect<InvalidDataException>(() => malformed.LoadEncrypted(malformedEnvelopePath, rawKey),
                "Encrypted loading must reject a malformed envelope before mutation.");
        }

        Require(!Directory.EnumerateFiles(directory, "*.tmp", SearchOption.TopDirectoryOnly).Any(),
            "Successful and failed atomic saves must not leave temporary files.");
    }
    finally
    {
        Directory.Delete(directory, recursive: true);
    }

    var concurrentDispose = new ConfigFile();
    var concurrentDisposeKey = new ConfigKey<int>("state", "value");
    Exception? concurrentDisposeError = null;
    using (var start = new Barrier(2))
    {
        Parallel.Invoke(
            () =>
            {
                start.SignalAndWait();
                for (var index = 0; index < 10_000; index++)
                {
                    try
                    {
                        concurrentDispose.SetValue(concurrentDisposeKey, index);
                        _ = concurrentDispose.GetValue(concurrentDisposeKey);
                    }
                    catch (ObjectDisposedException)
                    {
                        return;
                    }
                    catch (Exception error)
                    {
                        Interlocked.CompareExchange(ref concurrentDisposeError, error, null);
                        return;
                    }
                }
            },
            () =>
            {
                start.SignalAndWait();
                concurrentDispose.Dispose();
            });
    }

    Require(concurrentDispose.IsDisposed && concurrentDisposeError is null,
        "Concurrent configuration access and disposal must terminate with only the documented disposal rejection.");

    config.Dispose();
    Expect<ObjectDisposedException>(config.Clear, "Disposed configurations must reject mutation.");
    Expect<ObjectDisposedException>(() => config.HasSection("section"),
        "Disposed configurations must reject reads.");
    Expect<ObjectDisposedException>(() => config.Save("unused.cfg"),
        "Disposed configurations must reject file operations before touching the path.");
}

static void VerifyFileAccess()
{
    Expect<ArgumentNullException>(() => EngineFileAccess.Open(null!, FileAccessMode.Read),
        "File access must reject null paths.");
    Expect<ArgumentException>(() => EngineFileAccess.Open(string.Empty, FileAccessMode.Read),
        "File access must reject empty paths.");
    Expect<ArgumentOutOfRangeException>(() => EngineFileAccess.Open("unused", (FileAccessMode)5),
        "File access must reject unknown mode combinations.");
    Expect<NotSupportedException>(() => EngineFileAccess.FileExists("uid://123"),
        "Resource-identity paths must remain explicit until their resolver exists.");
    Expect<NotSupportedException>(() => EngineFileAccess.FileExists("pipe://channel"),
        "Pipe paths must remain explicit until their platform backend exists.");
    Expect<ArgumentException>(() => EngineFileAccess.CreateTemp(prefix: "bad/name"),
        "Temporary-file prefixes must reject directory separators.");
    Expect<ArgumentException>(() => EngineFileAccess.CreateTemp(extension: "bad/name"),
        "Temporary-file extensions must reject directory separators.");

    var directory = Path.Combine(Path.GetTempPath(), $"electron2d-file-access-{Guid.NewGuid():N}");
    Directory.CreateDirectory(directory);
    try
    {
        var path = Path.Combine(directory, "data.bin");
        Expect<FileNotFoundException>(() => EngineFileAccess.Open(path, FileAccessMode.Read),
            "Read mode must require an existing file.");
        Expect<FileNotFoundException>(() => EngineFileAccess.GetSize(path),
            "Static size lookup must reject a missing file.");
        Expect<FileNotFoundException>(() => EngineFileAccess.GetAccessTime(path),
            "Static access-time lookup must reject a missing file.");
        Expect<FileNotFoundException>(() => EngineFileAccess.GetModifiedTime(path),
            "Static modification-time lookup must reject a missing file.");
        Expect<DirectoryNotFoundException>(() => EngineFileAccess.Open(
                Path.Combine(directory, "missing", "data.bin"), FileAccessMode.Write),
            "Write mode must not create missing directories.");

        using (var file = EngineFileAccess.Open(path, FileAccessMode.WriteRead))
        {
            Require(file.IsOpen && file.Path == path && file.AbsolutePath == Path.GetFullPath(path) &&
                    file.Position == 0 && file.Length == 0 && !file.EofReached && !file.BigEndian,
                "A newly truncated file must expose its identity, cursor, length, and byte order.");

            file.WriteByte(0x7f);
            file.WriteUInt16(0x1234);
            file.WriteUInt32(0x12345678);
            file.WriteUInt64(0x0123456789abcdef);
            file.WriteHalf((Half)1.5f);
            file.WriteSingle(-2.25f);
            file.WriteDouble(Math.PI);
            file.WriteReal((float)-Math.E);
            file.BigEndian = true;
            file.WriteUInt16(0xabcd);
            file.WriteUInt32(0x89abcdef);
            file.WriteUInt64(0xfedcba9876543210);
            file.WriteHalf((Half)(-0.5f));
            file.WriteSingle(123.25f);
            file.WriteDouble(-456.5);
            file.WritePascalString("строка");
            file.WriteLine("line\r");
            file.WriteCsvLine(["plain", "with,delimiter", "quote\"value", "two\nlines"]);
            var length = file.Length;
            Require(length > 0 && file.Position == length,
                "Writes must advance the cursor and grow the file.");
            file.Flush();

            file.Seek(0);
            file.BigEndian = false;
            Require(file.ReadByte() == 0x7f && file.ReadUInt16() == 0x1234 &&
                    file.ReadUInt32() == 0x12345678 && file.ReadUInt64() == 0x0123456789abcdef &&
                    file.ReadHalf() == (Half)1.5f && file.ReadSingle() == -2.25f &&
                    file.ReadDouble() == Math.PI && file.ReadReal() == (float)-Math.E,
                "Little-endian numeric values must round-trip exactly.");
            file.BigEndian = true;
            Require(file.ReadUInt16() == 0xabcd && file.ReadUInt32() == 0x89abcdef &&
                    file.ReadUInt64() == 0xfedcba9876543210 && file.ReadHalf() == (Half)(-0.5f) &&
                    file.ReadSingle() == 123.25f && file.ReadDouble() == -456.5,
                "Big-endian numeric values must round-trip exactly.");
            Require(file.ReadPascalString() == "строка" && file.ReadLine() == "line" &&
                    file.ReadCsvLine().SequenceEqual(["plain", "with,delimiter", "quote\"value", "two\nlines"]),
                "Length-prefixed strings, CRLF handling, and quoted multiline CSV fields must round-trip.");
            Require(!file.EofReached && file.Position == file.Length,
                "Reaching the exact end must not mark EOF before another read is attempted.");
            Expect<EndOfStreamException>(() => file.ReadByte(),
                "A scalar read beyond the end must fail.");
            Require(file.EofReached, "An incomplete read must mark EOF.");
            file.SeekEnd(-1);
            Require(!file.EofReached && file.Position == file.Length - 1,
                "A successful seek from the end must clear EOF.");
            var beforeText = file.Position;
            Expect<DecoderFallbackException>(() => file.ReadAllText(),
                "Whole-text reading must reject binary data that is not valid UTF-8.");
            Require(file.Position == beforeText,
                "Whole-text reading must preserve the cursor when decoding fails.");
            file.Resize(length + 3);
            file.SeekEnd(-3);
            Require(file.ReadBytes(3).SequenceEqual(new byte[] { 0, 0, 0 }),
                "Extending a file must fill the new region with zero bytes.");
            file.Resize(length);
        }

        using (var writeOnly = EngineFileAccess.Open(path, FileAccessMode.Write))
        {
            Require(writeOnly.Length == 0, "Write mode must truncate an existing file.");
            Expect<InvalidOperationException>(() => writeOnly.ReadByte(),
                "Write-only files must reject reads.");
            writeOnly.WriteString("abcdef");
        }
        using (var readOnly = EngineFileAccess.Open(path, FileAccessMode.Read))
        {
            Expect<InvalidOperationException>(() => readOnly.WriteByte(1),
                "Read-only files must reject writes.");
            Require(readOnly.ReadString(6) == "abcdef", "Raw UTF-8 strings must round-trip.");
            Require(readOnly.ReadBytes(1).Length == 0 && readOnly.EofReached,
                "A short buffer read must return available bytes and mark EOF.");
            readOnly.Close();
            readOnly.Close();
            Require(!readOnly.IsOpen, "Closing must be idempotent and publish the closed state.");
            Expect<InvalidOperationException>(() => readOnly.Seek(0),
                "Closed files must reject cursor operations.");
        }

        using (var readWrite = EngineFileAccess.Open(path, FileAccessMode.ReadWrite))
        {
            readWrite.WriteString("XY");
        }
        Require(EngineFileAccess.GetFileAsString(path) == "XYcdef",
            "Read-write mode must preserve length and overwrite from the beginning.");
        Require(EngineFileAccess.FileExists(path) && EngineFileAccess.GetSize(path) == 6 &&
                EngineFileAccess.GetFileAsBytes(path).SequenceEqual(Encoding.UTF8.GetBytes("XYcdef")) &&
                EngineFileAccess.GetAccessTime(path) > 0 && EngineFileAccess.GetModifiedTime(path) > 0,
            "Static file inspection must resolve contents, size, and timestamps.");
        Require(EngineFileAccess.GetMd5(path) == Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes("XYcdef"))).ToLowerInvariant() &&
                EngineFileAccess.GetSha256(path) == Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("XYcdef"))).ToLowerInvariant(),
            "Static digest helpers must return lowercase MD5 and SHA-256 values.");

        var linePath = Path.Combine(directory, "lines.txt");
        File.WriteAllBytes(linePath, Encoding.UTF8.GetBytes("first\rsecond\0third\n"));
        using (var lines = EngineFileAccess.Open(linePath, FileAccessMode.Read))
        {
            Require(lines.ReadLine() == "first" && lines.ReadLine() == "second" && lines.ReadLine() == "third",
                "Line reads must recognize CR, null, and LF terminators.");
            var lineCursor = lines.Position;
            Require(lines.ReadAllText(skipCarriageReturns: true) == "firstsecond\0third\n" &&
                    lines.Position == lineCursor,
                "Whole-text reads must optionally remove CR bytes without changing the cursor.");
        }

        if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD())
        {
            var originalReadOnly = EngineFileAccess.IsReadOnly(path);
            EngineFileAccess.SetReadOnly(path, !originalReadOnly);
            Require(EngineFileAccess.IsReadOnly(path) != originalReadOnly,
                "The read-only attribute must be mutable on supported platforms.");
            EngineFileAccess.SetReadOnly(path, originalReadOnly);
        }
        else
        {
            Expect<PlatformNotSupportedException>(() => EngineFileAccess.IsReadOnly(path),
                "Unsupported platforms must not simulate a read-only file attribute.");
            Expect<PlatformNotSupportedException>(() => EngineFileAccess.IsHidden(path),
                "Unsupported platforms must not simulate a hidden file attribute.");
        }
        if (!OperatingSystem.IsWindows())
        {
            var originalPermissions = EngineFileAccess.GetUnixPermissions(path);
            var restrictedPermissions = UnixPermissionFlags.ReadOwner | UnixPermissionFlags.WriteOwner;
            EngineFileAccess.SetUnixPermissions(path, restrictedPermissions);
            Require(EngineFileAccess.GetUnixPermissions(path) == restrictedPermissions,
                "Unix permission bits must round-trip exactly.");
            EngineFileAccess.SetUnixPermissions(path, originalPermissions);
        }

        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsWindows())
        {
            using (var attributes = EngineFileAccess.Open(path, FileAccessMode.ReadWrite))
            {
                if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD())
                    Require(attributes.ReadOnly == EngineFileAccess.IsReadOnly(path) &&
                            attributes.Hidden == EngineFileAccess.IsHidden(path),
                        "Opened files must expose the current physical attribute state on supported platforms.");
                if (!OperatingSystem.IsWindows())
                    Require(attributes.UnixPermissions == EngineFileAccess.GetUnixPermissions(path),
                        "Opened files must expose current Unix permission bits.");
            }

            const string attributeName = "electron2d.test";
            EngineFileAccess.SetExtendedAttributeString(path, attributeName, "значение");
            Require(EngineFileAccess.GetExtendedAttributeString(path, attributeName) == "значение" &&
                    EngineFileAccess.GetExtendedAttributesList(path).Contains(attributeName, StringComparer.Ordinal),
                "Extended attributes must support text values and enumeration without platform namespace syntax.");
            EngineFileAccess.SetExtendedAttribute(path, attributeName, [0, 1, 2, 255]);
            Require(EngineFileAccess.GetExtendedAttribute(path, attributeName).SequenceEqual(new byte[] { 0, 1, 2, 255 }),
                "Extended attributes must preserve arbitrary bytes.");
            EngineFileAccess.RemoveExtendedAttribute(path, attributeName);
            Require(!EngineFileAccess.GetExtendedAttributesList(path).Contains(attributeName, StringComparer.Ordinal),
                "Extended attributes must be removable.");
            Expect<IOException>(() => EngineFileAccess.GetExtendedAttribute(path, attributeName),
                "Reading a removed extended attribute must fail explicitly.");
        }

        string temporaryPath;
        using (var readOnlyTemporary = EngineFileAccess.CreateTemp(
                   FileAccessMode.Read, prefix: "electron2d", extension: ".dat"))
        {
            temporaryPath = readOnlyTemporary.AbsolutePath;
            Require(temporaryPath.EndsWith(".dat", StringComparison.Ordinal) && readOnlyTemporary.Length == 0,
                "Temporary files must support read-only mode, prefixes, and normalized extensions.");
        }
        Require(!File.Exists(temporaryPath), "Read-only temporary files must be deleted on close.");

        var temporary = EngineFileAccess.CreateTemp();
        temporaryPath = temporary.AbsolutePath;
        temporary.WriteString("temporary");
        temporary.Close();
        Require(!File.Exists(temporaryPath), "Temporary files must be deleted on close by default.");
        using (var keptTemporary = EngineFileAccess.CreateTemp(keep: true))
        {
            temporaryPath = keptTemporary.AbsolutePath;
            keptTemporary.WriteString("kept");
        }
        Require(File.Exists(temporaryPath), "Kept temporary files must survive disposal.");
        File.Delete(temporaryPath);

        foreach (var compression in new[]
                 {
                     FileCompressionMode.Deflate,
                     FileCompressionMode.Gzip,
                     FileCompressionMode.Brotli
                 })
        {
            var compressedPath = Path.Combine(directory, $"compressed-{compression}.bin");
            using (var compressed = EngineFileAccess.OpenCompressed(compressedPath, FileAccessMode.WriteRead, compression))
            {
                compressed.WriteLine("compressible compressible compressible");
                compressed.WriteUInt32(123456789);
                compressed.Flush();
                compressed.Seek(0);
                Require(compressed.ReadLine() == "compressible compressible compressible" &&
                        compressed.ReadUInt32() == 123456789,
                    $"{compression} data must remain readable after an intermediate commit.");
            }
            using var decoded = EngineFileAccess.OpenCompressed(compressedPath, FileAccessMode.Read, compression);
            Require(decoded.ReadLine() == "compressible compressible compressible" &&
                    decoded.ReadUInt32() == 123456789,
                $"{compression} containers must round-trip across instances.");
        }
        Expect<InvalidDataException>(() => EngineFileAccess.OpenCompressed(
                Path.Combine(directory, "compressed-Deflate.bin"), FileAccessMode.Read, FileCompressionMode.Gzip),
            "Compressed access must reject a container opened with the wrong codec.");
        Expect<NotSupportedException>(() => EngineFileAccess.OpenCompressed(
                Path.Combine(directory, "fastlz.bin"), FileAccessMode.Write, FileCompressionMode.FastLz),
            "Unavailable FastLZ support must fail explicitly.");
        Expect<NotSupportedException>(() => EngineFileAccess.OpenCompressed(
                Path.Combine(directory, "zstd.bin"), FileAccessMode.Write, FileCompressionMode.Zstandard),
            "Unavailable Zstandard support must fail explicitly.");
        var corruptCompressedPath = Path.Combine(directory, "corrupt-compressed.bin");
        File.WriteAllBytes(corruptCompressedPath, [1, 2, 3]);
        Expect<InvalidDataException>(() => EngineFileAccess.OpenCompressed(
                corruptCompressedPath, FileAccessMode.Read, FileCompressionMode.Deflate),
            "Malformed compressed envelopes must be rejected before exposure.");

        var encryptionKey = Enumerable.Range(0, 32).Select(value => (byte)value).ToArray();
        var encryptedPath = Path.Combine(directory, "encrypted.bin");
        using (var encrypted = EngineFileAccess.OpenEncrypted(encryptedPath, FileAccessMode.WriteRead, encryptionKey))
        {
            encrypted.WriteLine("secret text");
            encrypted.WriteUInt64(ulong.MaxValue);
            encrypted.Flush();
            encrypted.Seek(0);
            Require(encrypted.ReadLine() == "secret text" && encrypted.ReadUInt64() == ulong.MaxValue,
                "Raw-key encrypted data must remain readable after an intermediate authenticated commit.");
        }
        Require(File.ReadAllBytes(encryptedPath).AsSpan().IndexOf(Encoding.UTF8.GetBytes("secret text")) < 0,
            "Encrypted files must not contain plaintext payloads.");
        using (var encrypted = EngineFileAccess.OpenEncrypted(encryptedPath, FileAccessMode.Read, encryptionKey))
            Require(encrypted.ReadLine() == "secret text" && encrypted.ReadUInt64() == ulong.MaxValue,
                "Raw-key encrypted files must authenticate and round-trip.");
        Expect<ArgumentException>(() => EngineFileAccess.OpenEncrypted(encryptedPath, FileAccessMode.Read, new byte[31]),
            "Raw-key encryption must require exactly 256 key bits.");
        Expect<CryptographicException>(() => EngineFileAccess.OpenEncrypted(
                encryptedPath, FileAccessMode.Read, Enumerable.Repeat((byte)0xff, 32).ToArray()),
            "Encrypted files must reject a wrong raw key.");
        var tamperedEncryptedPath = Path.Combine(directory, "tampered-encrypted.bin");
        var tampered = File.ReadAllBytes(encryptedPath);
        tampered[^1] ^= 1;
        File.WriteAllBytes(tamperedEncryptedPath, tampered);
        Expect<CryptographicException>(() => EngineFileAccess.OpenEncrypted(
                tamperedEncryptedPath, FileAccessMode.Read, encryptionKey),
            "Encrypted files must reject modified ciphertext.");

        var passwordPath = Path.Combine(directory, "password.bin");
        using (var encrypted = EngineFileAccess.OpenEncryptedWithPassword(
                   passwordPath, FileAccessMode.Write, "correct horse battery staple"))
            encrypted.WriteString("password secret");
        using (var encrypted = EngineFileAccess.OpenEncryptedWithPassword(
                   passwordPath, FileAccessMode.Read, "correct horse battery staple"))
            Require(encrypted.ReadAllText() == "password secret",
                "Password-derived encrypted files must authenticate and round-trip.");
        Expect<InvalidDataException>(() => EngineFileAccess.OpenEncrypted(passwordPath, FileAccessMode.Read, encryptionKey),
            "Encrypted access must reject raw-key/password mode confusion.");
        Expect<CryptographicException>(() => EngineFileAccess.OpenEncryptedWithPassword(
                passwordPath, FileAccessMode.Read, "wrong password"),
            "Password-derived encrypted files must reject a wrong password.");
        Expect<ArgumentException>(() => EngineFileAccess.OpenEncryptedWithPassword(
                passwordPath, FileAccessMode.Read, string.Empty),
            "Password encryption must reject an empty password.");

        var virtualName = $"electron2d-file-access-{Guid.NewGuid():N}.tmp";
        var virtualPath = $"res://{virtualName}";
        var physicalVirtualPath = Path.Combine(ProjectSettings.Instance.ProjectRoot, virtualName);
        try
        {
            using (var virtualFile = EngineFileAccess.Open(virtualPath, FileAccessMode.Write))
                virtualFile.WriteString("virtual");
            Require(File.ReadAllText(physicalVirtualPath) == "virtual" && EngineFileAccess.FileExists(virtualPath),
                "Directory-backed project paths must resolve for instance and static access.");
        }
        finally
        {
            File.Delete(physicalVirtualPath);
        }

        Parallel.For(0, 32, _ =>
        {
            Require(EngineFileAccess.GetSha256(path).Length == 64,
                "Independent static hash operations must be safe concurrently.");
        });

        var concurrentPath = Path.Combine(directory, "concurrent.bin");
        using (var concurrent = EngineFileAccess.Open(concurrentPath, FileAccessMode.WriteRead))
        {
            Parallel.For(0, 128, index => concurrent.WritePascalString(index.ToString("D3", CultureInfo.InvariantCulture)));
            Require(concurrent.Length == 128 * 7,
                "Concurrent Pascal writes must keep each prefix and payload in one serialized operation.");
            concurrent.Seek(0);
            var values = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < 128; index++)
                values.Add(concurrent.ReadPascalString());
            Require(values.SetEquals(Enumerable.Range(0, 128).Select(index => index.ToString("D3", CultureInfo.InvariantCulture))),
                "Concurrent compound writes must remain independently readable without interleaving.");
        }

        var failedPath = Path.Combine(directory, "absent", "compressed.bin");
        var failedCommit = EngineFileAccess.OpenCompressed(failedPath, FileAccessMode.Write, FileCompressionMode.Deflate);
        failedCommit.WriteString("pending");
        Expect<DirectoryNotFoundException>(failedCommit.Close,
            "A transformed commit must surface a missing destination directory.");
        Require(!failedCommit.IsOpen,
            "A failed close must still release the transformed stream.");
        failedCommit.Dispose();

        var disposed = EngineFileAccess.Open(path, FileAccessMode.Read);
        disposed.Dispose();
        Expect<ObjectDisposedException>(() => disposed.Close(),
            "Disposed file access must reject public operations.");
    }
    finally
    {
        Directory.Delete(directory, recursive: true);
    }
}

static void VerifyDirAccess()
{
    Expect<ArgumentNullException>(() => DirAccess.Open(null!),
        "Directory opening must reject null paths.");
    Expect<ArgumentException>(() => DirAccess.Open(string.Empty),
        "Directory opening must reject empty paths.");
    Expect<DirectoryNotFoundException>(() => DirAccess.Open(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))),
        "Directory opening must reject missing directories.");
    Expect<NotSupportedException>(() => DirAccess.Open("uid://missing"),
        "Directory opening must reject unknown virtual schemes.");
    Expect<ArgumentException>(() => DirAccess.GetFilesAt("relative"),
        "Static directory operations must reject relative paths.");
    Expect<ArgumentException>(() => DirAccess.CreateTemp("../invalid"),
        "Temporary directory prefixes must reject separators.");

    var root = Path.Combine(Path.GetTempPath(), $"electron2d-dir-{Guid.NewGuid():N}");
    Directory.CreateDirectory(root);
    try
    {
        var alphaPath = Path.Combine(root, "alpha.txt");
        var zetaPath = Path.Combine(root, "zeta.txt");
        var hiddenPath = Path.Combine(root, ".hidden.txt");
        File.WriteAllText(zetaPath, "zeta");
        File.WriteAllText(alphaPath, "alpha");
        File.WriteAllText(hiddenPath, "hidden");
        Directory.CreateDirectory(Path.Combine(root, "visible-dir"));
        Directory.CreateDirectory(Path.Combine(root, ".hidden-dir"));

        using (var directory = DirAccess.Open(root))
        {
            Require(!directory.IncludeHidden && !directory.IncludeNavigational &&
                    directory.GetCurrentDir() == Path.GetFullPath(root) &&
                    !directory.CurrentIsDir() && directory.GetNext().Length == 0,
                "An opened directory must expose default filters, its normalized path, and an inactive listing.");
            Require(directory.FileExists("alpha.txt") && directory.DirExists("visible-dir") &&
                    !directory.FileExists("missing") && !directory.DirExists("missing"),
                "Relative file and directory existence must resolve from the current directory.");

            Require(directory.GetFiles().SequenceEqual(["alpha.txt", "zeta.txt"]) &&
                    directory.GetDirectories().SequenceEqual(["visible-dir"]),
                "Sorted snapshots must exclude hidden and navigational entries by default.");
            directory.IncludeHidden = true;
            directory.IncludeNavigational = true;
            Require(directory.GetFiles().SequenceEqual([".hidden.txt", "alpha.txt", "zeta.txt"]) &&
                    directory.GetDirectories().SequenceEqual([".", "..", ".hidden-dir", "visible-dir"]),
                "Snapshot filters must include hidden and navigational entries when enabled.");

            directory.ListDirBegin();
            var streamed = new Dictionary<string, bool>(StringComparer.Ordinal);
            while (true)
            {
                var name = directory.GetNext();
                if (name.Length == 0)
                    break;
                streamed.Add(name, directory.CurrentIsDir());
            }
            Require(streamed.Count == 7 && streamed["."] && streamed[".."] && streamed["visible-dir"] &&
                    streamed[".hidden-dir"] && !streamed["alpha.txt"] && !directory.CurrentIsDir(),
                "Streaming enumeration must return names, preserve entry kinds, and clear state at EOF.");
            directory.ListDirEnd();
            directory.ListDirEnd();

            directory.IncludeHidden = false;
            directory.IncludeNavigational = false;
            directory.ListDirBegin();
            var concurrentNames = new System.Collections.Concurrent.ConcurrentBag<string>();
            Parallel.For(0, 16, _ =>
            {
                while (true)
                {
                    var name = directory.GetNext();
                    if (name.Length == 0)
                        break;
                    concurrentNames.Add(name);
                }
            });
            Require(concurrentNames.Order(StringComparer.Ordinal).SequenceEqual(
                    new[] { "alpha.txt", "visible-dir", "zeta.txt" }.Order(StringComparer.Ordinal)),
                "Concurrent GetNext calls must consume every visible snapshot entry at most once.");

            directory.MakeDir("single");
            Expect<IOException>(() => directory.MakeDir("single"),
                "Single-directory creation must reject an existing target.");
            Expect<DirectoryNotFoundException>(() => directory.MakeDir("missing/child"),
                "Single-directory creation must require an existing parent.");
            directory.MakeDirRecursive("nested/child");
            directory.MakeDirRecursive("nested/child");
            Require(directory.DirExists("single") && directory.DirExists("nested/child"),
                "Recursive directory creation must create missing parents and be idempotent.");

            directory.ChangeDir("nested/child");
            Require(directory.GetCurrentDir() == Path.Combine(root, "nested", "child"),
                "Relative directory changes must update the current native path.");
            directory.ChangeDir("../..");
            Expect<InvalidOperationException>(() => directory.ChangeDir("res://"),
                "A physical accessor must reject a virtual scope switch.");

            directory.Copy("alpha.txt", "copy.txt");
            Require(File.ReadAllText(Path.Combine(root, "copy.txt")) == "alpha",
                "File copying must preserve contents.");
            File.WriteAllText(Path.Combine(root, "copy.txt"), "old");
            directory.Copy("zeta.txt", "copy.txt");
            Require(File.ReadAllText(Path.Combine(root, "copy.txt")) == "zeta",
                "File copying must overwrite an existing destination.");
            if (OperatingSystem.IsWindows())
            {
                Expect<PlatformNotSupportedException>(() =>
                        directory.Copy("alpha.txt", "copy.txt", UnixPermissionFlags.ReadOwner),
                    "Copy permissions must fail explicitly when Unix modes are unavailable.");
            }
            else
            {
                Expect<ArgumentOutOfRangeException>(() =>
                        directory.Copy("alpha.txt", "copy.txt", (UnixPermissionFlags)(1 << 20)),
                    "Copy permissions must reject unknown bits before filesystem mutation.");
            }
            Require(File.ReadAllText(Path.Combine(root, "copy.txt")) == "zeta",
                "Invalid copy permissions must not replace the destination.");
            Expect<ArgumentException>(() => directory.Copy("copy.txt", "./copy.txt"),
                "File copying must reject an equivalent source and destination path.");

            File.WriteAllText(Path.Combine(root, "rename-source.txt"), "new");
            File.WriteAllText(Path.Combine(root, "rename-target.txt"), "old");
            directory.Rename("rename-source.txt", "rename-target.txt");
            Require(!File.Exists(Path.Combine(root, "rename-source.txt")) &&
                    File.ReadAllText(Path.Combine(root, "rename-target.txt")) == "new",
                "File rename must move and overwrite atomically where the filesystem supports it.");
            Directory.CreateDirectory(Path.Combine(root, "rename-dir-source"));
            Directory.CreateDirectory(Path.Combine(root, "rename-dir-target"));
            directory.Rename("rename-dir-source", "rename-dir-target");
            Require(Directory.Exists(Path.Combine(root, "rename-dir-target")),
                "Directory rename must replace an empty destination directory.");
            Directory.CreateDirectory(Path.Combine(root, "rollback-source", "child"));
            Expect<IOException>(() => directory.Rename("rollback-source", "rollback-source/child"),
                "A directory cannot be moved into itself.");
            Require(Directory.Exists(Path.Combine(root, "rollback-source", "child")),
                "A failed directory move must restore an empty destination removed for overwrite.");

            File.WriteAllText(Path.Combine(root, "single", "child.txt"), "child");
            Expect<IOException>(() => directory.Remove("single"),
                "Directory removal must reject a nonempty directory.");
            directory.Remove("single/child.txt");
            directory.Remove("single");
            Expect<FileNotFoundException>(() => directory.Remove("single"),
                "Removal must reject a missing entry instead of silently succeeding.");

            if (OperatingSystem.IsLinux() || OperatingSystem.IsWindows() || OperatingSystem.IsMacOS())
            {
                directory.CreateLink("alpha.txt", "alpha-link");
                Require(directory.IsLink("alpha-link") && directory.ReadLink("alpha-link").Length > 0 &&
                        directory.IsEquivalent("alpha.txt", "alpha-link"),
                    "Symbolic links must expose their target and compare equivalent to it.");
                directory.Remove("alpha-link");
                Require(File.Exists(alphaPath), "Removing a symbolic link must preserve its target.");

                directory.CreateLink("visible-dir", "directory-link");
                Require(directory.GetDirectories().Contains("directory-link", StringComparer.Ordinal),
                    "A link to a directory must be classified as a directory during enumeration.");
                directory.Remove("directory-link");
                Require(Directory.Exists(Path.Combine(root, "visible-dir")),
                    "Removing a directory link must not traverse or delete its target.");

                File.WriteAllText(Path.Combine(root, "dangling-source.txt"), "source");
                directory.CreateLink("dangling-source.txt", "dangling-link");
                File.Delete(Path.Combine(root, "dangling-source.txt"));
                Require(directory.IsLink("dangling-link"),
                    "Link detection must recognize a dangling symbolic link.");
                directory.Remove("dangling-link");
                Require(!directory.IsLink("dangling-link"),
                    "A dangling symbolic link must be removable without a target.");

                directory.CreateLink("never-created.txt", "direct-dangling-link");
                Require(directory.IsLink("direct-dangling-link") &&
                        directory.ReadLink("direct-dangling-link") == "never-created.txt",
                    "Link creation must preserve a relative target and allow it to be dangling.");
                directory.Remove("direct-dangling-link");
            }

            if (OperatingSystem.IsLinux())
            {
                var hardLink = Path.Combine(root, "alpha-hard-link");
                Require(TestNativeLinks.CreateHardLink(alphaPath, hardLink) == 0 &&
                        directory.IsEquivalent("alpha.txt", "alpha-hard-link"),
                    "Filesystem identity must recognize distinct hard-link names.");
                File.Delete(hardLink);
            }

            var alternateAlpha = Path.Combine(root, "ALPHA.TXT");
            var expectedCaseSensitivity = !File.Exists(alternateAlpha);
            Require(directory.IsCaseSensitive("alpha.txt") == expectedCaseSensitivity,
                "Case-sensitivity detection must follow the current directory policy.");
            Require(directory.GetSpaceLeft() >= 0 && directory.GetFilesystemType().Length > 0,
                "Filesystem queries must expose byte capacity and a filesystem identifier.");
            if (OperatingSystem.IsWindows())
            {
                using var extendedPath = DirAccess.Open(@"\\?\" + Path.GetFullPath(root));
                Require(extendedPath.GetFilesystemType() != "Network Share",
                    "An extended-length local path must not be classified as a network share.");
            }
            if (!OperatingSystem.IsMacOS())
            {
                Expect<PlatformNotSupportedException>(() => directory.IsBundle("visible-dir"),
                    "Bundle classification must reject unsupported hosts explicitly.");
            }
            if (OperatingSystem.IsLinux() || OperatingSystem.IsWindows() || OperatingSystem.IsMacOS())
            {
                var driveCount = DirAccess.GetDriveCount();
                var currentDrive = directory.GetCurrentDrive();
                Require(driveCount > 0 && currentDrive >= 0 && currentDrive < driveCount &&
                        DirAccess.GetDriveName(currentDrive).Length > 0,
                    "Drive enumeration must identify the current directory's filesystem root.");
                _ = DirAccess.GetDriveLabel(currentDrive);
                Expect<ArgumentOutOfRangeException>(() => DirAccess.GetDriveName(driveCount),
                    "Drive lookup must reject an invalid index.");
            }

            var absoluteSingle = Path.Combine(root, "absolute-single");
            var absoluteRecursive = Path.Combine(root, "absolute", "recursive");
            DirAccess.MakeDirAbsolute(absoluteSingle);
            DirAccess.MakeDirRecursiveAbsolute(absoluteRecursive);
            Require(DirAccess.DirExistsAbsolute(absoluteSingle) && DirAccess.DirExistsAbsolute(absoluteRecursive) &&
                    DirAccess.GetFilesAt(root).Contains("alpha.txt", StringComparer.Ordinal) &&
                    DirAccess.GetDirectoriesAt(root).Contains("absolute", StringComparer.Ordinal),
                "Absolute helpers must create, inspect, and list native paths.");
            var absoluteRenameSource = Path.Combine(root, "absolute-rename-source.txt");
            var absoluteRenameTarget = Path.Combine(root, "absolute-rename-target.txt");
            File.WriteAllText(absoluteRenameSource, "renamed");
            DirAccess.RenameAbsolute(absoluteRenameSource, absoluteRenameTarget);
            Require(!File.Exists(absoluteRenameSource) && File.ReadAllText(absoluteRenameTarget) == "renamed",
                "Absolute rename must move a native file.");
            DirAccess.RemoveAbsolute(absoluteRenameTarget);
            DirAccess.RemoveAbsolute(absoluteSingle);
        }

        var virtualName = $"electron2d-dir-access-{Guid.NewGuid():N}";
        var virtualRoot = Path.Combine(ProjectSettings.Instance.ProjectRoot, virtualName);
        Directory.CreateDirectory(virtualRoot);
        try
        {
            File.WriteAllText(Path.Combine(virtualRoot, "source.txt"), "virtual");
            using var virtualDirectory = DirAccess.Open($"res://{virtualName}");
            Require(virtualDirectory.GetCurrentDir() == $"res://{virtualName}" &&
                    virtualDirectory.FileExists("source.txt"),
                "Resource-scoped directory access must preserve its virtual prefix.");
            Expect<InvalidOperationException>(() => virtualDirectory.ChangeDir("user://"),
                "Resource-scoped access must reject user-data scope switches.");
            Expect<UnauthorizedAccessException>(() => virtualDirectory.ChangeDir("res://../outside"),
                "Virtual directory changes must reject lexical root traversal.");
            DirAccess.CopyAbsolute($"res://{virtualName}/source.txt", $"res://{virtualName}/copy.txt");
            Require(File.ReadAllText(Path.Combine(virtualRoot, "copy.txt")) == "virtual",
                "Absolute virtual copying must resolve both paths through one project-root snapshot.");
        }
        finally
        {
            Directory.Delete(virtualRoot, recursive: true);
        }

        string disposableTempPath;
        var disposableTemp = DirAccess.CreateTemp("electron2d");
        disposableTempPath = disposableTemp.GetCurrentDir();
        disposableTemp.MakeDirRecursive("nested/child");
        File.WriteAllText(Path.Combine(disposableTempPath, "nested", "child", "data.txt"), "temporary");
        if (OperatingSystem.IsLinux() || OperatingSystem.IsWindows() || OperatingSystem.IsMacOS())
            disposableTemp.CreateLink(Path.Combine(root, "visible-dir"), "nested/external-link");
        disposableTemp.ChangeDir(Path.GetTempPath());
        disposableTemp.Dispose();
        Require(!Directory.Exists(disposableTempPath) && Directory.Exists(Path.Combine(root, "visible-dir")),
            "Temporary disposal must delete the captured owned root after directory changes without traversing links.");

        string keptTempPath;
        using (var keptTemp = DirAccess.CreateTemp("electron2d", keep: true))
        {
            keptTempPath = keptTemp.GetCurrentDir();
            keptTemp.MakeDir("kept");
        }
        Require(Directory.Exists(keptTempPath), "Kept temporary directories must survive disposal.");
        Directory.Delete(keptTempPath, recursive: true);

        var disposed = DirAccess.Open(root);
        disposed.Dispose();
        Expect<ObjectDisposedException>(() => disposed.GetFiles(),
            "Disposed directory access must reject public operations.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

static void VerifyProjectSettings()
{
    Expect<ArgumentNullException>(() => new ProjectSetting<int>(null!, 1),
        "Project setting names must reject null.");
    Expect<ArgumentException>(() => new ProjectSetting<int>("missing_category", 1),
        "Project setting names must require a category path.");
    Expect<ArgumentException>(() => new ProjectSetting<int>("invalid/name.debug", 1),
        "Project setting names must reserve periods for feature overrides.");
    Expect<NotSupportedException>(() => new ProjectSetting<object>("invalid/object", new object()),
        "Project settings must reject universal object values.");
    Expect<ArgumentOutOfRangeException>(() => new ProjectSetting<int>("invalid/default", 0, value => value > 0),
        "Project setting validators must accept their default.");

    var root = Path.Combine(Path.GetTempPath(), $"electron2d-project-{Guid.NewGuid():N}");
    var user = Path.Combine(Path.GetTempPath(), $"electron2d-user-{Guid.NewGuid():N}");
    var secondRoot = Path.Combine(Path.GetTempPath(), $"electron2d-project-{Guid.NewGuid():N}");
    var secondUser = Path.Combine(Path.GetTempPath(), $"electron2d-user-{Guid.NewGuid():N}");
    Directory.CreateDirectory(root);
    Directory.CreateDirectory(user);
    Directory.CreateDirectory(secondRoot);
    Directory.CreateDirectory(secondUser);

    try
    {
        Expect<DirectoryNotFoundException>(() => new ProjectSettings(Path.Combine(root, "missing"), user),
            "ProjectSettings must reject an absent project root.");
        Expect<ArgumentException>(() => new ProjectSettings(root, root),
            "ProjectSettings must reject identical resource and user roots.");

        using var settings = new ProjectSettings(root, user);
        Require(settings.ProjectRoot == Path.GetFullPath(root) && settings.UserDataRoot == Path.GetFullPath(user) &&
                settings.ProjectFilePath == Path.Combine(root, ProjectSettings.ProjectFileName) &&
                settings.OverrideFilePath == Path.Combine(root, ProjectSettings.OverrideFileName) &&
                settings.ProjectDataPath == Path.Combine(root, ProjectSettings.ProjectDataDirectoryName),
            "ProjectSettings must expose normalized project paths without creating hidden state.");
        Require(settings.HasSetting(ProjectSettings.ApplicationName) &&
                settings.HasSetting(ProjectSettings.ApplicationVersion) &&
                settings.HasSetting(ProjectSettings.PhysicsTicksPerSecond) &&
                settings.Get(ProjectSettings.PhysicsTicksPerSecond) == 60,
            "Every registry must contain the implemented built-in settings and their defaults.");
        Expect<InvalidOperationException>(() => settings.Unregister(ProjectSettings.ApplicationName),
            "Built-in project settings must not be removable.");

        var score = new ProjectSetting<int>("gameplay/score", 10, value => value > 0);
        var title = new ProjectSetting<string>("application/title", "Untitled");
        var checkpoints = new ProjectSetting<List<int>>("gameplay/checkpoints", [1, 2]);
        var duplicateScore = new ProjectSetting<int>("gameplay/score", 10);
        var propertyListChanges = 0;
        settings.PropertyListChanged += _ => propertyListChanges++;
        settings.Register(score);
        settings.Register(title);
        settings.Register(checkpoints);
        Require(propertyListChanges == 3 && settings.HasSetting(score) && !settings.HasSetting(duplicateScore) &&
                settings.Get(score) == 10 && settings.Get(title) == "Untitled",
            "Registration must publish tooling changes and expose typed defaults only through the exact definition.");
        Expect<InvalidOperationException>(() => settings.Register(duplicateScore),
            "Duplicate setting names must be rejected regardless of definition identity.");
        Expect<InvalidOperationException>(() => settings.Get(duplicateScore),
            "A different definition with the same name must not access a registered setting.");

        var callbackFailureSetting = new ProjectSetting<int>("test/property_callback_failure", 1);
        void ThrowingPropertyListHandler(ElectronObject _) =>
            throw new InvalidOperationException("expected property list failure");
        settings.PropertyListChanged += ThrowingPropertyListHandler;
        Require(Capture(() => settings.Register(callbackFailureSetting)) is InvalidOperationException &&
                settings.HasSetting(callbackFailureSetting),
            "A property-list callback failure must propagate after registration has committed.");
        settings.PropertyListChanged -= ThrowingPropertyListHandler;
        settings.Unregister(callbackFailureSetting);
        settings.Save();
        settings.FlushChanges();

        var firstDefault = checkpoints.DefaultValue;
        firstDefault.Add(3);
        var firstRead = settings.Get(checkpoints);
        firstRead.Add(4);
        Require(checkpoints.DefaultValue.SequenceEqual([1, 2]) && settings.Get(checkpoints).SequenceEqual([1, 2]),
            "Mutable setting defaults and reads must be independent serialized snapshots.");

        var version = settings.Version;
        settings.Set(score, 20);
        settings.Set(score, 20);
        Require(settings.Get(score) == 20 && settings.Version == version + 1 &&
                settings.GetChangedSettings().SequenceEqual([score.Name]) &&
                settings.CheckChangedSettingsInGroup("gameplay/") &&
                !settings.CheckChangedSettingsInGroup("display/"),
            "Setting writes must validate, snapshot, dirty, and version only an actual serialized change.");
        Expect<ArgumentOutOfRangeException>(() => settings.Set(score, 0),
            "Setting validators must reject invalid writes.");
        Require(settings.Get(score) == 20,
            "A rejected setting write must preserve the previous value.");

        settings.SetInitialValue(score, 7);
        Require(settings.Get(score) == 20,
            "Changing a revert value must not change the current setting.");
        var scoreProperty = settings.GetPropertyList()
            .OfType<PropertyDescriptor<ProjectSettings, int>>()
            .Single(property => property.Name == score.Name);
        Require(scoreProperty.TryGetRevertValue(settings, out var initialScore) && initialScore == 7 &&
                settings.PropertyCanRevert(scoreProperty),
            "Registered settings must expose typed tooling descriptors with the current initial value.");
        settings.RevertProperty(scoreProperty);
        Require(settings.Get(score) == 7,
            "Typed property reversion must store the current initial value.");
        settings.Reset(score);
        Require(settings.Get(score) == 7,
            "Reset must retain a non-default initial value as the effective explicit value.");

        settings.SetOrder(score, -10);
        settings.SetAsBasic(score, true);
        settings.SetRestartIfChanged(score, true);
        Require(settings.GetOrder(score) == -10 && settings.IsBasic(score) && settings.IsRestartRequired(score) &&
                settings.GetSettingNames().First() == score.Name,
            "Setting metadata must retain order, basic-view, and restart-required flags.");
        settings.SetAsInternal(title, true);
        Require(settings.IsInternal(title) && !settings.GetSettingNames(includeInternal: false).Contains(title.Name) &&
                settings.GetPropertyList().All(property => property.Name != title.Name),
            "Internal settings must be hidden from ordinary tooling discovery while remaining registered.");
        settings.SetAsInternal(title, false);

        settings.SetFeatureOverride(score, "desktop", 30);
        settings.SetFeatureOverride(score, "portable", 40);
        Require(settings.Get(score) == 7 && settings.GetWithOverride(score, ["portable"]) == 40 &&
                settings.GetWithOverride(score, ["desktop", "portable"]) == 30 &&
                settings.GetFeatureOverrides(score).SequenceEqual(["desktop", "portable"]),
            "Feature overrides must preserve base access, explicit lookup, and deterministic insertion precedence.");
        settings.FlushChanges();
        Require(settings.AddCustomFeature("unmatched") && !settings.FlushChanges() &&
                settings.RemoveCustomFeature("unmatched") && !settings.FlushChanges(),
            "A custom feature that changes no selected override must not queue a settings notification.");
        Require(settings.AddCustomFeature("PORTABLE") && settings.HasFeature("portable") &&
                settings.GetWithOverride(score) == 40 && !settings.AddCustomFeature("portable"),
            "Custom feature tags must be normalized, idempotent, and participate in current-feature lookup.");
        Require(settings.RemoveCustomFeature("portable") && !settings.HasFeature("portable") &&
                settings.GetWithOverride(score) == 7,
            "Removing a custom feature must immediately restore base lookup.");
        Require(settings.ClearFeatureOverride(score, "desktop") &&
                !settings.ClearFeatureOverride(score, "desktop") &&
                settings.GetFeatureOverrides(score).SequenceEqual(["portable"]),
            "Feature override removal must report whether stored state changed.");
        Expect<ArgumentException>(() => settings.AddCustomFeature("bad feature"),
            "Feature tags must reject whitespace.");
        Expect<ArgumentException>(() => settings.AddCustomFeature("bad..feature"),
            "Feature tags must reject empty dotted segments.");
        Require(settings.GetActiveFeatures().Contains("dotnet") &&
                settings.GetActiveFeatures().Contains("debug") == false,
            "Release tests must expose deterministic runtime feature tags.");

        var settingsEvents = 0;
        var reenterEvent = true;
        void OnSettingsChanged(ProjectSettings sender)
        {
            settingsEvents++;
            Require(sender.GetChangedSettings().Count > 0,
                "SettingsChanged handlers must observe the batch currently being delivered.");
            if (reenterEvent)
            {
                reenterEvent = false;
                sender.Set(title, "Changed from event");
                Require(sender.GetChangedSettings().Contains(title.Name) && !sender.FlushChanges(),
                    "A handler-created batch must remain visible but cannot be delivered recursively.");
            }
        }

        settings.SettingsChanged += OnSettingsChanged;
        Require(settings.FlushChanges() && settingsEvents == 1 && settings.GetChangedSettings().SequenceEqual([title.Name]) &&
                settings.FlushChanges() && settingsEvents == 2 && settings.GetChangedSettings().Count == 0 &&
                !settings.FlushChanges() && settings.Get(title) == "Changed from event",
            "SettingsChanged must expose and clear each delivered batch while retaining handler-created changes for the next flush.");
        settings.SettingsChanged -= OnSettingsChanged;

        settings.Set(title, "Before failing handler");
        void ThrowingSettingsHandler(ProjectSettings _) => throw new InvalidOperationException("expected settings event failure");
        settings.SettingsChanged += ThrowingSettingsHandler;
        Require(Capture(() => settings.FlushChanges()) is InvalidOperationException && !settings.FlushChanges(),
            "A SettingsChanged handler failure must propagate after consuming that pending invocation.");
        settings.SettingsChanged -= ThrowingSettingsHandler;

        Require(settings.GlobalizePath("res://assets/player.png") == Path.Combine(root, "assets", "player.png") &&
                settings.GlobalizePath("user://save/game.json") == Path.Combine(user, "save", "game.json") &&
                settings.LocalizePath(Path.Combine(root, "assets", "player.png")) == "res://assets/player.png" &&
                settings.LocalizePath(Path.Combine(user, "save", "game.json")) == "user://save/game.json",
            "Virtual paths must round-trip against their configured project and user roots.");
        Expect<UnauthorizedAccessException>(() => settings.GlobalizePath("res://../escape.txt"),
            "Project virtual paths must reject parent traversal.");
        Expect<UnauthorizedAccessException>(() => settings.LocalizePath("user://../escape.txt"),
            "Localization must validate already-virtual paths instead of preserving traversal.");
        Expect<NotSupportedException>(() => settings.GlobalizePath("remote://asset"),
            "Unknown virtual path schemes must be rejected.");

        settings.Save();
        var projectText = File.ReadAllText(settings.ProjectFilePath, Encoding.UTF8);
        using (var savedDocument = new ConfigFile())
        {
            savedDocument.Load(settings.ProjectFilePath);
            Require(File.Exists(settings.ProjectFilePath) && settings.GetChangedSettings().Count == 0 &&
                        !savedDocument.HasSectionKey(new ConfigKey<int>("gameplay", "score")) &&
                        savedDocument.HasSectionKey(new ConfigKey<int>("gameplay", "score.portable")) &&
                        projectText.IndexOf("[gameplay]", StringComparison.Ordinal) <
                        projectText.IndexOf("[application]", StringComparison.Ordinal),
                "A successful save must omit the initial value, retain overrides, apply setting order, and clear unsaved tracking.");
        }
        using (var initialReload = new ProjectSettings(root, user))
        {
            initialReload.Register(score);
            initialReload.SetInitialValue(score, 7);
            initialReload.Load();
            Require(initialReload.Get(score) == 7,
                "A value omitted from persistence must resolve to the registry's current initial value after loading.");
        }

        settings.SaveCustom("user://custom.cfg");
        Require(File.Exists(Path.Combine(user, "custom.cfg")),
            "Custom persistence must accept a confined user virtual path.");
        var nested = Path.Combine(root, "nested", "deeper");
        Directory.CreateDirectory(nested);
        Require(ProjectSettings.FindProjectRoot(nested) == root,
            "Project root discovery must search parent directories for the nearest project file.");

        settings.Set(score, 11);
        Expect<DirectoryNotFoundException>(() => settings.SaveCustom(Path.Combine(root, "missing", "custom.cfg")),
            "A failed custom save must surface an absent destination directory.");
        Require(settings.GetChangedSettings().Contains(score.Name),
            "A failed save must preserve the pending change batch.");
        Expect<InvalidOperationException>(() => settings.ConfigurePaths(secondRoot, secondUser),
            "A failed save must preserve unsaved-value tracking.");
        settings.Save();
        Require(settings.GetChangedSettings().Contains(score.Name),
            "Saving must not consume the change-notification batch.");
        settings.FlushChanges();

        using (var baseDocument = new ConfigFile())
        {
            baseDocument.Load(settings.ProjectFilePath);
            baseDocument.SetValue(new ConfigKey<int>("late", "value"), 77);
            baseDocument.Save(settings.ProjectFilePath);
        }

        using (var overrideDocument = new ConfigFile())
        {
            overrideDocument.SetValue(new ConfigKey<int>("gameplay", "score"), 99);
            overrideDocument.Save(settings.OverrideFilePath);
        }

        using (var loaded = new ProjectSettings(root, user))
        {
            loaded.Register(score);
            loaded.Load();
            Require(loaded.Get(score) == 99 && loaded.GetChangedSettings().Count == 0,
                "Project loading must merge the conventional override file and start from a clean state.");

            var late = new ProjectSetting<int>("late/value", 1);
            loaded.Register(late);
            Require(loaded.Get(late) == 77,
                "Unknown loaded values must remain available for later typed registration.");

            loaded.Set(late, 78);
            var validCustomPath = Path.Combine(root, "valid-custom.cfg");
            File.WriteAllText(validCustomPath, "[gameplay]\n\nscore=88\n", new UTF8Encoding(false));
            loaded.LoadCustom(validCustomPath);
            Require(loaded.Get(score) == 88 && loaded.Get(late) == 78 &&
                    loaded.GetChangedSettings().SequenceEqual([late.Name]),
                "A custom merge must retain pre-existing unsaved values and their unsaved tracking.");

            var invalidPath = Path.Combine(root, "invalid.cfg");
            File.WriteAllText(invalidPath, "[gameplay]\n\nscore=0\n", new UTF8Encoding(false));
            Expect<ArgumentOutOfRangeException>(() => loaded.LoadCustom(invalidPath),
                "Custom loading must reject values that fail a registered validator.");
            Require(loaded.Get(score) == 88,
                "A validator failure must leave the complete settings document unchanged.");

            var reentrantValidation = false;
            var reentrant = new ProjectSetting<int>("test/reentrant", 1, value =>
            {
                if (reentrantValidation)
                    loaded.Set(score, 5);
                return value > 0;
            });
            loaded.Register(reentrant);
            var reentrantPath = Path.Combine(root, "reentrant.cfg");
            File.WriteAllText(reentrantPath, "[test]\n\nreentrant=2\n", new UTF8Encoding(false));
            reentrantValidation = true;
            Expect<InvalidOperationException>(() => loaded.LoadCustom(reentrantPath),
                "A validator must not mutate project settings re-entrantly during transactional loading.");
            Require(loaded.Get(score) == 88 && loaded.Get(reentrant) == 1,
                "Rejected re-entrant loading must preserve every current value.");

            loaded.Set(score, 13);
            Expect<InvalidOperationException>(() => loaded.ConfigurePaths(secondRoot, secondUser),
                "Path reconfiguration must reject unsaved changes.");
            loaded.Save();
            loaded.ConfigurePaths(secondRoot, secondUser);
            Require(loaded.ProjectRoot == Path.GetFullPath(secondRoot) && loaded.Get(score) == 10,
                "Clean path reconfiguration must retain definitions while clearing loaded values.");
        }

        Parallel.For(1, 129, value =>
        {
            settings.Set(score, value);
            Require(settings.Get(score) > 0, "Concurrent setting access must preserve validator invariants.");
        });

        settings.Unregister(checkpoints);
        Require(!settings.HasSetting(checkpoints) && propertyListChanges >= 8,
            "Custom setting removal must update registration and tooling discovery.");

        var disposable = new ProjectSettings(root, user);
        disposable.Dispose();
        Expect<ObjectDisposedException>(() => disposable.Get(ProjectSettings.ApplicationName),
            "Disposed isolated registries must reject reads.");
        Expect<InvalidOperationException>(ProjectSettings.Instance.Dispose,
            "The process-wide ProjectSettings registry must reject disposal.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
        Directory.Delete(user, recursive: true);
        Directory.Delete(secondRoot, recursive: true);
        Directory.Delete(secondUser, recursive: true);
    }
}

static void VerifyEngine()
{
    var engine = Engine.Instance;
    Require(ReferenceEquals(engine, Engine.Instance), "Engine must be a process-wide singleton.");
    Require(!engine.IsDisposed, "The process-wide Engine must remain live.");
    Require(engine.HasSingleton(nameof(Engine)) && ReferenceEquals(engine.GetSingleton<Engine>(nameof(Engine)), engine) &&
            engine.HasSingleton(nameof(ProjectSettings)) &&
            ReferenceEquals(engine.GetSingleton<ProjectSettings>(nameof(ProjectSettings)), ProjectSettings.Instance) &&
            engine.GetSingletonList().SequenceEqual([nameof(Engine), nameof(ProjectSettings)]),
        "The built-in Engine and ProjectSettings instances must be present in the global singleton registry.");
    Expect<InvalidOperationException>(() => engine.UnregisterSingleton(nameof(Engine)),
        "The built-in Engine registry entry must not be removable.");
    Expect<InvalidOperationException>(() => engine.UnregisterSingleton(nameof(ProjectSettings)),
        "The built-in ProjectSettings registry entry must not be removable.");
    Expect<InvalidOperationException>(engine.Dispose, "The process-wide Engine must reject disposal.");
    Require(engine.PhysicsTicksPerSecond == 60 && engine.MaxPhysicsStepsPerFrame == 8 &&
            DoubleNearlyEqual(engine.PhysicsJitterFix, 0.5d) && DoubleNearlyEqual(engine.TimeScale, 1d),
        "Engine timing settings must expose their documented defaults.");
    var processSettings = ProjectSettings.Instance;
    processSettings.SetFeatureOverride(ProjectSettings.PhysicsTicksPerSecond, "tests", 30);
    processSettings.AddCustomFeature("tests");
    Require(engine.PhysicsTicksPerSecond == 30,
        "Engine timing must read active feature overrides from process-wide project settings.");
    processSettings.RemoveCustomFeature("tests");
    processSettings.ClearFeatureOverride(ProjectSettings.PhysicsTicksPerSecond, "tests");
    processSettings.FlushChanges();
    Expect<ArgumentOutOfRangeException>(() => engine.PhysicsTicksPerSecond = 0,
        "Physics tick frequency must reject zero.");
    Expect<ArgumentOutOfRangeException>(() => engine.MaxPhysicsStepsPerFrame = -1,
        "Maximum physics steps must reject negative values.");
    Expect<ArgumentOutOfRangeException>(() => engine.PhysicsJitterFix = double.NaN,
        "Physics jitter fix must reject NaN.");
    Expect<ArgumentOutOfRangeException>(() => engine.TimeScale = double.PositiveInfinity,
        "Time scale must reject infinity.");
    Expect<ArgumentOutOfRangeException>(() => engine.TimeScale = -double.Epsilon,
        "Time scale must reject negative values.");
    engine.PhysicsJitterFix = -1d;
    Require(engine.PhysicsJitterFix == 0d, "A negative physics jitter fix must clamp to zero.");

    var engineProperties = engine.GetPropertyList();
    Require(engineProperties.Any(property => property.Name == nameof(Engine.PhysicsTicksPerSecond)) &&
            engineProperties.Any(property => property.Name == nameof(Engine.TimeScale)) &&
            engineProperties.Any(property => property.Name == nameof(Engine.ProcessFrames)),
        "Engine timing settings and metrics must participate in typed property discovery.");
    Require(!string.IsNullOrWhiteSpace(engine.ArchitectureName) &&
            engine.VersionInfo.AssemblyVersion == typeof(Engine).Assembly.GetName().Version &&
            !string.IsNullOrWhiteSpace(engine.VersionInfo.InformationalVersion) &&
            engine.VersionInfo.ToString() == engine.VersionInfo.InformationalVersion,
        "Engine build information must describe the loaded assembly and process architecture.");

    using (var registered = new TestObject())
    {
        engine.RegisterSingleton("tests.primary", registered);
        Require(engine.HasSingleton("tests.primary") &&
                ReferenceEquals(engine.GetSingleton("tests.primary"), registered) &&
                ReferenceEquals(engine.GetSingleton<TestObject>("tests.primary"), registered) &&
                engine.GetSingletonList().SequenceEqual([nameof(Engine), nameof(ProjectSettings), "tests.primary"]),
            "Engine singleton lookup must preserve identity, type, and registration order.");
        Expect<InvalidOperationException>(() => engine.RegisterSingleton("tests.primary", registered),
            "Engine singleton names must be unique.");
        Expect<InvalidCastException>(() => engine.GetSingleton<Resource>("tests.primary"),
            "Typed engine singleton lookup must reject an incompatible type.");
        engine.UnregisterSingleton("tests.primary");
        Require(!engine.HasSingleton("tests.primary") && !registered.IsDisposed,
            "Unregistering an engine singleton must not dispose the registered object.");
    }

    Expect<ArgumentNullException>(() => engine.HasSingleton(null!), "Engine singleton names must reject null.");
    Expect<ArgumentException>(() => engine.HasSingleton("  "), "Engine singleton names must reject whitespace.");
    Expect<KeyNotFoundException>(() => engine.GetSingleton("tests.missing"),
        "Engine singleton lookup must report an absent name.");
    Expect<KeyNotFoundException>(() => engine.UnregisterSingleton("tests.missing"),
        "Engine singleton removal must report an absent name.");
    var disposedSingleton = new TestObject();
    disposedSingleton.Dispose();
    Expect<ObjectDisposedException>(() => engine.RegisterSingleton("tests.disposed", disposedSingleton),
        "Engine singleton registration must reject disposed objects.");

    Parallel.For(0, 32, index => engine.RegisterSingleton($"tests.concurrent.{index}", new TestObject()));
    Require(engine.GetSingletonList().Count == 34, "Concurrent singleton registration must not lose entries.");
    Parallel.For(0, 32, index =>
    {
        var name = $"tests.concurrent.{index}";
        var instance = engine.GetSingleton(name);
        engine.UnregisterSingleton(name);
        instance.Dispose();
    });
    Require(engine.GetSingletonList().SequenceEqual([nameof(Engine), nameof(ProjectSettings)]),
        "Concurrent singleton removal must preserve only the built-in registry entries.");

    engine.PhysicsTicksPerSecond = 10;
    engine.MaxPhysicsStepsPerFrame = 3;
    engine.PhysicsJitterFix = 0d;
    engine.TimeScale = 2d;

    using (var loop = new EngineProbeMainLoop())
    {
        var projectSettingsEvents = 0;
        void OnProjectSettingsChanged(ProjectSettings _) => projectSettingsEvents++;
        processSettings.SettingsChanged += OnProjectSettingsChanged;
        processSettings.Set(ProjectSettings.ApplicationVersion, "frame-test");
        engine.Start(loop);
        Require(loop.InitializeCount == 1 && ReferenceEquals(loop.MainLoopDuringInitialize, loop) &&
                ReferenceEquals(engine.MainLoop, loop),
            "Engine.Start must publish and initialize an uninitialized MainLoop exactly once.");
        Expect<InvalidOperationException>(() => engine.Start(loop), "Engine must reject a second active MainLoop.");
        Expect<ArgumentOutOfRangeException>(() => engine.AdvanceFrame(double.NaN),
            "Engine frame scheduling must reject NaN.");
        Expect<ArgumentOutOfRangeException>(() => engine.AdvanceFrame(-double.Epsilon),
            "Engine frame scheduling must reject negative elapsed time.");

        Require(!engine.AdvanceFrame(0.05d) && loop.PhysicsDeltas.Count == 0 &&
                loop.ProcessDeltas.Count == 1 && DoubleNearlyEqual(loop.ProcessDeltas[0], 0.1d) &&
                DoubleNearlyEqual(engine.PhysicsInterpolationFraction, 0.5d) && projectSettingsEvents == 1,
            "A half fixed interval must run one scaled process callback, expose interpolation, and flush project settings once.");
        processSettings.SettingsChanged -= OnProjectSettingsChanged;
        processSettings.Set(ProjectSettings.ApplicationVersion, string.Empty);
        processSettings.FlushChanges();
        Require(!engine.AdvanceFrame(0.05d) && loop.PhysicsDeltas.Count == 1 &&
                DoubleNearlyEqual(loop.PhysicsDeltas[0], 0.2d) && !engine.IsInPhysicsFrame &&
                loop.ObservedPhysicsFrameState && !loop.ObservedProcessFrameState &&
                loop.Order.TakeLast(2).SequenceEqual(["physics", "process"]),
            "A complete fixed interval must run a scaled fixed callback before process and restore physics state.");
        Require(engine.ProcessFrames == 2 && engine.PhysicsFrames == 1,
            "Engine frame counters must reflect completed process and started fixed callbacks.");

        loop.ReenterEngine = true;
        engine.AdvanceFrame(0.1d);
        Require(loop.AdvanceReentryError is InvalidOperationException && loop.StopReentryError is InvalidOperationException,
            "Engine callbacks must reject frame and stop re-entry without poisoning the runtime.");
        loop.ReenterEngine = false;

        var physicsBeforeCap = engine.PhysicsFrames;
        engine.AdvanceFrame(1000.05d);
        Require(engine.PhysicsFrames - physicsBeforeCap <= 3 && engine.PhysicsInterpolationFraction is >= 0d and <= 1d,
            "A long host stall must obey the fixed-step catch-up cap and preserve a bounded interpolation fraction.");

        loop.PhysicsResult = true;
        loop.ProcessResult = true;
        var physicsBeforeStopRequest = loop.PhysicsDeltas.Count;
        Require(engine.AdvanceFrame(0.3d) && loop.ProcessDeltas.Count >= 4 &&
                loop.PhysicsDeltas.Count == physicsBeforeStopRequest + 1,
            "A fixed-step stop request must skip remaining fixed work but still run process and combine stop requests.");
        loop.PhysicsResult = false;
        loop.ProcessResult = false;

        var processFramesBeforeFailure = engine.ProcessFrames;
        loop.ThrowOnProcess = true;
        Require(Capture(() => engine.AdvanceFrame(0d)) is InvalidOperationException &&
                engine.ProcessFrames == processFramesBeforeFailure && ReferenceEquals(engine.MainLoop, loop),
            "A process callback failure must not count completion or detach the running loop.");
        loop.ThrowOnProcess = false;
        engine.AdvanceFrame(0d);

        Require(Task.Run(() => Capture(() => engine.AdvanceFrame(0d))).GetAwaiter().GetResult() is InvalidOperationException &&
                Task.Run(() => Capture(engine.Stop)).GetAwaiter().GetResult() is InvalidOperationException,
            "Engine frame execution and stop must reject a non-owner thread.");

        engine.Stop();
        Require(loop.FinalizeCount == 1 && ReferenceEquals(loop.MainLoopDuringFinalize, loop) &&
                engine.MainLoop is null && !loop.IsDisposed,
            "Engine.Stop must finalize and detach, but not dispose, the MainLoop.");
        Expect<InvalidOperationException>(() => engine.Start(loop),
            "Engine must reject reattaching a finalized MainLoop and return to idle.");
        Expect<InvalidOperationException>(() => engine.AdvanceFrame(0d),
            "Engine must reject frames after stop.");
        Expect<InvalidOperationException>(engine.Stop, "Engine must reject stop while idle.");
    }

    engine.TimeScale = 1d;
    engine.PhysicsTicksPerSecond = 60;
    engine.MaxPhysicsStepsPerFrame = 8;
    engine.PhysicsJitterFix = 0.5d;

    using (var failedInitialize = new EngineProbeMainLoop { ThrowOnInitialize = true })
    {
        Require(Capture(() => engine.Start(failedInitialize)) is InvalidOperationException && engine.MainLoop is null,
            "Failed loop initialization must return Engine to idle and clear its MainLoop.");
    }

    var disposedLoop = new EngineProbeMainLoop();
    disposedLoop.Dispose();
    Expect<ObjectDisposedException>(() => engine.Start(disposedLoop),
        "Engine must reject a disposed MainLoop and return to idle.");

    using (var wrongThreadLoop = new EngineProbeMainLoop())
    {
        Require(Task.Run(() => Capture(() => engine.Start(wrongThreadLoop))).GetAwaiter().GetResult() is InvalidOperationException &&
                engine.MainLoop is null,
            "Engine.Start must preserve MainLoop owner-thread affinity and roll back a failed attachment.");
    }

    using (var lifecycleReentry = new EngineProbeMainLoop
    {
        ReenterDuringInitialize = true,
        ReenterDuringFinalize = true
    })
    {
        engine.Start(lifecycleReentry);
        Require(lifecycleReentry.StartDuringInitializeError is InvalidOperationException,
            "Engine startup must reject lifecycle re-entry.");
        engine.Stop();
        Require(lifecycleReentry.StartDuringFinalizeError is InvalidOperationException &&
                lifecycleReentry.StopDuringFinalizeError is InvalidOperationException,
            "Engine shutdown must reject lifecycle re-entry.");
    }

    var failedFinalize = new EngineProbeMainLoop { ThrowOnFinalize = true };
    engine.Start(failedFinalize);
    Require(Capture(engine.Stop) is InvalidOperationException && engine.MainLoop is null,
        "Failed loop finalization must still detach the terminal MainLoop.");
    failedFinalize.Dispose();

    using (var physicsFailure = new EngineProbeMainLoop { ThrowOnPhysics = true })
    {
        var physicsFramesBeforeFailure = engine.PhysicsFrames;
        engine.Start(physicsFailure);
        Require(Capture(() => engine.AdvanceFrame(1d / 60d)) is InvalidOperationException &&
                engine.PhysicsFrames == physicsFramesBeforeFailure + 1 && !engine.IsInPhysicsFrame &&
                ReferenceEquals(engine.MainLoop, physicsFailure),
            "A fixed callback failure must count its start, clear physics state, and leave Engine running.");
        physicsFailure.ThrowOnPhysics = false;
        engine.AdvanceFrame(0d);
        engine.Stop();
    }

    using (var fpsLoop = new EmptyMainLoop())
    {
        engine.Start(fpsLoop);
        for (var index = 0; index < 10; index++)
            engine.AdvanceFrame(0.1d);
        Require(DoubleNearlyEqual(engine.FramesPerSecond, 10d),
            "Engine must publish completed process frames per unscaled host second.");

        for (var index = 0; index < 16; index++)
            engine.AdvanceFrame(0d);
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 128; index++)
            engine.AdvanceFrame(0d);
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        Require(allocatedBytes == 0,
            $"A warmed idle Engine frame must not allocate; observed {allocatedBytes} bytes.");
        engine.Stop();
    }

    using (var frequencyChange = new EngineProbeMainLoop())
    {
        engine.PhysicsTicksPerSecond = 10;
        engine.PhysicsJitterFix = 0d;
        engine.TimeScale = 0d;
        engine.Start(frequencyChange);
        engine.AdvanceFrame(0.05d);
        engine.PhysicsTicksPerSecond = 20;
        engine.AdvanceFrame(0.025d);
        Require(frequencyChange.PhysicsDeltas.Count == 0 && frequencyChange.ProcessDeltas.All(delta => delta == 0d) &&
                DoubleNearlyEqual(engine.PhysicsInterpolationFraction, 0.5d),
            $"A tick-frequency change must re-baseline old fractional time, while zero time scale freezes delivered deltas. Physics={frequencyChange.PhysicsDeltas.Count}, process=[{string.Join(',', frequencyChange.ProcessDeltas)}], interpolation={engine.PhysicsInterpolationFraction}.");
        engine.Stop();
    }

    using (var jitterTolerance = new EngineProbeMainLoop())
    {
        engine.PhysicsTicksPerSecond = 10;
        engine.PhysicsJitterFix = 0.5d;
        engine.TimeScale = 1d;
        engine.Start(jitterTolerance);
        engine.AdvanceFrame(0.05d);
        Require(jitterTolerance.PhysicsDeltas.Count == 1 && engine.PhysicsInterpolationFraction == 0d,
            "The default jitter tolerance must permit a stable fixed callback at a half-step boundary.");
        engine.Stop();
    }

    engine.PhysicsTicksPerSecond = 60;
    engine.PhysicsJitterFix = 0.5d;
    engine.TimeScale = 1d;

    var root = new Node();
    var tree = new SceneTree(root);
    engine.Start(tree);
    engine.AdvanceFrame(0d);
    engine.Stop();
    Require(root.IsDisposed && !tree.IsDisposed,
        "Engine must attach an already initialized SceneTree and finalize its owned hierarchy exactly once.");
    tree.Dispose();
}

static void VerifyMainLoop()
{
    using (var empty = new EmptyMainLoop())
    {
        empty.Initialize();
        Require(!empty.Process(0d) && !empty.PhysicsProcess(0d),
            "The default MainLoop frame hooks must perform no work and return no stop request.");
        empty.FinalizeLoop();
    }

    using (var loop = new TestMainLoop())
    {
        Expect<InvalidOperationException>(() => loop.Process(0d), "Process must require successful initialization.");
        Expect<InvalidOperationException>(() => loop.PhysicsProcess(0d), "PhysicsProcess must require successful initialization.");
        Expect<InvalidOperationException>(loop.FinalizeLoop, "FinalizeLoop must require successful initialization.");
        Expect<ArgumentOutOfRangeException>(() => loop.Process(double.NaN), "Process delta must reject NaN before execution.");

        loop.Initialize();
        Require(loop.Log.SequenceEqual(["initialize"]), "Initialize must invoke its hook exactly once.");
        Expect<InvalidOperationException>(loop.Initialize, "Initialize must reject a second call.");

        loop.ProcessResult = true;
        loop.PhysicsResult = false;
        Require(loop.Process(0.25d), "Process must return the stop request from its callback.");
        Require(!loop.PhysicsProcess(0.5d), "PhysicsProcess must return its callback result.");
        Require(loop.Log.TakeLast(2).SequenceEqual(["process:0.25", "physics:0.5"]),
            "Frame callbacks must receive their supplied deltas.");
        Expect<ArgumentOutOfRangeException>(() => loop.Process(-double.Epsilon), "Process delta must reject negatives.");
        Expect<ArgumentOutOfRangeException>(() => loop.PhysicsProcess(double.PositiveInfinity),
            "PhysicsProcess delta must reject infinity.");

        loop.ReenterProcess = true;
        loop.Process(0d);
        Require(loop.ReentryError is InvalidOperationException, "A frame callback must not re-enter frame execution.");
        loop.ReenterProcess = false;

        loop.DisposeDuringProcess = true;
        loop.Process(0d);
        Require(loop.DisposeError is InvalidOperationException && !loop.IsDisposed,
            "Disposal from a frame callback must be rejected without poisoning the loop.");
        loop.DisposeDuringProcess = false;

        loop.FinalizeDuringProcess = true;
        loop.Process(0d);
        Require(loop.FinalizeError is InvalidOperationException,
            "Finalization from a frame callback must be rejected without poisoning the loop.");
        loop.FinalizeDuringProcess = false;

        loop.ThrowOnProcess = true;
        Require(Capture(() => loop.Process(0d)) is InvalidOperationException,
            "A process callback failure must propagate.");
        loop.ThrowOnProcess = false;
        Require(loop.Process(0d), "A process callback failure must leave a running loop usable.");

        var permissions = new List<string>();
        loop.OnRequestPermissionsResult += (sender, permission, granted) =>
        {
            Require(ReferenceEquals(sender, loop), "The permission event must identify its loop.");
            permissions.Add($"{permission}:{granted}");
        };
        loop.PublishPermission("camera", granted: true);
        Require(permissions.SequenceEqual(["camera:True"]), "Permission results must preserve their typed payload.");
        Expect<ArgumentNullException>(() => loop.PublishPermission(null!, granted: false),
            "Permission publication must reject a null platform name.");

        loop.Notify(MainLoop.NotificationApplicationPaused);
        Require(loop.Notifications.Contains(MainLoop.NotificationApplicationPaused),
            "MainLoop must retain inherited numeric notification dispatch.");

        loop.FinalizeLoop();
        Require(loop.Log.Last() == "finalize" && loop.FinalizeCount == 1,
            "FinalizeLoop must invoke its hook once.");
        Expect<InvalidOperationException>(loop.FinalizeLoop, "FinalizeLoop must reject a second call.");
        Expect<InvalidOperationException>(() => loop.Process(0d), "A finalized loop must reject later frames.");
        Expect<InvalidOperationException>(() => loop.PublishPermission("camera", granted: false),
            "A finalized loop must reject permission publication.");
    }

    var disposedLoop = new TestMainLoop();
    disposedLoop.Initialize();
    disposedLoop.Dispose();
    Require(disposedLoop.IsDisposed && disposedLoop.FinalizeCount == 1,
        "Disposing a running loop must finalize it exactly once.");

    var neverInitialized = new TestMainLoop();
    neverInitialized.Dispose();
    Require(neverInitialized.FinalizeCount == 0,
        "Disposing an uninitialized loop must not invoke finalization.");

    var lifecycleReentry = new TestMainLoop
    {
        DisposeDuringInitialize = true,
        DisposeDuringFinalize = true,
        ReenterInitialize = true
    };
    lifecycleReentry.Initialize();
    Require(lifecycleReentry.InitializeReentryError is InvalidOperationException &&
            lifecycleReentry.InitializeDisposeError is InvalidOperationException && !lifecycleReentry.IsDisposed,
        "Initialization must reject recursive initialization and disposal before changing object lifetime.");
    lifecycleReentry.FinalizeLoop();
    Require(lifecycleReentry.FinalizeDisposeError is InvalidOperationException && !lifecycleReentry.IsDisposed,
        "Finalization must reject disposal until its callback reaches terminal state.");
    lifecycleReentry.Dispose();

    using (var handlerFailure = new TestMainLoop())
    {
        handlerFailure.Initialize();
        var laterHandlerRan = false;
        handlerFailure.OnRequestPermissionsResult += (_, _, _) => throw new InvalidOperationException("expected permission failure");
        handlerFailure.OnRequestPermissionsResult += (_, _, _) => laterHandlerRan = true;
        Require(Capture(() => handlerFailure.PublishPermission("camera", true)) is InvalidOperationException &&
                !laterHandlerRan && !handlerFailure.IsDisposed,
            "A permission handler failure must propagate synchronously and preserve loop lifetime.");
        handlerFailure.Process(0d);
    }

    var initializationFailure = new TestMainLoop { ThrowOnInitialize = true };
    Require(Capture(initializationFailure.Initialize) is InvalidOperationException,
        "An initialization failure must propagate.");
    Expect<InvalidOperationException>(initializationFailure.Initialize,
        "Failed initialization must be terminal instead of retrying user code.");
    Expect<InvalidOperationException>(() => initializationFailure.Process(0d),
        "A loop with failed initialization must reject frames.");
    initializationFailure.Dispose();
    Require(initializationFailure.FinalizeCount == 0 && initializationFailure.IsDisposed,
        "Disposal after failed initialization must not invoke the unmatched finalize hook.");

    var finalizationFailure = new TestMainLoop { ThrowOnFinalize = true };
    finalizationFailure.Initialize();
    Require(Capture(finalizationFailure.FinalizeLoop) is InvalidOperationException,
        "A finalization failure must propagate.");
    Expect<InvalidOperationException>(finalizationFailure.FinalizeLoop,
        "Failed finalization must still leave a terminal loop.");
    finalizationFailure.Dispose();
    Require(finalizationFailure.IsDisposed && finalizationFailure.FinalizeCount == 1,
        "Disposal must not retry a failed finalize hook.");

    var wrongThreadLoop = new TestMainLoop();
    Require(Task.Run(() => Capture(wrongThreadLoop.Initialize)).GetAwaiter().GetResult() is InvalidOperationException,
        "Initialization must reject a non-owner thread.");
    wrongThreadLoop.Initialize();
    Require(Task.Run(() => Capture(() => wrongThreadLoop.Process(0d))).GetAwaiter().GetResult() is InvalidOperationException &&
            Task.Run(() => Capture(() => wrongThreadLoop.PublishPermission("camera", true))).GetAwaiter().GetResult() is InvalidOperationException &&
            Task.Run(() => Capture(wrongThreadLoop.FinalizeLoop)).GetAwaiter().GetResult() is InvalidOperationException &&
            Task.Run(() => Capture(wrongThreadLoop.Dispose)).GetAwaiter().GetResult() is InvalidOperationException &&
            !wrongThreadLoop.IsDisposed,
        "Frames, permission results, finalization, and disposal must retain owner-thread affinity.");
    wrongThreadLoop.Dispose();

    Require(MainLoop.NotificationOsMemoryWarning == 2009 && MainLoop.NotificationTranslationChanged == 2010 &&
            MainLoop.NotificationWmAbout == 2011 && MainLoop.NotificationCrash == 2012 &&
            MainLoop.NotificationOsImeUpdate == 2013 && MainLoop.NotificationApplicationResumed == 2014 &&
            MainLoop.NotificationApplicationPaused == 2015 && MainLoop.NotificationApplicationFocusIn == 2016 &&
            MainLoop.NotificationApplicationFocusOut == 2017 && MainLoop.NotificationTextServerChanged == 2018 &&
            MainLoop.NotificationApplicationPipModeEntered == 2019 && MainLoop.NotificationApplicationPipModeExited == 2020,
        "MainLoop system notification identifiers must retain their stable values.");
    Require(Node.NotificationOsMemoryWarning == MainLoop.NotificationOsMemoryWarning &&
            Node.NotificationTranslationChanged == MainLoop.NotificationTranslationChanged &&
            Node.NotificationWmAbout == MainLoop.NotificationWmAbout && Node.NotificationCrash == MainLoop.NotificationCrash &&
            Node.NotificationOsImeUpdate == MainLoop.NotificationOsImeUpdate &&
            Node.NotificationApplicationResumed == MainLoop.NotificationApplicationResumed &&
            Node.NotificationApplicationPaused == MainLoop.NotificationApplicationPaused &&
            Node.NotificationApplicationFocusIn == MainLoop.NotificationApplicationFocusIn &&
            Node.NotificationApplicationFocusOut == MainLoop.NotificationApplicationFocusOut &&
            Node.NotificationTextServerChanged == MainLoop.NotificationTextServerChanged &&
            Node.NotificationApplicationPipModeEntered == MainLoop.NotificationApplicationPipModeEntered &&
            Node.NotificationApplicationPipModeExited == MainLoop.NotificationApplicationPipModeExited,
        "Node system notification aliases must match MainLoop.");

    var notificationLog = new List<string>();
    var notificationRoot = new SystemNotificationNode("root", notificationLog);
    notificationRoot.AddChild(new SystemNotificationNode("child", notificationLog));
    using (var tree = new SceneTree(notificationRoot))
    {
        Require(tree is MainLoop, "SceneTree must implement the MainLoop contract.");
        Expect<InvalidOperationException>(tree.Initialize, "SceneTree construction must complete MainLoop initialization.");
        Require(!tree.Process(0d) && tree.ProcessFrameCount == 1,
            "Driving SceneTree through MainLoop.Process must run its process pipeline without requesting termination.");
        tree.Notify(MainLoop.NotificationTranslationChanged);
        Require(notificationLog.SequenceEqual(["root:2010", "child:2010"]),
            "SceneTree must propagate system notifications through the active hierarchy in depth-first order.");
    }

    var failingNotificationLog = new List<string>();
    var failingNotificationRoot = new SystemNotificationNode("root", failingNotificationLog) { ThrowOnSystem = true };
    failingNotificationRoot.AddChild(new SystemNotificationNode("child", failingNotificationLog));
    using (var tree = new SceneTree(failingNotificationRoot))
    {
        Require(Capture(() => tree.Notify(MainLoop.NotificationApplicationPaused)) is AggregateException &&
                failingNotificationLog.SequenceEqual(["root:2015", "child:2015"]),
            "A failing system-notification callback must not prevent later live nodes from receiving it.");
    }

    var finalizeOnEnter = new FinalizeOnEnterNode();
    using (var tree = new SceneTree(finalizeOnEnter))
    {
        Require(finalizeOnEnter.FinalizeError is InvalidOperationException && !tree.IsDisposed,
            "Finalization requested from construction lifecycle must be rejected before changing loop state.");
        tree.Process(0d);
    }

    var finalizedRoot = new Node();
    var finalizedTree = new SceneTree(finalizedRoot);
    var finalizedTimer = finalizedTree.CreateTimer(1d);
    finalizedTree.FinalizeLoop();
    Require(finalizedRoot.IsDisposed && finalizedTimer.IsDisposed && finalizedTree.NodeCount == 0 && !finalizedTree.IsDisposed,
        "Explicit SceneTree finalization must release its owned hierarchy and timers before object disposal.");
    Expect<InvalidOperationException>(() => finalizedTree.Process(0d),
        "An explicitly finalized SceneTree must reject later frames.");
    Expect<ObjectDisposedException>(() => finalizedTree.Defer(static () => { }),
        "An explicitly finalized SceneTree must reject new deferred work.");
    finalizedTree.Dispose();

    using var allocationTree = new SceneTree(new Node());
    for (var index = 0; index < 16; index++)
        allocationTree.Process(0d);
    var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
    for (var index = 0; index < 128; index++)
    {
        allocationTree.Process(0d);
        allocationTree.PhysicsProcess(0d);
    }
    var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
    Require(allocatedBytes == 0, $"An idle SceneTree process hot path must not allocate after warm-up; observed {allocatedBytes} bytes.");
}

static void VerifyInstanceIds()
{
    const int objectCount = 10_000;
    var instanceIds = new ulong[objectCount];

    Parallel.For(0, objectCount, index => instanceIds[index] = new TestObject().InstanceId);

    Require(!instanceIds.Contains(0UL), "Instance IDs must be non-zero.");
    Require(instanceIds.Distinct().Count() == objectCount, "Instance IDs must be unique.");
}

static void VerifyLifetime()
{
    var instance = new TestObject();
    var disposedEvents = 0;

    Require(instance.ClassName == nameof(TestObject), "ClassName must contain the runtime type name.");
    Require(instance.ToString() == $"{nameof(TestObject)}#{instance.InstanceId}", "ToString must identify the instance.");

    instance.Disposed += sender =>
    {
        Require(ReferenceEquals(sender, instance), "Disposed must identify its sender.");
        Interlocked.Increment(ref disposedEvents);
    };

    Parallel.For(0, 1_000, _ => instance.Dispose());

    Require(instance.IsDisposed, "Dispose must mark the instance as disposed.");
    Require(instance.DisposeCount == 1, "Dispose(bool) must run exactly once.");
    Require(disposedEvents == 1, "Disposed must be raised exactly once.");
    Require(instance.Notifications.Count(notification => notification == ElectronObject.NotificationPreDelete) == 1,
        "Dispose must deliver one pre-delete notification.");
    Require(instance.CanTranslateDuringPreDelete,
        "The disposing thread must be able to inspect object state during pre-delete callbacks.");

    Expect<ObjectDisposedException>(instance.Use, "ThrowIfDisposed must reject access after disposal.");
}

static void VerifyNotificationsAndProperties()
{
    using var instance = new TestObject();
    var propertyListChanges = 0;
    var scriptChanges = 0;

    instance.Notify(1234);
    Require(instance.Notifications.Contains(1234), "Notify must invoke OnNotification.");

    instance.PropertyListChanged += _ => propertyListChanges++;
    instance.AnnouncePropertyListChanged();
    Require(propertyListChanges == 1, "PropertyListChanged must be raised by the protected notifier.");

    instance.ScriptChanged += sender =>
    {
        Require(ReferenceEquals(sender, instance), "ScriptChanged must identify its sender.");
        scriptChanges++;
    };
    instance.AnnounceScriptChanged();
    Require(scriptChanges == 1, "ScriptChanged must be raised by the protected notifier.");

    var properties = instance.GetPropertyList();
    var valueProperty = properties.OfType<PropertyDescriptor<TestObject, int>>().Single(property => property.Name == nameof(TestObject.Value));

    Require(valueProperty.GetValue(instance) == 0, "A property descriptor must read its value.");
    valueProperty.SetValue(instance, 42);
    Require(instance.Value == 42, "A property descriptor must write its value.");
    Require(instance.PropertyCanRevert(valueProperty), "A changed property must be revertible.");
    Require(valueProperty.TryGetRevertValue(instance, out var revertValue) && revertValue == 0,
        "A property descriptor must expose its typed revert value.");

    instance.RevertProperty(valueProperty);
    Require(instance.Value == 0, "RevertProperty must restore the descriptor's revert value.");

    Expect<ArgumentOutOfRangeException>(
        () => valueProperty.SetValue(instance, -1),
        "A property validator must reject an invalid value.");

    using var duplicateProperties = new DuplicatePropertyObject();
    Expect<InvalidOperationException>(
        () => duplicateProperties.GetPropertyList(),
        "GetPropertyList must reject duplicate property names.");
}

static void VerifyEventConnections()
{
    using var source = new TestObject();
    var duplicateCalls = 0;
    Action<ElectronObject> duplicateHandler = _ => duplicateCalls++;

    source.ScriptChanged += duplicateHandler;
    source.ScriptChanged += duplicateHandler;
    source.AnnounceScriptChanged();
    Require(duplicateCalls == 2, "Adding the same C# event handler twice must deliver it twice.");

    source.ScriptChanged -= duplicateHandler;
    source.AnnounceScriptChanged();
    Require(duplicateCalls == 3, "Removing one duplicate subscription must leave one occurrence connected.");

    source.ScriptChanged -= duplicateHandler;
    source.AnnounceScriptChanged();
    Require(duplicateCalls == 3, "Removing both duplicate subscriptions must disconnect the handler.");

    var reusableCalls = 0;
    var reusable = EventConnection.Subscribe<ElectronObject>(
        handler => source.ScriptChanged += handler,
        handler => source.ScriptChanged -= handler,
        sender =>
        {
            Require(ReferenceEquals(sender, source), "A managed subscription must preserve typed event arguments.");
            reusableCalls++;
        });

    Require(reusable.IsConnected, "A new managed subscription must be connected.");
    source.AnnounceScriptChanged();
    reusable.Dispose();
    reusable.Dispose();
    source.AnnounceScriptChanged();
    Require(reusableCalls == 1 && !reusable.IsConnected,
        "Disposal must idempotently disconnect a reusable subscription.");

    var oneShotCalls = 0;
    using var oneShot = EventConnection.Subscribe<ElectronObject>(
        handler => source.ScriptChanged += handler,
        handler => source.ScriptChanged -= handler,
        _ =>
        {
            oneShotCalls++;
            source.AnnounceScriptChanged();
        },
        oneShot: true);

    source.AnnounceScriptChanged();
    source.AnnounceScriptChanged();
    Require(oneShotCalls == 1 && !oneShot.IsConnected,
        "A one-shot subscription must disconnect before its handler and reject re-entrant delivery.");

    using var throwingSource = new TestObject();
    var throwingCalls = 0;
    using var throwingOneShot = EventConnection.Subscribe<ElectronObject>(
        handler => throwingSource.ScriptChanged += handler,
        handler => throwingSource.ScriptChanged -= handler,
        _ =>
        {
            throwingCalls++;
            throw new InvalidOperationException("expected one-shot failure");
        },
        oneShot: true);

    Require(Capture(throwingSource.AnnounceScriptChanged) is InvalidOperationException,
        "A synchronous handler exception must propagate to the event publisher.");
    Require(Capture(throwingSource.AnnounceScriptChanged) is null && throwingCalls == 1 && !throwingOneShot.IsConnected,
        "A throwing one-shot handler must remain disconnected.");

    var concurrentSource = new EventSource();
    var concurrentCalls = 0;
    using var concurrentOneShot = EventConnection.Subscribe(
        handler => concurrentSource.Pulse += handler,
        handler => concurrentSource.Pulse -= handler,
        () => Interlocked.Increment(ref concurrentCalls),
        oneShot: true);

    Parallel.For(0, 1_000, _ => concurrentSource.RaisePulse());
    Require(concurrentCalls == 1 && !concurrentOneShot.IsConnected,
        "Concurrent emissions must consume a one-shot connection exactly once.");

    var root = new Node { Name = "event-root" };
    using var tree = new SceneTree(root);
    var deferredSource = new EventSource();
    var deferredValues = new List<int>();

    using var deferred = EventConnection.Subscribe<int>(
        handler => deferredSource.Value += handler,
        handler => deferredSource.Value -= handler,
        value => deferredValues.Add(value),
        defer: tree.Defer);

    deferredSource.RaiseValue(1);
    deferredSource.RaiseValue(2);
    Require(deferredValues.Count == 0, "Deferred subscriptions must not invoke handlers during event emission.");
    tree.FlushDeferred();
    Require(deferredValues.SequenceEqual([1, 2]), "Deferred subscriptions must capture event arguments at emission time.");

    deferredSource.RaiseValue(3);
    deferred.Dispose();
    tree.FlushDeferred();
    Require(deferredValues.SequenceEqual([1, 2]),
        "Disposing a deferred subscription must cancel callbacks that have not started.");

    var deferredOneShotCalls = 0;
    using var deferredOneShot = EventConnection.Subscribe<int>(
        handler => deferredSource.Value += handler,
        handler => deferredSource.Value -= handler,
        _ => deferredOneShotCalls++,
        oneShot: true,
        defer: tree.Defer);

    deferredSource.RaiseValue(4);
    deferredSource.RaiseValue(5);
    Require(!deferredOneShot.IsConnected && deferredOneShotCalls == 0,
        "A deferred one-shot subscription must disconnect on its first accepted emission.");
    tree.FlushDeferred();
    Require(deferredOneShotCalls == 1, "A deferred one-shot subscription must invoke exactly once at the safe point.");

    var cancelledOneShotCalls = 0;
    var cancelledOneShot = EventConnection.Subscribe<int>(
        handler => deferredSource.Value += handler,
        handler => deferredSource.Value -= handler,
        _ => cancelledOneShotCalls++,
        oneShot: true,
        defer: tree.Defer);

    deferredSource.RaiseValue(6);
    cancelledOneShot.Dispose();
    tree.FlushDeferred();
    Require(cancelledOneShotCalls == 0, "Disposal must cancel an accepted deferred one-shot callback before it starts.");

    var deferredFailureCalls = 0;
    using var deferredFailure = EventConnection.Subscribe<int>(
        handler => deferredSource.Value += handler,
        handler => deferredSource.Value -= handler,
        _ =>
        {
            deferredFailureCalls++;
            throw new InvalidOperationException("expected deferred failure");
        },
        oneShot: true,
        defer: tree.Defer);

    deferredSource.RaiseValue(7);
    Require(Capture(tree.FlushDeferred) is AggregateException && deferredFailureCalls == 1 && !deferredFailure.IsConnected,
        "Deferred handler failures must be reported by the scheduler without reconnecting one-shot subscriptions.");

    Action? partiallyAttached = null;
    Expect<InvalidOperationException>(
        () => EventConnection.Subscribe(
            handler =>
            {
                partiallyAttached += handler;
                throw new InvalidOperationException("expected subscription failure");
            },
            handler => partiallyAttached -= handler,
            () => { }),
        "A failing subscription accessor must propagate its exception.");
    Require(partiallyAttached is null, "A partially attached subscription must be rolled back.");

    var subscriptionAndRollbackFailure = Capture(() => EventConnection.Subscribe(
        _ => throw new InvalidOperationException("expected subscription failure"),
        _ => throw new InvalidOperationException("expected rollback failure"),
        () => { }));
    Require(subscriptionAndRollbackFailure is AggregateException { InnerExceptions.Count: 2 },
        "Subscription and rollback failures must both be preserved.");

    Action? removalFailureSource = null;
    var removalFailureCalls = 0;
    var removalFailure = EventConnection.Subscribe(
        handler => removalFailureSource += handler,
        _ => throw new InvalidOperationException("expected removal failure"),
        () => removalFailureCalls++);

    Expect<InvalidOperationException>(removalFailure.Dispose,
        "A failing removal accessor must propagate its exception.");
    removalFailureSource?.Invoke();
    Require(!removalFailure.IsConnected && removalFailureCalls == 0,
        "A connection must remain terminal and inert when its removal accessor fails.");

    Action? oneShotRemovalFailureSource = null;
    var oneShotRemovalFailureCalls = 0;
    using var oneShotRemovalFailure = EventConnection.Subscribe(
        handler => oneShotRemovalFailureSource += handler,
        _ => throw new InvalidOperationException("expected one-shot removal failure"),
        () => oneShotRemovalFailureCalls++,
        oneShot: true);
    Require(Capture(() => oneShotRemovalFailureSource?.Invoke()) is InvalidOperationException,
        "A one-shot removal failure must propagate before the user handler runs.");
    Require(Capture(() => oneShotRemovalFailureSource?.Invoke()) is null &&
            !oneShotRemovalFailure.IsConnected &&
            oneShotRemovalFailureCalls == 0,
        "A one-shot connection must remain consumed and inert when removal fails.");

    var schedulingSource = new EventSource();
    using var schedulingFailure = EventConnection.Subscribe(
        handler => schedulingSource.Pulse += handler,
        handler => schedulingSource.Pulse -= handler,
        () => { },
        defer: _ => throw new InvalidOperationException("expected scheduler failure"));
    Require(Capture(schedulingSource.RaisePulse) is InvalidOperationException && schedulingFailure.IsConnected,
        "A reusable connection must remain connected when its scheduler rejects one emission.");
    schedulingFailure.Dispose();

    using var oneShotSchedulingFailure = EventConnection.Subscribe(
        handler => schedulingSource.Pulse += handler,
        handler => schedulingSource.Pulse -= handler,
        () => { },
        oneShot: true,
        defer: _ => throw new InvalidOperationException("expected one-shot scheduler failure"));
    Require(Capture(schedulingSource.RaisePulse) is InvalidOperationException && !oneShotSchedulingFailure.IsConnected,
        "A one-shot connection must remain consumed when its scheduler rejects delivery.");

    var pairSource = new EventSource();
    EventSource? observedSource = null;
    var observedValue = 0;
    using var pairConnection = EventConnection.Subscribe<EventSource, int>(
        handler => pairSource.Pair += handler,
        handler => pairSource.Pair -= handler,
        (sender, value) =>
        {
            observedSource = sender;
            observedValue = value;
        });
    pairSource.RaisePair(42);
    Require(ReferenceEquals(observedSource, pairSource) && observedValue == 42,
        "Two-argument subscriptions must preserve sender-first event arguments.");

    Expect<ArgumentNullException>(
        () => EventConnection.Subscribe(null!, _ => { }, () => { }),
        "Subscribe must reject a null add accessor.");
    Expect<ArgumentNullException>(
        () => EventConnection.Subscribe(_ => { }, null!, () => { }),
        "Subscribe must reject a null remove accessor.");
    Expect<ArgumentNullException>(
        () => EventConnection.Subscribe(_ => { }, _ => { }, null!),
        "Subscribe must reject a null handler.");
}

static void VerifyTranslations()
{
    var previousCulture = TranslationServer.Culture;
    var previousEnabled = TranslationServer.Enabled;

    try
    {
        TranslationServer.Clear();
        TranslationServer.Enabled = true;
        TranslationServer.Culture = CultureInfo.GetCultureInfo("fr-FR");
        TranslationServer.AddTranslation(CultureInfo.GetCultureInfo("fr"), "game", "Hello", "Bonjour");
        TranslationServer.AddPluralTranslation(
            CultureInfo.GetCultureInfo("fr"),
            "game",
            "apple",
            "apples",
            count => count > 1 ? "pommes" : "pomme");

        using var instance = new TestObject { TranslationDomain = "game" };

        Require(instance.Tr("Hello") == "Bonjour", "Tr must use parent-culture fallback.");
        Require(instance.Tr("Unknown") == "Unknown", "Tr must return the source message when no translation exists.");
        Require(instance.TrN("apple", "apples", 0) == "pomme", "TrN must use the registered culture-specific selector.");
        Require(instance.TrN("apple", "apples", 2) == "pommes", "TrN must resolve plural messages.");

        instance.CanTranslateMessages = false;
        Require(instance.Tr("Hello") == "Hello", "Per-object translation disabling must return the source message.");
    }
    finally
    {
        TranslationServer.Clear();
        TranslationServer.Culture = previousCulture;
        TranslationServer.Enabled = previousEnabled;
    }
}

static void VerifyNodeHierarchyAndTransforms()
{
    var root = new TransformNode
    {
        Name = "root",
        Position = new Vector2(10f, 5f),
        RotationDegrees = 90f
    };
    var first = new TransformNode { Name = "first", Position = new Vector2(2f, 0f) };
    var second = new TransformNode { Name = "second", Position = new Vector2(-3f, 1f) };
    var mover = new TransformNode { Name = "mover", Position = new Vector2(4f, 2f) };

    var addedChildren = new List<(Node Source, Node Child)>();
    root.ChildAdded += (source, child) => addedChildren.Add((source, child));

    root.AddChild(first);
    root.AddChild(second);
    first.AddChild(mover);

    Require(addedChildren.SequenceEqual([(root, first), (root, second)]),
        "ChildAdded must provide the publishing parent before the added child.");

    var enteredChildren = new List<(Node Source, Node Child)>();
    root.ChildEnteredTree += (source, child) => enteredChildren.Add((source, child));

    using var tree = new SceneTree(root);

    Require(enteredChildren.SequenceEqual([(root, first), (root, second)]),
        "ChildEnteredTree must provide the publishing parent before the entering child.");

    Require(VectorNearlyEqual(first.GlobalPosition, new Vector2(10f, 7f)), "A child global position must include its parent transform.");
    Require(VectorNearlyEqual(first.ToGlobal(Vector2.Zero), first.GlobalPosition), "ToGlobal must transform the local origin.");
    Require(VectorNearlyEqual(first.ToLocal(first.ToGlobal(new Vector2(3f, -2f))), new Vector2(3f, -2f)),
        "ToLocal must invert ToGlobal.");

    first.GlobalPosition = new Vector2(4f, 9f);
    Require(VectorNearlyEqual(first.Position, new Vector2(4f, 6f)), "Setting GlobalPosition must solve the local position.");

    first.Scale = new Vector2(2f, 3f);
    first.Skew = 0.2f;
    first.Rotation = 0.4f;
    var composed = first.Transform;
    first.Transform = composed;
    Require(VectorNearlyEqual(first.Scale, new Vector2(2f, 3f)) && NearlyEqual(first.Rotation, 0.4f) && NearlyEqual(first.Skew, 0.2f),
        "Transform decomposition must preserve rotation, scale, and skew.");

    first.NotifyLocalTransformChanges = true;
    first.NotifyTransformChanges = true;
    mover.NotifyTransformChanges = true;
    first.Notifications.Clear();
    mover.Notifications.Clear();
    first.Position += Vector2.One;
    Require(first.Notifications.Contains(Node.NotificationLocalTransformChanged) &&
            first.Notifications.Contains(Node.NotificationTransformChanged) &&
            mover.Notifications.Contains(Node.NotificationTransformChanged),
        "A local transform change must notify the node and affected descendants.");

    mover.TopLevel = true;
    mover.Notifications.Clear();
    first.Position += Vector2.One;
    Require(!mover.Notifications.Contains(Node.NotificationTransformChanged),
        "A top-level node must ignore ancestor transform changes.");
    mover.TopLevel = false;

    var beforeReparent = mover.GlobalTransform;
    mover.Reparent(second, keepGlobalTransform: true);
    Require(MatrixNearlyEqual(mover.GlobalTransform, beforeReparent), "Reparent must preserve the global transform by default.");
    Require(root.GetNode("second/mover") == mover && root.GetNode("/root/second/mover") == mover,
        "Relative and absolute paths must resolve the same node.");
    Require(root.GetPathTo(mover) == "second/mover" && mover.GetPath() == "/root/second/mover",
        "Node paths must describe the hierarchy.");
    Require(root.FindChild("MOV*") == mover && mover.FindParent("SEC*") == second,
        "Wildcard hierarchy search must find descendants and parents case-insensitively.");

    mover.AddToGroup("actors");
    Require(tree.GetFirstNodeInGroup("actors") == mover && tree.GetNodesInGroup("actors").SequenceEqual([mover]),
        "SceneTree group queries must return members in tree order.");
    Require(mover.RemoveFromGroup("actors") && !mover.IsInGroup("actors"), "Group removal must update membership.");

    var third = new Node { Name = "third" };
    second.AddSibling(third);
    root.MoveChild(third, 0);
    Require(root.GetChild(0) == third && third.GetIndex() == 0, "Sibling insertion and child reordering must be observable.");

    (Node Source, Node Child)? exitingChild = null;
    (Node Source, Node Child)? removedChild = null;
    root.ChildExitingTree += (source, child) => exitingChild = (source, child);
    root.ChildRemoved += (source, child) => removedChild = (source, child);
    Require(root.RemoveChild(third), "A direct child must be removable for event verification.");
    Require(exitingChild == (root, third) && removedChild == (root, third),
        "Child exit and removal events must provide the publishing parent before the affected child.");
    root.AddChild(third);

    using var duplicate = new Node { Name = "first" };
    Expect<InvalidOperationException>(() => root.AddChild(duplicate), "Sibling names must be unique.");

    var visibilityEvents = 0;
    mover.VisibilityChanged += _ => visibilityEvents++;
    root.Hide();
    Require(!mover.IsVisibleInTree && visibilityEvents == 1, "Ancestor visibility must affect descendants and notify them.");
    root.Show();
    Require(mover.IsVisibleInTree && visibilityEvents == 2, "Showing an ancestor must restore effective visibility.");

    root.ZIndex = 3;
    second.ZIndex = 2;
    mover.ZIndex = 1;
    Require(mover.EffectiveZIndex == 6, "Relative Z indices must accumulate through the hierarchy.");
    mover.ZAsRelative = false;
    Require(mover.EffectiveZIndex == 1, "An absolute Z index must ignore ancestors.");
    Expect<ArgumentOutOfRangeException>(() => mover.ZIndex = Node.MaximumZIndex + 1, "ZIndex must enforce its documented range.");

    var motion = new Node { Name = "motion" };
    motion.MoveLocalX(3f);
    motion.Rotate(MathF.PI / 2f);
    motion.Translate(Vector2.UnitX);
    motion.ApplyScale(new Vector2(2f, 4f));
    Require(VectorNearlyEqual(motion.Position, new Vector2(3f, 1f)) && VectorNearlyEqual(motion.Scale, new Vector2(2f, 4f)),
        "Local movement, rotation, translation, and scaling helpers must compose.");
    motion.LookAt(new Vector2(3f, 11f));
    Require(NearlyEqual(motion.GlobalRotation, MathF.PI / 2f), "LookAt must point the local +X axis at a global point.");
    motion.Scale = new Vector2(0f, 1f);
    Expect<InvalidOperationException>(() => motion.ToLocal(Vector2.Zero), "A singular transform cannot convert a global point to local space.");
    motion.Dispose();
}

static void VerifyProcessing()
{
    var log = new List<string>();
    var root = new Node { Name = "root" };
    var early = new ProcessingNode("early", log)
    {
        ProcessEnabled = true,
        PhysicsProcessEnabled = true,
        ProcessPriority = -10,
        PhysicsProcessPriority = 10,
        ProcessMode = NodeProcessMode.Always
    };
    var late = new ProcessingNode("late", log)
    {
        ProcessEnabled = true,
        PhysicsProcessEnabled = true,
        ProcessPriority = 10,
        PhysicsProcessPriority = -10
    };
    var pausedOnly = new ProcessingNode("paused", log)
    {
        ProcessEnabled = true,
        ProcessMode = NodeProcessMode.WhenPaused
    };
    var inheritedMode = new TransformNode { Name = "inherited-mode" };

    root.AddChild(early);
    root.AddChild(late);
    root.AddChild(pausedOnly);
    root.AddChild(inheritedMode);

    using var tree = new SceneTree(root);
    tree.ProcessFrame(0.25d);
    Require(log.SequenceEqual(["process:early:0.25", "process:late:0.25"]),
        "Process callbacks must use priority order and skip WhenPaused nodes while running.");

    log.Clear();
    tree.PhysicsFrame(0.5d);
    Require(log.SequenceEqual(["physics:late:0.5", "physics:early:0.5"]),
        "Physics callbacks must use their independent priority order.");

    log.Clear();
    tree.Paused = true;
    tree.ProcessFrame(0.125d);
    Require(log.SequenceEqual(["process:early:0.125", "process:paused:0.125"]),
        "Pause-aware processing must honor Always, Pausable, and WhenPaused modes.");
    Require(DoubleNearlyEqual(early.ProcessDeltaTime, 0.125d), "ProcessDeltaTime must retain the delivered frame delta.");
    Expect<ArgumentOutOfRangeException>(() => tree.ProcessFrame(double.NaN), "Frame delta must be finite.");

    inheritedMode.Notifications.Clear();
    root.ProcessMode = NodeProcessMode.Disabled;
    Require(inheritedMode.Notifications.Contains(Node.NotificationDisabled) && !inheritedMode.CanProcess(),
        "Disabling an inherited process mode must notify and disable affected descendants.");
    root.ProcessMode = NodeProcessMode.Inherit;
    Require(inheritedMode.Notifications.Contains(Node.NotificationEnabled),
        "Restoring an inherited process mode must notify affected descendants.");
}

static void VerifySceneTree()
{
    var manualLifecycle = new List<string>();
    var manuallyNotifiedRoot = new RecordingNode("manual", manualLifecycle);
    manuallyNotifiedRoot.Notify(Node.NotificationReady);
    using (var manuallyNotifiedTree = new SceneTree(manuallyNotifiedRoot))
    {
        Require(manualLifecycle.Count(item => item == "ready:manual") == 2,
            "Manual lifecycle notification must not consume SceneTree's one-shot ready state.");
    }

    var queuedRoot = new Node();
    queuedRoot.QueueFree();
    Expect<ArgumentException>(
        () => new SceneTree(queuedRoot),
        "SceneTree must reject a root already queued for deletion.");
    queuedRoot.CancelFree();
    queuedRoot.Dispose();

    var lifecycle = new List<string>();
    var root = new RecordingNode("root", lifecycle);
    var child = new RecordingNode("child", lifecycle);
    var grandchild = new RecordingNode("grandchild", lifecycle);

    child.AddChild(grandchild);
    root.AddChild(child);

    Expect<InvalidOperationException>(
        () => grandchild.AddChild(root),
        "AddChild must reject a cycle before mutating the hierarchy.");

    using var tree = new SceneTree(root);

    Expect<InvalidOperationException>(root.Dispose, "An active SceneTree root must be disposed through its owning tree.");
    Require(!root.IsDisposed && ReferenceEquals(tree.Root, root), "Rejected root disposal must leave tree ownership intact.");

    using (var otherTree = new SceneTree(new Node { Name = "other-root" }))
    {
        Expect<InvalidOperationException>(
            () => root.AddChild(otherTree.Root),
            "A SceneTree root must not be reparented into another tree.");

        Require(otherTree.Root.Parent is null && ReferenceEquals(otherTree.Root.Tree, otherTree),
            "Rejected cross-tree parenting must not mutate either tree.");
    }

    Require(lifecycle.SequenceEqual(
    [
        "enter:root",
        "enter:child",
        "enter:grandchild",
        "ready:grandchild",
        "ready:child",
        "ready:root"
    ]), "SceneTree lifecycle order must match the documented parent-enter/child-ready order.");

    Require(root.RemoveChild(child), "RemoveChild must detach a direct child.");
    Require(lifecycle.TakeLast(2).SequenceEqual(["exit:grandchild", "exit:child"]),
        "Removing a subtree must deliver exit child-first.");
    root.AddChild(child);
    Require(lifecycle.Count(item => item == "ready:child") == 1 && lifecycle.Count(item => item == "ready:grandchild") == 1,
        "Ready must run only once when a subtree is detached and reattached.");
    child.RequestReady();
    root.RemoveChild(child);
    root.AddChild(child);
    Require(lifecycle.Count(item => item == "ready:child") == 2 && lifecycle.Count(item => item == "ready:grandchild") == 1,
        "RequestReady must re-arm only the requested node for its next attachment.");

    var firstBatch = 0;
    var secondBatch = 0;
    var deferredValue = 0;

    tree.Defer(() =>
    {
        firstBatch++;
        tree.Defer(() => secondBatch++);
    });
    tree.SetDeferred(value => deferredValue = value, 42);
    tree.FlushDeferred();

    Require(firstBatch == 1 && secondBatch == 0 && deferredValue == 42,
        "FlushDeferred must execute one captured batch and typed deferred setters.");

    tree.FlushDeferred();
    Require(secondBatch == 1, "Work deferred during a flush must run in the next batch.");

    var wrongThreadFlush = Task.Run(() => Capture(tree.FlushDeferred)).GetAwaiter().GetResult();
    Require(wrongThreadFlush is InvalidOperationException, "FlushDeferred must reject a non-owner thread.");

    var wrongThreadDispose = Task.Run(() => Capture(tree.Dispose)).GetAwaiter().GetResult();
    Require(wrongThreadDispose is InvalidOperationException && !tree.IsDisposed,
        "SceneTree disposal must reject a non-owner thread before disposal starts.");

    var wrongThreadNodeDispose = Task.Run(() => Capture(child.Dispose)).GetAwaiter().GetResult();
    Require(wrongThreadNodeDispose is InvalidOperationException && !child.IsDisposed,
        "Attached Node disposal must reject a non-owner thread before disposal starts.");

    var directLifecycle = new List<string>();
    var directRoot = new RecordingNode("direct-root", directLifecycle);
    var directlyDisposed = new RecordingNode("direct-child", directLifecycle);
    directRoot.AddChild(directlyDisposed);
    using (var directTree = new SceneTree(directRoot))
    {
        directLifecycle.Clear();
        directlyDisposed.Dispose();
        Require(directlyDisposed.IsDisposed && directRoot.ChildCount == 0 && directLifecycle.SequenceEqual(["exit:direct-child"]),
            "Direct disposal must detach an attached node while allowing its exit callback to inspect node state.");
    }

    child.QueueFree();
    Require(child.IsQueuedForDeletion && child.CancelFree(), "CancelFree must cancel queued deletion.");
    tree.FlushDeferred();
    Require(!child.IsDisposed && ReferenceEquals(child.Parent, root), "Cancelled deletion must preserve the node.");

    child.QueueFree();
    tree.FlushDeferred();
    Require(child.IsDisposed && grandchild.IsDisposed, "Queued deletion must dispose the complete subtree.");
    Require(root.Children.Count == 0, "Queued deletion must remove the node from its parent.");
}

static void VerifySceneTreeGroupsEventsAndTimers()
{
    var groupLog = new List<string>();
    var root = new GroupNode("root", groupLog);
    var child = new GroupNode("child", groupLog);
    var grandchild = new GroupNode("grandchild", groupLog);
    child.AddChild(grandchild);
    root.AddChild(child);

    foreach (var node in new[] { root, child, grandchild })
        node.AddToGroup("actors");

    using var tree = new SceneTree(root);
    var treeProperties = tree.GetPropertyList().Select(property => property.Name).ToHashSet(StringComparer.Ordinal);
    Require(treeProperties.IsSupersetOf([nameof(SceneTree.Root), nameof(SceneTree.Paused), nameof(SceneTree.NodeCount),
        nameof(SceneTree.ProcessFrameCount), nameof(SceneTree.PhysicsFrameCount), nameof(SceneTree.HasDeferredWork)]),
        "SceneTree must expose its typed public state through property descriptors.");
    Require(tree.NodeCount == 3 && tree.GetNodeCountInGroup("actors") == 3 && tree.HasGroup("actors"),
        "SceneTree counts and group presence must reflect the active hierarchy.");

    tree.CallGroup("actors", node => groupLog.Add($"call:{node.Name}"));
    Require(groupLog.SequenceEqual(["call:root", "call:child", "call:grandchild"]),
        "Immediate group calls must follow hierarchy order.");

    groupLog.Clear();
    tree.CallGroup("actors", node => groupLog.Add($"reverse:{node.Name}"), GroupCallFlags.Reverse);
    Require(groupLog.SequenceEqual(["reverse:grandchild", "reverse:child", "reverse:root"]),
        "Reverse group calls must visit descendants before ancestors.");

    tree.SetGroup("actors", static (node, visible) => node.Visible = visible, false);
    Require(tree.GetNodesInGroup("actors").All(node => !node.Visible),
        "Typed group setters must apply the captured value to every current member.");

    tree.NotifyGroup("actors", 9_001);
    Require(groupLog.TakeLast(3).SequenceEqual(["notify:root:9001", "notify:child:9001", "notify:grandchild:9001"]),
        "Group notifications must use hierarchy order.");

    groupLog.Clear();
    Action<Node> uniqueCall = node => groupLog.Add($"unique:{node.Name}");
    var uniqueFlags = GroupCallFlags.Deferred | GroupCallFlags.Unique;
    tree.CallGroup("actors", uniqueCall, uniqueFlags);
    tree.CallGroup("actors", uniqueCall, uniqueFlags);
    tree.FlushDeferred();
    Require(groupLog.SequenceEqual(["unique:root", "unique:child", "unique:grandchild"]),
        "Equal unique deferred group calls must execute only once.");
    Expect<ArgumentException>(
        () => tree.CallGroup("actors", uniqueCall, GroupCallFlags.Unique),
        "Unique group calls must require deferred scheduling.");

    var nodeEvents = new List<string>();
    var treeChanges = 0;
    tree.NodeAdded += (_, node) => nodeEvents.Add($"added:{node.Name}");
    tree.NodeRemoved += (_, node) => nodeEvents.Add($"removed:{node.Name}");
    tree.NodeRenamed += (_, node) => nodeEvents.Add($"renamed:{node.Name}");
    tree.TreeChanged += _ => treeChanges++;

    var added = new Node { Name = "added" };
    var addedChild = new Node { Name = "added-child" };
    added.AddChild(addedChild);
    root.AddChild(added);
    added.Name = "renamed";
    root.RemoveChild(added);

    Require(nodeEvents.SequenceEqual(["added:added", "added:added-child", "renamed:renamed", "removed:added-child", "removed:renamed"]),
        "Tree node events must describe parent-first entry, rename, and child-first exit.");
    Require(treeChanges == 3, "TreeChanged must report insertion, rename, and removal once each.");
    added.Dispose();

    var frameStarts = new List<string>();
    tree.ProcessFrameStarted += _ => frameStarts.Add("process");
    tree.PhysicsFrameStarted += _ => frameStarts.Add("physics");
    tree.ProcessFrame(0.01d);
    tree.PhysicsFrame(0.02d);
    Require(frameStarts.SequenceEqual(["process", "physics"]) &&
            tree.ProcessFrameCount == 1 && tree.PhysicsFrameCount == 1,
        "Frame events and counters must advance once per valid frame attempt.");

    var timeoutCount = 0;
    var pausableTimer = tree.CreateTimer(0.5d, processAlways: false);
    var timerProperties = pausableTimer.GetPropertyList().Select(property => property.Name).ToHashSet(StringComparer.Ordinal);
    Require(timerProperties.IsSupersetOf([nameof(SceneTreeTimer.TimeLeft), nameof(SceneTreeTimer.ProcessAlways),
        nameof(SceneTreeTimer.ProcessInPhysics)]),
        "SceneTreeTimer must expose its typed public state through property descriptors.");
    pausableTimer.Timeout += _ => timeoutCount++;
    tree.Paused = true;
    tree.ProcessFrame(0.5d);
    Require(timeoutCount == 0 && DoubleNearlyEqual(pausableTimer.TimeLeft, 0.5d),
        "A pause-aware timer must not advance while the tree is paused.");
    tree.Paused = false;
    tree.ProcessFrame(0.25d);
    tree.ProcessFrame(0.25d);
    Require(timeoutCount == 1 && pausableTimer.IsDisposed,
        "A timer must emit once and dispose itself when its delay reaches zero.");

    var physicsTimeouts = 0;
    var physicsTimer = tree.CreateTimer(0.1d, processInPhysics: true);
    physicsTimer.Timeout += _ => physicsTimeouts++;
    tree.ProcessFrame(1d);
    Require(physicsTimeouts == 0, "A physics timer must ignore process frames.");
    tree.PhysicsFrame(0.1d);
    Require(physicsTimeouts == 1 && physicsTimer.IsDisposed, "A physics timer must expire in the physics lane.");

    var laterTimerRan = false;
    tree.CreateTimer(0d).Timeout += _ => throw new InvalidOperationException("expected timer failure");
    tree.CreateTimer(0d).Timeout += _ => laterTimerRan = true;
    Require(Capture(() => tree.ProcessFrame(0d)) is AggregateException && laterTimerRan,
        "A failing timeout handler must not stop later timers or the frame safe point.");
    Expect<ArgumentOutOfRangeException>(() => tree.CreateTimer(double.PositiveInfinity),
        "Timer duration must be finite.");

    using var queuedObject = new TestObject();
    tree.QueueDelete(queuedObject);
    tree.FlushDeferred();
    Require(queuedObject.IsDisposed, "QueueDelete must dispose a detached engine object in the deletion phase.");

    tree.Defer(tree.FlushDeferred);
    Require(Capture(tree.FlushDeferred) is AggregateException && !tree.IsDisposed,
        "Re-entrant flush must be rejected without poisoning the tree.");
    tree.Defer(tree.Dispose);
    Require(Capture(tree.FlushDeferred) is AggregateException && !tree.IsDisposed,
        "Disposal from a frame or flush callback must be rejected before lifetime changes.");
}

static void VerifySceneTreeFailureSafety()
{
    var enterRoot = new FailingLifecycleNode
    {
        Name = "enter-root",
        AddChildOnEnter = true,
        CreateTimerOnEnter = true,
        ThrowOnEnter = true
    };
    var enterChild = new Node { Name = "enter-child" };
    enterRoot.AddChild(enterChild);
    Require(Capture(() => new SceneTree(enterRoot)) is AggregateException,
        "A failing enter callback must fail construction with aggregated context.");
    Require(enterRoot.Tree is null && enterChild.Tree is null && enterRoot.AddedChild?.Tree is null &&
            enterRoot.AddedChild?.IsNodeReady == false && enterRoot.CreatedTimer?.IsDisposed == true &&
            !enterRoot.IsDisposed && !enterChild.IsDisposed,
        "Failed construction must roll back all tree membership without taking caller ownership.");
    var escapedTree = enterRoot.CapturedTree!;
    Require(escapedTree.IsDisposed && Capture(() => escapedTree.Defer(static () => { })) is ObjectDisposedException &&
            Capture(() => escapedTree.CreateTimer(1d)) is ObjectDisposedException &&
            enterRoot.TimerCreationDuringRollbackError is ObjectDisposedException,
        "A tree escaped from failed construction must be terminal before rollback callbacks can enqueue new work.");
    escapedTree.Dispose();
    Require(!enterRoot.IsDisposed && !enterChild.IsDisposed,
        "Disposing an escaped failed-construction tree must not take ownership of the caller's hierarchy.");
    enterRoot.Dispose();

    var readyRoot = new FailingLifecycleNode { Name = "ready-root", AddChildOnReady = true, ThrowOnReady = true };
    var readyChild = new Node { Name = "ready-child" };
    readyRoot.AddChild(readyChild);
    Require(Capture(() => new SceneTree(readyRoot)) is AggregateException,
        "A failing ready callback must fail construction.");
    Require(readyRoot.Tree is null && readyChild.Tree is null && readyRoot.AddedChild?.Tree is null &&
            !readyRoot.IsNodeReady && !readyChild.IsNodeReady && readyRoot.AddedChild?.IsNodeReady == false,
        "Failed ready delivery must restore ready state consumed by the activation attempt.");
    readyRoot.Dispose();

    var disposeRoot = new FailingLifecycleNode { Name = "dispose-root", ThrowOnExit = true };
    var failingChild = new FailingLifecycleNode { Name = "failing-child", ThrowOnDispose = true };
    var laterChild = new Node { Name = "later-child" };
    disposeRoot.AddChild(failingChild);
    disposeRoot.AddChild(laterChild);
    var failingTree = new SceneTree(disposeRoot);
    Require(Capture(failingTree.Dispose) is AggregateException,
        "Tree disposal must report lifecycle and descendant cleanup failures.");
    Require(failingTree.IsDisposed && disposeRoot.IsDisposed && failingChild.IsDisposed && laterChild.IsDisposed &&
            disposeRoot.Tree is null && laterChild.Tree is null && !failingTree.HasDeferredWork,
        "Tree disposal must finish every teardown stage despite user callback failures.");

    var teardownRoot = new TeardownQueueNode { Name = "teardown-root" };
    var teardownTree = new SceneTree(teardownRoot);
    teardownTree.Dispose();
    Require(teardownRoot.QueueWasRejected && teardownRoot.PauseWasRejected && teardownTree.IsDisposed &&
            !teardownTree.HasDeferredWork,
        "Work and pause mutation during teardown must be rejected instead of touching partial state.");

    var mutationRoot = new Node { Name = "mutation-root" };
    var exitingChild = new Node { Name = "exiting-child" };
    mutationRoot.AddChild(exitingChild);
    using (var mutationTree = new SceneTree(mutationRoot))
    {
        Exception? nestedRemovalError = null;
        Exception? reparentError = null;
        Exception? disposalError = null;
        exitingChild.TreeExiting += node =>
        {
            nestedRemovalError = Capture(() => mutationRoot.RemoveChild(node));
            reparentError = Capture(() => node.Reparent(mutationRoot));
            disposalError = Capture(node.Dispose);
        };

        Require(mutationRoot.RemoveChild(exitingChild), "The outer removal must complete after rejected exit re-entry.");
        Require(nestedRemovalError is InvalidOperationException && reparentError is InvalidOperationException &&
                disposalError is InvalidOperationException && exitingChild.Tree is null && exitingChild.Parent is null &&
                !exitingChild.IsDisposed,
            "Removal, reparenting, and disposal must be rejected while exit callbacks are in progress.");
        exitingChild.Dispose();
    }

    var reattachRoot = new Node { Name = "reattach-root" };
    var reattachTree = new SceneTree(reattachRoot);
    Exception? reattachError = null;
    reattachRoot.TreeExited += node => reattachError = Capture(() => new SceneTree(node));
    reattachTree.Dispose();
    Require(reattachError is AggregateException && reattachRoot.IsDisposed && reattachRoot.Tree is null,
        "A node must not re-enter another tree from its in-progress exit callback.");

    var enteringRoot = new Node { Name = "entering-root" };
    var enteringFirst = new Node { Name = "entering-first" };
    var enteringSecond = new Node { Name = "entering-second" };
    enteringRoot.AddChild(enteringFirst);
    enteringRoot.AddChild(enteringSecond);
    Exception? enteringRemovalError = null;
    enteringFirst.TreeEntered += node => enteringRemovalError = Capture(() => enteringRoot.RemoveChild(node));
    enteringFirst.TreeEntered += _ => enteringRoot.RemoveChild(enteringSecond);
    using (var enteringTree = new SceneTree(enteringRoot))
    {
        Require(enteringRemovalError is InvalidOperationException && ReferenceEquals(enteringFirst.Tree, enteringTree) &&
                enteringSecond.Tree is null && enteringSecond.Parent is null && !enteringSecond.IsNodeReady,
            "Entry re-entry must be rejected and a removed snapshot sibling must not enter or become ready.");
    }
    enteringSecond.Dispose();

    var readySnapshotRoot = new Node { Name = "ready-snapshot-root" };
    var readyFirst = new Node { Name = "ready-first" };
    var readySecond = new Node { Name = "ready-second" };
    readySnapshotRoot.AddChild(readyFirst);
    readySnapshotRoot.AddChild(readySecond);
    readyFirst.Ready += _ => readySnapshotRoot.RemoveChild(readySecond);
    using (var readySnapshotTree = new SceneTree(readySnapshotRoot))
    {
        Require(readySecond.Tree is null && readySecond.Parent is null && !readySecond.IsNodeReady,
            "A removed ready snapshot sibling must not receive ready after it leaves the tree.");
    }
    readySecond.Dispose();

    var nestedReadyRoot = new Node { Name = "nested-ready-root" };
    var nestedReadyParent = new Node { Name = "nested-ready-parent" };
    var nestedReadyChild = new Node { Name = "nested-ready-child" };
    Exception? readyParentRemovalError = null;
    Exception? readyParentDisposalError = null;
    nestedReadyParent.AddChild(nestedReadyChild);
    nestedReadyRoot.AddChild(nestedReadyParent);
    nestedReadyChild.Ready += _ =>
    {
        readyParentRemovalError = Capture(() => nestedReadyRoot.RemoveChild(nestedReadyParent));
        readyParentDisposalError = Capture(nestedReadyParent.Dispose);
    };
    using (var nestedReadyTree = new SceneTree(nestedReadyRoot))
    {
        Require(readyParentRemovalError is InvalidOperationException &&
                readyParentDisposalError is InvalidOperationException && nestedReadyParent.IsNodeReady &&
                ReferenceEquals(nestedReadyParent.Tree, nestedReadyTree),
            "A descendant ready callback must not remove or dispose its subtree root during ready delivery.");
    }

    var deletionRoot = new Node { Name = "deletion-root" };
    var queueFreeFailure = new Node { Name = "queue-free-failure" };
    var queueDeleteFailure = new Node { Name = "queue-delete-failure" };
    deletionRoot.AddChild(queueFreeFailure);
    deletionRoot.AddChild(queueDeleteFailure);
    using (var deletionTree = new SceneTree(deletionRoot))
    {
        queueFreeFailure.TreeExiting += _ => throw new InvalidOperationException("expected queued exit failure");
        queueDeleteFailure.TreeExiting += _ => throw new InvalidOperationException("expected queued exit failure");
        queueFreeFailure.QueueFree();
        deletionTree.QueueDelete(queueDeleteFailure);
        Require(Capture(deletionTree.FlushDeferred) is AggregateException && queueFreeFailure.IsDisposed &&
                queueDeleteFailure.IsDisposed && queueFreeFailure.Parent is null && queueDeleteFailure.Parent is null,
            "Queued deletion must finish disposal after detach callbacks fail.");
    }

    var oldRoot = new Node { Name = "old-root" };
    var newRoot = new Node { Name = "new-root" };
    var transferred = new Node { Name = "transferred" };
    oldRoot.AddChild(transferred);
    using (var oldTree = new SceneTree(oldRoot))
    using (var newTree = new SceneTree(newRoot))
    {
        transferred.QueueFree();
        transferred.Reparent(newRoot);
        oldTree.FlushDeferred();
        Require(!transferred.IsDisposed && transferred.IsQueuedForDeletion && ReferenceEquals(transferred.Tree, newTree),
            "A stale deletion in the old tree must not consume a request transferred to the new tree.");
        newTree.FlushDeferred();
        Require(transferred.IsDisposed && transferred.Parent is null,
            "The destination tree must execute a transferred queued deletion.");
    }

    var disposingParent = new Node { Name = "disposing-parent" };
    var disposingChild = new Node { Name = "disposing-child" };
    var lateChild = new Node { Name = "late-child" };
    Exception? disposalMutationError = null;
    disposingParent.AddChild(disposingChild);
    disposingChild.Disposed += _ => disposalMutationError = Capture(() => disposingParent.AddChild(lateChild));
    disposingParent.Dispose();
    Require(disposalMutationError is ObjectDisposedException && disposingParent.IsDisposed && disposingChild.IsDisposed &&
            lateChild.Parent is null && !lateChild.IsDisposed,
        "A disposing parent must reject re-entrant child insertion and leave the candidate detached.");
    lateChild.Dispose();

    var transferParent = new Node { Name = "transfer-parent" };
    var transferTrigger = new Node { Name = "transfer-trigger" };
    var transferCandidate = new Node { Name = "transfer-candidate" };
    var transferDestination = new Node { Name = "transfer-destination" };
    Exception? disposalTransferError = null;
    transferParent.AddChild(transferTrigger);
    transferParent.AddChild(transferCandidate);
    transferTrigger.Disposed += _ => disposalTransferError = Capture(() => transferCandidate.Reparent(transferDestination));
    transferParent.Dispose();
    Require(disposalTransferError is ObjectDisposedException && transferCandidate.IsDisposed &&
            transferDestination.ChildCount == 0,
        "A child cannot escape its disposing parent's ownership through a re-entrant reparent.");
    transferDestination.Dispose();

    var preDeleteParent = new PreDeleteReparentNode { Name = "pre-delete-parent" };
    var preDeleteChild = new Node { Name = "pre-delete-child" };
    var preDeleteDestination = new Node { Name = "pre-delete-destination" };
    preDeleteParent.Target = preDeleteChild;
    preDeleteParent.Destination = preDeleteDestination;
    preDeleteParent.AddChild(preDeleteChild);
    preDeleteParent.Dispose();
    Require(preDeleteParent.ReparentError is ObjectDisposedException && preDeleteChild.IsDisposed &&
            preDeleteDestination.ChildCount == 0,
        "Pre-delete callbacks must not transfer children out of the disposal ownership snapshot.");
    preDeleteDestination.Dispose();

    var exitOwnershipRoot = new Node { Name = "exit-ownership-root" };
    var exitMutator = new ExitSiblingMutationNode { Name = "exit-mutator" };
    var exitOwnedSibling = new Node { Name = "exit-owned-sibling" };
    var exitDestination = new Node { Name = "exit-destination" };
    exitMutator.Sibling = exitOwnedSibling;
    exitMutator.Destination = exitDestination;
    exitOwnershipRoot.AddChild(exitMutator);
    exitOwnershipRoot.AddChild(exitOwnedSibling);
    new SceneTree(exitOwnershipRoot).Dispose();
    Require(exitMutator.RemoveError is InvalidOperationException &&
            exitMutator.ReparentError is InvalidOperationException &&
            exitMutator.DisposeError is InvalidOperationException && exitOwnedSibling.IsDisposed &&
            exitDestination.ChildCount == 0,
        "Exit callbacks must not remove sibling nodes from the hierarchy owned by tree disposal.");
    exitDestination.Dispose();

    var lifecycleRoot = new Node { Name = "lifecycle-root" };
    using (var lifecycleTree = new SceneTree(lifecycleRoot))
    {
        var enteringNode = new Node { Name = "runtime-entering" };
        Exception? enterFlushError = null;
        Exception? enterDisposeError = null;
        enteringNode.TreeEntered += node =>
        {
            node.QueueFree();
            enterFlushError = Capture(lifecycleTree.FlushDeferred);
            enterDisposeError = Capture(lifecycleTree.Dispose);
        };
        lifecycleRoot.AddChild(enteringNode);
        Require(enterFlushError is InvalidOperationException && enterDisposeError is InvalidOperationException &&
                enteringNode.IsQueuedForDeletion && !lifecycleTree.IsDisposed,
            "Flush and tree disposal must be rejected during runtime entry without consuming queued deletion.");
        lifecycleTree.FlushDeferred();
        Require(enteringNode.IsDisposed, "Queued deletion from runtime entry must execute at the next safe point.");

        var exitingNode = new Node { Name = "runtime-exiting" };
        Exception? exitFlushError = null;
        lifecycleRoot.AddChild(exitingNode);
        exitingNode.TreeExiting += node =>
        {
            node.QueueFree();
            exitFlushError = Capture(lifecycleTree.FlushDeferred);
        };
        lifecycleRoot.RemoveChild(exitingNode);
        Require(exitFlushError is InvalidOperationException && exitingNode.IsQueuedForDeletion && exitingNode.Tree is null,
            "Flush must be rejected during runtime exit without consuming queued deletion.");
        lifecycleTree.FlushDeferred();
        Require(exitingNode.IsDisposed, "A queued node detached before its safe point must still be disposed.");
    }

    var pauseRoot = new ReentrantPauseNode { Name = "pause-root" };
    using (var pauseTree = new SceneTree(pauseRoot))
    {
        Require(Capture(() => pauseTree.Paused = true) is AggregateException &&
                pauseRoot.ReentryError is InvalidOperationException && pauseTree.Paused,
            "An opposite pause transition must be rejected during pause notification without corrupting final state.");
    }

    var pauseMutationRoot = new Node { Name = "pause-mutation-root" };
    var pauseMutator = new PauseMutationNode { Name = "pause-mutator" };
    var pauseRemoved = new PauseMutationNode { Name = "pause-removed" };
    pauseMutator.Target = pauseRemoved;
    pauseMutationRoot.AddChild(pauseMutator);
    pauseMutationRoot.AddChild(pauseRemoved);
    using (var pauseMutationTree = new SceneTree(pauseMutationRoot))
    {
        pauseMutationTree.Paused = true;
        Require(pauseRemoved.IsDisposed && pauseRemoved.PauseNotifications == 0,
            "Pause traversal must skip a captured node removed and disposed by an earlier notification.");
    }

    var pauseReparentRoot = new Node { Name = "pause-reparent-root" };
    var pauseReparenter = new PauseReparentNode { Name = "pause-reparenter" };
    var pauseMoved = new PauseMutationNode { Name = "pause-moved" };
    var pauseDestination = new Node { Name = "pause-destination" };
    pauseReparenter.Target = pauseMoved;
    pauseReparenter.Destination = pauseDestination;
    pauseReparentRoot.AddChild(pauseReparenter);
    pauseReparentRoot.AddChild(pauseMoved);
    pauseReparentRoot.AddChild(pauseDestination);
    using (var pauseReparentTree = new SceneTree(pauseReparentRoot))
    {
        pauseReparentTree.Paused = true;
        Require(ReferenceEquals(pauseMoved.Parent, pauseDestination) && pauseMoved.PauseNotifications == 1,
            "Pause traversal must notify a node at most once when an earlier callback reparents it into a later branch.");
    }

    var pauseBarrierRoot = new PauseBarrierNode { Name = "pause-barrier-root" };
    using (var pauseBarrierTree = new SceneTree(pauseBarrierRoot))
    {
        pauseBarrierTree.Paused = true;
        Require(pauseBarrierRoot.FlushError is InvalidOperationException &&
                pauseBarrierRoot.DisposeError is InvalidOperationException && !pauseBarrierTree.IsDisposed,
            "Flush and tree disposal must be rejected during pause notification delivery.");
    }

    for (var iteration = 0; iteration < 256; iteration++)
    {
        var queueRaceRoot = new Node { Name = $"queue-race-root-{iteration}" };
        var queueRaceChild = new Node { Name = "queue-race-child" };
        queueRaceRoot.AddChild(queueRaceChild);
        using var queueRaceTree = new SceneTree(queueRaceRoot);
        using var queueRaceStart = new ManualResetEventSlim();
        var queueTask = Task.Run(() =>
        {
            queueRaceStart.Wait();
            queueRaceChild.QueueFree();
        });
        queueRaceStart.Set();
        queueRaceTree.FlushDeferred();
        queueTask.Wait();
        queueRaceTree.FlushDeferred();
        Require(queueRaceChild.IsDisposed && !queueRaceChild.IsQueuedForDeletion,
            "Concurrent QueueFree publication and flush must not lose the deletion request.");
    }

    for (var iteration = 0; iteration < 64; iteration++)
    {
        var raceTree = new SceneTree(new Node { Name = $"race-{iteration}" });
        using var start = new ManualResetEventSlim();
        var workers = Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
        {
            start.Wait();

            try
            {
                raceTree.Defer(static () => { });
            }
            catch (ObjectDisposedException)
            {
            }
        })).ToArray();

        start.Set();
        raceTree.Dispose();
        Task.WaitAll(workers);
        Require(!raceTree.HasDeferredWork, "Concurrent enqueue/disposal must never leave accepted work stranded.");
    }
}

static void VerifyResources()
{
    using var resource = new Resource();
    Require(!resource.ResourceLocalToScene && resource.ResourceName.Length == 0 &&
            resource.ResourcePath.Length == 0 && resource.ResourceSceneUniqueId.Length == 0 && resource.IsBuiltIn,
        "A resource must start unnamed, pathless, built-in, and not local to a scene.");

    var propertyNames = resource.GetPropertyList().Select(property => property.Name).ToHashSet(StringComparer.Ordinal);
    Require(propertyNames.IsSupersetOf([
        nameof(Resource.ResourceLocalToScene),
        nameof(Resource.ResourceName),
        nameof(Resource.ResourcePath),
        nameof(Resource.ResourceSceneUniqueId)
    ]), "The typed property list must expose all resource properties.");

    var changes = 0;
    resource.Changed += sender =>
    {
        Require(ReferenceEquals(sender, resource), "Changed must identify its resource.");
        changes++;
    };
    resource.ResourceName = "data";
    resource.ResourceName = "data";
    resource.ResourceLocalToScene = true;
    resource.ResourceSceneUniqueId = "Data_42";
    Require(changes == 2, "Every resource-name assignment, and no configuration-only assignment, must emit Changed.");

    Expect<ArgumentNullException>(() => resource.ResourceName = null!, "A resource name must reject null.");
    Expect<ArgumentNullException>(() => resource.ResourceSceneUniqueId = null!, "A scene ID must reject null.");
    Expect<ArgumentException>(() => resource.ResourceSceneUniqueId = "bad-id", "A scene ID must reject punctuation.");
    Require(resource.ResourceSceneUniqueId == "Data_42", "A rejected scene ID must not change stored state.");

    var generatedIds = new string[1_024];
    Parallel.For(0, generatedIds.Length, index => generatedIds[index] = Resource.GenerateSceneUniqueId());
    Require(generatedIds.All(id => id.Length == 5 && id.All(character =>
            character is >= 'a' and <= 'y' or >= '0' and <= '8')) && generatedIds.Distinct().Count() > 1,
        "Generated scene IDs must use the documented compact alphabet and be safe under concurrent calls.");

    var prefix = $"memory://resource-tests/{Guid.NewGuid():N}";
    var ownedPath = $"{prefix}/owned";
    using var contender = new Resource { ResourcePath = $"{prefix}/previous" };
    resource.ResourcePath = ownedPath;
    Expect<InvalidOperationException>(() => contender.ResourcePath = ownedPath,
        "Assigning a path owned by another live resource must fail.");
    Require(contender.ResourcePath == $"{prefix}/previous" && resource.ResourcePath == ownedPath,
        "A failed path assignment must leave both owners unchanged.");

    contender.TakeOverPath(ownedPath);
    Require(resource.ResourcePath.Length == 0 && contender.ResourcePath == ownedPath,
        "Taking over a path must atomically clear the previous owner.");

    using var rawA = new TestResource();
    using var rawB = new TestResource();
    rawA.SetPathCache($"{prefix}/raw");
    rawB.SetPathCache($"{prefix}/raw");
    Require(rawA.ResourcePath == rawB.ResourcePath && rawA.PathCacheSetCount == 1 && rawB.PathCacheSetCount == 1,
        "Raw path-cache assignment must bypass uniqueness and invoke the typed hook.");
    rawA.ResourcePath = $"{prefix}/raw";
    Expect<InvalidOperationException>(() => rawB.ResourcePath = $"{prefix}/raw",
        "Assigning an unchanged raw path must still attempt to claim cache ownership.");
    rawB.TakeOverPath($"{prefix}/raw");
    Require(rawA.ResourcePath.Length == 0 && rawB.ResourcePath == $"{prefix}/raw",
        "Taking over an unchanged raw path must transfer its registered owner.");

    resource.ResourcePath = string.Empty;
    Require(resource.IsBuiltIn, "A pathless resource must be built-in.");
    resource.ResourcePath = $"{prefix}/external";
    Require(!resource.IsBuiltIn, "A standalone external path must not be built-in.");
    resource.ResourcePath = $"{prefix}/external::nested";
    Require(resource.IsBuiltIn, "An embedded-resource path must be built-in.");
    resource.ResourcePath = $"local://{Guid.NewGuid():N}";
    Require(resource.IsBuiltIn, "A local-resource path must be built-in.");

    var released = new Resource { ResourcePath = $"{prefix}/released" };
    released.Dispose();
    using var replacement = new Resource { ResourcePath = $"{prefix}/released" };

    var racers = Enumerable.Range(0, 32).Select(_ => new Resource()).ToArray();
    var winners = 0;
    var occupiedFailures = 0;
    Parallel.ForEach(racers, candidate =>
    {
        try
        {
            candidate.ResourcePath = $"{prefix}/race";
            Interlocked.Increment(ref winners);
        }
        catch (InvalidOperationException)
        {
            Interlocked.Increment(ref occupiedFailures);
        }
    });
    Require(winners == 1 && occupiedFailures == racers.Length - 1,
        "Concurrent path claims must select exactly one owner.");
    foreach (var racer in racers)
        racer.Dispose();

    using var throwingName = new Resource();
    throwingName.Changed += _ => throw new InvalidOperationException("expected change failure");
    Require(Capture(() => throwingName.ResourceName = "committed") is InvalidOperationException &&
            throwingName.ResourceName == "committed",
        "A throwing change handler must propagate after the name is committed.");

    using var setup = new SetupProbeResource();
#pragma warning disable CS0618
    setup.SetupLocalToSceneRequested += _ => setup.Order.Add("event");
    setup.SetupLocalToScene();
#pragma warning restore CS0618
    Require(setup.Order.SequenceEqual(["event", "hook"]),
        "Scene-local setup must publish its compatibility event before the virtual hook.");

    using var failingSetup = new SetupProbeResource { ThrowInHook = true };
#pragma warning disable CS0618
    failingSetup.SetupLocalToSceneRequested += _ => throw new ArgumentException("expected event failure");
    var setupError = Capture(failingSetup.SetupLocalToScene);
#pragma warning restore CS0618
    Require(setupError is AggregateException { InnerExceptions.Count: 2 } && failingSetup.Order.SequenceEqual(["hook"]),
        "Scene-local setup must attempt the hook and aggregate failures after a throwing event.");

    using var plainDuplicate = resource.Duplicate();
    Require(plainDuplicate.GetType() == typeof(Resource) && plainDuplicate.ResourceName == resource.ResourceName &&
            plainDuplicate.ResourceLocalToScene == resource.ResourceLocalToScene &&
            plainDuplicate.ResourcePath.Length == 0 && plainDuplicate.ResourceSceneUniqueId.Length == 0,
        "A base resource duplicate must copy stored content but not path identity.");

    using var root = new TestResource
    {
        ResourceName = "root",
        ResourceLocalToScene = true,
        ResourcePath = $"{prefix}/root",
        ResourceSceneUniqueId = "root_1",
        Value = 7,
        Numbers = [1, 2, 3]
    };
    using var embedded = new TestResource { Value = 11, Numbers = [4] };
    using var external = new TestResource { Value = 13, ResourcePath = $"{prefix}/child" };
    root.First = embedded;
    root.Second = embedded;
    root.External = external;
    root.Always = external;
    root.Never = embedded;
    embedded.First = root;

    using var shallow = (TestResource)root.Duplicate();
    Require(ReferenceEquals(shallow.Numbers, root.Numbers) && ReferenceEquals(shallow.First, embedded) &&
            shallow.Always is not null && !ReferenceEquals(shallow.Always, external) && ReferenceEquals(shallow.Never, embedded) &&
            shallow.ResourcePath.Length == 0 && shallow.ResourceSceneUniqueId.Length == 0,
        "Shallow duplication must honor default, forced, and never-duplicate typed properties while clearing identity.");

    using var containerDeep = (TestResource)root.DuplicateDeep(DeepDuplicateMode.None);
    Require(!ReferenceEquals(containerDeep.Numbers, root.Numbers) && containerDeep.Numbers.SequenceEqual(root.Numbers) &&
            ReferenceEquals(containerDeep.First, embedded) && ReferenceEquals(containerDeep.External, external) &&
            containerDeep.Always is not null && !ReferenceEquals(containerDeep.Always, external) &&
            ReferenceEquals(containerDeep.Never, embedded),
        "Deep duplication with None must clone containers, share default resources, and honor explicit overrides.");

    using var internalDeep = (TestResource)root.Duplicate(deep: true);
    Require(internalDeep.First is not null && !ReferenceEquals(internalDeep.First, embedded) &&
            ReferenceEquals(internalDeep.First, internalDeep.Second) && ReferenceEquals(internalDeep.First.First, internalDeep) &&
            ReferenceEquals(internalDeep.External, external) && internalDeep.Always is not null &&
            !ReferenceEquals(internalDeep.Always, external) && ReferenceEquals(internalDeep.Never, embedded) &&
            !ReferenceEquals(internalDeep.First.Numbers, embedded.Numbers),
        "Internal deep duplication must preserve aliases and cycles while sharing external nested resources.");

    using var allDeep = (TestResource)root.DuplicateDeep(DeepDuplicateMode.All);
    Require(allDeep.External is not null && !ReferenceEquals(allDeep.External, external) &&
            ReferenceEquals(allDeep.Always, allDeep.External) && ReferenceEquals(allDeep.Never, embedded) &&
            allDeep.External.ResourcePath.Length == 0,
        "All-mode deep duplication must duplicate external resources once, honor never-copy fields, and clear path identity.");
    Expect<ArgumentOutOfRangeException>(() => root.DuplicateDeep((DeepDuplicateMode)99),
        "Deep duplication must reject unknown policies.");

    using var copySource = new TestResource
    {
        ResourceName = "source",
        ResourceLocalToScene = true,
        ResourceSceneUniqueId = "source_1",
        Value = 21,
        Numbers = [8, 9],
        First = embedded
    };
    using var copyTarget = new TestResource
    {
        ResourceName = "target",
        ResourcePath = $"{prefix}/copy-target",
        ResourceSceneUniqueId = "target_1",
        Transient = 99
    };
    var copyChanges = 0;
    copyTarget.Changed += _ => copyChanges++;
    copyTarget.CopyFromResource(copySource);
    Require(copyTarget.ResourceName == "source" && copyTarget.ResourceLocalToScene && copyTarget.Value == 21 &&
            ReferenceEquals(copyTarget.Numbers, copySource.Numbers) && ReferenceEquals(copyTarget.First, embedded) &&
            copyTarget.ResourcePath == $"{prefix}/copy-target" && copyTarget.ResourceSceneUniqueId == "target_1" &&
            copyTarget.Transient == 0 && copyTarget.ResetCount == 1 && copyChanges == 1,
        "CopyFromResource must reset state, shallow-copy stored data, preserve target identity, and coalesce changes.");
    copyTarget.CopyFromResource(copyTarget);
    Require(copyChanges == 1, "Copying a resource from itself must be a no-op.");
    Expect<ArgumentException>(() => copyTarget.CopyFromResource(resource),
        "CopyFromResource must require the exact same runtime type.");

    using var concurrentCopySource = new Resource { ResourceName = "concurrent-source" };
    using var concurrentCopyTarget = new Resource();
    var concurrentCopyChanges = 0;
    concurrentCopyTarget.Changed += _ => Interlocked.Increment(ref concurrentCopyChanges);
    Parallel.For(0, 256, _ => concurrentCopyTarget.CopyFromResource(concurrentCopySource));
    Require(concurrentCopyChanges == 256,
        "Concurrent copy batches must serialize and publish one coalesced change per operation.");

    using var resetSource = new ResetFailureResource();
    using var resetTarget = new ResetFailureResource { ThrowOnReset = true };
    var failedCopyChanges = 0;
    resetTarget.Changed += _ => failedCopyChanges++;
    Require(Capture(() => resetTarget.CopyFromResource(resetSource)) is InvalidOperationException && failedCopyChanges == 1,
        "A failed non-transactional copy must still report a possibly partial state change exactly once.");

    using var dualFailureTarget = new ResetFailureResource { ThrowOnReset = true };
    dualFailureTarget.Changed += _ => throw new ArgumentException("expected change failure");
    Require(Capture(() => dualFailureTarget.CopyFromResource(resetSource)) is AggregateException { InnerExceptions.Count: 2 },
        "CopyFromResource must aggregate operation and final change-handler failures.");

    using var unsupported = new UnsupportedResource();
    Expect<NotSupportedException>(() => unsupported.Duplicate(),
        "A derived resource without explicit duplication hooks must not silently lose custom state.");

    using var wrongFactory = new WrongFactoryResource();
    WrongFactoryResource.LastCreated = null;
    Expect<InvalidOperationException>(() => wrongFactory.Duplicate(),
        "A duplication factory must return the exact source runtime type.");
    Require(WrongFactoryResource.LastCreated is { IsDisposed: true },
        "A rejected factory result must be disposed during duplication rollback.");

    using var dirtyCopy = new DirtyCopyResource();
    DirtyCopyResource.LastCreated = null;
    Expect<InvalidOperationException>(() => dirtyCopy.Duplicate(),
        "A custom copier must not assign external identity to a duplicate.");
    Require(DirtyCopyResource.LastCreated is { IsDisposed: true },
        "A duplicate that violates post-copy identity must be disposed during rollback.");

    using var selfFactory = new SelfFactoryResource();
    Expect<InvalidOperationException>(() => selfFactory.Duplicate(),
        "A duplication factory must not return its source.");
    Require(!selfFactory.IsDisposed, "Rejecting a self-returning factory must not dispose the source.");

    FailingDuplicateResource.Created.Clear();
    using var failingChild = new FailingDuplicateResource();
    using var failingRoot = new FailingDuplicateResource { Child = failingChild, ThrowOnCopy = true };
    Require(Capture(() => failingRoot.DuplicateDeep(DeepDuplicateMode.All)) is InvalidOperationException &&
            FailingDuplicateResource.Created.Count == 2 && FailingDuplicateResource.Created.All(item => item.IsDisposed),
        "A failed graph duplication must dispose every partially created resource.");

    using var cleanupFailure = new CleanupFailureResource();
    var cleanupFailureError = Capture(() => cleanupFailure.Duplicate());
    Require(cleanupFailureError is AggregateException { InnerExceptions.Count: 2 } &&
            CleanupFailureResource.LastCreated is { IsDisposed: true },
        "Duplication must aggregate its original failure with cleanup failures after finalizing partial targets.");

    var disposablePath = $"{prefix}/disposed";
    var disposable = new Resource { ResourcePath = disposablePath };
    disposable.Dispose();
    Expect<ObjectDisposedException>(disposable.EmitChanged, "Disposed resources must reject change publication.");
    using var afterDispose = new Resource { ResourcePath = disposablePath };
}

static void VerifyPackedScenes()
{
    var prefix = $"memory://packed-scenes/{Guid.NewGuid():N}";
    using var scene = new PackedScene { ResourcePath = $"{prefix}/main.scene" };
    var liveState = scene.GetState();
    Require(!scene.CanInstantiate() && liveState.GetNodeCount() == 0 &&
            ReferenceEquals(liveState, scene.GetState()) && liveState.GetPath() == scene.ResourcePath,
        "A new packed scene must expose one live empty state with its resource path.");
    Expect<InvalidOperationException>(() => scene.Instantiate(),
        "An empty packed scene must not instantiate.");
    Expect<ArgumentOutOfRangeException>(() => scene.Instantiate((PackedSceneEditState)99),
        "Packed-scene instantiation must reject unknown edit states.");
    Expect<NotSupportedException>(() => scene.Instantiate(PackedSceneEditState.Instance),
        "Runtime packed-scene instantiation must reject editor-only modes.");

    using var nestedLocal = new PackedTestResource
    {
        ResourceLocalToScene = true,
        ResourcePath = $"{prefix}/nested.resource",
        Value = 31
    };
    using var local = new PackedTestResource
    {
        ResourceLocalToScene = true,
        Value = 17,
        Child = nestedLocal
    };

    var root = new PackedTestNode { Name = "Root", Value = 7, Data = local, TranslationDomain = "scene" };
    var child = new PackedTestNode { Name = "Child", Value = 11, Data = local };
    var grandchild = new PackedTestNode { Name = "Grandchild", Value = 13 };
    var unowned = new PackedTestNode { Name = "RuntimeOnly", Value = 99 };
    var prunedGrandchild = new PackedTestNode { Name = "Pruned", Value = 101 };
    root.AddChild(child);
    child.Owner = root;
    child.AddChild(grandchild);
    grandchild.Owner = root;
    root.AddChild(unowned);
    unowned.AddChild(prunedGrandchild);
    prunedGrandchild.Owner = root;
    child.AddToGroup("persistent", persistent: true);
    child.AddToGroup("runtime-only");

    scene.Pack(root);
    Require(scene.CanInstantiate() && liveState.GetNodeCount() == 3 &&
            liveState.GetNodeName(0) == "Root" && liveState.GetNodeName(1) == "Child" &&
            liveState.GetNodeName(2) == "Grandchild" && liveState.GetNodePath(0) == "." &&
            liveState.GetNodePath(2) == "Child/Grandchild" && liveState.GetNodePath(2, forParent: true) == "Child" &&
            liveState.GetNodeOwnerPath(0).Length == 0 && liveState.GetNodeOwnerPath(1) == "." &&
            liveState.GetNodeGroups(1).SequenceEqual(["persistent"]) && liveState.GetNodeIndex(1) == -1 &&
            liveState.GetNodeInstance(1) is null && !liveState.IsNodeInstancePlaceholder(1) &&
            liveState.GetNodeInstancePlaceholder(1).Length == 0 && liveState.GetBaseSceneState() is null &&
            liveState.GetConnectionCount() == 0,
        "Scene state must expose the owned DFS hierarchy and its runtime-authored metadata.");
    var valuePropertyIndex = Enumerable.Range(0, liveState.GetNodePropertyCount(1))
        .Single(index => liveState.GetNodePropertyName(1, index) == nameof(PackedTestNode.Value));
    Require(liveState.GetNodePropertyType(1, valuePropertyIndex) == typeof(int) &&
            liveState.GetNodePropertyValue<int>(1, valuePropertyIndex) == 11 &&
            liveState.GetNodeType(1) == nameof(PackedTestNode),
        "Scene state must expose strongly typed stored properties and node types.");
    Expect<InvalidCastException>(() => liveState.GetNodePropertyValue<string>(1, valuePropertyIndex),
        "Scene state must reject an incompatible requested property type.");
    Expect<ArgumentOutOfRangeException>(() => liveState.GetNodeName(3),
        "Scene state must reject an invalid node index.");
    Expect<ArgumentOutOfRangeException>(() => liveState.GetNodePropertyName(1, 999),
        "Scene state must reject an invalid property index.");

    Expect<ArgumentNullException>(() => scene.Pack(null!),
        "Packing a null root must be rejected.");
    Require(scene.CanInstantiate() && liveState.GetNodeCount() == 3,
        "Rejecting a null pack root must preserve the previous scene.");
    root.Dispose();

    using (var instance = (PackedTestNode)scene.Instantiate())
    {
        var instanceChild = (PackedTestNode)instance.Children[0];
        var instanceGrandchild = (PackedTestNode)instanceChild.Children[0];
        Require(instance.Parent is null && instance.Tree is null && instance.Name == "Root" && instance.Value == 7 &&
                instance.TranslationDomain == "scene" && instance.SceneFilePath == scene.ResourcePath &&
                instance.SceneNotifications == 1 && instanceChild.SceneNotifications == 0 &&
                instanceGrandchild.SceneNotifications == 0 && ReferenceEquals(instanceChild.Owner, instance) &&
                ReferenceEquals(instanceGrandchild.Owner, instance) && instanceChild.IsInGroup("persistent") &&
                !instanceChild.IsInGroup("runtime-only") && instance.FindChild("RuntimeOnly") is null,
            "Instantiation must restore properties, owners, persistent groups, source path, and root-only notification.");
        Require(instance.Data is not null && instanceChild.Data is not null &&
                ReferenceEquals(instance.Data, instanceChild.Data) && !ReferenceEquals(instance.Data, local) &&
                instance.Data.Value == 17 && instance.Data.SetupCount == 1 &&
                ReferenceEquals(instance.Data.GetLocalScene(), instance) && instance.Data.Child is not null &&
                !ReferenceEquals(instance.Data.Child, nestedLocal) && instance.Data.Child.Value == 31 &&
                instance.Data.Child.SetupCount == 1 && ReferenceEquals(instance.Data.Child.GetLocalScene(), instance),
            "Scene-local resource duplication must preserve aliases, include nested external local resources, and set up once.");
    }
    Require(local.SetupCount == 0 && nestedLocal.SetupCount == 0,
        "Instantiating a scene must not set up source resources.");

    using (var second = (PackedTestNode)scene.Instantiate())
    {
        Require(second.Data is not null && !ReferenceEquals(second.Data, local),
            "Each scene instance must receive an independent scene-local resource graph.");
    }

    using (var duplicate = (PackedScene)scene.Duplicate())
    using (var duplicateInstance = (PackedTestNode)duplicate.Instantiate())
    {
        Require(duplicate.CanInstantiate() && duplicateInstance.Value == 7 && duplicate.GetState().GetPath().Length == 0,
            "Packed-scene duplication must preserve immutable scene data while clearing resource identity.");
    }

    using (var failingLocal = new PackedTestResource { ResourceLocalToScene = true, ThrowOnSetup = true })
    {
        var failingSetupRoot = new PackedTestNode { Name = "FailingSetup", Data = failingLocal };
        scene.Pack(failingSetupRoot);
        failingSetupRoot.Dispose();
        PackedTestResource.Created.Clear();
        Expect<AggregateException>(() => scene.Instantiate(),
            "A scene-local setup failure must abort instantiation.");
        Require(PackedTestResource.Created.Count == 1 && PackedTestResource.Created.All(resource => resource.IsDisposed),
            "A scene-local setup failure must dispose every partial resource duplicate.");
    }

    var replacementRoot = new PackedTestNode { Name = "Replacement", Value = 23 };
    scene.Pack(replacementRoot);
    replacementRoot.Dispose();
    Require(liveState.GetNodeCount() == 1 && liveState.GetNodeName(0) == "Replacement",
        "A previously returned live scene state must observe a successful repack.");

    var unsupportedRoot = new PackedTestNode { Name = "UnsupportedRoot" };
    var unsupportedChild = new UnsupportedPackedNode { Name = "UnsupportedChild" };
    unsupportedRoot.AddChild(unsupportedChild);
    unsupportedChild.Owner = unsupportedRoot;
    Expect<NotSupportedException>(() => scene.Pack(unsupportedRoot),
        "Packing must reject a derived node that has no explicit reusable factory.");
    Require(!scene.CanInstantiate() && liveState.GetNodeCount() == 0,
        "A failure after capture starts must leave the packed scene empty.");
    unsupportedRoot.Dispose();

    var movingRoot = new MovingCaptureNode { Name = "Moving" };
    using var destination = new Node { Name = "Destination" };
    MovingCaptureNode.Destination = destination;
    Expect<InvalidOperationException>(() => scene.Pack(movingRoot),
        "A stored-property getter must not move a captured node through another parent.");
    Require(movingRoot.Parent is null && destination.Children.Count == 0 && !scene.CanInstantiate(),
        "Rejected capture-time hierarchy mutation must leave both hierarchies and packed state unchanged.");
    movingRoot.Dispose();
    MovingCaptureNode.Destination = null;

    var capturingFactoryRoot = new CapturingPackedFactoryNode { Name = "CapturingFactory" };
    Expect<InvalidOperationException>(() => scene.Pack(capturingFactoryRoot),
        "Packing must reject a scene factory that captures source state.");
    Require(!scene.CanInstantiate(),
        "Rejecting a capturing scene factory must leave the packed scene empty.");
    capturingFactoryRoot.Dispose();

    var wrongFactoryRoot = new WrongPackedFactoryNode { Name = "WrongFactory" };
    scene.Pack(wrongFactoryRoot);
    wrongFactoryRoot.Dispose();
    WrongPackedFactoryNode.LastCreated = null;
    Expect<InvalidOperationException>(() => scene.Instantiate(),
        "Instantiation must reject a factory result of the wrong runtime type.");
    Require(WrongPackedFactoryNode.LastCreated is { IsDisposed: true },
        "Instantiation rollback must dispose a rejected factory result.");

    var activeFactorySource = new ActiveFactoryPackedNode { Name = "ActiveFactory" };
    scene.Pack(activeFactorySource);
    activeFactorySource.Dispose();
    ActiveFactoryPackedNode.ActivationError = null;
    ActiveFactoryPackedNode.ExistingTreeActivationError = null;
    var activeFactoryDestination = new Node { Name = "ActiveFactoryDestination" };
    using (var activeFactoryTree = new SceneTree(activeFactoryDestination))
    {
        ActiveFactoryPackedNode.Destination = activeFactoryDestination;
        using var activeFactoryInstance = scene.Instantiate();
        Require(ActiveFactoryPackedNode.ActivationError is InvalidOperationException &&
                ActiveFactoryPackedNode.ExistingTreeActivationError is AggregateException existingTreeError &&
                existingTreeError.Flatten().InnerExceptions.Any(error => error is InvalidOperationException) &&
                activeFactoryInstance is ActiveFactoryPackedNode { EnterCount: 0, Parent: null, Tree: null } &&
                activeFactoryDestination.Children.Count == 0,
            "A node factory must not activate its result in a new or existing tree before returning it.");
        ActiveFactoryPackedNode.Destination = null;
    }

    var escapingSource = new EscapingPackedNode { Name = "Escaping" };
    using var escapeDestination = new Node { Name = "EscapeDestination" };
    scene.Pack(escapingSource);
    escapingSource.Dispose();
    EscapingPackedNode.Destination = escapeDestination;
    EscapingPackedNode.LastCreated = null;
    Expect<InvalidOperationException>(() => scene.Instantiate(),
        "Instantiation must reject a callback that moves a created root into an external hierarchy.");
    Require(escapeDestination.Children.Count == 0 && EscapingPackedNode.LastCreated is { IsDisposed: true },
        "Failed instantiation must detach and dispose a node that escaped through a callback.");
    EscapingPackedNode.Destination = null;

    var treeEscapingSource = new TreeEscapingPackedNode { Name = "TreeEscaping" };
    scene.Pack(treeEscapingSource);
    treeEscapingSource.Dispose();
    TreeEscapingPackedNode.LastCreated = null;
    Expect<InvalidOperationException>(() => scene.Instantiate(),
        "A scene-instantiation callback must not activate its unfinished root in a SceneTree.");
    Require(TreeEscapingPackedNode.LastCreated is { IsDisposed: true, Tree: null },
        "Rejecting premature SceneTree activation must leave no live escaped root.");

    var activeEscapeSource = new ActiveTreeEscapingPackedNode { Name = "ActiveTreeEscaping" };
    scene.Pack(activeEscapeSource);
    activeEscapeSource.Dispose();
    var activeEscapeRoot = new Node { Name = "ActiveEscapeDestination" };
    using (var activeEscapeTree = new SceneTree(activeEscapeRoot))
    {
        ActiveTreeEscapingPackedNode.Destination = activeEscapeRoot;
        ActiveTreeEscapingPackedNode.LastCreated = null;
        var activeEscapeError = Capture(() => scene.Instantiate());
        Require(activeEscapeError is AggregateException activeEscapeAggregate &&
                activeEscapeAggregate.Flatten().InnerExceptions.Any(error => error is InvalidOperationException) &&
                activeEscapeRoot.Children.Count == 0 &&
                ActiveTreeEscapingPackedNode.LastCreated is { IsDisposed: true, Tree: null, EnterCount: 0 },
            "An unfinished scene instance must not enter an active tree, run enter callbacks, or remain attached.");
        ActiveTreeEscapingPackedNode.Destination = null;
    }

    var returningSource = new SourceReturningPackedNode { Name = "SourceReturning" };
    SourceReturningPackedNode.Source = returningSource;
    scene.Pack(returningSource);
    Expect<InvalidOperationException>(() => scene.Instantiate(),
        "A factory must not return its captured source node.");
    Require(!returningSource.IsDisposed && returningSource.Parent is null,
        "Rejecting a source-returning factory must not dispose the source.");
    SourceReturningPackedNode.Source = null;
    returningSource.Dispose();

    var singletonSource = new SingletonPackedNode { Name = "Singleton" };
    SingletonPackedNode.Cached = null;
    scene.Pack(singletonSource);
    singletonSource.Dispose();
    using (var singletonInstance = scene.Instantiate())
    {
        Expect<InvalidOperationException>(() => scene.Instantiate(),
            "A factory must not issue the same live node to two scene instances.");
        Require(!singletonInstance.IsDisposed,
            "Rejecting a reused factory result must not dispose the already issued instance.");
    }
    SingletonPackedNode.Cached = null;

    var raceRoot = new PackedTestNode { Name = "RaceRoot", Value = 47 };
    scene.Pack(raceRoot);
    for (var index = 0; index < 64; index++)
    {
        var racedState = scene.GetState();
        Parallel.Invoke(racedState.Dispose, () => scene.Pack(raceRoot));
        Require(!scene.GetState().IsDisposed && scene.CanInstantiate(),
            "Disposing a cached scene state must not break a concurrent repack.");
    }
    Parallel.For(0, 64, index => scene.ResourcePath = $"{prefix}/concurrent-{index}.scene");
    Require(scene.GetState().GetPath() == scene.ResourcePath,
        "Concurrent path callbacks must leave scene-state path synchronized with the resource.");
    raceRoot.Dispose();

    scene.ResourcePath = $"{prefix}/final.scene";
    var survivingState = scene.GetState();
    scene.Dispose();
    Require(survivingState.GetNodeCount() == 1 && survivingState.GetPath() == $"{prefix}/final.scene",
        "An external scene-state reference must survive disposal of its packed-scene resource.");
    survivingState.Dispose();
}

static void Require(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

static bool NearlyEqual(float left, float right, float epsilon = 0.0001f) => MathF.Abs(left - right) <= epsilon;

static bool DoubleNearlyEqual(double left, double right, double epsilon = 0.0000001d) => Math.Abs(left - right) <= epsilon;

static bool VectorNearlyEqual(Vector2 left, Vector2 right, float epsilon = 0.0001f) => Vector2.Distance(left, right) <= epsilon;

static bool MatrixNearlyEqual(Matrix3x2 left, Matrix3x2 right, float epsilon = 0.0001f) =>
    NearlyEqual(left.M11, right.M11, epsilon) && NearlyEqual(left.M12, right.M12, epsilon) &&
    NearlyEqual(left.M21, right.M21, epsilon) && NearlyEqual(left.M22, right.M22, epsilon) &&
    NearlyEqual(left.M31, right.M31, epsilon) && NearlyEqual(left.M32, right.M32, epsilon);

static void Expect<TException>(Action action, string message)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException(message);
}

static Exception? Capture(Action action)
{
    try
    {
        action();
        return null;
    }
    catch (Exception error)
    {
        return error;
    }
}

sealed class CyclicConfigValue
{
    public CyclicConfigValue? Next { get; set; }
}

sealed record ConfigProfile(string Name, int Level);

static class TestNativeLinks
{
    [DllImport("libc", EntryPoint = "link", SetLastError = true)]
    internal static extern int CreateHardLink(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string existingPath,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string newPath);
}

sealed class ColorPackedNode : Node
{
    private static readonly PropertyDescriptor<ColorPackedNode, Color> TintProperty = new(
        nameof(Tint),
        node => node.Tint,
        (node, value) => node.Tint = value,
        _ => Colors.White,
        stored: true);
    private static readonly PropertyDescriptor<ColorPackedNode, Rect2> BoundsProperty = new(
        nameof(Bounds),
        node => node.Bounds,
        (node, value) => node.Bounds = value,
        _ => default,
        stored: true);
    private static readonly PropertyDescriptor<ColorPackedNode, Transform2D> TransformProperty = new(
        nameof(PackedTransform),
        node => node.PackedTransform,
        (node, value) => node.PackedTransform = value,
        _ => Transform2D.Identity,
        stored: true);

    private Color _tint = Colors.White;
    private Rect2 _bounds;
    private Transform2D _transform = Transform2D.Identity;

    public Color Tint
    {
        get => _tint;
        set
        {
            EnsureMutable();
            _tint = value;
        }
    }

    public Rect2 Bounds
    {
        get => _bounds;
        set
        {
            EnsureMutable();
            _bounds = value;
        }
    }

    public Transform2D PackedTransform
    {
        get => _transform;
        set
        {
            EnsureMutable();
            _transform = value;
        }
    }

    protected override Func<Node> CreateSceneInstanceFactory() => CreateNode;

    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Append(TintProperty).Append(BoundsProperty).Append(TransformProperty);

    private static Node CreateNode() => new ColorPackedNode();
}

sealed class PackedTestNode : Node
{
    private static readonly PropertyDescriptor<PackedTestNode, int> ValueProperty = new(
        nameof(Value),
        node => node.Value,
        (node, value) => node.Value = value,
        _ => 0,
        stored: true);
    private static readonly PropertyDescriptor<PackedTestNode, PackedTestResource?> DataProperty = new(
        nameof(Data),
        node => node.Data,
        (node, value) => node.Data = value,
        _ => null,
        stored: true);

    private int _value;
    private PackedTestResource? _data;

    public int Value
    {
        get => _value;
        set
        {
            EnsureMutable();
            _value = value;
        }
    }

    public PackedTestResource? Data
    {
        get => _data;
        set
        {
            EnsureMutable();
            _data = value;
        }
    }

    public int SceneNotifications { get; private set; }

    protected override Func<Node> CreateSceneInstanceFactory() => CreateNode;

    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Append(ValueProperty).Append(DataProperty);

    protected override void OnNotification(int what)
    {
        if (what == NotificationSceneInstantiated)
            SceneNotifications++;

        base.OnNotification(what);
    }

    private static Node CreateNode() => new PackedTestNode();
}

sealed class PackedTestResource : Resource
{
    public static List<PackedTestResource> Created { get; } = [];

    public int Value { get; set; }

    public PackedTestResource? Child { get; set; }

    public int SetupCount { get; private set; }

    public bool ThrowOnSetup { get; set; }

    protected override Resource CreateDuplicateInstance()
    {
        var resource = new PackedTestResource();
        Created.Add(resource);
        return resource;
    }

    protected override void CopyCustomStateTo(
        Resource target,
        bool deep,
        DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource,
        Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var typedTarget = (PackedTestResource)target;
        typedTarget.Value = Value;
        typedTarget.Child = (PackedTestResource?)duplicateSubresource(Child);
        typedTarget.ThrowOnSetup = ThrowOnSetup;
    }

    protected override void OnSetupLocalToScene()
    {
        SetupCount++;
        if (ThrowOnSetup)
            throw new InvalidOperationException("expected local setup failure");
    }
}

sealed class UnsupportedPackedNode : Node
{
}

sealed class MovingCaptureNode : Node
{
    private static readonly PropertyDescriptor<MovingCaptureNode, int> MovingProperty = new(
        "MovingValue",
        node => MoveDuringCapture(node),
        (node, _) => node.EnsureMutable(),
        _ => 0,
        stored: true);

    public static Node? Destination { get; set; }

    protected override Func<Node> CreateSceneInstanceFactory() => CreateNode;

    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Append(MovingProperty);

    private static int MoveDuringCapture(MovingCaptureNode node)
    {
        Destination?.AddChild(node);
        return 0;
    }

    private static Node CreateNode() => new MovingCaptureNode();
}

sealed class WrongPackedFactoryNode : Node
{
    public static Node? LastCreated { get; set; }

    protected override Func<Node> CreateSceneInstanceFactory() => CreateNode;

    private static Node CreateNode() => LastCreated = new Node();
}

sealed class CapturingPackedFactoryNode : Node
{
    protected override Func<Node> CreateSceneInstanceFactory() =>
        () => new CapturingPackedFactoryNode { Name = Name };
}

sealed class ActiveFactoryPackedNode : Node
{
    public static Exception? ActivationError { get; set; }

    public static Node? Destination { get; set; }

    public static Exception? ExistingTreeActivationError { get; set; }

    public int EnterCount { get; private set; }

    protected override Func<Node> CreateSceneInstanceFactory() => CreateNode;

    private static Node CreateNode()
    {
        var node = new ActiveFactoryPackedNode();
        ActivationError = Capture(() => _ = new SceneTree(node));
        ExistingTreeActivationError = Capture(() => Destination?.AddChild(node));
        node.Parent?.RemoveChild(node);
        return node;
    }

    protected override void OnEnterTree() => EnterCount++;

    private static Exception? Capture(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception error)
        {
            return error;
        }
    }
}

sealed class EscapingPackedNode : Node
{
    public static Node? Destination { get; set; }

    public static EscapingPackedNode? LastCreated { get; set; }

    protected override Func<Node> CreateSceneInstanceFactory() => CreateNode;

    protected override void OnNotification(int what)
    {
        if (what == NotificationSceneInstantiated)
            Destination?.AddChild(this);

        base.OnNotification(what);
    }

    private static Node CreateNode() => LastCreated = new EscapingPackedNode();
}

sealed class SourceReturningPackedNode : Node
{
    public static SourceReturningPackedNode? Source { get; set; }

    protected override Func<Node> CreateSceneInstanceFactory() => CreateNode;

    private static Node CreateNode() => Source!;
}

sealed class TreeEscapingPackedNode : Node
{
    public static TreeEscapingPackedNode? LastCreated { get; set; }

    protected override Func<Node> CreateSceneInstanceFactory() => CreateNode;

    protected override void OnNotification(int what)
    {
        if (what == NotificationSceneInstantiated)
            _ = new SceneTree(this);

        base.OnNotification(what);
    }

    private static Node CreateNode() => LastCreated = new TreeEscapingPackedNode();
}

sealed class ActiveTreeEscapingPackedNode : Node
{
    public static Node? Destination { get; set; }

    public static ActiveTreeEscapingPackedNode? LastCreated { get; set; }

    public int EnterCount { get; private set; }

    protected override Func<Node> CreateSceneInstanceFactory() => CreateNode;

    protected override void OnNotification(int what)
    {
        if (what == NotificationSceneInstantiated)
            Destination?.AddChild(this);

        base.OnNotification(what);
    }

    protected override void OnEnterTree() => EnterCount++;

    private static Node CreateNode() => LastCreated = new ActiveTreeEscapingPackedNode();
}

sealed class SingletonPackedNode : Node
{
    public static SingletonPackedNode? Cached { get; set; }

    protected override Func<Node> CreateSceneInstanceFactory() => CreateNode;

    private static Node CreateNode() => Cached ??= new SingletonPackedNode();
}

sealed class TestResource : Resource
{
    public int Value { get; set; }

    public List<int> Numbers { get; set; } = [];

    public TestResource? First { get; set; }

    public TestResource? Second { get; set; }

    public TestResource? External { get; set; }

    public TestResource? Always { get; set; }

    public TestResource? Never { get; set; }

    public int Transient { get; set; }

    public int ResetCount { get; private set; }

    public int PathCacheSetCount { get; private set; }

    protected override Resource CreateDuplicateInstance() => new TestResource();

    protected override void CopyCustomStateTo(
        Resource target,
        bool deep,
        DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource,
        Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var typedTarget = (TestResource)target;
        typedTarget.Value = Value;
        typedTarget.Numbers = deep ? [.. Numbers] : Numbers;
        typedTarget.First = (TestResource?)duplicateSubresource(First);
        typedTarget.Second = (TestResource?)duplicateSubresource(Second);
        typedTarget.External = (TestResource?)duplicateSubresource(External);
        typedTarget.Always = (TestResource?)forceDuplicateSubresource(Always);
        typedTarget.Never = Never;
    }

    protected override void OnResetState()
    {
        ResetCount++;
        Transient = 0;
    }

    protected override void OnPathCacheSet(string path) => PathCacheSetCount++;
}

sealed class SetupProbeResource : Resource
{
    public List<string> Order { get; } = [];

    public bool ThrowInHook { get; init; }

    protected override void OnSetupLocalToScene()
    {
        Order.Add("hook");
        if (ThrowInHook)
            throw new InvalidOperationException("expected setup failure");
    }
}

sealed class ResetFailureResource : Resource
{
    public bool ThrowOnReset { get; init; }

    protected override void OnResetState()
    {
        if (ThrowOnReset)
            throw new InvalidOperationException("expected reset failure");
    }
}

sealed class UnsupportedResource : Resource
{
}

sealed class WrongFactoryResource : Resource
{
    public static Resource? LastCreated { get; set; }

    protected override Resource CreateDuplicateInstance() => LastCreated = new Resource();

    protected override void CopyCustomStateTo(
        Resource target,
        bool deep,
        DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource,
        Func<Resource?, Resource?> forceDuplicateSubresource)
    {
    }
}

sealed class SelfFactoryResource : Resource
{
    protected override Resource CreateDuplicateInstance() => this;

    protected override void CopyCustomStateTo(
        Resource target,
        bool deep,
        DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource,
        Func<Resource?, Resource?> forceDuplicateSubresource)
    {
    }
}

sealed class DirtyCopyResource : Resource
{
    public static DirtyCopyResource? LastCreated { get; set; }

    protected override Resource CreateDuplicateInstance() => LastCreated = new DirtyCopyResource();

    protected override void CopyCustomStateTo(
        Resource target,
        bool deep,
        DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource,
        Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        target.ResourcePath = $"memory://dirty-copy/{Guid.NewGuid():N}";
    }
}

sealed class FailingDuplicateResource : Resource
{
    public static List<FailingDuplicateResource> Created { get; } = [];

    public FailingDuplicateResource? Child { get; set; }

    public bool ThrowOnCopy { get; init; }

    protected override Resource CreateDuplicateInstance()
    {
        var created = new FailingDuplicateResource();
        Created.Add(created);
        return created;
    }

    protected override void CopyCustomStateTo(
        Resource target,
        bool deep,
        DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource,
        Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        ((FailingDuplicateResource)target).Child = (FailingDuplicateResource?)duplicateSubresource(Child);
        if (ThrowOnCopy)
            throw new InvalidOperationException("expected duplication failure");
    }
}

sealed class CleanupFailureResource : Resource
{
    public static CleanupFailureResource? LastCreated { get; private set; }

    public bool ThrowOnDispose { get; init; }

    protected override Resource CreateDuplicateInstance() =>
        LastCreated = new CleanupFailureResource { ThrowOnDispose = true };

    protected override void CopyCustomStateTo(
        Resource target,
        bool deep,
        DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource,
        Func<Resource?, Resource?> forceDuplicateSubresource) =>
        throw new InvalidOperationException("expected duplication failure");

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && ThrowOnDispose)
            throw new ArgumentException("expected cleanup failure");
    }
}

sealed class EventSource
{
    public event Action? Pulse;

    public event Action<int>? Value;

    public event Action<EventSource, int>? Pair;

    public void RaisePulse() => Pulse?.Invoke();

    public void RaiseValue(int value) => Value?.Invoke(value);

    public void RaisePair(int value) => Pair?.Invoke(this, value);
}

sealed class TestMainLoop : MainLoop
{
    public bool DisposeDuringFinalize { get; set; }

    public bool DisposeDuringInitialize { get; set; }

    public bool DisposeDuringProcess { get; set; }

    public Exception? DisposeError { get; private set; }

    public int FinalizeCount { get; private set; }

    public bool FinalizeDuringProcess { get; set; }

    public Exception? FinalizeError { get; private set; }

    public Exception? FinalizeDisposeError { get; private set; }

    public Exception? InitializeDisposeError { get; private set; }

    public Exception? InitializeReentryError { get; private set; }

    public List<string> Log { get; } = [];

    public List<int> Notifications { get; } = [];

    public bool PhysicsResult { get; set; }

    public bool ProcessResult { get; set; }

    public bool ReenterInitialize { get; set; }

    public bool ReenterProcess { get; set; }

    public Exception? ReentryError { get; private set; }

    public bool ThrowOnFinalize { get; set; }

    public bool ThrowOnInitialize { get; set; }

    public bool ThrowOnProcess { get; set; }

    public void PublishPermission(string permission, bool granted) => NotifyRequestPermissionsResult(permission, granted);

    protected override void OnInitialize()
    {
        Log.Add("initialize");

        if (ReenterInitialize)
            InitializeReentryError = Capture(Initialize);

        if (DisposeDuringInitialize)
            InitializeDisposeError = Capture(Dispose);

        if (ThrowOnInitialize)
            throw new InvalidOperationException("expected initialization failure");
    }

    protected override bool OnProcess(double delta)
    {
        Log.Add($"process:{delta}");

        if (ReenterProcess)
            ReentryError = Capture(() => Process(delta));

        if (DisposeDuringProcess)
            DisposeError = Capture(Dispose);

        if (FinalizeDuringProcess)
            FinalizeError = Capture(FinalizeLoop);

        if (ThrowOnProcess)
            throw new InvalidOperationException("expected process failure");

        return ProcessResult;
    }

    protected override bool OnPhysicsProcess(double delta)
    {
        Log.Add($"physics:{delta}");
        return PhysicsResult;
    }

    protected override void OnFinalize()
    {
        FinalizeCount++;
        Log.Add("finalize");

        if (DisposeDuringFinalize)
            FinalizeDisposeError = Capture(Dispose);

        if (ThrowOnFinalize)
            throw new InvalidOperationException("expected finalization failure");
    }

    protected override void OnNotification(int what)
    {
        Notifications.Add(what);
        base.OnNotification(what);
    }

    private static Exception? Capture(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception error)
        {
            return error;
        }
    }
}

sealed class EmptyMainLoop : MainLoop
{
}

sealed class EngineProbeMainLoop : MainLoop
{
    public Exception? AdvanceReentryError { get; private set; }

    public int FinalizeCount { get; private set; }

    public int InitializeCount { get; private set; }

    public MainLoop? MainLoopDuringFinalize { get; private set; }

    public MainLoop? MainLoopDuringInitialize { get; private set; }

    public List<string> Order { get; } = [];

    public bool ObservedPhysicsFrameState { get; private set; }

    public bool ObservedProcessFrameState { get; private set; }

    public List<double> PhysicsDeltas { get; } = [];

    public bool PhysicsResult { get; set; }

    public List<double> ProcessDeltas { get; } = [];

    public bool ProcessResult { get; set; }

    public bool ReenterEngine { get; set; }

    public bool ReenterDuringFinalize { get; set; }

    public bool ReenterDuringInitialize { get; set; }

    public Exception? StartDuringFinalizeError { get; private set; }

    public Exception? StartDuringInitializeError { get; private set; }

    public Exception? StopDuringFinalizeError { get; private set; }

    public Exception? StopReentryError { get; private set; }

    public bool ThrowOnFinalize { get; set; }

    public bool ThrowOnInitialize { get; set; }

    public bool ThrowOnPhysics { get; set; }

    public bool ThrowOnProcess { get; set; }

    protected override void OnInitialize()
    {
        InitializeCount++;
        MainLoopDuringInitialize = Engine.Instance.MainLoop;

        if (ReenterDuringInitialize)
            StartDuringInitializeError = Capture(() => Engine.Instance.Start(this));

        if (ThrowOnInitialize)
            throw new InvalidOperationException("expected engine initialization failure");
    }

    protected override bool OnPhysicsProcess(double delta)
    {
        PhysicsDeltas.Add(delta);
        Order.Add("physics");
        ObservedPhysicsFrameState |= Engine.Instance.IsInPhysicsFrame;

        if (ThrowOnPhysics)
            throw new InvalidOperationException("expected engine physics failure");

        return PhysicsResult;
    }

    protected override bool OnProcess(double delta)
    {
        ProcessDeltas.Add(delta);
        Order.Add("process");
        ObservedProcessFrameState |= Engine.Instance.IsInPhysicsFrame;

        if (ReenterEngine)
        {
            AdvanceReentryError = Capture(() => Engine.Instance.AdvanceFrame(0d));
            StopReentryError = Capture(Engine.Instance.Stop);
        }

        if (ThrowOnProcess)
            throw new InvalidOperationException("expected engine process failure");

        return ProcessResult;
    }

    protected override void OnFinalize()
    {
        FinalizeCount++;
        MainLoopDuringFinalize = Engine.Instance.MainLoop;

        if (ReenterDuringFinalize)
        {
            StartDuringFinalizeError = Capture(() => Engine.Instance.Start(this));
            StopDuringFinalizeError = Capture(Engine.Instance.Stop);
        }

        if (ThrowOnFinalize)
            throw new InvalidOperationException("expected engine finalization failure");
    }

    private static Exception? Capture(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception error)
        {
            return error;
        }
    }
}

sealed class SystemNotificationNode : Node
{
    private readonly List<string> _log;

    public SystemNotificationNode(string name, List<string> log)
    {
        Name = name;
        _log = log;
    }

    public bool ThrowOnSystem { get; init; }

    protected override void OnNotification(int what)
    {
        if (what is >= MainLoop.NotificationOsMemoryWarning and <= MainLoop.NotificationApplicationPipModeExited)
        {
            _log.Add($"{Name}:{what}");

            if (ThrowOnSystem)
                throw new InvalidOperationException("expected system notification failure");
        }

        base.OnNotification(what);
    }
}

sealed class FinalizeOnEnterNode : Node
{
    public Exception? FinalizeError { get; private set; }

    protected override void OnEnterTree()
    {
        FinalizeError = Capture(Tree!.FinalizeLoop);
        base.OnEnterTree();
    }

    private static Exception? Capture(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception error)
        {
            return error;
        }
    }
}

sealed class TestObject : ElectronObject
{
    private static readonly PropertyDescriptor<TestObject, int> ValueProperty = new(
        nameof(Value),
        instance => instance.Value,
        (instance, value) => instance.Value = value,
        _ => 0,
        (_, value) => value >= 0);

    private int _disposeCount;

    public int DisposeCount => Volatile.Read(ref _disposeCount);

    public bool CanTranslateDuringPreDelete { get; private set; }

    public List<int> Notifications { get; } = [];

    public int Value { get; private set; }

    public void Use() => ThrowIfDisposed();

    public void AnnouncePropertyListChanged() => NotifyPropertyListChanged();

    public void AnnounceScriptChanged() => NotifyScriptChanged();

    protected override void OnNotification(int what)
    {
        if (what == NotificationPreDelete)
            CanTranslateDuringPreDelete = CanTranslateMessages;

        Notifications.Add(what);
        base.OnNotification(what);
    }

    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Append(ValueProperty);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            Interlocked.Increment(ref _disposeCount);

        base.Dispose(disposing);
    }
}

sealed class RecordingNode : Node
{
    private readonly List<string> _lifecycle;

    public RecordingNode(string name, List<string> lifecycle)
    {
        Name = name;
        _lifecycle = lifecycle;
    }

    protected override void OnEnterTree() => _lifecycle.Add($"enter:{Name}");

    protected override void OnExitTree() => _lifecycle.Add($"exit:{Name}");

    protected override void OnReady() => _lifecycle.Add($"ready:{Name}");
}

sealed class DuplicatePropertyObject : ElectronObject
{
    private static readonly PropertyDescriptor<DuplicatePropertyObject, int> First = new("Duplicate", _ => 1);
    private static readonly PropertyDescriptor<DuplicatePropertyObject, int> Second = new("Duplicate", _ => 2);

    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Append(First).Append(Second);
}

sealed class TransformNode : Node
{
    public List<int> Notifications { get; } = [];

    protected override void OnNotification(int what)
    {
        Notifications.Add(what);
        base.OnNotification(what);
    }
}

sealed class ProcessingNode : Node
{
    private readonly List<string> _log;

    public ProcessingNode(string name, List<string> log)
    {
        Name = name;
        _log = log;
    }

    protected override void OnProcess(double delta) => _log.Add($"process:{Name}:{delta}");

    protected override void OnPhysicsProcess(double delta) => _log.Add($"physics:{Name}:{delta}");
}

sealed class GroupNode : Node
{
    private readonly List<string> _log;

    public GroupNode(string name, List<string> log)
    {
        Name = name;
        _log = log;
    }

    protected override void OnNotification(int what)
    {
        if (what == 9_001)
            _log.Add($"notify:{Name}:{what}");

        base.OnNotification(what);
    }
}

sealed class FailingLifecycleNode : Node
{
    public bool AddChildOnEnter { get; init; }

    public bool AddChildOnReady { get; init; }

    public bool CreateTimerOnEnter { get; init; }

    public bool ThrowOnEnter { get; init; }

    public bool ThrowOnReady { get; init; }

    public bool ThrowOnExit { get; init; }

    public bool ThrowOnDispose { get; init; }

    public Node? AddedChild { get; private set; }

    public SceneTreeTimer? CreatedTimer { get; private set; }

    public SceneTree? CapturedTree { get; private set; }

    public Exception? TimerCreationDuringRollbackError { get; private set; }

    protected override void OnEnterTree()
    {
        CapturedTree = Tree;

        if (CreateTimerOnEnter)
        {
            CreatedTimer = Tree!.CreateTimer(1d);
            CreatedTimer.Disposed += _ => TimerCreationDuringRollbackError = CaptureTimerCreation();
        }

        if (AddChildOnEnter)
        {
            AddedChild = new Node { Name = "added-during-enter" };
            AddChild(AddedChild);
        }

        if (ThrowOnEnter)
            throw new InvalidOperationException("expected enter failure");
    }

    private Exception? CaptureTimerCreation()
    {
        try
        {
            CapturedTree!.CreateTimer(1d);
            return null;
        }
        catch (Exception error)
        {
            return error;
        }
    }

    protected override void OnReady()
    {
        if (AddChildOnReady)
        {
            AddedChild = new Node { Name = "added-during-ready" };
            AddChild(AddedChild);
        }

        if (ThrowOnReady)
            throw new InvalidOperationException("expected ready failure");
    }

    protected override void OnExitTree()
    {
        if (ThrowOnExit)
            throw new InvalidOperationException("expected exit failure");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && ThrowOnDispose)
            throw new InvalidOperationException("expected node disposal failure");
    }
}

sealed class TeardownQueueNode : Node
{
    public bool QueueWasRejected { get; private set; }

    public bool PauseWasRejected { get; private set; }

    protected override void OnExitTree()
    {
        try
        {
            Tree!.Defer(static () => { });
        }
        catch (ObjectDisposedException)
        {
            QueueWasRejected = true;
        }

        try
        {
            Tree!.Paused = true;
        }
        catch (ObjectDisposedException)
        {
            PauseWasRejected = true;
        }
    }
}

sealed class ReentrantPauseNode : Node
{
    public Exception? ReentryError { get; private set; }

    protected override void OnNotification(int what)
    {
        if (what == NotificationPaused)
        {
            try
            {
                Tree!.Paused = false;
            }
            catch (Exception error)
            {
                ReentryError = error;
                throw;
            }
        }

        base.OnNotification(what);
    }
}

sealed class PauseMutationNode : Node
{
    public Node? Target { get; set; }

    public int PauseNotifications { get; private set; }

    protected override void OnNotification(int what)
    {
        if (what == NotificationPaused)
        {
            PauseNotifications++;
            Target?.Dispose();
        }

        base.OnNotification(what);
    }
}

sealed class PauseReparentNode : Node
{
    public Node? Target { get; set; }

    public Node? Destination { get; set; }

    protected override void OnNotification(int what)
    {
        if (what == NotificationPaused)
            Target!.Reparent(Destination!);

        base.OnNotification(what);
    }
}

sealed class PauseBarrierNode : Node
{
    public Exception? FlushError { get; private set; }

    public Exception? DisposeError { get; private set; }

    protected override void OnNotification(int what)
    {
        if (what == NotificationPaused)
        {
            FlushError = Capture(Tree!.FlushDeferred);
            DisposeError = Capture(Tree.Dispose);
        }

        base.OnNotification(what);
    }

    private static Exception? Capture(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception error)
        {
            return error;
        }
    }
}

sealed class PreDeleteReparentNode : Node
{
    public Node? Target { get; set; }

    public Node? Destination { get; set; }

    public Exception? ReparentError { get; private set; }

    protected override void OnNotification(int what)
    {
        if (what == NotificationPreDelete)
        {
            try
            {
                Target!.Reparent(Destination!);
            }
            catch (Exception error)
            {
                ReparentError = error;
            }
        }

        base.OnNotification(what);
    }
}

sealed class ExitSiblingMutationNode : Node
{
    public Node? Sibling { get; set; }

    public Node? Destination { get; set; }

    public Exception? RemoveError { get; private set; }

    public Exception? ReparentError { get; private set; }

    public Exception? DisposeError { get; private set; }

    protected override void OnExitTree()
    {
        RemoveError = Capture(() => Parent!.RemoveChild(Sibling!));
        ReparentError = Capture(() => Sibling!.Reparent(Destination!));
        DisposeError = Capture(Sibling!.Dispose);
        base.OnExitTree();
    }

    private static Exception? Capture(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception error)
        {
            return error;
        }
    }
}
