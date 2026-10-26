namespace Electron2D;

public sealed partial class AccessibilityServer
{
    /// <summary>Registers the integer-ID callback for custom native actions.</summary>
    /// <param name="id">Live element.</param><param name="action">Custom.</param><param name="callback">Borrowed compiled callback receiving the published custom ID.</param>
    /// <exception cref="ArgumentNullException">Callback is null.</exception><exception cref="ArgumentOutOfRangeException">The selector is not Custom.</exception>
    /// <exception cref="NotSupportedException">No bridge is active.</exception><exception cref="KeyNotFoundException">The identity is absent or retired.</exception><exception cref="InvalidOperationException">Access is off-owner.</exception>
    public static void UpdateAddAction(RID id, AccessibilityAction action, Action<int> callback)
    {
        ArgumentNullException.ThrowIfNull(callback); if (action != AccessibilityAction.Custom) throw new ArgumentOutOfRangeException(nameof(action));
        var e = Require().Get(id); e.CustomAction = callback; Runtime._native!.ActionsChanged(e);
    }
    /// <summary>Adds or updates a custom action's ID and native description.</summary>
    /// <param name="id">Live element.</param><param name="actionID">Exact signed integer delivered to the custom callback.</param><param name="actionDescription">Native action name/description; empty leaves publication unchanged.</param>
    /// <remarks>IDs retain insertion order; updating an existing ID preserves its ordinal. A registered Custom callback owns execution.</remarks>
    /// <exception cref="ArgumentNullException">Description is null.</exception><exception cref="NotSupportedException">No bridge is active.</exception><exception cref="KeyNotFoundException">The identity is absent or retired.</exception><exception cref="InvalidOperationException">Access is off-owner.</exception>
    public static void UpdateAddCustomAction(RID id, int actionID, string actionDescription)
    {
        ArgumentNullException.ThrowIfNull(actionDescription); var e = Require().Get(id); if (actionDescription.Length == 0) return;
        e.CustomActions ??= [];
        for (var i = 0; i < e.CustomActions.Count; i++)
            if (e.CustomActions[i].ID == actionID)
            { if (e.CustomActions[i].Description == actionDescription) return; e.CustomActions[i] = (actionID, actionDescription); Runtime._native!.ActionsChanged(e); return; }
        e.CustomActions.Add((actionID, actionDescription)); Runtime._native!.ActionsChanged(e);
    }
    /// <summary>Publishes supplementary context appended to the native accessible name.</summary>
    /// <param name="id">Live element.</param><param name="name">Context; empty removes it.</param>
    /// <remarks>The base name stays independent; changing either field republishes their space-separated label.</remarks>
    /// <exception cref="ArgumentNullException">Context is null.</exception><exception cref="NotSupportedException">No bridge is active.</exception><exception cref="KeyNotFoundException">The identity is absent or retired.</exception><exception cref="InvalidOperationException">Access is off-owner.</exception>
    public static void UpdateSetExtraInfo(RID id, string name)
    {
        ArgumentNullException.ThrowIfNull(name); var e = Require().Get(id); if (e.ExtraInfo == name) return; e.ExtraInfo = name;
        Runtime._native!.PropertyChanged(e, "accessible-name", e.Name);
    }
}
