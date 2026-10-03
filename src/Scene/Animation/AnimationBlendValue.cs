using System.Runtime.CompilerServices;
using System.Text;

namespace Electron2D;

// Numeric scratch belongs to one concrete typed property. No heterogeneous value access is exposed.
internal abstract class AnimationBlendValue<T>
{
    internal abstract T Zero { get; }
    internal abstract void Begin(T rest);
    internal abstract void Add(T value, double weight, bool angle);
    internal abstract T Finish(double totalWeight = 1);
    internal static AnimationBlendValue<T>? Create(Func<T, T, double, T>? custom = null)
    {
        if (AnimationNumericBlend<T>.Supported) return new AnimationNumericBlend<T>();
        if (typeof(T) == typeof(Transform)) return new AnimationAlgebraBlend<T>(TweenValue<T>.Interpolate!);
        if (typeof(T) == typeof(string)) return Cast(new AnimationStringBlend());
        if (typeof(T) == typeof(float[])) return Cast(new AnimationArrayBlend<float>());
        if (typeof(T) == typeof(double[])) return Cast(new AnimationArrayBlend<double>());
        if (typeof(T) == typeof(int[])) return Cast(new AnimationArrayBlend<int>());
        if (typeof(T) == typeof(long[])) return Cast(new AnimationArrayBlend<long>());
        if (typeof(T) == typeof(bool[])) return Cast(new AnimationArrayBlend<bool>());
        if (typeof(T) == typeof(Vector2[])) return Cast(new AnimationArrayBlend<Vector2>());
        if (typeof(T) == typeof(Vector2i[])) return Cast(new AnimationArrayBlend<Vector2i>());
        if (typeof(T) == typeof(Vector3[])) return Cast(new AnimationArrayBlend<Vector3>());
        if (typeof(T) == typeof(Vector3i[])) return Cast(new AnimationArrayBlend<Vector3i>());
        if (typeof(T) == typeof(Vector4[])) return Cast(new AnimationArrayBlend<Vector4>());
        if (typeof(T) == typeof(Vector4i[])) return Cast(new AnimationArrayBlend<Vector4i>());
        if (typeof(T) == typeof(Rect2[])) return Cast(new AnimationArrayBlend<Rect2>());
        if (typeof(T) == typeof(Rect2i[])) return Cast(new AnimationArrayBlend<Rect2i>());
        if (typeof(T) == typeof(Transform[])) return Cast(new AnimationArrayBlend<Transform>());
        if (typeof(T) == typeof(int[][])) return Cast(new AnimationArrayBlend<int[]>());
        if (typeof(T) == typeof(Color[])) return Cast(new AnimationArrayBlend<Color>());
        if (typeof(T) == typeof(string[])) return Cast(new AnimationArrayBlend<string>());
        return custom is null ? null : new AnimationAlgebraBlend<T>(custom);
    }
    private static AnimationBlendValue<T> Cast<TElement>(AnimationBlendValue<TElement> value) => (AnimationBlendValue<T>)(object)value;
}
internal sealed class AnimationNumericBlend<T> : AnimationBlendValue<T>
{
    internal static readonly bool Supported = typeof(T) == typeof(float) || typeof(T) == typeof(double) || typeof(T) == typeof(int) || typeof(T) == typeof(long) || typeof(T) == typeof(bool) || typeof(T) == typeof(Vector2) || typeof(T) == typeof(Vector2i) || typeof(T) == typeof(Vector3) || typeof(T) == typeof(Vector3i) || typeof(T) == typeof(Vector4) || typeof(T) == typeof(Vector4i) || typeof(T) == typeof(Color) || typeof(T) == typeof(Rect2) || typeof(T) == typeof(Rect2i);
    private readonly double[] _rest = new double[4], _sum = new double[4], _sample = new double[4];
    private decimal _longRest, _longSum;
    internal override T Zero => default!;
    internal override void Begin(T rest)
    { if (typeof(T) == typeof(long)) { _longRest = Unsafe.As<T, long>(ref rest); _longSum = _longRest; } else { Read(rest, _rest); _rest.CopyTo(_sum, 0); } }
    internal override void Add(T value, double weight, bool angle)
    {
        if (typeof(T) == typeof(long)) { _longSum += (Unsafe.As<T, long>(ref value) - _longRest) * (decimal)weight; return; }
        Read(value, _sample);
        if (angle)
        {
            var rest = Mathf.PosMod(_rest[0], Math.Tau); var from = Mathf.PosMod(_sum[0], Math.Tau); var to = Mathf.PosMod(_sample[0], Math.Tau);
            if (rest < Math.PI) { if (from > rest + Math.PI) from -= Math.Tau; if (to > rest + Math.PI) to -= Math.Tau; }
            else { if (from < rest - Math.PI) from += Math.Tau; if (to < rest - Math.PI) to += Math.Tau; }
            _sum[0] = Mathf.PosMod(from + (to - rest) * weight, Math.Tau); return;
        }
        for (var i = 0; i < 4; i++) _sum[i] += (_sample[i] - _rest[i]) * weight;
    }
    internal override T Finish(double totalWeight = 1)
    {
        if (typeof(T) == typeof(long)) { var result = checked((long)Math.Round(_longSum, 0, MidpointRounding.AwayFromZero)); return Return(result); }
        if (typeof(T) == typeof(float)) return Return((float)_sum[0]);
        if (typeof(T) == typeof(double)) return Return(_sum[0]);
        if (typeof(T) == typeof(int)) return Return(Round(_sum[0]));
        if (typeof(T) == typeof(bool)) return Return(Math.Round(_sum[0], MidpointRounding.AwayFromZero) != 0);
        if (typeof(T) == typeof(Vector2)) return Return(new Vector2((float)_sum[0], (float)_sum[1]));
        if (typeof(T) == typeof(Vector2i)) return Return(new Vector2i(Round(_sum[0]), Round(_sum[1])));
        if (typeof(T) == typeof(Vector3)) return Return(new Vector3((float)_sum[0], (float)_sum[1], (float)_sum[2]));
        if (typeof(T) == typeof(Vector3i)) return Return(new Vector3i(Round(_sum[0]), Round(_sum[1]), Round(_sum[2])));
        if (typeof(T) == typeof(Vector4)) return Return(new Vector4((float)_sum[0], (float)_sum[1], (float)_sum[2], (float)_sum[3]));
        if (typeof(T) == typeof(Vector4i)) return Return(new Vector4i(Round(_sum[0]), Round(_sum[1]), Round(_sum[2]), Round(_sum[3])));
        if (typeof(T) == typeof(Color)) return Return(new Color((float)_sum[0], (float)_sum[1], (float)_sum[2], (float)_sum[3]));
        if (typeof(T) == typeof(Rect2)) return Return(new Rect2((float)_sum[0], (float)_sum[1], (float)_sum[2], (float)_sum[3]));
        if (typeof(T) == typeof(Rect2i)) return Return(new Rect2i(Round(_sum[0]), Round(_sum[1]), Round(_sum[2]), Round(_sum[3])));
        throw new NotSupportedException();
    }
    private static void Read(T value, Span<double> output)
    {
        output.Clear();
        if (typeof(T) == typeof(float)) output[0] = Unsafe.As<T, float>(ref value);
        else if (typeof(T) == typeof(double)) output[0] = Unsafe.As<T, double>(ref value);
        else if (typeof(T) == typeof(int)) output[0] = Unsafe.As<T, int>(ref value);
        else if (typeof(T) == typeof(bool)) output[0] = Unsafe.As<T, bool>(ref value) ? 1 : 0;
        else if (typeof(T) == typeof(Vector2)) { var v = Unsafe.As<T, Vector2>(ref value); output[0] = v.X; output[1] = v.Y; }
        else if (typeof(T) == typeof(Vector2i)) { var v = Unsafe.As<T, Vector2i>(ref value); output[0] = v.X; output[1] = v.Y; }
        else if (typeof(T) == typeof(Vector3)) { var v = Unsafe.As<T, Vector3>(ref value); output[0] = v.X; output[1] = v.Y; output[2] = v.Z; }
        else if (typeof(T) == typeof(Vector3i)) { var v = Unsafe.As<T, Vector3i>(ref value); output[0] = v.X; output[1] = v.Y; output[2] = v.Z; }
        else if (typeof(T) == typeof(Vector4)) { var v = Unsafe.As<T, Vector4>(ref value); output[0] = v.X; output[1] = v.Y; output[2] = v.Z; output[3] = v.W; }
        else if (typeof(T) == typeof(Vector4i)) { var v = Unsafe.As<T, Vector4i>(ref value); output[0] = v.X; output[1] = v.Y; output[2] = v.Z; output[3] = v.W; }
        else if (typeof(T) == typeof(Color)) { var v = Unsafe.As<T, Color>(ref value); output[0] = v.R; output[1] = v.G; output[2] = v.B; output[3] = v.A; }
        else if (typeof(T) == typeof(Rect2)) { var v = Unsafe.As<T, Rect2>(ref value); output[0] = v.Position.X; output[1] = v.Position.Y; output[2] = v.Size.X; output[3] = v.Size.Y; }
        else if (typeof(T) == typeof(Rect2i)) { var v = Unsafe.As<T, Rect2i>(ref value); output[0] = v.Position.X; output[1] = v.Position.Y; output[2] = v.Size.X; output[3] = v.Size.Y; }
    }
    private static T Return<TValue>(TValue value) => Unsafe.As<TValue, T>(ref value);
    private static int Round(double value) => checked((int)Math.Round(value, MidpointRounding.AwayFromZero));
}
internal sealed class AnimationAlgebraBlend<T>(Func<T, T, double, T> interpolate) : AnimationBlendValue<T>
{
    private T _rest = default!, _sum = default!;
    internal override T Zero { get { if (typeof(T) == typeof(Transform)) { var identity = Transform.Identity; return Unsafe.As<Transform, T>(ref identity); } return default!; } }
    internal override void Begin(T rest) { _rest = rest; _sum = rest; }
    internal override void Add(T value, double weight, bool angle)
    {
        if (typeof(T) == typeof(Transform)) _sum = TweenValue<T>.Add!(_sum, interpolate(Zero, TweenValue<T>.Subtract!(value, _rest), weight));
        else _sum = interpolate(interpolate(_rest, _sum, 2), interpolate(_rest, value, 2 * weight), .5);
    }
    internal override T Finish(double totalWeight = 1) => _sum;
}
internal sealed class AnimationArrayBlend<TElement> : AnimationBlendValue<TElement[]>
{
    private readonly List<AnimationBlendValue<TElement>> _elements = [];
    private TElement[] _rest = [];
    private int _size;
    internal override TElement[] Zero => [];
    internal override void Begin(TElement[] rest) { _rest = rest ?? []; _size = 0; Prepare(_rest.Length); for (var i = 0; i < _elements.Count; i++) _elements[i].Begin(i < _rest.Length ? _rest[i] : _elements[i].Zero); }
    private void Prepare(int size) { while (_elements.Count < size) { var element = AnimationBlendValue<TElement>.Create()!; element.Begin(_elements.Count < _rest.Length ? _rest[_elements.Count] : element.Zero); _elements.Add(element); } }
    internal override void Add(TElement[] value, double weight, bool angle) { value ??= []; Prepare(Math.Max(value.Length, _rest.Length)); _size = Math.Max(_size, value.Length); for (var i = 0; i < _elements.Count; i++) _elements[i].Add(i < value.Length ? value[i] : _elements[i].Zero, weight, false); }
    internal override TElement[] Finish(double totalWeight = 1) { var result = new TElement[Math.Abs(totalWeight) < 1 ? Math.Max(_size, _rest.Length) : _size]; for (var i = 0; i < result.Length; i++) result[i] = _elements[i].Finish(totalWeight); return result; }
}
internal sealed class AnimationStringBlend : AnimationBlendValue<string>
{
    private double[] _rest = [], _sum = [];
    private char[] _text = [];
    private int _restLength, _activeMax; private double _length;
    internal override string Zero => "";
    private void Prepare(int count) { if (_sum.Length >= count) return; Array.Resize(ref _sum, count); Array.Resize(ref _rest, count); }
    internal override void Begin(string rest)
    { rest ??= ""; var count = 0; foreach (var rune in rest.EnumerateRunes()) count++; Prepare(count); _rest.AsSpan().Clear(); var i = 0; foreach (var rune in rest.EnumerateRunes()) _rest[i++] = rune.Value; _rest.CopyTo(_sum, 0); _restLength = count; _activeMax = count; _length = count; }
    internal override void Add(string value, double weight, bool angle)
    { value ??= ""; var count = 0; foreach (var rune in value.EnumerateRunes()) count++; Prepare(count); _activeMax = Math.Max(_activeMax, count); var i = 0; foreach (var rune in value.EnumerateRunes()) { _sum[i] += (rune.Value - _rest[i]) * weight; i++; } for (; i < _sum.Length; i++) _sum[i] -= _rest[i] * weight; _length += (count - _restLength) * weight; }
    internal override string Finish(double totalWeight = 1)
    {
        var count = checked((int)Math.Round(Math.Abs(_length), MidpointRounding.AwayFromZero)); if (count < _activeMax && Math.Abs(totalWeight) < 1) count = checked((int)Math.Round(_restLength + (count - _restLength) * Math.Abs(totalWeight), MidpointRounding.AwayFromZero)); if (count == 0) return "";
        if (_text.Length < checked(count * 2)) Array.Resize(ref _text, checked(count * 2)); var written = 0; for (var i = 0; i < count; i++) { var scalar = i < _sum.Length ? checked((int)Math.Round(_sum[i], MidpointRounding.AwayFromZero)) : 0; var rune = Rune.TryCreate(scalar, out var valid) ? valid : Rune.ReplacementChar; written += rune.EncodeToUtf16(_text.AsSpan(written)); }
        return new string(_text, 0, written);
    }
}
