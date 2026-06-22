# Editor session discovery и Editor-hosted Agent Gateway

Статус: целевая спецификация для `T-0141`.
Обновлено: 2026-06-22.
Связанные документы: [AI-friendly workflow Electron2D 0.1](../architecture/ai-friendly-workflow.md); [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md); [Live ProjectWorkspace](../project-system/live-project-workspace.md); [Electron2D.Tooling service boundary](tooling-service-boundary.md).

## Назначение

Открытый `Electron2D.Editor` является единственным основным владельцем живого `ProjectWorkspace` для конкретного project root. CLI и MCP должны сначала искать активную Editor-сессию этого проекта и направлять изменяющие команды в неё. Если Editor закрыт или активная сессия устарела, CLI и MCP создают headless workspace, то есть рабочую модель без открытого окна редактора.

Editor-hosted Agent Gateway — локальная точка подключения к уже открытому Editor. Gateway не является облачным сервисом, не содержит ключи моделей и не запускает произвольные shell-команды. Он только предоставляет безопасный путь к `ProjectToolingHost` активного workspace и минимальное состояние сессии, достаточное для будущих CLI/MCP adapter-ов.

## Endpoint и platform abstraction

Session endpoint описывает способ локального соединения:

- `NamedPipe` для Windows;
- `UnixDomainSocket` для Linux/macOS.

Адрес endpoint должен быть локальным адресом канала и не должен включать access token, пароль, ключ API, query string или другой секрет. Секреты не записываются в descriptor, diagnostics, task activity, документацию или shell history. Если endpoint выглядит как строка с секретом, регистрация active session должна fail closed и вернуть structured diagnostic.

Тесты используют абстракцию endpoint, а не реальный внешний AI-провайдер и не обязаны открывать настоящую named pipe или socket. Проверяемый контракт находится на уровне registry/discovery/gateway.

## Registry и lease

Session registry хранит активную запись на normalized project root. Запись содержит:

- `SessionId`;
- `OwnerId`;
- normalized `ProjectRoot`;
- `Endpoint`;
- `OpenMode`;
- `RegisteredAtUtc`;
- `LastHeartbeatUtc`;
- `LeaseExpiresAtUtc`;
- ссылку на active `ProjectWorkspace` для in-process gateway;
- diagnostics последнего discovery/cleanup.

Lease обновляется heartbeat-ом. Если `nowUtc` больше `LeaseExpiresAtUtc`, session считается stale. Stale session удаляется до открытия нового primary Editor или до headless fallback для CLI/MCP. Такое удаление является crash-safe release lock: новый Editor может стать primary owner после истечения lease даже если старый процесс не вызвал graceful release.

Graceful release удаляет active descriptor только если `SessionId` и `OwnerId` совпадают. Это защищает новый owner от позднего release старого процесса.

## Ownership

Для одного project root допускается один primary Editor writer.

Если второй Editor открывает тот же normalized project root, registry не должен создавать второго writer-а. Допустимые варианты:

- вернуть read-only workspace со ссылкой на active primary session;
- вернуть отказ со structured diagnostic.

В `0.1.0 Preview` выбран read-only режим, потому что он позволяет будущему Editor показать проект и объяснить, кто владеет записью. Read-only workspace не выполняет mutating commands.

## Discovery и verification

CLI и MCP adapter-ы ищут active session по normalized project root. Discovery обязан проверить, что descriptor относится к тому же project root, который запросил adapter. Descriptor от другого проекта не используется даже если endpoint доступен.

Результат discovery:

- `ActiveEditor` — найден живой primary Editor, adapter получает gateway к его `ProjectToolingHost`;
- `ReadOnlyEditor` — найден только read-only workspace или mutating access запрещён;
- `HeadlessFallback` — active Editor отсутствует, graceful release уже выполнен или stale descriptor очищен;
- `Rejected` — endpoint небезопасен, project root mismatch, lease повреждён или нет права работать с project root.

Для `HeadlessFallback` adapter создаёт `ProjectWorkspace` в режиме `Headless`. Этот fallback должен быть явным в result, diagnostics и документации, чтобы agent/CI не думали, что работают с открытым Editor.

## Mutating command routing

Если discovery вернул `ActiveEditor`, изменяющие команды CLI/MCP идут через `ProjectToolingHost`, привязанный к active `ProjectWorkspace` Editor. Они не создают второй independent workspace и не пишут файлы напрямую.

Если discovery вернул `HeadlessFallback`, изменяющие команды работают через headless `ProjectWorkspace` и тот же `Electron2D.Tooling` contract. Headless режим не используется автоматически, когда active Editor найден, кроме будущего явного флага adapter-а.

## Diagnostics

Session discovery использует structured diagnostics:

- `E2D-TOOLING-0003` — active session не найдена, устарела, освобождена или descriptor относится к другому project root;
- `E2D-TOOLING-0004` — endpoint небезопасен или не является локальным endpoint поддержанного типа.

Diagnostics должны быть понятны человеку и adapter-у: они называют project root, состояние lease и выбранный fallback, но не печатают секреты.

## Критерии приёмки

- Есть focused tests для `NamedPipe` и `UnixDomainSocket` endpoint abstraction без внешнего AI-провайдера.
- Editor регистрирует active session с normalized project root, безопасным endpoint и lease.
- CLI и MCP adapter kinds находят active session по project root и получают gateway к тому же `ProjectWorkspace`.
- Discovery отвергает descriptor, если project root не совпадает с requested root.
- Mutating command при active Editor меняет active workspace, а не отдельный headless workspace.
- Закрытый Editor и отсутствие active session возвращают явный `HeadlessFallback`.
- Stale session cleanup удаляет просроченный descriptor и разрешает новому Editor стать primary owner.
- Graceful release удаляет lock только для текущего `SessionId`/`OwnerId`.
- Второй Editor для того же project root получает read-only workspace и не может стать вторым writer-ом.
- Endpoint с token/secret/password или query string отклоняется без записи секрета в descriptor.
- Implementation documentation описывает фактическое поведение, ограничения и focused test command.
