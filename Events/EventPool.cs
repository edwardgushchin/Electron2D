/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using Electron2D.Kernel;
using Electron2D.Inputs;
using Electron2D.Graphics;
using Electron2D.Binding.SDL;

namespace Electron2D.Events
{
	internal class EventPool
	{
		public EventPool()
		{
			if(SDL.SDL_Init(SDL.SDL_INIT_EVENTS) != 0)
			{
				Debug.Log("An error occurred while initializing the event subsystem: " + SDL.SDL_GetError(), Debug.Sender.Event, Debug.MessageStatus.Error);
				Initialized = false;
			}
			else
			{
				Debug.Log("Initialization of the event subsystem completed successfully.", Debug.Sender.Event);
				Initialized = true;
			}
		}

		public bool Initialized
		{
			internal set;
			get;
		}

		#region Keyboard Events
		public event System.EventHandler<KeyboardEventArgs> OnKeyDownEvent;
		public event System.EventHandler<KeyboardEventArgs> OnKeyUpEvent;
		#endregion

		#region Mouse Events
		public event System.EventHandler<MouseButtonEventArgs> OnMouseButtonDownEvent;
		public event System.EventHandler<MouseButtonEventArgs> OnMouseButtonUpEvent;
		public event System.EventHandler<MouseMotionEventArgs> OnMouseMotionEvent;
		public event System.EventHandler<MouseWheelEventArgs> OnMouseWheelEvent;
		#endregion

		#region Window Events
		public event System.EventHandler<WindowEventArgs> OnWindowShowEvent;
		public event System.EventHandler<WindowEventArgs> OnWindowHiddenEvent;
		public event System.EventHandler<WindowEventArgs> OnWindowExposedEvent;
		public event System.EventHandler<WindowEventArgs> OnWindowMovedEvent;
		public event System.EventHandler<WindowEventArgs> OnWindowResizedEvent;
		public event System.EventHandler<WindowEventArgs> OnWindowSizeChangedEvent;
		public event System.EventHandler<WindowEventArgs> OnWindowMinimizedEvent;
		public event System.EventHandler<WindowEventArgs> OnWindowMaximizedEvent;
		public event System.EventHandler<WindowEventArgs> OnWindowRestoredEvent;
		public event System.EventHandler<WindowEventArgs> OnWindowEnterEvent;
		public event System.EventHandler<WindowEventArgs> OnWindowLeaveEvent;
		public event System.EventHandler<WindowEventArgs> OnWindowFocusGainedEvent;
		public event System.EventHandler<WindowEventArgs> OnWindowFocusLostEvent;
		public event System.EventHandler<WindowEventArgs> OnWindowCloseEvent;
		#endregion

		public void Pool()
		{
            while (SDL.SDL_PollEvent(out SDL.SDL_Event e) == 1)
            {
                PoolWindowEvent(e);
                PoolKeyEvent(e);
                PoolMouseEvent(e);
            }
        }

		private void PoolWindowEvent(SDL.SDL_Event e)
		{
			if(e.type == SDL.SDL_EventType.SDL_WINDOWEVENT)
			{
                switch (e.window.windowEvent)
                {
                    case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_SHOWN:
                        OnWindowShowEvent(e.window.windowEvent, new WindowEventArgs());
                        break;
                    case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_HIDDEN:
                        OnWindowHiddenEvent(e.window.windowEvent, new WindowEventArgs());
                        break;
                    case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_EXPOSED:
                        OnWindowExposedEvent(e.window.windowEvent, new WindowEventArgs());
                        break;
                    case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_MOVED:
                        OnWindowMovedEvent(e.window.windowEvent, new WindowEventArgs(new Point(e.window.data1, e.window.data2)));
                        break;
                    case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_RESIZED:
                        OnWindowResizedEvent(e.window.windowEvent, new WindowEventArgs(new Size(e.window.data1, e.window.data2)));
                        break;
                    case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_SIZE_CHANGED:
                        Settings.Resolution = new Size(e.window.data1, e.window.data2);
                        OnWindowSizeChangedEvent(e.window.windowEvent, new WindowEventArgs(new Size(e.window.data1, e.window.data2)));
                        break;
                    case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_MAXIMIZED:
                        OnWindowMaximizedEvent(e.window.windowEvent, new WindowEventArgs());
                        break;
                    case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_MINIMIZED:
                        OnWindowMinimizedEvent(e.window.windowEvent, new WindowEventArgs());
                        break;
                    case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_RESTORED:
                        OnWindowRestoredEvent(e.window.windowEvent, new WindowEventArgs());
                        break;
                    case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_ENTER:
                        OnWindowEnterEvent(e.window.windowEvent, new WindowEventArgs());
                        break;
                    case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_LEAVE:
                        OnWindowLeaveEvent(e.window.windowEvent, new WindowEventArgs());
                        break;
                    case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_GAINED:
                        OnWindowFocusGainedEvent(e.window.windowEvent, new WindowEventArgs());
                        break;
                    case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_LOST:
                        OnWindowFocusLostEvent(e.window.windowEvent, new WindowEventArgs());
                        break;
                    case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE:
                        OnWindowCloseEvent(e.window.windowEvent, new WindowEventArgs());
                        break;
                }
            }
		}

		private void PoolKeyEvent(SDL.SDL_Event e )
		{
			var ev = new KeyboardEventArgs((Keyboard.Keys)e.key.keysym.sym, (Keyboard.KeyMod)e.key.keysym.mod);
			if(e.type == SDL.SDL_EventType.SDL_KEYDOWN)
			{
				Input.SetKeyDown(ev.Key, true);
				OnKeyDownEvent(e.window.windowEvent, ev);
			}

			if(e.type == SDL.SDL_EventType.SDL_KEYUP)
			{
				Input.SetKeyDown(ev.Key, false);
				OnKeyUpEvent(e.window.windowEvent, new KeyboardEventArgs((Keyboard.Keys)e.key.keysym.sym, (Keyboard.KeyMod)e.key.keysym.mod));
			}
		}

		private void PoolMouseEvent(SDL.SDL_Event e)
		{
			if(e.type == SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN)
			{
				var ev = new MouseButtonEventArgs((Mouse.Button)e.button.button, new Point(e.button.x, e.button.y), e.button.clicks);
				Input.SetMouseButtonDown(ev.Button, true);
				OnMouseButtonDownEvent(e.window.windowEvent, ev);
			}
			
			if(e.type == SDL.SDL_EventType.SDL_MOUSEBUTTONUP)
			{
				var ev = new MouseButtonEventArgs((Mouse.Button)e.button.button, new Point(e.button.x, e.button.y), e.button.clicks);
				Input.SetMouseButtonDown(ev.Button, false);
				OnMouseButtonUpEvent(e.window.windowEvent, ev);
			}

			if(e.type == SDL.SDL_EventType.SDL_MOUSEMOTION)
				OnMouseMotionEvent(e.window.windowEvent, new MouseMotionEventArgs(new Point(e.motion.x, e.motion.y), e.motion.xrel, e.motion.yrel));

			if(e.type == SDL.SDL_EventType.SDL_MOUSEWHEEL)
				OnMouseWheelEvent(e.window.windowEvent, new MouseWheelEventArgs(new Point(e.wheel.x, e.wheel.y)));
		}
	}
}
