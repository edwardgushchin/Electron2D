/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using System;

using Electron2D.Inputs;

namespace Electron2D.Events
{
	public delegate void KeyboardEventHundler(object sender, KeyboardEventArgs e);

	public class KeyboardEventArgs : EventArgs
	{
		public KeyboardEventArgs(Keyboard.Keys Key, Keyboard.KeyMod Mod)
		{
			this.Key = Key;
			this.Mod = Mod;
		}

		public Keyboard.Keys Key { get; internal set; }
		public Keyboard.KeyMod Mod { get; internal set; }
	}
}
