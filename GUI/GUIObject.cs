/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using System;

using Electron2D.Graphics;

namespace Electron2D.GUI
{
    public class GUIObject : IDisposable
	{
		RectTransform rectTransform;
		ScaleMode scaleMode;
        
        public GUIObject()
        {
            rectTransform = new RectTransform(0, 0, 0, 0);
			Enable = true;
			Awake();        
		}

		public GUIObject(bool enable) 
        {
            rectTransform = new RectTransform(0, 0, 0, 0);
			this.Enable = enable;
            Awake();
        }

        public bool Enable { get; set; }

        public RectTransform RectTransform
		{
			get { return rectTransform; }
		}

		public ScaleMode UIScaleMode { get; set; }

		protected virtual void Awake() {}

        public virtual void Update(double deltaTime) {}
		
		protected virtual void OnPostUpdate(double deltaTime) {}
		
		protected virtual void OnPreUpdate(double deltaTime) {}
		
		protected virtual void OnDestroy() {}
		
		protected virtual void OnDisable() {}
		
		protected virtual void OnEnable() {}
		
		protected virtual void OnMouseDown() {}
		
		protected virtual void OnMouseDrag() {}
		
		protected virtual void OnMouseEnter() {}
		
		protected virtual void OnMouseExit() {}
		
		protected virtual void OnMouseOver() {}
		
		protected virtual void OnMouseUp() {}

        public void Dispose()
		{
			rectTransform = null;
			Enable = false;
			OnDestroy();
		}
    }
}