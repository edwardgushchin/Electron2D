namespace Electron2D;

public sealed partial class RenderingServer
{
    /// <summary>Selects how an item composites its same-Z render descendants.</summary>
    public enum CanvasGroupMode
    {
        /// <summary>Draws ordinary parent and child commands without group compositing.</summary>
        Disabled = 0,
        /// <summary>Uses the parent drawing as an alpha mask without drawing its color.</summary>
        ClipOnly = 1,
        /// <summary>Draws parent color under descendants and uses its drawing as an alpha mask.</summary>
        ClipAndDraw = 2,
        /// <summary>Draws descendants into a transparent backbuffer and composites them once.</summary>
        Transparent = 3
    }
    internal readonly record struct CanvasGroupSettings(CanvasGroupMode Mode, float ClearMargin, bool FitEmpty, float FitMargin, bool BlurMIPMaps);

    /// <summary>Configures same-Z render-descendant masking or transparent group composition.</summary>
    /// <param name="item">A live scene or caller-owned canvas item.</param>
    /// <param name="mode">The enabled group policy, or Disabled for ordinary drawing.</param>
    /// <param name="clearMargin">Finite nonnegative screen-pixel expansion of backbuffer preparation bounds.</param>
    /// <param name="fitEmpty">Whether an item without commands gets a rectangle fitted around its descendants.</param>
    /// <param name="fitMargin">Finite nonnegative screen-pixel expansion of that fitted rectangle.</param>
    /// <param name="blurMIPMaps">Whether to generate backbuffer mipmaps before the final composite; requires GPU rendering.</param>
    /// <remarks>Renderer-session overrides preserve authored getters and follow logical reparenting.
    /// Matching scene setters republish their policy. Nested same-Z groups require independent backbuffers and reject before native submission.</remarks>
    /// <exception cref="ArgumentException">The item identity is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The mode is undefined or a margin is negative/nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The renderer is unavailable or its owner/capture/lifetime rules reject mutation.</exception>
    public static void CanvasItemSetCanvasGroupMode(RID item, CanvasGroupMode mode, float clearMargin = 5, bool fitEmpty = false, float fitMargin = 0, bool blurMIPMaps = false) =>
        RequireService().CanvasItemSetCanvasGroupModeCore(item, mode, clearMargin, fitEmpty, fitMargin, blurMIPMaps);

    private void CanvasItemSetCanvasGroupModeCore(RID item, CanvasGroupMode mode, float clearMargin, bool fitEmpty, float fitMargin, bool blurMIPMaps)
    {
        if ((uint)mode > (uint)CanvasGroupMode.Transparent) throw new ArgumentOutOfRangeException(nameof(mode));
        if (!float.IsFinite(clearMargin) || clearMargin < 0) throw new ArgumentOutOfRangeException(nameof(clearMargin));
        if (!float.IsFinite(fitMargin) || fitMargin < 0) throw new ArgumentOutOfRangeException(nameof(fitMargin));
        Change(item, out _).Group = new(mode, clearMargin, fitEmpty, fitMargin, blurMIPMaps);
    }
    private static CanvasGroupSettings GroupSettings(CanvasItem item)
    {
        if (item.ServerState?.Group is { } state) return state;
        if (item is CanvasGroup group) return new(CanvasGroupMode.Transparent, group.ClearMargin, true, group.FitMargin, group.UseMIPMaps);
        return new(item.ClipChildren switch { ClipChildrenMode.Only => CanvasGroupMode.ClipOnly, ClipChildrenMode.AndDraw => CanvasGroupMode.ClipAndDraw, _ => CanvasGroupMode.Disabled }, 0, false, 0, false);
    }
}
