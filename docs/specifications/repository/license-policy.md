# Политика лицензирования исходного кода

## Цель

Electron2D `0.1.0 Preview` должен распространяться по MIT License. Лицензионное состояние проекта не должно зависеть только от NuGet metadata или корневого файла: каждый вручную написанный source-файл должен начинаться с явного MIT license header в стиле SDL, чтобы условия лицензии сохранялись при копировании отдельного файла.

## Контракт

- Корневой файл `LICENSE` содержит стандартный текст MIT License для `Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>`.
- `src/Electron2D/Electron2D.csproj` сохраняет `PackageLicenseExpression` со значением `MIT`.
- Все отслеживаемые Git вручную написанные C# source-файлы `*.cs` в `src/`, `tests/` и `templates/` начинаются с полного MIT license header.
- Все отслеживаемые Git PowerShell source-файлы `*.ps1` в `tools/` начинаются с полного MIT license header в block-comment форме.
- Сгенерированные файлы в `bin/`, `obj/`, package output, coverage output и временные файлы не входят в область проверки.
- Новые source-файлы без header должны ломать локальную проверку и CI.

Источник канонического текста лицензии: [SPDX MIT License](https://spdx.org/licenses/MIT). SPDX указывает, что у MIT License нет единого обязательного standard header, поэтому Electron2D закрепляет собственный полный header в этой спецификации.

## Форма header

C# файлы используют обычный block comment `/* ... */`.

PowerShell файлы используют block comment `<# ... #>`, чтобы `param(...)` оставался первой исполняемой конструкцией скрипта.

Header содержит:

- название проекта `Electron2D`;
- строку `MIT License`;
- copyright;
- `SPDX-License-Identifier: MIT`;
- полный текст условий MIT License.

## Проверка

Обязательная команда:

```powershell
powershell -ExecutionPolicy Bypass -File tools/Verify-SourceLicenseHeaders.ps1
```

GitHub Actions должен запускать эту проверку до тестов.

## Критерии приёмки

- `LICENSE` соответствует MIT License.
- Все source-файлы в области проверки имеют корректный header.
- Проверка падает на отсутствующем или изменённом header.
- Документация реализации описывает фактическую область проверки.
