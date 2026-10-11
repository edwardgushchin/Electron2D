using System.Globalization;

namespace Electron2D;

internal abstract class TileCustomDataType(Type type)
{
    internal Type Type { get; } = type;
    internal string ID { get; } = ResourceFileTypes.ValueID(type);
    internal abstract TileCustomDataValue CreateDefault();
    internal abstract TileCustomDataValue ConvertValue(TileCustomDataValue? value);
    internal static TileCustomDataType Register<T>()
    {
        _ = ResourceFileTypes.HasCodec(typeof(bool));
        var registry = ResourceLoader.Runtime.FileTypes;
        lock (registry.Gate)
        {
            if (registry.TileDataTypes.TryGetValue(typeof(T), out var existing)) return existing;
            var created = new TileCustomDataType<T>(); registry.TileDataTypes.Add(typeof(T), created); return created;
        }
    }
    internal static TileCustomDataType? Find(string id)
    {
        _ = ResourceFileTypes.HasCodec(typeof(bool)); var registry = ResourceLoader.Runtime.FileTypes;
        lock (registry.Gate) return id.Length == 0 ? null : registry.TileDataTypes.Values.FirstOrDefault(t => t.ID == id) ?? throw new NotSupportedException($"Custom tile type '{id}' has no prepared typed factory.");
    }
    internal static TileCustomDataType? Find(Type? type)
    {
        _ = ResourceFileTypes.HasCodec(typeof(bool)); var registry = ResourceLoader.Runtime.FileTypes;
        lock (registry.Gate) return type is null ? null : registry.TileDataTypes.GetValueOrDefault(type) ?? throw new NotSupportedException($"Custom tile type {type.Name} has no prepared typed factory.");
    }
}
internal sealed class TileCustomDataType<T>() : TileCustomDataType(typeof(T))
{
    private static T DefaultValue() => typeof(T) == typeof(string) ? (T)(object)string.Empty : typeof(T) == typeof(Color) ? (T)(object)Colors.Black : typeof(T) == typeof(Transform) ? (T)(object)Transform.Identity : typeof(T).IsArray ? (T)(object)Array.CreateInstance(typeof(T).GetElementType()!, new int[typeof(T).GetArrayRank()]) : default!;
    internal override TileCustomDataValue CreateDefault() => new TileCustomDataValue<T>(DefaultValue());
    private static string Text(object? value, int depth = 0)
    {
        if (depth > 64) throw new InvalidOperationException("Custom-data array text exceeds 64 nested levels.");
        if (value is Array array)
        {
            var values = new List<string>(array.Length);
            foreach (var entry in array) values.Add(entry is string text ? System.Text.Json.JsonSerializer.Serialize(text) : Text(entry, depth + 1));
            return "[" + string.Join(", ", values) + "]";
        }
        return value is null ? "null" : value is bool flag ? flag ? "true" : "false" : value is IFormattable number ? number.ToString(null, CultureInfo.InvariantCulture) : value.ToString() ?? string.Empty;
    }
    internal override TileCustomDataValue ConvertValue(TileCustomDataValue? value)
    {
        if (value is TileCustomDataValue<T> exact) return exact;
        var raw = value?.Boxed;
        if (raw is T compatible) return new TileCustomDataValue<T>(compatible);
        if (raw is null) return CreateDefault();
        var type = typeof(T); object? converted = null;
        if (type == typeof(bool) && raw is bool or byte or sbyte or short or ushort or int or uint or long or ulong or float or double or string)
            converted = raw is string text ? text.Length != 0 : Convert.ToDouble(raw, CultureInfo.InvariantCulture) != 0;
        else if (type == typeof(string) && raw is not ElectronObject && raw is not Delegate)
            converted = Text(raw);
        else if (type == typeof(float) || type == typeof(double))
        {
            if (raw is string text) converted = TileCustomDataNumbers.Float(text);
            else if (raw is bool or byte or sbyte or short or ushort or int or uint or long or ulong or float or double) converted = Convert.ToDouble(raw, CultureInfo.InvariantCulture);
            if (converted is double number && type == typeof(float)) converted = (float)number;
        }
        else if (type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort) || type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong))
        {
            if (raw is string text) converted = TileCustomDataNumbers.Integer(text);
            else if (raw is bool or byte or sbyte or short or ushort or int or uint or long or ulong) converted = raw;
            else if (raw is double number) converted = checked((long)number);
            else if (raw is float scalar) converted = checked((long)scalar);
            if (converted is not null) converted = Convert.ChangeType(converted, type, CultureInfo.InvariantCulture);
        }
        else if (type == typeof(Vector2) && raw is Vector2i vector) converted = new Vector2(vector.X, vector.Y);
        else if (type == typeof(Vector2i) && raw is Vector2 vectorFloat) converted = new Vector2i(checked((int)vectorFloat.X), checked((int)vectorFloat.Y));
        else if (type == typeof(Vector4) && raw is Vector4i vector4) converted = new Vector4(vector4.X, vector4.Y, vector4.Z, vector4.W);
        else if (type == typeof(Vector4i) && raw is Vector4 vector4Float) converted = new Vector4i(checked((int)vector4Float.X), checked((int)vector4Float.Y), checked((int)vector4Float.Z), checked((int)vector4Float.W));
        else if (type == typeof(Rect2) && raw is Rect2i rectInteger) converted = (Rect2)rectInteger;
        else if (type == typeof(Rect2i) && raw is Rect2 rectFloat) converted = new Rect2i(new Vector2i(checked((int)rectFloat.Position.X), checked((int)rectFloat.Position.Y)), new Vector2i(checked((int)rectFloat.Size.X), checked((int)rectFloat.Size.Y)));
        else if (type == typeof(Color) && raw is string colorText) converted = Color.FromString(colorText, Colors.Black);
        else if (type == typeof(Color) && raw is int colorInteger) converted = new Color(unchecked((uint)colorInteger));
        else if (type == typeof(Color) && raw is long colorLong) converted = new Color(unchecked((uint)colorLong));
        return converted is T result ? new TileCustomDataValue<T>(result) : CreateDefault();
    }
}
internal static class TileCustomDataNumbers
{
    internal static long Integer(string value)
    {
        ulong number = 0; var positive = true;
        foreach (var c in value)
        {
            if (c == '.') break;
            if (c is >= '0' and <= '9')
            {
                var digit = (uint)(c - '0'); var limit = positive ? (ulong)long.MaxValue : 1UL << 63;
                if (number > (limit - digit) / 10) return positive ? long.MaxValue : long.MinValue;
                number = number * 10 + digit;
            }
            else if (number == 0 && c == '-') positive = !positive;
        }
        return positive ? (long)number : unchecked(-(long)number);
    }
    internal static double Float(string value)
    {
        var span = value.AsSpan().TrimStart(); var i = 0;
        if (i < span.Length && span[i] is '+' or '-') i++;
        var digits = 0;
        while (i < span.Length && char.IsAsciiDigit(span[i])) { i++; digits++; }
        if (i < span.Length && span[i] == '.') { i++; while (i < span.Length && char.IsAsciiDigit(span[i])) { i++; digits++; } }
        if (digits == 0) return 0;
        if (i < span.Length && span[i] is 'e' or 'E')
        {
            var exponent = i++; if (i < span.Length && span[i] is '+' or '-') i++;
            var start = i; while (i < span.Length && char.IsAsciiDigit(span[i])) i++; if (start == i) i = exponent;
        }
        return double.TryParse(span[..i], NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
    }
}
internal abstract class TileCustomDataValue
{
    internal abstract TileCustomDataType Schema { get; }
    internal abstract object? Boxed { get; }
    internal virtual Resource? ResourceValue => null;
    internal abstract TileCustomDataValue Copy(Func<Resource?, Resource?>? duplicate);
    internal abstract PropertyDescriptor Property<TOwner>(string name, Func<TOwner, TileData> data, int index) where TOwner : ElectronObject;
}
internal sealed class TileCustomDataValue<T>(T value) : TileCustomDataValue
{
    internal T Value = value;
    internal override TileCustomDataType Schema => TileCustomDataType.Register<T>();
    internal override object? Boxed => Value;
    internal override Resource? ResourceValue => Value as Resource;
    internal override TileCustomDataValue Copy(Func<Resource?, Resource?>? duplicate)
    {
        if (duplicate is null) return new TileCustomDataValue<T>(Value);
        var stored = new StoredPropertyValue<T>(Value);
        var copied = (StoredPropertyValue<T>)stored.TransformResources(r => duplicate(r)!);
        return new TileCustomDataValue<T>(copied.Value);
    }
    internal override PropertyDescriptor Property<TOwner>(string name, Func<TOwner, TileData> data, int index) =>
        new PropertyDescriptor<TOwner, T>(name, n => data(n).GetCustomDataByLayerID<T>(index), (n, v) => data(n).SetCustomDataByLayerID(index, v), _ => default!, stored: true);
}
