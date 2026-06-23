# FileSystem dock редактора

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0083`.
Дата: 2026-06-22.

## Цель

`Electron2D.Editor` должен получить проверяемую модель FileSystem dock для работы с файлами проекта и ресурсами. Dock должен показывать дерево проекта, создавать папки, переименовывать и перемещать ресурсы, запускать переимпорт, искать файлы, переносить ресурс в сцену и показывать ошибки импорта рядом с теми ресурсами, которые их вызвали.

Эта задача не добавляет новый публичный runtime API. FileSystem dock является внутренней моделью редактора поверх уже существующих `ResourceImportPipeline`, import cache manifest, `ResourceUid` и scene serialization.

## Контракт данных

Dock работает внутри project root и не должен позволять операциям выйти за границы проекта. Из просмотра исключаются служебные каталоги `.git`, `.electron2d`, `.temp`, `bin`, `obj`, `artifacts`, `publish`, `packages`, `TestResults` и `coverage`.

Для каждого элемента дерева dock должен хранить:

- относительный путь от project root;
- `res://` путь для файлов проекта;
- тип элемента: папка или файл;
- признак импортируемого ресурса;
- UID, если ресурс есть в manifest или в `.e2res`;
- последний статус импорта;
- текст ошибки импорта, если импорт завершился ошибкой.
- live status от `ExternalChangeSynchronizer`, если файл уже обнаружен на диске, но полноценный import job ещё не завершён.

## Стабильность UID

Перед переимпортом dock должен загрузить UID из существующего import cache manifest во внутренний registry `ResourceUid`. Если ресурс переименован или перемещён через dock, старая UID-связь должна быть перенесена на новый `res://` путь до запуска import pipeline. Поэтому последующий импорт должен сохранить прежний UID.

Для `.e2res` файлов dock также обновляет поле `path` внутри файла после rename/move. UID внутри `.e2res` не должен меняться.

Если сцены в проекте содержат external reference на перемещённый ресурс, dock должен обновить readable path у reference, сохранив UID. Это нужно, чтобы scene JSON оставался понятным для review и не терял стабильную ссылку.

## Операции

FileSystem dock должен поддерживать:

- browse: построение снимка дерева проекта;
- create folder: создание папки внутри project root;
- rename file/folder: переименование без выхода за границы project root;
- move file/folder: перенос в другую папку project root;
- reimport: запуск import pipeline и обновление import status в dock;
- live import status: отображение `Importing`, `Compiling` или `Error` из состояния synchronizer/job adapter до следующего полного reimport;
- search: поиск по имени и `res://` пути без учёта регистра;
- drag resource into scene: добавление external reference в `SceneFileDocument` и создание node, который ссылается на ресурс;
- import errors: доступный список ошибок с `res://` путём, причиной и текстом ошибки.

## Smoke-режим

Editor executable должен поддерживать аргумент:

```text
--file-system-dock-smoke <work-root>
```

Smoke-режим должен:

- создать временный проект в `<work-root>`;
- создать папки и валидный `.e2res` ресурс с UID;
- выполнить reimport и убедиться, что ресурс импортирован;
- переименовать ресурс и переместить его в другую папку;
- выполнить reimport повторно и убедиться, что UID остался прежним;
- перенести ресурс в scene file и сохранить scene JSON;
- выполнить поиск по новому имени ресурса;
- создать невалидный `.e2res`, выполнить reimport и вывести ошибку импорта;
- вывести machine-readable строки: `ScenePath`, `InitialItemCount`, `FolderCreated`, `MovedFileExists`, `RenamedResourcePath`, `MovedResourcePath`, `UidBefore`, `UidAfter`, `UidStable`, `SceneExternalReferencePath`, `SceneExternalReferenceUid`, `DraggedNodeType`, `SearchResults`, `ImportErrorCount`, `ImportErrorPath`, `ImportErrorVisible`, `RoundTripStable`;
- вернуть exit code `0`, если все инварианты выполнены.

## Приемочные критерии

- Integration test запускает `dotnet run --project src/Electron2D.Editor/Electron2D.Editor.csproj -- --file-system-dock-smoke ...`.
- Тест подтверждает создание папки, browse и search.
- Тест подтверждает rename/move ресурса.
- Тест подтверждает, что UID после rename/move совпадает с UID до операции.
- Тест подтверждает drag resource into scene через сохранённый `SceneFileDocument`.
- Тест подтверждает, что ошибка импорта видна как отдельная запись dock.
- Тест подтверждает, что новый asset может отображать live status `Importing` до завершения reimport.
- Документация реализации описывает workflow и ограничения.
- `powershell -ExecutionPolicy Bypass -File tools\Verify-UiPublicApiGate.ps1 -WikiPath .github\wiki` проходит перед editor work.
- `powershell -ExecutionPolicy Bypass -File tools\Verify-SourceLicenseHeaders.ps1` проходит.
- `powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1` проходит.
- `dotnet build src\Electron2D.sln -c Release` проходит.

## Фактическое состояние, ограничения и проверки

Статус: документация реализации для `T-0083`.
Дата: 2026-06-22.

## Назначение

FileSystem dock в `Electron2D.Editor` управляет файлами проекта и ресурсами. Текущий слой является внутренней моделью редактора: он не добавляет публичные типы в runtime assembly `Electron2D` и не создаёт отдельный формат данных.

Dock работает поверх существующего import cache manifest, `ResourceImportPipeline`, `ResourceUid`, `.e2res` resource files и `SceneFileDocument`. Это позволяет editor workflow использовать тот же `res://` путь и тот же UID, которые затем видит runtime resource pipeline.

## Текущее поведение

Модель FileSystem dock поддерживает:

- построение снимка дерева project root;
- исключение служебных каталогов из просмотра;
- создание папок;
- переименование файлов и папок;
- перемещение файлов и папок;
- обновление `path` внутри `.e2res` после rename/move;
- перенос UID из старого `res://` пути на новый путь;
- обновление external references в scene files после rename/move;
- reimport через текущий resource import cache;
- поиск по имени и `res://` пути;
- перенос ресурса в сцену с созданием external reference и node;
- список видимых import errors;
- отображение live import status, если Editor передал provider состояния для файлов, уже обнаруженных synchronizer-ом, но ещё не прошедших полный reimport.

Перед импортом dock загружает текущий manifest во внутренний UID registry. Если ресурс перемещён через dock, registry получает новую связь UID -> новый `res://` путь до запуска import pipeline. Благодаря этому импортируемые файлы, чей UID создаётся по пути, сохраняют прежний UID после rename/move.

Для внешних изменений, найденных `ExternalChangeSynchronizer`, dock может получить live status provider. Такой provider принимает project-relative path и возвращает статус вроде `Importing`. Если provider вернул status, он имеет приоритет над cached manifest status и позволяет показать новый asset в dock до завершения полноценного import job.

## Smoke workflow

Локальная проверка:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --file-system-dock-smoke .temp\editor-file-system-dock
```

Ожидаемый результат включает:

```text
Electron2D.Editor file system dock smoke passed
FolderCreated=True
UidStable=True
ImportErrorVisible=True
LiveImportStatusVisible=True
RoundTripStable=True
```

Smoke-команда создаёт временный проект, импортирует валидный resource file, переименовывает и перемещает ресурс, повторно импортирует его, переносит ресурс в scene file, выполняет поиск и проверяет видимую ошибку для невалидного resource file.
Также smoke создаёт новый `pending.png`, передаёт live status `Importing` через provider и проверяет, что `Browse()` показывает этот статус.

## Ограничения

- В этой задаче FileSystem dock реализован как внутренняя модель и smoke-команда, а не как постоянное визуальное окно с pointer/keyboard input.
- Live import status provider является adapter boundary: текущий dock не владеет watcher-ом и не запускает synchronizer сам.
- Drag resource into scene создаёт базовый node с external resource reference. Более точное поведение для специализированных ресурсов будет расширяться в задачах специализированных редакторов.
- Dock обновляет scene files, которые использует текущий scene JSON serializer. Другие будущие пользовательские текстовые форматы должны подключаться отдельными task-проходами.

## Проверки

Фокусная проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorFileSystemDockTests"
```

Полные проверки:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-UiPublicApiGate.ps1 -WikiPath .github\wiki
powershell -ExecutionPolicy Bypass -File tools\Verify-SourceLicenseHeaders.ps1
powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1
dotnet build src\Electron2D.sln -c Release
```
