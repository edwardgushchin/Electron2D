/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using System;

using Electron2D.Kernel;
using Electron2D.Graphics;
using Electron2D.Binding.SDL;

using System.Runtime.InteropServices;

namespace Electron2D.Graphics
{
    public class Sprite
    {

        IntPtr sprite;
        SDL.SDL_Rect scr_rect;
        SDL.SDL_FRect draw_rect; 
        int width, height, access;
        uint format;
        public Sprite(string path)
        {
            Path = path;
            Transform = new Transform(new Point(0, 0));
            sprite = Image.IMG_LoadTexture(Game.RenderContext, path);
            SDL.SDL_QueryTexture(sprite, out format, out access, out width, out height); // get the width and height of the texture

            draw_rect = new SDL.SDL_FRect();
            draw_rect.w = width;
            draw_rect.h = height;
            scr_rect.x = 0; scr_rect.y = 0; scr_rect.w = width; scr_rect.h = height; 
        }

        public Transform Transform
        {
            get; set;
        }

        void Resize()
        {
            int ratio = 0;
            if (width > height)
                ratio = width / Settings.Resolution.Width;
            else
                ratio = height / Settings.Resolution.Height;
            
            var newWidth  = ratio * Settings.Resolution.Width;
            var newHeight = ratio * Settings.Resolution.Height;


            draw_rect = new SDL.SDL_FRect();
            draw_rect.w = newWidth;//newWidth;
            draw_rect.h = newHeight;//newHeight;
            //draw_rect.x = (float)Transform.Position.X;
            //draw_rect.y = (float)Transform.Position.Y;
        }

        public string Path {get; private set;}

        public void Draw()
        {
            draw_rect.x = (float)Transform.Position.X;
            draw_rect.y = (float)Transform.Position.Y;
            SDL.SDL_RenderCopyExF(Game.RenderContext, sprite, ref scr_rect, ref draw_rect, Transform.Degrees, IntPtr.Zero, SDL.SDL_RendererFlip.SDL_FLIP_NONE);
        }
    }
}