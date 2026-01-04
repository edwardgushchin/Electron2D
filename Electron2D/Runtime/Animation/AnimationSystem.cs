namespace Electron2D;

internal sealed class AnimationSystem
{
    public void Process(SceneTree sceneTree, float deltaSeconds)
    {
        ArgumentNullException.ThrowIfNull(sceneTree);

        var animators = sceneTree.SpriteAnimators;
        for (var i = 0; i < animators.Length; i++)
            animators[i].InternalTick(deltaSeconds);
    }
}