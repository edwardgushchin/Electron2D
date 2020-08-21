/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using System.IO;
using System.Xml;
using System.Collections.Generic;

using Electron2D.Graphics;

namespace Electron2D.Kernel
{
    public static class ResourceManager
    {
        //private static readonly List<Font> fontCache;
        private static readonly Dictionary<string, Texture> textureCache;

        static ResourceManager()
        {
            //fontCache = new List<Font>();
            textureCache = new Dictionary<string, Texture>();
        }

        /*internal static int FontCacheCount
        {
            get { return fontCache.Count; }
        }*/

        internal static int TextureCacheCount => textureCache.Count;

        /*public static Font GetFont(string path)
        {
            return fontCache.Find(x => x.Path.Contains(path));
        }*/

        /*public static Sprite GetSprite(string path)
        {
            return spriteCache.Find(x => x.Path.Contains(path));
        }*/

        public static Texture GetTexture(string resourceName)
        {
            return textureCache[resourceName];
        }

        /*public static Font LoadFont(string path, int size)
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
        }*/

        /*public static Sprite LoadSprite(string path)
        {
            if(!new System.IO.FileInfo(path).Exists)
            {
                Debug.Log($"Resource not found on path \"{path}\"!", Debug.Sender.ResourceManager, Debug.MessageStatus.Error);
                return null;
            }
            spriteCache.Add(new Sprite(path));
            Debug.Log($"Resource \"{path}\" was successfully loaded.", Debug.Sender.ResourceManager);
            return spriteCache[^1];
        }*/

        public static Texture LoadTexture(string name, string path)
        {
            if(!new FileInfo(path).Exists)
            {
                Debug.Log($"Texture not found on path \"{path}\"!", Debug.Sender.ResourceManager, Debug.MessageStatus.Error);
                return null;
            }
            textureCache.Add(name, new Texture(path));
            Debug.Log($"Texture \"{name}\" was successfully loaded.", Debug.Sender.ResourceManager);
            return textureCache[name];
        }

        public static SpriteSheet LoadTextureAtlas(string name, string path)
        {
            Debug.Log($"Texture atlas \"{name}\" loading...", Debug.Sender.ResourceManager, Debug.MessageStatus.Log);
            if(!new FileInfo(path).Exists)
            {
                Debug.Log($"Texture atlas not found on path \"{path}\"!", Debug.Sender.ResourceManager, Debug.MessageStatus.Error);
                return null;
            }

            var xmlAtlas = new XmlDocument();
            xmlAtlas.Load(path);
            var atlasRoot = xmlAtlas.DocumentElement;
            var texturePath = atlasRoot.Attributes[0].Value;

            if(!new FileInfo(texturePath).Exists)
            {
                Debug.Log($"Texture not found on path \"{path}\"!", Debug.Sender.ResourceManager, Debug.MessageStatus.Error);
                return null;
            }

            textureCache.Add(name, new Texture(texturePath));
            var spriteSheet = new SpriteSheet(textureCache[name], atlasRoot);
            Debug.Log($"Texture atlas \"{name}\" was successfully loaded.", Debug.Sender.ResourceManager);
            return spriteSheet;
        }

        /*public void asd()
        {
            long size = 0;
            object obj = new object();
            using (System.IO.Stream stream = new System.IO.MemoryStream()) {
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                formatter.Serialize(stream, );
                size = stream.Length;
            }
        }*/
    }
}