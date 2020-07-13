/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using Electron2D.Graphics;

namespace Electron2D.Kernel
{
    public sealed class Resource<T>
    {
        private Sprite sprite;

        public Resource(Sprite sprite)
        {
            this.sprite = sprite;
        }

        public Resource(string name, string path)
        {
            Name = name;
            Path = path;
        }

        public string Path { get; }

        public string Name { get; }
    }
}