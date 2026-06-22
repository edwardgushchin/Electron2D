# FileSystem dock редактора

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
- Документация реализации описывает workflow и ограничения.
- `powershell -ExecutionPolicy Bypass -File tools\Verify-UiPublicApiGate.ps1 -WikiPath .github\wiki` проходит перед editor work.
- `powershell -ExecutionPolicy Bypass -File tools\Verify-SourceLicenseHeaders.ps1` проходит.
- `powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1` проходит.
- `dotnet build src\Electron2D.sln -c Release` проходит.
