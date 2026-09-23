using System.Numerics;

namespace Electron2D;

/// <summary>Stores a compact two-dimensional mask and extracts its opaque contours.</summary>
/// <remarks>Coordinates are zero based. Mutations are serialized per instance and raise <see cref="Resource.Changed"/> after commitment.</remarks>
public class BitMap : Resource
{
    private readonly object _gate = new();
    private byte[] _bits = [];
    private int _width;
    private int _height;

    /// <summary>Creates an empty, zero-sized mask.</summary>
    public BitMap() { }

    /// <summary>Returns the mask dimensions.</summary>
    /// <returns>The width and height in pixels.</returns>
    public Vector2I GetSize() { lock (_gate) { ThrowIfDisposed(); return new(_width, _height); } }

    /// <summary>Replaces the mask with unset bits of the requested positive size.</summary>
    /// <param name="size">The new dimensions.</param>
    /// <exception cref="ArgumentOutOfRangeException">A dimension is nonpositive or the bit count exceeds the supported range.</exception>
    public void Create(Vector2I size)
    {
        var data = Allocate(size);
        lock (_gate) { ThrowIfDisposed(); _width = size.X; _height = size.Y; _bits = data; }
        EmitChanged();
    }

    /// <summary>Sets bits where the image alpha strictly exceeds the threshold.</summary>
    /// <param name="image">A nonempty uncompressed image.</param>
    /// <param name="threshold">Alpha threshold in normalized units; equality is transparent.</param>
    /// <exception cref="ArgumentNullException"><paramref name="image"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The image is empty or cannot be converted to uncompressed pixels.</exception>
    public void CreateFromImageAlpha(Image image, float threshold = 0.1f)
    {
        ArgumentNullException.ThrowIfNull(image);
        using var copy = (Image)image.Duplicate();
        if (copy.IsEmpty) throw new InvalidOperationException("An empty image has no alpha mask.");
        copy.Convert(Image.Format.La8);
        var size = copy.Size;
        var alpha = copy.GetData();
        var data = Allocate(size);
        for (var index = 0; index < (long)size.X * size.Y; index++)
            if (alpha[checked((int)(index * 2 + 1))] / 255f > threshold)
                Set(data, (int)index, true);
        lock (_gate) { ThrowIfDisposed(); _width = size.X; _height = size.Y; _bits = data; }
        EmitChanged();
    }

    /// <summary>Reads one bit.</summary>
    /// <param name="x">Horizontal pixel coordinate.</param>
    /// <param name="y">Vertical pixel coordinate.</param>
    /// <returns>Whether the bit is set.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A coordinate lies outside the mask.</exception>
    public bool GetBit(int x, int y)
    {
        lock (_gate) { ThrowIfDisposed(); return Get(_bits, Offset(x, y)); }
    }

    /// <summary>Reads one bit by integer point.</summary>
    /// <param name="position">The pixel coordinate.</param>
    /// <returns>Whether the bit is set.</returns>
    public bool GetBitv(Vector2I position) => GetBit(position.X, position.Y);

    /// <summary>Sets one bit.</summary>
    /// <param name="x">Horizontal pixel coordinate.</param>
    /// <param name="y">Vertical pixel coordinate.</param>
    /// <param name="bit">The new value.</param>
    /// <exception cref="ArgumentOutOfRangeException">A coordinate lies outside the mask.</exception>
    public void SetBit(int x, int y, bool bit)
    {
        bool changed;
        lock (_gate)
        {
            ThrowIfDisposed();
            var offset = Offset(x, y);
            changed = Get(_bits, offset) != bit;
            Set(_bits, offset, bit);
        }
        if (changed) EmitChanged();
    }

    /// <summary>Sets one bit by integer point.</summary>
    /// <param name="position">The pixel coordinate.</param>
    /// <param name="bit">The new value.</param>
    public void SetBitv(Vector2I position, bool bit) => SetBit(position.X, position.Y, bit);

    /// <summary>Sets all bits in the clipped half-open rectangle.</summary>
    /// <param name="rect">The requested region.</param>
    /// <param name="bit">The new value.</param>
    public void SetBitRect(RectI rect, bool bit)
    {
        bool changed = false;
        lock (_gate)
        {
            ThrowIfDisposed();
            var area = Clip(rect);
            for (var y = area.Top; y < area.Bottom; y++)
                for (var x = area.Left; x < area.Right; x++)
                {
                    var offset = y * _width + x;
                    changed |= Get(_bits, offset) != bit;
                    Set(_bits, offset, bit);
                }
        }
        if (changed) EmitChanged();
    }

    /// <summary>Counts set bits.</summary>
    /// <returns>The number of true pixels.</returns>
    public int GetTrueBitCount()
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            var count = 0;
            foreach (var value in _bits) count += BitOperations.PopCount((uint)value);
            return count;
        }
    }

    /// <summary>Resizes with nearest-neighbor sampling; a default empty mask becomes all false.</summary>
    /// <param name="newSize">Positive destination dimensions.</param>
    public void Resize(Vector2I newSize)
    {
        var data = Allocate(newSize);
        lock (_gate)
        {
            ThrowIfDisposed();
            if (newSize.X == _width && newSize.Y == _height) return;
            if (_width > 0 && _height > 0)
                for (var y = 0; y < newSize.Y; y++)
                    for (var x = 0; x < newSize.X; x++)
                    {
                        var sourceX = (int)((long)x * _width / newSize.X);
                        var sourceY = (int)((long)y * _height / newSize.Y);
                        if (Get(_bits, sourceY * _width + sourceX)) Set(data, y * newSize.X + x, true);
                    }
            _width = newSize.X;
            _height = newSize.Y;
            _bits = data;
        }
        EmitChanged();
    }

    /// <summary>Returns an L8 image with white true pixels and black false pixels.</summary>
    /// <returns>An independent image, or an empty image when the mask has not been created.</returns>
    public Image ConvertToImage()
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            if (_width == 0 || _height == 0) return new Image();
            var data = new byte[checked(_width * _height)];
            for (var i = 0; i < data.Length; i++) if (Get(_bits, i)) data[i] = 255;
            return Image.CreateFromData(_width, _height, false, Image.Format.L8, data);
        }
    }

    /// <summary>Dilates a mask for positive pixels or erodes it for negative pixels within a region.</summary>
    /// <param name="pixels">Signed Euclidean radius.</param>
    /// <param name="rect">Half-open affected region; pixels outside it stay unchanged and count as unset during erosion.</param>
    public void GrowMask(int pixels, RectI rect)
    {
        if (pixels == 0) { ThrowIfDisposed(); return; }
        bool changed = false;
        lock (_gate)
        {
            ThrowIfDisposed();
            var area = Clip(rect);
            if (area.Right <= area.Left || area.Bottom <= area.Top) return;
            var source = (byte[])_bits.Clone();
            var radius = Math.Min(Math.Abs((long)pixels), Math.Max(_width, _height));
            var squared = radius * radius;
            var grow = pixels > 0;
            for (var y = area.Top; y < area.Bottom; y++)
                for (var x = area.Left; x < area.Right; x++)
                {
                    var offset = y * _width + x;
                    if (Get(source, offset) == grow) continue;
                    var found = false;
                    for (var dy = -radius; dy <= radius && !found; dy++)
                    {
                        var yy = y + dy;
                        for (var dx = -radius; dx <= radius; dx++)
                        {
                            if (dx * dx + dy * dy > squared) continue;
                            var xx = x + dx;
                            if (xx < area.Left || xx >= area.Right || yy < area.Top || yy >= area.Bottom)
                            {
                                if (!grow) { found = true; break; }
                            }
                            else if (Get(source, (int)(yy * _width + xx)) == grow) { found = true; break; }
                        }
                    }
                    if (found) { Set(_bits, offset, grow); changed = true; }
                }
        }
        if (changed) EmitChanged();
    }

    /// <summary>Extracts polygons around connected opaque pixels and reduces long contours.</summary>
    /// <param name="rect">The half-open region to inspect.</param>
    /// <param name="epsilon">Nonnegative Ramer-Douglas-Peucker reduction tolerance.</param>
    /// <returns>Polygons with points relative to the clipped rectangle's origin.</returns>
    public Vector2[][] OpaqueToPolygons(RectI rect, float epsilon = 2f)
    {
        if (!float.IsFinite(epsilon) || epsilon < 0) throw new ArgumentOutOfRangeException(nameof(epsilon));
        lock (_gate)
        {
            ThrowIfDisposed();
            var area = Clip(rect);
            if (area.Right <= area.Left || area.Bottom <= area.Top) return [];
            var visited = new byte[_bits.Length];
            var polygons = new List<Vector2[]>();
            for (var y = area.Top; y < area.Bottom; y++)
                for (var x = area.Left; x < area.Right; x++)
                {
                    var offset = y * _width + x;
                    if (!Get(_bits, offset) || Get(visited, offset)) continue;
                    Flood(visited, area, x, y);
                    foreach (var contour in March(area, x, y))
                    {
                        var reduced = Reduce(contour, area, epsilon);
                        if (reduced.Length >= 3) polygons.Add(reduced);
                    }
                }
            return [.. polygons];
        }
    }

    private void Flood(byte[] visited, (int Left, int Top, int Right, int Bottom) area, int x, int y)
    {
        var pending = new Stack<int>();
        var start = y * _width + x;
        Set(visited, start, true);
        pending.Push(start);
        while (pending.TryPop(out var offset))
        {
            var px = offset % _width;
            var py = offset / _width;
            for (var yy = Math.Max(area.Top, py - 1); yy <= Math.Min(area.Bottom - 1, py + 1); yy++)
                for (var xx = Math.Max(area.Left, px - 1); xx <= Math.Min(area.Right - 1, px + 1); xx++)
                {
                    var next = yy * _width + xx;
                    if (Get(visited, next) || !Get(_bits, next)) continue;
                    Set(visited, next, true);
                    pending.Push(next);
                }
        }
    }

    private IEnumerable<Vector2[]> March((int Left, int Top, int Right, int Bottom) area, int startX, int startY)
    {
        var contours = new List<Vector2[]>();
        var points = new List<Vector2>();
        var crossings = new Dictionary<(int X, int Y), int>();
        var x = startX;
        var y = startY;
        var previousX = 0;
        var previousY = 0;
        var maxSteps = 2L * ((long)_width * _height + 1);
        for (long count = 0; count <= maxSteps; count++)
        {
            var value = 0;
            if (Inside(area, x - 1, y - 1) && Get(_bits, (y - 1) * _width + x - 1)) value |= 1;
            if (Inside(area, x, y - 1) && Get(_bits, (y - 1) * _width + x)) value |= 2;
            if (Inside(area, x - 1, y) && Get(_bits, y * _width + x - 1)) value |= 4;
            if (Inside(area, x, y) && Get(_bits, y * _width + x)) value |= 8;
            (int dx, int dy) = value switch
            {
                1 or 5 or 13 => (0, -1),
                8 or 10 or 11 => (0, 1),
                4 or 12 or 14 => (-1, 0),
                2 or 3 or 7 => (1, 0),
                9 => previousX == 1 ? (0, 1) : (0, -1),
                6 => previousY == -1 ? (1, 0) : (-1, 0),
                _ => throw new InvalidOperationException("The mask contour could not be traced.")
            };
            if (value is 6 or 9)
            {
                var position = (x, y);
                if (crossings.TryGetValue(position, out var index))
                {
                    contours.Add(points.Skip(index + 1).ToArray());
                    points.RemoveRange(index + 1, points.Count - index - 1);
                    foreach (var key in crossings.Where(pair => pair.Value > index).Select(pair => pair.Key).ToArray()) crossings.Remove(key);
                    crossings.Remove(position);
                }
                else crossings.Add(position, points.Count - 1);
            }
            x += dx;
            y += dy;
            var point = new Vector2(x - area.Left, y - area.Top);
            if (dx == previousX && dy == previousY) points[^1] = point;
            else points.Add(point);
            previousX = dx;
            previousY = dy;
            if (x == startX && y == startY) { contours.Insert(0, [.. points]); return contours; }
        }
        throw new InvalidOperationException("The mask contour exceeded its finite step bound.");
    }

    private static Vector2[] Reduce(Vector2[] points, (int Left, int Top, int Right, int Bottom) area, float epsilon)
    {
        if (points.Length < 9) return points;
        var tolerance = Math.Min(epsilon, Math.Min(area.Right - area.Left, area.Bottom - area.Top) / 2f);
        var kept = new bool[points.Length];
        kept[0] = kept[^1] = true;
        var stack = new Stack<(int First, int Last)>();
        stack.Push((0, points.Length - 1));
        while (stack.TryPop(out var segment))
        {
            double best = tolerance;
            var at = -1;
            var a = points[segment.First];
            var b = points[segment.Last];
            for (var i = segment.First + 1; i < segment.Last; i++)
            {
                var distance = PerpendicularDistance(points[i], a, b);
                if (distance > best) { best = distance; at = i; }
            }
            if (at < 0) continue;
            kept[at] = true;
            stack.Push((segment.First, at));
            stack.Push((at, segment.Last));
        }
        var result = new List<Vector2>();
        for (var i = 0; i < points.Length; i++) if (kept[i]) result.Add(points[i]);
        return [.. result];
    }

    private static double PerpendicularDistance(Vector2 point, Vector2 a, Vector2 b)
    {
        var dx = (double)b.X - a.X;
        var dy = (double)b.Y - a.Y;
        if (dx == 0) return Math.Abs(point.X - b.X);
        if (dy == 0) return Math.Abs(point.Y - b.Y);
        return Math.Abs(dy * point.X - dx * point.Y + b.X * a.Y - b.Y * a.X) / Math.Sqrt(dx * dx + dy * dy);
    }

    private static bool Inside((int Left, int Top, int Right, int Bottom) area, int x, int y) =>
        x >= area.Left && x < area.Right && y >= area.Top && y < area.Bottom;

    private (int Left, int Top, int Right, int Bottom) Clip(RectI rect)
    {
        var left = (int)Math.Clamp((long)rect.Position.X, 0, _width);
        var top = (int)Math.Clamp((long)rect.Position.Y, 0, _height);
        var right = (int)Math.Clamp((long)rect.Position.X + rect.Size.X, 0, _width);
        var bottom = (int)Math.Clamp((long)rect.Position.Y + rect.Size.Y, 0, _height);
        return (left, top, Math.Max(left, right), Math.Max(top, bottom));
    }

    private int Offset(int x, int y)
    {
        if ((uint)x >= (uint)_width) throw new ArgumentOutOfRangeException(nameof(x));
        if ((uint)y >= (uint)_height) throw new ArgumentOutOfRangeException(nameof(y));
        return y * _width + x;
    }

    private static byte[] Allocate(Vector2I size)
    {
        var pixels = (long)size.X * size.Y;
        if (size.X <= 0 || size.Y <= 0 || pixels > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(size));
        return new byte[(pixels + 7) / 8];
    }

    private static bool Get(byte[] data, int offset) => (data[offset >> 3] & (1 << (offset & 7))) != 0;

    private static void Set(byte[] data, int offset, bool value)
    {
        var mask = 1 << (offset & 7);
        if (value) data[offset >> 3] |= (byte)mask;
        else data[offset >> 3] &= (byte)~mask;
    }

    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance()
    {
        if (GetType() != typeof(BitMap))
            throw new NotSupportedException($"{GetType().Name} must override {nameof(CreateDuplicateInstance)} to support duplication.");
        return new BitMap();
    }

    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var bitmap = (BitMap)target;
        int width;
        int height;
        byte[] bits;
        lock (_gate)
        {
            ThrowIfDisposed();
            width = _width;
            height = _height;
            bits = (byte[])_bits.Clone();
        }
        lock (bitmap._gate) { bitmap._width = width; bitmap._height = height; bitmap._bits = bits; }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) lock (_gate) { _bits = []; _width = 0; _height = 0; }
        base.Dispose(disposing);
    }
}
