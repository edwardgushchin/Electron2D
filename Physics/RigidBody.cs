/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using Electron2D.Graphics;
using Electron2D.Binding.Box2D.Dynamics;
using Electron2D.Binding.Box2D.Collision.Shapes;

namespace Electron2D.Physics
{
    public class RigidBody
    {
        private readonly World _world;
        private BodyDef _bodyDef;
        private readonly Body _body;

        public RigidBody(PhysicalWorld world, PhysicalBodyType type, Point position)
        {
            _world = world.Instance;

            _bodyDef = new BodyDef
            {
                Position = new System.Numerics.Vector2((float)position.X, (float)position.Y),
                BodyType = (BodyType)type
            };

            Transform = new Transform(position);

            Bullet = false;
            //BodyType = PhysicalBodyType.Dynamic;
            AllowSleep = true;
            GravityScale = 1;
            //LinearDamping = 1;
            //AngularDamping = 1;
            //LinearVelocity = new Vector();
            //AngularVelocity = 1;
            FixedRotation = false;
            //Awake = false;

            _body = _world.CreateBody(_bodyDef);

            PolygonShape dynamicBox = new PolygonShape();
            dynamicBox.SetAsBox(1.26f/2, 1.26f/2);

            FixtureDef fixtureDef = new FixtureDef
            {
                Shape = dynamicBox,
                Density = 540.0f,
                Friction = 0.3f,
                Restitution = 0.3f,
            };

            _body.CreateFixture(fixtureDef);
        }

        public PhysicalBodyType BodyType
        {
            get
            {
                if(_body == null) return (PhysicalBodyType)_bodyDef.BodyType;
                return (PhysicalBodyType)_body.BodyType;
            }
            set
            {
                if(_body == null) _bodyDef.BodyType = (BodyType)value;
                else _body.BodyType = (BodyType)value;
            }
        }

        public bool Bullet
        {
            get
            {
                if(_body == null) return _bodyDef.Bullet;
                return _body.IsBullet;
            }
            set
            {
                 if(_body == null) _bodyDef.Bullet = value;
                 else _body.IsBullet = value;
            }
        }

        public bool AllowSleep
        {
            get { return _bodyDef.AllowSleep; }
            set { _bodyDef.AllowSleep = value; }
        }

        public double GravityScale
        {
            get
            {
                if(_body == null) return _bodyDef.GravityScale;
                return _body.GravityScale;
            }
            set
            {
                if(_body == null) _bodyDef.GravityScale = (float)value;
                else _body.GravityScale = (float)value;
            }
        }

        public double LinearDamping
        {
            get { return _bodyDef.LinearDamping; }
            set { _bodyDef.LinearDamping = (float)value; }
        }

        public double AngularDamping
        {
            get { return _bodyDef.AngularDamping; }
            set { _bodyDef.LinearDamping = (float)value; }
        }

        public Vector LinearVelocity
        {
            get { return new Vector(_bodyDef.LinearVelocity.X, _bodyDef.LinearVelocity.Y); }
            set
            {
                 _bodyDef.LinearVelocity.X = (float)value.X;
                 _bodyDef.LinearVelocity.Y = (float)value.Y;
            }
        }

        public double AngularVelocity
        {
            get { return _bodyDef.AngularVelocity; }
            set { _bodyDef.AngularVelocity = (float)value; }
        }

        public bool FixedRotation
        {
            get { return _bodyDef.FixedRotation; }
            set { _bodyDef.FixedRotation = value; }
        }

        public bool Awake
        {
            get { return _bodyDef.Awake; }
            set { _bodyDef.Awake = value; }
        }

        public void Update()
        {
            var pos = _body.GetPosition();

            Transform.Position = new Point(pos.X, pos.Y);
            Transform.Degrees = _body.GetAngle();

           // Debug.Log(pos.ToString());
        }

        public Transform Transform
        {
            get
            {
                if (_body == null)
                {
                    return new Transform(new Point(_bodyDef.Position.X, _bodyDef.Position.Y))
                    {
                        Degrees = -_bodyDef.Angle * 180 / System.Math.PI
                    };
                }
                else
                {
                    return new Transform(new Point(_body.Transform.Position.X, _body.Transform.Position.Y))
                    {
                        Degrees = -_body.GetAngle() * 180 / System.Math.PI
                    };
                }
            }
            set
            {
                if (_body == null)
                {
                    _bodyDef.Position = new System.Numerics.Vector2((float)value.Position.X, (float)value.Position.Y);
                    _bodyDef.Angle = (float)value.Degrees;
                }
                else
                {
                    _body.SetTransform(new System.Numerics.Vector2((float)value.Position.X, (float)value.Position.Y), (float)value.Degrees);
                }
            }
        }
    }

    public enum PhysicalBodyType
    {
        Static = 0,

        Kinematic = 1,

        Dynamic = 2
    }
}