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
/// Marks a script field or property as an exported value that can be serialized
/// and shown by editor tooling.
/// </summary>
/// <remarks>
/// <para>
/// Use this attribute on ordinary C# script classes that inherit from
/// <see cref="Node"/> or another Electron2D node type. The attribute is a
/// public marker for user code and source-generated metadata.
/// </para>
/// <para>
/// Electron2D 0.1.0 Preview does not scan assemblies at runtime to discover
/// exported members. Serialization and Inspector tooling use explicit internal
/// metadata generated or registered for a script type.
/// </para>
/// <threadsafety>
/// The attribute is immutable and is safe to read from any thread.
/// </threadsafety>
/// </remarks>
/// <since>
/// This attribute is available since Electron2D 0.1.0 Preview.
/// </since>
/// <seealso cref="SignalAttribute"/>
/// <seealso cref="ToolAttribute"/>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public sealed class ExportAttribute : Attribute;
