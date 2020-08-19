/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using System.Collections.Generic;

namespace Electron2D.Graphics
{
    internal static class SpriteRenderer
    {
        private static readonly List<Sprite> _spriteCache;

		static SpriteRenderer()
		{
			_spriteCache = new List<Sprite>();
		}

		internal static void Add(Sprite sprite)
		{
			_spriteCache.Add(sprite);
		}

		internal static void Sort()
		{
			_spriteCache.Sort((x, y) => x.Layer.CompareTo(y.Layer));
		}

		internal static void Update()
		{
			_spriteCache.ForEach((Sprite sprite) => {

				var cameraBounds = Camera.MainCamera.Bounds;
				var cameraPos = Camera.MainCamera.Transform.Position;
				var spritePos = sprite.Transform.Position;

				var left = spritePos.X - cameraPos.X + (sprite.Size.Width / 2) > cameraBounds.X - cameraPos.X;
				var right = spritePos.X - cameraPos.X - (sprite.Size.Width / 2) < -cameraBounds.X + cameraPos.X;
				var top = spritePos.Y + cameraPos.Y - (sprite.Size.Height / 2) < cameraBounds.Y + cameraPos.Y;
				var bottom = spritePos.Y - cameraPos.Y + (sprite.Size.Height / 2) > -cameraBounds.Y + cameraPos.Y;

				if(left && right && top && bottom) {
					sprite.Draw();
				}
			});
		}
    }
}