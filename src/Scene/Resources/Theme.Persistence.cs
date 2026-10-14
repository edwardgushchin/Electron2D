namespace Electron2D;

public partial class Theme
{
    private static readonly PropertyDescriptor[] ThemeStructureProperties =
    [
        new PropertyDescriptor<Theme, string[]>("__ThemeGroups", theme => theme.CaptureGroups(), (theme, value) => theme.RestoreGroups(value), _ => [], stored: true)
    ];
    private static readonly PropertyDescriptor ThemeVariationOrderProperty =
        new PropertyDescriptor<Theme, string[]>("__ThemeVariationOrder", theme => theme.CaptureVariationOrder(), (theme, value) => theme.RestoreVariationOrder(value), _ => [], stored: true);

    internal static PropertyDescriptor? StoredProperty(string path, Type valueType)
    {
        var parts = path.Split('/');
        if (parts.Length is not (2 or 3)) return null;
        ValidateName(parts[0], true);
        if (parts.Length == 2)
            return parts[1] == "base_type" && parts[0].Length != 0 && valueType == typeof(string)
                ? new PropertyDescriptor<Theme, string>(path, theme => theme.GetTypeVariationBase(parts[0]), (theme, value) => theme.SetTypeVariation(parts[0], value), stored: true) : null;
        ValidateName(parts[2], false); var type = parts[0]; var name = parts[2];
        return parts[1] switch
        {
            "colors" when valueType == typeof(Color) => new PropertyDescriptor<Theme, Color>(path, theme => theme.GetColor(name, type), (theme, value) => theme.SetColor(name, type, value), stored: true),
            "constants" when valueType == typeof(int) => new PropertyDescriptor<Theme, int>(path, theme => theme.GetConstant(name, type), (theme, value) => theme.SetConstant(name, type, value), stored: true),
            "font_sizes" when valueType == typeof(int) => new PropertyDescriptor<Theme, int>(path, theme => theme.StoredValue(theme._fontSizes, name, type), (theme, value) => theme.SetFontSize(name, type, value), stored: true),
            "fonts" when valueType == typeof(Font) => new PropertyDescriptor<Theme, Font?>(path, theme => theme.StoredValue(theme._fonts, name, type), (theme, value) => theme.SetFont(name, type, value), stored: true),
            "icons" when valueType == typeof(Texture) => new PropertyDescriptor<Theme, Texture?>(path, theme => theme.StoredValue(theme._icons, name, type), (theme, value) => theme.SetIcon(name, type, value), stored: true),
            "styles" when valueType == typeof(StyleBox) => new PropertyDescriptor<Theme, StyleBox?>(path, theme => theme.StoredValue(theme._styles, name, type), (theme, value) => theme.SetStyleBox(name, type, value), stored: true),
            _ => null
        };
    }
    private string[] CaptureGroups()
    {
        lock (_gate)
        {
            ThrowIfDisposed(); var groups = new List<string>();
            foreach (var type in _colors.Keys) groups.Add("colors/" + type);
            foreach (var type in _constants.Keys) groups.Add("constants/" + type);
            foreach (var type in _fontSizes.Keys) groups.Add("font_sizes/" + type);
            foreach (var type in _fonts.Keys) groups.Add("fonts/" + type);
            foreach (var type in _icons.Keys) groups.Add("icons/" + type);
            foreach (var type in _styles.Keys) groups.Add("styles/" + type);
            return [.. groups];
        }
    }
    private void RestoreGroups(string[] groups)
    {
        ArgumentNullException.ThrowIfNull(groups); var seen = new HashSet<string>(StringComparer.Ordinal);
        var parsed = new (string Category, string Type)[groups.Length];
        for (var i = 0; i < groups.Length; i++)
        {
            var parts = groups[i]?.Split('/');
            if (parts is not { Length: 2 } || !seen.Add(groups[i])) throw new InvalidDataException("Invalid or repeated theme group.");
            ValidateName(parts[1], true);
            if (parts[0] is not ("colors" or "constants" or "font_sizes" or "fonts" or "icons" or "styles")) throw new InvalidDataException("Unknown theme category.");
            parsed[i] = (parts[0], parts[1]);
        }
        lock (_gate)
        {
            ThrowIfDisposed();
            foreach (var item in parsed)
                switch (item.Category)
                {
                    case "colors": EnsureType(_colors, item.Type); break;
                    case "constants": EnsureType(_constants, item.Type); break;
                    case "font_sizes": EnsureType(_fontSizes, item.Type); break;
                    case "fonts": EnsureType(_fonts, item.Type); break;
                    case "icons": EnsureType(_icons, item.Type); break;
                    case "styles": EnsureType(_styles, item.Type); break;
                }
        }
        Publish(true);
    }
    private string[] CaptureVariationOrder()
    {
        lock (_gate)
        {
            ThrowIfDisposed(); var result = new List<string>();
            foreach (var entry in _variationChildren) foreach (var child in entry.Value) { result.Add(entry.Key); result.Add(child); }
            return [.. result];
        }
    }
    private void RestoreVariationOrder(string[] pairs)
    {
        ArgumentNullException.ThrowIfNull(pairs);
        if ((pairs.Length & 1) != 0) throw new InvalidDataException("Incomplete theme variation order.");
        lock (_gate)
        {
            ThrowIfDisposed(); var children = new Dictionary<string, List<string>>(StringComparer.Ordinal); var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < pairs.Length; i += 2)
            {
                ValidateName(pairs[i], true); ValidateName(pairs[i + 1], false);
                if (!seen.Add(pairs[i + 1]) || !_variations.TryGetValue(pairs[i + 1], out var parent) || parent != pairs[i]) throw new InvalidDataException("Theme variation order does not match its links.");
                if (!children.TryGetValue(parent, out var list)) children[parent] = list = [];
                list.Add(pairs[i + 1]);
            }
            if (seen.Count != _variations.Count) throw new InvalidDataException("Theme variation order omits links.");
            _variationChildren.Clear(); foreach (var entry in children) _variationChildren.Add(entry.Key, entry.Value);
        }
        Publish(false);
    }
}
