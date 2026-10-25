namespace Electron2D;

public partial class Line
{
    // Adapted algorithm attribution is retained in licence/LineBuilder-LICENSE.txt.
    private void BuildMesh(Texture? texture)
    {
        _mesh.Clear(); _lineIndices.Clear(); _effectivePoints.Clear();
        foreach (var point in _points) if (_effectivePoints.Count == 0 || point != _effectivePoints[^1]) _effectivePoints.Add(point);
        if (_closed && _effectivePoints.Count > 2 && _effectivePoints[0] == _effectivePoints[^1]) _effectivePoints.RemoveAt(_effectivePoints.Count - 1);
        var count = _effectivePoints.Count; if (count < 2) return;
        var wrap = _closed && count > 2; var half = _width / 2; var aspect = 1f;
        if (texture is not null)
        {
            var height = texture.GetHeight(); var width = texture.GetWidth(); if (height <= 0 || width <= 0) return;
            aspect = width / (float)height;
        }
        var distanceRequired = _gradient is not null || _widthCurve is not null || _textureMode != LineTextureMode.None;
        var pos0 = _effectivePoints[0]; var pos1 = _effectivePoints[1]; var forward0 = LineDirection(pos0, pos1); var normal0 = forward0.Orthogonal();
        var factor = WidthFactor(0); var radius = half * factor; var total = 0f;
        if (distanceRequired)
        {
            for (var i = 1; i < count; i++) total += LineDistance(_effectivePoints[i - 1], _effectivePoints[i]);
            if (wrap) total += LineDistance(_effectivePoints[^1], pos0);
            else
            {
                if (_beginCapMode != LineCapMode.None) total += radius;
                if (_endCapMode != LineCapMode.None) total += half * WidthFactor(1);
            }
            if (!float.IsFinite(total)) throw new InvalidOperationException("Line length exceeds finite drawing coordinates.");
            if (Mathf.IsZeroApprox(total)) return;
        }
        var color = EndpointColor(false); var up0 = pos0 + normal0 * radius; var down0 = pos0 - normal0 * radius;
        var distance = 0f; var u0 = 0f;
        if (!wrap)
        {
            if (_beginCapMode == LineCapMode.Box) { up0 -= forward0 * radius; down0 -= forward0 * radius; distance += radius; }
            else if (_beginCapMode == LineCapMode.Round)
            {
                u0 = _textureMode == LineTextureMode.Tile ? factor * .5f / aspect : _textureMode == LineTextureMode.Stretch ? _width * factor / total : 0;
                LineCap(pos0, normal0 * radius, -Mathf.Pi, color, new(0, 0, u0 * 2, 1)); distance += radius;
            }
            StripBegin(up0, down0, color, u0);
        }
        var firstUp = Vector2.Zero; var firstDown = Vector2.Zero; var firstSharp = false;
        var segments = wrap ? count : count - 2;
        for (var i = wrap ? -1 : 1; i <= segments; i++)
        {
            pos1 = _effectivePoints[i == -1 ? count - 1 : i % count]; var pos2 = _effectivePoints[(i + 1) % count];
            var forward1 = LineDirection(pos1, pos2); var normal1 = forward1.Orthogonal(); var up = normal0.Dot(forward1) > 0;
            if (distanceRequired && i >= 1) distance += LineDistance(pos0, pos1);
            color = ColorAt(distanceRequired ? distance / total : 0); factor = WidthFactor(distanceRequired ? distance / total : 0); radius = half * factor;
            var inner0 = normal0 * (up ? radius : -radius); var inner1 = normal1 * (up ? radius : -radius);
            var intersection = Geometry.SegmentIntersectsSegment(pos0 + inner0, pos1 + inner0, pos1 + inner1, pos2 + inner1);
            var inside = intersection ?? pos1 + inner0; var outside = intersection.HasValue ? 2 * pos1 - inside : pos1 - inner0;
            var cornerUp = up ? inside : outside; var cornerDown = up ? outside : inside; var joint = _jointMode;
            if (intersection.HasValue && joint == LineJointMode.Sharp &&
                (double)(outside - pos1).LengthSquared() > (double)radius * radius * _sharpLimit * _sharpLimit) joint = LineJointMode.Bevel;
            if (!intersection.HasValue && joint == LineJointMode.Sharp) joint = LineJointMode.Bevel;
            Vector2 up1, down1;
            if (intersection.HasValue && joint != LineJointMode.Sharp)
            { up1 = up ? cornerUp : pos1 + normal0 * radius; down1 = up ? pos1 - normal0 * radius : cornerDown; }
            else { up1 = cornerUp; down1 = cornerDown; }
            normal0 = normal1; forward0 = forward1; pos0 = pos1;
            if (intersection.HasValue && joint == LineJointMode.Sharp) { up0 = up1; down0 = down1; }
            else if (intersection.HasValue) { up0 = up ? cornerUp : pos1 + normal1 * radius; down0 = up ? pos1 - normal1 * radius : cornerDown; }
            else { up0 = pos1 + normal1 * radius; down0 = pos1 - normal1 * radius; }
            if (i == -1) continue;
            var u1 = LongitudinalUV(distance, total, aspect);
            if (wrap && i == 0)
            {
                var center = (up1 + down1) / 2;
                firstUp = factor == 0 ? center : center + (up1 - center) / factor;
                firstDown = factor == 0 ? center : center + (down1 - center) / factor;
                firstSharp = joint == LineJointMode.Sharp;
            }
            if (wrap && _widthCurve is not null && !firstSharp && i == segments)
            { var center = (firstUp + firstDown) / 2; StripQuad(center + (firstUp - center) * factor, center + (firstDown - center) * factor, color, u1); return; }
            if (_mesh.Count == 0) StripBegin(up1, down1, color, u1);
            else StripQuad(up1, down1, color, u1);
            if (joint != LineJointMode.Sharp)
            {
                var begin = up ? down1 : up1; var end = up ? down0 : up0;
                if (!(wrap && i == segments))
                {
                    if (joint == LineJointMode.Bevel) StripTriangle(end, up);
                    else
                    {
                        var a = begin - pos1; var b = end - pos1; var cross = a.Cross(b); if (cross == 0) cross = 0;
                        StripArc(pos1, MathF.Atan2(cross, a.Dot(b)), up);
                    }
                }
                if (!intersection.HasValue) StripBegin(up0, down0, color, u1);
            }
        }
        if (wrap) return;
        pos1 = _effectivePoints[^1]; if (distanceRequired) distance += LineDistance(pos0, pos1);
        color = EndpointColor(true); factor = WidthFactor(1); radius = half * factor;
        var endUp = pos1 + normal0 * radius; var endDown = pos1 - normal0 * radius;
        if (_endCapMode == LineCapMode.Box) { endUp += forward0 * radius; endDown += forward0 * radius; distance += radius; }
        var endU = LongitudinalUV(distance, total, aspect); StripQuad(endUp, endDown, color, endU);
        if (_endCapMode == LineCapMode.Round)
        {
            var extent = _textureMode == LineTextureMode.Tile ? factor / aspect : _textureMode == LineTextureMode.Stretch ? _width * factor / total : 0;
            LineCap(pos1, endUp - pos1, Mathf.Pi, color, new(endU - extent * .5f, 0, extent, 1));
        }
    }

    private float WidthFactor(float t)
    { var value = _widthCurve?.SampleBaked(t) ?? 1; if (!float.IsFinite(value)) throw new InvalidOperationException("Line width curve produced a nonfinite width."); return value; }
    private Color ColorAt(float t) => _gradient?.Sample(t) ?? _defaultColor;
    private Color EndpointColor(bool end) => _gradient is { } gradient && gradient.GetPointCount() is var count && count > 0 ? gradient.GetColor(end ? count - 1 : 0) : ColorAt(end ? 1 : 0);
    private float LongitudinalUV(float distance, float total, float aspect) => _textureMode switch
    { LineTextureMode.Tile => distance / (_width * aspect), LineTextureMode.Stretch => distance / total, _ => 0 };
    private static float LineDistance(Vector2 a, Vector2 b)
    { var x = (double)b.X - a.X; var y = (double)b.Y - a.Y; return (float)Math.Sqrt(x * x + y * y); }
    private static Vector2 LineDirection(Vector2 a, Vector2 b)
    { var x = (double)b.X - a.X; var y = (double)b.Y - a.Y; var length = Math.Sqrt(x * x + y * y); return length == 0 ? Vector2.Zero : new((float)(x / length), (float)(y / length)); }
    private int LineVertex(Vector2 position, Color color, Vector2 uv)
    { var index = _mesh.Count; _mesh.Add(new(position, color, _textureMode == LineTextureMode.None ? Vector2.Zero : uv)); return index; }
    private void LineTriangle(int a, int b, int c)
    {
        var pa = _mesh[a].Position; var pb = _mesh[b].Position; var pc = _mesh[c].Position;
        var area = ((double)pb.X - pa.X) * ((double)pc.Y - pa.Y) - ((double)pb.Y - pa.Y) * ((double)pc.X - pa.X);
        if (area == 0) return;
        _lineIndices.Add(a); _lineIndices.Add(area > 0 ? b : c); _lineIndices.Add(area > 0 ? c : b);
    }
    private void StripBegin(Vector2 up, Vector2 down, Color color, float u)
    { _stripUp = LineVertex(up, color, new(u, 0)); _stripDown = LineVertex(down, color, new(u, 1)); }
    private void StripQuad(Vector2 up, Vector2 down, Color color, float u)
    {
        var nextUp = LineVertex(up, color, new(u, 0)); var nextDown = LineVertex(down, color, new(u, 1));
        LineTriangle(_stripUp, nextDown, _stripDown); LineTriangle(_stripUp, nextUp, nextDown); _stripUp = nextUp; _stripDown = nextDown;
    }
    private void StripTriangle(Vector2 point, bool up)
    {
        var opposite = up ? _stripDown : _stripUp; var same = up ? _stripUp : _stripDown;
        var next = LineVertex(point, _mesh[^1].Color, _mesh[opposite].UV); LineTriangle(opposite, next, same);
        if (up) _stripDown = next; else _stripUp = next;
    }
    private void StripArc(Vector2 center, float angle, bool up)
    {
        var begin = _mesh[up ? _stripDown : _stripUp].Position - center; var radius = begin.Length(); if (radius == 0) return;
        var start = MathF.Atan2(begin.Y, begin.X); var step = MathF.CopySign(Mathf.Pi / _roundPrecision, angle);
        var count = checked((int)Math.Ceiling(Math.Abs((double)angle) / Math.Abs(step))); _mesh.EnsureCapacity(checked(_mesh.Count + count));
        for (var i = 1; i <= count; i++) { var a = start + (i == count ? angle : step * i); StripTriangle(center + new Vector2(MathF.Cos(a), MathF.Sin(a)) * radius, up); }
    }
    private void LineCap(Vector2 center, Vector2 begin, float angle, Color color, Rect2 uv)
    {
        var radius = begin.Length(); if (radius == 0) return;
        var start = MathF.Atan2(begin.Y, begin.X); var step = MathF.CopySign(Mathf.Pi / _roundPrecision, angle);
        var count = checked((int)Math.Ceiling(Math.Abs((double)angle) / Math.Abs(step))); _mesh.EnsureCapacity(checked(_mesh.Count + count + 1));
        var first = -1; var previous = -1;
        for (var i = 0; i <= count; i++)
        {
            var delta = i == count ? angle : i * step; var a = start + delta; var textureAngle = -Mathf.Pi / 2 + delta;
            var position = i == 0 ? center + begin : i == count ? center - begin : center + new Vector2(MathF.Cos(a), MathF.Sin(a)) * radius;
            var mapped = uv.Position + (new Vector2(MathF.Cos(textureAngle), MathF.Sin(textureAngle)) + Vector2.One) * .5f * uv.Size;
            var next = LineVertex(position, color, mapped);
            if (first < 0) first = next; else if (i > 1) LineTriangle(first, previous, next); previous = next;
        }
    }
}
