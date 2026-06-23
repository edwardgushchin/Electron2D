# Agent process bootstrap из Editor

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт, состояние и проверки

Статус: реализованная внутренняя основа.
Задача: `T-0149`.
Обновлено: 2026-06-23.

## Назначение

`EditorAgentProcessBootstrapper` реализован в `Electron2D.Tooling` как internal contract для запуска локальных агентских клиентов из открытого Editor. Это не встроенный чат и не интеграция с конкретным облачным поставщиком: слой создаёт session-scoped MCP configuration, план запуска процесса и проверяемый handshake с active Editor session.

Dock UI `Agent Workspace` описан отдельно в [Agent Workspace panel редактора](agent-workspace-panel.md). Bootstrapper остаётся источником agent session state, а панель показывает это состояние вместе с task, diagnostics, artifacts и jobs без повторного определения статусов.

## Профили

Текущие встроенные профили:

| ProfileId | Display name | Command |
| --- | --- | --- |
| `codex` | `Codex` | `codex` |
| `opencode` | `OpenCode` | `opencode` |
| `claude-code` | `Claude Code` | `claude` |

Профиль задаёт только безопасный план запуска. Bootstrapper не хранит API keys, не добавляет cloud SDK и не передаёт секреты через командную строку.

## Temporary MCP configuration

При запуске агента Editor-side bootstrapper создаёт:

- `AgentSessionId`;
- local endpoint из active Editor descriptor;
- ephemeral token с временем истечения;
- временный config file `mcp-bootstrap.json`.

Config создаётся во временном каталоге, переданном Editor, и проверяется как путь вне project root. Он не попадает в `.electron2d/`, project files, `AGENTS.md`, `TASKS.md`, source control или shell history.

Process start plan использует:

- `WorkingDirectory = projectRoot`;
- `UseShellExecute = false`;
- environment variable `ELECTRON2D_MCP_CONFIG` со значением пути к config file;
- environment variable `ELECTRON2D_AGENT_SESSION_ID` со значением agent session id.

Ephemeral token лежит только во временном config file. Он не попадает в process arguments, environment values, status text или diagnostics.

## Handshake

`CompleteHandshake(agentSessionId, token, nowUtc)`:

1. проверяет существование agent session;
2. сверяет token без вывода token value в diagnostic;
3. проверяет expiry;
4. подключается к `EditorSessionRegistry` как `Mcp` adapter через active Editor descriptor;
5. возвращает `Connected` только для route `activeEditor`;
6. отдаёт initial state: open documents, dirty documents, document revisions, diagnostics и признак доступности selection resource.

Если token неверный, возвращается `HandshakeError` и diagnostic `E2D-AGENT-0001`. Если token истёк, возвращается `TokenExpired` и diagnostic `E2D-AGENT-0002`. Ошибка запуска процесса возвращает `E2D-AGENT-0003`.

`Disconnect(agentSessionId, nowUtc)` переводит agent workspace state в `Disconnected`. Это не освобождает primary Editor lease: ручная работа в Editor и MCP route остаются доступными.

## Agent Workspace state

Model-only state содержит:

- `AgentSessionId`;
- `ProfileId`;
- `ConnectionState`: `Starting`, `Connected`, `Disconnected`, `HandshakeError`, `TokenExpired`;
- route, когда handshake подключился к active Editor;
- redacted config path;
- timestamps старта, подключения, отключения и expiry;
- diagnostic codes без секретных значений.

## Команда подключения внешнего агента

Для агента, запущенного вне кнопки Editor, пользователь должен получить временный config path из Agent Workspace или будущего connection dialog и запустить клиент из project root без передачи token в командной строке:

```powershell
$env:ELECTRON2D_MCP_CONFIG = "<temp>\\<agent-session-id>\\mcp-bootstrap.json"
$env:ELECTRON2D_AGENT_SESSION_ID = "<agent-session-id>"
codex
```

Для других профилей меняется только command (`opencode` или `claude`). Token остаётся во временном config file и истекает по `expiresAtUtc`.

## Текущие ограничения

Текущий слой:

- не открывает реальный named pipe или Unix socket transport;
- не запускает облачного AI-провайдера в тестах;
- не выполняет arbitrary shell;
- не сохраняет agent config в проект.

Agent Workspace dock уже имеет model-first UI snapshot и visual harness, но live binding к реальному MCP transport остаётся следующим слоем поверх текущего bootstrapper-а.

`IAgentProcessLauncher` позволяет production path запускать реальный процесс, а tests используют fake launcher без внешнего AI-клиента.

## Проверка

Focused проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter FullyQualifiedName~EditorAgentBootstrapTests
```

Проверка покрывает встроенные профили, временный MCP config вне project root, отсутствие token leak в process plan и project files, handshake в active Editor route, чтение dirty workspace state, diagnostics, неверный token, expired token и disconnect без освобождения primary Editor lease.
