namespace Electron2D;

public sealed partial class AccessibilityServer
{
    internal enum RelationKind { Controls, DescribedBy, Details, FlowTo, LabeledBy, RadioGroup, MemberOf, ErrorMessage, LinkTarget, ActiveDescendant }
    internal readonly record struct Relation(RelationKind Kind, Element Target, bool Automatic = false);
    private readonly List<Element> _activeOwners = [];
    private readonly Dictionary<int, Element> _focusOwners = [];
    internal static ulong ElementRevision => Runtime._elementRevision;
    private ulong _elementRevision;
    /// <summary>Adds a non-owning reference to an element that is controlled by this element.</summary>
    /// <param name="id">The producer.</param><param name="relatedID">A live target in the same native window.</param>
    /// <remarks>Repeated targets are deduplicated. References retire with either element; they never transfer lifetime.</remarks>
    /// <exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">An identity is absent, foreign or retired.</exception>
    /// <exception cref="ArgumentException">Elements belong to different native windows.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    /// <exception cref="AggregateException">Native notifications fail after the graph change commits.</exception>
    public static void UpdateAddRelatedControls(RID id, RID relatedID)
    {
        var r = Require(); var owner = r.Get(id); var target = r.RelationTarget(owner, relatedID);
        r.AddRelation(owner, new(RelationKind.Controls, target!));
    }

    /// <summary>Adds a non-owning reference to an element that describes this element.</summary>
    /// <param name="id">The producer.</param><param name="relatedID">A live target in the same native window.</param>
    /// <remarks>Repeated targets are deduplicated. References retire with either element; they never transfer lifetime.</remarks>
    /// <exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">An identity is absent, foreign or retired.</exception>
    /// <exception cref="ArgumentException">Elements belong to different native windows.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    /// <exception cref="AggregateException">Native notifications fail after the graph change commits.</exception>
    public static void UpdateAddRelatedDescribedBy(RID id, RID relatedID)
    {
        var r = Require(); var owner = r.Get(id); var target = r.RelationTarget(owner, relatedID);
        r.AddRelation(owner, new(RelationKind.DescribedBy, target!));
    }

    /// <summary>Adds a non-owning reference to an element that provides details about this element.</summary>
    /// <param name="id">The producer.</param><param name="relatedID">A live target in the same native window.</param>
    /// <remarks>Repeated targets are deduplicated. References retire with either element; they never transfer lifetime.</remarks>
    /// <exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">An identity is absent, foreign or retired.</exception>
    /// <exception cref="ArgumentException">Elements belong to different native windows.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    /// <exception cref="AggregateException">Native notifications fail after the graph change commits.</exception>
    public static void UpdateAddRelatedDetails(RID id, RID relatedID)
    {
        var r = Require(); var owner = r.Get(id); var target = r.RelationTarget(owner, relatedID);
        r.AddRelation(owner, new(RelationKind.Details, target!));
    }

    /// <summary>Adds a non-owning reference to an element that follows this element in the semantic reading flow.</summary>
    /// <param name="id">The producer.</param><param name="relatedID">A live target in the same native window.</param>
    /// <remarks>Repeated targets are deduplicated. References retire with either element; they never transfer lifetime.</remarks>
    /// <exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">An identity is absent, foreign or retired.</exception>
    /// <exception cref="ArgumentException">Elements belong to different native windows.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    /// <exception cref="AggregateException">Native notifications fail after the graph change commits.</exception>
    public static void UpdateAddRelatedFlowTo(RID id, RID relatedID)
    {
        var r = Require(); var owner = r.Get(id); var target = r.RelationTarget(owner, relatedID);
        r.AddRelation(owner, new(RelationKind.FlowTo, target!));
    }

    /// <summary>Adds a non-owning reference to an element that labels this element.</summary>
    /// <param name="id">The producer.</param><param name="relatedID">A live target in the same native window.</param>
    /// <remarks>Repeated targets are deduplicated. References retire with either element; they never transfer lifetime.</remarks>
    /// <exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">An identity is absent, foreign or retired.</exception>
    /// <exception cref="ArgumentException">Elements belong to different native windows.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    /// <exception cref="AggregateException">Native notifications fail after the graph change commits.</exception>
    public static void UpdateAddRelatedLabeledBy(RID id, RID relatedID)
    {
        var r = Require(); var owner = r.Get(id); var target = r.RelationTarget(owner, relatedID);
        r.AddRelation(owner, new(RelationKind.LabeledBy, target!));
    }

    /// <summary>Adds a non-owning reference to an element that is another member of the same radio group.</summary>
    /// <param name="id">The producer.</param><param name="relatedID">A live target in the same native window.</param>
    /// <remarks>Repeated targets are deduplicated. References retire with either element; they never transfer lifetime.</remarks>
    /// <exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">An identity is absent, foreign or retired.</exception>
    /// <exception cref="ArgumentException">Elements belong to different native windows.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    /// <exception cref="AggregateException">Native notifications fail after the graph change commits.</exception>
    public static void UpdateAddRelatedRadioGroup(RID id, RID relatedID)
    {
        var r = Require(); var owner = r.Get(id); var target = r.RelationTarget(owner, relatedID);
        r.AddRelation(owner, new(RelationKind.RadioGroup, target!));
    }

    /// <summary>Sets the single semantic group containing this element.</summary>
    /// <param name="id">The producer.</param><param name="groupID">A live same-window target, or an empty RID to clear.</param>
    /// <remarks>This is a non-owning relationship. Replacing or retiring the target removes its old reference.</remarks>
    /// <exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">An identity is absent, foreign or retired.</exception>
    /// <exception cref="ArgumentException">Windows differ, or an active target is not a strict descendant.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    /// <exception cref="AggregateException">Native notifications fail after the graph change commits.</exception>
    public static void UpdateSetMemberOf(RID id, RID groupID)
    {
        var r = Require(); var owner = r.Get(id); var target = r.RelationTarget(owner, groupID, allowEmpty: true);
        r.SetRelation(owner, RelationKind.MemberOf, target);
    }

    /// <summary>Sets the element containing this element's validation error and updates native invalid-entry state.</summary>
    /// <param name="id">The producer.</param><param name="otherID">A live same-window target, or an empty RID to clear.</param>
    /// <remarks>This is a non-owning relationship. Replacing or retiring the target removes its old reference.</remarks>
    /// <exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">An identity is absent, foreign or retired.</exception>
    /// <exception cref="ArgumentException">Windows differ, or an active target is not a strict descendant.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    /// <exception cref="AggregateException">Native notifications fail after the graph change commits.</exception>
    public static void UpdateSetErrorMessage(RID id, RID otherID)
    {
        var r = Require(); var owner = r.Get(id); var target = r.RelationTarget(owner, otherID, allowEmpty: true);
        r.SetRelation(owner, RelationKind.ErrorMessage, target);
    }

    /// <summary>Sets the single native Hyperlink anchor for an internal link.</summary>
    /// <param name="id">The producer.</param><param name="otherID">A live same-window target, or an empty RID to clear.</param>
    /// <remarks>This is a non-owning relationship. Replacing or retiring the target removes its old reference.</remarks>
    /// <exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">An identity is absent, foreign or retired.</exception>
    /// <exception cref="ArgumentException">Windows differ, or an active target is not a strict descendant.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    /// <exception cref="AggregateException">Native notifications fail after the graph change commits.</exception>
    public static void UpdateSetInPageLinkTarget(RID id, RID otherID)
    {
        var r = Require(); var owner = r.Get(id); var target = r.RelationTarget(owner, otherID, allowEmpty: true);
        r.SetRelation(owner, RelationKind.LinkTarget, target);
    }

    /// <summary>Sets the strict semantic descendant selected by this composite's native focus.</summary>
    /// <param name="id">The producer.</param><param name="otherID">A live same-window target, or an empty RID to clear.</param>
    /// <remarks>This is a non-owning relationship. Replacing or retiring the target removes its old reference.</remarks>
    /// <exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">An identity is absent, foreign or retired.</exception>
    /// <exception cref="ArgumentException">Windows differ, or an active target is not a strict descendant.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    /// <exception cref="AggregateException">Native notifications fail after the graph change commits.</exception>
    public static void UpdateSetActiveDescendant(RID id, RID otherID)
    {
        var r = Require(); var owner = r.Get(id); var target = r.RelationTarget(owner, otherID, allowEmpty: true);
        if (target is not null && !IsDescendant(owner, target)) throw new ArgumentException("The active element must be a strict semantic descendant.", nameof(otherID));
        r.SetRelation(owner, RelationKind.ActiveDescendant, target);
    }
    private Element? RelationTarget(Element owner, RID id, bool allowEmpty = false)
    {
        if (allowEmpty && !id.IsValid()) return null;
        var target = Get(id); if (target.WindowID != owner.WindowID) throw new ArgumentException("Related elements belong to different native windows.", nameof(id));
        return target;
    }
    private static bool IsDescendant(Element owner, Element target)
    {
        for (var parent = target.Parent; parent is not null; parent = parent.Parent) if (parent == owner) return true;
        return false;
    }
    internal static Element? RelatedTarget(Element owner, RelationKind kind)
    {
        foreach (var relation in owner.Relations) if (relation.Kind == kind && !relation.Target.Retired) return relation.Target;
        return null;
    }
    private static bool HasTarget(Element owner, Element target)
    {
        foreach (var relation in owner.Relations) if (relation.Target == target) return true;
        return false;
    }
    private void AddRelation(Element owner, Relation relation)
    {
        if (owner.Relations.Contains(relation)) return;
        var visible = false;
        foreach (var existing in owner.Relations) if (existing.Kind == relation.Kind && existing.Target == relation.Target) { visible = true; break; }
        owner.Relations.Add(relation); relation.Target.RelationOwners.Add(owner);
        if (!visible) RelationChanged(owner, relation);
    }
    private void RemoveRelation(Element owner, int index)
    {
        var relation = owner.Relations[index]; owner.Relations.RemoveAt(index);
        if (!HasTarget(owner, relation.Target)) relation.Target.RelationOwners.Remove(owner);
        foreach (var existing in owner.Relations) if (existing.Kind == relation.Kind && existing.Target == relation.Target) return;
        RelationChanged(owner, relation);
    }
    private void SetRelation(Element owner, RelationKind kind, Element? target)
    {
        var oldTarget = RelatedTarget(owner, kind); if (oldTarget == target) return;
        if (kind == RelationKind.LinkTarget && target is not null && !owner.HyperlinkRegistered)
        { _native!.EnableHyperlink(owner); owner.HyperlinkRegistered = true; }
        // Replace the graph before notifying so native clients cannot observe both old and new single targets.
        for (var i = owner.Relations.Count - 1; i >= 0; i--)
            if (owner.Relations[i].Kind == kind)
            {
                var previous = owner.Relations[i].Target; owner.Relations.RemoveAt(i);
                if (!HasTarget(owner, previous)) previous.RelationOwners.Remove(owner);
            }
        if (target is not null) { owner.Relations.Add(new(kind, target)); target.RelationOwners.Add(owner); }
        List<Exception>? errors = null;
        try { RelationChanged(owner, new(kind, target ?? owner)); } catch (Exception error) { (errors ??= []).Add(error); }
        if (kind == RelationKind.ErrorMessage && oldTarget is { Retired: false } && oldTarget != owner && oldTarget != target)
            try { _native!.RelationsChanged(oldTarget); } catch (Exception error) { (errors ??= []).Add(error); }
        if (errors is not null) throw new AggregateException("Accessibility relation replacement failed.", errors);
    }
    private void RelationChanged(Element owner, Relation relation)
    {
        List<Exception>? errors = null;
        if (!owner.Retired)
        {
            if (relation.Kind == RelationKind.ActiveDescendant)
            {
                var target = RelatedTarget(owner, RelationKind.ActiveDescendant);
                if (target is null) _activeOwners.Remove(owner); else if (!_activeOwners.Contains(owner)) _activeOwners.Add(owner);
                try { _native!.ActiveDescendantChanged(owner, target); } catch (Exception error) { (errors ??= []).Add(error); }
                if (_focusOwners.TryGetValue(owner.WindowID, out var focused) && !focused.Retired)
                    try { SetFocusOwner(focused); } catch (Exception error) { (errors ??= []).Add(error); }
            }
            else if (relation.Kind == RelationKind.LinkTarget)
            {
                try { _native!.ActionsChanged(owner); } catch (Exception error) { (errors ??= []).Add(error); }
            }
            else
            {
                try { _native!.RelationsChanged(owner); } catch (Exception error) { (errors ??= []).Add(error); }
                if (relation.Kind == RelationKind.ErrorMessage)
                    try { _native!.StateChanged(owner, "invalid-entry", RelatedTarget(owner, RelationKind.ErrorMessage) is not null); } catch (Exception error) { (errors ??= []).Add(error); }
            }
        }
        if (!relation.Target.Retired && relation.Target != owner && relation.Kind is not (RelationKind.ActiveDescendant or RelationKind.LinkTarget or RelationKind.MemberOf or RelationKind.RadioGroup))
            try { _native!.RelationsChanged(relation.Target); } catch (Exception error) { (errors ??= []).Add(error); }
        if (errors is not null) throw new AggregateException("Accessibility relation notifications failed.", errors);
    }
    internal void ValidateActiveRelations()
    {
        for (var i = _activeOwners.Count - 1; i >= 0; i--)
        {
            var owner = _activeOwners[i]; var target = RelatedTarget(owner, RelationKind.ActiveDescendant);
            if (target is null || !IsDescendant(owner, target)) SetRelation(owner, RelationKind.ActiveDescendant, null);
        }
    }
    private void RetireRelations(Element element, ref List<Exception>? errors)
    {
        _activeOwners.Remove(element);
        while (element.Relations.Count != 0)
            try { RemoveRelation(element, element.Relations.Count - 1); } catch (Exception error) { (errors ??= []).Add(error); }
        while (element.RelationOwners.Count != 0)
        {
            using var iterator = element.RelationOwners.GetEnumerator(); iterator.MoveNext(); var owner = iterator.Current;
            for (var i = owner.Relations.Count - 1; i >= 0; i--)
                if (owner.Relations[i].Target == element)
                    try { RemoveRelation(owner, i); } catch (Exception error) { (errors ??= []).Add(error); }
        }
    }
    private void SetFocusOwner(Element owner)
    {
        _focusOwners[owner.WindowID] = owner; var selected = owner;
        while (RelatedTarget(selected, RelationKind.ActiveDescendant) is { } target && IsDescendant(selected, target)) selected = target;
        if (_focused.GetValueOrDefault(owner.WindowID) == selected) return;
        if (_focused.TryGetValue(owner.WindowID, out var previous)) { previous.Focused = false; _native!.StateChanged(previous, "focused", false); }
        _focused[owner.WindowID] = selected; selected.Focused = true;
        if (_windows.TryGetValue(owner.WindowID, out var window) && window.WindowFocused) _native!.StateChanged(selected, "focused", true);
    }
    internal static void ReconcileSourceRelations(Element owner)
    {
        var r = Runtime;
        for (var i = owner.Relations.Count - 1; i >= 0; i--)
            if (owner.Relations[i].Automatic && !owner.ExpectedRelations.Contains(owner.Relations[i])) r.RemoveRelation(owner, i);
        foreach (var relation in owner.ExpectedRelations) r.AddRelation(owner, relation);
    }
}
