using System.Reflection;
using Electron2D;

internal static partial class PublicServerPerformance
{
    private sealed partial class ServerFixture
    {
        private readonly RID[] _batch = new RID[32];
        private readonly PhysicsCheckpoint?[] _checkpoints = new PhysicsCheckpoint?[32];
        private readonly Vector2[] _collisionPoints = new Vector2[16];
        private readonly Node _associationA = new(), _associationB = new();
        private readonly PhysicsTestMotionParameters _motion = new() { Motion = new(20, 0) };
        private readonly PhysicsTestMotionResult _motionResult = new();
        private Action? _prepareBatch;
        private bool _removeExceptions, _checkCollision, _checkMotion, _motionHit;
        private int _collisionCount;

        internal void PrepareBatch() => _prepareBatch?.Invoke();

        internal Func<int, int>? Special(MethodInfo method)
        {
            switch (method.Name)
            {
                case nameof(PhysicsServer.BodyCreate): return i => { _batch[i] = PhysicsServer.BodyCreate(); return 1; };
                case nameof(PhysicsServer.AreaCreate): return i => { _batch[i] = PhysicsServer.AreaCreate(); return 1; };
                case nameof(PhysicsServer.JointCreate): return i => { _batch[i] = PhysicsServer.JointCreate(); return 1; };
                case nameof(PhysicsServer.CircleShapeCreate): return i => { _batch[i] = PhysicsServer.CircleShapeCreate(); return 1; };
                case nameof(PhysicsServer.CapsuleShapeCreate): return i => { _batch[i] = PhysicsServer.CapsuleShapeCreate(); return 1; };
                case nameof(PhysicsServer.RectangleShapeCreate): return i => { _batch[i] = PhysicsServer.RectangleShapeCreate(); return 1; };
                case nameof(PhysicsServer.SegmentShapeCreate): return i => { _batch[i] = PhysicsServer.SegmentShapeCreate(); return 1; };
                case nameof(PhysicsServer.SeparationRayShapeCreate): return i => { _batch[i] = PhysicsServer.SeparationRayShapeCreate(); return 1; };
                case nameof(PhysicsServer.ConvexPolygonShapeCreate): return i => { _batch[i] = PhysicsServer.ConvexPolygonShapeCreate(); return 1; };
                case nameof(PhysicsServer.ConcavePolygonShapeCreate): return i => { _batch[i] = PhysicsServer.ConcavePolygonShapeCreate(); return 1; };
                case nameof(PhysicsServer.WorldBoundaryShapeCreate): return i => { _batch[i] = PhysicsServer.WorldBoundaryShapeCreate(); return 1; };
                case nameof(PhysicsServer.CustomShapeCreate):
                    var geometry = ShapeGeometry.Custom("benchmark.empty/v1", [], default,
                        new(1, 1, Vector2.Zero), [], EmptyCustomCollision);
                    return i => { _batch[i] = PhysicsServer.CustomShapeCreate(geometry); return 1; };
                case nameof(PhysicsServer.SpaceCreate):
                    if (method.GetParameters().Length == 0) return i => { _batch[i] = PhysicsServer.SpaceCreate(); return 1; };
                    if (method.GetParameters()[0].ParameterType == typeof(string))
                    {
                        var backendName = PhysicsServer.SpaceGetBackendName(Space);
                        return i => { _batch[i] = PhysicsServer.SpaceCreate(backendName); return 1; };
                    }
                    return i => { _batch[i] = PhysicsServer.SpaceCreate(_backend); return 1; };
                case nameof(PhysicsServer.SpaceCreateCheckpoint): return i => { _checkpoints[i] = PhysicsServer.SpaceCreateCheckpoint(Space); return 1; };
                case nameof(PhysicsServer.SpaceStep): return _ => { PhysicsServer.SpaceStep(Space, 1d / 60); return 1; };
                case nameof(PhysicsServer.FreeRID):
                    _prepareBatch = () => PrepareBodies(shaped: true);
                    return i => { PhysicsServer.FreeRID(_batch[i]); _batch[i] = default; return 1; };
                case nameof(PhysicsServer.BodyAddShape):
                    _prepareBatch = () => PrepareBodies(shaped: false);
                    return i => { PhysicsServer.BodyAddShape(_batch[i], _first.GetRID()); return 1; };
                case nameof(PhysicsServer.BodyClearShapes):
                    _prepareBatch = () => PrepareBodies(shaped: true);
                    return i => { PhysicsServer.BodyClearShapes(_batch[i]); return 1; };
                case nameof(PhysicsServer.BodyRemoveShape):
                    _prepareBatch = () => PrepareBodies(shaped: true);
                    return i => { PhysicsServer.BodyRemoveShape(_batch[i], 0); return 1; };
                case nameof(PhysicsServer.AreaAddShape):
                    _prepareBatch = () => PrepareAreas(shaped: false);
                    return i => { PhysicsServer.AreaAddShape(_batch[i], _first.GetRID()); return 1; };
                case nameof(PhysicsServer.AreaClearShapes):
                    _prepareBatch = () => PrepareAreas(shaped: true);
                    return i => { PhysicsServer.AreaClearShapes(_batch[i]); return 1; };
                case nameof(PhysicsServer.AreaRemoveShape):
                    _prepareBatch = () => PrepareAreas(shaped: true);
                    return i => { PhysicsServer.AreaRemoveShape(_batch[i], 0); return 1; };
                case nameof(PhysicsServer.BodyAddCollisionException):
                    _prepareBatch = () => PrepareBodies(shaped: false); _removeExceptions = true;
                    return i => { PhysicsServer.BodyAddCollisionException(Bodies[0], _batch[i]); return 1; };
                case nameof(PhysicsServer.BodyRemoveCollisionException):
                    _prepareBatch = () => { PrepareBodies(shaped: false); foreach (var body in _batch) PhysicsServer.BodyAddCollisionException(Bodies[0], body); };
                    return i => { PhysicsServer.BodyRemoveCollisionException(Bodies[0], _batch[i]); return 1; };
                case nameof(PhysicsServer.BodyAddConstantCentralForce): return i => { PhysicsServer.BodyAddConstantCentralForce(Bodies[0], Force(i)); return 1; };
                case nameof(PhysicsServer.BodyAddConstantForce): return i => { PhysicsServer.BodyAddConstantForce(Bodies[0], Force(i)); return 1; };
                case nameof(PhysicsServer.BodyAddConstantTorque): return i => { PhysicsServer.BodyAddConstantTorque(Bodies[0], Torque(i)); return 1; };
                case nameof(PhysicsServer.BodyApplyCentralForce): return i => { PhysicsServer.BodyApplyCentralForce(Bodies[0], Force(i)); return 1; };
                case nameof(PhysicsServer.BodyApplyForce): return i => { PhysicsServer.BodyApplyForce(Bodies[0], Force(i)); return 1; };
                case nameof(PhysicsServer.BodyApplyCentralImpulse): return i => { PhysicsServer.BodyApplyCentralImpulse(Bodies[0], Force(i)); return 1; };
                case nameof(PhysicsServer.BodyApplyImpulse): return i => { PhysicsServer.BodyApplyImpulse(Bodies[0], Force(i)); return 1; };
                case nameof(PhysicsServer.BodyApplyTorque): return i => { PhysicsServer.BodyApplyTorque(Bodies[0], Torque(i)); return 1; };
                case nameof(PhysicsServer.BodyApplyTorqueImpulse): return i => { PhysicsServer.BodyApplyTorqueImpulse(Bodies[0], Torque(i)); return 1; };
                case nameof(PhysicsServer.BodyResetMassProperties): return _ => { PhysicsServer.BodyResetMassProperties(Bodies[0]); return 1; };
                case nameof(PhysicsServer.BodyAttachObject): return i => { PhysicsServer.BodyAttachObject(Bodies[0], i % 2 == 0 ? _associationA : _associationB); return 1; };
                case nameof(PhysicsServer.AreaAttachObject): return i => { PhysicsServer.AreaAttachObject(_area, i % 2 == 0 ? _associationA : _associationB); return 1; };
                case nameof(PhysicsServer.BodyAttachCanvasInstanceID): return i => { PhysicsServer.BodyAttachCanvasInstanceID(Bodies[0], (ulong)(i % 2 + 1)); return 1; };
                case nameof(PhysicsServer.AreaAttachCanvasInstanceID): return i => { PhysicsServer.AreaAttachCanvasInstanceID(_area, (ulong)(i % 2 + 1)); return 1; };
                case nameof(PhysicsServer.JointClear):
                    _prepareBatch = () => { for (var i = 0; i < _batch.Length; i++) { _batch[i] = PhysicsServer.JointCreate(); PhysicsServer.JointMakePin(_batch[i], Vector2.Zero, Bodies[0], Bodies[1]); } };
                    return i => { PhysicsServer.JointClear(_batch[i]); return 1; };
                case nameof(PhysicsServer.JointMakePin): return i => { PhysicsServer.JointMakePin(_pin, new(i % 2, 0), Bodies[0], Bodies[1]); return 1; };
                case nameof(PhysicsServer.JointMakeDampedSpring): return i => { PhysicsServer.JointMakeDampedSpring(_spring, new(i % 2, 0), new(20, 0), Bodies[0], Bodies[1]); return 1; };
                case nameof(PhysicsServer.JointMakeGroove): return i => { PhysicsServer.JointMakeGroove(_pin, Vector2.Zero, new(0, 20), new(20, i % 2), Bodies[0], Bodies[1]); return 1; };
                case nameof(PhysicsServer.BodyTestMotion):
                    PhysicsServer.JointClear(_pin); PhysicsServer.JointClear(_spring);
                    PhysicsServer.BodySetMode(Bodies[1], PhysicsServer.BodyMode.Static);
                    PhysicsServer.BodySetTransform(Bodies[1], new(0, new(10, 0)));
                    _motion.From = PhysicsServer.BodyGetTransform(Bodies[0]);
                    _checkMotion = true;
                    return i => { _motionHit = PhysicsServer.BodyTestMotion(Bodies[0], _motion, _motionResult); return 1; };
                case nameof(PhysicsServer.ShapeCollide):
                    _checkCollision = true;
                    return i => { PhysicsServer.ShapeCollide(Space, _first.GetRID(), Transform.Identity, Vector2.Zero, _second.GetRID(), new(0, new(1, 0)), Vector2.Zero, _collisionPoints, out _collisionCount); return 1; };
                case nameof(PhysicsServer.BodyCollideShape):
                    _checkCollision = true;
                    return i => { PhysicsServer.BodyCollideShape(Bodies[0], 0, _second.GetRID(), new(0, new(1, 0)), Vector2.Zero, _collisionPoints, out _collisionCount); return 1; };
                default: return null;
            }
        }

        private static Vector2 Force(int index) => new(index % 2 == 0 ? .01f : -.01f, 0);
        private static float Torque(int index) => index % 2 == 0 ? .01f : -.01f;

        private static bool EmptyCustomCollision(ShapeGeometry first, Transform firstPose, Vector2 firstMotion,
            ShapeGeometry second, Transform secondPose, Vector2 secondMotion, Span<Vector2> contacts, out int count)
        { count = 0; return false; }

        private void PrepareBodies(bool shaped)
        {
            for (var i = 0; i < _batch.Length; i++)
            {
                var body = _batch[i] = PhysicsServer.BodyCreate();
                PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static);
                PhysicsServer.BodySetTransform(body, new(0, new(1000 + i * 20, 0)));
                if (shaped) PhysicsServer.BodyAddShape(body, _first.GetRID());
                PhysicsServer.BodySetSpace(body, Space);
            }
        }

        private void PrepareAreas(bool shaped)
        {
            for (var i = 0; i < _batch.Length; i++)
            {
                var area = _batch[i] = PhysicsServer.AreaCreate();
                if (shaped) PhysicsServer.AreaAddShape(area, _first.GetRID());
                PhysicsServer.AreaSetSpace(area, Space);
            }
        }

        private void CleanupSpecial()
        {
            for (var i = 0; i < _batch.Length; i++)
            {
                _checkpoints[i]?.Dispose(); _checkpoints[i] = null;
                if (!_batch[i].IsValid()) continue;
                if (_removeExceptions) PhysicsServer.BodyRemoveCollisionException(Bodies[0], _batch[i]);
                PhysicsServer.FreeRID(_batch[i]); _batch[i] = default;
            }
            if (_checkCollision && _collisionCount == 0) throw new InvalidOperationException("Prepared public collision inputs must overlap.");
            if (_checkMotion && (!_motionHit || _motionResult.GetColliderRID() != Bodies[1] || !_motionResult.GetTravel().IsFinite()))
                throw new InvalidOperationException("Prepared public body motion must reach the retained collider.");
        }

        private void DisposeSpecial()
        {
            _associationA.Dispose(); _associationB.Dispose(); _motion.Dispose(); _motionResult.Dispose();
        }
    }
}
