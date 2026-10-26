namespace Electron2D;

public sealed partial class AccessibilityServer
{
    /// <summary>Publishes a human-readable direct keyboard shortcut for the element's activation action.</summary>
    /// <param name="id">Live element.</param><param name="shortcut">Shortcut text; empty clears it.</param>
    /// <remarks>Native Action keybindings put this text in the third, direct-shortcut field of Click or the first/default action. It does not install an input binding.</remarks>
    /// <exception cref="ArgumentNullException">Text is null.</exception><exception cref="NotSupportedException">No bridge is active.</exception><exception cref="KeyNotFoundException">The element is absent or retired.</exception><exception cref="InvalidOperationException">Access is off-owner.</exception>
    public static void UpdateSetShortcut(RID id, string shortcut)
    {
        ArgumentNullException.ThrowIfNull(shortcut); var e = Require().Get(id); e.ExplicitShortcut = true; if (e.Shortcut == shortcut) return;
        e.Shortcut = shortcut; Runtime._native!.ActionsChanged(e);
    }
    /// <summary>Publishes the producer's class name through native Accessible attributes.</summary>
    /// <param name="id">Live element.</param><param name="className">Class name; empty removes it.</param>
    /// <exception cref="ArgumentNullException">Text is null.</exception><exception cref="NotSupportedException">No bridge is active.</exception><exception cref="KeyNotFoundException">The element is absent or retired.</exception><exception cref="InvalidOperationException">Access is off-owner.</exception>
    public static void UpdateSetClassName(RID id, string className)
    {
        ArgumentNullException.ThrowIfNull(className); var e = Require().Get(id); if (e.ClassName == className) return;
        e.ClassName = className; Runtime._native!.SemanticAttributesChanged(e);
    }
    /// <summary>Publishes an author-localized role description without changing the role taxonomy.</summary>
    /// <param name="id">Live element.</param><param name="description">Role description; empty removes it.</param>
    /// <exception cref="ArgumentNullException">Text is null.</exception><exception cref="NotSupportedException">No bridge is active.</exception><exception cref="KeyNotFoundException">The element is absent or retired.</exception><exception cref="InvalidOperationException">Access is off-owner.</exception>
    public static void UpdateSetRoleDescription(RID id, string description)
    {
        ArgumentNullException.ThrowIfNull(description); var e = Require().Get(id); if (e.RoleDescription == description) return;
        e.RoleDescription = description; Runtime._native!.SemanticAttributesChanged(e);
    }
    internal static bool UpdateSourceContext(Element e, Control control)
    {
        var tree = control.Tree!;
        var parent = control.GetParentControl(); var generation = parent is null ? 0 : SourceElement(parent)?.SourceContextGeneration ?? 0;
        var path = tree.PathRevision; var locale = TranslationServer.Culture.Name;
        if (e.SourceContextPrepared && !control.AccessibilityQueued && e.SourceContextParent == parent && e.SourceParentContextGeneration == generation && e.SourceContextPath == path && e.SourceContextLocale == locale) return false;
        var container = parent?.AccessibleContainerName(control) ?? "";
        if (e.Retired || control.IsDisposed || control.IsDepartingTree || !ReferenceEquals(control.Tree, tree)) return false;
        var help = control.AccessibleContextualInfo();
        if (e.Retired || control.IsDisposed || control.IsDepartingTree || !ReferenceEquals(control.Tree, tree)) return false;
        var changed = e.ContainerContext != container; e.ContainerContext = container;
        if (e.HelpText != help) { e.HelpText = help; Runtime._native!.PropertyChanged(e, "accessible-help-text", help); }
        if (!e.SourceContextPrepared) UpdateSetClassName(e.ID, control.GetType().Name);
        e.SourceContextParent = parent; e.SourceParentContextGeneration = generation; e.SourceContextPath = path; e.SourceContextLocale = locale;
        e.SourceContextPrepared = true; e.SourceContextGeneration++;
        return changed;
    }
    internal static void UpdateSourceName(Element e, string name, bool contextChanged)
    {
        if (e.Name == name && e.IncludeContainerContext && !contextChanged) return;
        e.Name = name; e.IncludeContainerContext = true; Runtime._native!.PropertyChanged(e, "accessible-name", name);
    }
}
