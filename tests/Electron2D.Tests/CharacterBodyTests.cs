using Electron2D;

internal static class CharacterBodyTests
{
    internal static void Run()
    {
        VerifyDefaultsAndGroundedMotion();
        VerifyWallCeilingAndFloating();
        VerifyFloorSnapAndPlatform();
        VerifyValidationAndPacking();
        VerifyFixedLaneBackendSync();
        VerifyPlatformLeavePolicies();
        VerifyFloatingSlideThreshold();
        VerifyFloorSlopeSpeed();
        VerifyDirectionalClassification();
        VerifyWallPlatformLayers();
        VerifyCharacterGravityArea();
        VerifyWallBlockingAndSlideLimit();
        VerifySlopedCeilingOption();
        VerifyFreeMotionAndLifecycle();
        Console.WriteLine("CharacterBody grounded, floating, snap and platform checks passed.");
    }

    private static void VerifyDefaultsAndGroundedMotion()
    {
        using var bodyShape = new CircleShape();
        using var floorShape = new RectangleShape { Size = new(200, 20) };
        var root = new Node();
        var character = new CharacterBody { Name = "Character" };
        var local = new CollisionShape { Shape = bodyShape };
        character.AddChild(local);
        var floor = new StaticBody { Name = "Floor", Position = new(0, 100) };
        floor.AddChild(new CollisionShape { Shape = floorShape });
        root.AddChild(character); root.AddChild(floor);
        using var tree = new SceneTree(root);
        Check(character.MotionMode == CharacterMotionMode.Grounded &&
              character.PlatformOnLeave == CharacterPlatformOnLeave.AddVelocity &&
              character.SafeMargin == 0.08f && character.FloorStopOnSlope &&
              !character.FloorConstantSpeed && character.FloorBlockOnWall &&
              character.SlideOnCeiling && character.MaxSlides == 4 &&
              character.UpDirection == Vector2.Up && character.PlatformFloorLayers == uint.MaxValue &&
              character.PlatformWallLayers == 0 && character.Velocity == Vector2.Zero &&
              !character.IsOnFloor() && character.GetSlideCollisionCount() == 0,
            "The character exposes the pinned grounded defaults before moving.");
        character.PhysicsProcessEnabled = true;
        tree.PhysicsFrame(1d / 60);
        Check(character.GetGravity().Y is > 979 and < 981,
            "The inherited gravity query reads the resolved world field for a kinematic character.");
        character.Velocity = new(0, 6000);
        Check(character.MoveAndSlide() && character.IsOnFloor() &&
              character.GetFloorNormal().Y < -0.9f && character.GetSlideCollisionCount() >= 1 &&
              character.GlobalPosition.Y is > 70 and < 85 &&
              character.GetLastMotion() == Vector2.Zero &&
              character.GetPositionDelta().Y is > 70 and < 85 &&
              character.GetRealVelocity().Y > 4000 &&
              MathF.Abs(character.GetFloorAngle()) < 0.1f &&
              MathF.Abs(character.Velocity.Y) < 0.01f,
            $"MoveAndSlide floor state: floor={character.IsOnFloor()}, normal={character.GetFloorNormal()}, count={character.GetSlideCollisionCount()}, pos={character.GlobalPosition}, last={character.GetLastMotion()}, delta={character.GetPositionDelta()}, real={character.GetRealVelocity()}, velocity={character.Velocity}.");
        using var immediatePoint = new PhysicsPointQueryParameters2D
        {
            Position = character.GlobalPosition,
            Exclude = [floor.GetRID()]
        };
        Check(character.GetWorld2D()!.DirectSpaceState.IntersectPoint(immediatePoint) is [var sameFrame] &&
              sameFrame.ColliderRID == character.GetRID(),
            "A direct query after MoveAndSlide sees the committed character pose in the same frame.");
        using var hit = character.GetLastSlideCollision();
        Check(hit is not null && hit.GetColliderRID() == floor.GetRID() &&
              ReferenceEquals(hit.GetLocalShape(), local) && hit.GetNormal().Y < -0.9f,
            "The last slide returns a caller-owned collision with local and collider identity.");
        using var first = character.GetSlideCollision(0);
        Check(first.GetColliderRID() == hit!.GetColliderRID() &&
              !ReferenceEquals(first, hit),
            "Indexed and last-slide getters return separate caller-owned snapshots.");
        local.Scale = new(2, 1);
        var retainedPosition = character.GlobalPosition;
        Reject<InvalidOperationException>(() => character.MoveAndSlide());
        Check(character.GlobalPosition == retainedPosition && character.IsOnFloor() &&
              character.GetSlideCollisionCount() == 1,
            "A failed shape preparation leaves the last character contact snapshot and pose intact.");
        local.Scale = Vector2.One;
    }

    private static void VerifyWallCeilingAndFloating()
    {
        using var circle = new CircleShape();
        using var wallShape = new RectangleShape { Size = new(20, 200) };
        using var ceilingShape = new RectangleShape { Size = new(200, 20) };
        var root = new Node();
        var character = new CharacterBody { Name = "Character", PhysicsProcessEnabled = true };
        character.AddChild(new CollisionShape { Shape = circle });
        var wall = new StaticBody { Name = "Wall", Position = new(100, 0) };
        wall.AddChild(new CollisionShape { Shape = wallShape });
        var ceiling = new StaticBody { Name = "Ceiling", Position = new(0, -100) };
        ceiling.AddChild(new CollisionShape { Shape = ceilingShape });
        root.AddChild(character); root.AddChild(wall); root.AddChild(ceiling);
        using var tree = new SceneTree(root);
        tree.PhysicsFrame(1d / 60);
        character.Velocity = new(6000, 0);
        Check(character.MoveAndSlide() && character.IsOnWallOnly() &&
              character.GetWallNormal().X < -0.9f && character.GlobalPosition.X is > 70 and < 85,
            "Grounded horizontal motion stops at a wall and reports its outward normal.");
        character.GlobalPosition = Vector2.Zero;
        character.Velocity = new(0, -6000);
        Check(character.MoveAndSlide() && character.IsOnCeilingOnly() &&
              character.GlobalPosition.Y is > -85 and < -70 && !character.IsOnFloor(),
            "Upward movement classifies a flat overhead surface as ceiling.");
        character.GlobalPosition = Vector2.Zero;
        character.MotionMode = CharacterMotionMode.Floating;
        character.Velocity = new(6000, 3000);
        Check(character.MoveAndSlide() && character.IsOnWall() && !character.IsOnFloor() &&
              !character.IsOnCeiling() && character.GlobalPosition.X is > 70 and < 85 &&
              character.GlobalPosition.Y > 20,
            "Floating movement slides along the wall without floor or ceiling classification.");
    }

    private static void VerifyFloorSnapAndPlatform()
    {
        using var circle = new CircleShape();
        using var floorShape = new RectangleShape { Size = new(200, 20) };
        var root = new Node();
        var character = new CharacterBody
        {
            Name = "Character",
            Position = new(0, 78),
            FloorSnapLength = 5,
            PhysicsProcessEnabled = true
        };
        character.AddChild(new CollisionShape { Shape = circle });
        var platform = new AnimatableBody
        {
            Name = "Platform",
            Position = new(0, 100),
            SyncToPhysics = false
        };
        platform.AddChild(new CollisionShape { Shape = floorShape });
        root.AddChild(character); root.AddChild(platform);
        using var tree = new SceneTree(root);
        tree.PhysicsFrame(1d / 60);
        character.ApplyFloorSnap();
        Check(character.IsOnFloor() && character.GlobalPosition.Y is > 79 and < 82,
            "A forced floor snap moves a nearby character down and stores floor state.");
        platform.Position = new(10, 100);
        tree.PhysicsFrame(1d / 60);
        character.Velocity = Vector2.Zero;
        character.MoveAndSlide();
        Check(character.GlobalPosition.X is > 8 and < 12 && character.IsOnFloor() &&
              character.GetPlatformVelocity().X > 300,
            "A grounded character follows a moving kinematic platform's point velocity.");
        character.PlatformFloorLayers = 0;
        platform.Position = new(20, 100);
        tree.PhysicsFrame(1d / 60);
        var before = character.GlobalPosition.X;
        character.MoveAndSlide();
        Check(MathF.Abs(character.GlobalPosition.X - before) < 1,
            "A zero floor-platform mask prevents platform carry while retaining contact classification.");
        Check(character.GetSlideCollisionCount() > 0,
            "The warmed stationary loop below retains an active floor contact.");
        for (var frame = 0; frame < 64; frame++) character.MoveAndSlide();
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var frame = 0; frame < 64; frame++) character.MoveAndSlide();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        Check(allocated == 0,
            $"Warmed stationary character slide calls allocate no managed bytes: {allocated}.");
    }

    private static void VerifyValidationAndPacking()
    {
        var root = new Node();
        var character = new CharacterBody { Name = "PackedCharacter" };
        root.AddChild(character); character.Owner = root;
        Reject<ArgumentOutOfRangeException>(() => character.GetSlideCollision(0));
        Check(character.GetLastSlideCollision() is null,
            "An unused character has no slide result.");
        Reject<ArgumentOutOfRangeException>(() => character.MaxSlides = 0);
        Reject<ArgumentOutOfRangeException>(() => character.UpDirection = Vector2.Zero);
        Reject<ArgumentOutOfRangeException>(() => character.SafeMargin = -1);
        Reject<ArgumentOutOfRangeException>(() => character.FloorSnapLength = -1);
        Reject<ArgumentOutOfRangeException>(() => character.Velocity = new(float.NaN, 0));
        Reject<ArgumentOutOfRangeException>(() => character.Velocity = new(float.MaxValue, float.MaxValue));
        Reject<ArgumentOutOfRangeException>(() => character.MotionMode = (CharacterMotionMode)42);
        Reject<ArgumentOutOfRangeException>(() => character.PlatformOnLeave = (CharacterPlatformOnLeave)42);
        Check(character.MaxSlides == 4 && character.UpDirection == Vector2.Up &&
              character.SafeMargin == 0.08f && character.FloorSnapLength == 1 &&
              character.Velocity == Vector2.Zero && character.MotionMode == CharacterMotionMode.Grounded,
            "Invalid writes reject before changing character state.");
        character.Velocity = new(120, -30);
        character.MotionMode = CharacterMotionMode.Floating;
        character.PlatformOnLeave = CharacterPlatformOnLeave.DoNothing;
        character.SafeMargin = 0.2f;
        character.FloorStopOnSlope = false;
        character.FloorConstantSpeed = true;
        character.FloorBlockOnWall = false;
        character.SlideOnCeiling = false;
        character.MaxSlides = 6;
        character.FloorMaxAngle = 0.7f;
        character.FloorSnapLength = 5;
        character.WallMinSlideAngle = 0.4f;
        character.UpDirection = new(0, -5);
        character.PlatformFloorLayers = 2;
        character.PlatformWallLayers = 4;
        using var packed = new PackedScene(); packed.Pack(root);
        using var restoredRoot = packed.Instantiate();
        var restored = restoredRoot.GetNode<CharacterBody>("PackedCharacter");
        Check(restored.Velocity == new Vector2(120, -30) &&
              restored.MotionMode == CharacterMotionMode.Floating &&
              restored.PlatformOnLeave == CharacterPlatformOnLeave.DoNothing &&
              restored.SafeMargin == 0.2f && !restored.FloorStopOnSlope &&
              restored.FloorConstantSpeed && !restored.FloorBlockOnWall &&
              !restored.SlideOnCeiling && restored.MaxSlides == 6 &&
              restored.FloorMaxAngle == 0.7f && restored.FloorSnapLength == 5 &&
              restored.WallMinSlideAngle == 0.4f && restored.UpDirection == Vector2.Up &&
              restored.PlatformFloorLayers == 2 && restored.PlatformWallLayers == 4 &&
              !restored.IsOnFloor() && restored.GetSlideCollisionCount() == 0,
            "PackedScene restores the exact character type and all stored options without transient contacts.");
    }

    private static void VerifyFixedLaneBackendSync()
    {
        using var characterShape = new CircleShape();
        using var floorShape = new RectangleShape { Size = new(200, 20) };
        var root = new Node();
        var character = new DrivenCharacter
        {
            Name = "Character",
            Velocity = new(0, 6000)
        };
        character.AddChild(new CollisionShape { Shape = characterShape });
        var floor = new StaticBody { Name = "Floor", Position = new(0, 100) };
        floor.AddChild(new CollisionShape { Shape = floorShape });
        root.AddChild(character); root.AddChild(floor);
        using var tree = new SceneTree(root);
        tree.PhysicsFrame(1d / 60);
        using var point = new PhysicsPointQueryParameters2D
        {
            Position = character.GlobalPosition,
            Exclude = [floor.GetRID()]
        };
        var hits = character.GetWorld2D()!.DirectSpaceState.IntersectPoint(point);
        Check(character.IsOnFloor() && character.GlobalPosition.Y is > 70 and < 85 &&
              hits is [var hit] && hit.ColliderRID == character.GetRID(),
            "A character moving inside the fixed lane leaves its scene pose and solver fixture aligned.");
    }

    private static void VerifyPlatformLeavePolicies()
    {
        var add = DepartPlatform(CharacterPlatformOnLeave.AddVelocity, new(10, 0));
        var none = DepartPlatform(CharacterPlatformOnLeave.DoNothing, new(10, 0));
        var upward = DepartPlatform(CharacterPlatformOnLeave.AddUpwardVelocity, new(0, 10));
        Check(!add.OnFloor && add.Velocity.X > 300 &&
              !none.OnFloor && MathF.Abs(none.Velocity.X) < 1 &&
              !upward.OnFloor && upward.Velocity.Y < -500,
            $"Platform leave policy results: add={add}, none={none}, upward={upward}.");
    }

    private static (bool OnFloor, Vector2 Velocity) DepartPlatform(
        CharacterPlatformOnLeave policy, Vector2 platformShift)
    {
        using var circle = new CircleShape();
        using var floorShape = new RectangleShape { Size = new(200, 20) };
        var root = new Node();
        var character = new CharacterBody
        {
            Name = "Character",
            Position = new(0, 78),
            FloorSnapLength = 5,
            PlatformOnLeave = policy,
            PhysicsProcessEnabled = true
        };
        character.AddChild(new CollisionShape { Shape = circle });
        var platform = new AnimatableBody
        {
            Name = "Platform",
            Position = new(0, 100),
            SyncToPhysics = false
        };
        platform.AddChild(new CollisionShape { Shape = floorShape });
        root.AddChild(character); root.AddChild(platform);
        using var tree = new SceneTree(root);
        tree.PhysicsFrame(1d / 60);
        character.ApplyFloorSnap();
        platform.Position += platformShift;
        tree.PhysicsFrame(1d / 60);
        character.Velocity = platformShift.Y > 0 ? new(0, -1200) : new(0, -600);
        character.MoveAndSlide();
        character.MoveAndSlide();
        return (character.IsOnFloor(), character.Velocity);
    }

    private static void VerifyFloatingSlideThreshold()
    {
        var sliding = FloatTowardWall(0);
        var stopped = FloatTowardWall(Mathf.Pi);
        Check(sliding.Y > stopped.Y + 5 && sliding.Count > 0 && stopped.Count > 0,
            $"Floating wall threshold: sliding={sliding}, stopped={stopped}.");
    }

    private static (float Y, int Count) FloatTowardWall(float threshold)
    {
        using var circle = new CircleShape();
        using var wallShape = new RectangleShape { Size = new(20, 200) };
        var root = new Node();
        var character = new CharacterBody
        {
            Name = "FloatingCharacter",
            MotionMode = CharacterMotionMode.Floating,
            WallMinSlideAngle = threshold,
            Velocity = new(6000, 3000),
            PhysicsProcessEnabled = true
        };
        character.AddChild(new CollisionShape { Shape = circle });
        var wall = new StaticBody { Name = "Wall", Position = new(100, 0) };
        wall.AddChild(new CollisionShape { Shape = wallShape });
        root.AddChild(character); root.AddChild(wall);
        using var tree = new SceneTree(root);
        tree.PhysicsFrame(1d / 60);
        character.MoveAndSlide();
        return (character.GlobalPosition.Y, character.GetSlideCollisionCount());
    }

    private static void VerifyFloorSlopeSpeed()
    {
        var ordinary = SlideUpSlope(constantSpeed: false);
        var constant = SlideUpSlope(constantSpeed: true);
        Check(ordinary.OnFloor && constant.OnFloor &&
              ordinary.X > -80 && constant.X > -80 &&
              ordinary.Y < 100 && constant.Y < 100 &&
              MathF.Abs(ordinary.X - constant.X) > 0.1f,
            $"Floor slope speed variants: ordinary={ordinary}, constant={constant}.");
    }

    private static (float X, float Y, bool OnFloor) SlideUpSlope(bool constantSpeed)
    {
        using var circle = new CircleShape();
        using var slope = new SegmentShape
        {
            A = new(-100, 20),
            B = new(100, -20)
        };
        var root = new Node();
        var character = new CharacterBody
        {
            Name = "Character",
            Position = new(-80, 100),
            FloorSnapLength = 12,
            FloorConstantSpeed = constantSpeed,
            Velocity = new(6000, 0),
            PhysicsProcessEnabled = true
        };
        character.AddChild(new CollisionShape { Shape = circle });
        var floor = new StaticBody { Name = "Slope", Position = new(0, 100) };
        floor.AddChild(new CollisionShape { Shape = slope });
        root.AddChild(character); root.AddChild(floor);
        using var tree = new SceneTree(root);
        tree.PhysicsFrame(1d / 60);
        character.ApplyFloorSnap();
        character.MoveAndSlide();
        return (character.GlobalPosition.X, character.GlobalPosition.Y, character.IsOnFloor());
    }

    private static void VerifyDirectionalClassification()
    {
        var narrow = FallOntoSlope(0.1f);
        var wide = FallOntoSlope(0.5f);
        Check(!narrow.Floor && narrow.Wall && wide.Floor && !wide.Wall,
            $"Floor-angle classification: narrow={narrow}, wide={wide}.");

        using var circle = new CircleShape();
        using var wallShape = new RectangleShape { Size = new(20, 200) };
        var root = new Node();
        var character = new CharacterBody
        {
            Name = "SidewaysCharacter",
            UpDirection = Vector2.Right,
            Velocity = new(-6000, 0),
            PhysicsProcessEnabled = true
        };
        character.AddChild(new CollisionShape { Shape = circle });
        var wall = new StaticBody { Name = "LeftWall", Position = new(-100, 0) };
        wall.AddChild(new CollisionShape { Shape = wallShape });
        root.AddChild(character); root.AddChild(wall);
        using var tree = new SceneTree(root);
        tree.PhysicsFrame(1d / 60);
        character.MoveAndSlide();
        Check(character.IsOnFloor() && character.GetFloorNormal().X > 0.9f,
            "A rightward up direction treats the left wall as a floor.");
    }

    private static (bool Floor, bool Wall) FallOntoSlope(float maxAngle)
    {
        using var circle = new CircleShape();
        using var slope = new SegmentShape { A = new(-100, 20), B = new(100, -20) };
        var root = new Node();
        var character = new CharacterBody
        {
            Name = "Character",
            FloorMaxAngle = maxAngle,
            Velocity = new(0, 6000),
            PhysicsProcessEnabled = true
        };
        character.AddChild(new CollisionShape { Shape = circle });
        var floor = new StaticBody { Name = "Slope", Position = new(0, 100) };
        floor.AddChild(new CollisionShape { Shape = slope });
        root.AddChild(character); root.AddChild(floor);
        using var tree = new SceneTree(root);
        tree.PhysicsFrame(1d / 60);
        character.MoveAndSlide();
        return (character.IsOnFloor(), character.IsOnWall());
    }

    private static void VerifyWallPlatformLayers()
    {
        var ignored = FollowMovingWall(0);
        var followed = FollowMovingWall(1);
        Check(MathF.Abs(ignored) < 1 && followed > 8,
            $"Wall platform masks: ignored={ignored}, followed={followed}.");
    }

    private static float FollowMovingWall(uint layers)
    {
        using var circle = new CircleShape();
        using var wallShape = new RectangleShape { Size = new(20, 200) };
        var root = new Node();
        var character = new CharacterBody
        {
            Name = "Character",
            Velocity = new(6000, 0),
            PlatformWallLayers = layers,
            PhysicsProcessEnabled = true
        };
        character.AddChild(new CollisionShape { Shape = circle });
        var wall = new AnimatableBody
        {
            Name = "MovingWall",
            Position = new(100, 0),
            SyncToPhysics = false
        };
        wall.AddChild(new CollisionShape { Shape = wallShape });
        root.AddChild(character); root.AddChild(wall);
        using var tree = new SceneTree(root);
        tree.PhysicsFrame(1d / 60);
        character.MoveAndSlide();
        wall.Position = new(100, 10);
        tree.PhysicsFrame(1d / 60);
        character.Velocity = Vector2.Zero;
        character.MoveAndSlide();
        return character.GlobalPosition.Y;
    }

    private static void VerifyCharacterGravityArea()
    {
        using var circle = new CircleShape();
        using var fieldShape = new RectangleShape { Size = new(200, 200) };
        var root = new Node();
        var character = new CharacterBody { Name = "Character" };
        character.AddChild(new CollisionShape { Shape = circle });
        var area = new Area
        {
            Name = "Field",
            GravitySpaceOverride = Area.SpaceOverride.Replace,
            Gravity = 400,
            GravityDirection = Vector2.Right
        };
        area.AddChild(new CollisionShape { Shape = fieldShape });
        root.AddChild(character); root.AddChild(area);
        using var tree = new SceneTree(root);
        tree.PhysicsFrame(1d / 60);
        Check(character.GetGravity().X is > 399 and < 401 &&
              MathF.Abs(character.GetGravity().Y) < 1,
            "The inherited gravity query applies a prioritized Area field to a CharacterBody.");
        area.GravitySpaceOverride = Area.SpaceOverride.Disabled;
        tree.PhysicsFrame(1d / 60);
        Check(character.GetGravity().Y is > 979 and < 981,
            "Disabling the field restores the sampled world gravity for a character.");
    }

    private static void VerifyWallBlockingAndSlideLimit()
    {
        var blocked = RunFloorWall(blockOnWall: true);
        var free = RunFloorWall(blockOnWall: false);
        Check(blocked.OnFloor && free.OnFloor &&
              blocked.LastMotion == Vector2.Zero && free.LastMotion.X > 50,
            $"Floor wall blocking: blocked={blocked}, free={free}.");

        var one = RunCornerSlide(1);
        var four = RunCornerSlide(4);
        Check(one == 1 && four >= 2,
            $"Maximum slide count: one={one}, four={four}.");
    }

    private static (bool OnFloor, Vector2 LastMotion) RunFloorWall(bool blockOnWall)
    {
        using var circle = new CircleShape();
        using var floorShape = new RectangleShape { Size = new(200, 20) };
        using var wallShape = new RectangleShape { Size = new(20, 200) };
        var root = new Node();
        var character = new CharacterBody
        {
            Name = "Character",
            Position = new(0, 78),
            FloorSnapLength = 5,
            FloorBlockOnWall = blockOnWall,
            Velocity = new(6000, 0),
            PhysicsProcessEnabled = true
        };
        character.AddChild(new CollisionShape { Shape = circle });
        var floor = new StaticBody { Name = "Floor", Position = new(0, 100) };
        floor.AddChild(new CollisionShape { Shape = floorShape });
        var wall = new StaticBody { Name = "Wall", Position = new(100, 0) };
        wall.AddChild(new CollisionShape { Shape = wallShape });
        root.AddChild(character); root.AddChild(floor); root.AddChild(wall);
        using var tree = new SceneTree(root);
        tree.PhysicsFrame(1d / 60);
        character.ApplyFloorSnap();
        character.MoveAndSlide();
        return (character.IsOnFloor(), character.GetLastMotion());
    }

    private static int RunCornerSlide(int maxSlides)
    {
        using var circle = new CircleShape();
        using var floorShape = new RectangleShape { Size = new(200, 20) };
        using var wallShape = new RectangleShape { Size = new(20, 200) };
        var root = new Node();
        var character = new CharacterBody
        {
            Name = "Character",
            Velocity = new(6000, 6000),
            MaxSlides = maxSlides,
            PhysicsProcessEnabled = true
        };
        character.AddChild(new CollisionShape { Shape = circle });
        var floor = new StaticBody { Name = "Floor", Position = new(0, 100) };
        floor.AddChild(new CollisionShape { Shape = floorShape });
        var wall = new StaticBody { Name = "Wall", Position = new(100, 0) };
        wall.AddChild(new CollisionShape { Shape = wallShape });
        root.AddChild(character); root.AddChild(floor); root.AddChild(wall);
        using var tree = new SceneTree(root);
        tree.PhysicsFrame(1d / 60);
        character.MoveAndSlide();
        return character.GetSlideCollisionCount();
    }

    private static void VerifySlopedCeilingOption()
    {
        var sliding = HitSlopedCeiling(slideOnCeiling: true);
        var stopped = HitSlopedCeiling(slideOnCeiling: false);
        Check(sliding.Ceiling && stopped.Ceiling &&
              MathF.Abs(sliding.Velocity.X - stopped.Velocity.X) > 100,
            $"Sloped ceiling option: sliding={sliding}, stopped={stopped}.");
    }

    private static (bool Ceiling, Vector2 Velocity) HitSlopedCeiling(bool slideOnCeiling)
    {
        using var circle = new CircleShape();
        using var ceilingShape = new SegmentShape
        {
            A = new(-100, -20),
            B = new(100, 20)
        };
        var root = new Node();
        var character = new CharacterBody
        {
            Name = "Character",
            SlideOnCeiling = slideOnCeiling,
            Velocity = new(3000, -6000),
            PhysicsProcessEnabled = true
        };
        character.AddChild(new CollisionShape { Shape = circle });
        var ceiling = new StaticBody { Name = "Ceiling", Position = new(0, -100) };
        ceiling.AddChild(new CollisionShape { Shape = ceilingShape });
        root.AddChild(character); root.AddChild(ceiling);
        using var tree = new SceneTree(root);
        tree.PhysicsFrame(1d / 60);
        character.MoveAndSlide();
        return (character.IsOnCeiling(), character.Velocity);
    }

    private static void VerifyFreeMotionAndLifecycle()
    {
        using var circle = new CircleShape();
        var root = new Node();
        var character = new CharacterBody { Name = "Character", Velocity = new(60, 0) };
        character.AddChild(new CollisionShape { Shape = circle });
        root.AddChild(character);
        using var tree = new SceneTree(root);
        Check(!character.MoveAndSlide() && character.GlobalPosition.X is > 0.9f and < 1.1f &&
              character.GetSlideCollisionCount() == 0 && character.GetLastSlideCollision() is null &&
              MathF.Abs(character.GetRealVelocity().X - 60) < 1,
            "Before the first frame a free character uses the initial fixed delta and returns no contact.");
        tree.PhysicsFrame(1d / 60);
        character.ApplyFloorSnap();
        Check(!character.IsOnFloor(), "Floor snap is inert when no floor is reachable.");
        Reject<InvalidOperationException>(() => Task.Run(character.MoveAndSlide).GetAwaiter().GetResult());
        root.RemoveChild(character);
        Reject<InvalidOperationException>(() => character.MoveAndSlide());
        Check(character.GetGravity() == Vector2.Zero, "Detached characters expose zero inherited gravity.");
        root.AddChild(character);
        Check(!character.IsOnFloor() && character.GetSlideCollisionCount() == 0 &&
              character.Velocity == new Vector2(60, 0),
            "Tree reentry clears transient contacts while retaining stored desired velocity.");
        character.Velocity = Vector2.Zero;
        for (var frame = 0; frame < 64; frame++) character.MoveAndSlide();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var frame = 0; frame < 64; frame++) character.MoveAndSlide();
        Check(GC.GetAllocatedBytesForCurrentThread() - before == 0,
            "Warmed idle character slide calls allocate no managed bytes on the owner thread.");
    }

    private sealed class DrivenCharacter : CharacterBody
    {
        internal DrivenCharacter() => PhysicsProcessEnabled = true;

        protected override void OnPhysicsProcess(double delta)
        {
            MoveAndSlide();
            base.OnPhysicsProcess(delta);
        }
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
