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

`Home.md`, `_Sidebar.md`, `_Footer.md`, `API-by-Category.md`, category pages, `API-Reference.md` и страницы типов создаются автоматически. `API-Compatibility.md` остаётся отдельной GitHub Wiki page с release/status таблицей, не перезаписывается генератором и не публикует отдельный блок removed/legacy API.

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

Генератор собирает `src\Electron2D\Electron2D.csproj` с XML documentation file, читает compiled public surface из runtime assembly и создаёт GitHub Wiki Markdown pages. Ручной список публичных типов не используется.

## Текущий статус

На 2026-06-21 генератор создаёт `136` сгенерированных страниц: `Home.md`, `_Sidebar.md`, `_Footer.md`, `API-by-Category.md`, `API-Reference.md`, `11` category pages и `120` страниц публичных типов. Вместе с ручной compatibility page в `Electron2D.wiki.git` хранится `137` Markdown-файлов.

CI клонирует `Electron2D.wiki.git` в `.github/wiki` и запускает `tools\Update-ApiWiki.ps1 -OutputPath .github/wiki -Check`. Если public API или XML documentation меняются, но GitHub Wiki не обновлена, CI падает.

Все внутренние ссылки генерируются без расширения `.md`, чтобы GitHub открывал rendered Wiki pages, а не raw Markdown.

## Что проверяет `-Check`

Verifier сравнивает expected generated Wiki pages с текущими файлами в клоне `Electron2D.wiki.git` и падает, если:

- сгенерированная page отсутствует;
- сгенерированная page устарела;
- осталась лишняя сгенерированная page;
- отсутствует `API-Compatibility.md`.

Такой режим гарантирует, что GitHub Wiki остаётся частью обычного review и release gate без отслеживания Wiki pages в основном репозитории.
