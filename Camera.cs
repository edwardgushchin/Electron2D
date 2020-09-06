/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using Electron2D.Kernel;
using Electron2D.Graphics;

namespace Electron2D
{
    public class Camera
    {
        private float _size;
        public Camera()
        {
            _size = 5;
            Transform = new Transform();
            UpdateUnit(null, null);
        }

        public float Size
        {
            get => _size;
            set
            {
                _size = value;
                UpdateUnit(null, null);
            }
        }

        internal float WorldUnit { get; private set; }

        public Transform Transform { get; set; }

        public Bounds Bounds
        {
            get
            {
                float ratio = (float)Settings.Resolution.Width / (float)Settings.Resolution.Height;
                var extents = new Point(ratio * _size, _size);
                var x = Transform.Position.X - extents.X;
                var y = Transform.Position.Y + extents.Y;
                var w = extents.X * 2f;
                var h = extents.Y * 2f;
                return new Bounds(x, y, w, h);
            }
        }

        internal void UpdateUnit(object sender, Events.WindowEventArgs e)
        {
            WorldUnit = ConvertWorldToScreen(new Point(1, 0)).X - ConvertWorldToScreen(new Point()).X;
        }

        public Point ConvertScreenToWorld(Point screenPoint)
        {
            var w = (float)Settings.Resolution.Width;
            var h = (float)Settings.Resolution.Height;
            var u = screenPoint.X / w;
            var v = (h - screenPoint.Y) / h;

            var ratio = (float)(w / h);
            var extents = new Point(ratio * _size, _size);

            return new Point(
                ((1f - u) * -extents.X) + (u * extents.X),
                ((1f - v) * -extents.Y) + (v * extents.Y)
            );
        }

        public Point ConvertWorldToScreen(Point worldPoint)
        {
            var w = (float)Settings.Resolution.Width;
            var h = (float)Settings.Resolution.Height;
            var ratio = w / h;
            var extents = new Point(ratio * _size, _size);

            var u = (worldPoint.X + extents.X) / (extents.X + extents.X);
            var v = (worldPoint.Y + extents.Y) / (extents.Y + extents.Y);

            return new Point(u * w, (1f - v) * h);
        }

        public static Camera MainCamera => SceneManager.GetCurrentScene.Camera;
    }
}