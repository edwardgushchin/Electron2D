# CodexAgentExecutor

Last updated: 2026-10-26

- **Assembly/namespace:** `Electron2D.Editor.Session` / `Electron2D.Editor`, outside the runtime assembly.
- **Source:** [CodexAgentExecutor.cs](../../editor/Session/Agents/CodexAgentExecutor.cs).
- **Declaration:** `public sealed class CodexAgentExecutor : ISessionAgentExecutor`

Concrete qualified local app-server adapter. It reuses the installed CLI login and selected default model; it neither creates nor reads API keys.

## Public members

| Signature | Contract |
| --- | --- |
| `CodexAgentExecutor(string executable = "codex")` | Validate the executable name/path without starting a process. |
| `string Name { get; }` | Display executor identity. |
| `SessionAgentStatus Status { get; }` | Thread-safe lifecycle state. |
| `bool IsConnected { get; }` | Whether an authenticated conversation remains usable. |
| `string Model { get; }` | Actual selected model after connection; empty beforehand. |
| `string? ThreadID { get; }` | Conversation identity retained for reconnect. |
| `Task ConnectAsync(string projectRoot, string actor, string capability, CancellationToken cancellation = default)` | Connect using privately issued rights and verify actual protocol/tool support. |
| `Task SubmitAsync(string prompt, CancellationToken cancellation = default)` | Submit a task; acknowledgement does not establish terminal success. |
| `Task InterruptAsync(CancellationToken cancellation = default)` | Request turn interruption; observe a later terminal event. |
| `Task DisconnectAsync()` | Retire owned processes/pipes, retain conversation identity. |
| `IReadOnlyList<SessionAgentEvent> DrainEvents()` | Return at most 128 copied literal events without worker-thread UI callbacks. |
| `ValueTask DisposeAsync()` | Permanently disconnect/retire this adapter. |

## Contract and verification

Requires Linux setsid/proc process ownership and exact codex-cli 0.163.0. Connect verifies CLI identity, existing account, JSON-RPC initialization, read-only filesystem policy and all 17 real MCP tools, rejecting unrelated active MCP servers. A private temporary cwd isolates repository instructions; per-process configuration disables shell, apps, plugins, memory, delegation and hooks, disables web search, and enables the code-mode tool router needed by this CLI. Global configuration is unchanged. The filesystem policy is read-only, not a restricted-read sandbox. Game authoring/snapshot code remains trusted. The operation capability is inherited privately by the MCP child and redacted from bounded events. Unsupported approval/forms are declined/reported; supply further instructions in the conversation. Prompt max 16384 UTF-16 characters and strict UTF-8 validation. Protocol requests time out after 30 seconds; at most 32 are outstanding. A failed/unknown submission disconnects semantic rights through the frontend. Interruption waits at most 10 seconds for the real turn ID. Reconnect resumes the same conversation for the same project; it cannot cross roots. Native acceptance names the actual model in its receipt and verifies unsaved edits, conflict repair, capture, cancellation and reconnect. This is the exercised provider adapter; ACP and other CLI versions/providers are not qualified.

See [the agent workflow](../components/project-agent.md) for protocol, examples, credentials, cancellation, evidence and platform limits. This tooling API changes no runtime coverage row.
