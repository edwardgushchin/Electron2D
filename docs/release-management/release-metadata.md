# Версионирование и release metadata `0.1.0 Preview`

Обновлено: 2026-06-27.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация.
Задача: `T-0005`.
Обновлено: 2026-06-27.

## Цель

Runtime package должен иметь единый preview-version baseline, а changelog и release notes должны явно отражать clean rewrite state.

## Требуемые значения

- `Version`: `0.1.0-preview`
- `PackageVersion`: `0.1.0-preview`
- `AssemblyVersion`: `0.1.0.0`
- `FileVersion`: `0.1.0.0`
- `InformationalVersion`: `0.1.0-preview`
- `PackageId`: `Electron2D`
- `Authors`: `Electron2D Team`
- `PackageLicenseExpression`: `MIT`
- `PackageReadmeFile`: `README.md`
- `PackageIcon`: `electron2d_windows_icon_128.png`
- `RepositoryType`: `git`

`PackageIcon` берётся из `data/assets/branding/icon/electron2d_windows_icon_128.png` и пакуется в корень NuGet package рядом с `README.md`.

`Electron2D.Editor` должен использовать `data/assets/branding/icon/electron2d.ico` как `ApplicationIcon`, чтобы desktop executable сразу имел брендовую иконку.

## Документы релиза и локальные черновики

Удалённый репозиторий не должен отслеживать рабочие файлы:

- `CHANGELOG*`
- `RELEASE-NOTES*`

Maintainer может держать такие файлы локально как черновики будущего GitHub Release. Публичная release metadata для репозитория хранится в `README.md`, project metadata и GitHub Release, который создаётся только по явной команде maintainer-а.

## Breaking changes policy для `0.x`

Пока Electron2D находится в ветке `0.x`, публичный API может меняться между preview-сборками. Любое breaking change должно быть явно отражено в публичном release text или локальном release draft перед публикацией GitHub Release; compatibility layer ради старого API не добавляется.

## Верификация

```bash
dotnet run --project eng\Electron2D.Build -- verify release-metadata
```

Verifier сверяет package metadata в `src/Electron2D/Electron2D.csproj`, иконку `Electron2D.Editor`, наличие брендовых файлов, ссылки README на брендовые SVG, отсутствие tracked release draft файлов и упоминание package version `0.1.0-preview` в README. Команда выполняется C#-инструментом репозитория и выводит структурированные JSON-диагностики. Для `T-0214` `verify release-metadata` является единственной целевой поверхностью проверки release metadata.

## Фактическое состояние, ограничения и проверки

Статус: реализованный metadata baseline.
Задача: `T-0005`.
Обновлено: 2026-06-27.

## Package metadata

Runtime package `src/Electron2D/Electron2D.csproj` использует:

- `Version`: `0.1.0-preview`
- `PackageVersion`: `0.1.0-preview`
- `AssemblyVersion`: `0.1.0.0`
- `FileVersion`: `0.1.0.0`
- `InformationalVersion`: `0.1.0-preview`
- `PackageId`: `Electron2D`
- `Authors`: `Electron2D Team`
- `PackageLicenseExpression`: `MIT`
- `PackageReadmeFile`: `README.md`
- `PackageIcon`: `electron2d_windows_icon_128.png`
- `RepositoryType`: `git`

Runtime package пакует `data/assets/branding/icon/electron2d_windows_icon_128.png` в корень package как `electron2d_windows_icon_128.png`. `Electron2D.Editor` использует `data/assets/branding/icon/electron2d.ico` как `ApplicationIcon` для desktop executable.

## Релизные документы и локальные черновики

- `CHANGELOG*` и `RELEASE-NOTES*` являются локальными черновиками maintainer-а и не отслеживаются Git.
- Публичная release metadata для репозитория сейчас живёт в `README.md` и project metadata.
- Breaking changes policy для `0.x` описывает, что публичный API может меняться между preview-сборками без compatibility layer, но каждое breaking change должно быть явно записано в публичном release text или локальном release draft перед публикацией GitHub Release.

## Проверка

```bash
dotnet run --project eng\Electron2D.Build -- verify release-metadata
```

Команда проверяет согласованность package metadata, README, брендовых файлов, иконки `Electron2D.Editor` и отсутствие tracked release draft файлов.
