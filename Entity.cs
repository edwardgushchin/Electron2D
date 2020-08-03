/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using Electron2D.Graphics;

namespace Electron2D
{
	public class Entity
	{
        //private readonly List<Sprite> spriteList;

		public Entity()
		{
			Transform = new Transform(new Point());
			Enable = true;
			//spriteList = new List<Sprite>();
			Awake();

			//SceneManager.GetCurrentScene.
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
			//spriteList = new List<Sprite>();
			Awake();
		}

		/*public Entity(float x, float y)
		{
			Transform = new Transform(new Point(x, y));
			Enable = true;
			spriteList = new List<Sprite>();
			Awake();
		}*/

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

        /*public Entity(bool enable)
        {
            this.Enable = enable;
			spriteList = new List<Sprite>();
            Awake();
        }*/

		/*public void AddComponent(Sprite sprite)
		{
			spriteList.Add(sprite);
		}*/

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

		//Метод вызывается, когда экземпляр объекта будет загружен
		public virtual void Awake() {}

		//Update вызывается каждый кадр
		public virtual void Update()
		{
			/*if (Parrent != null)
            {
                spriteList.ForEach((Sprite sprite) =>
                {
                    sprite.Draw(new Transform
                    {
                        Position = Transform.Position + Parrent.Transform.Position,
                        Degrees = Transform.Degrees + Parrent.Transform.Degrees,
                        LocalScale = new Vector(
                            Transform.LocalScale.X * Parrent.Transform.LocalScale.X,
                            Transform.LocalScale.Y * Parrent.Transform.LocalScale.Y
                        )
                    });
                });
            }
            else
            {
                spriteList.ForEach((Sprite sprite) => sprite.Draw(Transform));
            }*/
        }

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
