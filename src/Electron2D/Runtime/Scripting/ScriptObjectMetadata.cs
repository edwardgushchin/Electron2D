/*
    Electron2D
    MIT License
    Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
    SPDX-License-Identifier: MIT

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/
namespace Electron2D;

internal sealed class ScriptExportPropertyMetadata
{
    private readonly Func<Node, SerializedPropertyValue> _capture;
    private readonly Action<Node, SerializedPropertyValue> _restore;

    private ScriptExportPropertyMetadata(
        Type scriptType,
        Type valueType,
        string name,
        Func<Node, SerializedPropertyValue> capture,
        Action<Node, SerializedPropertyValue> restore)
    {
        ScriptType = scriptType ?? throw new ArgumentNullException(nameof(scriptType));
        ValueType = valueType ?? throw new ArgumentNullException(nameof(valueType));
        Name = !string.IsNullOrWhiteSpace(name)
            ? name
            : throw new ArgumentException("Script export property metadata name must not be empty.", nameof(name));
        _capture = capture ?? throw new ArgumentNullException(nameof(capture));
        _restore = restore ?? throw new ArgumentNullException(nameof(restore));
    }

    public string Name { get; }

    public Type ScriptType { get; }

    public Type ValueType { get; }

    public static ScriptExportPropertyMetadata Create<TScript, TValue>(
        string name,
        Func<TScript, TValue> getter,
        Action<TScript, TValue> setter)
        where TScript : Node
    {
        ArgumentNullException.ThrowIfNull(getter);
        ArgumentNullException.ThrowIfNull(setter);

        return new ScriptExportPropertyMetadata(
            typeof(TScript),
            typeof(TValue),
            name,
            script => SerializedPropertyValueConverter.FromValue(getter((TScript)script)),
            (script, value) => setter((TScript)script, SerializedPropertyValueConverter.ToValue<TValue>(value)));
    }

    public SerializedPropertyValue Capture(Node script)
    {
        ArgumentNullException.ThrowIfNull(script);
        EnsureScriptType(script);
        return _capture(script);
    }

    public void Restore(Node script, SerializedPropertyValue value)
    {
        ArgumentNullException.ThrowIfNull(script);
        ArgumentNullException.ThrowIfNull(value);
        EnsureScriptType(script);
        _restore(script, value);
    }

    private void EnsureScriptType(Node script)
    {
        if (script.GetType() != ScriptType)
        {
            throw new InvalidOperationException(
                $"Script export metadata '{Name}' targets '{ScriptType.FullName}', not '{script.GetType().FullName}'.");
        }
    }
}

internal sealed class ScriptSignalMetadata
{
    private ScriptSignalMetadata(Type scriptType, Type delegateType, string name)
    {
        ScriptType = scriptType ?? throw new ArgumentNullException(nameof(scriptType));
        DelegateType = delegateType ?? throw new ArgumentNullException(nameof(delegateType));
        Name = !string.IsNullOrWhiteSpace(name)
            ? name
            : throw new ArgumentException("Script signal metadata name must not be empty.", nameof(name));
    }

    public string Name { get; }

    public Type ScriptType { get; }

    public Type DelegateType { get; }

    public static ScriptSignalMetadata Create<TScript, TSignal>(string name)
        where TScript : Node
        where TSignal : Delegate
    {
        return new ScriptSignalMetadata(typeof(TScript), typeof(TSignal), name);
    }
}

internal sealed class ScriptObjectTypeMetadata
{
    private ScriptObjectTypeMetadata(
        Type scriptType,
        string scriptName,
        IEnumerable<ScriptExportPropertyMetadata> exports,
        IEnumerable<ScriptSignalMetadata> signals,
        bool isTool)
    {
        ScriptType = scriptType ?? throw new ArgumentNullException(nameof(scriptType));
        ScriptName = !string.IsNullOrWhiteSpace(scriptName)
            ? scriptName
            : throw new ArgumentException("Script metadata name must not be empty.", nameof(scriptName));
        ExportedProperties = ValidateExports(scriptType, exports).ToArray();
        Signals = ValidateSignals(scriptType, signals).ToArray();
        IsTool = isTool;
        IsToolExperimental = isTool;
        IsToolExecutionSandboxed = isTool;
    }

    public Type ScriptType { get; }

    public string ScriptName { get; }

    public IReadOnlyList<ScriptExportPropertyMetadata> ExportedProperties { get; }

    public IReadOnlyList<ScriptSignalMetadata> Signals { get; }

    public bool IsTool { get; }

    public bool IsToolExperimental { get; }

    public bool IsToolExecutionSandboxed { get; }

    public static ScriptObjectTypeMetadata Create<TScript>(
        string scriptName,
        IEnumerable<ScriptExportPropertyMetadata> exports,
        IEnumerable<ScriptSignalMetadata> signals,
        bool isTool = false)
        where TScript : Node
    {
        return new ScriptObjectTypeMetadata(typeof(TScript), scriptName, exports, signals, isTool);
    }

    private static IEnumerable<ScriptExportPropertyMetadata> ValidateExports(
        Type scriptType,
        IEnumerable<ScriptExportPropertyMetadata> exports)
    {
        ArgumentNullException.ThrowIfNull(exports);

        var exportList = exports.ToArray();
        foreach (var export in exportList)
        {
            if (export.ScriptType != scriptType)
            {
                throw new ArgumentException(
                    $"Export metadata '{export.Name}' targets '{export.ScriptType.FullName}', not '{scriptType.FullName}'.",
                    nameof(exports));
            }
        }

        var duplicate = exportList
            .GroupBy(export => export.Name, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException($"Duplicate script export metadata name '{duplicate.Key}'.", nameof(exports));
        }

        return exportList.OrderBy(export => export.Name, StringComparer.Ordinal);
    }

    private static IEnumerable<ScriptSignalMetadata> ValidateSignals(
        Type scriptType,
        IEnumerable<ScriptSignalMetadata> signals)
    {
        ArgumentNullException.ThrowIfNull(signals);

        var signalList = signals.ToArray();
        foreach (var signal in signalList)
        {
            if (signal.ScriptType != scriptType)
            {
                throw new ArgumentException(
                    $"Signal metadata '{signal.Name}' targets '{signal.ScriptType.FullName}', not '{scriptType.FullName}'.",
                    nameof(signals));
            }
        }

        var duplicate = signalList
            .GroupBy(signal => signal.Name, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException($"Duplicate script signal metadata name '{duplicate.Key}'.", nameof(signals));
        }

        return signalList.OrderBy(signal => signal.Name, StringComparer.Ordinal);
    }
}

internal static class ScriptObjectMetadataRegistry
{
    private static readonly object SyncRoot = new();
    private static readonly Dictionary<Type, ScriptObjectTypeMetadata> ByScriptType = [];
    private static readonly Dictionary<string, ScriptObjectTypeMetadata> ByScriptName = new(StringComparer.Ordinal);

    public static void Register(ScriptObjectTypeMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        lock (SyncRoot)
        {
            if (ByScriptName.TryGetValue(metadata.ScriptName, out var registeredByName) &&
                registeredByName.ScriptType != metadata.ScriptType)
            {
                throw new InvalidOperationException(
                    $"Script metadata name '{metadata.ScriptName}' is already registered for '{registeredByName.ScriptType.FullName}'.");
            }

            if (ByScriptType.TryGetValue(metadata.ScriptType, out var registeredByType))
            {
                ByScriptName.Remove(registeredByType.ScriptName);
            }

            ByScriptType[metadata.ScriptType] = metadata;
            ByScriptName[metadata.ScriptName] = metadata;
        }
    }

    public static ScriptObjectTypeMetadata GetByScriptType(Type scriptType)
    {
        ArgumentNullException.ThrowIfNull(scriptType);

        lock (SyncRoot)
        {
            if (ByScriptType.TryGetValue(scriptType, out var metadata))
            {
                return metadata;
            }
        }

        throw new InvalidOperationException(
            $"AOT-safe metadata is not registered for script type '{scriptType.FullName}'.");
    }

    public static bool TryGetByScriptType(Type scriptType, out ScriptObjectTypeMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(scriptType);

        lock (SyncRoot)
        {
            return ByScriptType.TryGetValue(scriptType, out metadata!);
        }
    }

    public static ScriptObjectTypeMetadata GetByScriptName(string scriptName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scriptName);

        lock (SyncRoot)
        {
            if (ByScriptName.TryGetValue(scriptName, out var metadata))
            {
                return metadata;
            }
        }

        throw new InvalidOperationException(
            $"AOT-safe metadata is not registered for script name '{scriptName}'.");
    }

    public static void ApplySignals(Node script)
    {
        ArgumentNullException.ThrowIfNull(script);

        var metadata = GetByScriptType(script.GetType());
        foreach (var signal in metadata.Signals)
        {
            script.AddUserSignal(signal.Name);
        }
    }
}
