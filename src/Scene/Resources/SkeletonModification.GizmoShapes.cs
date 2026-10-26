namespace Electron2D;

public abstract partial class SkeletonModification
{
    internal static readonly Color EditorGizmoColor = new(1, .65f, 0, .8f);

    internal static void DrawAngleGizmo(Skeleton skeleton, Bone bone, float minimum, float maximum, bool constrained, bool local, bool inverted)
    {
        static float Bound(float angle) { var result = Mathf.PosMod(angle, Mathf.Tau); return result == 0 && angle > 0 ? Mathf.Tau : result; }
        minimum = Bound(minimum); maximum = Bound(maximum);
        if (minimum > maximum) (minimum, maximum) = (maximum, minimum);
        minimum += bone.GetBoneAngle(); maximum += bone.GetBoneAngle();
        var rotation = constrained && local && bone.Parent is Bone parent ? parent.GlobalRotation : 0;
        skeleton.DrawSetTransform(skeleton.ToLocal(bone.GlobalPosition), rotation - skeleton.GlobalRotation);
        var length = bone.GetLength();
        if (length <= 0) return;
        if (constrained)
        {
            skeleton.DrawArc(Vector2.Zero, length, inverted ? minimum + Mathf.Tau : minimum, maximum, 32, EditorGizmoColor, 1);
            skeleton.DrawLine(Vector2.Zero, Vector2.FromAngle(minimum) * length, EditorGizmoColor, 1);
            skeleton.DrawLine(Vector2.Zero, Vector2.FromAngle(maximum) * length, EditorGizmoColor, 1);
        }
        else
        {
            skeleton.DrawArc(Vector2.Zero, length, 0, Mathf.Tau, 32, EditorGizmoColor, 1);
            skeleton.DrawLine(Vector2.Zero, Vector2.Right * length, EditorGizmoColor, 1);
        }
    }
}

public sealed partial class SkeletonModificationLookAt
{
    /// <inheritdoc />
    protected override void OnDrawEditorGizmo()
    {
        var skeleton = BoundSkeleton;
        if (!Enabled || !GetIsSetup() || skeleton is null || skeleton.GlobalTransform.Determinant() == 0) return;
        lock (ModificationGate)
        {
            var bone = _boneNode.Length > 0 ? skeleton.GetNodeOrNull(_boneNode) as Bone
                : _boneIndex >= 0 && _boneIndex < skeleton.GetBoneCount() ? skeleton.GetBone(_boneIndex) : null;
            if (bone is not null && ReferenceEquals(bone.SkeletonOwner, skeleton))
                DrawAngleGizmo(skeleton, bone, _minimum, _maximum, _constraint, _local, _invert);
        }
    }
}

public sealed partial class SkeletonModificationCCDIK
{
    /// <summary>Returns whether an indexed joint draws its configured editor angle guide.</summary>
    /// <param name="jointIndex">A valid authored joint slot.</param><returns>True initially for each new slot.</returns>
    public bool GetCCDIKJointEditorDrawGizmo(int jointIndex) { lock (ModificationGate) return _joints[Index(jointIndex)].EditorGizmo; }
    /// <summary>Sets whether an indexed joint draws its configured editor angle guide.</summary>
    /// <param name="jointIndex">A valid authored joint slot.</param><param name="drawGizmo">Stored joint visibility.</param>
    public void SetCCDIKJointEditorDrawGizmo(int jointIndex, bool drawGizmo)
    { lock (ModificationGate) { EnsureModificationMutable(); _joints[Index(jointIndex)].EditorGizmo = drawGizmo; } EmitChanged(); }

    /// <inheritdoc />
    protected override void OnDrawEditorGizmo()
    {
        var skeleton = BoundSkeleton;
        if (!Enabled || !GetIsSetup() || skeleton is null || skeleton.GlobalTransform.Determinant() == 0) return;
        lock (ModificationGate)
            foreach (var joint in _joints)
                if (joint.EditorGizmo && joint.Binding.Bone(skeleton, joint.Path, joint.Index) is { } bone)
                    DrawAngleGizmo(skeleton, bone, joint.Minimum, joint.Maximum, joint.EnableConstraint, joint.Local, joint.Invert);
    }
}

public sealed partial class SkeletonModificationTwoBoneIK
{
    private bool _editorDrawMinMax;
    /// <summary>Gets or sets whether the editor draws the configured target distance interval.</summary>
    /// <value>False initially. Requires this modification's EditorDrawGizmo and an edited-scene owner.</value>
    public bool EditorDrawMinMax
    {
        get { lock (ModificationGate) { ThrowIfDisposed(); return _editorDrawMinMax; } }
        set { lock (ModificationGate) { EnsureModificationMutable(); _editorDrawMinMax = value; } EmitChanged(); }
    }

    /// <inheritdoc />
    protected override void OnDrawEditorGizmo()
    {
        var skeleton = BoundSkeleton;
        if (!Enabled || !GetIsSetup() || skeleton is null || skeleton.GlobalTransform.Determinant() == 0) return;
        lock (ModificationGate)
        {
            if (_one.Bone(skeleton, _onePath, _oneIndex) is not { } bone) return;
            skeleton.DrawSetTransform(skeleton.ToLocal(bone.GlobalPosition), bone.GlobalRotation - skeleton.GlobalRotation, bone.GlobalScale);
            var angle = (_flip ? -Mathf.Pi : Mathf.Pi) / 2 + bone.GetBoneAngle();
            skeleton.DrawLine(Vector2.Zero, Vector2.FromAngle(angle) * (bone.GetLength() * .5f), EditorGizmoColor, 2);
            if (!_editorDrawMinMax || _minimum == 0 && _maximum == 0) return;
            var direction = Vector2.Down;
            if (_target.Resolve(skeleton, _targetPath) is Entity target)
                direction = SkeletonIKBinding.Direction(bone.GlobalPosition, target.GlobalPosition, Vector2.Down);
            skeleton.DrawSetTransform(skeleton.ToLocal(bone.GlobalPosition), -skeleton.GlobalRotation);
            skeleton.DrawCircle(direction * _minimum, 8, EditorGizmoColor);
            skeleton.DrawCircle(direction * _maximum, 8, EditorGizmoColor);
            skeleton.DrawLine(direction * _minimum, direction * _maximum, EditorGizmoColor, 2);
        }
    }
}

public sealed partial class SkeletonModificationStackHolder
{
    /// <inheritdoc />
    protected override void OnDrawEditorGizmo()
    {
        SkeletonModificationStack? held;
        lock (ModificationGate) held = _held;
        held?.DrawEditorGizmos();
    }
}
