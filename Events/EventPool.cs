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
			private set;
			get;
		}
		
		#region Keyboard Events
		public event KeyboardEventHundler OnKeyDownEvent;
		public event KeyboardEventHundler OnKeyUpEvent;
		#endregion
		
		#region Mouse Events
		public event MouseButtonEventHundler OnMouseButtonDownEvent;
		public event MouseButtonEventHundler OnMouseButtonUpEvent;
		public event MouseMotionEventHundler OnMouseMotionEvent;
		public event MouseWheelEventHundler OnMouseWheelEvent;
		#endregion
		
		#region Window Events
		public event WindowEventHundler OnWindowShowEvent;
		public event WindowEventHundler OnWindowHiddenEvent;
		public event WindowEventHundler OnWindowExposedEvent;
		public event WindowEventHundler OnWindowMovedEvent;
		public event WindowEventHundler OnWindowResizedEvent;
		public event WindowEventHundler OnWindowSizeChangedEvent;
		public event WindowEventHundler OnWindowMinimizedEvent;
		public event WindowEventHundler OnWindowMaximizedEvent;
		public event WindowEventHundler OnWindowRestoredEvent;
		public event WindowEventHundler OnWindowEnterEvent;
		public event WindowEventHundler OnWindowLeaveEvent;
		public event WindowEventHundler OnWindowFocusGainedEvent;
		public event WindowEventHundler OnWindowFocusLostEvent;
		public event WindowEventHundler OnWindowCloseEvent;
		#endregion
		
		public void Pool()
		{
			SDL.SDL_Event e;
			while (SDL.SDL_PollEvent(out e) == 1)
			{
				poolWindowEvent(e);
				poolKeyEvent(e);
				poolMouseEvent(e);
			}
		}
		
		void poolWindowEvent(SDL.SDL_Event e)
		{
			if(e.type == SDL.SDL_EventType.SDL_WINDOWEVENT)
			{
				if(e.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_SHOWN)
					OnWindowShowEvent(e.window.windowEvent, new WindowEventArgs());
				
				else if(e.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_HIDDEN)
					OnWindowHiddenEvent(e.window.windowEvent, new WindowEventArgs());

				
				else if(e.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_EXPOSED) 
					OnWindowExposedEvent(e.window.windowEvent, new WindowEventArgs());
				
				else if(e.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_MOVED)
					OnWindowMovedEvent(e.window.windowEvent, new WindowEventArgs(new Point(e.window.data1, e.window.data2)));
			
				else if(e.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_RESIZED)
					OnWindowResizedEvent(e.window.windowEvent, new WindowEventArgs(new Size(e.window.data1, e.window.data2)));
				
				else if(e.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_SIZE_CHANGED) {
					Settings.Resolution = new Size(e.window.data1, e.window.data2);
					OnWindowSizeChangedEvent(e.window.windowEvent, new WindowEventArgs(new Size(e.window.data1, e.window.data2)));
				}
				
				else if(e.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_MAXIMIZED)
					OnWindowMaximizedEvent(e.window.windowEvent, new WindowEventArgs());
			
				else if(e.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_MINIMIZED)
					OnWindowMinimizedEvent(e.window.windowEvent, new WindowEventArgs());
				
				else if(e.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_RESTORED)
					OnWindowRestoredEvent(e.window.windowEvent, new WindowEventArgs());
				
				else if(e.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_ENTER) 
					OnWindowEnterEvent(e.window.windowEvent, new WindowEventArgs());
				
				else if(e.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_LEAVE)
					OnWindowLeaveEvent(e.window.windowEvent, new WindowEventArgs());
				
				else if(e.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_GAINED)
					OnWindowFocusGainedEvent(e.window.windowEvent, new WindowEventArgs());
				
				else if(e.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_LOST)
					OnWindowFocusLostEvent(e.window.windowEvent, new WindowEventArgs());
				
				else if(e.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE)
					OnWindowCloseEvent(e.window.windowEvent, new WindowEventArgs());
			}
		}
		
		void poolKeyEvent(SDL.SDL_Event e )
		{
			if(e.type == SDL.SDL_EventType.SDL_KEYDOWN)
			{
				var ev = new KeyboardEventArgs((Keyboard.Keys)e.key.keysym.sym, (Keyboard.KeyMod)e.key.keysym.mod);
				Input.SetKeyDown(ev.Key, true);
				OnKeyDownEvent(e.window.windowEvent, ev);
			}
			
			if(e.type == SDL.SDL_EventType.SDL_KEYUP)
			{
				var ev = new KeyboardEventArgs((Keyboard.Keys)e.key.keysym.sym, (Keyboard.KeyMod)e.key.keysym.mod);
				Input.SetKeyDown(ev.Key, false);
				OnKeyUpEvent(e.window.windowEvent, new KeyboardEventArgs((Keyboard.Keys)e.key.keysym.sym, (Keyboard.KeyMod)e.key.keysym.mod));
			}
		}
		
		void poolMouseEvent(SDL.SDL_Event e)
		{
			if(e.type == SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN)
				OnMouseButtonDownEvent(e.window.windowEvent, new MouseButtonEventArgs((Mouse.Button)e.button.button, new Point(e.button.x, e.button.y), e.button.clicks));
			
			if(e.type == SDL.SDL_EventType.SDL_MOUSEBUTTONUP)
				OnMouseButtonUpEvent(e.window.windowEvent, new MouseButtonEventArgs((Mouse.Button)e.button.button, new Point(e.button.x, e.button.y), e.button.clicks));
			
			if(e.type == SDL.SDL_EventType.SDL_MOUSEMOTION)
				OnMouseMotionEvent(e.window.windowEvent, new MouseMotionEventArgs(new Point(e.motion.x, e.motion.y), e.motion.xrel, e.motion.yrel));
			
			if(e.type == SDL.SDL_EventType.SDL_MOUSEWHEEL)
				OnMouseWheelEvent(e.window.windowEvent, new MouseWheelEventArgs(new Point(e.wheel.x, e.wheel.y)));
		}
	}
}
