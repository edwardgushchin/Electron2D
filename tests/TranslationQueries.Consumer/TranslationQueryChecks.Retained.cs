using System.Globalization;
using System.Reflection;
using Electron2D;
using IOPath = System.IO.Path;

internal static partial class TranslationQueryChecks
{
    private static void CheckRetainedServer()
    {
        const string name = nameof(TranslationServer);
        const string domainName = "retained-server-check";
        const BindingFlags declared = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        var type = typeof(TranslationServer);
        var constructor = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Single();
        Check(type.IsSealed && constructor.IsPrivate && constructor.GetParameters().Length == 0 &&
            type.GetMember("Instance", declared).Length == 0 && type.GetMethods(declared).All(method => method.IsStatic),
            "TranslationServer declares a private constructor and static public operations only.");

        var server = Engine.GetSingleton<TranslationServer>(name);
        var id = server.InstanceID;
        var properties = server.GetPropertyList();
        var culture = properties.OfType<PropertyDescriptor<TranslationServer, CultureInfo>>().Single(property => property.Name == nameof(TranslationServer.Culture));
        var pseudo = properties.OfType<PropertyDescriptor<TranslationServer, bool>>().Single(property => property.Name == nameof(TranslationServer.PseudolocalizationEnabled));
        void CheckIdentity()
        {
            Check(Engine.HasSingleton(name) && Engine.GetSingletonList().Count(entry => entry == name) == 1 &&
                ReferenceEquals(Engine.GetSingleton(name), server) && ReferenceEquals(Engine.GetSingleton<TranslationServer>(name), server) &&
                server.InstanceID == id && !server.IsDisposed, "Engine retains one live translation service.");
        }

        CheckIdentity();
        Reject<InvalidOperationException>(server.Dispose);
        Reject<InvalidOperationException>(() => Engine.UnregisterSingleton(name));
        Reject<InvalidOperationException>(() => Engine.RegisterSingleton(name, server));
        Reject<ArgumentNullException>(() => culture.SetValue(server, null!));
        CheckIdentity();

        var previousCulture = TranslationServer.Culture;
        var previousPseudo = TranslationServer.PseudolocalizationEnabled;
        var previousTest = ProjectSettings.Get(ProjectSettings.LocaleTest);
        var previousFallback = ProjectSettings.Get(ProjectSettings.LocaleFallback);
        var previousProjectPseudo = ProjectSettings.Get(ProjectSettings.PseudolocalizationEnabled);
        var french = CultureInfo.GetCultureInfo("fr");
        var english = CultureInfo.GetCultureInfo("en");
        var german = CultureInfo.GetCultureInfo("de");
        using var catalog = new Translation { Locale = "fr" };
        catalog.AddMessage("message", "catalog");
        catalog.AddPluralMessage("resource", ["one", "many"]);
        using var domain = TranslationServer.GetOrAddDomain(domainName);
        using var loop = new TranslationLoop();
        try
        {
            TranslationServer.Culture = french;
            pseudo.SetValue(server, true);
            Check(ReferenceEquals(culture.GetValue(server), french) && TranslationServer.PseudolocalizationEnabled,
                "Descriptors read static culture writes and write the main domain switch.");
            TranslationServer.AddTranslation(catalog, domainName);
            TranslationServer.AddTranslation(french, domainName, "direct", "entry");
            TranslationServer.AddTranslation(english, domainName, "fallback", "English");
            Check(domain.Translate("message") == "catalog" && domain.Translate("direct") == "entry",
                "The registered domain shares resource and direct entries with the facade.");

            ProjectSettings.Set(ProjectSettings.LocaleTest, "fr");
            ProjectSettings.Set(ProjectSettings.LocaleFallback, "en");
            ProjectSettings.Set(ProjectSettings.PseudolocalizationEnabled, false);
            loop.Frame = () =>
            {
                CheckIdentity();
                Check(ReferenceEquals(TranslationServer.GetOrAddDomain(domainName), domain) &&
                    culture.GetValue(server).Name == "fr" && !pseudo.GetValue(server) && domain.Translate("fallback") == "English",
                    "The engine frame sees startup settings on the same service and domain.");
            };
            Engine.Start(loop);
            try
            {
                Check(!Engine.AdvanceFrame(0), "The public host drives a translation check frame.");
                CheckRefreshBeforeNotification(loop, server, culture, french, english);
                var delivered = new List<CultureInfo>();
                loop.Changed = () =>
                {
                    var current = culture.GetValue(server);
                    delivered.Add(current);
                    if (ReferenceEquals(current, english))
                    {
                        var observed = Task.Run(() => TranslationServer.Culture).WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
                        Check(ReferenceEquals(observed, english), "The notification runs after releasing the catalog lock.");
                        TranslationServer.Culture = german;
                    }
                };
                culture.SetValue(server, english);
                Check(delivered.SequenceEqual([english, german]) && ReferenceEquals(TranslationServer.Culture, german),
                    "Descriptor writes commit before synchronous notification and allow nested facade writes.");
                var equalName = new CultureInfo("de");
                culture.SetValue(server, equalName);
                Check(delivered.Count == 2 && ReferenceEquals(TranslationServer.Culture, equalName),
                    "Equal culture names retain the new value without re-notifying.");
                TranslationServer.PseudolocalizationEnabled = true;
                pseudo.SetValue(server, true);
                Check(delivered.Count == 4 && pseudo.GetValue(server), "Every switch assignment notifies and exposes committed state.");

                var failure = new ApplicationException("translation notification");
                loop.Changed = () => throw failure;
                try
                {
                    culture.SetValue(server, french);
                    throw new InvalidOperationException("Observer failure must propagate.");
                }
                catch (ApplicationException error) when (ReferenceEquals(error, failure)) { }
                Check(ReferenceEquals(TranslationServer.Culture, french), "Observer failure leaves the new culture committed.");
            }
            finally { Engine.Stop(); }

            CheckIdentity();
            Check(ReferenceEquals(TranslationServer.GetOrAddDomain(domainName), domain) && domain.Translate("message") == "catalog",
                "Engine shutdown preserves registered catalogs and domains.");
            TranslationServer.AddPluralTranslation(french, domainName, "direct", "directs", _ => domain.Translate("direct"));
            Check(domain.TranslatePlural("direct", "directs", 2) == "entry", "The direct plural selector supports same-thread reentry.");
            TranslationServer.AddPluralTranslation(french, domainName, "invalid", "invalids", _ => null!);
            Reject<InvalidOperationException>(() => domain.TranslatePlural("invalid", "invalids", 2));

            catalog.PluralSelector = _ =>
            {
                var found = Task.Run(() => domain.Translate("direct")).WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
                Check(found == "entry", "Resource selectors can reenter from another thread while the lookup is in flight.");
                return 1;
            };
            Check(domain.TranslatePlural("resource", "resources", 2) == "many", "Resource plural lookup uses the borrowed selector.");

            void ReadPrepared()
            {
                Check(ReferenceEquals(Engine.GetSingleton<TranslationServer>(name), server) &&
                    ReferenceEquals(culture.GetValue(server), french) && pseudo.GetValue(server) &&
                    domain.Translate("message") == "catalog" && domain.TranslatePlural("direct", "directs", 2) == "entry",
                    "Prepared state and lookups remain visible.");
            }
            for (var i = 0; i < 128; i++) ReadPrepared();
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 1024; i++) ReadPrepared();
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Check(allocated == 0, $"Warm singleton lookup, descriptor reads and catalog lookups allocated {allocated} bytes.");

            TranslationServer.RemoveDomain(domainName);
            Check(!TranslationServer.HasDomain(domainName) && !domain.IsDisposed && domain.Translate("message") == "catalog",
                "Removing a domain transfers it out of the registry without disposing it or its borrowed catalog.");
            domain.Dispose();
            Check(!catalog.IsDisposed && catalog.GetMessage("message") == "catalog", "Domain disposal leaves borrowed resources usable.");
            Console.WriteLine($"Retained TranslationServer: stable Engine identity, typed state, lifecycle, callbacks and ownership passed; {allocated} B per 1024 warm groups.");
        }
        finally
        {
            TranslationServer.RemoveDomain(domainName);
            TranslationServer.Culture = previousCulture;
            TranslationServer.PseudolocalizationEnabled = previousPseudo;
            ProjectSettings.Set(ProjectSettings.LocaleTest, previousTest);
            ProjectSettings.Set(ProjectSettings.LocaleFallback, previousFallback);
            ProjectSettings.Set(ProjectSettings.PseudolocalizationEnabled, previousProjectPseudo);
            ProjectSettings.FlushChanges();
        }
    }

    private static void CheckRefreshBeforeNotification(TranslationLoop loop, TranslationServer server,
        PropertyDescriptor<TranslationServer, CultureInfo> culture, CultureInfo french, CultureInfo english)
    {
        var previousRemaps = ProjectSettings.Get(ProjectSettings.LocaleResourceRemaps);
        var directory = Directory.CreateTempSubdirectory("e2d-translation-order-");
        var originalPath = IOPath.Combine(directory.FullName, "original.e2dres");
        var localizedPath = IOPath.Combine(directory.FullName, "localized.e2dres");
        try
        {
            using var original = new Translation { Locale = "fr" };
            using var localized = new Translation { Locale = "en" };
            original.AddMessage("greeting", "Bonjour");
            localized.AddMessage("greeting", "Hello");
            ResourceSaver.Save(original, originalPath);
            ResourceSaver.Save(localized, localizedPath);
            ProjectSettings.Set(ProjectSettings.LocaleResourceRemaps,
                new ResourceLocaleRemap[] { new(originalPath, "en", localizedPath) });
            using var loaded = ResourceLoader.Load<Translation>(originalPath);
            Check(loaded.GetMessage("greeting") == "Bonjour", "The initial resource uses the source archive.");
            var notifications = 0;
            loop.Changed = () =>
            {
                notifications++;
                Check(ReferenceEquals(TranslationServer.Culture, english) && loaded.ResourcePath == originalPath &&
                    loaded.GetMessage("greeting") == "Hello", "The live resource refresh precedes application notification.");
            };
            culture.SetValue(server, english);
            Check(notifications == 1, "The descriptor completes resource refresh and synchronous delivery before returning.");
        }
        finally
        {
            loop.Changed = null;
            ProjectSettings.Set(ProjectSettings.LocaleResourceRemaps, previousRemaps);
            TranslationServer.Culture = french;
            File.Delete(originalPath);
            File.Delete(localizedPath);
            directory.Delete();
        }
    }

    private sealed class TranslationLoop : MainLoop
    {
        internal Action? Frame { get; set; }
        internal Action? Changed { get; set; }

        protected override bool OnProcess(double delta)
        {
            Frame?.Invoke();
            return false;
        }

        protected override void OnNotification(int what)
        {
            if (what == NotificationTranslationChanged) Changed?.Invoke();
        }
    }
}
