using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
namespace Electron2D;

public partial class RichTextLabel
{
    private static readonly Regex IntegerExpression = new("^[-+]?\\d+$", RegexOptions.CultureInvariant);
    private static readonly Regex DecimalExpression = new("^[+-]?\\d*(\\.\\d*)?([eE][+-]?\\d+)?$", RegexOptions.CultureInvariant);
    /// <summary>Parses named scalar and mixed-array effect arguments into a dedicated typed context.</summary><param name="expressions">Nonnull name=value expressions.</param><returns>Parsed context; malformed assignment terminates parsing after prior entries.</returns>
    public RichTextEffectEnvironment ParseExpressionsForValues(string[] expressions)
    {
        CheckRich(); ArgumentNullException.ThrowIfNull(expressions); var result = new RichTextEffectEnvironment();
        foreach (var expression in expressions) { ArgumentNullException.ThrowIfNull(expression); var parts = expression.Split('='); if (parts.Length != 2) return result; var values = parts[1].Split(',', StringSplitOptions.RemoveEmptyEntries); var parsed = new List<object>(); foreach (var value in values) { object item; if (value.Length is 4 or 7 && value[0] == '#' && Color.HTMLIsValid(value)) item = RichTextEffectEnvironment.Parsed(Color.FromHTML(value)); else if (value.StartsWith('$')) item = RichTextEffectEnvironment.Parsed(value[1..]); else if (value is "true" or "false") item = RichTextEffectEnvironment.Parsed(value == "true"); else if (IntegerExpression.IsMatch(value)) { long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number); item = RichTextEffectEnvironment.Parsed(number); } else if (DecimalExpression.IsMatch(value)) { double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number); item = RichTextEffectEnvironment.Parsed(number); } else item = RichTextEffectEnvironment.Parsed(value); parsed.Add(item); } if (parsed.Count == 1) result.StoreParsed(parts[0], parsed[0]); else if (parsed.Count > 1) result.StoreParsed(parts[0], RichTextEffectEnvironment.ParsedElements(parsed)); }
        return result;
    }
    /// <summary>Replaces runtime content with parsed markup.</summary><param name="bbcode">Nonnull markup.</param>
    public void ParseBBCode(string bbcode) { ArgumentNullException.ThrowIfNull(bbcode); MutableRich(); Clear(); AppendText(bbcode); }
    /// <summary>Appends markup; closing tags may only close tags opened in this append call.</summary><param name="bbcode">Nonnull markup.</param>
    /// <remarks>Font tags create owned variation spans borrowing Font resources, with spacing, synthetic outline, collection face, design coordinates and feature options.</remarks>
    public void AppendText(string bbcode)
    {
        MutableRich(); ArgumentNullException.ThrowIfNull(bbcode); bbcode = bbcode.Replace("\r\n", "\n"); var opened = new Stack<(string Name, int Depth)>(); var at = 0;
        while (at < bbcode.Length)
        {
            var bracket = bbcode.IndexOf('[', at); if (bracket < 0) { AddText(bbcode[at..]); break; }
            if (bracket > at) AddText(bbcode[at..bracket]); var end = FindTagEnd(bbcode, bracket + 1); if (end < 0) { AddText(bbcode[bracket..]); break; }
            var raw = bbcode[(bracket + 1)..end]; at = end + 1;
            if (raw is "lb" or "rb") { AddText(raw == "lb" ? "[" : "]"); continue; }
            if (raw.StartsWith('/')) { var closing = raw[1..]; if (opened.Count > 0 && opened.Peek().Name == closing) { var entry = opened.Pop(); while (_stack.Count > entry.Depth) Pop(); } else AddText("[" + raw + "]"); continue; }
            var tokens = SplitTag(raw); if (tokens.Count == 0) { AddText("[]"); continue; }
            var split = tokens[0].IndexOf('='); var name = split < 0 ? tokens[0] : tokens[0][..split]; var value = split < 0 ? "" : tokens[0][(split + 1)..]; value = Unquote(value); var arguments = new Dictionary<string, string>(StringComparer.Ordinal); for (var i = 1; i < tokens.Count; i++) { var equals = tokens[i].IndexOf('='); if (equals >= 0) arguments[tokens[i][..equals]] = Unquote(tokens[i][(equals + 1)..]); }
            string Arg(string key, string fallback = "") => arguments.GetValueOrDefault(key, fallback); float Number(string key, float fallback) => float.TryParse(Arg(key).TrimEnd('%').Replace("px", "", StringComparison.Ordinal), NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? n : fallback; Color Tint(string key, Color fallback) => Color.FromString(Arg(key), fallback);
            var depth = _stack.Count; var handled = true; var paired = true;
            switch (name)
            {
                case "br": Newline(); paired = false; break;
                case "char": if (int.TryParse(value, System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var scalar) && Rune.IsValid(scalar)) { AddText(new Rune(scalar).ToString()); paired = false; } else handled = false; break;
                case "lrm": case "rlm": case "lre": case "rle": case "lro": case "rlo": case "pdf": case "alm": case "lri": case "rli": case "fsi": case "pdi": case "zwj": case "zwnj": case "wj": case "shy": AddText(new Rune(name switch { "lrm" => 0x200e, "rlm" => 0x200f, "lre" => 0x202a, "rle" => 0x202b, "lro" => 0x202d, "rlo" => 0x202e, "pdf" => 0x202c, "alm" => 0x061c, "lri" => 0x2066, "rli" => 0x2067, "fsi" => 0x2068, "pdi" => 0x2069, "zwj" => 0x200d, "zwnj" => 0x200c, "wj" => 0x2060, _ => 0x00ad }).ToString()); paired = false; break;
                case "b": if (_format.Role == FontRole.Italics) PushBoldItalics(); else PushBold(); break;
                case "i": if (_format.Role == FontRole.Bold) PushBoldItalics(); else PushItalics(); break;
                case "u": PushUnderline(arguments.ContainsKey("color") ? Tint("color", Colors.White) : null); break;
                case "s": PushStrikethrough(arguments.ContainsKey("color") ? Tint("color", Colors.White) : null); break;
                case "code": case "mono": PushMono(); break;
                case "color": case "font_color": PushColor(Color.FromString(value, GetThemeColor("default_color"))); break;
                case "bgcolor": PushBGColor(Color.FromString(value, Colors.Transparent)); break;
                case "fgcolor": PushFGColor(Color.FromString(value, Colors.Transparent)); break;
                case "outline_color": PushOutlineColor(Color.FromString(value, Colors.Black)); break;
                case "outline_size": if (int.TryParse(value, out var outline) && outline >= 0) PushOutlineSize(outline); else handled = false; break;
                case "font_size": if (int.TryParse(value, out var fontSize) && fontSize > 0) PushFontSize(fontSize); else handled = false; break;
                case "opentype_features": case "otf": var featureFont = CurrentMarkupFont(); if (featureFont == null) { handled = false; break; } PushVariationFont(featureFont, new() { ["otf"] = value }, 0); break;
                case "font": try { var fontPath = Arg("name", Arg("n", value)); var font = fontPath.Length > 0 ? ResourceLoader.Load<Font>(fontPath) : CurrentMarkupFont(); if (font == null) { handled = false; break; } PushVariationFont(font, arguments, (int)Number("size", Number("s", 0))); } catch (IOException) { handled = false; } break;
                case "lang": PushLanguage(value); break;
                case "hint": PushHint(value); break;
                case "url": var url = value; if (url.Length == 0) { var stop = bbcode.IndexOf("[/url]", at, StringComparison.Ordinal); url = stop < 0 ? bbcode[at..] : bbcode[at..stop]; } PushMeta(url, Arg("underline", "always") switch { "never" => MetaUnderline.Never, "on_hover" => MetaUnderline.OnHover, _ => MetaUnderline.Always }, Arg("tooltip")); break;
                case "left": PushParagraph(HorizontalAlignment.Left); break;
                case "center": PushParagraph(HorizontalAlignment.Center); break;
                case "right": PushParagraph(HorizontalAlignment.Right); break;
                case "fill": PushParagraph(HorizontalAlignment.Fill); break;
                case "p": PushParagraph(Arg("align") switch { "center" => HorizontalAlignment.Center, "right" => HorizontalAlignment.Right, "fill" => HorizontalAlignment.Fill, _ => _horizontal }, Arg("dir") switch { "ltr" => TextDirection.LTR, "rtl" => TextDirection.RTL, _ => _direction }, Arg("lang", _language)); break;
                case "indent": PushIndent(int.TryParse(value, out var indent) ? Math.Max(0, indent) : 1); break;
                case "ul": PushList(int.TryParse(value, out var ul) ? Math.Max(0, ul) : 1, ListType.Dots, false, Arg("bullet", "•")); break;
                case "ol": var listType = Arg("type", value); PushList((int)Number("level", 1), listType switch { "a" or "A" => ListType.Letters, "i" or "I" => ListType.Roman, _ => ListType.Numbers }, listType is "A" or "I"); break;
                case "table": var tableValues = value.Split(','); if (int.TryParse(tableValues[0], out var columns) && columns > 0) PushTable(columns, ParseInlineAlignment(Arg("align", tableValues.Length > 1 ? string.Join(',', tableValues.Skip(1).Take(2)) : "top")), tableValues.Length > 3 && int.TryParse(tableValues[3], out var row) ? row : (int)Number("align_to_row", -1), Arg("name")); else handled = false; break;
                case "cell": if (_table == null) { handled = false; break; } PushCell(); if (value.Length > 0 && int.TryParse(value, out var expand)) SetTableColumnExpand((_table.Cells.Count - 1) % _table.Columns.Length, true, Math.Max(1, expand)); if (arguments.ContainsKey("name")) SetTableColumnName((_table.Cells.Count - 1) % _table.Columns.Length, Arg("name")); if (arguments.ContainsKey("minsize") || arguments.ContainsKey("maxsize")) { var min = ParseNumbers(Arg("minsize")); var max = ParseNumbers(Arg("maxsize")); SetCellSizeOverride(min.Length == 2 ? new(min[0], min[1]) : default, max.Length == 2 ? new(max[0], max[1]) : default); } if (arguments.ContainsKey("expand")) SetTableColumnExpand((_table.Cells.Count - 1) % _table.Columns.Length, true, (int)Number("expand", 1)); if (arguments.ContainsKey("border")) SetCellBorderColor(Tint("border", Colors.Transparent)); if (arguments.ContainsKey("bg")) { var colors = Arg("bg").Split(','); SetCellRowBackgroundColor(Color.FromString(colors[0], Colors.Transparent), Color.FromString(colors.Length > 1 ? colors[1] : colors[0], Colors.Transparent)); } if (arguments.ContainsKey("padding")) { var pads = ParseNumbers(Arg("padding")); if (pads.Length == 4) SetCellPadding(new(pads[0], pads[1], pads[2], pads[3])); } break;
                case "img": var imageEnd = bbcode.IndexOf("[/img]", at, StringComparison.Ordinal); if (imageEnd < 0) { handled = false; break; } var path = bbcode[at..imageEnd]; try { var texture = ResourceLoader.Load<Texture>(path); var dimensions = value.Split('x'); var widthText = Arg("width", dimensions[0]); var heightText = Arg("height", dimensions.Length > 1 ? dimensions[1] : ""); var regionValues = ParseNumbers(Arg("region")); var crop = regionValues.Length == 4 ? new Rect2(regionValues[0], regionValues[1], regionValues[2], regionValues[3]) : default; AddImage(texture, ParseDimension(widthText), ParseDimension(heightText), Tint("color", Colors.White), ParseInlineAlignment(Arg("align", "center")), crop, pad: Arg("pad") == "true", tooltip: Arg("tooltip"), widthUnit: DimensionUnit(widthText), heightUnit: DimensionUnit(heightText), altText: Arg("alt")); at = imageEnd + 6; paired = false; } catch (IOException) { handled = false; } break;
                case "hr": AddHR((int)Number("width", 90), (int)Number("height", 2), Tint("color", Colors.White), Arg("align") switch { "left" => HorizontalAlignment.Left, "right" => HorizontalAlignment.Right, _ => HorizontalAlignment.Center }, !Arg("width").EndsWith("px", StringComparison.Ordinal), Arg("height").EndsWith('%')); paired = false; break;
                case "dropcap": var capEnd = bbcode.IndexOf("[/dropcap]", at, StringComparison.Ordinal); if (capEnd < 0) { handled = false; break; } var cap = bbcode[at..capEnd]; var capFont = Arg("font").Length > 0 ? ResourceLoader.Load<Font>(Arg("font")) : GetThemeFont("normal_font")!; PushDropcap(cap, capFont, (int)Number("font_size", 64), color: Tint("color", GetThemeColor("default_color")), outlineSize: (int)Number("outline_size", 0), outlineColor: Tint("outline_color", Colors.Transparent)); at = capEnd + 10; paired = false; break;
                case "fade": case "shake": case "wave": case "tornado": case "rainbow": case "pulse": PushFX(name, ParseExpressionsForValues(tokens.Skip(1).ToArray())); break;
                default: var custom = _effects.FirstOrDefault(e => !e.IsDisposed && e.BBCode == name); if (custom != null) PushCustomFX(custom, ParseExpressionsForValues(tokens.Skip(1).ToArray())); else handled = false; break;
            }
            if (!handled) AddText("[" + raw + "]"); else if (paired) opened.Push((name, depth));
        }
        Changed();
    }
    private Font? CurrentMarkupFont() => _format.Font ?? GetThemeFont(_format.Role switch { FontRole.Bold => "bold_font", FontRole.Italics => "italics_font", FontRole.BoldItalics => "bold_italics_font", FontRole.Mono => "mono_font", _ => "normal_font" });
    private void PushVariationFont(Font source, Dictionary<string, string> arguments, int size)
    {
        var font = new FontVariation { BaseFont = source };
        try
        {
            string Arg(string key, string alias) => arguments.GetValueOrDefault(key, arguments.GetValueOrDefault(alias, ""));
            int Integer(string key, string alias) => int.TryParse(Arg(key, alias), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0;
            float Number(string key, string alias) => float.TryParse(Arg(key, alias), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && float.IsFinite(value) ? value : 0;
            font.SpacingGlyph = Integer("glyph_spacing", "gl"); font.SpacingSpace = Integer("space_spacing", "sp");
            font.SpacingTop = Integer("top_spacing", "top"); font.SpacingBottom = Integer("bottom_spacing", "bt");
            font.VariationEmbolden = Number("embolden", "emb"); font.VariationFaceIndex = Math.Max(0, Integer("face_index", "fi"));
            var slant = Number("slant", "sln"); font.VariationTransform = new(new(1, Math.Clamp(slant, -32767, 32767)), new(0, 1), default);
            var features = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var entry in Arg("opentype_features", "otf").Split(',')) { var pair = entry.Trim().Split('='); if (pair[0].Length > 0) features[pair[0]] = pair.Length > 1 && int.TryParse(pair[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : 1; }
            font.OpenTypeFeatures = features;
            var coordinates = new Dictionary<uint, float>();
            foreach (var entry in Arg("opentype_variation", "otv").Split(',')) { var pair = entry.Trim().Split('='); if (pair.Length == 2 && pair[0].Length > 0 && float.TryParse(pair[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var n) && float.IsFinite(n)) coordinates[OpenTypeFeatureTags.Resolve(pair[0].Trim())] = n; }
            font.VariationOpenType = coordinates; _ownedFonts.Add(font); PushFont(font, size);
        }
        catch { _ownedFonts.Remove(font); font.Dispose(); throw; }
    }
    private static int FindTagEnd(string text, int from) { var quote = '\0'; for (var i = from; i < text.Length; i++) { var c = text[i]; if (quote != '\0') { if (c == quote) quote = '\0'; } else if (c is '\'' or '"') quote = c; else if (c == ']') return i; } return -1; }
    private static string Unquote(string text) => text.Length >= 2 && (text[0] is '\'' or '"') && text[^1] == text[0] ? text[1..^1] : text;
    private static List<string> SplitTag(string text) { var result = new List<string>(); var start = 0; var quote = '\0'; for (var i = 0; i <= text.Length; i++) { var c = i < text.Length ? text[i] : ' '; if (quote != '\0') { if (c == quote) quote = '\0'; continue; } if (c is '\'' or '"') quote = c; if (char.IsWhiteSpace(c)) { if (i > start) result.Add(text[start..i]); start = i + 1; } } return result; }
    private static ImageUnit DimensionUnit(string text) => text.EndsWith('%') ? ImageUnit.Percent : text.EndsWith("em", StringComparison.Ordinal) ? ImageUnit.EM : ImageUnit.Pixel;
    private static float ParseDimension(string text) => float.TryParse(text.TrimEnd('%').Replace("px", "", StringComparison.Ordinal).Replace("em", "", StringComparison.Ordinal), NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? Math.Max(0, n) : 0;
    private static float[] ParseNumbers(string text) => text.Split(',').Select(s => float.TryParse(s.TrimEnd('%'), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : 0).ToArray();
    private static InlineAlignment ParseInlineAlignment(string text) { var parts = text.Split(','); if (parts.Length == 1) return parts[0] switch { "top" or "t" => InlineAlignment.Top, "bottom" or "b" => InlineAlignment.Bottom, "baseline" or "l" => InlineAlignment.BaselineTo | InlineAlignment.ToBaseline, _ => InlineAlignment.Center }; var image = parts[0] switch { "top" or "t" => InlineAlignment.TopTo, "bottom" or "b" => InlineAlignment.BottomTo, "baseline" or "l" => InlineAlignment.BaselineTo, _ => InlineAlignment.CenterTo }; var line = parts[1] switch { "top" or "t" => InlineAlignment.ToTop, "bottom" or "b" => InlineAlignment.ToBottom, "baseline" or "l" => InlineAlignment.ToBaseline, _ => InlineAlignment.ToCenter }; return image | line; }
}
