# Canonical document model, revision model и structural diff

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0145`.
Обновлено: 2026-06-22.
Связанные документы: [Agent-native cross-platform 2D game engine workflow Electron2D 0.1](../architecture/agent-native-workflow.md), [Сериализация сцен, ресурсов и переносимых property values](../resources/scene-resource-serialization.md), [Resource file baseline, stable UID и ссылки ресурсов](../resources/resource-file-baseline.md), [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md).

## Назначение

`ProjectWorkspace` не должен начинаться с приватных моделей Editor. До него нужен общий canonical document model, то есть единая внутренняя модель проектного файла с устойчивым identity, ревизиями, объектами, parser/serializer boundary и структурным сравнением.

Этот слой не является публичным runtime API. Он нужен Editor, будущему `Electron2D.Tooling`, CLI, MCP, import/export tooling и тестам, чтобы одинаково понимать сцены, ресурсы, настройки, код и служебные документы.

## Document identity

Каждый документ получает `DocumentId` в стабильной текстовой форме:

- сцены используют kind `Scene`;
- resource files используют kind `Resource`;
- project settings, input settings, export presets и task storage используют kind `Settings` или `EditorMetadata`;
- `.cs` files используют kind `Code`;
- JSON-файлы без специального формата используют kind `Json`;
- обычные текстовые файлы используют kind `Text`;
- generated files используют kind `Generated`;
- binary assets используют kind `BinaryAsset`.

`DocumentId` строится из normalized project-relative path и, когда файл содержит собственный persistent UID, из этого UID. Для resource files основным identity является `uid://...`, а path остаётся fallback для review и восстановления. Для scene/settings документов без собственного UID первая реализация использует normalized `res://...` path; когда отдельная задача добавит schema-level document UID, `DocumentId` должен перейти на этот UID без изменения structural diff API.

Normalized path:

- использует `/`;
- не содержит `.` и `..`;
- не выходит за project root;
- чувствителен к регистру, чтобы не скрывать конфликт между платформами.

## Revision model

Модель хранит две ревизии:

- `PersistedRevision` — состояние последнего успешно прочитанного или сохранённого файла;
- `InMemoryRevision` — текущее состояние открытого документа в памяти.

Если ревизии совпадают, документ не содержит unsaved changes. Любая структурная mutation увеличивает только `InMemoryRevision`. Успешный save обновляет `PersistedRevision` до текущей `InMemoryRevision`. External import сравнивает новый parsed snapshot с `PersistedRevision`, а затем пытается объединить результат с текущей in-memory model.

Ревизии являются монотонными неотрицательными числами внутри одного workspace session. Они не являются timestamp и не зависят от порядка файловой системы.

## Canonical object model

Parser должен возвращать flat object map, пригодную для structural diff:

```text
ProjectDocumentSnapshot
├── Identity
├── Classification
├── PersistedRevision
├── InMemoryRevision
└── Objects
    ├── ObjectUid
    ├── ParentObjectUid
    ├── Type
    ├── Name
    ├── Order
    └── Properties
```

`ObjectUid` стабилен внутри документа. Для текущих scene/resource formats первая реализация использует уже существующие стабильные ids:

- scene node: `scene-node:<id>`;
- resource main object: `resource:main`;
- internal resource: `resource-internal:<id>`;
- external resource reference: `resource-external:<id>`;
- settings root: `settings:root`;
- generic JSON root: `json:root`;
- text/code root: `text:root`;
- binary root: `binary:root`.

Object UID нельзя пересоздавать при rename или move. Rename меняет только `Name`, move меняет только `ParentObjectUid` или `Order`.

## Parser/serializer boundary

Parser должен fail closed:

- unknown special `format` в JSON не должен молча интерпретироваться как сцена или resource file;
- malformed JSON возвращает ошибку parsing;
- binary asset не пытается декодироваться как text;
- generated files классифицируются отдельно, чтобы будущая merge policy могла запрещать ручное объединение.

Serializer первого слоя не обязан заменять stable formatter из `T-0117`. В `T-0145` он должен уметь вернуть canonical structure в текстовом debug-представлении и сохранить identity/object UID без потери данных при parse -> serialize debug view -> parse structure в тестовом сценарии. Production-format canonical formatting, migrations и JSON Schema остаются за `T-0117`.

## Structural diff

Structural diff сравнивает два `ProjectDocumentSnapshot` одного `DocumentId` и возвращает typed changes:

- `PropertyChanged` — property path изменился у того же `ObjectUid`;
- `Renamed` — изменился `Name` у того же `ObjectUid`;
- `Moved` — изменился `ParentObjectUid` или `Order` у того же `ObjectUid`;
- `Deleted` — `ObjectUid` был в before snapshot и отсутствует в after snapshot;
- `Added` — `ObjectUid` отсутствовал в before snapshot и появился в after snapshot.

Diff должен различать непересекающиеся property changes. Две property-правки считаются непересекающимися, если у них разные `ObjectUid` или разные property paths. Это минимальная основа для будущего merge: такие изменения можно применить независимо, а одинаковый property path у одного объекта требует conflict handling на следующем слое.

Rename и move определяются по `ObjectUid`, а не по имени или позиции в массиве. Поэтому переименование узла и перенос узла не должны выглядеть как deletion/addition.

## Критерии приёмки

- Specification и implementation documentation описывают document identity, classification, revision model, parser/serializer boundary и structural diff.
- Automated tests проверяют classification для scene/resource/settings, C#/JSON/text, generated files и binary assets.
- Automated tests проверяют stable identity для scene/resource/settings documents.
- Automated tests проверяют preservation `ObjectUid` при scene/resource parse round-trip.
- Automated tests проверяют monotonic `PersistedRevision`/`InMemoryRevision` transitions.
- Automated tests проверяют structural diff для непересекающихся property changes, rename, move, deletion и addition.
- Focused tests сначала падают на отсутствии реализации и проходят после минимальной реализации.
- Source license verifier проходит для новых C# files.

## Фактическое состояние, ограничения и проверки

Текущий baseline `T-0145` добавляет отдельный internal project `src/Electron2D.ProjectSystem`. Он не расширяет публичный runtime API `Electron2D`, а даёт общий слой для будущих Editor, Tooling, CLI, MCP и headless-сценариев.

`internal project` здесь означает отдельную .NET-сборку с типами, доступными только дружественным сборкам: тестам, редактору и будущему tooling. Пользовательская игра не должна зависеть от этих типов напрямую.

## Classification

`ProjectDocumentClassifier` распознаёт:

- scene documents: `*.scene.json` или JSON format `Electron2D.SceneFile`;
- resource documents: `*.e2res` или JSON format `Electron2D.ResourceFile`;
- settings documents: `project.e2project.json`, input/export settings и JSON format с `Settings`;
- C# code: `*.cs`;
- generic JSON;
- plain text;
- generated files, включая `.electron2d/import-cache/`, `.electron2d/workspaces/`, `.electron2d/context/`, `.electron2d/session/`, `bin/`, `obj/` и paths с `/generated/`;
- binary assets through `ClassifyBinary()`;
- editor metadata under `.electron2d/tasks/` and `.electron2d/user/`.

Generated and binary flags are explicit, so future merge policy can reject unsafe automatic merges before touching file contents.

## Identity

`ProjectDocumentParser` normalizes project-relative paths to `/` separators, rejects `..`, and creates stable `DocumentId`:

- scene: `document://scene/res://...`;
- resource: `document://resource/uid://...` when top-level `uid` exists;
- settings: `document://settings/res://...`;
- code/json/text/generated/binary/editor metadata use their own kind segment and normalized `res://...` path.

Scene and settings documents currently use the normalized `res://` path because their current file formats do not yet contain a schema-level document UID. Resource documents already use the top-level `uid://...`.

## Revisions

`ProjectDocumentRevisionState.Clean(value)` creates equal persisted and in-memory revisions. `ProjectDocumentSnapshot.IsDirty` becomes true when `InMemoryRevision` differs from `PersistedRevision`.

`WithInMemoryRevision()` updates only the in-memory revision. `MarkPersisted()` copies the current in-memory revision to persisted revision after a successful save. Revisions are monotonic numbers inside one workspace session, not timestamps.

## Object map

Parser output is a flat `ProjectDocumentSnapshot.Objects` map:

- scene nodes become `scene-node:<id>`;
- resource main object becomes `resource:main`;
- internal resources become `resource-internal:<id>`;
- external references become `resource-external:<id>`;
- settings root becomes `settings:root`;
- generic JSON root becomes `json:root`;
- text/code root becomes `text:root`;
- binary root becomes `binary:root`.

Each object carries parent UID, type, name, order and canonical property strings. This is enough for the first structural diff without taking ownership of stable formatting, migrations or JSON Schema; those remain in `T-0117`.

## Serializer

`ProjectDocumentStructuralSerializer` writes and reads a debug JSON snapshot with format `Electron2D.ProjectDocumentSnapshot`. `debug JSON snapshot` means a test/tooling representation of the parsed object map, not a replacement for user project files.

The serializer preserves document identity, classification, revisions, object UIDs, parent links and properties. It is used to prove that parser output can be round-tripped as canonical structure.

## Structural diff

`ProjectDocumentStructuralDiff.Compare(before, after)` requires the same `DocumentId` and returns typed changes:

- `PropertyChanged`;
- `Renamed`;
- `Moved`;
- `Deleted`;
- `Added`.

Rename and move are detected by stable `ObjectUid`; they do not become delete/add changes when the UID remains the same. `AreNonOverlappingPropertyChanges()` returns true only for two property changes on different objects or different property paths.

## Проверка

Focused check:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter FullyQualifiedName~ProjectDocumentModelTests
```

Source header check for the new C# files:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-SourceLicenseHeaders.ps1
```
