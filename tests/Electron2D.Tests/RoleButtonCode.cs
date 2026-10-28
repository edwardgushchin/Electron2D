using System.Runtime.CompilerServices;
using Electron2D;

internal sealed class RoleButtonCode
{
    internal static readonly string SourcePath = Source();
    internal sealed class State { internal RID Element; internal int FocusQueries, Retired, Clears; internal bool Invalid; }
    private static readonly ConditionalWeakTable<Button, State> States = new();
    internal static State Get(Button owner) => States.GetValue(owner, static _ => throw new InvalidOperationException("No button hook state."));
    internal static void Bind(Button owner)
    {
        var state = new State(); States.Add(owner, state);
        owner.SetScriptCallbacks(new NodeScriptCallbacks<Button>
        {
            Notification = (button, what, _) =>
            {
                if (what == Node.NotificationAccessibilityUpdate && !AccessibilityServer.HasElement(state.Element))
                { state.Element = AccessibilityServer.CreateSubElement(button.GetAccessibilityElement(), AccessibilityServer.AccessibilityRole.Button); AccessibilityServer.UpdateSetName(state.Element, "Script focus target"); }
            },
            GetConfigurationWarnings = _ => ["button role"],
            GetAccessibilityConfigurationWarnings = _ => ["button accessibility"],
            GetFocusedAccessibilityElement = _ => { state.FocusQueries++; return state.Invalid ? default : state.Element; },
            Retired = _ => state.Retired++
        });
    }
    internal static void Clear(Button owner) { if (States.TryGetValue(owner, out var state)) { state.Clears++; States.Remove(owner); } }
    private static string Source([CallerFilePath] string path = "") => path;
}
