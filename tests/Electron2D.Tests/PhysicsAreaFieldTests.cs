using Electron2D;
using System.Text.Json;

internal static class PhysicsAreaFieldTests
{
    internal static void Run()
    {
        VerifyDefaultsAndValidation();
        VerifyPriorityAndPointGravity();
        VerifyDampingAndWorldSettings();
        VerifyPackedFields();
        VerifySleepingBodyWakesOnFieldChange();
        VerifyWarmFieldAllocations();
        Console.WriteLine("Physics area gravity, damping, priority and project-setting checks passed.");
    }

    private static void VerifyDefaultsAndValidation()
    {
        using var area = new Area();
        using var body = new RigidBody();
        using var stationary = new StaticBody();
        Check(body.GetGravity() == Vector2.Zero && stationary.GetGravity() == Vector2.Zero,
            "Detached physics bodies have no resolved gravity state.");
        Check(area.GravitySpaceOverride == Area.SpaceOverride.Disabled && area.Gravity == 980 &&
              area.GravityDirection == new Vector2(0, 1) && area.GravityPointCenter == new Vector2(0, 1) &&
              !area.GravityPoint && area.GravityPointUnitDistance == 0 &&
              area.LinearDamp == 0.1f && area.AngularDamp == 1 && area.Priority == 0 &&
              area.LinearDampSpaceOverride == Area.SpaceOverride.Disabled &&
              area.AngularDampSpaceOverride == Area.SpaceOverride.Disabled,
            "Area field defaults retain the pinned two-dimensional surface values.");
        Check((int)Area.SpaceOverride.Disabled == 0 && (int)Area.SpaceOverride.Combine == 1 &&
              (int)Area.SpaceOverride.CombineReplace == 2 && (int)Area.SpaceOverride.Replace == 3 &&
              (int)Area.SpaceOverride.ReplaceCombine == 4 &&
              (int)RigidBody.DampMode.Combine == 0 && (int)RigidBody.DampMode.Replace == 1 &&
              body.LinearDampMode == RigidBody.DampMode.Combine && body.AngularDampMode == RigidBody.DampMode.Combine,
            "Area and body mode numeric values and defaults match the pinned reference.");
        area.GravityDirection = new(2, 3);
        Check(area.GravityPointCenter == new Vector2(2, 3),
            "Point center and direction share the reference gravity vector.");
        area.GravityPointCenter = new(4, 5);
        Check(area.GravityDirection == new Vector2(4, 5), "Changing the point center updates direction.");
        Reject<ArgumentOutOfRangeException>(() => area.Gravity = float.NaN);
        Reject<ArgumentOutOfRangeException>(() => area.GravityDirection = new(float.PositiveInfinity, 0));
        Reject<ArgumentOutOfRangeException>(() => area.LinearDamp = float.NegativeInfinity);
        Reject<ArgumentOutOfRangeException>(() => area.GravitySpaceOverride = (Area.SpaceOverride)9);
        Reject<ArgumentOutOfRangeException>(() => body.AngularDampMode = (RigidBody.DampMode)(-1));
        Check(area.Gravity == 980 && area.GravityDirection == new Vector2(4, 5) &&
              area.LinearDamp == 0.1f && area.GravitySpaceOverride == Area.SpaceOverride.Disabled &&
              body.AngularDampMode == RigidBody.DampMode.Combine,
            "Invalid writes leave detached area and body state intact.");
        Check(ProjectSettings.Physics2DDefaultGravity.DefaultValue == 980 &&
              ProjectSettings.Physics2DDefaultGravityVector.DefaultValue == new Vector2(0, 1) &&
              ProjectSettings.Physics2DDefaultLinearDamp.DefaultValue == 0.1f &&
              ProjectSettings.Physics2DDefaultAngularDamp.DefaultValue == 1,
            "Typed world settings expose the pinned gravity and damping defaults.");
        var settings = ProjectSettings.Service;
        var previousGravity = ProjectSettings.GetWithOverride(ProjectSettings.Physics2DDefaultGravity);
        var previousDirection = ProjectSettings.GetWithOverride(ProjectSettings.Physics2DDefaultGravityVector);
        var previousLinear = ProjectSettings.GetWithOverride(ProjectSettings.Physics2DDefaultLinearDamp);
        var previousAngular = ProjectSettings.GetWithOverride(ProjectSettings.Physics2DDefaultAngularDamp);
        Reject<ArgumentException>(() => ProjectSettings.Set(ProjectSettings.Physics2DDefaultGravity, float.NaN));
        Reject<JsonException>(() => ProjectSettings.Set(ProjectSettings.Physics2DDefaultGravityVector,
            new Vector2(float.PositiveInfinity, 0)));
        Reject<ArgumentException>(() => ProjectSettings.Set(ProjectSettings.Physics2DDefaultLinearDamp,
            float.NegativeInfinity));
        Reject<ArgumentException>(() => ProjectSettings.Set(ProjectSettings.Physics2DDefaultAngularDamp,
            float.NaN));
        Check(ProjectSettings.GetWithOverride(ProjectSettings.Physics2DDefaultGravity) == previousGravity &&
              ProjectSettings.GetWithOverride(ProjectSettings.Physics2DDefaultGravityVector) == previousDirection &&
              ProjectSettings.GetWithOverride(ProjectSettings.Physics2DDefaultLinearDamp) == previousLinear &&
              ProjectSettings.GetWithOverride(ProjectSettings.Physics2DDefaultAngularDamp) == previousAngular,
            "Invalid project physics defaults reject before changing registry state.");
    }

    private static void VerifyPriorityAndPointGravity()
    {
        using var region = new RectangleShape { Size = new(400, 400) };
        using var circle = new CircleShape();
        var root = new Node();
        var high = new Area
        {
            Name = "High",
            Priority = 10,
            Gravity = 100,
            GravityDirection = new(1, 0),
            GravitySpaceOverride = Area.SpaceOverride.Combine
        };
        high.AddChild(new CollisionShape { Shape = region });
        var low = new Area
        {
            Name = "Low",
            Priority = 5,
            Gravity = 200,
            GravityDirection = new(0, -1),
            GravitySpaceOverride = Area.SpaceOverride.Combine
        };
        low.AddChild(new CollisionShape { Shape = region });
        var body = new RigidBody { CollisionMask = 0, CanSleep = false };
        body.AddChild(new CollisionShape { Shape = circle });
        var stationary = new StaticBody { Position = new(1000, 1000) };
        stationary.AddChild(new CollisionShape { Shape = circle });
        root.AddChild(high); root.AddChild(low); root.AddChild(body); root.AddChild(stationary);
        using var tree = new SceneTree(root);
        tree.PhysicsFrame(1d / 60);
        Check(stationary.GetGravity() == Vector2.Zero,
            "An attached stationary body reports no integrated gravity.");
        Reject<InvalidOperationException>(() => Task.Run(body.GetGravity).GetAwaiter().GetResult());
        Check(body.GetGravity().IsEqualApprox(new Vector2(100, 780)),
            "Overlapping fields add in priority order and include world gravity despite body mask zero.");
        low.GravitySpaceOverride = Area.SpaceOverride.Replace;
        ResetBody();
        Check(body.GetGravity().IsEqualApprox(new Vector2(0, -200)),
            "A lower-priority Replace discards earlier fields and ignores the world default.");
        high.GravitySpaceOverride = Area.SpaceOverride.CombineReplace;
        ResetBody();
        Check(body.GetGravity().IsEqualApprox(new Vector2(100, 0)),
            "CombineReplace adds the higher-priority field and stops lower-priority gravity.");
        low.Priority = 20;
        ResetBody();
        Check(body.GetGravity().IsEqualApprox(new Vector2(0, -200)),
            "Changing priority live reverses which stopping field wins.");
        low.Priority = 5;
        high.GravitySpaceOverride = Area.SpaceOverride.ReplaceCombine;
        ResetBody();
        Check(body.GetGravity().IsEqualApprox(new Vector2(0, -200)),
            "ReplaceCombine resets the accumulated field and continues to lower priorities.");
        high.GravitySpaceOverride = Area.SpaceOverride.Replace;
        ResetBody();
        Check(body.GetGravity().IsEqualApprox(new Vector2(100, 0)),
            "Replace ignores lower fields and world gravity.");

        low.GravitySpaceOverride = Area.SpaceOverride.Disabled;
        high.GravityPoint = true;
        high.Gravity = 100;
        high.GravityPointCenter = Vector2.Zero;
        high.GravityPointUnitDistance = 10;
        high.Position = new(20, 0);
        ResetBody();
        Check(body.GetGravity().IsEqualApprox(new Vector2(25, 0)),
            "Point gravity uses transformed center and inverse-square falloff.");
        high.GravityPointUnitDistance = 0;
        ResetBody();
        Check(body.GetGravity().IsEqualApprox(new Vector2(100, 0)),
            "Zero unit distance retains constant-magnitude point gravity.");
        high.GravityPointUnitDistance = -10;
        ResetBody();
        Check(body.GetGravity().IsEqualApprox(new Vector2(100, 0)),
            "Negative unit distance also selects constant-magnitude point gravity.");
        high.GravityPointUnitDistance = 0;
        body.GravityScale = 0.5f;
        ResetBody();
        Check(body.GetGravity().IsEqualApprox(new Vector2(50, 0)),
            "Body gravity scale applies after area priority reduction.");
        body.GravityScale = -1;
        ResetBody();
        Check(body.GetGravity().IsEqualApprox(new Vector2(-100, 0)),
            "Negative body gravity scale reverses the resolved area field.");
        body.GravityScale = 0.5f;
        high.Position = Vector2.Zero;
        high.Rotation = MathF.PI / 2;
        high.GravityPointCenter = new(10, 0);
        high.GravityPointUnitDistance = 10;
        ResetBody();
        Check(MathF.Abs(body.GetGravity().X) < 1e-4f && MathF.Abs(body.GetGravity().Y - 50) < 1e-3f,
            "Point gravity transforms its local center through area rotation.");
        high.Rotation = 0;
        high.Position = new(20, 0);
        high.GravityPointCenter = Vector2.Zero;
        high.GravityPointUnitDistance = 0;
        body.GlobalPosition = new(20, 0);
        body.LinearVelocity = Vector2.Zero;
        tree.PhysicsFrame(1d / 60);
        Check(body.GetGravity() == Vector2.Zero,
            "At the point center the gravity vector is zero.");
        body.GlobalPosition = Vector2.Zero;
        high.Monitoring = false;
        high.Monitorable = false;
        high.GravityPoint = false;
        high.GravityDirection = new(2, 0);
        ResetBody();
        Check(body.GetGravity().IsEqualApprox(new Vector2(100, 0)),
            "Directional gravity does not normalize its vector and ignores monitoring flags.");
        high.Position = new(1000, 0);
        ResetBody();
        Check(body.GetGravity().IsEqualApprox(new Vector2(0, 490)),
            "Leaving every area restores world gravity before body scaling.");
        high.Position = Vector2.Zero;
        high.CollisionMask = 0;
        ResetBody();
        Check(body.GetGravity().IsEqualApprox(new Vector2(0, 490)),
            "An area field does not affect a body whose layer its mask excludes.");
        high.CollisionMask = 1;
        high.GravityPoint = true;
        high.GravityPointCenter = new(2, 0);
        high.GravityPointUnitDistance = float.MaxValue;
        body.GlobalPosition = Vector2.Zero;
        Reject<AggregateException>(() => tree.PhysicsFrame(1d / 60));
        high.GravityPointUnitDistance = 0;
        ResetBody();
        Check(body.GetGravity().IsEqualApprox(new Vector2(50, 0)),
            "A nonfinite resolved field rejects the step and a corrected later step succeeds.");

        void ResetBody()
        {
            body.GlobalPosition = Vector2.Zero;
            body.LinearVelocity = Vector2.Zero;
            tree.PhysicsFrame(1d / 60);
        }
    }

    private static void VerifyDampingAndWorldSettings()
    {
        var settings = ProjectSettings.Service;
        var oldGravity = ProjectSettings.GetWithOverride(ProjectSettings.Physics2DDefaultGravity);
        var oldDirection = ProjectSettings.GetWithOverride(ProjectSettings.Physics2DDefaultGravityVector);
        var oldLinear = ProjectSettings.GetWithOverride(ProjectSettings.Physics2DDefaultLinearDamp);
        var oldAngular = ProjectSettings.GetWithOverride(ProjectSettings.Physics2DDefaultAngularDamp);
        try
        {
            ProjectSettings.Set(ProjectSettings.Physics2DDefaultGravity, 0f);
            ProjectSettings.Set(ProjectSettings.Physics2DDefaultGravityVector, new Vector2(0, 1));
            ProjectSettings.Set(ProjectSettings.Physics2DDefaultLinearDamp, 0.1f);
            ProjectSettings.Set(ProjectSettings.Physics2DDefaultAngularDamp, 1f);
            using var region = new RectangleShape { Size = new(1000, 1000) };
            using var circle = new CircleShape();
            var root = new Node();
            var area = new Area
            {
                LinearDampSpaceOverride = Area.SpaceOverride.Replace,
                LinearDamp = 10,
                AngularDampSpaceOverride = Area.SpaceOverride.Replace,
                AngularDamp = 5
            };
            area.AddChild(new CollisionShape { Shape = region });
            var body = new RigidBody
            {
                CanSleep = false,
                LockRotation = false,
                LinearDamp = 2,
                AngularDamp = 3,
                LinearVelocity = new(100, 0),
                AngularVelocity = 2
            };
            body.AddChild(new CollisionShape { Shape = circle });
            root.AddChild(area); root.AddChild(body);
            using var tree = new SceneTree(root);
            tree.PhysicsFrame(0.1);
            Check(body.LinearVelocity.Length() < 0.01f && MathF.Abs(body.AngularVelocity - 0.4f) < 0.05f,
                "Combined area and body damping uses zero-clamped per-step factors.");
            body.LinearDampMode = RigidBody.DampMode.Replace;
            body.AngularDampMode = RigidBody.DampMode.Replace;
            body.LinearVelocity = new(100, 0);
            body.AngularVelocity = 2;
            tree.PhysicsFrame(0.1);
            Check(MathF.Abs(body.LinearVelocity.X - 80) < 1 && MathF.Abs(body.AngularVelocity - 1.4f) < 0.05f,
                "Body Replace modes ignore area and world damping independently.");
            body.LinearDamp = -2;
            body.AngularDamp = -1;
            body.LinearVelocity = new(100, 0);
            body.AngularVelocity = 2;
            tree.PhysicsFrame(0.1);
            Check(MathF.Abs(body.LinearVelocity.X - 120) < 1 && MathF.Abs(body.AngularVelocity - 2.2f) < 0.05f,
                "Finite negative body damping increases velocity like the pinned step equation.");

            area.LinearDampSpaceOverride = Area.SpaceOverride.Disabled;
            area.AngularDampSpaceOverride = Area.SpaceOverride.Disabled;
            body.LinearDampMode = RigidBody.DampMode.Combine;
            body.AngularDampMode = RigidBody.DampMode.Combine;
            body.LinearDamp = 0;
            body.AngularDamp = 0;
            body.LinearVelocity = new(100, 0);
            body.AngularVelocity = 2;
            tree.PhysicsFrame(0.1);
            Check(MathF.Abs(body.LinearVelocity.X - 99) < 1 && MathF.Abs(body.AngularVelocity - 1.8f) < 0.05f,
                "Disabled area fields leave the configured world damping active.");

            area.LinearDampSpaceOverride = Area.SpaceOverride.Replace;
            area.LinearDamp = -2;
            body.LinearDampMode = RigidBody.DampMode.Combine;
            body.LinearVelocity = new(100, 0);
            tree.PhysicsFrame(0.1);
            Check(MathF.Abs(body.LinearVelocity.X - 120) < 1,
                "Signed area damping increases speed when the resolved rate is negative.");

            var lower = new Area
            {
                Name = "Lower",
                Priority = -1,
                LinearDamp = 2,
                LinearDampSpaceOverride = Area.SpaceOverride.Combine,
                AngularDamp = 3,
                AngularDampSpaceOverride = Area.SpaceOverride.Combine
            };
            lower.AddChild(new CollisionShape { Shape = region });
            root.AddChild(lower);
            area.LinearDamp = 1;
            area.AngularDamp = 2;
            area.LinearDampSpaceOverride = Area.SpaceOverride.Combine;
            area.AngularDampSpaceOverride = Area.SpaceOverride.Combine;
            body.LinearDamp = 0;
            body.AngularDamp = 0;
            ResetVelocities();
            Check(MathF.Abs(body.LinearVelocity.X - 69) < 1 && MathF.Abs(body.AngularVelocity - 0.8f) < 0.05f,
                "Independent damping channels combine high and low areas with world defaults.");
            area.LinearDampSpaceOverride = Area.SpaceOverride.CombineReplace;
            area.AngularDampSpaceOverride = Area.SpaceOverride.CombineReplace;
            ResetVelocities();
            Check(MathF.Abs(body.LinearVelocity.X - 90) < 1 && MathF.Abs(body.AngularVelocity - 1.6f) < 0.05f,
                "Damping CombineReplace stops lower-priority areas and world defaults.");
            area.LinearDampSpaceOverride = Area.SpaceOverride.ReplaceCombine;
            area.AngularDampSpaceOverride = Area.SpaceOverride.ReplaceCombine;
            lower.LinearDampSpaceOverride = Area.SpaceOverride.Replace;
            lower.AngularDampSpaceOverride = Area.SpaceOverride.Replace;
            ResetVelocities();
            Check(MathF.Abs(body.LinearVelocity.X - 80) < 1 && MathF.Abs(body.AngularVelocity - 1.4f) < 0.05f,
                "Damping ReplaceCombine continues so a lower Replace can supersede it.");
            area.LinearDampSpaceOverride = Area.SpaceOverride.Replace;
            area.AngularDampSpaceOverride = Area.SpaceOverride.Replace;
            ResetVelocities();
            Check(MathF.Abs(body.LinearVelocity.X - 90) < 1 && MathF.Abs(body.AngularVelocity - 1.6f) < 0.05f,
                "Damping Replace stops lower-priority areas and world defaults.");

            ProjectSettings.Set(ProjectSettings.Physics2DDefaultGravity, 400f);
            ProjectSettings.Set(ProjectSettings.Physics2DDefaultGravityVector, new Vector2(1, 0));
            using var anotherShape = new CircleShape();
            var anotherRoot = new Node();
            var anotherBody = new RigidBody { CanSleep = false };
            anotherBody.AddChild(new CollisionShape { Shape = anotherShape });
            anotherRoot.AddChild(anotherBody);
            using var anotherTree = new SceneTree(anotherRoot);
            anotherTree.PhysicsFrame(1d / 60);
            Check(anotherBody.GetGravity().IsEqualApprox(new Vector2(400, 0)),
                "A new world samples typed project gravity settings.");
            Check(body.GetGravity() == Vector2.Zero,
                "An existing world keeps its sampled project gravity until recreated.");

            void ResetVelocities()
            {
                body.LinearVelocity = new(100, 0);
                body.AngularVelocity = 2;
                tree.PhysicsFrame(0.1);
            }
        }
        finally
        {
            ProjectSettings.Set(ProjectSettings.Physics2DDefaultGravity, oldGravity);
            ProjectSettings.Set(ProjectSettings.Physics2DDefaultGravityVector, oldDirection);
            ProjectSettings.Set(ProjectSettings.Physics2DDefaultLinearDamp, oldLinear);
            ProjectSettings.Set(ProjectSettings.Physics2DDefaultAngularDamp, oldAngular);
        }
    }

    private static void VerifyPackedFields()
    {
        using var shape = new RectangleShape();
        using var root = new Node { Name = "Root" };
        var area = new Area
        {
            Name = "Field",
            GravitySpaceOverride = Area.SpaceOverride.Replace,
            Gravity = 123,
            GravityPoint = true,
            GravityPointCenter = new(7, 8),
            GravityPointUnitDistance = 20,
            LinearDampSpaceOverride = Area.SpaceOverride.Combine,
            LinearDamp = -0.5f,
            AngularDampSpaceOverride = Area.SpaceOverride.ReplaceCombine,
            AngularDamp = 3,
            Priority = 9
        };
        var collider = new CollisionShape { Shape = shape, Name = "Collider" };
        area.AddChild(collider);
        root.AddChild(area);
        area.Owner = root; collider.Owner = root;
        using var packed = new PackedScene(); packed.Pack(root);
        using var copy = packed.Instantiate();
        var field = copy.GetNode<Area>("Field");
        Check(field.GravitySpaceOverride == Area.SpaceOverride.Replace && field.Gravity == 123 &&
              field.GravityPoint && field.GravityPointCenter == new Vector2(7, 8) &&
              field.GravityDirection == new Vector2(7, 8) && field.GravityPointUnitDistance == 20 &&
              field.LinearDampSpaceOverride == Area.SpaceOverride.Combine && field.LinearDamp == -0.5f &&
              field.AngularDampSpaceOverride == Area.SpaceOverride.ReplaceCombine && field.AngularDamp == 3 &&
              field.Priority == 9,
            "PackedScene restores area fields and shared direction/point state.");
    }

    private static void VerifyWarmFieldAllocations()
    {
        using var region = new RectangleShape { Size = new(1000, 1000) };
        using var circle = new CircleShape();
        var root = new Node();
        var area = new Area
        {
            GravitySpaceOverride = Area.SpaceOverride.Replace,
            Gravity = 0,
            GravityPoint = true,
            GravityPointCenter = new(0, 10),
            LinearDampSpaceOverride = Area.SpaceOverride.Replace,
            LinearDamp = 0,
            AngularDampSpaceOverride = Area.SpaceOverride.Replace,
            AngularDamp = 0
        };
        area.AddChild(new CollisionShape { Shape = region });
        var body = new RigidBody { CanSleep = false, LinearVelocity = new(1, 0) };
        body.AddChild(new CollisionShape { Shape = circle });
        root.AddChild(area); root.AddChild(body);
        using var tree = new SceneTree(root);
        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        Check(GC.GetAllocatedBytesForCurrentThread() - before == 0,
            "Warmed moving-body field reduction and point gravity allocate no managed memory.");
    }

    private static void VerifySleepingBodyWakesOnFieldChange()
    {
        using var region = new RectangleShape { Size = new(200, 200) };
        using var circle = new CircleShape();
        var root = new Node();
        var area = new Area { GravitySpaceOverride = Area.SpaceOverride.Replace, Gravity = 0 };
        area.AddChild(new CollisionShape { Shape = region });
        var body = new RigidBody { Sleeping = true };
        body.AddChild(new CollisionShape { Shape = circle });
        root.AddChild(area); root.AddChild(body);
        using var tree = new SceneTree(root);
        tree.PhysicsFrame(1d / 60);
        Check(body.Sleeping, "An unchanged zero-gravity area preserves an initially sleeping body.");
        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        Check(GC.GetAllocatedBytesForCurrentThread() - before == 0,
            "Warmed sleeping-body field reduction allocates no managed memory.");
        area.GravityDirection = new(1, 0);
        area.Gravity = 980;
        tree.PhysicsFrame(1d / 60);
        Check(!body.Sleeping && body.GlobalPosition.X > 0,
            "Changing an active area field wakes an overlapping sleeping body.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
