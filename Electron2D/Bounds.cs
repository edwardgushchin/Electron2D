namespace Electron2D
{
    /// <summary>
    /// Описывает прямоугольную коробку (Axis-Aligned Bounding Box) в мировом пространстве.
    /// </summary>
    public struct Bounds
    {
        public Vector2 Center { get; }
        public Vector2 Size { get; }
        public Vector2 Extents { get; }
        public Vector2 Min { get; }
        public Vector2 Max { get; }

        public Bounds(Vector2 center, Vector2 size)
        {
            Center = center;
            Size = size;
            Extents = size * 0.5f;
            Min = center - Extents;
            Max = center + Extents;
        }

        /// <summary>
        /// Проверяет, находится ли точка внутри границ.
        /// </summary>
        public bool Contains(Vector2 point)
        {
            return point.X >= Min.X && point.X <= Max.X
                                    && point.Y >= Min.Y && point.Y <= Max.Y;
        }
    }
}