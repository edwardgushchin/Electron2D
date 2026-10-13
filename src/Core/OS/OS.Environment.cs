using SDL3;

namespace Electron2D;

public sealed partial class OS
{
    private readonly object _environmentGate = new();

    /// <summary>Gets a process environment variable.</summary>
    /// <param name="variable">A nonempty name without equals or null characters.</param>
    /// <returns>The value, or an empty string when absent. Use HasEnvironment to distinguish an empty value.</returns>
    /// <remarks>Names follow platform case sensitivity. This is a cold, allocating system operation.</remarks>
    public static string GetEnvironment(string variable)
    {
        ValidateEnvironmentName(variable);
        lock (Service._environmentGate) return Environment.GetEnvironmentVariable(variable) ?? string.Empty;
    }

    /// <summary>Reports whether a process environment variable exists, including an empty value.</summary>
    /// <param name="variable">A nonempty name without equals or null characters.</param>
    /// <returns>Whether the current managed process environment contains the name.</returns>
    public static bool HasEnvironment(string variable)
    {
        ValidateEnvironmentName(variable);
        lock (Service._environmentGate) return Environment.GetEnvironmentVariable(variable) is not null;
    }

    /// <summary>Sets a variable for this process and subsequently launched children.</summary>
    /// <param name="variable">A nonempty name without equals or null characters.</param>
    /// <param name="value">The value, including an empty string; null characters are rejected.</param>
    /// <remarks>Changes are process-local and do not persist after exit. SDL and managed child launch paths are updated.
    /// External native environment edits are outside this synchronized service; callers must coordinate process-wide edits.</remarks>
    /// <exception cref="ArgumentException">The name or value is invalid or contains malformed Unicode.</exception>
    /// <exception cref="IOException">The native environment cannot be updated.</exception>
    public static void SetEnvironment(string variable, string value)
    {
        ValidateEnvironmentName(variable);
        ArgumentNullException.ThrowIfNull(value);
        ProcessUTF8.GetByteCount(value);
        if (value.Contains('\0')) throw new ArgumentException("An environment value cannot contain a null character.", nameof(value));
        Service.ChangeEnvironment(variable, value);
    }

    /// <summary>Removes a variable from this process and subsequently launched children.</summary>
    /// <param name="variable">A nonempty name without equals or null characters.</param>
    /// <remarks>Removing an absent name succeeds. The removal does not persist after exit.</remarks>
    /// <exception cref="IOException">The native environment cannot be updated.</exception>
    public static void UnsetEnvironment(string variable)
    {
        ValidateEnvironmentName(variable);
        Service.ChangeEnvironment(variable, null);
    }

    private static void ValidateEnvironmentName(string variable)
    {
        ArgumentException.ThrowIfNullOrEmpty(variable);
        ProcessUTF8.GetByteCount(variable);
        if (variable.Contains('=') || variable.Contains('\0')) throw new ArgumentException("An environment name cannot contain equals or null characters.", nameof(variable));
    }

    private void ChangeEnvironment(string variable, string? value)
    {
        lock (_environmentGate)
        {
            var native = SDL.GetEnvironment();
            if (native == 0) throw new IOException("Cannot access the native environment: " + SDL.GetError());
            var previous = Environment.GetEnvironmentVariable(variable);
            Environment.SetEnvironmentVariable(variable, value);
            if (value is null ? SDL.UnsetEnvironmentVariable(native, variable) : SDL.SetEnvironmentVariable(native, variable, value, true)) return;
            var error = SDL.GetError();
            Environment.SetEnvironmentVariable(variable, previous);
            throw new IOException("Cannot update the native environment: " + error);
        }
    }
}
