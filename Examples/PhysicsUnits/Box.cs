using System.Numerics;
using Electron2D;

namespace PhysicsUnits;

public sealed class Box : Node
{
    private readonly Sprite _sprite;
    private Rigidbody _rigidbody = null!;
    private BoxCollider _collider = null!;
    private Vector2 _velocity;

    public Box(string name) : base(name)
    {
        var boxTexture = Resources.GetTexture("box.png");
        _sprite = new Sprite(boxTexture, 512);
    }

    public void SetVelocity(Vector2 velocity) => _velocity = velocity;

    protected override void Ready()
    {
        var renderer = AddComponent<SpriteRenderer>();
        renderer.SetSprite(_sprite);

        _rigidbody = AddComponent<Rigidbody>();
        _collider = AddComponent<BoxCollider>();
        _collider.Size = GetSpriteSize(_sprite);
    }

    protected override void PhysicsProcess(float fixedDelta)
    {
        if (_velocity == Vector2.Zero)
            return;

        Transform.Translate(_velocity * fixedDelta);
    }

    private static Vector2 GetSpriteSize(Sprite sprite)
    {
        var texture = sprite.Texture;
        var rect = sprite.TextureRect;
        var width = rect.Width > 0 ? rect.Width : texture.Width;
        var height = rect.Height > 0 ? rect.Height : texture.Height;
        return new Vector2(width / sprite.PixelsPerUnit, height / sprite.PixelsPerUnit);
    }
}