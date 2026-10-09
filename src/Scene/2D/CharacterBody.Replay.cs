namespace Electron2D;

public partial class CharacterBody
{
    internal sealed class CharacterReplay
    {
        internal readonly record struct Configuration(CharacterMotionMode _motionMode, CharacterPlatformOnLeave _platformOnLeave, float _safeMargin, bool _floorStopOnSlope, bool _floorConstantSpeed, bool _floorBlockOnWall, bool _slideOnCeiling, int _maxSlides, float _floorMaxAngle, float _floorSnapLength, float _wallMinSlideAngle, Vector2 _upDirection, uint _platformFloorLayers, uint _platformWallLayers);
        internal Configuration Settings;
        internal Vector2 _velocity;
        internal Vector2 _floorNormal;
        internal Vector2 _wallNormal;
        internal Vector2 _platformVelocity;
        internal Vector2 _lastMotion;
        internal Vector2 _previousPosition;
        internal Vector2 _realVelocity;
        internal Vector2 _resolvedGravity;
        internal RID _platformRID;
        internal uint _platformLayer;
        internal bool _onFloor;
        internal bool _onWall;
        internal bool _onCeiling;
        internal Transform _solverPose;
        internal bool _queryPoseApplied;
        internal readonly List<MotionResultData> Slides = [];
    }
    private CharacterReplay.Configuration ReplayConfiguration() => new(_motionMode, _platformOnLeave, _safeMargin, _floorStopOnSlope, _floorConstantSpeed, _floorBlockOnWall, _slideOnCeiling, _maxSlides, _floorMaxAngle, _floorSnapLength, _wallMinSlideAngle, _upDirection, _platformFloorLayers, _platformWallLayers);
    internal void CaptureReplay(CharacterReplay state)
    {
        PhysicsReplayCopy.Require(!_queryPoseApplied);
        state.Settings = ReplayConfiguration();
        state._velocity = _velocity;
        state._floorNormal = _floorNormal;
        state._wallNormal = _wallNormal;
        state._platformVelocity = _platformVelocity;
        state._lastMotion = _lastMotion;
        state._previousPosition = _previousPosition;
        state._realVelocity = _realVelocity;
        state._resolvedGravity = _resolvedGravity;
        state._platformRID = _platformRID;
        state._platformLayer = _platformLayer;
        state._onFloor = _onFloor;
        state._onWall = _onWall;
        state._onCeiling = _onCeiling;
        state._solverPose = _solverPose;
        state._queryPoseApplied = _queryPoseApplied;
        PhysicsReplayCopy.List(_slideResults, state.Slides);
    }
    internal void ValidateReplay(CharacterReplay state)
    {
        PhysicsReplayCopy.Require(state.Settings == ReplayConfiguration());
        PhysicsReplayCopy.Require(!_queryPoseApplied);
    }
    internal void RestoreReplay(CharacterReplay state)
    {
        _velocity = state._velocity;
        _floorNormal = state._floorNormal;
        _wallNormal = state._wallNormal;
        _platformVelocity = state._platformVelocity;
        _lastMotion = state._lastMotion;
        _previousPosition = state._previousPosition;
        _realVelocity = state._realVelocity;
        _resolvedGravity = state._resolvedGravity;
        _platformRID = state._platformRID;
        _platformLayer = state._platformLayer;
        _onFloor = state._onFloor;
        _onWall = state._onWall;
        _onCeiling = state._onCeiling;
        _solverPose = state._solverPose;
        _queryPoseApplied = state._queryPoseApplied;
        PhysicsReplayCopy.List(state.Slides, _slideResults);
    }
}
