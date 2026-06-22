# GitHub Wiki API reference

Текущая реализация генерирует API reference Electron2D в настоящий GitHub Wiki repository:

```text
https://github.com/edwardgushchin/Electron2D.wiki.git
```

Основной репозиторий не отслеживает `.github/wiki/` как часть своего исходного кода и не содержит локальный сайт документации.

Локальный путь `.github/wiki` используется только как рабочий клон GitHub Wiki repository для генерации, проверки и публикации. Этот каталог игнорируется основным репозиторием и не коммитится в него.

## Wiki layout

В `Electron2D.wiki.git` хранятся:

- `Home.md`;
- `_Sidebar.md`;
- `_Footer.md`;
- `API-by-Category.md`;
- `API-*.md` category pages;
- `API-Reference.md`;
- страницы публичных типов с короткими именами, например `Object.md`, `Node.md`, `Collections-Array.md`;
- `API-Compatibility.md`.

`Home.md`, `_Sidebar.md`, `_Footer.md`, `API-by-Category.md`, category pages, `API-Reference.md` и страницы типов создаются автоматически. `API-Compatibility.md` остаётся отдельной GitHub Wiki page с release/status таблицей, не перезаписывается генератором и содержит верхнюю навигацию к остальным API reference pages.

## Generator

Команда локальной генерации во временную папку:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Update-ApiWiki.ps1
```

Команда обновления клона GitHub Wiki:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Update-ApiWiki.ps1 -OutputPath .github/wiki
```

Команда проверки синхронизации с клоном GitHub Wiki:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Update-ApiWiki.ps1 -OutputPath .github/wiki -Check
```

Генератор собирает `src\Electron2D\Electron2D.csproj` с XML documentation file, читает compiled public surface из runtime assembly, читает `data/api/electron2d-api-manifest.json` и создаёт GitHub Wiki Markdown pages. Ручной список публичных типов не используется.

Каждая generated type page содержит блок `Godot 4.7 C# profile compatibility`. Для `Supported` API блок показывает `Supported / Parity verified` и `Out of profile: no`; для `Partial`, `Experimental` и `Planned` API показывает непроверенный status и `Out of profile: yes`.

## Текущий статус

На 2026-06-22 генератор создаёт `190` сгенерированных страниц: `Home.md`, `_Sidebar.md`, `_Footer.md`, `API-by-Category.md`, `API-Reference.md`, category pages и страницы публичных типов. Вместе с ручной compatibility page в `Electron2D.wiki.git` хранится полный API reference для текущего public runtime surface.

CI клонирует `Electron2D.wiki.git` в `.github/wiki`, запускает `tools\Update-ApiManifest.ps1 -WikiPath .github/wiki -Check`, затем запускает `tools\Update-ApiWiki.ps1 -OutputPath .github/wiki -Check`. Если public API, XML documentation, manifest или generated Wiki pages меняются несогласованно, CI падает.

Все внутренние ссылки генерируются без расширения `.md`, чтобы GitHub открывал rendered Wiki pages, а не raw Markdown.

## Что проверяет `-Check`

Verifier сравнивает expected generated Wiki pages с текущими файлами в клоне `Electron2D.wiki.git` и падает, если:

- сгенерированная page отсутствует;
- сгенерированная page устарела;
- осталась лишняя сгенерированная page;
- отсутствует `API-Compatibility.md`.
- tracked `data/api/electron2d-api-manifest.json` устарел относительно public API, XML documentation или `API-Compatibility.md`.

Дополнительный audit проверяет, что `Home.md`, `API-Reference.md`, `_Sidebar.md` и `API-Compatibility.md` связаны wiki-style ссылками без расширения `.md`, а compatibility page содержит status legend, current public runtime surface и planned preview surface.

Такой режим гарантирует, что GitHub Wiki остаётся частью обычного review и release gate без отслеживания Wiki pages в основном репозитории.
