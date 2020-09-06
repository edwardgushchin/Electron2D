/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using System;

using Electron2D.Binding.SDL;

namespace Electron2D.Graphics
{
    public struct Bounds
    {
        internal SDL.SDL_Rect SDLRect;
		private float _x, _y, _w, _h;

        public Bounds(float x, float y, float width, float height)
        {
            _x = x;
            _y = y;
            _w = width;
            _h = height;
			SDLRect = new SDL.SDL_Rect() { x = (int)_x, y = (int)_y, w = (int)_w, h = (int)_h};
        }

        public float X
        {
            get => _x;
            set
            {
                SDLRect.x = (int)value;
				_x = value;
            }
        }

        public float Y
		{
			get => _y;
            set
            {
                SDLRect.y = (int)value;
				_y = value;
            }
		}

        public float Width
		{
			get => _w;
            set
            {
                SDLRect.w = (int)value;
				_w = value;
            }
		}

        public float Height
		{
			get => _h;
            set
            {
               	SDLRect.h = (int)value;
				_h = value;
            }
		}

        public override string ToString()
		{
			return string.Format("({0}:{1}; {2}x{3})", X, Y, Width, Height);
		}
    }
}