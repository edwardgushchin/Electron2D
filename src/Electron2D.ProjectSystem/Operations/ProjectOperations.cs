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
namespace Electron2D.ProjectSystem;

internal enum PrincipalKind
{
    Human,
    Agent,
    Cli,
    ExternalFile,
    System,
    Test
}

internal enum OperationCapability
{
    TaskWrite,
    TaskEditUnprivilegedFields,
    TaskSubmitForAcceptance,
    TaskAccept,
    TaskRequestChanges,
    TaskCancel,
    TaskReopen
}

internal sealed class OperationContext
{
    private readonly HashSet<OperationCapability> capabilities;

    public OperationContext(
        string principalId,
        PrincipalKind principalKind,
        string sessionId,
        IEnumerable<OperationCapability> capabilities,
        string origin)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(principalId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentNullException.ThrowIfNull(capabilities);
        ArgumentException.ThrowIfNullOrWhiteSpace(origin);

        PrincipalId = principalId;
        PrincipalKind = principalKind;
        SessionId = sessionId;
        this.capabilities = capabilities.ToHashSet();
        Origin = origin;
    }

    public string PrincipalId { get; }

    public PrincipalKind PrincipalKind { get; }

    public string SessionId { get; }

    public IReadOnlySet<OperationCapability> Capabilities => capabilities;

    public string Origin { get; }

    public bool HasCapability(OperationCapability capability)
    {
        return capabilities.Contains(capability);
    }
}
