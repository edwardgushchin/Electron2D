using Electron2D;
using Electron2D.Graphics;
using Electron2D.Inputs;
using Electron2D.Resources;

namespace StartGame;

public class Player(string name, Texture player) : Node(name)
{
    private Sprite _playerSprite;

    protected override void Awake()
    {
        _playerSprite = new Sprite("PlayerSprite", player);
        _playerSprite.PixelsPerUnit = 50;
        
        AddChild(_playerSprite);
    }

    protected override void Update(float deltaTime)
    {
        const float moveSpeed = 2f; // в юнитах в секунду
        
        _playerSprite.Transform.LocalRotation += moveSpeed * deltaTime;
        
        //Console.WriteLine("LocalPosition: X={0}, Y={1}", LocalPosition.X, LocalPosition.Y);

        if (Input.GetKeyDown(Scancode.Left))
        {
            Transform.LocalPosition = new Vector2(Transform.LocalPosition.X - moveSpeed * deltaTime, Transform.LocalPosition.Y);
        }

        if (Input.GetKeyDown(Scancode.Right))
        {
            Transform.LocalPosition = new Vector2(Transform.LocalPosition.X + moveSpeed * deltaTime, Transform.LocalPosition.Y);
        }

        if (Input.GetKeyDown(Scancode.Up))
        {
            Transform.LocalPosition = new Vector2(Transform.LocalPosition.X, Transform.LocalPosition.Y + moveSpeed * deltaTime);
        }

        if (Input.GetKeyDown(Scancode.Down))
        {
            Transform.LocalPosition = new Vector2(Transform.LocalPosition.X, Transform.LocalPosition.Y - moveSpeed * deltaTime);
        }
    }

    public override void Destroy()
    {
        _playerSprite.Dispose();
    }
}