namespace Electron2D.Physics;

public class BoxCollider : Collider
{
    private Vector2 _size;
    private Vector2 _offset;

    // Use stackalloc for temporary vertices when possible
    private readonly Vector2[] _cachedVertices = new Vector2[4];

    public BoxCollider(string name, Vector2 size, Vector2 offset = default) : base(name)
    {
        _size = size;
        _offset = offset;
    }

    public Vector2 Size
    {
        get => _size;
        set => _size = value;
    }

    public Vector2 Offset
    {
        get => _offset;
        set => _offset = value;
    }
    
    public bool IsTrigger { get; set; }
    
    public override Bounds Bounds => GetWorldBounds();

    public override Vector2[] GetWorldVertices()
    {
        GetWorldVertices(_cachedVertices.AsSpan());
        return _cachedVertices;
    }
    
    public override bool Contains(Vector2 worldPoint)
    {
        GetWorldVertices(_cachedVertices);
        return IsPointInRotatedRectangle(worldPoint, _cachedVertices);
    }
    
    private Bounds GetWorldBounds()
    {
        GetWorldVertices(_cachedVertices);
        
        var min = new Vector2(float.MaxValue, float.MaxValue);
        var max = new Vector2(float.MinValue, float.MinValue);
        
        for (var i = 0; i < 4; i++)
        {
            var vertex = _cachedVertices[i];
            min.X = MathF.Min(min.X, vertex.X);
            min.Y = MathF.Min(min.Y, vertex.Y);
            max.X = MathF.Max(max.X, vertex.X);
            max.Y = MathF.Max(max.Y, vertex.Y);
        }
        
        var center = (min + max) * 0.5f;
        var size = max - min;
        return new Bounds(center, size);
    }
    
    public override bool Intersects(Collider other)
    {
        return other switch
        {
            BoxCollider box => IntersectsBox(box),
            _ => false
        };
    }
    
    private void GetWorldVertices(Span<Vector2> outputVertices)
    {
        var worldPos = Transform.GlobalPosition;
        var worldRot = Transform.GlobalRotation;
        var worldScale = Transform.GlobalScale;

        var scaledSize = new Vector2(_size.X * worldScale.X, _size.Y * worldScale.Y);
        var scaledOffset = new Vector2(_offset.X * worldScale.X, _offset.Y * worldScale.Y);
        var halfSize = scaledSize * 0.5f;

        // Local vertices (could be stackalloc for even better performance)
        Span<Vector2> localVertices =
        [
            new(-halfSize.X, -halfSize.Y), // Bottom-left
            new(+halfSize.X, -halfSize.Y), // Bottom-right  
            new(+halfSize.X, +halfSize.Y), // Top-right
            new(-halfSize.X, +halfSize.Y) // Top-left
        ];

        // Transform vertices
        for (var i = 0; i < 4; i++)
        {
            var rotated = Vector2.RotatePoint(localVertices[i], -worldRot);
            var offsetRotated = Vector2.RotatePoint(scaledOffset, -worldRot);
            outputVertices[i] = worldPos + rotated + offsetRotated;
        }
    }
    
    private bool IntersectsBox(BoxCollider other)
    {
        GetWorldVertices(_cachedVertices);
        var otherVertices = other.GetWorldVertices();
        return SATIntersection(_cachedVertices, otherVertices);
    }
    
    private static bool IsPointInRotatedRectangle(Vector2 point, Vector2[] vertices)
    {
        var intersections = 0;
        for (var i = 0; i < vertices.Length; i++)
        {
            var v1 = vertices[i];
            var v2 = vertices[(i + 1) % vertices.Length];
            
            if (((v1.Y > point.Y) != (v2.Y > point.Y)) &&
                (point.X < (v2.X - v1.X) * (point.Y - v1.Y) / (v2.Y - v1.Y) + v1.X))
            {
                intersections++;
            }
        }
        return intersections % 2 == 1;
    }
    
    private static bool SATIntersection(Vector2[] rect1, Vector2[] rect2)
    {
        Span<Vector2> axes = stackalloc Vector2[8];
        var axisCount = 0;

        for (var i = 0; i < rect1.Length; i++)
        {
            var edge = rect1[(i + 1) % rect1.Length] - rect1[i];
            var normal = new Vector2(-edge.Y, edge.X);
            axes[axisCount++] = Vector2.Normalize(normal);
        }

        for (var i = 0; i < rect2.Length; i++)
        {
            var edge = rect2[(i + 1) % rect2.Length] - rect2[i];
            var normal = new Vector2(-edge.Y, edge.X);
            axes[axisCount++] = Vector2.Normalize(normal);
        }

        for (var i = 0; i < axisCount; i++)
        {
            var axis = axes[i];
            var proj1 = ProjectOntoAxis(rect1, axis);
            var proj2 = ProjectOntoAxis(rect2, axis);

            if (proj1.max < proj2.min || proj2.max < proj1.min)
                return false;
        }

        return true;
    }
    
    private static (float min, float max) ProjectOntoAxis(Vector2[] vertices, Vector2 axis)
    {
        var min = Vector2.Dot(vertices[0], axis);
        var max = min;
        
        for (var i = 1; i < vertices.Length; i++)
        {
            var projection = Vector2.Dot(vertices[i], axis);
            min = MathF.Min(min, projection);
            max = MathF.Max(max, projection);
        }
        
        return (min, max);
    }
}