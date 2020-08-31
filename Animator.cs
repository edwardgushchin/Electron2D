/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using System.Collections.Generic;

namespace Electron2D
{
    public class Animator
    {
        private readonly List<Animation> _animations;

        public Animator()
        {
            _animations = new List<Animation>();
        }

		public void Play(string name)
		{
			_animations.Find(a => a.Name == name).Update();
		}

        public void Add(Animation animation)
        {
            _animations.Add(animation);
        }

		public void Remove(Animation animation)
		{
			_animations.Remove(animation);
		}
    }
}