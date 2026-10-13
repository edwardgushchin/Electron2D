using Electron2D;

internal static class AnimationNodeExtensionTests
{
    internal static void Run()
    {
        Reject(() => new AnimationNodePlaybackInfo(double.NaN, 0, 0, -1, false, false, Animation.LoopedFlag.None, 1));
        Reject(() => new AnimationNodeTimeInfo(-1, 0, 0));
        Reject(() => AnimationNodeExtension.GetRemainingTime(new(double.MaxValue, -double.MaxValue, 0)));
        var loop = new AnimationNodeTimeInfo(2, .5, .25, SpriteFrames.LoopMode.Linear);
        Check(AnimationNodeExtension.IsLooping(loop) && AnimationNodeExtension.GetRemainingTime(loop) == 31540000 && AnimationNodeExtension.GetRemainingTime(loop, true) == 1.5, "Typed loop helpers.");
        Check(AnimationNodeExtension.GetRemainingTime(new(2, 3, 0)) == -1 && AnimationNodeExtension.GetRemainingTime(new(2, 2, 0, SpriteFrames.LoopMode.Linear, true), true) == 0 && AnimationNodeExtension.GetRemainingTime(new(2, 0, 0, isInfinite: true), true) == 31540000, "End, overshoot and infinite policies.");
        using var probe = new Probe(); using var graph = new AnimationNodeBlendTree(); graph.AddNode("probe", probe); graph.ConnectNode("output", 0, "probe");
        var root = new Node(); var controller = new AnimationTree { TreeRoot = graph, CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual }; root.AddChild(controller); using var scene = new SceneTree(root);
        controller.Advance(0); Check(probe.Input.Time == 0 && probe.Input.Delta == 0 && probe.Input.Start == 0 && probe.Input.End == -1 && probe.Input.Seeked && !probe.Input.IsExternalSeeking && probe.Input.Weight == 1 && !probe.TestOnly, "All initial playback input fields.");
        controller.Advance(.5); Check(probe.Input.Time == .5 && probe.Input.Delta == .5 && probe.Input.End == 2 && !probe.Input.Seeked && probe.Input.LoopedFlag == Animation.LoopedFlag.None, "Prior timeline bounds and ordinary delta.");
        controller.Advance(2); Check(probe.Input.LoopedFlag == Animation.LoopedFlag.End && controller.GetParameter("probe", AnimationNode.CurrentPosition) == .5, "Crossing and returned timeline commit.");
        probe.Fail = true; try { controller.Advance(.25); throw new InvalidOperationException("Expected callback failure."); } catch (ApplicationException) { }
        Check(controller.GetParameter("probe", AnimationNode.CurrentPosition) == .5, "Failed callback preserves committed timeline."); probe.Fail = false;
        for (var i = 0; i < 100; i++) controller.Advance(.001);
        var allocated = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 1000; i++) controller.Advance(.001); Check(GC.GetAllocatedBytesForCurrentThread() == allocated, "Typed extension has zero warmed managed allocations.");
        using var other = new Probe(); using var subtraction = new AnimationNodeSub2 { Sync = true }; using var weighted = new AnimationNodeBlendTree();
        weighted.AddNode("base", other); weighted.AddNode("probe", probe); weighted.AddNode("subtract", subtraction); weighted.ConnectNode("subtract", 0, "base"); weighted.ConnectNode("subtract", 1, "probe"); weighted.ConnectNode("output", 0, "subtract");
        controller.TreeRoot = weighted; controller.SetParameter("subtract", AnimationNodeSub2.SubAmount, .5); controller.Advance(0); Check(probe.Input.Weight == -.5 && other.Input.Weight == 1, "Signed incoming weight reaches typed custom nodes.");
        Console.WriteLine("Modern graph extension: typed playback/timeline, loop/end/infinite policies, callback rollback and zero warmed allocations passed.");
    }
    private sealed class Probe : AnimationNodeExtension
    {
        internal AnimationNodePlaybackInfo Input; internal bool TestOnly, Fail;
        protected override AnimationNodeTimeInfo OnProcessAnimationNode(in AnimationNodePlaybackInfo playbackInfo, bool testOnly)
        { Input = playbackInfo; TestOnly = testOnly; if (Fail) throw new ApplicationException("fixture"); return new(2, ((playbackInfo.Time % 2) + 2) % 2, playbackInfo.Delta, SpriteFrames.LoopMode.Linear); }
    }
    private static void Reject(Action action) { try { action(); } catch (ArgumentOutOfRangeException) { return; } throw new InvalidOperationException("Expected invalid typed timeline."); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
