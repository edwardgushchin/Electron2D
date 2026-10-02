namespace Electron2D;

internal readonly record struct CanvasNinePatch(Vector2 Begin, Vector2 End, AxisStretchMode Horizontal,
    AxisStretchMode Vertical, bool DrawCenter)
{
    internal void Append(List<CanvasVertex> output, CanvasCommand command, Transform transform, Color modulation, bool snap)
    {
        var texture = command.Texture!;
        var destination = new Rect2(command.A, command.B.Abs());
        if (command.B.X < 0 || command.B.Y < 0)
        {
            var reflectX = command.B.X < 0 ? -1 : 1; var reflectY = command.B.Y < 0 ? -1 : 1;
            transform *= new Transform(new Vector2(reflectX, 0), new Vector2(0, reflectY), new Vector2(reflectX < 0 ? 2 * command.A.X + destination.Size.X : 0, reflectY < 0 ? 2 * command.A.Y + destination.Size.Y : 0));
        }
        var source = command.Source;
        while (texture is AtlasTexture atlas)
        {
            var underlying = atlas.ResolveDrawRegion(ref destination, ref source);
            if (underlying is null) return;
            texture = underlying;
        }
        var textureSize = texture.GetSize();
        if (source.Size == Vector2.Zero) source.Size = textureSize;
        if (textureSize.X <= 0 || textureSize.Y <= 0 || source.Size.X == 0 || source.Size.Y == 0 || destination.Size.X <= 0 || destination.Size.Y <= 0) return;
        if (!textureSize.IsFinite() || !source.IsFinite() || !destination.IsFinite()) throw new InvalidOperationException("Nine-patch geometry must remain finite.");
        command = command with { A = destination.Position, B = destination.Size, Texture = texture };
        var x = new Axis(command.B.X, MathF.Abs(source.Size.X), Begin.X, End.X, Horizontal);
        var y = new Axis(command.B.Y, MathF.Abs(source.Size.Y), Begin.Y, End.Y, Vertical);
        // ponytail: CPU tessellation grows with tile count; use a common shader path if dense tiled panels become a measured bottleneck.
        if ((long)x.Count * y.Count > 1_048_576) throw new InvalidOperationException("Nine-patch tiling exceeds the prepared canvas geometry range.");
        for (var row = 0; row < y.Count; row++)
        {
            var py = y.Piece(row);
            for (var column = 0; column < x.Count; column++)
            {
                var px = x.Piece(column);
                if (!DrawCenter && px.Center && py.Center || px.To <= px.From || py.To <= py.From) continue;
                var partDestination = new Rect2(command.A + new Vector2(px.From, py.From), new(px.To - px.From, py.To - py.From));
                var sx = source.Size.X < 0 ? MathF.Abs(source.Size.X) - px.SourceTo : px.SourceFrom;
                var sy = source.Size.Y < 0 ? MathF.Abs(source.Size.Y) - py.SourceTo : py.SourceFrom;
                var region = new Rect2(source.Position + new Vector2(sx, sy), new(px.SourceTo - px.SourceFrom, py.SourceTo - py.SourceFrom));
                var uvBegin = region.Position / textureSize;
                var uvEnd = region.End / textureSize;
                // Keep a tiled boundary on the inside texel when raster interpolation rounds just below its exact coordinate.
                uvBegin += new Vector2(0.00001f / textureSize.X, 0.00001f / textureSize.Y);
                var part = command with { A = partDestination.Position, B = partDestination.Size, Source = new(uvBegin, (uvEnd - uvBegin) * new Vector2(source.Size.X < 0 ? -1 : 1, source.Size.Y < 0 ? -1 : 1)), NinePatch = null, ConstantSource = true };
                CanvasGeometry.Append(output, part, transform, modulation, snap);
            }
        }
    }

    private readonly record struct Segment(float From, float To, float SourceFrom, float SourceTo, bool Center);

    private readonly struct Axis
    {
        private readonly float _draw, _texture, _begin, _end, _start, _stop, _sourceCenter, _period;
        private readonly AxisStretchMode _mode;
        private readonly int _tiles;
        internal int Count => _tiles + 2;
        internal Axis(float draw, float texture, float begin, float end, AxisStretchMode mode)
        {
            _draw = draw; _texture = texture; _begin = begin; _end = end; _mode = mode;
            _start = Math.Clamp(begin, 0, draw); _stop = Math.Clamp(draw - end, _start, draw);
            _sourceCenter = texture - begin - end;
            if (_stop <= _start || _sourceCenter == 0 && mode != AxisStretchMode.Stretch) { _tiles = 0; _period = 0; return; }
            if (mode == AxisStretchMode.Stretch) { _tiles = 1; _period = _stop - _start; return; }
            var centerDraw = draw - begin - end;
            var count = mode == AxisStretchMode.TileFit
                ? MathF.Max(1, MathF.Floor(centerDraw / MathF.Max(_sourceCenter, 0.0000001f) + 0.5f))
                : MathF.Ceiling((_stop - _start + (_start - begin) % MathF.Abs(_sourceCenter)) / MathF.Abs(_sourceCenter));
            if (!float.IsFinite(count) || count > 1_048_574) throw new InvalidOperationException("Nine-patch axis exceeds the finite canvas tile range.");
            _tiles = (int)count;
            _period = mode == AxisStretchMode.TileFit ? centerDraw / count : MathF.Abs(_sourceCenter);
        }
        internal Segment Piece(int index)
        {
            if (index == 0) return new(0, _start, 0, _start, false);
            if (index == Count - 1) return new(_stop, _draw, _texture - (_draw - _stop), _texture, false);
            if (_mode == AxisStretchMode.Stretch)
            {
                var scale = _sourceCenter / (_draw - _begin - _end);
                return new(_start, _stop, _begin + (_start - _begin) * scale, _begin + (_stop - _begin) * scale, true);
            }
            var first = _start - Mathf.PosMod(_start - _begin, _period);
            var from = MathF.Max(_start, first + (index - 1) * _period);
            var to = MathF.Min(_stop, first + index * _period);
            var ratio = MathF.Abs(_sourceCenter) / _period;
            var sourceStart = _sourceCenter > 0 ? _begin : _begin + _sourceCenter;
            return new(from, to, sourceStart + (from - (first + (index - 1) * _period)) * ratio,
                sourceStart + (to - (first + (index - 1) * _period)) * ratio, true);
        }
    }
}
