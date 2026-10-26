using System.Buffers.Text;
using static Electron2D.GIONative;
using Element = Electron2D.AccessibilityServer.Element;
using Live = Electron2D.AccessibilityServer.AccessibilityLiveMode;
namespace Electron2D;

internal sealed unsafe partial class ATSPIServer
{
    private readonly List<Element> _announcements = [];
    internal void QueueAnnouncement(Element element, string? text = null)
    {
        if (element.TextProtected) return;
        var e = element;
        while (e.LiveMode is null && e.Parent is not null) e = e.Parent;
        if (e.LiveMode is null or Live.Off || e.Retired) return;
        if (e != element) text ??= element.SourceLiveContent ?? element.TextValue ?? element.Name;
        if (text is not null) e.AnnouncementText = text;
        if (e.AnnouncementPending) return; e.AnnouncementPending = true; _announcements.Add(e);
    }
    internal void FlushAnnouncements()
    {
        List<Exception>? errors = null; Span<byte> buffer = stackalloc byte[64];
        foreach (var e in _announcements)
        {
            e.AnnouncementPending = false; var pending = e.AnnouncementText; e.AnnouncementText = null;
            if (e.Retired || e.LiveMode is null or Live.Off || !Visible(e) || e.TextProtected) continue;
            try
            {
                nint value;
                if (e.HasNumericValue)
                {
                    if (!Utf8Formatter.TryFormat(e.NumericValue, buffer[..^1], out var count)) continue;
                    buffer[count] = 0; fixed (byte* p = buffer) value = g_variant_new_string(p);
                }
                else
                {
                    var text = pending ?? e.SourceLiveContent ?? e.TextValue ?? e.Name; if (text.Length == 0) continue; value = S(text);
                }
                Emit(e.Path, "org.a11y.atspi.Event.Object", "Announcement", Event("", (int)e.LiveMode.Value, 0, value, e));
            }
            catch (Exception error) { (errors ??= []).Add(error); }
        }
        _announcements.Clear(); if (errors is not null) throw new AggregateException("Native live announcements failed.", errors);
    }
    internal void SemanticAttributesChanged(Element e)
    {
        StateChanged(e, "has-tooltip", e.Tooltip.Length != 0); StateChanged(e, "has-popup", e.PopupType is not null);
        Emit(e.Path, "org.a11y.atspi.Event.Object", "AttributesChanged", Event("", 0, 0, I(0), e));
    }
    private static void AddLiveAttributes(nint builder, Element e)
    {
        static void Add(nint b, string name, string value) => g_variant_builder_add_value(b, g_variant_new_dict_entry(S(name), S(value)));
        if (e.LiveMode is { } live) Add(builder, "live", live switch { Live.Polite => "polite", Live.Assertive => "assertive", _ => "off" });
        if (e.PopupType is { } popup) Add(builder, "haspopup", popup switch { AccessibilityServer.AccessibilityPopupType.List => "listbox", AccessibilityServer.AccessibilityPopupType.Tree => "tree", AccessibilityServer.AccessibilityPopupType.Dialog => "dialog", _ => "menu" });
        if (e.Tooltip.Length != 0) Add(builder, "tooltip", e.Tooltip);
    }
}
