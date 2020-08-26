/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

/*using Electron2D.Graphics;
using Electron2D.Binding.Box2D.Dynamics;

namespace Electron2D.Physics
{
    public class PhysicalWorld
    {
        //internal World Instance { get; }

        public PhysicalWorld()
        {
            //Instance = new World(new System.Numerics.Vector2(0, -9.81f));
            VelocityIterations = 8;
            PositionIterations = 3;
            Debug.Log("A physical world with a default gravity vector is created.", Debug.Sender.Physics);
        }

        public PhysicalWorld(Vector gravity)
        {
            Instance = new World(new System.Numerics.Vector2((float)gravity.X, (float)gravity.Y));
            VelocityIterations = 8;
            PositionIterations = 3;
            Debug.Log($"The physical world with the gravity vector ({gravity.X}; {gravity.Y}) is created.", Debug.Sender.Physics);
        }

        public void Update()
        {
            Instance.Step((float)Time.DeltaTime, VelocityIterations, PositionIterations);
        }

        public int VelocityIterations { get; set; }

        public int PositionIterations { get; set; }
    }
}*/