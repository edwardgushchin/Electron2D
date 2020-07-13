/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using System;
using System.Collections.Generic;

using Electron2D.Graphics;
using Electron2D.Binding.SDL;

namespace Electron2D.Kernel
{
    public static class ResourceManager
    {
        private static readonly List<Font> fontCache;
        private static readonly List<Sprite> spriteCache;

        static ResourceManager()
        {
            fontCache = new List<Font>();
            spriteCache = new List<Sprite>();
        }

        internal static int FontCacheCount
        {
            get { return fontCache.Count; }
        }

        internal static int SpriteCacheCount
        {
            get { return spriteCache.Count; }
        }

        public static Font GetFont(string path)
        {
            return fontCache.Find(x => x.Path.Contains(path));
        }

        public static Sprite GetSprite(string path)
        {
            return spriteCache.Find(x => x.Path.Contains(path));
        }

        public static Font LoadFont(string path, int size)
        {
            var f = GetFont(path);
            if (f != null)
            {
                Debug.Log($"Resource \"{path}\" has already been loaded. Use ResourceManager.GetFont(string path) to get the link.", Debug.Sender.ResourceManager, Debug.MessageStatus.Warning);
                return f;
            }
            var pFont = TTFont.TTF_OpenFont(path, size);
            if (pFont != IntPtr.Zero)
            {
                fontCache.Add(new Font(path, pFont, size));
                Debug.Log($"Resource \"{path}\" was successfully loaded.", Debug.Sender.ResourceManager);
                return fontCache[^1];
            }
            Debug.Log($"Failed to load font! TTFont Error: {SDL.SDL_GetError()}", Debug.Sender.ResourceManager, Debug.MessageStatus.Error);
            return null;
        }

        public static Sprite LoadSprite(string path)
        {
            if(!new System.IO.FileInfo(path).Exists)
            {
                Debug.Log($"Resource not found on path \"{path}\"!", Debug.Sender.ResourceManager, Debug.MessageStatus.Error);
                return null;
            }
            spriteCache.Add(new Sprite(path));
            Debug.Log($"Resource \"{path}\" was successfully loaded.", Debug.Sender.ResourceManager);
            return spriteCache[^1];
        }
    }
}