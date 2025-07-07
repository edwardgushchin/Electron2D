using Electron2D.Components;
using Electron2D.Resources;
using SDL3;

namespace Electron2D.Graphics;

public class Sprite : Node, IDisposable
{
    private Texture _texture;
    
    private bool _disposed;
    
    private int _layer;
    
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
    
    /// <summary>
    /// Границы спрайта в мировых координатах.
    /// </summary>
    public Bounds WorldBounds
    {
        get
        {
            // Центр спрайта в мире
            Vector2 center = Transform.GlobalPosition;

            // Размеры спрайта в юнитах
            float width = (SourceRect.Width / PixelsPerUnit) * Transform.GlobalScale.X;
            float height = (SourceRect.Height / PixelsPerUnit) * Transform.GlobalScale.Y;

            return new Bounds(center, new Vector2(width, height));
        }
    }

    
    public void Dispose()
    {
        if (_disposed) return;

        _texture.RemoveReference();
        _disposed = true;
    }
}