/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using System;

using Electron2D.Binding.SDL;

namespace Electron2D.Graphics
{
    public class Texture : IDisposable
    {
        internal Bounds DrawRect;

        internal Texture(string path)
        {
            Instance = Image.IMG_LoadTexture(Game.RenderContext, path);
            SDL.SDL_QueryTexture(Instance, out uint format, out int access, out int width, out int height);
            DrawRect = new Bounds { X = 0, Y = 0, Width = width, Height = height};
            PixelFormat = ConvertToPixelFormat(format);
            Access = access;
            Width = width;
            Height = height;
        }

        public Texture(int width, int height, PixelFormat pixelFormat)
        {
            Instance = SDL.SDL_CreateTexture(Game.RenderContext, ConvertToSDLPixelFormat(pixelFormat), 0, width, height);
            DrawRect = new Bounds { X = 0, Y = 0, Width = width, Height = height};
        }

        private uint ConvertToSDLPixelFormat(PixelFormat pixelFormat) => pixelFormat switch
        {
            PixelFormat.Unknown => SDL.SDL_PIXELFORMAT_UNKNOWN,
            PixelFormat.INDEX1LSB => SDL.SDL_PIXELFORMAT_INDEX1LSB,
            PixelFormat.INDEX1MSB => SDL.SDL_PIXELFORMAT_INDEX1MSB,
            PixelFormat.INDEX4LSB => SDL.SDL_PIXELFORMAT_INDEX4LSB,
            PixelFormat.INDEX4MSB => SDL.SDL_PIXELFORMAT_INDEX4MSB,
            PixelFormat.INDEX8 => SDL.SDL_PIXELFORMAT_INDEX8,
            PixelFormat.RGB332 => SDL.SDL_PIXELFORMAT_RGB332,
            PixelFormat.RGB444 => SDL.SDL_PIXELFORMAT_RGB444,
            PixelFormat.BGR444 => SDL.SDL_PIXELFORMAT_BGR444,
            PixelFormat.RGB555 => SDL.SDL_PIXELFORMAT_RGB555,
            PixelFormat.BGR555 => SDL.SDL_PIXELFORMAT_BGR555,
            PixelFormat.ARGB4444 => SDL.SDL_PIXELFORMAT_ARGB4444,
            PixelFormat.RGBA4444 => SDL.SDL_PIXELFORMAT_RGBA4444,
            PixelFormat.ABGR4444 => SDL.SDL_PIXELFORMAT_ABGR4444,
            PixelFormat.BGRA4444 => SDL.SDL_PIXELFORMAT_BGRA4444,
            PixelFormat.ARGB1555 => SDL.SDL_PIXELFORMAT_ARGB1555,
            PixelFormat.RGBA5551 => SDL.SDL_PIXELFORMAT_RGBA5551,
            PixelFormat.ABGR1555 => SDL.SDL_PIXELFORMAT_ABGR1555,
            PixelFormat.BGRA5551 => SDL.SDL_PIXELFORMAT_BGRA5551,
            PixelFormat.RGB565 => SDL.SDL_PIXELFORMAT_RGB565,
            PixelFormat.BGR565 => SDL.SDL_PIXELFORMAT_BGR565,
            PixelFormat.RGB24 => SDL.SDL_PIXELFORMAT_RGB24,
            PixelFormat.BGR24 => SDL.SDL_PIXELFORMAT_BGR24,
            PixelFormat.RGB888 => SDL.SDL_PIXELFORMAT_RGB888,
            PixelFormat.RGBX8888 => SDL.SDL_PIXELFORMAT_RGBX8888,
            PixelFormat.BGR888 => SDL.SDL_PIXELFORMAT_BGR888,
            PixelFormat.BGRX8888 => SDL.SDL_PIXELFORMAT_BGRX8888,
            PixelFormat.ARGB8888 => SDL.SDL_PIXELFORMAT_ARGB8888,
            PixelFormat.RGBA8888 => SDL.SDL_PIXELFORMAT_RGBA8888,
            PixelFormat.ABGR8888 => SDL.SDL_PIXELFORMAT_ABGR8888,
            PixelFormat.ARGB2101010 => SDL.SDL_PIXELFORMAT_ARGB2101010,
            PixelFormat.YV12 => SDL.SDL_PIXELFORMAT_YV12,
            PixelFormat.IYUV => SDL.SDL_PIXELFORMAT_IYUV,
            PixelFormat.YUY2 => SDL.SDL_PIXELFORMAT_YUY2,
            PixelFormat.UYVY => SDL.SDL_PIXELFORMAT_UYVY,
            PixelFormat.YVYU => SDL.SDL_PIXELFORMAT_YVYU,
            _ => SDL.SDL_PIXELFORMAT_UNKNOWN,
        };

        private PixelFormat ConvertToPixelFormat(uint pixelFormat)
        {
            if (pixelFormat == SDL.SDL_PIXELFORMAT_INDEX1LSB) return PixelFormat.INDEX1LSB;
            else if (pixelFormat == SDL.SDL_PIXELFORMAT_INDEX1MSB) return PixelFormat.INDEX1MSB;
            else if (pixelFormat == SDL.SDL_PIXELFORMAT_INDEX1LSB) return PixelFormat.INDEX1LSB;
            else if (pixelFormat == SDL.SDL_PIXELFORMAT_INDEX4LSB) return PixelFormat.INDEX4LSB;
            else if (pixelFormat == SDL.SDL_PIXELFORMAT_INDEX4MSB) return PixelFormat.INDEX4MSB;
            else if (pixelFormat == SDL.SDL_PIXELFORMAT_INDEX8) return PixelFormat.INDEX8;
            else if (pixelFormat == SDL.SDL_PIXELFORMAT_RGB332) return PixelFormat.RGB332;
            else if (pixelFormat == SDL.SDL_PIXELFORMAT_RGB444) return PixelFormat.RGB444;
            else if (pixelFormat == SDL.SDL_PIXELFORMAT_BGR444) return PixelFormat.BGR444;
            else if (pixelFormat == SDL.SDL_PIXELFORMAT_RGB555) return PixelFormat.RGB555;
            else if (pixelFormat == SDL.SDL_PIXELFORMAT_BGR555) return PixelFormat.BGR555;
            else if (pixelFormat == SDL.SDL_PIXELFORMAT_ARGB4444) return PixelFormat.ARGB4444;
            else if (pixelFormat == SDL.SDL_PIXELFORMAT_RGBA4444) return PixelFormat.RGBA4444;
            else if (pixelFormat == SDL.SDL_PIXELFORMAT_ABGR4444) return PixelFormat.ABGR4444;
            else if (pixelFormat == SDL.SDL_PIXELFORMAT_BGRA4444) return PixelFormat.BGRA4444;
            else if (pixelFormat == SDL.SDL_PIXELFORMAT_ARGB1555) return PixelFormat.ARGB1555;
            else if (pixelFormat == SDL.SDL_PIXELFORMAT_RGBA5551) return PixelFormat.RGBA5551;
            else if (pixelFormat == SDL.SDL_PIXELFORMAT_ABGR1555) return PixelFormat.ABGR1555;
            else if (pixelFormat == SDL.SDL_PIXELFORMAT_BGRA5551) return PixelFormat.BGRA5551;
            else if (pixelFormat == SDL.SDL_PIXELFORMAT_RGB565) return PixelFormat.RGB565;
            else if (pixelFormat == SDL.SDL_PIXELFORMAT_BGR565) return PixelFormat.BGR565;
            else if (pixelFormat == SDL.SDL_PIXELFORMAT_RGB24) return PixelFormat.RGB24;
            else if (pixelFormat == SDL.SDL_PIXELFORMAT_BGR24) return PixelFormat.BGR24;
            else if (pixelFormat == SDL.SDL_PIXELFORMAT_RGB888) return PixelFormat.RGB888;
            else if (pixelFormat == SDL.SDL_PIXELFORMAT_RGBX8888) return PixelFormat.RGBX8888;
            else if (pixelFormat == SDL.SDL_PIXELFORMAT_BGRX8888) return PixelFormat.BGRX8888;
            else if (pixelFormat == SDL.SDL_PIXELFORMAT_ARGB8888) return PixelFormat.ARGB8888;
            else if (pixelFormat == SDL.SDL_PIXELFORMAT_RGBA8888) return PixelFormat.RGBA8888;
            else if (pixelFormat == SDL.SDL_PIXELFORMAT_ABGR8888) return PixelFormat.ABGR8888;
            else if (pixelFormat == SDL.SDL_PIXELFORMAT_ARGB2101010) return PixelFormat.ARGB2101010;
            else if (pixelFormat == SDL.SDL_PIXELFORMAT_YV12) return PixelFormat.YV12;
            else if (pixelFormat == SDL.SDL_PIXELFORMAT_IYUV) return PixelFormat.IYUV;
            else if (pixelFormat == SDL.SDL_PIXELFORMAT_YUY2) return PixelFormat.YUY2;
            else if (pixelFormat == SDL.SDL_PIXELFORMAT_UYVY) return PixelFormat.UYVY;
            else if (pixelFormat == SDL.SDL_PIXELFORMAT_YVYU) return PixelFormat.YVYU;
            else return PixelFormat.Unknown;
        }

        internal IntPtr Instance { get; }

        public int Width { get; }

        public int Height { get; }

        public PixelFormat PixelFormat { get; }

        public int Access { get; }

        public void Dispose()
        {
            SDL.SDL_DestroyTexture(Instance);
        }
    }
}