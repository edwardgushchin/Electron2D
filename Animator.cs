/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using System.Collections.Generic;

using Electron2D.Graphics;

namespace Electron2D
{
    public class Animator
    {
        private readonly Dictionary<string, Animation> _animations;
        private string _playedAnimation;
        private readonly int _pixelPerUnit;
        private int _layer;

        public Animator(Point position, int layer, int pixelPerUnit)
        {
            _animations = new Dictionary<string, Animation>();
            Transform = new Transform(position);
            _layer = layer;
            _pixelPerUnit = pixelPerUnit;
        }

        public Transform Transform { get; set; }

        public int Layer 
        {
            get => _layer;
            set
            {
                foreach (var a in _animations)
                    a.Value.UpdateLayer(value);
                _layer = value;
            }
        }

        public void Play(string name)
        {
            Play(name, false);
        }

		public void Play(string name, bool flipX)
		{
            if(_playedAnimation != null && _playedAnimation != name)
                _animations[_playedAnimation].Reset();
            _animations[name].Update(Transform, _pixelPerUnit, flipX);
            _playedAnimation = name;
		}

        public void Add(Animation animation)
        {
            animation.Reset();
            animation.UpdateLayer(_layer);
            _animations.Add(animation.Name, animation);
        }

		public void Remove(string name)
		{
			_animations.Remove(name);
		}
    }
}