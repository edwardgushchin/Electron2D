/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using System;
using Electron2D.Events;
using Electron2D.Graphics;
using Electron2D.Binding.SDL;

namespace Electron2D
{
	public class Scene : IDisposable
	{
		private Game _game;
		private Color _clearColor;

		public Scene()
		{
			this.Index = 0;
			_clearColor = Color.Black;
			Camera = new Camera();
		}

		public Scene(int Index)
		{
			this.Index = Index;
			_clearColor = Color.Black;
			Camera = new Camera();
		}

		public Scene(int Index, Color sceneColor)
		{
			this.Index = Index;
			_clearColor = sceneColor;
			Camera = new Camera();
		}

		public void SetCursor(string path)
		{
			SDL.SDL_SetCursor(SDL.SDL_CreateColorCursor(Image.IMG_Load(path), 0, 0));
		}

        public Camera Camera { get; }

		public Color ClearColor
		{
			get { return _clearColor; }
			set
			{
				SDL.SDL_SetRenderDrawColor(Game.RenderContext, value.R, value.G, value.B, value.A);
				_clearColor = value;
			}
		}

		public int Index { get; internal set; }

		private void SubscribeEvents()
		{
			#region Subscribe to Window event
			_game.Events.OnWindowShowEvent += OnWindowShow;
			_game.Events.OnWindowHiddenEvent += OnWindowHidden;
			_game.Events.OnWindowExposedEvent += OnWindowExposed;
			_game.Events.OnWindowMovedEvent += OnWindowMoved;
			_game.Events.OnWindowResizedEvent += OnWindowResized;
			_game.Events.OnWindowResizedEvent += Camera.UpdateUnit;
			_game.Events.OnWindowSizeChangedEvent += OnWindowSizeChanged;
			_game.Events.OnWindowMinimizedEvent += OnWindowMinimized;
			_game.Events.OnWindowMaximizedEvent += OnWindowMaximized;
			_game.Events.OnWindowRestoredEvent += OnWindowRestored;
			_game.Events.OnWindowEnterEvent += OnWindowEnter;
			_game.Events.OnWindowLeaveEvent += OnWindowLeave;
			_game.Events.OnWindowFocusGainedEvent += OnWindowFocusGained;
			_game.Events.OnWindowFocusLostEvent += OnWindowFocusLost;
			_game.Events.OnWindowCloseEvent += OnWindowClose;
			#endregion

			#region Subscribe to Render event
			_game.Render.OnPreUpdate += PreUpdate;
			_game.Render.OnUpdate += Update;
			_game.Render.OnPostUpdate += PostUpdate;
			#endregion

			#region Subscribe to Keyboard events
			_game.Events.OnKeyDownEvent += OnKeyDown;
			_game.Events.OnKeyUpEvent += OnKeyUp;
			#endregion

			#region Subscribe to Mouse events
			_game.Events.OnMouseButtonDownEvent += OnMouseButtonDown;
			_game.Events.OnMouseButtonUpEvent += OnMouseButtonUp;
			_game.Events.OnMouseMotionEvent += OnMouseMotion;
			_game.Events.OnMouseWheelEvent += OnMouseWheel;
			#endregion
		}

		private void DescribeEvents()
		{
			#region Subscribe to Window event
			_game.Events.OnWindowShowEvent -= OnWindowShow;
			_game.Events.OnWindowHiddenEvent -= OnWindowHidden;
			_game.Events.OnWindowExposedEvent -= OnWindowExposed;
			_game.Events.OnWindowMovedEvent -= OnWindowMoved;
			_game.Events.OnWindowResizedEvent -= OnWindowResized;
			_game.Events.OnWindowResizedEvent -= Camera.UpdateUnit;
			_game.Events.OnWindowSizeChangedEvent -= OnWindowSizeChanged;
			_game.Events.OnWindowMinimizedEvent -= OnWindowMinimized;
			_game.Events.OnWindowMaximizedEvent -= OnWindowMaximized;
			_game.Events.OnWindowRestoredEvent -= OnWindowRestored;
			_game.Events.OnWindowEnterEvent -= OnWindowEnter;
			_game.Events.OnWindowLeaveEvent -= OnWindowLeave;
			_game.Events.OnWindowFocusGainedEvent -= OnWindowFocusGained;
			_game.Events.OnWindowFocusLostEvent -= OnWindowFocusLost;
			_game.Events.OnWindowCloseEvent -= OnWindowClose;
			#endregion

			#region Subscribe to Render event
			_game.Render.OnPreUpdate -= PreUpdate;
			_game.Render.OnUpdate -= Update;
			_game.Render.OnPostUpdate -= PostUpdate;
			#endregion

			#region Subscribe to Keyboard events
			_game.Events.OnKeyDownEvent -= OnKeyDown;
			_game.Events.OnKeyUpEvent -= OnKeyUp;
			#endregion

			#region Subscribe to Mouse events
			_game.Events.OnMouseButtonDownEvent -= OnMouseButtonDown;
			_game.Events.OnMouseButtonUpEvent -= OnMouseButtonUp;
			_game.Events.OnMouseMotionEvent -= OnMouseMotion;
			_game.Events.OnMouseWheelEvent -= OnMouseWheel;
			#endregion
		}

		public void Start()
		{
			PreLoadScene();
			SubscribeEvents();
			OnLoadScene();
		}

		public void Dispose()
		{
			DescribeEvents();
		}

		internal void SetGame(Game gameContext)
		{
			_game = gameContext;
		}

		internal void Stop()
		{
			Dispose();
		}

		public string Name
		{
			get { return GetType().Name; }
		}

		protected virtual void PreLoadScene() {}
		protected virtual void OnLoadScene() {}

		#region Render Virtual Metods
		protected virtual void PreUpdate(){}
		protected virtual void Update(){}
		protected virtual void PostUpdate(){}
        #endregion

        #region Keyboard Virtual Metods
        /// <summary>
        /// Событие
        /// </summary>
        /// <param name="sender">Отправитель</param>
        /// <param name="e">Экземпляр <see cref="Electron2D.Events.KeyboardEventArgs"/> с данными о событии</param>
        protected virtual void OnKeyDown(object sender, KeyboardEventArgs e) { }
		protected virtual void OnKeyUp(object sender, KeyboardEventArgs e){}
		#endregion

		#region Mouse Virtual Methods
		protected virtual void OnMouseButtonDown(object sender, MouseButtonEventArgs e) {}
		protected virtual void OnMouseButtonUp(object sender, MouseButtonEventArgs e) {}
		protected virtual void OnMouseMotion(object sender, MouseMotionEventArgs e ) {}
		protected virtual void OnMouseWheel(object sender, MouseWheelEventArgs e ) {}
		#endregion

		#region Window Virtual Metods
		/// <summary>
		/// Событие вызвается при открытии формы
		/// </summary>
		/// <param name="sender">Отправитель</param>
		/// <param name="e">Экземпляр <see cref="Electron2D.Events.WindowEventArgs"/> с данными о событии</param>
		protected virtual void OnWindowShow(object sender, WindowEventArgs e){}

		/// <summary>
		/// Событие вызвается при скрытии формы
		/// </summary>
		/// <param name="sender">Отправитель</param>
		/// <param name="e">Экземпляр <see cref="Electron2D.Events.WindowEventArgs"/> с данными о событии</param>
		protected virtual void OnWindowHidden(object sender, WindowEventArgs e){}

		/// <summary>
		/// Событие вызвается при восстановлении окна. Уведомляет о том, что окно должно быть перерисовано
		/// </summary>
		/// <param name="sender">Отправитель</param>
		/// <param name="e">Экземпляр <see cref="WindowEventArgs"/> с данными о событии</param>
		protected virtual void OnWindowExposed(object sender, WindowEventArgs e){}

		/// <summary>
		/// Событие вызывается после окончания перемещения формы
		/// </summary>
		/// <param name="sender">Отправитель</param>
		/// <param name="e">Экземпляр <see cref="WindowEventArgs"/> с данными о событии</param>
		protected virtual void OnWindowMoved(object sender, WindowEventArgs e){}

		/// <summary>
		/// Событие вызывается после изменение размера формы.
		/// Этому событию всегда предшествует <see cref="Game.OnWindowSizeChanged"/>
		/// </summary>
		/// <param name="sender">Отправитель</param>
		/// <param name="e">Экземпляр <see cref="WindowEventArgs"/> с данными о событии</param>
		protected virtual void OnWindowResized(object sender, WindowEventArgs e){}

		/// <summary>
		/// Размер окна изменился либо в результате вызова API, либо через систему или пользователя,
		/// изменившего размер окна; за этим событием следует <see cref="Electron2D.Game.OnWindowResized"/>,
		/// если размер был изменен внешним событием, т. е. пользователем или оконным менеджером
		/// </summary>
		/// <param name="sender">Отправитель</param>
		/// <param name="e">Экземпляр <see cref="WindowEventArgs"/> с данными о событии</param>
		protected virtual void OnWindowSizeChanged(object sender, WindowEventArgs e){}

		/// <summary>
		/// Событие вызывается после сворачивания формы
		/// </summary>
		/// <param name="sender">Отправитель</param>
		/// <param name="e">Экземпляр <see cref="WindowEventArgs"/> с данными о событии</param>
		protected virtual void OnWindowMinimized(object sender, WindowEventArgs e){}

		/// <summary>
		/// Событие вызывается после разворачивания формы на полный экран
		/// </summary>
		/// <param name="sender">Отправитель</param>
		/// <param name="e">Экземпляр <see cref="WindowEventArgs"/> с данными о событии</param>
		protected virtual void OnWindowMaximized(object sender, WindowEventArgs e){}

		/// <summary>
		/// Событие вызывается когда окно было восстановлено до нормального размера и положения
		/// </summary>
		/// <param name="sender">Отправитель</param>
		/// <param name="e">Экземпляр <see cref="WindowEventArgs"/> с данными о событии</param>
		protected virtual void OnWindowRestored(object sender, WindowEventArgs e){}

		/// <summary>
		/// Событие вызывается когда на окно был наведен фокус мыши
		/// </summary>
		/// <param name="sender">Отправитель</param>
		/// <param name="e">Экземпляр <see cref="WindowEventArgs"/> с данными о событии</param>
		protected virtual void OnWindowEnter(object sender, WindowEventArgs e){}

		/// <summary>
		/// Событие вызывается когда фокус мыши с окна был потерян
		/// </summary>
		/// <param name="sender">Отправитель</param>
		/// <param name="e">Экземпляр <see cref="WindowEventArgs"/> с данными о событии</param>
		protected virtual void OnWindowLeave(object sender, WindowEventArgs e){}

		/// <summary>
		/// Событие вызывается когда окно получило фокус с клавиатуры
		/// </summary>
		/// <param name="sender">Отправитель</param>
		/// <param name="e">Экземпляр <see cref="WindowEventArgs"/> с данными о событии</param>
		protected virtual void OnWindowFocusGained(object sender, WindowEventArgs e){}

		/// <summary>
		/// Событие вызывается когда фокус с клавиатуры был потерян
		/// </summary>
		/// <param name="sender">Отправитель</param>
		/// <param name="e">Экземпляр <see cref="WindowEventArgs"/> с данными о событии</param>
		protected virtual void OnWindowFocusLost(object sender, WindowEventArgs e){}

		/// <summary>
		/// Событие вызывается перед закрытием формы. Если его не переопределять, событие закроет окно
		/// </summary>
		/// <param name="sender">Отправитель</param>
		/// <param name="e">Экземпляр <see cref="WindowEventArgs"/> с данными о событии</param>
		protected virtual void OnWindowClose(object sender, WindowEventArgs e)
		{
			_game.Exit();
		}
		#endregion
	}
}
