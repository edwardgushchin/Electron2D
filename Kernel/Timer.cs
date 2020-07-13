/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using Electron2D.Binding.SDL;

namespace Electron2D.Kernel
{
	public class Timer
	{
		private uint mStartTicks, mPausedTicks;
        private bool mPaused, mStarted;

		public Timer()
		{
			mStartTicks = 0;
		    mPausedTicks = 0;

		    mPaused = false;
		    mStarted = false;
		}

		public void Start()
		{
			mStarted = true;
		    mPaused = false;
		    mStartTicks = SDL.SDL_GetTicks();
		    mPausedTicks = 0;
		}

        public void Stop()
        {
		    mStarted = false;
		    mPaused = false;
		    mStartTicks = 0;
    		mPausedTicks = 0;
        }

        public void Pause()
        {
		    if( mStarted && !mPaused )
		    {
		        mPaused = true;

		        mPausedTicks = SDL.SDL_GetTicks() - mStartTicks;
		        mStartTicks = 0;
		    }
        }

        public void Unpause()
        {
		    if( mStarted && mPaused )
		    {
		        mPaused = false;

		        mStartTicks = SDL.SDL_GetTicks() - mPausedTicks;

		        mPausedTicks = 0;
		    }
        }

        public uint GetTicks()
        {
		    uint time = 0;

		    if( mStarted )
		    {
		        if( mPaused )
		        {
		            time = mPausedTicks;
		        }
		        else
		        {
		            time = SDL.SDL_GetTicks() - mStartTicks;
		        }
		    }

		    return time;
        }

        public bool IsStarted
		{
		    get { return mStarted; }
		}

		public bool IsPaused
		{
		    get { return mPaused && mStarted; }
		}
	}
}
