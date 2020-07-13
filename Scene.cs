/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using System;

using Electron2D.GUI;
using Electron2D.Events;
using Electron2D.Kernel;
using Electron2D.Graphics;
using Electron2D.Binding.SDL;

using System.Collections.Generic;

namespace Electron2D
{
	public class Scene : IDisposable
	{
		private Game game;
		private Color clearColor;

		// Хватит везде пихать лист, это не самое быстрое решение для кэшей
		//List<GameObject> objectsList;
		//List<GUIObject> guiObjectsList, debugGuiObjectsList;

		private DebugInfo debugInfo;

		public Scene()
		{
			this.Index = 0;
			clearColor = Color.Black;
			//objectsList = new List<GameObject>();
			//guiObjectsList = new List<GUIObject>();
			if(Settings.DebugInfo) LoadDebugInfo();
		}

		public Scene(int Index)
		{
			this.Index = Index;
			clearColor = Color.Black;
			//objectsList = new List<GameObject>();
			//guiObjectsList = new List<GUIObject>();
			if(Settings.DebugInfo) LoadDebugInfo();
		}

		public Scene(int Index, Color sceneColor)
		{
			this.Index = Index;
			clearColor = sceneColor;
			//objectsList = new List<GameObject>();
			//guiObjectsList = new List<GUIObject>();
			if(Settings.DebugInfo) LoadDebugInfo();
		}

		private void LoadDebugInfo()
		{
			//debugGuiObjectsList = new List<GUIObject>();
			if (Kernel.Settings.DebugMode) {
				debugInfo = new DebugInfo();
				//debugGuiObjectsList.AddRange(debugInfo.ObjectsList);
			}
		}

		public void SetCursor(Sprite cursor)
		{
			SDL.SDL_SetCursor(SDL.SDL_CreateColorCursor(Image.IMG_Load(cursor.Path), 0, 0));
		}

		public int GUIObjectsCount
		{
			get
			{
				//return guiObjectsList.Count; 
				return 0;
			}
		}

		public int GameObjectsCount
		{
			get
			{
				//return objectsList.Count; 
				return 0;
			}
		}

		public Color ClearColor
		{
			get { return clearColor; }
			set
			{
				SDL.SDL_SetRenderDrawColor(Game.RenderContext, value.R, value.G, value.B, value.A);
				clearColor = value;
			}
		}

		public int Index { get; internal set; }

		private void SubscribeEvents()
		{
			#region Subscribe to Window event
			game.Events.OnWindowShowEvent += OnWindowShow;
			game.Events.OnWindowHiddenEvent += OnWindowHidden;
			game.Events.OnWindowExposedEvent += OnWindowExposed;
			game.Events.OnWindowMovedEvent += OnWindowMoved;
			game.Events.OnWindowResizedEvent += OnWindowResized;
			game.Events.OnWindowSizeChangedEvent += OnWindowSizeChanged;
			game.Events.OnWindowMinimizedEvent += OnWindowMinimized;
			game.Events.OnWindowMaximizedEvent += OnWindowMaximized;
			game.Events.OnWindowRestoredEvent += OnWindowRestored;
			game.Events.OnWindowEnterEvent += OnWindowEnter;
			game.Events.OnWindowLeaveEvent += OnWindowLeave;
			game.Events.OnWindowFocusGainedEvent += OnWindowFocusGained;
			game.Events.OnWindowFocusLostEvent += OnWindowFocusLost;
			game.Events.OnWindowCloseEvent += OnWindowClose;
			#endregion

			#region Subscribe to Render event
			game.Render.OnPreUpdate += PreUpdate;
			game.Render.OnUpdate += Pepiline;
			game.Render.OnPostUpdate += PostUpdate;
			#endregion

			#region Subscribe to Keyboard events
			game.Events.OnKeyDownEvent += OnKeyDown;
			game.Events.OnKeyUpEvent += OnKeyUp;
			#endregion

			#region Subscribe to Mouse events
			game.Events.OnMouseButtonDownEvent += OnMouseButtonDown;
			game.Events.OnMouseButtonUpEvent += OnMouseButtonUp;
			game.Events.OnMouseMotionEvent += OnMouseMotion;
			game.Events.OnMouseWheelEvent += OnMouseWheel;
			#endregion
		}

		private void DescribeEvents()
		{
			#region Subscribe to Window event
			game.Events.OnWindowShowEvent -= OnWindowShow;
			game.Events.OnWindowHiddenEvent -= OnWindowHidden;
			game.Events.OnWindowExposedEvent -= OnWindowExposed;
			game.Events.OnWindowMovedEvent -= OnWindowMoved;
			game.Events.OnWindowResizedEvent -= OnWindowResized;
			game.Events.OnWindowSizeChangedEvent -= OnWindowSizeChanged;
			game.Events.OnWindowMinimizedEvent -= OnWindowMinimized;
			game.Events.OnWindowMaximizedEvent -= OnWindowMaximized;
			game.Events.OnWindowRestoredEvent -= OnWindowRestored;
			game.Events.OnWindowEnterEvent -= OnWindowEnter;
			game.Events.OnWindowLeaveEvent -= OnWindowLeave;
			game.Events.OnWindowFocusGainedEvent -= OnWindowFocusGained;
			game.Events.OnWindowFocusLostEvent -= OnWindowFocusLost;
			game.Events.OnWindowCloseEvent -= OnWindowClose;
			#endregion

			#region Subscribe to Render event
			game.Render.OnPreUpdate -= PreUpdate;
			game.Render.OnUpdate -= Pepiline;
			game.Render.OnPostUpdate -= PostUpdate;
			#endregion

			#region Subscribe to Keyboard events
			game.Events.OnKeyDownEvent -= OnKeyDown;
			game.Events.OnKeyUpEvent -= OnKeyUp;
			#endregion

			#region Subscribe to Mouse events
			game.Events.OnMouseButtonDownEvent -= OnMouseButtonDown;
			game.Events.OnMouseButtonUpEvent -= OnMouseButtonUp;
			game.Events.OnMouseMotionEvent -= OnMouseMotion;
			game.Events.OnMouseWheelEvent -= OnMouseWheel;
			#endregion
		}

		private void Pepiline()
		{
			//Clear();
			Update();
			//UpdateObjects(deltaTime);
			//UpdateGUIObjects(deltaTime);
			//if(Settings.DebugInfo) UpdateGUIDebugInfoObjects(deltaTime);
		}

		/*private void UpdateGUIDebugInfoObjects(double deltaTime)
		{
			//if(debugInfo != null) 
			//{
				//debugInfo.Update(deltaTime, guiObjectsList.Count, objectsList.Count, Kernel.ResourceManager.FontCacheCount);
				//debugInfo.Update(deltaTime, 0, 0, Kernel.ResourceManager.FontCacheCount);
				//debugGuiObjectsList.ForEach(delegate(GUIObject obj) {
				//	obj.Update(deltaTime);
				//});
			//}
		}

		private void UpdateObjects(double deltaTime)
		{
			//objectsList.ForEach(delegate(GameObject obj) {
				//if (obj.Enable) 
				//obj.Update(deltaTime);
			//});
		}

		private void UpdateGUIObjects(double deltaTime)
		{
			//guiObjectsList.ForEach(delegate(GUIObject obj) {
				//if (obj.Enable) 
				//obj.Update(deltaTime);
			//});
		}*/
		private void Clear()
		{
			SDL.SDL_SetRenderDrawColor(game.Render.RenderContext, clearColor.R, clearColor.G, clearColor.B, clearColor.A);
			SDL.SDL_RenderClear(game.Render.RenderContext);
		}

		public void Start()
		{
			SubscribeEvents();
			OnLoadScene();
		}

		public void AddObject(GameObject obj)
		{
			//objectsList.Add(obj);
		}

		public void AddGUIObject(GUIObject gobj)
		{
			//guiObjectsList.Add(gobj);
		}

		public void Dispose()
		{
			DescribeEvents();
		}

		internal void SetGame(Game gameContext)
		{
			game = gameContext;
		}

		internal void Stop()
		{
			Dispose();
		}

		public string Name
		{
			get { return GetType().Name; }
		}

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
		/// <param name="e">Экземпляр <see cref="Electron2D.Events.WindowEventArgs"/> с данными о событии</param>
		protected virtual void OnWindowExposed(object sender, WindowEventArgs e){}

		/// <summary>
		/// Событие вызывается после окончания перемещения формы
		/// </summary>
		/// <param name="sender">Отправитель</param>
		/// <param name="e">Экземпляр <see cref="Electron2D.Events.WindowEventArgs"/> с данными о событии</param>
		protected virtual void OnWindowMoved(object sender, WindowEventArgs e){}

		/// <summary>
		/// Событие вызывается после изменение размера формы.
		/// Этому событию всегда предшествует <see cref="Electron2D.Game.OnWindowSizeChanged"/>
		/// </summary>
		/// <param name="sender">Отправитель</param>
		/// <param name="e">Экземпляр <see cref="Electron2D.Events.WindowEventArgs"/> с данными о событии</param>
		protected virtual void OnWindowResized(object sender, WindowEventArgs e){}

		/// <summary>
		/// Размер окна изменился либо в результате вызова API, либо через систему или пользователя,
		/// изменившего размер окна; за этим событием следует <see cref="Electron2D.Game.OnWindowResized"/>,
		/// если размер был изменен внешним событием, т. е. пользователем или оконным менеджером
		/// </summary>
		/// <param name="sender">Отправитель</param>
		/// <param name="e">Экземпляр <see cref="Electron2D.Events.WindowEventArgs"/> с данными о событии</param>
		protected virtual void OnWindowSizeChanged(object sender, WindowEventArgs e){}

		/// <summary>
		/// Событие вызывается после сворачивания формы
		/// </summary>
		/// <param name="sender">Отправитель</param>
		/// <param name="e">Экземпляр <see cref="Electron2D.Events.WindowEventArgs"/> с данными о событии</param>
		protected virtual void OnWindowMinimized(object sender, WindowEventArgs e){}

		/// <summary>
		/// Событие вызывается после разворачивания формы на полный экран
		/// </summary>
		/// <param name="sender">Отправитель</param>
		/// <param name="e">Экземпляр <see cref="Electron2D.Events.WindowEventArgs"/> с данными о событии</param>
		protected virtual void OnWindowMaximized(object sender, WindowEventArgs e){}

		/// <summary>
		/// Событие вызывается когда окно было восстановлено до нормального размера и положения
		/// </summary>
		/// <param name="sender">Отправитель</param>
		/// <param name="e">Экземпляр <see cref="Electron2D.Events.WindowEventArgs"/> с данными о событии</param>
		protected virtual void OnWindowRestored(object sender, WindowEventArgs e){}

		/// <summary>
		/// Событие вызывается когда на окно был наведен фокус мыши
		/// </summary>
		/// <param name="sender">Отправитель</param>
		/// <param name="e">Экземпляр <see cref="Electron2D.Events.WindowEventArgs"/> с данными о событии</param>
		protected virtual void OnWindowEnter(object sender, WindowEventArgs e){}

		/// <summary>
		/// Событие вызывается когда фокус мыши с окна был потерян
		/// </summary>
		/// <param name="sender">Отправитель</param>
		/// <param name="e">Экземпляр <see cref="Electron2D.Events.WindowEventArgs"/> с данными о событии</param>
		protected virtual void OnWindowLeave(object sender, WindowEventArgs e){}

		/// <summary>
		/// Событие вызывается когда окно получило фокус с клавиатуры
		/// </summary>
		/// <param name="sender">Отправитель</param>
		/// <param name="e">Экземпляр <see cref="Electron2D.Events.WindowEventArgs"/> с данными о событии</param>
		protected virtual void OnWindowFocusGained(object sender, WindowEventArgs e){}

		/// <summary>
		/// Событие вызывается когда фокус с клавиатуры был потерян
		/// </summary>
		/// <param name="sender">Отправитель</param>
		/// <param name="e">Экземпляр <see cref="Electron2D.Events.WindowEventArgs"/> с данными о событии</param>
		protected virtual void OnWindowFocusLost(object sender, WindowEventArgs e){}

		/// <summary>
		/// Событие вызывается перед закрытием формы. Если его не переопределять, событие закроет окно
		/// </summary>
		/// <param name="sender">Отправитель</param>
		/// <param name="e">Экземпляр <see cref="Electron2D.Events.WindowEventArgs"/> с данными о событии</param>
		protected virtual void OnWindowClose(object sender, WindowEventArgs e)
		{
			game.Exit();
		}
		#endregion
	}
}
