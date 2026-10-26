using System.Diagnostics;
using System.Runtime.InteropServices;
using static Electron2D.GIONative;
using Element = Electron2D.AccessibilityServer.Element;
using AAction = Electron2D.AccessibilityServer.AccessibilityAction;
using AFlag = Electron2D.AccessibilityServer.AccessibilityFlags;

namespace Electron2D;

internal sealed unsafe partial class ATSPIServer : IDisposable
{
    private const string Accessible = "org.a11y.atspi.Accessible", Component = "org.a11y.atspi.Component", ActionInterface = "org.a11y.atspi.Action", ApplicationInterface = "org.a11y.atspi.Application", Cache = "org.a11y.atspi.Cache";
    private const string CachePath = "/org/a11y/atspi/cache", Registry = "org.a11y.atspi.Registry", RegistryRoot = "/org/a11y/atspi/accessible/root", NullPath = "/org/a11y/atspi/null";
    private sealed class InvalidRequestException(string message, bool retired = false) : Exception(message)
    { internal string ErrorName => retired ? "org.freedesktop.DBus.Error.UnknownObject" : "org.freedesktop.DBus.Error.InvalidArgs"; }
    private sealed class CallbackException(Exception inner) : Exception(inner.Message, inner);
    private sealed class Registration(ATSPIServer bridge, Element element)
    {
        internal readonly ATSPIServer Bridge = bridge;
        internal readonly Element Element = element;
        internal readonly List<uint> IDs = [];
    }
    private readonly AccessibilityServer _owner;
    private readonly Dictionary<Element, Registration> _registrations = [];
    private readonly int _ownerThread = Environment.CurrentManagedThreadId;
    private readonly nint _token = (nint)RID.Allocate().GetID();
    private static ATSPIServer? _opening;
    private nint _context, _connection, _nodeInfo;
    private VTable* _vtable;
    private bool _pendingEmbed, _disposing;
    private int _liveNativeHandles;
    private Exception? _embedError;
    private List<Exception>? _callbackErrors;
    internal string UniqueName { get; private set; } = "";
    internal bool Available => _connection != 0 && g_dbus_connection_is_closed(_connection) == 0;
    private ATSPIServer(AccessibilityServer owner) => _owner = owner;
    internal static ATSPIServer? TryCreate(AccessibilityServer owner)
    {
        if (!OperatingSystem.IsLinux()) return null;
        var bridge = new ATSPIServer(owner);
        try { bridge.Open(); return bridge; }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException or ExternalException)
        {
            bridge.Dispose(); return null;
        }
        catch { bridge.Dispose(); throw; }
    }
    internal static bool DesktopEnabled()
    {
        if (!OperatingSystem.IsLinux()) return false;
        nint session = 0, reply = 0, child = 0, value = 0;
        try
        {
            session = g_bus_get_sync(2, 0, out var error);
            if (session == 0) { if (error != 0) g_error_free(error); return false; }
            reply = g_dbus_connection_call_sync(session, "org.a11y.Bus", "/org/a11y/bus", "org.freedesktop.DBus.Properties", "Get", Tuple([S("org.a11y.Status"), S("IsEnabled")]), 0, 0, 1000, 0, out error);
            if (reply == 0) { if (error != 0) g_error_free(error); return false; }
            child = g_variant_get_child_value(reply, 0); value = g_variant_get_variant(child);
            return g_variant_get_boolean(value) != 0;
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException) { return false; }
        finally
        {
            if (value != 0) g_variant_unref(value); if (child != 0) g_variant_unref(child); if (reply != 0) g_variant_unref(reply); if (session != 0) g_object_unref(session);
        }
    }
    private void Open()
    {
        _context = g_main_context_new();
        g_main_context_push_thread_default(_context);
        try
        {
            var session = g_bus_get_sync(2, 0, out var error);
            if (session == 0) throw new ExternalException(ErrorMessage(error));
            string address;
            try
            {
                var reply = g_dbus_connection_call_sync(session, "org.a11y.Bus", "/org/a11y/bus", "org.a11y.Bus", "GetAddress", Tuple([]), 0, 0, 2000, 0, out error);
                if (reply == 0) throw new ExternalException(ErrorMessage(error));
                try { address = StringArg(reply, 0); } finally { g_variant_unref(reply); }
            }
            finally { g_object_unref(session); }
            _connection = g_dbus_connection_new_for_address_sync(address, 1 | 8, 0, 0, out error);
            if (_connection == 0) throw new ExternalException(ErrorMessage(error));
            g_dbus_connection_set_exit_on_close(_connection, 0);
            UniqueName = Marshal.PtrToStringUTF8(g_dbus_connection_get_unique_name(_connection)) ?? throw new ExternalException("The native accessibility connection has no identity.");
            _nodeInfo = g_dbus_node_info_new_for_xml(Introspection, out error);
            if (_nodeInfo == 0) throw new ExternalException(ErrorMessage(error));
            _vtable = (VTable*)NativeMemory.AllocZeroed((nuint)sizeof(VTable));
            _vtable->Method = (nint)(delegate* unmanaged<nint, nint, nint, nint, nint, nint, nint, nint, void>)&Method;
            _vtable->GetProperty = (nint)(delegate* unmanaged<nint, nint, nint, nint, nint, nint, nint, nint>)&Property;
            _vtable->SetProperty = (nint)(delegate* unmanaged<nint, nint, nint, nint, nint, nint, nint, nint, int>)&SetProperty;
            _owner.Application.Name = ProjectSettings.Get(ProjectSettings.ApplicationName);
            Register(_owner.Application);
            RegisterInterface(_registrations[_owner.Application], CachePath, Cache);
            _pendingEmbed = true; _opening = this;
            g_dbus_connection_call(_connection, Registry, RegistryRoot, "org.a11y.atspi.Socket", "Embed", Tuple([ObjectRef(_owner.Application)]), 0, 0, 2000, 0,
                (nint)(delegate* unmanaged<nint, nint, nint, void>)&Embedded, _token);
            var started = Stopwatch.GetTimestamp();
            while (_pendingEmbed && Stopwatch.GetElapsedTime(started).TotalSeconds < 3)
            {
                if (g_main_context_iteration(_context, 0) == 0) Thread.Sleep(1);
            }
            if (_pendingEmbed) throw new ExternalException("Native accessibility registration timed out.");
            if (_embedError is not null) throw _embedError;
        }
        finally { _opening = null; g_main_context_pop_thread_default(_context); }
    }
    [UnmanagedCallersOnly]
    private static void Embedded(nint connection, nint result, nint data)
    {
        var reply = g_dbus_connection_call_finish(connection, result, out var error);
        if (_opening is { } bridge && bridge._token == data)
        {
            bridge._pendingEmbed = false;
            if (reply == 0) bridge._embedError = new ExternalException(ErrorMessage(error));
        }
        else if (error != 0) g_error_free(error);
        if (reply != 0) g_variant_unref(reply);
    }
    internal void Register(Element e)
    {
        var r = new Registration(this, e); _registrations.Add(e, r);
        g_main_context_push_thread_default(_context);
        try
        {
            RegisterInterface(r, e.Path, Accessible);
            RegisterInterface(r, e.Path, Component);
            RegisterInterface(r, e.Path, ActionInterface);
            if (e.IsApplication) RegisterInterface(r, e.Path, ApplicationInterface);
        }
        catch { Unregister(e); throw; }
        finally { g_main_context_pop_thread_default(_context); }
    }
    private void RegisterInterface(Registration r, string path, string iface)
    {
        var handle = GCHandle.Alloc(r); _liveNativeHandles++;
        var id = g_dbus_connection_register_object(_connection, path, g_dbus_node_info_lookup_interface(_nodeInfo, iface), (nint)_vtable, GCHandle.ToIntPtr(handle), (nint)(delegate* unmanaged<nint, void>)&ReleaseRegistration, out var error);
        if (id == 0) { handle.Free(); _liveNativeHandles--; throw new ExternalException(ErrorMessage(error)); }
        r.IDs.Add(id);
    }
    internal void Unregister(Element e)
    {
        if (!_registrations.Remove(e, out var r)) return;
        try
        {
            if (e.Retired && Available)
            {
                StateChanged(e, "defunct", true);
                Emit(CachePath, Cache, "RemoveAccessible", Tuple([ObjectRef(e)]));
            }
        }
        finally { foreach (var id in r.IDs) g_dbus_connection_unregister_object(_connection, id); }
    }
    [UnmanagedCallersOnly]
    private static void ReleaseRegistration(nint data)
    {
        var handle = GCHandle.FromIntPtr(data); var r = (Registration)handle.Target!;
        r.Bridge._liveNativeHandles--; handle.Free();
    }
    internal void Pump()
    {
        if (Environment.CurrentManagedThreadId != _ownerThread) throw new InvalidOperationException("Native accessibility requests require the display owner.");
        for (var i = 0; i < 64 && g_main_context_pending(_context) != 0; i++) g_main_context_iteration(_context, 0);
        if (_callbackErrors is { } errors) { _callbackErrors = null; throw new AggregateException("Native accessibility callbacks failed.", errors); }
    }
    private void Fail(Exception error) => (_callbackErrors ??= []).Add(error);
    private static Registration Target(nint data) => (Registration)GCHandle.FromIntPtr(data).Target!;
    [UnmanagedCallersOnly]
    private static void Method(nint connection, nint sender, nint path, nint iface, nint method, nint parameters, nint invocation, nint data)
    {
        var r = Target(data);
        try
        {
            if (r.Bridge._disposing) { g_dbus_method_invocation_return_dbus_error(invocation, "org.freedesktop.DBus.Error.Disconnected", "The accessibility owner is retiring."); return; }
            var result = r.Bridge.Call(r.Element, Marshal.PtrToStringUTF8(iface)!, Marshal.PtrToStringUTF8(method)!, parameters);
            g_dbus_method_invocation_return_value(invocation, result);
        }
        catch (InvalidRequestException e) { g_dbus_method_invocation_return_dbus_error(invocation, e.ErrorName, e.Message); }
        catch (Exception e)
        {
            r.Bridge.Fail(e is CallbackException { InnerException: { } original } ? original : e);
            g_dbus_method_invocation_return_dbus_error(invocation, "org.freedesktop.DBus.Error.Failed", e.Message);
        }
    }
    [UnmanagedCallersOnly]
    private static nint Property(nint connection, nint sender, nint path, nint iface, nint property, nint error, nint data)
    {
        var r = Target(data);
        try { return r.Bridge.GetProperty(r.Element, Marshal.PtrToStringUTF8(iface)!, Marshal.PtrToStringUTF8(property)!); }
        catch (InvalidRequestException e) { SetError(error, e.Message, e.ErrorName); return 0; }
        catch (Exception e) { r.Bridge.Fail(e); SetError(error, e.Message); return 0; }
    }
    private int _applicationID;
    [UnmanagedCallersOnly]
    private static int SetProperty(nint connection, nint sender, nint path, nint iface, nint property, nint value, nint error, nint data)
    {
        var r = Target(data);
        try
        {
            if (r.Element.IsApplication && Marshal.PtrToStringUTF8(iface) == ApplicationInterface && Marshal.PtrToStringUTF8(property) == "Id")
            { r.Bridge._applicationID = g_variant_get_int32(value); return 1; }
            SetError(error, "The accessible property is read-only."); return 0;
        }
        catch (Exception e) { r.Bridge.Fail(e); SetError(error, e.Message); return 0; }
    }
    private nint ObjectRef(Element? e) => e is null ? Ref("", NullPath) : Ref(UniqueName, e.Path);
    private nint ParentRef(Element e) => e.IsApplication ? Ref(Registry, RegistryRoot) : ObjectRef(e.Parent);
    private static nint S(string v) => g_variant_new_string(v.Contains('\0') ? v.Replace('\0', '\ufffd') : v);
    private static nint I(int v) => g_variant_new_int32(v);
    private static nint U(uint v) => g_variant_new_uint32(v);
    private static nint B(bool v) => g_variant_new_boolean(v ? 1 : 0);
    private nint GetProperty(Element e, string iface, string name)
    {
        if (e.Retired) throw new InvalidRequestException("The accessibility element has retired.", retired: true);
        if (name is "version" or "InterfaceVersion") return U(1);
        if (iface == Accessible) return name switch
        {
            "Name" => S(e.Name),
            "Description" => S(e.Description),
            "Parent" => ParentRef(e),
            "ChildCount" => I(e.Children.Count),
            "Locale" => S(TranslationServer.Culture.Name),
            "AccessibleId" => S(e.Path),
            "HelpText" => S(""),
            _ => throw new InvalidOperationException("Unknown accessible property.")
        };
        if (iface == ActionInterface && name == "NActions") return I(e.Actions.Count);
        if (iface == ApplicationInterface) return name switch
        {
            "ToolkitName" => S("Electron2D"),
            "Version" or "ToolkitVersion" => S(typeof(AccessibilityServer).Assembly.GetName().Version!.ToString()),
            "AtspiVersion" => S("2.1"),
            "Id" => I(_applicationID),
            _ => throw new InvalidOperationException("Unknown application property.")
        };
        throw new InvalidOperationException("Unknown accessibility property.");
    }
    private nint Interfaces(Element e)
    {
        var b = Builder("as");
        try
        {
            g_variant_builder_add_value(b, S(Accessible)); g_variant_builder_add_value(b, S(Component));
            if (e.Actions.Count != 0) g_variant_builder_add_value(b, S(ActionInterface));
            if (e.IsApplication) g_variant_builder_add_value(b, S(ApplicationInterface));
            return g_variant_builder_end(b);
        }
        finally { g_variant_builder_unref(b); }
    }
    private nint Children(Element e)
    {
        var b = Builder("a(so)"); try { foreach (var child in e.Children) g_variant_builder_add_value(b, ObjectRef(child)); return g_variant_builder_end(b); } finally { g_variant_builder_unref(b); }
    }
    private nint CacheItem(Element e) => Tuple([ObjectRef(e), ObjectRef(_owner.Application), ParentRef(e), I(e.Parent?.Children.IndexOf(e) ?? -1), I(e.Children.Count), Interfaces(e), S(e.Name), U(NativeRole(e)), S(e.Description), States(e)]);
    private nint CacheItems()
    {
        var b = Builder("a((so)(so)(so)iiassusau)");
        try
        {
            g_variant_builder_add_value(b, CacheItem(_owner.Application));
            foreach (var e in _owner.Elements) if (e.Parent is not null) g_variant_builder_add_value(b, CacheItem(e));
            return g_variant_builder_end(b);
        }
        finally { g_variant_builder_unref(b); }
    }
    private nint Call(Element e, string iface, string method, nint args)
    {
        if (e.Retired) throw new InvalidRequestException("The accessibility element has retired.", retired: true);
        if (iface == Cache && method == "GetItems") return Tuple([CacheItems()]);
        if (iface == ApplicationInterface) return method switch { "GetLocale" => Tuple([S(TranslationServer.Culture.Name)]), "GetApplicationBusAddress" => Tuple([S("")]), _ => throw new InvalidOperationException("Unknown application method.") };
        if (iface == Accessible) return method switch
        {
            "GetChildAtIndex" => Tuple([ObjectRef(e.Children.ElementAtOrDefault(IntArg(args, 0)))]),
            "GetChildren" => Tuple([Children(e)]),
            "GetIndexInParent" => Tuple([I(e.Parent?.Children.IndexOf(e) ?? -1)]),
            "GetRelationSet" => Tuple([Array("(ua(so))", [])]),
            "GetRole" => Tuple([U(NativeRole(e))]),
            "GetRoleName" or "GetLocalizedRoleName" => Tuple([S(NativeRoleName(e))]),
            "GetState" => Tuple([States(e)]),
            "GetAttributes" => Tuple([Array("{ss}", [g_variant_new_dict_entry(S("toolkit"), S("Electron2D"))])]),
            "GetApplication" => Tuple([ObjectRef(_owner.Application)]),
            "GetInterfaces" => Tuple([Interfaces(e)]),
            _ => throw new InvalidOperationException("Unknown accessible method.")
        };
        if (iface == ActionInterface)
        {
            if (method == "GetActions")
            {
                var builder = Builder("a(sss)");
                try { for (var i = 0; i < e.Actions.Count; i++) { var name = ActionName(ActionAt(e, i)); g_variant_builder_add_value(builder, Tuple([S(name), S(name), S("")])); } return Tuple([g_variant_builder_end(builder)]); }
                finally { g_variant_builder_unref(builder); }
            }
            var action = ActionAt(e, IntArg(args, 0));
            return method switch
            {
                "GetName" or "GetLocalizedName" or "GetDescription" => Tuple([S(ActionName(action))]),
                "GetKeyBinding" => Tuple([S("")]),
                "DoAction" => Tuple([B(Invoke(e, action))]),
                _ => throw new InvalidOperationException("Unknown action method.")
            };
        }
        if (iface == Component) return ComponentCall(e, method, args);
        throw new InvalidOperationException("Unknown accessibility interface.");
    }
    private static AAction ActionAt(Element e, int index)
    {
        if (index < 0) throw new InvalidRequestException("The action index is outside the supported action list.");
        for (var action = AAction.Click; action <= AAction.Blur; action++) if (e.Actions.ContainsKey(action) && index-- == 0) return action;
        throw new InvalidRequestException("The action index is outside the supported action list.");
    }
    private static string ActionName(AAction action) => action switch { AAction.Click => "click", AAction.Focus => "focus", AAction.Blur => "blur", _ => throw new ArgumentOutOfRangeException(nameof(action)) };
    private static bool Invoke(Element e, AAction action)
    {
        if (!Showing(e) || e.Flag(AFlag.Disabled)) return false;
        if (e.Source is { } source && (source.IsDisposed || !source.IsInsideTree || source.IsDepartingTree || source.IsQueuedForDeletion || source is BaseButton { Disabled: true } || source is CanvasItem { IsVisibleInTree: false } || source is Window { Visible: false })) return false;
        for (var p = e; p is not null; p = p.Parent) if (p.Flag(AFlag.Hidden) || p.Source is Window { Visible: false }) return false;
        if (!e.Actions.TryGetValue(action, out var callback)) return false;
        if (action == AAction.Focus && callback == e.SourceFocusAction && e.Source is Control { CanReceiveGUIFocus: false }) return false;
        try { callback(); } catch (Exception error) { throw new CallbackException(error); }
        return true;
    }
    internal void ChildrenChanged(Element parent, Element child, bool added, int removedIndex = -1)
    {
        Emit(parent.Path, "org.a11y.atspi.Event.Object", "ChildrenChanged", Event(added ? "add" : "remove", added ? parent.Children.IndexOf(child) : removedIndex, 0, ObjectRef(child), parent));
        if (added) Emit(CachePath, Cache, "AddAccessible", Tuple([CacheItem(child)]));
        VisibilityChanged(child);
    }
    private nint Event(string detail, int first, int second, nint value, Element source) => Tuple([S(detail), I(first), I(second), g_variant_new_variant(value), Array("{sv}", [])]);
    private void Emit(string path, string iface, string name, nint value)
    {
        if (g_dbus_connection_emit_signal(_connection, 0, path, iface, name, value, out var error) == 0) throw new ExternalException(ErrorMessage(error));
    }
    internal void PropertyChanged(Element e, string property, string value) => Emit(e.Path, "org.a11y.atspi.Event.Object", "PropertyChange", Event(property, 0, 0, S(value), e));
    internal void RoleChanged(Element e) => Emit(e.Path, "org.a11y.atspi.Event.Object", "PropertyChange", Event("accessible-role", 0, 0, U(NativeRole(e)), e));
    internal void StateChanged(Element e, string state, bool value) => Emit(e.Path, "org.a11y.atspi.Event.Object", "StateChanged", Event(state, value ? 1 : 0, 0, I(0), e));
    internal void ActionsChanged(Element e) => Emit(CachePath, Cache, "AddAccessible", Tuple([CacheItem(e)]));
    public void Dispose()
    {
        Exception? retirementError = null;
        if (_connection != 0)
        {
            _disposing = true;
            foreach (var r in _registrations.Values) foreach (var id in r.IDs) g_dbus_connection_unregister_object(_connection, id);
            _registrations.Clear();
            g_dbus_connection_close_sync(_connection, 0, out var error); if (error != 0) g_error_free(error);
            g_object_unref(_connection); _connection = 0;
            var started = Stopwatch.GetTimestamp();
            while (_liveNativeHandles != 0 && Stopwatch.GetElapsedTime(started).TotalSeconds < 1)
                if (g_main_context_iteration(_context, 0) == 0) Thread.Sleep(1);
            if (_liveNativeHandles != 0) retirementError = new InvalidOperationException("Native accessibility registrations did not retire.");
        }
        if (_nodeInfo != 0) { g_dbus_node_info_unref(_nodeInfo); _nodeInfo = 0; }
        if (_context != 0) { g_main_context_unref(_context); _context = 0; }
        NativeMemory.Free(_vtable); _vtable = null;
        if (retirementError is not null) throw retirementError;
    }
}
