using System.Numerics;
using Electron2D;

namespace StartGame;

public class Player() : Node("Player")
{
    private SpriteRenderer _sprite;
    private Rigidbody _body;
    private float _speed = 5f;

    protected override void Ready()
    {
        _sprite = AddComponent<SpriteRenderer>();
        _sprite.SetSprite("player_idle.png", pixelsPerUnit: 100f);

        _body = AddComponent<Rigidbody>();
        _body.Mass = 1f;
    }

    protected override void Process(float delta)
    {
        //if (Input.IsKeyDown(Key.A)) _body.AddForce(new Vector2(-10, 0));
        //if (Input.IsKeyDown(Key.D)) _body.AddForce(new Vector2(10, 0));
        if(Input.IsKeyDown(Key.A)) Transform.Translate(x: -_speed * delta);
        if(Input.IsKeyDown(Key.D)) Transform.Translate(x: _speed * delta);
        if(Input.IsKeyDown(Key.W)) Transform.Translate(y: _speed * delta);
        if(Input.IsKeyDown(Key.S)) Transform.Translate(y: -_speed * delta);
    }
}