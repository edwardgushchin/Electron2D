using System.Numerics;

namespace Electron2D.Binding.Box2D.Common
{
    public interface IDrawer
    {
        DrawFlag Flags { get; set; }

        /// <summary>
        /// Draw a closed polygon provided in CCW order.
        /// </summary>
        void DrawPolygon(Vector2[] vertices, int vertexCount, in Color color);

        /// <summary>
        /// Draw a solid closed polygon provided in CCW order.
        /// </summary>
        void DrawSolidPolygon(Vector2[] vertices, int vertexCount, in Color color);

        /// <summary>
        /// Draw a circle.
        /// </summary>
        void DrawCircle(in Vector2 center, float radius, in Color color);

        /// <summary>
        /// Draw a solid circle.
        /// </summary>
        void DrawSolidCircle(in Vector2 center, float radius, in Vector2 axis, in Color color);

        /// <summary>
        /// Draw a line segment.
        /// </summary>
        void DrawSegment(in Vector2 p1, in Vector2 p2, in Color color);

        /// <summary>
        /// Draw a transform. Choose your own length scale.
        /// @param xf a transform.
        /// </summary>
        void DrawTransform(in Transform xf);

        /// <summary>
        /// Draw a point.
        /// </summary>
        void DrawPoint(in Vector2 p, float size, in Color color);
    }
}