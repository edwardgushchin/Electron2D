/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using Electron2D.Binding.SDL;

namespace Electron2D.Kernel
{
	public class Timer
	{
		private uint _mStartTicks, _mPausedTicks;
        private bool _mPaused;

        public Timer()
		{
			_mStartTicks = 0;
		    _mPausedTicks = 0;

		    _mPaused = false;
		    IsStarted = false;
		}

		public void Start()
		{
			_mStartTicks = SDL.SDL_GetTicks();
		    _mPausedTicks = 0;

			_mPaused = false;
			IsStarted = true;
		}

        public void Stop()
        {
		    _mStartTicks = 0;
    		_mPausedTicks = 0;

			_mPaused = false;
			IsStarted = false;
        }

        public void Pause()
        {
		    if( IsStarted && !_mPaused )
		    {
		        _mPausedTicks = SDL.SDL_GetTicks() - _mStartTicks;
		        _mStartTicks = 0;

				_mPaused = true;
		    }
        }

        public void Unpause()
        {
		    if( IsStarted && _mPaused )
		    {
		        _mStartTicks = SDL.SDL_GetTicks() - _mPausedTicks;
		        _mPausedTicks = 0;

				_mPaused = false;
		    }
        }

        public uint GetTicks()
        {
		    uint time = 0;

		    if( IsStarted )
		    {
		        if( _mPaused )
		        {
		            time = _mPausedTicks;
		        }
		        else
		        {
		            time = SDL.SDL_GetTicks() - _mStartTicks;
		        }
		    }

		    return time;
        }

        public bool IsStarted { get; private set; }

        public bool IsPaused
		{
		    get { return _mPaused && IsStarted; }
		}
	}
}
