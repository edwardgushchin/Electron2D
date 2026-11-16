using System.Globalization;
using Electron2D;

internal static partial class TranslationQueryChecks
{
    private sealed class EqualCatalog : Translation
    {
        public override bool Equals(object? other) => other is EqualCatalog;
        public override int GetHashCode() => 0;
    }

    internal static void RunRegistry()
    {
        var culture = TranslationServer.Culture;
        try
        {
            TranslationServer.Clear();
            TranslationServer.Culture = CultureInfo.GetCultureInfo("fr");
            using var first = new EqualCatalog { Locale = "fr" };
            using var second = new EqualCatalog { Locale = "fr" };
            first.AddMessage("key", "first");
            second.AddMessage("key", "second");
            first.AddPluralMessage("one", ["first-one", "first-many"]);
            second.AddPluralMessage("one", ["second-one", "second-many"]);
            TranslationServer.AddTranslation(first);
            TranslationServer.AddTranslation(second);
            TranslationServer.AddTranslation(first);
            var snapshot = TranslationServer.GetTranslations();
            Check(snapshot.Length == 2 && ReferenceEquals(snapshot[0], first) && ReferenceEquals(snapshot[1], second), "Identity and duplicate order.");
            snapshot[0] = second;
            Check(ReferenceEquals(TranslationServer.GetTranslations()[0], first), "Independent arrays.");
            Check(TranslationServer.Translate("", "key") == "second", "Later registration wins.");
            TranslationServer.RemoveTranslation(second);
            Check(TranslationServer.HasTranslation(first) && !TranslationServer.HasTranslation(second), "Removal uses identity even with equal catalogs.");
            Check(!second.IsDisposed && TranslationServer.Translate("", "key") == "first", "Removal borrows resources.");
            TranslationServer.AddTranslation(second);
            second.AddMessage("key", "edited");
            Check(TranslationServer.Translate("", "key") == "edited", "Live message edits.");
            TranslationServer.RemoveTranslation(first);
            TranslationServer.AddTranslation(first);
            Check(ReferenceEquals(TranslationServer.GetTranslations()[1], first) && TranslationServer.Translate("", "key") == "first", "Re-registration appends and changes tie priority.");
            TranslationServer.RemoveTranslation(first);
            TranslationServer.AddTranslation(second);
            TranslationServer.AddTranslation(first);
            TranslationServer.RemoveTranslation(first);
            Check(TranslationServer.TranslatePlural("", "one", "many", 2) == "second-many", "Resource plural priority.");
            TranslationServer.AddTranslation(TranslationServer.Culture, "", "key", "direct");
            TranslationServer.AddPluralTranslation(TranslationServer.Culture, "", "one", "many", _ => "direct-many");
            Check(TranslationServer.Translate("", "key") == "direct" && TranslationServer.TranslatePlural("", "one", "many", 2) == "direct-many", "Direct entries win score ties.");
            second.Dispose();
            Check(!TranslationServer.HasTranslation(second) && TranslationServer.GetTranslations().Length == 0, "Disposal retires the exact resource.");
            TranslationServer.AddTranslation(first);

            var domain = TranslationServer.GetOrAddDomain("Registry");
            var lower = TranslationServer.GetOrAddDomain("registry");
            Check(!ReferenceEquals(domain, lower) && ReferenceEquals(domain, TranslationServer.GetOrAddDomain("Registry")), "Ordinal names and repeated identity.");
            domain.AddTranslation(first);
            TranslationServer.AddTranslation(TranslationServer.Culture, "Registry", "key", "custom-direct");
            TranslationServer.RemoveDomain("Registry");
            Check(!domain.IsDisposed && domain.Translate("key") == "first" && !TranslationServer.HasDomain("Registry"), "Removed domain remains independent without direct entries.");
            TranslationServer.Clear();
            Check(domain.Translate("key") == "first", "Global clear leaves detached domains independent.");
            var replacement = TranslationServer.GetOrAddDomain("Registry");
            Check(!ReferenceEquals(domain, replacement) && replacement.GetTranslations().Length == 0, "Fresh identity under old name.");
            domain.Dispose();
            Check(TranslationServer.HasDomain("Registry"), "Old disposal cannot retire replacement.");
            replacement.Dispose();
            Check(!TranslationServer.HasDomain("Registry"), "Registered disposal retires name.");
            Reject<ArgumentException>(() => TranslationServer.RemoveDomain(""));
            Reject<ArgumentNullException>(() => TranslationServer.GetOrAddDomain(null!));
            Reject<ArgumentNullException>(() => TranslationServer.HasDomain(null!));
            Reject<ArgumentNullException>(() => TranslationServer.RemoveDomain(null!));
            Reject<ArgumentNullException>(() => TranslationServer.AddTranslation(null!));
            Reject<ArgumentNullException>(() => TranslationServer.RemoveTranslation(null!));
            Reject<ArgumentNullException>(() => TranslationServer.HasTranslation(null!));
            Reject<ObjectDisposedException>(() => TranslationServer.AddTranslation(second, "invalid-registry"));
            Check(!TranslationServer.HasDomain("invalid-registry"), "Invalid resource creates no domain.");
            TranslationServer.Clear();
            Check(TranslationServer.GetTranslations().Length == 0 && !first.IsDisposed && lower.GetTranslations().Length == 0 && TranslationServer.HasDomain("registry"), "Clear preserves domains and borrowed lifetime.");
            Check(TranslationServer.Translate("", "key") == "key" && TranslationServer.TranslatePlural("", "one", "many", 2) == "many", "Clear removes both direct entry forms.");
            TranslationServer.RemoveDomain("registry"); lower.Dispose();
            TranslationServer.AddPluralTranslation(TranslationServer.Culture, "reentry", "one", "many", _ =>
            {
                TranslationServer.Clear();
                TranslationServer.RemoveDomain("reentry");
                return "committed-selector";
            });
            Check(TranslationServer.TranslatePlural("reentry", "one", "many", 2) == "committed-selector" && !TranslationServer.HasDomain("reentry"), "Direct selector reentry keeps a coherent registry.");
            Parallel.For(0, 128, i =>
            {
                using var catalog = new Translation { Locale = "fr" };
                var name = "worker-" + i;
                var worker = TranslationServer.GetOrAddDomain(name);
                Parallel.Invoke(() => worker.AddTranslation(catalog), () => TranslationServer.Clear());
                TranslationServer.RemoveDomain(name);
                Check(!TranslationServer.HasDomain(name) && !catalog.IsDisposed, "Concurrent clear and registration preserve borrowing.");
                worker.Dispose();
            });
            var identities = new TranslationDomain[32];
            Parallel.For(0, identities.Length, i => identities[i] = TranslationServer.GetOrAddDomain("shared"));
            Check(identities.All(item => ReferenceEquals(item, identities[0])), "Concurrent retrieval publishes one identity.");
            Parallel.Invoke(() => TranslationServer.RemoveDomain("shared"), () => TranslationServer.GetOrAddDomain("shared"));
            var shared = TranslationServer.GetOrAddDomain("shared");
            Check(!shared.IsDisposed && TranslationServer.HasDomain("shared"), "Concurrent remove/get settles on a live registration.");
            TranslationServer.RemoveDomain("shared");
            foreach (var item in identities) item.Dispose();
            shared.Dispose();
            Console.WriteLine("Registry checks passed.");
        }
        finally { TranslationServer.Clear(); TranslationServer.Culture = culture; }
    }
}
