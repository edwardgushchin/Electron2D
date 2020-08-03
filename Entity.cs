/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using Electron2D.Graphics;

namespace Electron2D
{
	public class Entity
	{
		public Entity()
		{
			Transform = new Transform(new Point());
			Enable = true;
		}

		public Entity(Entity parrent)
		{
			Transform.LocalScale = new Vector(
				Transform.LocalScale.X + parrent.Transform.LocalScale.X,
				Transform.LocalScale.Y + parrent.Transform.LocalScale.Y
			);

			Transform.Position += parrent.Transform.Position;
			Transform.Degrees += parrent.Transform.Degrees;

			Parrent = parrent;

			Enable = true;
		}


        public Transform Transform { get; }

		private Transform AbsoluteTransform
		{
			get
			{
                if (Parrent != null)
                {
                    return new Transform
                    {
                        Position = Transform.Position + Parrent.Transform.Position,
                        Degrees = Transform.Degrees + Parrent.Transform.Degrees,
                        LocalScale = new Vector(
                            Transform.LocalScale.X * Parrent.Transform.LocalScale.X,
                            Transform.LocalScale.Y * Parrent.Transform.LocalScale.Y
                        )
                    };
                }
                else
                {
                    return Transform;
                }
            }
		}

		public void DrawSprite(Sprite sprite)
		{
			sprite.Draw(AbsoluteTransform);
		}

		public void DrawSprite(Sprite sprite, Transform transform)
		{
			var trans = AbsoluteTransform;
			sprite.Draw(new Transform {
				Position = trans.Position + transform.Position,
				Degrees = trans.Degrees + transform.Degrees,
				LocalScale = new Vector(
                    trans.LocalScale.X * transform.LocalScale.X,
                    trans.LocalScale.Y * transform.LocalScale.Y
                )
			});
		}

        public Entity Parrent { get; }

        public bool Enable { get; set; }

		public void Update()
		{
			OnPreUpdate();
			OnUpdate();
			OnPostUpdate();
		}

		//Update вызывается каждый кадр
		protected virtual void OnUpdate() {}

		public virtual void OnPostUpdate() {}

		public virtual void OnPreUpdate() {}

		public virtual void OnDestroy() {}

		public virtual void OnDisable() {}

		public virtual void OnEnable() {}

		public virtual void OnMouseDown() {}

		public virtual void OnMouseDrag() {}

		public virtual void OnMouseEnter() {}

		public virtual void OnMouseExit() {}

		public virtual void OnMouseOver() {}

		public virtual void OnMouseUp() {}

		//Передается когда входящий коллайдер контактирует с коллайдером данного объекта
		//public virtual void OnCollisionEnter(Collision coll) {}

		//Передается, когда коллайдер другого объекта перестает соприкасаться с коллайдером этого объекта 
		//public virtual void void OnCollisionExit(Collision coll)
	}
}
