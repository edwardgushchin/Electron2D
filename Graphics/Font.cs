/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using System;
using Electron2D.Binding.SDL;
using System.Runtime.InteropServices;

namespace Electron2D.Graphics
{
    public class Font
    {
        private IntPtr font, renderTextSurface, textTexture;
        private SDL.SDL_FRect labelRectangle;
        private SDL.SDL_Surface srcSurfaceTexture;
        private int size;

        public Font(string path, IntPtr pFont, int size)
        {
            this.Path = path;
            this.font = pFont;
            this.Color = Color.White;
            this.size = size;
            this.labelRectangle = new SDL.SDL_FRect();
        }

        public int Size
        {
            get { return size; }
            set
            {
                font = TTFont.TTF_OpenFont(Path, size);
                size = value;
            }
        }

        public Color Color { get; set; }

        public string Path { get; set; }

        /*internal void Draw(string text, RectTransform transform, Color color, int size)
        {
            SDL.SDL_FreeSurface(renderTextSurface);
            SDL.SDL_DestroyTexture(textTexture);

            if (this.size != size) Size = size;

            renderTextSurface = TTFont.TTF_RenderText_Blended(font, text, color.ConvertToSDLColor());
            textTexture = SDL.SDL_CreateTextureFromSurface(Game.RenderContext, renderTextSurface);
            srcSurfaceTexture = (SDL.SDL_Surface)Marshal.PtrToStructure(renderTextSurface, typeof(SDL.SDL_Surface));

            labelRectangle.h = srcSurfaceTexture.clip_rect.h;
            labelRectangle.w = srcSurfaceTexture.clip_rect.w;
            labelRectangle.x = (float)transform.Left;
            labelRectangle.y = (float)transform.Top;

            SDL.SDL_RenderCopyExF(Game.RenderContext, textTexture, ref srcSurfaceTexture.clip_rect, ref labelRectangle, transform.Degrees, IntPtr.Zero, SDL.SDL_RendererFlip.SDL_FLIP_NONE);
        }*/

        public void Dispose()
        {
            SDL.SDL_FreeSurface(renderTextSurface);
            SDL.SDL_DestroyTexture(textTexture);
        }
    }
}