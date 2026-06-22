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

internal static class ThemeResourceMetadata
{
    public static void Register()
    {
        ResourceObjectMetadataRegistry.Register(
            ResourceObjectTypeMetadata.Create<StyleBoxFlat>(
                "Electron2D.StyleBoxFlat",
                static () => new StyleBoxFlat(),
                [
                    ResourceObjectPropertyMetadata.Create<StyleBoxFlat, Color>("bg_color", static box => box.BgColor, static (box, value) => box.BgColor = value),
                    ResourceObjectPropertyMetadata.Create<StyleBoxFlat, Color>("border_color", static box => box.BorderColor, static (box, value) => box.BorderColor = value),
                    ResourceObjectPropertyMetadata.Create<StyleBoxFlat, int>("border_width_bottom", static box => box.BorderWidthBottom, static (box, value) => box.BorderWidthBottom = value),
                    ResourceObjectPropertyMetadata.Create<StyleBoxFlat, int>("border_width_left", static box => box.BorderWidthLeft, static (box, value) => box.BorderWidthLeft = value),
                    ResourceObjectPropertyMetadata.Create<StyleBoxFlat, int>("border_width_right", static box => box.BorderWidthRight, static (box, value) => box.BorderWidthRight = value),
                    ResourceObjectPropertyMetadata.Create<StyleBoxFlat, int>("border_width_top", static box => box.BorderWidthTop, static (box, value) => box.BorderWidthTop = value),
                    ResourceObjectPropertyMetadata.Create<StyleBoxFlat, float>("content_margin_bottom", static box => box.ContentMarginBottom, static (box, value) => box.ContentMarginBottom = value),
                    ResourceObjectPropertyMetadata.Create<StyleBoxFlat, float>("content_margin_left", static box => box.ContentMarginLeft, static (box, value) => box.ContentMarginLeft = value),
                    ResourceObjectPropertyMetadata.Create<StyleBoxFlat, float>("content_margin_right", static box => box.ContentMarginRight, static (box, value) => box.ContentMarginRight = value),
                    ResourceObjectPropertyMetadata.Create<StyleBoxFlat, float>("content_margin_top", static box => box.ContentMarginTop, static (box, value) => box.ContentMarginTop = value)
                ]));

        ResourceObjectMetadataRegistry.Register(
            ResourceObjectTypeMetadata.Create<Theme>(
                "Electron2D.Theme",
                static () => new Theme(),
                [
                    ResourceObjectPropertyMetadata.Create<Theme, float>("default_base_scale", static theme => theme.DefaultBaseScale, static (theme, value) => theme.DefaultBaseScale = value),
                    ResourceObjectPropertyMetadata.Create<Theme, int>("default_font_size", static theme => theme.DefaultFontSize, static (theme, value) => theme.DefaultFontSize = value),
                    ResourceObjectPropertyMetadata.CreateSerialized<Theme>("colors", CaptureColors, RestoreColors),
                    ResourceObjectPropertyMetadata.CreateSerialized<Theme>("constants", CaptureConstants, RestoreConstants),
                    ResourceObjectPropertyMetadata.CreateSerialized<Theme>("font_sizes", CaptureFontSizes, RestoreFontSizes),
                    ResourceObjectPropertyMetadata.CreateSerialized<Theme>("styleboxes", CaptureStyleBoxes, RestoreStyleBoxes)
                ]));
    }

    private static SerializedPropertyValue CaptureColors(Theme theme)
    {
        return Array(theme.GetColorItemsForSerialization().Select(item => Object(
            Property("name", Value(item.Name)),
            Property("theme_type", Value(item.ThemeType)),
            Property("value", Value(item.Value)))));
    }

    private static void RestoreColors(Theme theme, SerializedPropertyValue value)
    {
        foreach (var item in ReadArray(value, "Theme colors"))
        {
            var properties = ReadObject(item, "Theme color");
            theme.SetColor(
                ReadString(Required(properties, "name", "Theme color"), "Theme color name"),
                ReadString(Required(properties, "theme_type", "Theme color type"), "Theme color type"),
                ReadColor(Required(properties, "value", "Theme color value"), "Theme color value"));
        }
    }

    private static SerializedPropertyValue CaptureConstants(Theme theme)
    {
        return Array(theme.GetConstantItemsForSerialization().Select(item => Object(
            Property("name", Value(item.Name)),
            Property("theme_type", Value(item.ThemeType)),
            Property("value", Value(item.Value)))));
    }

    private static void RestoreConstants(Theme theme, SerializedPropertyValue value)
    {
        foreach (var item in ReadArray(value, "Theme constants"))
        {
            var properties = ReadObject(item, "Theme constant");
            theme.SetConstant(
                ReadString(Required(properties, "name", "Theme constant"), "Theme constant name"),
                ReadString(Required(properties, "theme_type", "Theme constant type"), "Theme constant type"),
                ReadInt32(Required(properties, "value", "Theme constant value"), "Theme constant value"));
        }
    }

    private static SerializedPropertyValue CaptureFontSizes(Theme theme)
    {
        return Array(theme.GetFontSizeItemsForSerialization().Select(item => Object(
            Property("name", Value(item.Name)),
            Property("theme_type", Value(item.ThemeType)),
            Property("value", Value(item.Value)))));
    }

    private static void RestoreFontSizes(Theme theme, SerializedPropertyValue value)
    {
        foreach (var item in ReadArray(value, "Theme font sizes"))
        {
            var properties = ReadObject(item, "Theme font size");
            theme.SetFontSize(
                ReadString(Required(properties, "name", "Theme font size"), "Theme font size name"),
                ReadString(Required(properties, "theme_type", "Theme font size type"), "Theme font size type"),
                ReadInt32(Required(properties, "value", "Theme font size value"), "Theme font size value"));
        }
    }

    private static SerializedPropertyValue CaptureStyleBoxes(Theme theme)
    {
        return Array(theme.GetStyleBoxItemsForSerialization().Select(item =>
        {
            if (item.Value is not StyleBoxFlat flat)
            {
                throw new InvalidOperationException($"Theme style box '{item.ThemeType}.{item.Name}' uses unsupported resource type '{item.Value.GetType().FullName}'.");
            }

            return Object(
                Property("name", Value(item.Name)),
                Property("theme_type", Value(item.ThemeType)),
                Property("kind", Value("StyleBoxFlat")),
                Property("bg_color", Value(flat.BgColor)),
                Property("border_color", Value(flat.BorderColor)),
                Property("border_width_bottom", Value(flat.BorderWidthBottom)),
                Property("border_width_left", Value(flat.BorderWidthLeft)),
                Property("border_width_right", Value(flat.BorderWidthRight)),
                Property("border_width_top", Value(flat.BorderWidthTop)),
                Property("content_margin_bottom", Value(flat.ContentMarginBottom)),
                Property("content_margin_left", Value(flat.ContentMarginLeft)),
                Property("content_margin_right", Value(flat.ContentMarginRight)),
                Property("content_margin_top", Value(flat.ContentMarginTop)));
        }));
    }

    private static void RestoreStyleBoxes(Theme theme, SerializedPropertyValue value)
    {
        foreach (var item in ReadArray(value, "Theme style boxes"))
        {
            var properties = ReadObject(item, "Theme style box");
            var kind = ReadString(Required(properties, "kind", "Theme style box kind"), "Theme style box kind");
            if (!string.Equals(kind, "StyleBoxFlat", StringComparison.Ordinal))
            {
                throw new FormatException($"Theme style box kind '{kind}' is not supported.");
            }

            var flat = new StyleBoxFlat
            {
                BgColor = ReadColor(Required(properties, "bg_color", "Theme style box background"), "Theme style box background"),
                BorderColor = ReadColor(Required(properties, "border_color", "Theme style box border color"), "Theme style box border color"),
                BorderWidthBottom = ReadInt32(Required(properties, "border_width_bottom", "Theme style box bottom border"), "Theme style box bottom border"),
                BorderWidthLeft = ReadInt32(Required(properties, "border_width_left", "Theme style box left border"), "Theme style box left border"),
                BorderWidthRight = ReadInt32(Required(properties, "border_width_right", "Theme style box right border"), "Theme style box right border"),
                BorderWidthTop = ReadInt32(Required(properties, "border_width_top", "Theme style box top border"), "Theme style box top border"),
                ContentMarginBottom = ReadFloat(Required(properties, "content_margin_bottom", "Theme style box bottom margin"), "Theme style box bottom margin"),
                ContentMarginLeft = ReadFloat(Required(properties, "content_margin_left", "Theme style box left margin"), "Theme style box left margin"),
                ContentMarginRight = ReadFloat(Required(properties, "content_margin_right", "Theme style box right margin"), "Theme style box right margin"),
                ContentMarginTop = ReadFloat(Required(properties, "content_margin_top", "Theme style box top margin"), "Theme style box top margin")
            };

            theme.SetStyleBox(
                ReadString(Required(properties, "name", "Theme style box name"), "Theme style box name"),
                ReadString(Required(properties, "theme_type", "Theme style box type"), "Theme style box type"),
                flat);
        }
    }

    private static SerializedPropertyDictionaryEntry Property(string name, SerializedPropertyValue value)
    {
        return new SerializedPropertyDictionaryEntry(Value(name), value);
    }

    private static SerializedPropertyValue Object(params SerializedPropertyDictionaryEntry[] properties)
    {
        return SerializedPropertyValue.FromDictionary(properties);
    }

    private static SerializedPropertyValue Array(IEnumerable<SerializedPropertyValue> values)
    {
        return SerializedPropertyValue.FromArray(values);
    }

    private static SerializedPropertyValue Value(string value)
    {
        return SerializedPropertyValue.FromVariant(value);
    }

    private static SerializedPropertyValue Value(int value)
    {
        return SerializedPropertyValue.FromVariant(value);
    }

    private static SerializedPropertyValue Value(float value)
    {
        return SerializedPropertyValue.FromVariant(value);
    }

    private static SerializedPropertyValue Value(Color value)
    {
        return SerializedPropertyValue.FromVariant(value);
    }

    private static IReadOnlyList<SerializedPropertyValue> ReadArray(SerializedPropertyValue value, string context)
    {
        if (value.Kind != SerializedPropertyValueKind.Array)
        {
            throw new FormatException($"{context} must be an array.");
        }

        return value.Items;
    }

    private static IReadOnlyDictionary<string, SerializedPropertyValue> ReadObject(SerializedPropertyValue value, string context)
    {
        if (value.Kind != SerializedPropertyValueKind.Dictionary)
        {
            throw new FormatException($"{context} must be an object.");
        }

        return value.DictionaryEntries.ToDictionary(
            entry => ReadString(entry.Key, $"{context} property name"),
            entry => entry.Value,
            StringComparer.Ordinal);
    }

    private static SerializedPropertyValue Required(
        IReadOnlyDictionary<string, SerializedPropertyValue> values,
        string name,
        string context)
    {
        return values.TryGetValue(name, out var value)
            ? value
            : throw new FormatException($"{context} is missing required property '{name}'.");
    }

    private static string ReadString(SerializedPropertyValue value, string context)
    {
        try
        {
            return SerializedPropertyValueConverter.ToValue<string>(value);
        }
        catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException)
        {
            throw new FormatException($"{context} must be a string.", exception);
        }
    }

    private static int ReadInt32(SerializedPropertyValue value, string context)
    {
        try
        {
            return SerializedPropertyValueConverter.ToValue<int>(value);
        }
        catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException)
        {
            throw new FormatException($"{context} must be an integer.", exception);
        }
    }

    private static float ReadFloat(SerializedPropertyValue value, string context)
    {
        try
        {
            return SerializedPropertyValueConverter.ToValue<float>(value);
        }
        catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException)
        {
            throw new FormatException($"{context} must be a float.", exception);
        }
    }

    private static Color ReadColor(SerializedPropertyValue value, string context)
    {
        try
        {
            return SerializedPropertyValueConverter.ToValue<Color>(value);
        }
        catch (Exception exception) when (exception is FormatException or InvalidCastException)
        {
            throw new FormatException($"{context} must be a color.", exception);
        }
    }
}
