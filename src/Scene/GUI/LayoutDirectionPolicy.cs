using System.Globalization;

namespace Electron2D;

internal static class LayoutDirectionPolicy
{
    internal static bool Resolve(Node node, LayoutDirection direction)
    {
        if (direction == LayoutDirection.LTR) return false;
        if (direction == LayoutDirection.RTL) return true;
        if (node.Tree?.ForceRTLLayout ?? ProjectSettings.GetWithOverride(ProjectSettings.ForceRightToLeftLayoutDirection)) return true;
        var domainName = node.TranslationDomain;
        if (direction == LayoutDirection.Inherited)
        {
            for (var parent = node.Parent; parent is not null && parent.TranslationDomain == domainName; parent = parent.Parent)
            {
                if (parent is Control control) return control.IsLayoutRTL();
                if (parent is Window window) return window.IsLayoutRTL();
            }
            switch (node.Tree?.RootLayoutDirection ?? ProjectSettings.GetWithOverride(ProjectSettings.RootNodeLayoutDirection))
            {
                case 1: return false;
                case 2: return true;
                case 3: direction = LayoutDirection.SystemLocale; break;
            }
        }
        var domain = TranslationServer.GetOrAddDomain(domainName);
        var culture = direction == LayoutDirection.SystemLocale ? CultureInfo.CurrentUICulture : domain.EffectiveCulture;
        return culture.TextInfo.IsRightToLeft && (domain.HasTranslationForCulture(culture, false)
            || TranslationServer.FallbackCulture?.TwoLetterISOLanguageName == culture.TwoLetterISOLanguageName);
    }
}
