namespace Electron2D;

public sealed partial class AccessibilityServer
{
    /// <summary>Publishes the external destination of a native hyperlink.</summary>
    /// <param name="id">Live element.</param><param name="url">Destination as supplied; empty clears the external destination.</param>
    /// <remarks>Does not open or validate a resource, install a handler, change role or replace an internal link target.</remarks>
    /// <exception cref="ArgumentNullException">The URL is null.</exception>
    /// <exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">The identity is absent or retired.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner.</exception>
    public static void UpdateSetURL(RID id, string url)
    {
        ArgumentNullException.ThrowIfNull(url); var r = Require(); var e = r.Get(id); if (e.URL == url) return;
        if (url.Length != 0 && !e.HyperlinkRegistered) { r._native!.EnableHyperlink(e); e.HyperlinkRegistered = true; }
        e.URL = url; r._native!.ActionsChanged(e);
    }
    internal static bool HasHyperlink(Element e) => e.URL.Length != 0 || RelatedTarget(e, RelationKind.LinkTarget) is not null;
    internal static bool UpdateSourceURL(Element e, LinkButton link)
    {
        var uri = link.URI; if (e.SourceURL == uri) return false;
        UpdateSetURL(e.ID, uri); e.SourceURL = uri; return true;
    }
}
