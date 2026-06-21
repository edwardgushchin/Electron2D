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
/// Marks a C# script class as capable of explicit editor-time execution.
/// </summary>
/// <remarks>
/// <para>
/// Apply this attribute to a script class that inherits from <see cref="Node"/>
/// when the class is intended to run inside editor tooling. In Electron2D 0.1.0
/// Preview, tool scripts are experimental and require explicit sandboxed
/// metadata before editor-time callbacks can run.
/// </para>
/// <para>
/// Runtime traversal stays separate from editor-time execution. The internal
/// tool execution host uses explicit metadata, isolates callback exceptions and
/// does not discover scripts through runtime assembly scanning.
/// </para>
/// <threadsafety>
/// The attribute is immutable and is safe to read from any thread.
/// </threadsafety>
/// </remarks>
/// <since>
/// This attribute is available since Electron2D 0.1.0 Preview.
/// </since>
/// <seealso cref="ExportAttribute"/>
/// <seealso cref="SignalAttribute"/>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class ToolAttribute : Attribute;
