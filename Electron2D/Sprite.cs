namespace Electron2D;

public sealed class Sprite(Texture texture) : Component
{
    private Vector2 _size = new (100, 100);
    private float _scale = 1;
    
    private Vector3 _offset = Vector3.Zero;
    
    public Vector3 Position => Owner?.Transform.Position + Offset ?? Vector3.Zero;

    public Vector3 Offset
    {
        get => _offset;
        set
        {
            if (Math.Abs(_offset.Z - value.Z) > float.Epsilon)
            {
                SpriteRenderer.MarkForSorting();
            }
            _offset = value;
        }
    }

    protected internal override void Awake()
    {
        SpriteRenderer.RegisterSprite(this);
    }

    protected internal override void Start() { }

    protected internal override void Update(float deltaTime) { }

    internal void Draw(IRenderContext context)
    {
        context.RenderTexture(texture, _size * _scale, Position);
    }

    protected internal override void OnDestroy()
    {
        SpriteRenderer.UnregisterSprite(this);
        texture.Destroy();
    }
}