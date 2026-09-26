namespace Electron2D;

public partial class TextureProgressBar
{
    private void DrawNinePatch(Texture texture, double ratio, bool progress, Color color)
    {
        var textureSize = texture.GetSize(); var begin = new Vector2(_margins[0], _margins[1]); var end = new Vector2(_margins[2], _margins[3]);
        var source = new Rect2(Vector2.Zero, textureSize); var destination = new Rect2(Vector2.Zero, Size);
        if (ratio < 1 && _fillMode is not (TextureProgressFillMode.Clockwise or TextureProgressFillMode.CounterClockwise or TextureProgressFillMode.ClockwiseAndCounterClockwise))
        {
            var horizontal = _fillMode is TextureProgressFillMode.LeftToRight or TextureProgressFillMode.RightToLeft or TextureProgressFillMode.BilinearLeftAndRight;
            var reverse = _fillMode is TextureProgressFillMode.RightToLeft or TextureProgressFillMode.BottomToTop;
            var bilateral = _fillMode is TextureProgressFillMode.BilinearLeftAndRight or TextureProgressFillMode.BilinearTopAndBottom;
            var total = horizontal ? destination.Size.X : destination.Size.Y;
            var original = horizontal ? textureSize.X : textureSize.Y;
            var first = horizontal ? begin.X : begin.Y; var last = horizontal ? end.X : end.Y;
            if (reverse) (first, last) = (last, first);
            var filled = total * ratio; double middle = Math.Max(0, original - first - last);
            var maxMiddleTexture = middle; var maxMiddleReal = Math.Max(0, total - first - last);
            if (bilateral)
            {
                first = (float)Math.Max(0, first - (total - filled) / 2); last = (float)Math.Max(0, last - (total - filled) / 2);
                middle *= maxMiddleReal == 0 ? 0 : Math.Min(maxMiddleReal, filled - first - last) / maxMiddleReal;
            }
            else
            {
                middle *= Math.Min(1, Math.Max(0, filled - first) / Math.Max(1, total - first - last));
                last = (float)Math.Max(0, last - (total - filled)); first = (float)Math.Min(first, filled);
            }
            var sourceFilled = (float)Math.Min(original, first + middle + last);
            var sourceOffset = reverse ? original - sourceFilled : 0;
            if (bilateral)
            {
                var axisBegin = horizontal ? begin.X : begin.Y; var axisEnd = horizontal ? end.X : end.Y;
                var mappedCenter = maxMiddleReal == 0 ? original / 2 : (total / 2 - axisBegin) / maxMiddleReal * maxMiddleTexture + axisBegin;
                var drift = 0d;
                // Preserve the source's horizontal orthogonal-margin condition; reject singular drift below instead of emitting nonfinite geometry.
                if (end.Y != begin.Y && axisEnd != axisBegin)
                    drift = (original / 2 - mappedCenter) * (last - first) / (axisEnd - axisBegin);
                sourceOffset = (float)(mappedCenter + drift - sourceFilled / 2);
            }
            var destinationOffset = reverse ? total - filled : bilateral ? (total - filled) / 2 : 0;
            if (horizontal)
            {
                source = new(new(sourceOffset, source.Position.Y), new(sourceFilled, source.Size.Y));
                destination = new(new((float)destinationOffset, destination.Position.Y), new((float)filled, destination.Size.Y));
                begin.X = reverse ? last : first; end.X = reverse ? first : last;
            }
            else
            {
                source = new(new(source.Position.X, sourceOffset), new(source.Size.X, sourceFilled));
                destination = new(new(destination.Position.X, (float)destinationOffset), new(destination.Size.X, (float)filled));
                begin.Y = reverse ? last : first; end.Y = reverse ? first : last;
            }
        }
        if (ReferenceEquals(texture, _textures[1])) destination.Position += _textureProgressOffset;
        if (!source.IsFinite() || !destination.IsFinite()) throw new InvalidOperationException("Progress geometry exceeds the finite canvas range.");
        RecordNinePatch(texture, destination, source, new(begin, end, NinePatchRect.AxisStretchMode.Stretch, NinePatchRect.AxisStretchMode.Stretch, true), color);
    }

    private Vector2 RelativeCenter(Vector2 textureSize) => (new Vector2(.5f, .5f) + _radialCenterOffset / textureSize).Clamp(Vector2.Zero, Vector2.One);

    private Vector2 RadialUV(float value, Vector2 center)
    {
        if (value < 0) value += 1;
        if (value > 1) value -= 1;
        var angle = value * Mathf.Tau - Mathf.Pi * .5f;
        var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle)); var time = 1f;
        for (var edge = 0; edge < 4; edge++)
        {
            float distance, projection;
            if (edge == 0) { if (direction.X > 0) continue; distance = center.X; direction.X *= 2 * distance; projection = -direction.X; }
            else if (edge == 1) { if (direction.X < 0) continue; distance = 1 - center.X; direction.X *= 2 * distance; projection = direction.X; }
            else if (edge == 2) { if (direction.Y > 0) continue; distance = center.Y; direction.Y *= 2 * distance; projection = -direction.Y; }
            else { if (direction.Y < 0) continue; distance = 1 - center.Y; direction.Y *= 2 * distance; projection = direction.Y; }
            var ratio = distance / projection;
            if (ratio >= 0 && ratio < time) time = ratio;
        }
        return center + time * direction;
    }
    private void DrawRadial(Texture texture, double ratio)
    {
        var textureSize = texture.GetSize(); if (textureSize.X <= 0 || textureSize.Y <= 0) return;
        var size = _ninePatchStretch ? Size : textureSize; var value = (float)ratio * _radialFillDegrees / 360;
        if (value == 1) { DrawTextureRectRegion(texture, new(_textureProgressOffset, size), new(Vector2.Zero, textureSize), _tints[1]); return; }
        if (value == 0) return;
        var start = _radialInitialAngle / 360;
        if (_fillMode == TextureProgressFillMode.ClockwiseAndCounterClockwise) start -= value / 2;
        var finish = start + (_fillMode == TextureProgressFillMode.CounterClockwise ? -value : value);
        var from = MathF.Min(start, finish); var to = MathF.Max(start, finish);
        Span<float> angles = stackalloc float[8]; var angleCount = 0; angles[angleCount++] = from;
        for (var corner = MathF.Floor(from * 4 + .5f) * .25f + .125f; corner < to; corner += .25f) angles[angleCount++] = corner;
        angles[angleCount++] = to;
        var center = RelativeCenter(textureSize); Span<Vector2> points = stackalloc Vector2[9]; Span<Vector2> uvs = stackalloc Vector2[9]; var count = 0;
        for (var index = 0; index < angleCount; index++)
        {
            var uv = RadialUV(angles[index], center); var duplicate = false;
            for (var previous = 0; previous < count; previous++) if (uvs[previous] == uv) { duplicate = true; break; }
            if (duplicate) continue;
            points[count] = _textureProgressOffset + uv * size; uvs[count++] = uv;
        }
        if (count < 2) return;
        points[count] = _textureProgressOffset + center * size; uvs[count++] = center;
        DrawColoredPolygon(points[..count], _tints[1], uvs[..count], texture);
    }
}
