# Runtime resource loader

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт, состояние и проверки

Статус: целевая спецификация для reference games export/run correction.
Обновлено: 2026-06-23.

## Назначение

Runtime resource filesystem должен дать игровому коду единый способ ссылаться на project resources через `res://...` как при запуске из редактора или CLI, так и из exported player. Игровой код не должен знать, лежат ресурсы loose-файлами в project root или внутри export packages, и не должен вручную mount/unmount пакеты ресурсов.

## Контракт

- В runtime есть внутренний resource filesystem - механизм движка, доступный runner/export player/tests, но не пользовательскому игровому коду.
- `ImageTexture.LoadFromFile("res://...")` декодирует PNG через текущий внутренний resource source, а не через прямой файловый путь.
- Чтение `project.e2d.json`, scene JSON и других служебных runtime-файлов из exported player также идёт через внутренний resource filesystem.
- В режиме `e2d run --project <root>` runner монтирует project root как filesystem-backed `res://`.
- В exported player runner монтирует `electron2d.pack.json` и `packs/**/*.e2dpkg` как package-backed `res://`.
- Package-backed runtime читает entries напрямую из `.e2dpkg`; он не извлекает assets во временную папку и не требует loose `assets/`, `resources/` или `scenes/` рядом с executable.
- Если `res://` путь не найден, loader fail-closed с понятным исключением о missing resource path.
- Этот срез не добавляет новый публичный mount API и не вводит публичные `LoadBytes`/`LoadText` helpers для игрового кода.

## Критерии приёмки

- Automated tests подтверждают, что `ImageTexture.LoadFromFile("res://...")` читает PNG из mounted project root.
- Automated tests подтверждают, что внутренний runtime reader читает text из `.e2dpkg` напрямую и не создаёт loose extracted files рядом с package output.
- Reference games используют `ImageTexture.LoadFromFile("res://...")` для runtime textures вместо hand-written placeholder textures.
- Reference games не вызывают public или internal mount API из пользовательского кода.
- Windows export player монтирует pack manifest и запускает reference platformer без распаковки пакетов во временную runtime-папку.
