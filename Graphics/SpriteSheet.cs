/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using System;
using System.Xml;
using System.Collections.Generic;

namespace Electron2D.Graphics
{
    public class SpriteSheet
    {
        private readonly Dictionary<string, Sprite> _spriteCache;

		public SpriteSheet(Texture texture, XmlElement atlas, int pixelPerUnit)
		{
			_spriteCache = new Dictionary<string, Sprite>();

			foreach(XmlNode xnode in atlas)
			{
				var h = Convert.ToDouble(xnode.Attributes["height"].Value);
				var w = Convert.ToDouble(xnode.Attributes["width"].Value);
				var x = Convert.ToDouble(xnode.Attributes["x"].Value);
				var y = Convert.ToDouble(xnode.Attributes["y"].Value);
				var n = xnode.Attributes["name"].Value;

				_spriteCache.Add(n, new Sprite(texture, new Bounds(x, y, w, h), 0, pixelPerUnit));

				Debug.Log($"Sprite \"{n}\" from texture atlas loaded successfully.", Debug.Sender.ResourceManager, Debug.MessageStatus.Log);
			}
		}

		public Dictionary<string, Sprite> Sprite => _spriteCache;
    }
}