using System.Globalization;
using System.Text;

namespace Electron2D;

public static partial class ByteBuffer
{
    private static readonly Encoding UTF8 = new UTF8Encoding(false, true);
    private static readonly Encoding UTF16LE = new UnicodeEncoding(false, false, true);
    private static readonly Encoding UTF16BE = new UnicodeEncoding(true, false, true);
    private static readonly Encoding UTF32LE = new UTF32Encoding(false, false, true);
    private static readonly Encoding UTF32BE = new UTF32Encoding(true, false, true);

    /// <summary>Maps Latin-1 bytes to characters, stopping at the first zero byte.</summary>
    /// <param name="data">Borrowed text bytes.</param><returns>A new string.</returns>
    public static string GetStringFromASCII(ReadOnlySpan<byte> data)
    {
        var zero = data.IndexOf((byte)0);
        return Encoding.Latin1.GetString(zero < 0 ? data : data[..zero]);
    }

    /// <summary>Decodes strict UTF-8, removing an optional leading byte-order mark.</summary>
    /// <param name="data">Borrowed text bytes; embedded zero characters are retained.</param><returns>A new string.</returns>
    /// <exception cref="DecoderFallbackException">The text is malformed or truncated.</exception>
    public static string GetStringFromUTF8(ReadOnlySpan<byte> data) => UTF8.GetString(data.StartsWith("\uFEFF"u8) ? data[3..] : data);

    /// <summary>Decodes strict UTF-16, honoring a leading byte-order mark and otherwise assuming little endian.</summary>
    /// <param name="data">Borrowed complete text bytes.</param><returns>A new string.</returns>
    /// <exception cref="DecoderFallbackException">The text is malformed or truncated.</exception>
    public static string GetStringFromUTF16(ReadOnlySpan<byte> data)
    {
        if (data.Length >= 2 && data[0] == 0xFE && data[1] == 0xFF) return UTF16BE.GetString(data[2..]);
        return UTF16LE.GetString(data.Length >= 2 && data[0] == 0xFF && data[1] == 0xFE ? data[2..] : data);
    }

    /// <summary>Decodes strict UTF-32, honoring a leading byte-order mark and otherwise assuming little endian.</summary>
    /// <param name="data">Borrowed complete text bytes.</param><returns>A new string.</returns>
    /// <exception cref="DecoderFallbackException">The text is malformed or truncated.</exception>
    public static string GetStringFromUTF32(ReadOnlySpan<byte> data)
    {
        if (data.Length >= 4 && data[0] == 0 && data[1] == 0 && data[2] == 0xFE && data[3] == 0xFF) return UTF32BE.GetString(data[4..]);
        return UTF32LE.GetString(data.Length >= 4 && data[0] == 0xFF && data[1] == 0xFE && data[2] == 0 && data[3] == 0 ? data[4..] : data);
    }

    /// <summary>Decodes native wide characters: UTF-16 on Windows, UTF-32 on other hosts.</summary>
    /// <param name="data">Borrowed complete wide-character bytes.</param><returns>A new string.</returns>
    /// <exception cref="DecoderFallbackException">The text is malformed or truncated.</exception>
    public static string GetStringFromWChar(ReadOnlySpan<byte> data) => OperatingSystem.IsWindows() ? GetStringFromUTF16(data) : GetStringFromUTF32(data);

    /// <summary>Decodes a named .NET multibyte encoding with strict error handling.</summary>
    /// <param name="data">Borrowed encoded bytes.</param>
    /// <param name="encoding">A .NET encoding name; empty uses the Windows culture ANSI code page or UTF-8 on other hosts.</param>
    /// <returns>A new string. The framework code-page provider is used locally without changing the process registry.</returns>
    /// <exception cref="ArgumentException">The encoding is null, unknown or unavailable.</exception>
    /// <exception cref="DecoderFallbackException">The text is malformed or truncated.</exception>
    public static string GetStringFromMultibyteChar(ReadOnlySpan<byte> data, string encoding = "")
    {
        ArgumentNullException.ThrowIfNull(encoding);
        if (encoding.Length == 0) encoding = OperatingSystem.IsWindows() ? "cp" + CultureInfo.CurrentCulture.TextInfo.ANSICodePage.ToString(CultureInfo.InvariantCulture) : "utf-8";
        var codec = CodePagesEncodingProvider.Instance.GetEncoding(encoding, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback)
            ?? Encoding.GetEncoding(encoding, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
        return codec.GetString(data);
    }

    /// <summary>Encodes consecutive UTF-8 strings, each followed by a zero byte.</summary>
    /// <param name="data">Borrowed strings, including empty strings and embedded zero characters.</param>
    /// <returns>A caller-owned byte array. No element count or length prefix is inserted.</returns>
    /// <exception cref="ArgumentNullException">An element is null.</exception>
    /// <exception cref="EncoderFallbackException">A string contains malformed UTF-16.</exception>
    /// <exception cref="OverflowException">The byte count exceeds the signed managed length range.</exception>
    public static byte[] ToByteArray(ReadOnlySpan<string> data)
    {
        var length = 0;
        foreach (var value in data)
        {
            ArgumentNullException.ThrowIfNull(value);
            length = checked(length + UTF8.GetByteCount(value) + 1);
        }
        var result = new byte[length];
        var offset = 0;
        foreach (var value in data) offset += UTF8.GetBytes(value, result.AsSpan(offset)) + 1;
        return result;
    }

    /// <summary>Encodes every source byte as two lowercase hexadecimal characters.</summary>
    /// <param name="data">Borrowed binary bytes.</param><returns>A new hexadecimal string.</returns>
    public static string HexEncode(ReadOnlySpan<byte> data) => Convert.ToHexStringLower(data);
}
