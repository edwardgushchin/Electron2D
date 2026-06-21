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

/// <summary>
/// Stores named <see cref="Animation"/> resources for <see cref="AnimationPlayer"/>.
/// </summary>
///
/// <remarks>
/// <para>
/// An <see cref="AnimationLibrary"/> groups animations under stable names. The
/// library does not play animations by itself; it is mounted into an
/// <see cref="AnimationPlayer"/> under a library name.
/// </para>
/// <para>
/// The library emits user signals named <c>animation_added</c>,
/// <c>animation_removed</c> and <c>animation_renamed</c> when its contents
/// change.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Mutate it during loading or on the main scene
/// thread. Read-only access is safe only when no thread is mutating the library.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="Animation"/>
/// <seealso cref="AnimationPlayer"/>
public sealed class AnimationLibrary : Resource
{
    private const string AnimationAddedSignal = "animation_added";
    private const string AnimationRemovedSignal = "animation_removed";
    private const string AnimationRenamedSignal = "animation_renamed";

    private readonly Dictionary<StringName, Animation> animations = new();

    /// <summary>
    /// Creates an empty animation library.
    /// </summary>
    ///
    /// <remarks>
    /// The instance starts without animations. Add entries with
    /// <see cref="AddAnimation"/>.
    /// </remarks>
    ///
    /// <threadsafety>
    /// Construction is safe on any thread when the instance is not shared until
    /// construction completes.
    /// </threadsafety>
    ///
    /// <since>
    /// This constructor is available since Electron2D 0.1.0 Preview.
    /// </since>
    /// <seealso cref="AnimationLibrary" />
    ///
    public AnimationLibrary()
    {
        AddUserSignal(AnimationAddedSignal);
        AddUserSignal(AnimationRemovedSignal);
        AddUserSignal(AnimationRenamedSignal);
    }

    /// <summary>
    /// Adds an animation under a name.
    /// </summary>
    ///
    /// <param name="name">The non-empty animation name.</param>
    /// <param name="animation">The animation resource to add.</param>
    ///
    /// <returns>
    /// <see cref="Error.Ok"/> on success, <see cref="Error.InvalidParameter"/>
    /// for an empty name or <c>null</c> animation, or
    /// <see cref="Error.AlreadyExists"/> when the name is already used.
    /// </returns>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Mutate the library from one thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="RemoveAnimation"/>
    /// <seealso cref="HasAnimation"/>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public Error AddAnimation(StringName name, Animation animation)
    {
        if (name.IsEmpty() || animation is null)
        {
            return Error.InvalidParameter;
        }

        if (animations.ContainsKey(name))
        {
            return Error.AlreadyExists;
        }

        animations.Add(name, animation);
        EmitSignal(AnimationAddedSignal, name);
        return Error.Ok;
    }

    /// <summary>
    /// Removes an animation by name.
    /// </summary>
    ///
    /// <param name="name">The animation name to remove.</param>
    ///
    /// <remarks>
    /// Removing a missing name is a no-op.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Mutate the library from one thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="AddAnimation"/>
    public void RemoveAnimation(StringName name)
    {
        if (name.IsEmpty())
        {
            return;
        }

        if (animations.Remove(name))
        {
            EmitSignal(AnimationRemovedSignal, name);
        }
    }

    /// <summary>
    /// Renames an existing animation.
    /// </summary>
    ///
    /// <param name="name">The existing animation name.</param>
    /// <param name="newName">The new non-empty animation name.</param>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when either name is empty.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="name"/> is missing or
    /// <paramref name="newName"/> already exists.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Mutate the library from one thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetAnimationList"/>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public void RenameAnimation(StringName name, StringName newName)
    {
        if (name.IsEmpty())
        {
            throw new ArgumentException("Animation name must not be empty.", nameof(name));
        }

        if (newName.IsEmpty())
        {
            throw new ArgumentException("New animation name must not be empty.", nameof(newName));
        }

        if (!animations.TryGetValue(name, out var animation))
        {
            throw new InvalidOperationException($"Animation '{name}' does not exist.");
        }

        if (animations.ContainsKey(newName))
        {
            throw new InvalidOperationException($"Animation '{newName}' already exists.");
        }

        animations.Remove(name);
        animations.Add(newName, animation);
        EmitSignal(AnimationRenamedSignal, name, newName);
    }

    /// <summary>
    /// Checks whether the library contains an animation.
    /// </summary>
    ///
    /// <param name="name">The animation name to look up.</param>
    ///
    /// <returns><c>true</c> when the name exists; otherwise, <c>false</c>.</returns>
    ///
    /// <threadsafety>
    /// This method is safe when no thread is mutating the library.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetAnimation"/>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public bool HasAnimation(StringName name)
    {
        return !name.IsEmpty() && animations.ContainsKey(name);
    }

    /// <summary>
    /// Gets an animation by name.
    /// </summary>
    ///
    /// <param name="name">The animation name to look up.</param>
    ///
    /// <returns>
    /// The animation resource when found; otherwise, <c>null</c>.
    /// </returns>
    ///
    /// <threadsafety>
    /// This method is safe when no thread is mutating the library.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="HasAnimation"/>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public Animation? GetAnimation(StringName name)
    {
        return !name.IsEmpty() && animations.TryGetValue(name, out var animation)
            ? animation
            : null;
    }

    /// <summary>
    /// Gets animation names in deterministic alphabetical order.
    /// </summary>
    ///
    /// <returns>An array containing all animation names.</returns>
    ///
    /// <threadsafety>
    /// This method is safe when no thread is mutating the library.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="AddAnimation"/>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public StringName[] GetAnimationList()
    {
        return animations.Keys
            .OrderBy(name => name.ToString(), StringComparer.Ordinal)
            .ToArray();
    }
}
