using System.Text;

namespace Electron2D;

internal enum DiagnosticTextFormat
{
    Line,
    Raw,
    Rich
}

internal static class RichDiagnosticText
{
    internal static string Format(string message, bool ansi)
    {
        var output = new StringBuilder(message.Length);
        var position = 0;
        var closing = -1;
        while (position < message.Length)
        {
            var opening = message.IndexOf('[', position);
            if (opening < 0)
            {
                break;
            }

            if (closing < opening)
            {
                closing = message.IndexOf(']', opening + 1);
                if (closing < 0)
                {
                    break;
                }
            }

            output.Append(message.AsSpan(position, opening - position));
            var tag = message.AsSpan(opening + 1, closing - opening - 1);
            if (AppendTag(output, tag, ansi))
            {
                position = closing + 1;
            }
            else
            {
                // A literal bracket may precede a supported tag inside the same span.
                output.Append('[');
                position = opening + 1;
            }
        }

        output.Append(message.AsSpan(position));
        if (ansi)
        {
            output.Append("\u001b[0m");
        }

        return output.ToString();
    }

    private static bool AppendTag(StringBuilder output, ReadOnlySpan<char> tag, bool ansi)
    {
        var layout = tag switch
        {
            "indent" => "    ",
            "center" => "\n\t\t\t",
            "right" => "\n\t\t\t\t\t\t",
            "/indent" or "/center" or "/right" or "url" or "/url" => "",
            _ => null
        };
        if (layout is not null)
        {
            output.Append(layout);
            return true;
        }

        var style = tag switch
        {
            "b" => "1",
            "i" => "3",
            "u" => "4",
            "s" => "9",
            "code" => "2",
            "/b" or "/code" => "22",
            "/i" => "23",
            "/u" => "24",
            "/s" => "29",
            "/color" => "39",
            "/bgcolor" => "49",
            "/fgcolor" => "39;49",
            _ => null
        };
        if (style is not null)
        {
            if (ansi)
            {
                output.Append("\u001b[").Append(style).Append('m');
            }

            return true;
        }

        var color = tag.StartsWith("color=", StringComparison.Ordinal);
        var background = tag.StartsWith("bgcolor=", StringComparison.Ordinal);
        var both = tag.StartsWith("fgcolor=", StringComparison.Ordinal);
        if (!color && !background && !both)
        {
            return false;
        }

        if (ansi)
        {
            var name = tag[(color ? 6 : 8)..];
            output.Append("\u001b[");
            if (!background)
            {
                AppendColor(output, name, background: false);
            }

            if (both)
            {
                output.Append(';');
            }

            if (!color)
            {
                AppendColor(output, name, background: true);
            }

            output.Append('m');
        }

        return true;
    }

    private static void AppendColor(StringBuilder output, ReadOnlySpan<char> name, bool background)
    {
        var code = name switch
        {
            "black" => 30,
            "red" => 91,
            "green" or "lime" => 92,
            "yellow" => 93,
            "blue" => 94,
            "magenta" => 95,
            "cyan" => 96,
            "white" => 97,
            "gray" => 90,
            _ => 0
        };
        if (code != 0)
        {
            output.Append(System.Globalization.CultureInfo.InvariantCulture, $"{code + (background ? 10 : 0)}");
            return;
        }

        var index = name switch
        {
            "pink" => 218,
            "purple" => 98,
            "orange" => 208,
            _ => 0
        };
        var plane = background ? 48 : 38;
        if (index != 0)
        {
            output.Append(System.Globalization.CultureInfo.InvariantCulture, $"{plane};5;{index}");
            return;
        }

        var color = Color.FromString(name.ToString(), default);
        output.Append(System.Globalization.CultureInfo.InvariantCulture, $"{plane};2;{(int)(color.R * 255)};{(int)(color.G * 255)};{(int)(color.B * 255)}");
    }
}
