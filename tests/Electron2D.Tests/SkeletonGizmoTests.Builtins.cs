using Electron2D;

internal static partial class SkeletonGizmoTests
{
    private static void Builtins(Skeleton rig, SkeletonModificationStack stack, Window window)
    {
        using var bone = new Bone { Name = "Arm", Position = new(64, 64) };
        bone.SetAutocalculateLengthAndAngle(false); bone.SetLength(24); bone.SetBoneAngle(0); rig.AddChild(bone);
        using var target = new Entity { Name = "Target", Position = new(120, 64) }; rig.AddChild(target);
        using var look = new SkeletonModificationLookAt { BoneIndex = 0 };
        using var ccd = new SkeletonModificationCCDIK { CCDIKDataChainLength = 1 };
        using var two = new SkeletonModificationTwoBoneIK { TargetNodePath = "Target" };
        using var held = new SkeletonModificationStack(); using var holder = new SkeletonModificationStackHolder { EditorDrawGizmo = true };
        Check(look.EditorDrawGizmo && ccd.EditorDrawGizmo && two.EditorDrawGizmo && !two.EditorDrawMinMax && ccd.GetCCDIKJointEditorDrawGizmo(0),
            "Builtin and per-joint defaults retain their authoring policy");
        look.SetEnableConstraint(true); look.SetConstraintAngleMax(Mathf.Pi / 2);
        stack.SetModification(0, look); var normal = Render(); Check(normal.Count > 8, "LookAt draws the actual configured angle sector");
        look.SetConstraintAngleInvert(true); var inverted = Render(); Check(inverted.Count > normal.Count, "Inverted limits draw the complementary sector");
        bone.Position += new Vector2(24, 0); var moved = Render();
        Check(Math.Abs(moved.MinX - inverted.MinX - 24) <= 2, "Bone pose edits refresh retained editor geometry");
        bone.Position = new(64, 64); look.Enabled = false; Check(Render().Count == 0, "Disabled builtin modifications clear their guides");
        ccd.SetCCDIKJointBoneIndex(0, 0); stack.SetModification(0, ccd); Check(Render().Count > 8, "CCDIK draws a selected joint without executing IK");
        ccd.SetCCDIKJointEditorDrawGizmo(0, false); Check(Render().Count == 0, "Per-joint visibility clears the real guide");
        using (var copy = (SkeletonModificationCCDIK)ccd.Duplicate())
            Check(!copy.GetCCDIKJointEditorDrawGizmo(0) && copy.EditorDrawGizmo, "Independent resource copy preserves both visibility levels");
        var stored = (PropertyDescriptor<SkeletonModificationCCDIK, byte[]>)ccd.GetPropertyList().First(p => p.Name == "_joints");
        var saved = stored.GetValue(ccd);
        ccd.SetCCDIKJointEditorDrawGizmo(0, true); stored.SetValue(ccd, saved);
        Check(!ccd.GetCCDIKJointEditorDrawGizmo(0), "Version-two joint storage preserves hidden guides");
        using (var stream = new MemoryStream())
        {
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
            { writer.Write(1); writer.Write(1); writer.Write(""); writer.Write(0); writer.Write(false); writer.Write(false); writer.Write(0f); writer.Write(Mathf.Tau); writer.Write(false); writer.Write(true); }
            stored.SetValue(ccd, stream.ToArray());
        }
        Check(ccd.GetCCDIKJointEditorDrawGizmo(0), "Version-one joint storage migrates to the original visible-joint default");
        var invalid = stored.GetValue(ccd); BitConverter.GetBytes(42).CopyTo(invalid, 0);
        Reject<InvalidDataException>(() => stored.SetValue(ccd, invalid)); Check(ccd.GetCCDIKJointEditorDrawGizmo(0), "Rejected storage keeps prior joint settings");
        two.SetJointOneBoneIndex(0); stack.SetModification(0, two);
        var bend = Render(); two.FlipBendDirection = true; var flipped = Render();
        Check(bend.Count > 0 && flipped.Count > 0 && flipped.MaxY < bend.MaxY - 6, "TwoBoneIK displays the selected bend direction");
        two.EditorDrawMinMax = true; two.TargetMinimumDistance = 8; two.TargetMaximumDistance = 40;
        var distance = Render(); Check(distance.MaxX > flipped.MaxX + 24, "Target distance settings produce actual endpoint and interval guides");
        using (var copy = (SkeletonModificationTwoBoneIK)two.Duplicate()) Check(copy.EditorDrawMinMax, "Distance-guide visibility copies with its concrete resource");
        stack.SetModification(0, null); look.Enabled = true;
        held.AddModification(look); holder.SetHeldModificationStack(held); stack.SetModification(0, holder);
        Check(Render().Count > 8, "A visible holder draws its bound child despite disabled numerical stacks");
        rig.Scale = Vector2.Zero; Check(Render().Count == 0, "Singular authoring transforms draw no invalid guide geometry"); rig.Scale = Vector2.One;
        holder.EditorDrawGizmo = false; Check(Render().Count == 0, "Holder visibility clears nested commands");
        stack.SetModification(0, null);

        (int Count, float MinX, float MaxX, float MaxY) Render()
        {
            RenderingServer.ForceDraw(false);
            using var image = window.GetTexture().GetImage()!;
            var count = 0; var minX = float.PositiveInfinity; var maxX = float.NegativeInfinity; var maxY = float.NegativeInfinity;
            for (var y = 0; y < image.Height; y++) for (var x = 0; x < image.Width; x++)
                {
                    var color = image.GetPixel(x, y);
                    if (color.R <= .5f || color.G <= .2f || color.B >= .2f) continue;
                    count++; var px = x * window.Size.X / (float)image.Width; var py = y * window.Size.Y / (float)image.Height;
                    minX = Math.Min(minX, px); maxX = Math.Max(maxX, px); maxY = Math.Max(maxY, py);
                }
            return (count, minX, maxX, maxY);
        }
    }
}
