namespace Electron2D;

public class Transform
{
    /// <summary>
    /// Local position relative to the parent.
    /// </summary>
    public Vector3 LocalPosition { get; set; } = new (0, 0, 0);

    /// <summary>
    /// Local rotation in degrees.
    /// </summary>
    public float LocalRotation { get; set; } = 0f;

    /// <summary>
    /// Local scale. (1,1) means no scaling.
    /// </summary>
    public Vector3 LocalScale { get; set; } = new (1, 1, 0);

    /// <summary>
    /// Parent transform. If null, the transform is considered root.
    /// </summary>
    public Transform? Parent { get; private set; }

    /// <summary>
    /// List of child transforms.
    /// </summary>
    public List<Transform> Children { get; } = [];

    /// <summary>
    /// Global (world) position calculated from the parent's transform.
    /// </summary>
    public Vector3 Position
    {
        get
        {
            if (Parent == null) return LocalPosition;
            
            // Scale local position by parent's scale.
            var scaled = new Vector3(
                LocalPosition.X * Parent.Scale.X,
                LocalPosition.Y * Parent.Scale.Y,
                LocalPosition.Z * Parent.Scale.Z
            );

            // Rotate the scaled vector by parent's rotation.
            var rotated = Rotate(scaled, Parent.Rotation);
            return Parent.Position + rotated;
        }
        set
        {
            if (Parent == null) LocalPosition = value;
            else
            {
                // Обратное вычисление LocalPosition из глобальной позиции
                var delta = value - Parent.Position;
                var inverseRotated = Rotate(delta, -Parent.Rotation);
                LocalPosition = new Vector3(
                    inverseRotated.X / Parent.Scale.X,
                    inverseRotated.Y / Parent.Scale.Y,
                    inverseRotated.Z / Parent.Scale.Z
                );
            }
        }
    }

    /// <summary>
    /// Global (world) rotation in degrees.
    /// </summary>
    public float Rotation => Parent?.Rotation + LocalRotation ?? LocalRotation;

    /// <summary>
    /// Global scale.
    /// </summary>
    public Vector3 Scale => Parent == null 
        ? LocalScale 
        : new Vector3(Parent.Scale.X * LocalScale.X, Parent.Scale.Y * LocalScale.Y, Parent.Scale.Z * LocalScale.Z);

    /// <summary>
    /// Sets the parent transform for this transform.
    /// </summary>
    /// <param name="parent">Parent transform.</param>
    public void SetParent(Transform parent)
    {
        Parent?.Children.Remove(this);
        Parent = parent;
        parent.Children.Add(this);
    }

    /// <summary>
    /// Helper method to rotate a vector by the given angle (in degrees).
    /// </summary>
    private static Vector3 Rotate(Vector3 vector, float degrees)
    {
        var radians = degrees * MathF.PI / 180f;
        var cos = MathF.Cos(radians);
        var sin = MathF.Sin(radians);
        return new Vector3(
            vector.X * cos - vector.Y * sin,
            vector.X * sin + vector.Y * cos,
            vector.X * cos + vector.Y * sin
        );
    }
}