/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/
using System;

using Electron2D.Graphics;

namespace Electron2D
{
	public class Entity : IDisposable
	{
		private bool _enabled;

		public Entity()
		{
			Transform = new Transform(new Point());
			_enabled = true;
		}

		public Entity(Point point)
		{
			Transform = new Transform(point);
			_enabled = true;
		}

        public Transform Transform { get; set; }

        public bool Enable
		{
			get => _enabled;
			set
			{
				if (value && !_enabled)
					OnEnable();
				else if (!value && _enabled)
					OnDisable();
				_enabled = value;
			}
		}

		public void Update()
		{
			if(_enabled)
			{
				OnPreUpdate();
				OnUpdate();
				OnPostUpdate();
			}
		}

		protected void Attach(Sprite sprite)
		{
		}

		protected void Attach(Sprite sprite, Point position)
		{

		}

		public void Dispose()
		{
			OnDestroy();
		}

		//Update вызывается каждый кадр
		protected virtual void OnUpdate() {}

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
		//public virtual void OnCollisionEnter(Collision coll) {}

		//Передается, когда коллайдер другого объекта перестает соприкасаться с коллайдером этого объекта 
		//public virtual void void OnCollisionExit(Collision coll)
	}
}
