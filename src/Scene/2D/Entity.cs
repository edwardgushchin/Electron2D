namespace Electron2D;

/// <summary>A spatial canvas node with position, rotation, scale and skew.</summary>
/// <remarks>Use this node as an empty spatial parent or derive a drawable game object from it. Transform setters
/// commit immediately, including equal assignments. Enabled local notifications are synchronous while attached;
/// global notifications coalesce until scene delivery or ForceUpdateTransform.</remarks>
public class Entity : CanvasItem
{
    /// <summary>Creates a detached spatial node with an identity transform.</summary>
    public Entity() { }

    /// <inheritdoc />
    public override Transform GetTransform() => Transform;

    /// <inheritdoc />
    public override void Reparent(Node newParent, bool keepGlobalTransform = true)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(newParent);
        if (ReferenceEquals(Parent, newParent)) { base.Reparent(newParent, keepGlobalTransform); return; }
        // Validate the inverse before any hierarchy mutation.
        var local = keepGlobalTransform ? ToLocalTransform(GlobalTransform, newParent, TopLevel) : Transform;
        List<Exception>? errors = null;
        try { base.Reparent(newParent, keepGlobalTransform); }
        catch (AggregateException error) { CollectException(ref errors, error); }
        if (!IsDisposed && ReferenceEquals(Parent, newParent) && keepGlobalTransform)
        {
            try { Transform = local; }
            catch (Exception error) { CollectException(ref errors, error); }
        }
        ThrowCollected("Reparent callbacks failed.", errors);
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(NodeProperties);

    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(Entity)
        ? CreateDefaultNode : base.CreateSceneInstanceFactory();

    private static Node CreateDefaultNode() => new Entity();

    private static readonly PropertyDescriptor[] NodeProperties =
    [
        new PropertyDescriptor<Entity, Vector2>(
            nameof(Position),
            node => node.Position,
            (node, value) => node.Position = value,
            _ => Vector2.Zero,
            (_, value) => IsFinite(value),
            stored: true),
        new PropertyDescriptor<Entity, float>(
            nameof(RotationDegrees),
            node => node.RotationDegrees,
            (node, value) => node.RotationDegrees = value,
            _ => 0f,
            (_, value) => Mathf.IsFinite(value),
            stored: true),
        new PropertyDescriptor<Entity, Vector2>(
            nameof(Scale),
            node => node.Scale,
            (node, value) => node.Scale = value,
            _ => Vector2.One,
            (_, value) => IsFinite(value),
            stored: true),
        new PropertyDescriptor<Entity, float>(
            nameof(Skew),
            node => node.Skew,
            (node, value) => node.Skew = value,
            _ => 0f,
            (_, value) => Mathf.IsFinite(value),
            stored: true)
    ];

    private const float MinimumScale = 0.00001f;

    private Transform _transform = Transform.Identity;

    /// <summary>Gets or sets the affine transform relative to the parent.</summary>
    /// <value>A finite <see cref="Electron2D.Transform"/>; the default is <see cref="Electron2D.Transform.Identity"/>.</value>
    /// <exception cref="ArgumentOutOfRangeException">An assigned transform component is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">An attached node is read or mutated from a thread other than the tree owner.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="Exception">An enabled local-transform notification or event handler throws after the transform changes.</exception>
    public Transform Transform
    {
        get
        {
            ThrowIfDisposed();
            Tree?.EnsureOwnerThread();
            return _transform;
        }
        set
        {
            EnsureMutable();
            SetTransform(value);
        }
    }

    /// <summary>Gets or sets the affine transform in hierarchy-global coordinates.</summary>
    /// <value>The local transform composed with non-top-level ancestors.</value>
    /// <exception cref="ArgumentOutOfRangeException">An assigned transform component is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">The parent transform is singular, or an attached node is read or mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">This node or an ancestor is disposing on another thread, or has finished disposing.</exception>
    /// <exception cref="Exception">An enabled local-transform notification or event handler throws after the transform changes.</exception>
    public Transform GlobalTransform
    {
        get
        {
            ThrowIfDisposed();
            return GetGlobalTransform();
        }
        set
        {
            EnsureMutable();
            EnsureFinite(value, nameof(value));
            SetTransform(ToLocalTransform(value, Parent, TopLevel));
        }
    }

    /// <summary>Gets or sets local translation in pixels or other host-defined 2D units.</summary>
    /// <value>The translation component of <see cref="Transform"/>.</value>
    /// <exception cref="ArgumentOutOfRangeException">An assigned component is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">An attached node is read or mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="Exception">An enabled local-transform notification or event handler throws after the position changes.</exception>
    public Vector2 Position
    {
        get
        {
            ThrowIfDisposed();
            Tree?.EnsureOwnerThread();
            return _transform.Origin;
        }
        set
        {
            EnsureMutable();
            EnsureFinite(value, nameof(value));

            var transform = _transform;
            transform.Origin = value;
            SetTransform(transform);
        }
    }

    /// <summary>Gets or sets translation in hierarchy-global coordinates.</summary>
    /// <value>The translation component of <see cref="GlobalTransform"/>.</value>
    /// <remarks>Assignment converts only the point through the direct canvas parent's inverse and preserves the
    /// local basis exactly. A neutral parent or TopLevel node uses the point directly.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">An assigned component is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">The parent transform is singular, or an attached node is read or mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">This node or an ancestor is disposing on another thread, or has finished disposing.</exception>
    /// <exception cref="Exception">An enabled local-transform notification or event handler throws after the position changes.</exception>
    public Vector2 GlobalPosition
    {
        get => GlobalTransform.Origin;
        set
        {
            EnsureMutable();
            EnsureFinite(value, nameof(value));

            var local = GetParentItem() is { } parent
                ? parent.GetGlobalTransform().AffineInverse() * value
                : value;
            Position = local;
        }
    }

    /// <summary>Gets or sets local rotation in radians.</summary>
    /// <value>The canonical rotation decomposed from <see cref="Transform"/>.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned angle is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">An attached node is read or mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="Exception">An enabled local-transform notification or event handler throws after the rotation changes.</exception>
    public float Rotation
    {
        get => Transform.Rotation;
        set
        {
            EnsureMutable();
            EnsureFinite(value, nameof(value));
            SetTransform(new Transform(value, _transform.Scale, _transform.Skew, _transform.Origin));
        }
    }

    /// <summary>Gets or sets local rotation in degrees.</summary>
    /// <value><see cref="Rotation"/> converted between radians and degrees.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned angle is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">An attached node is read or mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="Exception">An enabled local-transform notification or event handler throws after the rotation changes.</exception>
    public float RotationDegrees
    {
        get => Mathf.RadToDeg(Rotation);
        set
        {
            EnsureMutable();
            EnsureFinite(value, "degrees");
            Rotation = Mathf.DegToRad(value);
        }
    }

    /// <summary>Gets or sets hierarchy-global rotation in radians.</summary>
    /// <value>The canonical rotation decomposed from <see cref="GlobalTransform"/>.</value>
    /// <remarks>With a canvas parent, setting this changes only local rotation after converting the rotated global
    /// basis through the parent; local scale, skew, and position stay unchanged.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The assigned angle is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">The parent transform is singular, or an attached node is read or mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">This node or an ancestor is disposing on another thread, or has finished disposing.</exception>
    /// <exception cref="Exception">An enabled local-transform notification or event handler throws after the rotation changes.</exception>
    public float GlobalRotation
    {
        get => GlobalTransform.Rotation;
        set
        {
            EnsureMutable();
            EnsureFinite(value, nameof(value));
            if (GetParentItem() is not { } parent)
            {
                Rotation = value;
                return;
            }

            var parentGlobal = parent.GetGlobalTransform();
            var desired = parentGlobal * _transform;
            var scale = desired.Scale;
            var cosine = Mathf.Cos(value);
            var sine = Mathf.Sin(value);
            desired.X = new Vector2(cosine, sine).Normalized() * scale.X;
            desired.Y = new Vector2(-sine, cosine).Normalized() * scale.Y;
            Rotation = (parentGlobal.AffineInverse() * desired).Rotation;
        }
    }

    /// <summary>Gets or sets hierarchy-global rotation in degrees.</summary>
    /// <value><see cref="GlobalRotation"/> converted between radians and degrees.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned angle is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">The parent transform is singular, or an attached node is read or mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">This node or an ancestor is disposing on another thread, or has finished disposing.</exception>
    /// <exception cref="Exception">An enabled local-transform notification or event handler throws after the rotation changes.</exception>
    public float GlobalRotationDegrees
    {
        get => Mathf.RadToDeg(GlobalRotation);
        set
        {
            EnsureMutable();
            EnsureFinite(value, "degrees");
            GlobalRotation = Mathf.DegToRad(value);
        }
    }

    /// <summary>Gets or sets local scale.</summary>
    /// <value>The canonical scale decomposed from <see cref="Transform"/>.</value>
    /// <remarks>Components with magnitude below 0.00001 are replaced by positive 0.00001. Equivalent reflected
    /// matrices can decompose to a different but equivalent rotation, scale, and skew tuple.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">An assigned component is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">An attached node is read or mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="Exception">An enabled local-transform notification or event handler throws after the scale changes.</exception>
    public Vector2 Scale
    {
        get => Transform.Scale;
        set
        {
            EnsureMutable();
            EnsureFinite(value, nameof(value));
            value = new Vector2(
                Mathf.Abs(value.X) < MinimumScale ? MinimumScale : value.X,
                Mathf.Abs(value.Y) < MinimumScale ? MinimumScale : value.Y);
            SetTransform(new Transform(_transform.Rotation, value, _transform.Skew, _transform.Origin));
        }
    }

    /// <summary>Gets or sets hierarchy-global scale.</summary>
    /// <value>The canonical scale decomposed from <see cref="GlobalTransform"/>.</value>
    /// <remarks>The desired global basis keeps each axis direction, including reflection, while replacing its length;
    /// it is then converted through the parent and the resulting local scale uses the same near-zero replacement
    /// as <see cref="Scale"/>. Equivalent reflected matrices can decompose to a
    /// different but equivalent rotation, scale, and skew tuple.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">An assigned component is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">The parent transform is singular, or an attached node is read or mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">This node or an ancestor is disposing on another thread, or has finished disposing.</exception>
    /// <exception cref="Exception">An enabled local-transform notification or event handler throws after the scale changes.</exception>
    public Vector2 GlobalScale
    {
        get => GlobalTransform.Scale;
        set
        {
            EnsureMutable();
            EnsureFinite(value, nameof(value));
            if (GetParentItem() is not { } parent)
            {
                Scale = value;
                return;
            }

            var parentGlobal = parent.GetGlobalTransform();
            var desired = parentGlobal * _transform;
            desired.X = desired.X.Normalized() * value.X;
            desired.Y = desired.Y.Normalized() * value.Y;
            Scale = (parentGlobal.AffineInverse() * desired).Scale;
        }
    }

    /// <summary>Gets or sets the local skew angle in radians.</summary>
    /// <value>The canonical angle between the transformed basis axes relative to an unskewed basis.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned angle is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">An attached node is read or mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="Exception">An enabled local-transform notification or event handler throws after the skew changes.</exception>
    public float Skew
    {
        get => Transform.Skew;
        set
        {
            EnsureMutable();
            EnsureFinite(value, nameof(value));
            SetTransform(new Transform(_transform.Rotation, _transform.Scale, value, _transform.Origin));
        }
    }

    /// <summary>Gets or sets the hierarchy-global skew angle in radians.</summary>
    /// <value>The canonical skew decomposed from <see cref="GlobalTransform"/>.</value>
    /// <remarks>With a canvas parent, setting this changes only local skew after converting the globally skewed
    /// basis through the parent; local rotation, scale, and position stay unchanged.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The assigned angle is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">The parent transform is singular, or an attached node is read or mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">This node or an ancestor is disposing on another thread, or has finished disposing.</exception>
    /// <exception cref="Exception">An enabled local-transform notification or event handler throws after the skew changes.</exception>
    public float GlobalSkew
    {
        get => GlobalTransform.Skew;
        set
        {
            EnsureMutable();
            EnsureFinite(value, nameof(value));
            if (GetParentItem() is not { } parent)
            {
                Skew = value;
                return;
            }

            var parentGlobal = parent.GetGlobalTransform();
            var desired = parentGlobal * _transform;
            var determinant = desired.Determinant();
            var sign = determinant > 0f ? 1f : determinant < 0f ? -1f : 0f;
            desired.Y = sign * desired.X.Rotated(Mathf.Pi * 0.5f + value).Normalized() * desired.Y.Length();
            Skew = (parentGlobal.AffineInverse() * desired).Skew;
        }
    }

    /// <summary>Component-multiplies the local scale by a ratio.</summary>
    /// <param name="ratio">The finite X and Y scale ratios.</param>
    /// <exception cref="ArgumentOutOfRangeException">A ratio component is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="Exception">An enabled local-transform notification or event handler throws after the scale changes.</exception>
    public void ApplyScale(Vector2 ratio)
    {
        EnsureMutable();
        EnsureFinite(ratio, nameof(ratio));
        Scale *= ratio;
    }

    /// <summary>Adds an angle to the local rotation.</summary>
    /// <param name="radians">The finite angle in radians.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="radians"/> is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="Exception">An enabled local-transform notification or event handler throws after the rotation changes.</exception>
    public void Rotate(float radians)
    {
        EnsureMutable();
        EnsureFinite(radians, nameof(radians));
        Rotation += radians;
    }

    /// <summary>Adds an offset to this node's position in its parent coordinate space.</summary>
    /// <param name="offset">The finite local-space offset.</param>
    /// <remarks>Scale and skew do not affect the offset.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">An offset component is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="Exception">An enabled local-transform notification or event handler throws after the position changes.</exception>
    public void Translate(Vector2 offset)
    {
        EnsureMutable();
        EnsureFinite(offset, nameof(offset));
        Position += offset;
    }

    /// <summary>Moves this node by a hierarchy-global offset.</summary>
    /// <param name="offset">The finite global-space offset.</param>
    /// <exception cref="ArgumentOutOfRangeException">An offset component is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">The global transform is singular, or mutation occurs off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">This node or an ancestor is disposing on another thread, or has finished disposing.</exception>
    /// <exception cref="Exception">An enabled local-transform notification or event handler throws after the position changes.</exception>
    public void GlobalTranslate(Vector2 offset)
    {
        EnsureMutable();
        EnsureFinite(offset, nameof(offset));
        GlobalPosition += offset;
    }

    /// <summary>Moves this node along its local X basis axis.</summary>
    /// <param name="delta">The finite signed distance.</param>
    /// <param name="scaled">Whether scale magnitude is retained. By default the axis is normalized.</param>
    /// <remarks>An exactly zero or underflowed normalized axis causes no displacement, but still assigns Position.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="delta"/> is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="Exception">An enabled local-transform notification or event handler throws after the position changes.</exception>
    public void MoveLocalX(float delta, bool scaled = false) => MoveLocal(delta, useXAxis: true, scaled);

    /// <summary>Moves this node along its local Y basis axis.</summary>
    /// <param name="delta">The finite signed distance.</param>
    /// <param name="scaled">Whether scale magnitude is retained. By default the axis is normalized.</param>
    /// <remarks>An exactly zero or underflowed normalized axis causes no displacement, but still assigns Position.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="delta"/> is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="Exception">An enabled local-transform notification or event handler throws after the position changes.</exception>
    public void MoveLocalY(float delta, bool scaled = false) => MoveLocal(delta, useXAxis: false, scaled);

    /// <summary>Computes the signed local angle toward a global point, compensating for local scale.</summary>
    /// <param name="globalPoint">The finite point in hierarchy-global coordinates.</param>
    /// <returns>The angle of the inverse-transformed point multiplied by local scale, in radians.</returns>
    /// <exception cref="InvalidOperationException">The global transform is singular, or an attached node is queried off the owner thread.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A point component is NaN or infinite.</exception>
    /// <exception cref="ObjectDisposedException">This node or an ancestor is disposing on another thread, or has finished disposing.</exception>
    public float GetAngleTo(Vector2 globalPoint)
    {
        ThrowIfDisposed();
        Tree?.EnsureOwnerThread();
        EnsureFinite(globalPoint, nameof(globalPoint));
        return (ToLocal(globalPoint) * Scale).Angle();
    }

    /// <summary>Rotates this node so its positive local X direction points at a global point.</summary>
    /// <param name="globalPoint">The finite target point in hierarchy-global coordinates.</param>
    /// <remarks>A target equal to <see cref="GlobalPosition"/> adds a zero angle and leaves rotation unchanged, but
    /// still commits the transform and delivers enabled local notifications.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">A point component is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">The global transform is singular, or mutation occurs off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">This node or an ancestor is disposing on another thread, or has finished disposing.</exception>
    /// <exception cref="Exception">An enabled local-transform notification or event handler throws after the rotation changes.</exception>
    public void LookAt(Vector2 globalPoint)
    {
        EnsureMutable();
        EnsureFinite(globalPoint, nameof(globalPoint));

        Rotate(GetAngleTo(globalPoint));
    }

    /// <summary>Transforms a point from this node's local coordinates to hierarchy-global coordinates.</summary>
    /// <param name="localPoint">The finite local point.</param>
    /// <returns>The point transformed by <see cref="GlobalTransform"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A point component is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">An attached node is queried off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">This node or an ancestor is disposing on another thread, or has finished disposing.</exception>
    public Vector2 ToGlobal(Vector2 localPoint)
    {
        ThrowIfDisposed();
        Tree?.EnsureOwnerThread();
        EnsureFinite(localPoint, nameof(localPoint));
        return GlobalTransform * localPoint;
    }

    /// <summary>Transforms a point from hierarchy-global coordinates to this node's local coordinates.</summary>
    /// <param name="globalPoint">The finite global point.</param>
    /// <returns>The point transformed by the inverse global transform.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A point component is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">The global transform is singular, or an attached node is queried off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">This node or an ancestor is disposing on another thread, or has finished disposing.</exception>
    public Vector2 ToLocal(Vector2 globalPoint)
    {
        ThrowIfDisposed();
        Tree?.EnsureOwnerThread();
        EnsureFinite(globalPoint, nameof(globalPoint));
        return GlobalTransform.AffineInverse() * globalPoint;
    }

    /// <summary>Returns the product of local transforms up to a spatial ancestor.</summary>
    /// <param name="parent">This node itself or an ancestor connected through spatial nodes.</param>
    /// <returns>Identity for this node; otherwise the ordered product of local spatial transforms up to the ancestor.</returns>
    /// <remarks>TopLevel does not interrupt this query. Neutral and non-spatial canvas ancestors do interrupt it; no
    /// matrix inversion is needed. Invalid ancestry throws a typed exception instead of returning identity.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="parent"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="parent"/> is not connected by an uninterrupted spatial-parent chain.</exception>
    /// <exception cref="InvalidOperationException">An attached node is queried off the scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">
    /// This node, <paramref name="parent"/>, or a queried ancestor is disposing on another thread or has finished disposing.
    /// </exception>
    public Transform GetRelativeTransformToParent(Node parent)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(parent);
        if (parent.IsDisposed)
            throw new ObjectDisposedException(nameof(parent));
        Tree?.EnsureOwnerThread();

        if (ReferenceEquals(parent, this))
            return Transform.Identity;

        if (Parent is not Entity spatialParent)
            throw new ArgumentException("The supplied ancestor must be connected by spatial nodes.", nameof(parent));
        return ReferenceEquals(parent, spatialParent)
            ? Transform
            : spatialParent.GetRelativeTransformToParent(parent) * Transform;
    }

    private static bool IsFinite(Vector2 value) => Mathf.IsFinite(value.X) && Mathf.IsFinite(value.Y);

    private static void EnsureFinite(float value, string parameterName)
    {
        if (!Mathf.IsFinite(value))
            throw new ArgumentOutOfRangeException(parameterName, value, "The value must be finite.");
    }

    private static void EnsureFinite(Vector2 value, string parameterName)
    {
        if (!IsFinite(value))
            throw new ArgumentOutOfRangeException(parameterName, value, "Both vector components must be finite.");
    }

    private static void EnsureFinite(Transform value, string parameterName)
    {
        if (!value.IsFinite())
            throw new ArgumentOutOfRangeException(parameterName, value, "Every transform component must be finite.");
    }

    private static Transform ToLocalTransform(Transform global, Node? parent, bool topLevel)
    {
        if (parent is not CanvasItem canvas || topLevel)
            return global;

        return canvas.GetGlobalTransform().AffineInverse() * global;
    }

    private void SetTransform(Transform transform)
    {
        EnsureFinite(transform, nameof(transform));

        _transform = transform;

        NotifyLocalTransformChanged();
    }

    private void MoveLocal(float delta, bool useXAxis, bool scaled)
    {
        EnsureMutable();
        EnsureFinite(delta, nameof(delta));

        var axis = useXAxis ? _transform.X : _transform.Y;

        if (!scaled)
            axis = axis.Normalized();

        Position += axis * delta;
    }

}
