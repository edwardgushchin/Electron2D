using System.Globalization;

namespace Electron2D;

internal static class MathFormatting
{
    public static string Format(float value)
    {
        return value.ToString("0.######", CultureInfo.InvariantCulture);
    }

    public static string Format(int value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }
}
