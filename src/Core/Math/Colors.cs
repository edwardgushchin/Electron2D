using System.Collections.Frozen;

namespace Electron2D;

/// <summary>Provides the standard named color catalog used by <see cref="Color"/> string conversion.</summary>
/// <remarks>
/// Properties return values and own no resources. Several historical names are aliases with identical channel values.
/// <see cref="Transparent"/> is transparent white rather than the zero-initialized transparent black value.
/// </remarks>
public static class Colors
{
    private static readonly FrozenDictionary<string, Color> NamedColors =
        new Dictionary<string, Color>(StringComparer.Ordinal)
        {
            ["ALICEBLUE"] = AliceBlue,
            ["ANTIQUEWHITE"] = AntiqueWhite,
            ["AQUA"] = Aqua,
            ["AQUAMARINE"] = Aquamarine,
            ["AZURE"] = Azure,
            ["BEIGE"] = Beige,
            ["BISQUE"] = Bisque,
            ["BLACK"] = Black,
            ["BLANCHEDALMOND"] = BlanchedAlmond,
            ["BLUE"] = Blue,
            ["BLUEVIOLET"] = BlueViolet,
            ["BROWN"] = Brown,
            ["BURLYWOOD"] = Burlywood,
            ["CADETBLUE"] = CadetBlue,
            ["CHARTREUSE"] = Chartreuse,
            ["CHOCOLATE"] = Chocolate,
            ["CORAL"] = Coral,
            ["CORNFLOWERBLUE"] = CornflowerBlue,
            ["CORNSILK"] = Cornsilk,
            ["CRIMSON"] = Crimson,
            ["CYAN"] = Cyan,
            ["DARKBLUE"] = DarkBlue,
            ["DARKCYAN"] = DarkCyan,
            ["DARKGOLDENROD"] = DarkGoldenrod,
            ["DARKGRAY"] = DarkGray,
            ["DARKGREEN"] = DarkGreen,
            ["DARKKHAKI"] = DarkKhaki,
            ["DARKMAGENTA"] = DarkMagenta,
            ["DARKOLIVEGREEN"] = DarkOliveGreen,
            ["DARKORANGE"] = DarkOrange,
            ["DARKORCHID"] = DarkOrchid,
            ["DARKRED"] = DarkRed,
            ["DARKSALMON"] = DarkSalmon,
            ["DARKSEAGREEN"] = DarkSeaGreen,
            ["DARKSLATEBLUE"] = DarkSlateBlue,
            ["DARKSLATEGRAY"] = DarkSlateGray,
            ["DARKTURQUOISE"] = DarkTurquoise,
            ["DARKVIOLET"] = DarkViolet,
            ["DEEPPINK"] = DeepPink,
            ["DEEPSKYBLUE"] = DeepSkyBlue,
            ["DIMGRAY"] = DimGray,
            ["DODGERBLUE"] = DodgerBlue,
            ["FIREBRICK"] = Firebrick,
            ["FLORALWHITE"] = FloralWhite,
            ["FORESTGREEN"] = ForestGreen,
            ["FUCHSIA"] = Fuchsia,
            ["GAINSBORO"] = Gainsboro,
            ["GHOSTWHITE"] = GhostWhite,
            ["GOLD"] = Gold,
            ["GOLDENROD"] = Goldenrod,
            ["GRAY"] = Gray,
            ["GREEN"] = Green,
            ["GREENYELLOW"] = GreenYellow,
            ["HONEYDEW"] = Honeydew,
            ["HOTPINK"] = HotPink,
            ["INDIANRED"] = IndianRed,
            ["INDIGO"] = Indigo,
            ["IVORY"] = Ivory,
            ["KHAKI"] = Khaki,
            ["LAVENDER"] = Lavender,
            ["LAVENDERBLUSH"] = LavenderBlush,
            ["LAWNGREEN"] = LawnGreen,
            ["LEMONCHIFFON"] = LemonChiffon,
            ["LIGHTBLUE"] = LightBlue,
            ["LIGHTCORAL"] = LightCoral,
            ["LIGHTCYAN"] = LightCyan,
            ["LIGHTGOLDENROD"] = LightGoldenrod,
            ["LIGHTGRAY"] = LightGray,
            ["LIGHTGREEN"] = LightGreen,
            ["LIGHTPINK"] = LightPink,
            ["LIGHTSALMON"] = LightSalmon,
            ["LIGHTSEAGREEN"] = LightSeaGreen,
            ["LIGHTSKYBLUE"] = LightSkyBlue,
            ["LIGHTSLATEGRAY"] = LightSlateGray,
            ["LIGHTSTEELBLUE"] = LightSteelBlue,
            ["LIGHTYELLOW"] = LightYellow,
            ["LIME"] = Lime,
            ["LIMEGREEN"] = LimeGreen,
            ["LINEN"] = Linen,
            ["MAGENTA"] = Magenta,
            ["MAROON"] = Maroon,
            ["MEDIUMAQUAMARINE"] = MediumAquamarine,
            ["MEDIUMBLUE"] = MediumBlue,
            ["MEDIUMORCHID"] = MediumOrchid,
            ["MEDIUMPURPLE"] = MediumPurple,
            ["MEDIUMSEAGREEN"] = MediumSeaGreen,
            ["MEDIUMSLATEBLUE"] = MediumSlateBlue,
            ["MEDIUMSPRINGGREEN"] = MediumSpringGreen,
            ["MEDIUMTURQUOISE"] = MediumTurquoise,
            ["MEDIUMVIOLETRED"] = MediumVioletRed,
            ["MIDNIGHTBLUE"] = MidnightBlue,
            ["MINTCREAM"] = MintCream,
            ["MISTYROSE"] = MistyRose,
            ["MOCCASIN"] = Moccasin,
            ["NAVAJOWHITE"] = NavajoWhite,
            ["NAVYBLUE"] = NavyBlue,
            ["OLDLACE"] = OldLace,
            ["OLIVE"] = Olive,
            ["OLIVEDRAB"] = OliveDrab,
            ["ORANGE"] = Orange,
            ["ORANGERED"] = OrangeRed,
            ["ORCHID"] = Orchid,
            ["PALEGOLDENROD"] = PaleGoldenrod,
            ["PALEGREEN"] = PaleGreen,
            ["PALETURQUOISE"] = PaleTurquoise,
            ["PALEVIOLETRED"] = PaleVioletRed,
            ["PAPAYAWHIP"] = PapayaWhip,
            ["PEACHPUFF"] = PeachPuff,
            ["PERU"] = Peru,
            ["PINK"] = Pink,
            ["PLUM"] = Plum,
            ["POWDERBLUE"] = PowderBlue,
            ["PURPLE"] = Purple,
            ["REBECCAPURPLE"] = RebeccaPurple,
            ["RED"] = Red,
            ["ROSYBROWN"] = RosyBrown,
            ["ROYALBLUE"] = RoyalBlue,
            ["SADDLEBROWN"] = SaddleBrown,
            ["SALMON"] = Salmon,
            ["SANDYBROWN"] = SandyBrown,
            ["SEAGREEN"] = SeaGreen,
            ["SEASHELL"] = Seashell,
            ["SIENNA"] = Sienna,
            ["SILVER"] = Silver,
            ["SKYBLUE"] = SkyBlue,
            ["SLATEBLUE"] = SlateBlue,
            ["SLATEGRAY"] = SlateGray,
            ["SNOW"] = Snow,
            ["SPRINGGREEN"] = SpringGreen,
            ["STEELBLUE"] = SteelBlue,
            ["TAN"] = Tan,
            ["TEAL"] = Teal,
            ["THISTLE"] = Thistle,
            ["TOMATO"] = Tomato,
            ["TRANSPARENT"] = Transparent,
            ["TURQUOISE"] = Turquoise,
            ["VIOLET"] = Violet,
            ["WEBGRAY"] = WebGray,
            ["WEBGREEN"] = WebGreen,
            ["WEBMAROON"] = WebMaroon,
            ["WEBPURPLE"] = WebPurple,
            ["WHEAT"] = Wheat,
            ["WHITE"] = White,
            ["WHITESMOKE"] = WhiteSmoke,
            ["YELLOW"] = Yellow,
            ["YELLOWGREEN"] = YellowGreen,
        }.ToFrozenDictionary(StringComparer.Ordinal);

    /// <summary>Gets the standard AliceBlue color.</summary>
    /// <value><c>#f0f8ffff</c>.</value>
    public static Color AliceBlue => new(0xF0F8FFFFu);

    /// <summary>Gets the standard AntiqueWhite color.</summary>
    /// <value><c>#faebd7ff</c>.</value>
    public static Color AntiqueWhite => new(0xFAEBD7FFu);

    /// <summary>Gets the standard Aqua color.</summary>
    /// <value><c>#00ffffff</c>.</value>
    public static Color Aqua => new(0x00FFFFFFu);

    /// <summary>Gets the standard Aquamarine color.</summary>
    /// <value><c>#7fffd4ff</c>.</value>
    public static Color Aquamarine => new(0x7FFFD4FFu);

    /// <summary>Gets the standard Azure color.</summary>
    /// <value><c>#f0ffffff</c>.</value>
    public static Color Azure => new(0xF0FFFFFFu);

    /// <summary>Gets the standard Beige color.</summary>
    /// <value><c>#f5f5dcff</c>.</value>
    public static Color Beige => new(0xF5F5DCFFu);

    /// <summary>Gets the standard Bisque color.</summary>
    /// <value><c>#ffe4c4ff</c>.</value>
    public static Color Bisque => new(0xFFE4C4FFu);

    /// <summary>Gets the standard Black color.</summary>
    /// <value><c>#000000ff</c>.</value>
    public static Color Black => new(0x000000FFu);

    /// <summary>Gets the standard BlanchedAlmond color.</summary>
    /// <value><c>#ffebcdff</c>.</value>
    public static Color BlanchedAlmond => new(0xFFEBCDFFu);

    /// <summary>Gets the standard Blue color.</summary>
    /// <value><c>#0000ffff</c>.</value>
    public static Color Blue => new(0x0000FFFFu);

    /// <summary>Gets the standard BlueViolet color.</summary>
    /// <value><c>#8a2be2ff</c>.</value>
    public static Color BlueViolet => new(0x8A2BE2FFu);

    /// <summary>Gets the standard Brown color.</summary>
    /// <value><c>#a52a2aff</c>.</value>
    public static Color Brown => new(0xA52A2AFFu);

    /// <summary>Gets the standard Burlywood color.</summary>
    /// <value><c>#deb887ff</c>.</value>
    public static Color Burlywood => new(0xDEB887FFu);

    /// <summary>Gets the standard CadetBlue color.</summary>
    /// <value><c>#5f9ea0ff</c>.</value>
    public static Color CadetBlue => new(0x5F9EA0FFu);

    /// <summary>Gets the standard Chartreuse color.</summary>
    /// <value><c>#7fff00ff</c>.</value>
    public static Color Chartreuse => new(0x7FFF00FFu);

    /// <summary>Gets the standard Chocolate color.</summary>
    /// <value><c>#d2691eff</c>.</value>
    public static Color Chocolate => new(0xD2691EFFu);

    /// <summary>Gets the standard Coral color.</summary>
    /// <value><c>#ff7f50ff</c>.</value>
    public static Color Coral => new(0xFF7F50FFu);

    /// <summary>Gets the standard CornflowerBlue color.</summary>
    /// <value><c>#6495edff</c>.</value>
    public static Color CornflowerBlue => new(0x6495EDFFu);

    /// <summary>Gets the standard Cornsilk color.</summary>
    /// <value><c>#fff8dcff</c>.</value>
    public static Color Cornsilk => new(0xFFF8DCFFu);

    /// <summary>Gets the standard Crimson color.</summary>
    /// <value><c>#dc143cff</c>.</value>
    public static Color Crimson => new(0xDC143CFFu);

    /// <summary>Gets the standard Cyan color.</summary>
    /// <value><c>#00ffffff</c>.</value>
    public static Color Cyan => new(0x00FFFFFFu);

    /// <summary>Gets the standard DarkBlue color.</summary>
    /// <value><c>#00008bff</c>.</value>
    public static Color DarkBlue => new(0x00008BFFu);

    /// <summary>Gets the standard DarkCyan color.</summary>
    /// <value><c>#008b8bff</c>.</value>
    public static Color DarkCyan => new(0x008B8BFFu);

    /// <summary>Gets the standard DarkGoldenrod color.</summary>
    /// <value><c>#b8860bff</c>.</value>
    public static Color DarkGoldenrod => new(0xB8860BFFu);

    /// <summary>Gets the standard DarkGray color.</summary>
    /// <value><c>#a9a9a9ff</c>.</value>
    public static Color DarkGray => new(0xA9A9A9FFu);

    /// <summary>Gets the standard DarkGreen color.</summary>
    /// <value><c>#006400ff</c>.</value>
    public static Color DarkGreen => new(0x006400FFu);

    /// <summary>Gets the standard DarkKhaki color.</summary>
    /// <value><c>#bdb76bff</c>.</value>
    public static Color DarkKhaki => new(0xBDB76BFFu);

    /// <summary>Gets the standard DarkMagenta color.</summary>
    /// <value><c>#8b008bff</c>.</value>
    public static Color DarkMagenta => new(0x8B008BFFu);

    /// <summary>Gets the standard DarkOliveGreen color.</summary>
    /// <value><c>#556b2fff</c>.</value>
    public static Color DarkOliveGreen => new(0x556B2FFFu);

    /// <summary>Gets the standard DarkOrange color.</summary>
    /// <value><c>#ff8c00ff</c>.</value>
    public static Color DarkOrange => new(0xFF8C00FFu);

    /// <summary>Gets the standard DarkOrchid color.</summary>
    /// <value><c>#9932ccff</c>.</value>
    public static Color DarkOrchid => new(0x9932CCFFu);

    /// <summary>Gets the standard DarkRed color.</summary>
    /// <value><c>#8b0000ff</c>.</value>
    public static Color DarkRed => new(0x8B0000FFu);

    /// <summary>Gets the standard DarkSalmon color.</summary>
    /// <value><c>#e9967aff</c>.</value>
    public static Color DarkSalmon => new(0xE9967AFFu);

    /// <summary>Gets the standard DarkSeaGreen color.</summary>
    /// <value><c>#8fbc8fff</c>.</value>
    public static Color DarkSeaGreen => new(0x8FBC8FFFu);

    /// <summary>Gets the standard DarkSlateBlue color.</summary>
    /// <value><c>#483d8bff</c>.</value>
    public static Color DarkSlateBlue => new(0x483D8BFFu);

    /// <summary>Gets the standard DarkSlateGray color.</summary>
    /// <value><c>#2f4f4fff</c>.</value>
    public static Color DarkSlateGray => new(0x2F4F4FFFu);

    /// <summary>Gets the standard DarkTurquoise color.</summary>
    /// <value><c>#00ced1ff</c>.</value>
    public static Color DarkTurquoise => new(0x00CED1FFu);

    /// <summary>Gets the standard DarkViolet color.</summary>
    /// <value><c>#9400d3ff</c>.</value>
    public static Color DarkViolet => new(0x9400D3FFu);

    /// <summary>Gets the standard DeepPink color.</summary>
    /// <value><c>#ff1493ff</c>.</value>
    public static Color DeepPink => new(0xFF1493FFu);

    /// <summary>Gets the standard DeepSkyBlue color.</summary>
    /// <value><c>#00bfffff</c>.</value>
    public static Color DeepSkyBlue => new(0x00BFFFFFu);

    /// <summary>Gets the standard DimGray color.</summary>
    /// <value><c>#696969ff</c>.</value>
    public static Color DimGray => new(0x696969FFu);

    /// <summary>Gets the standard DodgerBlue color.</summary>
    /// <value><c>#1e90ffff</c>.</value>
    public static Color DodgerBlue => new(0x1E90FFFFu);

    /// <summary>Gets the standard Firebrick color.</summary>
    /// <value><c>#b22222ff</c>.</value>
    public static Color Firebrick => new(0xB22222FFu);

    /// <summary>Gets the standard FloralWhite color.</summary>
    /// <value><c>#fffaf0ff</c>.</value>
    public static Color FloralWhite => new(0xFFFAF0FFu);

    /// <summary>Gets the standard ForestGreen color.</summary>
    /// <value><c>#228b22ff</c>.</value>
    public static Color ForestGreen => new(0x228B22FFu);

    /// <summary>Gets the standard Fuchsia color.</summary>
    /// <value><c>#ff00ffff</c>.</value>
    public static Color Fuchsia => new(0xFF00FFFFu);

    /// <summary>Gets the standard Gainsboro color.</summary>
    /// <value><c>#dcdcdcff</c>.</value>
    public static Color Gainsboro => new(0xDCDCDCFFu);

    /// <summary>Gets the standard GhostWhite color.</summary>
    /// <value><c>#f8f8ffff</c>.</value>
    public static Color GhostWhite => new(0xF8F8FFFFu);

    /// <summary>Gets the standard Gold color.</summary>
    /// <value><c>#ffd700ff</c>.</value>
    public static Color Gold => new(0xFFD700FFu);

    /// <summary>Gets the standard Goldenrod color.</summary>
    /// <value><c>#daa520ff</c>.</value>
    public static Color Goldenrod => new(0xDAA520FFu);

    /// <summary>Gets the standard Gray color.</summary>
    /// <value><c>#bebebeff</c>.</value>
    public static Color Gray => new(0xBEBEBEFFu);

    /// <summary>Gets the standard Green color.</summary>
    /// <value><c>#00ff00ff</c>.</value>
    public static Color Green => new(0x00FF00FFu);

    /// <summary>Gets the standard GreenYellow color.</summary>
    /// <value><c>#adff2fff</c>.</value>
    public static Color GreenYellow => new(0xADFF2FFFu);

    /// <summary>Gets the standard Honeydew color.</summary>
    /// <value><c>#f0fff0ff</c>.</value>
    public static Color Honeydew => new(0xF0FFF0FFu);

    /// <summary>Gets the standard HotPink color.</summary>
    /// <value><c>#ff69b4ff</c>.</value>
    public static Color HotPink => new(0xFF69B4FFu);

    /// <summary>Gets the standard IndianRed color.</summary>
    /// <value><c>#cd5c5cff</c>.</value>
    public static Color IndianRed => new(0xCD5C5CFFu);

    /// <summary>Gets the standard Indigo color.</summary>
    /// <value><c>#4b0082ff</c>.</value>
    public static Color Indigo => new(0x4B0082FFu);

    /// <summary>Gets the standard Ivory color.</summary>
    /// <value><c>#fffff0ff</c>.</value>
    public static Color Ivory => new(0xFFFFF0FFu);

    /// <summary>Gets the standard Khaki color.</summary>
    /// <value><c>#f0e68cff</c>.</value>
    public static Color Khaki => new(0xF0E68CFFu);

    /// <summary>Gets the standard Lavender color.</summary>
    /// <value><c>#e6e6faff</c>.</value>
    public static Color Lavender => new(0xE6E6FAFFu);

    /// <summary>Gets the standard LavenderBlush color.</summary>
    /// <value><c>#fff0f5ff</c>.</value>
    public static Color LavenderBlush => new(0xFFF0F5FFu);

    /// <summary>Gets the standard LawnGreen color.</summary>
    /// <value><c>#7cfc00ff</c>.</value>
    public static Color LawnGreen => new(0x7CFC00FFu);

    /// <summary>Gets the standard LemonChiffon color.</summary>
    /// <value><c>#fffacdff</c>.</value>
    public static Color LemonChiffon => new(0xFFFACDFFu);

    /// <summary>Gets the standard LightBlue color.</summary>
    /// <value><c>#add8e6ff</c>.</value>
    public static Color LightBlue => new(0xADD8E6FFu);

    /// <summary>Gets the standard LightCoral color.</summary>
    /// <value><c>#f08080ff</c>.</value>
    public static Color LightCoral => new(0xF08080FFu);

    /// <summary>Gets the standard LightCyan color.</summary>
    /// <value><c>#e0ffffff</c>.</value>
    public static Color LightCyan => new(0xE0FFFFFFu);

    /// <summary>Gets the standard LightGoldenrod color.</summary>
    /// <value><c>#fafad2ff</c>.</value>
    public static Color LightGoldenrod => new(0xFAFAD2FFu);

    /// <summary>Gets the standard LightGray color.</summary>
    /// <value><c>#d3d3d3ff</c>.</value>
    public static Color LightGray => new(0xD3D3D3FFu);

    /// <summary>Gets the standard LightGreen color.</summary>
    /// <value><c>#90ee90ff</c>.</value>
    public static Color LightGreen => new(0x90EE90FFu);

    /// <summary>Gets the standard LightPink color.</summary>
    /// <value><c>#ffb6c1ff</c>.</value>
    public static Color LightPink => new(0xFFB6C1FFu);

    /// <summary>Gets the standard LightSalmon color.</summary>
    /// <value><c>#ffa07aff</c>.</value>
    public static Color LightSalmon => new(0xFFA07AFFu);

    /// <summary>Gets the standard LightSeaGreen color.</summary>
    /// <value><c>#20b2aaff</c>.</value>
    public static Color LightSeaGreen => new(0x20B2AAFFu);

    /// <summary>Gets the standard LightSkyBlue color.</summary>
    /// <value><c>#87cefaff</c>.</value>
    public static Color LightSkyBlue => new(0x87CEFAFFu);

    /// <summary>Gets the standard LightSlateGray color.</summary>
    /// <value><c>#778899ff</c>.</value>
    public static Color LightSlateGray => new(0x778899FFu);

    /// <summary>Gets the standard LightSteelBlue color.</summary>
    /// <value><c>#b0c4deff</c>.</value>
    public static Color LightSteelBlue => new(0xB0C4DEFFu);

    /// <summary>Gets the standard LightYellow color.</summary>
    /// <value><c>#ffffe0ff</c>.</value>
    public static Color LightYellow => new(0xFFFFE0FFu);

    /// <summary>Gets the standard Lime color.</summary>
    /// <value><c>#00ff00ff</c>.</value>
    public static Color Lime => new(0x00FF00FFu);

    /// <summary>Gets the standard LimeGreen color.</summary>
    /// <value><c>#32cd32ff</c>.</value>
    public static Color LimeGreen => new(0x32CD32FFu);

    /// <summary>Gets the standard Linen color.</summary>
    /// <value><c>#faf0e6ff</c>.</value>
    public static Color Linen => new(0xFAF0E6FFu);

    /// <summary>Gets the standard Magenta color.</summary>
    /// <value><c>#ff00ffff</c>.</value>
    public static Color Magenta => new(0xFF00FFFFu);

    /// <summary>Gets the standard Maroon color.</summary>
    /// <value><c>#b03060ff</c>.</value>
    public static Color Maroon => new(0xB03060FFu);

    /// <summary>Gets the standard MediumAquamarine color.</summary>
    /// <value><c>#66cdaaff</c>.</value>
    public static Color MediumAquamarine => new(0x66CDAAFFu);

    /// <summary>Gets the standard MediumBlue color.</summary>
    /// <value><c>#0000cdff</c>.</value>
    public static Color MediumBlue => new(0x0000CDFFu);

    /// <summary>Gets the standard MediumOrchid color.</summary>
    /// <value><c>#ba55d3ff</c>.</value>
    public static Color MediumOrchid => new(0xBA55D3FFu);

    /// <summary>Gets the standard MediumPurple color.</summary>
    /// <value><c>#9370dbff</c>.</value>
    public static Color MediumPurple => new(0x9370DBFFu);

    /// <summary>Gets the standard MediumSeaGreen color.</summary>
    /// <value><c>#3cb371ff</c>.</value>
    public static Color MediumSeaGreen => new(0x3CB371FFu);

    /// <summary>Gets the standard MediumSlateBlue color.</summary>
    /// <value><c>#7b68eeff</c>.</value>
    public static Color MediumSlateBlue => new(0x7B68EEFFu);

    /// <summary>Gets the standard MediumSpringGreen color.</summary>
    /// <value><c>#00fa9aff</c>.</value>
    public static Color MediumSpringGreen => new(0x00FA9AFFu);

    /// <summary>Gets the standard MediumTurquoise color.</summary>
    /// <value><c>#48d1ccff</c>.</value>
    public static Color MediumTurquoise => new(0x48D1CCFFu);

    /// <summary>Gets the standard MediumVioletRed color.</summary>
    /// <value><c>#c71585ff</c>.</value>
    public static Color MediumVioletRed => new(0xC71585FFu);

    /// <summary>Gets the standard MidnightBlue color.</summary>
    /// <value><c>#191970ff</c>.</value>
    public static Color MidnightBlue => new(0x191970FFu);

    /// <summary>Gets the standard MintCream color.</summary>
    /// <value><c>#f5fffaff</c>.</value>
    public static Color MintCream => new(0xF5FFFAFFu);

    /// <summary>Gets the standard MistyRose color.</summary>
    /// <value><c>#ffe4e1ff</c>.</value>
    public static Color MistyRose => new(0xFFE4E1FFu);

    /// <summary>Gets the standard Moccasin color.</summary>
    /// <value><c>#ffe4b5ff</c>.</value>
    public static Color Moccasin => new(0xFFE4B5FFu);

    /// <summary>Gets the standard NavajoWhite color.</summary>
    /// <value><c>#ffdeadff</c>.</value>
    public static Color NavajoWhite => new(0xFFDEADFFu);

    /// <summary>Gets the standard NavyBlue color.</summary>
    /// <value><c>#000080ff</c>.</value>
    public static Color NavyBlue => new(0x000080FFu);

    /// <summary>Gets the standard OldLace color.</summary>
    /// <value><c>#fdf5e6ff</c>.</value>
    public static Color OldLace => new(0xFDF5E6FFu);

    /// <summary>Gets the standard Olive color.</summary>
    /// <value><c>#808000ff</c>.</value>
    public static Color Olive => new(0x808000FFu);

    /// <summary>Gets the standard OliveDrab color.</summary>
    /// <value><c>#6b8e23ff</c>.</value>
    public static Color OliveDrab => new(0x6B8E23FFu);

    /// <summary>Gets the standard Orange color.</summary>
    /// <value><c>#ffa500ff</c>.</value>
    public static Color Orange => new(0xFFA500FFu);

    /// <summary>Gets the standard OrangeRed color.</summary>
    /// <value><c>#ff4500ff</c>.</value>
    public static Color OrangeRed => new(0xFF4500FFu);

    /// <summary>Gets the standard Orchid color.</summary>
    /// <value><c>#da70d6ff</c>.</value>
    public static Color Orchid => new(0xDA70D6FFu);

    /// <summary>Gets the standard PaleGoldenrod color.</summary>
    /// <value><c>#eee8aaff</c>.</value>
    public static Color PaleGoldenrod => new(0xEEE8AAFFu);

    /// <summary>Gets the standard PaleGreen color.</summary>
    /// <value><c>#98fb98ff</c>.</value>
    public static Color PaleGreen => new(0x98FB98FFu);

    /// <summary>Gets the standard PaleTurquoise color.</summary>
    /// <value><c>#afeeeeff</c>.</value>
    public static Color PaleTurquoise => new(0xAFEEEEFFu);

    /// <summary>Gets the standard PaleVioletRed color.</summary>
    /// <value><c>#db7093ff</c>.</value>
    public static Color PaleVioletRed => new(0xDB7093FFu);

    /// <summary>Gets the standard PapayaWhip color.</summary>
    /// <value><c>#ffefd5ff</c>.</value>
    public static Color PapayaWhip => new(0xFFEFD5FFu);

    /// <summary>Gets the standard PeachPuff color.</summary>
    /// <value><c>#ffdab9ff</c>.</value>
    public static Color PeachPuff => new(0xFFDAB9FFu);

    /// <summary>Gets the standard Peru color.</summary>
    /// <value><c>#cd853fff</c>.</value>
    public static Color Peru => new(0xCD853FFFu);

    /// <summary>Gets the standard Pink color.</summary>
    /// <value><c>#ffc0cbff</c>.</value>
    public static Color Pink => new(0xFFC0CBFFu);

    /// <summary>Gets the standard Plum color.</summary>
    /// <value><c>#dda0ddff</c>.</value>
    public static Color Plum => new(0xDDA0DDFFu);

    /// <summary>Gets the standard PowderBlue color.</summary>
    /// <value><c>#b0e0e6ff</c>.</value>
    public static Color PowderBlue => new(0xB0E0E6FFu);

    /// <summary>Gets the standard Purple color.</summary>
    /// <value><c>#a020f0ff</c>.</value>
    public static Color Purple => new(0xA020F0FFu);

    /// <summary>Gets the standard RebeccaPurple color.</summary>
    /// <value><c>#663399ff</c>.</value>
    public static Color RebeccaPurple => new(0x663399FFu);

    /// <summary>Gets the standard Red color.</summary>
    /// <value><c>#ff0000ff</c>.</value>
    public static Color Red => new(0xFF0000FFu);

    /// <summary>Gets the standard RosyBrown color.</summary>
    /// <value><c>#bc8f8fff</c>.</value>
    public static Color RosyBrown => new(0xBC8F8FFFu);

    /// <summary>Gets the standard RoyalBlue color.</summary>
    /// <value><c>#4169e1ff</c>.</value>
    public static Color RoyalBlue => new(0x4169E1FFu);

    /// <summary>Gets the standard SaddleBrown color.</summary>
    /// <value><c>#8b4513ff</c>.</value>
    public static Color SaddleBrown => new(0x8B4513FFu);

    /// <summary>Gets the standard Salmon color.</summary>
    /// <value><c>#fa8072ff</c>.</value>
    public static Color Salmon => new(0xFA8072FFu);

    /// <summary>Gets the standard SandyBrown color.</summary>
    /// <value><c>#f4a460ff</c>.</value>
    public static Color SandyBrown => new(0xF4A460FFu);

    /// <summary>Gets the standard SeaGreen color.</summary>
    /// <value><c>#2e8b57ff</c>.</value>
    public static Color SeaGreen => new(0x2E8B57FFu);

    /// <summary>Gets the standard Seashell color.</summary>
    /// <value><c>#fff5eeff</c>.</value>
    public static Color Seashell => new(0xFFF5EEFFu);

    /// <summary>Gets the standard Sienna color.</summary>
    /// <value><c>#a0522dff</c>.</value>
    public static Color Sienna => new(0xA0522DFFu);

    /// <summary>Gets the standard Silver color.</summary>
    /// <value><c>#c0c0c0ff</c>.</value>
    public static Color Silver => new(0xC0C0C0FFu);

    /// <summary>Gets the standard SkyBlue color.</summary>
    /// <value><c>#87ceebff</c>.</value>
    public static Color SkyBlue => new(0x87CEEBFFu);

    /// <summary>Gets the standard SlateBlue color.</summary>
    /// <value><c>#6a5acdff</c>.</value>
    public static Color SlateBlue => new(0x6A5ACDFFu);

    /// <summary>Gets the standard SlateGray color.</summary>
    /// <value><c>#708090ff</c>.</value>
    public static Color SlateGray => new(0x708090FFu);

    /// <summary>Gets the standard Snow color.</summary>
    /// <value><c>#fffafaff</c>.</value>
    public static Color Snow => new(0xFFFAFAFFu);

    /// <summary>Gets the standard SpringGreen color.</summary>
    /// <value><c>#00ff7fff</c>.</value>
    public static Color SpringGreen => new(0x00FF7FFFu);

    /// <summary>Gets the standard SteelBlue color.</summary>
    /// <value><c>#4682b4ff</c>.</value>
    public static Color SteelBlue => new(0x4682B4FFu);

    /// <summary>Gets the standard Tan color.</summary>
    /// <value><c>#d2b48cff</c>.</value>
    public static Color Tan => new(0xD2B48CFFu);

    /// <summary>Gets the standard Teal color.</summary>
    /// <value><c>#008080ff</c>.</value>
    public static Color Teal => new(0x008080FFu);

    /// <summary>Gets the standard Thistle color.</summary>
    /// <value><c>#d8bfd8ff</c>.</value>
    public static Color Thistle => new(0xD8BFD8FFu);

    /// <summary>Gets the standard Tomato color.</summary>
    /// <value><c>#ff6347ff</c>.</value>
    public static Color Tomato => new(0xFF6347FFu);

    /// <summary>Gets the standard Transparent color.</summary>
    /// <value><c>#ffffff00</c>.</value>
    public static Color Transparent => new(0xFFFFFF00u);

    /// <summary>Gets the standard Turquoise color.</summary>
    /// <value><c>#40e0d0ff</c>.</value>
    public static Color Turquoise => new(0x40E0D0FFu);

    /// <summary>Gets the standard Violet color.</summary>
    /// <value><c>#ee82eeff</c>.</value>
    public static Color Violet => new(0xEE82EEFFu);

    /// <summary>Gets the standard WebGray color.</summary>
    /// <value><c>#808080ff</c>.</value>
    public static Color WebGray => new(0x808080FFu);

    /// <summary>Gets the standard WebGreen color.</summary>
    /// <value><c>#008000ff</c>.</value>
    public static Color WebGreen => new(0x008000FFu);

    /// <summary>Gets the standard WebMaroon color.</summary>
    /// <value><c>#800000ff</c>.</value>
    public static Color WebMaroon => new(0x800000FFu);

    /// <summary>Gets the standard WebPurple color.</summary>
    /// <value><c>#800080ff</c>.</value>
    public static Color WebPurple => new(0x800080FFu);

    /// <summary>Gets the standard Wheat color.</summary>
    /// <value><c>#f5deb3ff</c>.</value>
    public static Color Wheat => new(0xF5DEB3FFu);

    /// <summary>Gets the standard White color.</summary>
    /// <value><c>#ffffffff</c>.</value>
    public static Color White => new(0xFFFFFFFFu);

    /// <summary>Gets the standard WhiteSmoke color.</summary>
    /// <value><c>#f5f5f5ff</c>.</value>
    public static Color WhiteSmoke => new(0xF5F5F5FFu);

    /// <summary>Gets the standard Yellow color.</summary>
    /// <value><c>#ffff00ff</c>.</value>
    public static Color Yellow => new(0xFFFF00FFu);

    /// <summary>Gets the standard YellowGreen color.</summary>
    /// <value><c>#9acd32ff</c>.</value>
    public static Color YellowGreen => new(0x9ACD32FFu);

    internal static bool TryGetNamed(string name, out Color color)
    {
        var normalized = name
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("'", string.Empty, StringComparison.Ordinal)
            .Replace(".", string.Empty, StringComparison.Ordinal)
            .ToUpperInvariant();
        return NamedColors.TryGetValue(normalized, out color);
    }
}

