using Electron2D.Resources;

namespace Electron2D.Graphics;

public class Sprite : Node, IDisposable
{
    private readonly Texture _texture;
    
    private bool _disposed;
    
    private int _layer;
    
    private Color      _color      = Color.White;
    private BlendMode  _blendMode  = BlendMode.Blend;
    private ScaleMode  _scaleMode  = ScaleMode.Linear;
    
    public Sprite(string name, Texture texture) : base(name)
    {
        _texture = texture;
        
        SourceRect = new Rect { X = 0, Y = 0, Width = texture.Width, Height = texture.Height };
        Center = new Vector2 { X = 0.5f, Y = 0.5f };
    }
    
    protected override void Awake()
    {
        _texture.AddReference();
    }
    
    internal Texture Texture => _texture;
    
    public Rect SourceRect { get; set; }
    
    public Vector2 Center { get; set; }

    public FlipMode Flip { get; set; } = FlipMode.None;
    
    internal bool LayerDirty { get; set; }
    
    /// <summary>
    /// Сколько пикселей в одном юните координат.
    /// Например, если 100, то спрайт шириной 200 пикселей будет занимать 2 юнита.
    /// </summary>
    public float PixelsPerUnit { get; set; } = 100f;
    
    public bool ShowDebugRect { get; set; } = false;
    
    public int Layer
    {
        get => _layer;
        set
        {
            if (_layer == value) return;
            
            _layer = value;
            LayerDirty = true;
        }
    }
    
    public Color Color
    {
        get => _color;
        set
        {
            if (_color.Equals(value)) return;
            _color = value;
            // Влияет на все спрайты, делящие текстуру ─ документируйте это!
            _texture.SetColorMod(_color.R, _color.G, _color.B);
            _texture.SetAlphaMod(_color.A);
        }
    }
    
    public BlendMode BlendMode
    {
        get => _blendMode;
        set
        {
            if (_blendMode == value) return;
            _blendMode = value;
            _texture.BlendMode = value;
        }
    }
    
    public ScaleMode ScaleMode
    {
        get => _scaleMode;
        set
        {
            if (_scaleMode == value) return;
            _scaleMode = value;
            _texture.ScaleMode = value;
        }
    }
    
    /// <summary>
    /// Границы спрайта в мировых координатах.
    /// </summary>
    public Bounds WorldBounds
    {
        get
        {
            // Центр спрайта в мире
            var width  = SourceRect.Width  / PixelsPerUnit * MathF.Abs(Transform.GlobalScale.X);
            var height = SourceRect.Height / PixelsPerUnit * MathF.Abs(Transform.GlobalScale.Y);

            // Смещение от pivot-а до геометрического центра AABB
            var pivotOffset = new Vector2(0.5f - Center.X, 0.5f - Center.Y);
            var centerWorld = Transform.GlobalPosition + new Vector2(width  * pivotOffset.X, height * pivotOffset.Y);

            return new Bounds(centerWorld, new Vector2(width, height));
        }
    }

    
    public void Dispose()
    {
        if (_disposed) return;

        _texture.RemoveReference();
        _disposed = true;
    }
}