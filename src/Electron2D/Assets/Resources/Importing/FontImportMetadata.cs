/*
    Electron2D
    MIT License
    Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
    SPDX-License-Identifier: MIT

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/
namespace Electron2D;

internal enum FontSourceFormat
{
    Ttf,
    Otf
}

internal enum FontRasterizationMode
{
    Bitmap,
    Sdf
}

internal sealed class FontImportMetadata
{
    public const string FormatName = "Electron2D.FontImportMetadata";
    public const int CurrentVersion = 1;

    public FontImportMetadata(
        string sourcePath,
        long uid,
        FontSourceFormat format,
        string familyName,
        string styleName,
        string fullName,
        string postScriptName,
        IEnumerable<string>? fallbackFontPaths = null,
        FontRasterizationSettings? rasterization = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(familyName);
        ArgumentException.ThrowIfNullOrWhiteSpace(styleName);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(postScriptName);

        if (uid <= 0 || uid == ResourceUid.InvalidId)
        {
            throw new ArgumentException("Font import UID must be positive.", nameof(uid));
        }

        SourcePath = sourcePath;
        Uid = uid;
        Format = format;
        FamilyName = familyName;
        StyleName = styleName;
        FullName = fullName;
        PostScriptName = postScriptName;
        FallbackFontPaths = (fallbackFontPaths ?? Array.Empty<string>())
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        Rasterization = rasterization ?? FontRasterizationSettings.DefaultBitmap;
    }

    public string SourcePath { get; }

    public long Uid { get; }

    public string UidText => ResourceUid.IdToText(Uid);

    public FontSourceFormat Format { get; }

    public string FamilyName { get; }

    public string StyleName { get; }

    public string FullName { get; }

    public string PostScriptName { get; }

    public IReadOnlyList<string> FallbackFontPaths { get; }

    public FontRasterizationSettings Rasterization { get; }
}

internal sealed class FontRasterizationSettings
{
    public static readonly FontRasterizationSettings DefaultBitmap = new(FontRasterizationMode.Bitmap, 16, 0);

    public FontRasterizationSettings(FontRasterizationMode mode, int baseSize, int sdfSpread)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(baseSize);
        ArgumentOutOfRangeException.ThrowIfNegative(sdfSpread);

        Mode = mode;
        BaseSize = baseSize;
        SdfSpread = mode == FontRasterizationMode.Bitmap ? 0 : sdfSpread;
    }

    public FontRasterizationMode Mode { get; }

    public int BaseSize { get; }

    public int SdfSpread { get; }
}
