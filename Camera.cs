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
        private int _size;
        public Camera()
        {
            _size = 5;
            Transform = new Transform();
            UpdateUnit(null, null);
        }

        public int Size
        {
            get => _size;
            set
            {
                _size = value;
                UpdateUnit(null, null);
            }
        }

        internal double WorldUnit { get; private set; }

        public Transform Transform { get; set; }

        public Point ConvertScreenToWorld(Point screenPoint)
        {
            var w = Settings.Resolution.Width;
            var h = Settings.Resolution.Height;
            var u = screenPoint.X / w;
            var v = (h - screenPoint.Y) / h;

            var ratio = w / h;
            var extents = new Point(ratio * _size, _size);

            return new Point(
                ((1 - u) * -extents.X) + (u * extents.X),
                ((1 - v) * -extents.Y) + (v * extents.Y)
            );
        }

        internal void UpdateUnit(object sender, Events.WindowEventArgs e)
        {
            WorldUnit = ConvertWorldToScreen(new Point(1, 0)).X - ConvertWorldToScreen(new Point()).X;
        }

        public Point ConvertWorldToScreen(Point worldPoint)
        {
            var w = Settings.Resolution.Width;
            var h = Settings.Resolution.Height;
            var ratio = w / h;
            var extents = new Point(ratio * _size, _size);

            var u = (worldPoint.X + extents.X) / (extents.X + extents.X);
            var v = (worldPoint.Y + extents.Y) / (extents.Y + extents.Y);

            return new Point(u * w, (1 - v) * h);
        }
    }
}