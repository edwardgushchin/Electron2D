/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using System;

using Electron2D.Graphics;

namespace Electron2D
{
	public class GameObject
	{
        public GameObject()
		{
			Transform = new Transform(new Point());
			Enable = true;
			Awake();
		}

		public GameObject(float x, float y)
		{
			Transform = new Transform(new Point(x, y));
			Enable = true;
			Awake();
		}

        public Transform Transform { get; private set; }

        public GameObject(bool enable)
        {
            this.Enable = enable;
            Awake();
        }
        public bool Enable { get; set; }

		//Метод вызывается, когда экземпляр объекта будет загружен
		protected virtual void Awake() {}

		//Update вызывается каждый кадр
		public virtual void Update() {}

		protected virtual void OnPostUpdate() {}

		protected virtual void OnPreUpdate() {}

		protected virtual void OnDestroy() {}

		protected virtual void OnDisable() {}

		protected virtual void OnEnable() {}

		protected virtual void OnMouseDown() {}

		protected virtual void OnMouseDrag() {}

		protected virtual void OnMouseEnter() {}

		protected virtual void OnMouseExit() {}

		protected virtual void OnMouseOver() {}

		protected virtual void OnMouseUp() {}

		//Передается когда входящий коллайдер контактирует с коллайдером данного объекта
		//protected virtual void OnCollisionEnter(Collision coll) {}

		//Передается, когда коллайдер другого объекта перестает соприкасаться с коллайдером этого объекта 
		//protected virtual void void OnCollisionExit(Collision coll)
	}
}
