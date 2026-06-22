# FileSystem dock редактора

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
