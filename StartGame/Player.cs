using System.Numerics;
using Electron2D;

namespace StartGame;

public class Player : Node
{
    private SpriteRenderer _sprite;
    private Rigidbody _body;

    protected override void Ready()
    {
        _sprite = AddComponent<SpriteRenderer>();
        _sprite.SetSprite("player_idle");

        _body = AddComponent<Rigidbody>();
        _body.Mass = 1f;
    }

    protected override void Process(float delta)
    {
        if (Input.IsKeyDown(Key.A)) _body.AddForce(new Vector2(-10, 0));
        if (Input.IsKeyDown(Key.D)) _body.AddForce(new Vector2(10, 0));
    }
}