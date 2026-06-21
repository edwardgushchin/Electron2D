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
using System.Buffers.Binary;
using System.Text;

namespace Electron2D;

internal static class FontMetadataReader
{
    public static FontSourceInfo Read(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var bytes = File.ReadAllBytes(path);
        if (bytes.Length < 12)
        {
            throw new FormatException("Font source is too small to contain an sfnt header.");
        }

        var format = ReadFormat(bytes);
        var nameTable = FindTable(bytes, "name");
        var names = nameTable is null ? new FontNameRecords() : ReadNameTable(bytes, nameTable.Value.Offset, nameTable.Value.Length);
        var fallbackFamily = Path.GetFileNameWithoutExtension(path);
        var familyName = FirstNonEmpty(names.FamilyName, fallbackFamily);
        var styleName = FirstNonEmpty(names.StyleName, "Regular");
        var fullName = FirstNonEmpty(names.FullName, familyName + " " + styleName);
        var postScriptName = FirstNonEmpty(names.PostScriptName, fullName.Replace(" ", string.Empty, StringComparison.Ordinal));

        return new FontSourceInfo(format, familyName, styleName, fullName, postScriptName);
    }

    private static FontSourceFormat ReadFormat(ReadOnlySpan<byte> bytes)
    {
        if (bytes[0] == 0x00 && bytes[1] == 0x01 && bytes[2] == 0x00 && bytes[3] == 0x00)
        {
            return FontSourceFormat.Ttf;
        }

        if (Encoding.ASCII.GetString(bytes[..4]) == "OTTO")
        {
            return FontSourceFormat.Otf;
        }

        throw new FormatException("Font source is not a supported TTF or OTF sfnt file.");
    }

    private static FontTableRecord? FindTable(ReadOnlySpan<byte> bytes, string tag)
    {
        var tableCount = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(4, 2));
        var directoryOffset = 12;
        for (var index = 0; index < tableCount; index++)
        {
            var recordOffset = directoryOffset + index * 16;
            if (recordOffset + 16 > bytes.Length)
            {
                throw new FormatException("Font table directory is malformed.");
            }

            var currentTag = Encoding.ASCII.GetString(bytes.Slice(recordOffset, 4));
            var offset = BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(recordOffset + 8, 4));
            var length = BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(recordOffset + 12, 4));
            if (offset > int.MaxValue || length > int.MaxValue || offset + length > bytes.Length)
            {
                throw new FormatException($"Font table '{currentTag}' range is malformed.");
            }

            if (currentTag == tag)
            {
                return new FontTableRecord((int)offset, (int)length);
            }
        }

        return null;
    }

    private static FontNameRecords ReadNameTable(ReadOnlySpan<byte> bytes, int tableOffset, int tableLength)
    {
        if (tableLength < 6 || tableOffset + tableLength > bytes.Length)
        {
            throw new FormatException("Font name table is malformed.");
        }

        var table = bytes.Slice(tableOffset, tableLength);
        var count = BinaryPrimitives.ReadUInt16BigEndian(table.Slice(2, 2));
        var stringOffset = BinaryPrimitives.ReadUInt16BigEndian(table.Slice(4, 2));
        var records = new FontNameRecords();

        for (var index = 0; index < count; index++)
        {
            var recordOffset = 6 + index * 12;
            if (recordOffset + 12 > table.Length)
            {
                throw new FormatException("Font name table record is malformed.");
            }

            var platformId = BinaryPrimitives.ReadUInt16BigEndian(table.Slice(recordOffset, 2));
            var nameId = BinaryPrimitives.ReadUInt16BigEndian(table.Slice(recordOffset + 6, 2));
            var length = BinaryPrimitives.ReadUInt16BigEndian(table.Slice(recordOffset + 8, 2));
            var offset = BinaryPrimitives.ReadUInt16BigEndian(table.Slice(recordOffset + 10, 2));
            var absoluteOffset = stringOffset + offset;
            if (absoluteOffset + length > table.Length)
            {
                throw new FormatException("Font name string range is malformed.");
            }

            var value = DecodeNameString(table.Slice(absoluteOffset, length), platformId);
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            records.SetIfEmpty(nameId, value);
        }

        return records;
    }

    private static string DecodeNameString(ReadOnlySpan<byte> value, ushort platformId)
    {
        return platformId is 0 or 3
            ? Encoding.BigEndianUnicode.GetString(value)
            : Encoding.ASCII.GetString(value);
    }

    private static string FirstNonEmpty(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private readonly struct FontTableRecord
    {
        public FontTableRecord(int offset, int length)
        {
            Offset = offset;
            Length = length;
        }

        public int Offset { get; }

        public int Length { get; }
    }

    private sealed class FontNameRecords
    {
        public string? FamilyName { get; private set; }

        public string? StyleName { get; private set; }

        public string? FullName { get; private set; }

        public string? PostScriptName { get; private set; }

        public void SetIfEmpty(ushort nameId, string value)
        {
            switch (nameId)
            {
                case 1 when string.IsNullOrWhiteSpace(FamilyName):
                    FamilyName = value;
                    break;
                case 2 when string.IsNullOrWhiteSpace(StyleName):
                    StyleName = value;
                    break;
                case 4 when string.IsNullOrWhiteSpace(FullName):
                    FullName = value;
                    break;
                case 6 when string.IsNullOrWhiteSpace(PostScriptName):
                    PostScriptName = value;
                    break;
            }
        }
    }
}

internal readonly struct FontSourceInfo
{
    public FontSourceInfo(
        FontSourceFormat format,
        string familyName,
        string styleName,
        string fullName,
        string postScriptName)
    {
        Format = format;
        FamilyName = familyName;
        StyleName = styleName;
        FullName = fullName;
        PostScriptName = postScriptName;
    }

    public FontSourceFormat Format { get; }

    public string FamilyName { get; }

    public string StyleName { get; }

    public string FullName { get; }

    public string PostScriptName { get; }
}
