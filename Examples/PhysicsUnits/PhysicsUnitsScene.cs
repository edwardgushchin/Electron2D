using System;
using System.Numerics;
using Electron2D;

namespace PhysicsUnits;

internal sealed class PhysicsUnitsScene : Node
{
    public PhysicsUnitsScene() : base("PhysicsUnitsScene")
    {
        var box = new Box("Box")
        {
            Transform = { WorldPosition = new Vector2(0f, 10f) }
        };
        var floor = new Floor("Floor")
        {
            Transform = { WorldPosition = new Vector2(0f, -4.5f) }
        };

        AddChild(floor);
        AddChild(box);
    }

    protected override void Process(float delta)
    {
        Console.WriteLine(Input.MousePosition);
    }
}
