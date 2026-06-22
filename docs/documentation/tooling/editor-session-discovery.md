# Editor session discovery и Editor-hosted Agent Gateway

Статус: реализованная внутренняя основа.
Задача: `T-0141`.
Обновлено: 2026-06-22.

## Назначение

`EditorSessionRegistry` реализован в `src/Electron2D.Tooling` как internal contract, то есть внутренний слой для будущих Editor, CLI и MCP adapter-ов. Он не является публичным runtime API для игр, не открывает сетевой сервер и не содержит интеграцию с конкретным AI-провайдером.

Registry связывает normalized project root с active Editor-сессией. Если Editor открыт, CLI/MCP adapter получает `ProjectToolingHost` того же `ProjectWorkspace`. Если Editor закрыт или lease устарел, adapter получает явный `HeadlessFallback` и headless `ProjectWorkspace`.

## Endpoint abstraction

`EditorSessionEndpoint` поддерживает два вида локальных endpoint-ов:

- `NamedPipe` для Windows;
- `UnixDomainSocket` для Linux/macOS.

Текущая реализация не открывает реальную pipe/socket. Она фиксирует безопасный descriptor и routing contract для будущих protocol adapter-ов.

Endpoint validation fail-closed отклоняет:

- пустой адрес;
- адрес с query string;
- адрес, содержащий `token`, `secret`, `password`, `apikey` или `api_key`;
- named pipe без локального префикса `\\.\pipe\`;
- Unix domain socket без rooted path.

Сообщение diagnostic не печатает сам секретный фрагмент.

## Open и connect

`OpenEditorSession(...)`:

- нормализует project root;
- проверяет endpoint;
- очищает stale active session для этого root;
- создаёт primary `ProjectWorkspace`, если writer-а нет;
- возвращает read-only workspace, если primary Editor уже активен.

`Connect(...)`:

- используется adapter kind `Cli` или `Mcp`;
- ищет active session по normalized project root;
- при active Editor возвращает `EditorSessionConnectionState.ActiveEditor`, тот же workspace и `ProjectToolingHost`;
- при отсутствии active Editor создаёт headless workspace и возвращает `EditorSessionConnectionState.HeadlessFallback`;
- при stale descriptor сначала удаляет его, затем возвращает headless fallback с diagnostic `E2D-TOOLING-0003`.

`ConnectToDescriptor(...)` проверяет, что descriptor относится к тому же project root, который запросил adapter. Mismatch возвращает `Rejected` и diagnostic `E2D-TOOLING-0003`.

## Lease и release

Каждая primary session содержит:

- `SessionId`;
- `OwnerId`;
- normalized `ProjectRoot`;
- `Endpoint`;
- `RegisteredAtUtc`;
- `LastHeartbeatUtc`;
- `LeaseExpiresAtUtc`.

`Heartbeat(...)` обновляет `LastHeartbeatUtc` и `LeaseExpiresAtUtc`, если `SessionId` и `OwnerId` совпадают с active owner. Heartbeat не может идти назад во времени.

`Release(...)` удаляет lock только для текущей пары `SessionId`/`OwnerId`. Поздний release старой session не снимает lock нового owner-а. `ProjectWorkspace.Dispose()` для primary session вызывает тот же release path.

## Mutating command routing

При `ActiveEditor` изменяющие команды выполняются через `ProjectToolingHost`, привязанный к active Editor workspace. Focused tests проверяют это через `ProjectService.ApplyTextEdit(...)`: документ меняется в in-memory workspace Editor, а файл на диске остаётся прежним до явного сохранения.

При `HeadlessFallback` adapter получает отдельный headless workspace. Это явный state в result, а не скрытый обход active Editor.

## Diagnostics

Используемые коды:

| Code | Severity | Category | Назначение |
| --- | --- | --- | --- |
| `E2D-TOOLING-0003` | `Warning` | `Tooling` | active session не найдена, устарела, освобождена или descriptor относится к другому project root |
| `E2D-TOOLING-0004` | `Error` | `Tooling` | endpoint небезопасен или не является поддержанным локальным endpoint |

## Текущие ограничения

`T-0141` закрывает in-process registry/gateway contract. Он не реализует:

- реальный named pipe server;
- реальный Unix domain socket server;
- MCP protocol schema;
- CLI command parser для project mutations;
- Agent Workspace UI;
- capability manifest parity checks.

Эти adapter-ы должны использовать текущий registry/gateway contract и `Electron2D.Tooling`, а не создавать второй independent workspace при открытом Editor.

## Проверка

Focused проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter FullyQualifiedName~EditorSessionDiscoveryTests
```

Проверка покрывает Windows-friendly и Unix-friendly endpoint abstraction, endpoint secret rejection, CLI/MCP discovery, routing mutating command в active Editor workspace, read-only второй Editor, stale cleanup, graceful release и project-root mismatch diagnostics.
