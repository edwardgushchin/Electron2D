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
        private readonly int _samples;
        private int _sprite_offset;
        private double _samples_offset;
        public Animation(string name, int samples)
        {
            _sprites = new List<Sprite>();
            _samples = samples;
            _sprite_offset = 0;
            _samples_offset = 0;
            Name = name;
        }

        public string Name { get; }

        public void AddSprite(Sprite sprite)
        {
            _sprites.Add(sprite);
        }

        public void Update()
        {
            _samples_offset += _samples * 5 * Time.DeltaTime;

            if(_samples_offset >= _samples)
            {
                _samples_offset = 0;

                if(_sprite_offset < _sprites.Count - 1)
                    _sprite_offset++;
                else
                    _sprite_offset = 0;

                _sprites.ForEach((Sprite s) => s.Visible = false);
            }
            _sprites[_sprite_offset].Visible = true;
        }
    }
}