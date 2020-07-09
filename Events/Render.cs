/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using System;

namespace Electron2D.Events
{
	public delegate void RenderEventHundler(double deltaTime);
	
	public class RenderEventArgs : EventArgs
	{
		public RenderEventArgs(double deltaTime)
		{
			this.deltaTime = deltaTime;
		}
		
		public double deltaTime { get; private set; }
	}
}
