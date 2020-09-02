/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using System.Collections.Generic;

using Electron2D.Graphics;

namespace Electron2D
{
    public class Animation
    {
        private readonly List<Sprite> _sprites;
        private readonly double _speed;
        private int _sprite_offset;
        private double _samples_offset;

        public Animation(string name, double speed)
        {
            _sprites = new List<Sprite>();
            _speed = speed;
            _sprite_offset = 0;
            _samples_offset = 0;
            Name = name;
            //Transform = new Transform(position);
        }

        public string Name { get; }

        public Transform Transform { get; set; }

        public void AddSprite(Sprite sprite)
        {
            //sprite.PixelPerUnit = _pixelPerUnit;
            _sprites.Add(sprite);
        }

        public void Reset()
        {
            _sprite_offset = 0;
            foreach (var s in _sprites) s.Visible = false;
        }

        public void Update(Transform transform, int layer, int pixelPerUnit, bool flipX)
        {
            _samples_offset += _speed * Time.DeltaTime;

            if(_samples_offset >= 1)
            {
                _samples_offset = 0;

                if(_sprite_offset < _sprites.Count - 1)
                    _sprite_offset++;
                else
                    _sprite_offset = 0;

                foreach (var s in _sprites) s.Visible = false;
            }

            _sprites[_sprite_offset].Visible = true;
            _sprites[_sprite_offset].FlipX = flipX;
            _sprites[_sprite_offset].Transform = transform;
            _sprites[_sprite_offset].Layer = layer;
            _sprites[_sprite_offset].PixelPerUnit = pixelPerUnit;
        }
    }
}