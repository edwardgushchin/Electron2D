using static Electron2D.GIONative;
using Element = Electron2D.AccessibilityServer.Element;
using Kind = Electron2D.AccessibilityServer.RelationKind;

namespace Electron2D;

internal sealed unsafe partial class ATSPIServer
{
    private const string Hyperlink = "org.a11y.atspi.Hyperlink";
    internal void EnableHyperlink(Element element) => EnableInterface(element, Hyperlink);
    private static (uint Forward, uint Reverse) RelationTypes(Kind kind) => kind switch
    {
        Kind.Controls => (3, 4),
        Kind.DescribedBy => (18, 17),
        Kind.Details => (19, 20),
        Kind.FlowTo => (10, 11),
        Kind.LabeledBy => (2, 1),
        Kind.RadioGroup or Kind.MemberOf => (5, 0),
        Kind.ErrorMessage => (21, 22),
        _ => (0, 0)
    };
    private nint Relations(Element element)
    {
        var groups = new SortedDictionary<uint, List<Element>>();
        void Add(uint kind, Element target)
        {
            if (kind == 0 || target.Retired) return;
            if (!groups.TryGetValue(kind, out var targets)) groups[kind] = targets = [];
            if (!targets.Contains(target)) targets.Add(target);
        }
        foreach (var relation in element.Relations) if (relation.Automatic) Add(RelationTypes(relation.Kind).Forward, relation.Target);
        foreach (var relation in element.Relations) if (!relation.Automatic) Add(RelationTypes(relation.Kind).Forward, relation.Target);
        foreach (var owner in element.RelationOwners)
            if (!owner.Retired) foreach (var relation in owner.Relations)
                    if (relation.Target == element) Add(RelationTypes(relation.Kind).Reverse, owner);
        if (element.CollectionParent is { } collectionParent) Add(7, collectionParent);
        if (element.Parent is { } collectionOwner) foreach (var child in collectionOwner.Children) if (child.CollectionParent == element) Add(8, child);
        var builder = Builder("a(ua(so))");
        try
        {
            foreach (var (kind, targets) in groups)
            {
                var refs = Builder("a(so)");
                try { foreach (var target in targets) g_variant_builder_add_value(refs, ObjectRef(target)); g_variant_builder_add_value(builder, Tuple([U(kind), g_variant_builder_end(refs)])); }
                finally { g_variant_builder_unref(refs); }
            }
            return g_variant_builder_end(builder);
        }
        finally { g_variant_builder_unref(builder); }
    }
    internal void RelationsChanged(Element element) => Emit(element.Path, "org.a11y.atspi.Event.Object", "PropertyChange", Event("accessible-relation-set", 0, 0, I(0), element));
    internal void ActiveDescendantChanged(Element element, Element? target)
    {
        StateChanged(element, "manages-descendants", target is not null);
        Emit(element.Path, "org.a11y.atspi.Event.Object", "ActiveDescendantChanged", Event("", target?.Parent?.Children.IndexOf(target) ?? -1, 0, ObjectRef(target), element));
    }
    private nint HyperlinkProperty(Element element, string name) => name switch
    {
        "NAnchors" => I(AccessibilityServer.HasHyperlink(element) ? 1 : 0),
        "StartIndex" or "EndIndex" => I(-1),
        _ => throw new InvalidRequestException("Unknown hyperlink property.")
    };
    private nint HyperlinkCall(Element element, string method, nint args)
    {
        var target = AccessibilityServer.RelatedTarget(element, Kind.LinkTarget);
        if (method == "IsValid") return Tuple([B(AccessibilityServer.HasHyperlink(element))]);
        if (!AccessibilityServer.HasHyperlink(element) || IntArg(args, 0) != 0) throw new InvalidRequestException("The hyperlink anchor is absent or outside its range.");
        return method switch
        {
            "GetObject" => Tuple([ObjectRef(target ?? element)]),
            "GetURI" => Tuple([S(element.URL)]),
            _ => throw new InvalidRequestException("Unknown hyperlink method.")
        };
    }
}
