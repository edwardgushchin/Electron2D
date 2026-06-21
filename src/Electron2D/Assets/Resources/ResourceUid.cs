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
using System.Buffers.Binary;
using System.Text;

namespace Electron2D;

/// <summary>
/// Manages stable unique identifiers for resource paths in an Electron2D project.
/// </summary>
///
/// <remarks>
/// <para>
/// Resource UIDs let scene and resource files keep a stable <c>uid://</c>
/// reference while a resource is renamed or moved. The path remains available
/// as a readable fallback for review tools and for projects that do not have a
/// UID mapping loaded yet.
/// </para>
///
/// <para>
/// This class provides Electron2D's resource UID registry using the C# naming
/// shape used by the runtime API. In Electron2D 0.1.0 Preview it is a static
/// runtime facade, not an engine singleton object.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// All methods are safe to call from any thread. Methods that read or mutate
/// the UID registry synchronize internally.
/// </threadsafety>
///
/// <since>
/// This class is available since Electron2D 0.1.0 Preview.
/// </since>
public static class ResourceUid
{
    /// <summary>
    /// The value used when a resource UID is invalid or cannot be resolved.
    /// </summary>
    ///
    /// <remarks>
    /// The text form of this value is <c>uid://&lt;invalid&gt;</c>.
    /// </remarks>
    ///
    /// <since>
    /// This constant is available since Electron2D 0.1.0 Preview.
    /// </since>
    public const long InvalidId = -1L;

    private const string Prefix = "uid://";
    private const string InvalidText = "uid://<invalid>";
    private const string Digits = "0123456789abcdefghijklmnopqrstuvwxyz";
    private static readonly object SyncRoot = new();
    private static readonly Dictionary<long, string> PathsById = new();
    private static readonly Dictionary<string, long> IdsByPath = new(StringComparer.Ordinal);

    /// <summary>
    /// Adds a UID mapping for a resource path.
    /// </summary>
    ///
    /// <param name="id">The UID value to register.</param>
    /// <param name="path">The resource path associated with <paramref name="id"/>.</param>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="id"/> is invalid or <paramref name="path"/>
    /// is empty.
    /// </exception>
    ///
    /// <exception cref="InvalidOperationException">
    /// Thrown when the UID or path is already registered.
    /// </exception>
    ///
    /// <threadsafety>
    /// It is safe to call this method from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="SetId(long, string)"/>
    /// <seealso cref="HasId(long)"/>
    public static void AddId(long id, string path)
    {
        ValidateId(id);
        ValidatePath(path);

        lock (SyncRoot)
        {
            if (PathsById.ContainsKey(id))
            {
                throw new InvalidOperationException($"Resource UID '{IdToText(id)}' is already registered.");
            }

            if (IdsByPath.ContainsKey(path))
            {
                throw new InvalidOperationException($"Resource path '{path}' already has a UID.");
            }

            PathsById.Add(id, path);
            IdsByPath.Add(path, id);
        }
    }

    /// <summary>
    /// Creates a random UID that is not currently registered.
    /// </summary>
    ///
    /// <returns>A positive UID value that can be registered with <see cref="AddId(long, string)"/> or <see cref="SetId(long, string)"/>.</returns>
    ///
    /// <threadsafety>
    /// It is safe to call this method from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public static long CreateId()
    {
        Span<byte> bytes = stackalloc byte[sizeof(long)];

        while (true)
        {
            System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
            var id = BinaryPrimitives.ReadInt64LittleEndian(bytes) & long.MaxValue;
            if (id == 0 || id == InvalidId)
            {
                continue;
            }

            lock (SyncRoot)
            {
                if (!PathsById.ContainsKey(id))
                {
                    return id;
                }
            }
        }
    }

    /// <summary>
    /// Creates a deterministic UID candidate for a resource path.
    /// </summary>
    ///
    /// <param name="path">The resource path used as the UID seed.</param>
    ///
    /// <returns>A positive UID value that is stable for <paramref name="path"/> within this runtime contract.</returns>
    ///
    /// <remarks>
    /// If the path is already registered, the registered UID is returned. If
    /// the deterministic candidate collides with another registered path, the
    /// next free positive value is returned.
    /// </remarks>
    ///
    /// <threadsafety>
    /// It is safe to call this method from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public static long CreateIdForPath(string path)
    {
        ValidatePath(path);

        lock (SyncRoot)
        {
            if (IdsByPath.TryGetValue(path, out var registeredId))
            {
                return registeredId;
            }
        }

        Span<byte> hash = stackalloc byte[32];
        System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(path), hash);
        var candidate = BinaryPrimitives.ReadInt64LittleEndian(hash) & long.MaxValue;
        if (candidate == 0 || candidate == InvalidId)
        {
            candidate = 1;
        }

        lock (SyncRoot)
        {
            while (PathsById.TryGetValue(candidate, out var existingPath) && existingPath != path)
            {
                candidate = NextId(candidate);
            }
        }

        return candidate;
    }

    /// <summary>
    /// Returns a path, converting a <c>uid://</c> value when needed.
    /// </summary>
    ///
    /// <param name="pathOrUid">A resource path or UID text.</param>
    ///
    /// <returns>
    /// The unchanged path when <paramref name="pathOrUid"/> is not a UID, the
    /// mapped resource path for a known UID, or an empty string for an unknown
    /// or invalid UID.
    /// </returns>
    ///
    /// <threadsafety>
    /// It is safe to call this method from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="UidToPath(string)"/>
    public static string EnsurePath(string pathOrUid)
    {
        ArgumentNullException.ThrowIfNull(pathOrUid);
        return pathOrUid.StartsWith(Prefix, StringComparison.Ordinal)
            ? UidToPath(pathOrUid)
            : pathOrUid;
    }

    /// <summary>
    /// Gets the path currently associated with a UID.
    /// </summary>
    ///
    /// <param name="id">The UID value to resolve.</param>
    ///
    /// <returns>The resource path associated with <paramref name="id"/>.</returns>
    ///
    /// <exception cref="InvalidOperationException">
    /// Thrown when the UID is not registered.
    /// </exception>
    ///
    /// <threadsafety>
    /// It is safe to call this method from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="HasId(long)"/>
    public static string GetIdPath(long id)
    {
        lock (SyncRoot)
        {
            return PathsById.TryGetValue(id, out var path)
                ? path
                : throw new InvalidOperationException($"Resource UID '{IdToText(id)}' is not registered.");
        }
    }

    /// <summary>
    /// Reports whether a UID is registered.
    /// </summary>
    ///
    /// <param name="id">The UID value to check.</param>
    ///
    /// <returns><c>true</c> when <paramref name="id"/> is known; otherwise, <c>false</c>.</returns>
    ///
    /// <threadsafety>
    /// It is safe to call this method from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public static bool HasId(long id)
    {
        lock (SyncRoot)
        {
            return PathsById.ContainsKey(id);
        }
    }

    /// <summary>
    /// Converts a UID value to <c>uid://</c> text.
    /// </summary>
    ///
    /// <param name="id">The UID value to convert.</param>
    ///
    /// <returns>A <c>uid://</c> string for valid positive values, or <c>uid://&lt;invalid&gt;</c> for invalid values.</returns>
    ///
    /// <threadsafety>
    /// It is safe to call this method from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="TextToId(string)"/>
    public static string IdToText(long id)
    {
        return id <= 0 || id == InvalidId ? InvalidText : Prefix + ToBase36(id);
    }

    /// <summary>
    /// Converts a resource path to UID text when the path is registered.
    /// </summary>
    ///
    /// <param name="path">The resource path to convert.</param>
    ///
    /// <returns>The registered UID text, or <paramref name="path"/> unchanged when no UID is registered.</returns>
    ///
    /// <threadsafety>
    /// It is safe to call this method from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="UidToPath(string)"/>
    public static string PathToUid(string path)
    {
        ValidatePath(path);

        lock (SyncRoot)
        {
            return IdsByPath.TryGetValue(path, out var id) ? IdToText(id) : path;
        }
    }

    /// <summary>
    /// Removes a registered UID.
    /// </summary>
    ///
    /// <param name="id">The UID value to remove.</param>
    ///
    /// <exception cref="InvalidOperationException">
    /// Thrown when the UID is not registered.
    /// </exception>
    ///
    /// <threadsafety>
    /// It is safe to call this method from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public static void RemoveId(long id)
    {
        lock (SyncRoot)
        {
            if (!PathsById.Remove(id, out var path))
            {
                throw new InvalidOperationException($"Resource UID '{IdToText(id)}' is not registered.");
            }

            IdsByPath.Remove(path);
        }
    }

    /// <summary>
    /// Updates the path associated with an existing UID.
    /// </summary>
    ///
    /// <param name="id">The registered UID value.</param>
    /// <param name="path">The new resource path for <paramref name="id"/>.</param>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="id"/> is invalid or <paramref name="path"/>
    /// is empty.
    /// </exception>
    ///
    /// <exception cref="InvalidOperationException">
    /// Thrown when the UID does not exist or the new path belongs to another
    /// UID.
    /// </exception>
    ///
    /// <threadsafety>
    /// It is safe to call this method from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="AddId(long, string)"/>
    public static void SetId(long id, string path)
    {
        ValidateId(id);
        ValidatePath(path);

        lock (SyncRoot)
        {
            if (!PathsById.TryGetValue(id, out var previousPath))
            {
                throw new InvalidOperationException($"Resource UID '{IdToText(id)}' is not registered.");
            }

            if (IdsByPath.TryGetValue(path, out var existingId) && existingId != id)
            {
                throw new InvalidOperationException($"Resource path '{path}' already belongs to another UID.");
            }

            IdsByPath.Remove(previousPath);
            PathsById[id] = path;
            IdsByPath[path] = id;
        }
    }

    /// <summary>
    /// Extracts a numeric UID from <c>uid://</c> text.
    /// </summary>
    ///
    /// <param name="textId">The UID text to parse.</param>
    ///
    /// <returns>The parsed UID, or <see cref="InvalidId"/> when the text is not a valid UID.</returns>
    ///
    /// <threadsafety>
    /// It is safe to call this method from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="IdToText(long)"/>
    public static long TextToId(string textId)
    {
        ArgumentNullException.ThrowIfNull(textId);
        if (textId == InvalidText)
        {
            return InvalidId;
        }

        if (!textId.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return InvalidId;
        }

        return TryParseBase36(textId[Prefix.Length..], out var id) ? id : InvalidId;
    }

    /// <summary>
    /// Converts UID text to a registered resource path.
    /// </summary>
    ///
    /// <param name="uid">The UID text to resolve.</param>
    ///
    /// <returns>The registered resource path, or an empty string when the UID is invalid or unknown.</returns>
    ///
    /// <threadsafety>
    /// It is safe to call this method from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="PathToUid(string)"/>
    public static string UidToPath(string uid)
    {
        var id = TextToId(uid);
        if (id == InvalidId)
        {
            return string.Empty;
        }

        lock (SyncRoot)
        {
            return PathsById.TryGetValue(id, out var path) ? path : string.Empty;
        }
    }

    private static void ValidateId(long id)
    {
        if (id <= 0 || id == InvalidId)
        {
            throw new ArgumentException("Resource UID must be a positive valid value.", nameof(id));
        }
    }

    private static void ValidatePath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
    }

    private static long NextId(long id)
    {
        return id == long.MaxValue ? 1 : id + 1;
    }

    private static string ToBase36(long value)
    {
        Span<char> buffer = stackalloc char[13];
        var position = buffer.Length;
        var remaining = value;

        do
        {
            buffer[--position] = Digits[(int)(remaining % Digits.Length)];
            remaining /= Digits.Length;
        }
        while (remaining > 0);

        return new string(buffer[position..]);
    }

    private static bool TryParseBase36(string text, out long value)
    {
        value = 0;
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        foreach (var character in text)
        {
            var digit = DigitValue(character);
            if (digit < 0 || value > (long.MaxValue - digit) / Digits.Length)
            {
                value = InvalidId;
                return false;
            }

            value = (value * Digits.Length) + digit;
        }

        if (value <= 0 || value == InvalidId)
        {
            value = InvalidId;
            return false;
        }

        return true;
    }

    private static int DigitValue(char character)
    {
        if (character is >= '0' and <= '9')
        {
            return character - '0';
        }

        if (character is >= 'a' and <= 'z')
        {
            return 10 + character - 'a';
        }

        if (character is >= 'A' and <= 'Z')
        {
            return 10 + character - 'A';
        }

        return -1;
    }
}
