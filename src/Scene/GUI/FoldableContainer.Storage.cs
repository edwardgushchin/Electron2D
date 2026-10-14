using System.Globalization;

namespace Electron2D;

public partial class FoldableContainer
{
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var descriptor in base.GetPropertyDescriptors()) if (descriptor.Name is not (nameof(MouseFilter) or nameof(FocusMode))) yield return descriptor;
        foreach (var descriptor in FoldableProperties) yield return descriptor;
        for (var i = 0; i < _titleControls.Count; i++) yield return TitleControlProperty(i);
    }
    internal static PropertyDescriptor? StoredTitleControl(string name, Type valueType)
    {
        const string prefix = "__TitleControl/";
        if (valueType != typeof(Control) || !name.StartsWith(prefix, StringComparison.Ordinal) || !int.TryParse(name.AsSpan(prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var index) || index < 0 || name != prefix + index.ToString(CultureInfo.InvariantCulture)) return null;
        return TitleControlProperty(index);
    }
    internal IEnumerable<PropertyDescriptor> CapturedTitleProperties(Node root)
    {
        var index = 0; foreach (var control in _titleControls) if (ReferenceEquals(control.Owner, root)) yield return TitleControlProperty(index++, control);
    }
    private static PropertyDescriptor TitleControlProperty(int index, Control? captured = null) => new PropertyDescriptor<FoldableContainer, Control?>("__TitleControl/" + index.ToString(CultureInfo.InvariantCulture),
        node => captured ?? (index < node._titleControls.Count ? node._titleControls[index] : null),
        (node, control) =>
        {
            node.EnsureMutable();
            if (control is null || control.IsDisposed || !ReferenceEquals(control.Parent, node) || index != node._titleControls.Count || node._titleControls.Contains(control)) throw new InvalidDataException("Invalid stored title control membership.");
            if (!control.IsInternalChild)
            {
                var owner = control.Owner; node.RemoveChild(control); node.AddChild(control, InternalMode.Front); control.Owner = owner;
            }
            node._titleControls.Add(control); node.InvalidateTitle();
        }, _ => null, stored: true);
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(FoldableContainer) ? CreateFoldable : base.CreateSceneInstanceFactory();
    private static Node CreateFoldable() => new FoldableContainer();
    private static readonly PropertyDescriptor[] FoldableProperties =
    [
        new PropertyDescriptor<FoldableContainer, MouseFilter>(nameof(MouseFilter), n => n.MouseFilter, (n,v) => n.MouseFilter=v, _ => MouseFilter.Stop, stored:true),
        new PropertyDescriptor<FoldableContainer, FocusMode>(nameof(FocusMode), n => n.FocusMode, (n,v) => n.FocusMode=v, _ => FocusMode.All, stored:true),
        new PropertyDescriptor<FoldableContainer, bool>(nameof(Folded), n => n.Folded, (n,v) => n.Folded=v, _ => false, stored:true),
        new PropertyDescriptor<FoldableContainer, FoldableGroup?>(nameof(FoldableGroup), n => n.FoldableGroup, (n,v) => n.FoldableGroup=v, _ => null, stored:true),
        new PropertyDescriptor<FoldableContainer, string>(nameof(Title), n => n.Title, (n,v) => n.Title=v, _ => "", stored:true),
        new PropertyDescriptor<FoldableContainer, string>(nameof(Language), n => n.Language, (n,v) => n.Language=v, _ => "", stored:true),
        new PropertyDescriptor<FoldableContainer, HorizontalAlignment>(nameof(TitleAlignment), n => n.TitleAlignment, (n,v) => n.TitleAlignment=v, _ => HorizontalAlignment.Left, stored:true),
        new PropertyDescriptor<FoldableContainer, BarPosition>(nameof(TitlePosition), n => n.TitlePosition, (n,v) => n.TitlePosition=v, _ => BarPosition.Top, stored:true),
        new PropertyDescriptor<FoldableContainer, TextDirection>(nameof(TitleTextDirection), n => n.TitleTextDirection, (n,v) => n.TitleTextDirection=v, _ => TextDirection.Auto, stored:true),
        new PropertyDescriptor<FoldableContainer, TextOverrunBehavior>(nameof(TitleTextOverrunBehavior), n => n.TitleTextOverrunBehavior, (n,v) => n.TitleTextOverrunBehavior=v, _ => TextOverrunBehavior.NoTrimming, stored:true)
    ];
}
