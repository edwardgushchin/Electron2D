# Troubleshooting guide и release checklist

Статус: целевая спецификация для `T-0101`.
Обновлено: 2026-06-21.

## Назначение

Пользовательская документация Electron2D `0.1.0 Preview` должна содержать отдельную страницу с практическим troubleshooting guide и release checklist. Страница нужна для разработчика, который проверяет локальное окружение, первый проект, импорт ресурсов, сборку, shader artifacts, export presets, mobile lifecycle gaps и runtime diagnostics перед тем, как считать preview-сборку готовой к ручной проверке.

Документация должна описывать только фактически проверенный baseline. Если часть release path ещё не закрыта задачами `0.1.0 Preview`, она должна быть явно обозначена как gap, а не как готовая возможность.

## Обязательные области troubleshooting

Страница должна покрывать следующие области с конкретными симптомами, проверками и безопасными действиями:

- `import` issues: source assets, import cache, sidecar settings, stable UID и reimport checks;
- `build` issues: .NET SDK, restore/build/run, template verification и compiler diagnostics;
- `shader` issues: source format, import-time diagnostics, target artifacts и limitations;
- `export` issues: desktop verifiers, runtime identifier, self-contained package, signing references без секретов;
- `mobile lifecycle` issues: touch/orientation/safe area expectations, pause/resume smoke requirements и текущие blocked mobile export gaps;
- `runtime diagnostics` issues: user-code exceptions, lifecycle callbacks, group calls, deferred calls, signals и crash-safe reporting.

## Release checklist

Страница должна включать release checklist, который отделяет:

- обязательные локальные проверки repository baseline;
- desktop export checks;
- documentation/API checks;
- проверки, которые нельзя считать выполненными до закрытия mobile/export/reference-game задач.

Checklist не должен требовать публикации GitHub Release или загрузки release artifact без явной команды пользователя.

## Проверяемость

`tools\Verify-UserDocumentation.ps1` должен проверять:

- наличие страницы `docs/documentation/documentation/troubleshooting-release-checklist.md`;
- наличие ссылки на неё из `docs/documentation/documentation/user-guide.md`;
- наличие marker `user-doc:release-checklist` в user guide;
- наличие обязательных областей `import`, `build`, `shader`, `export`, `mobile lifecycle`, `runtime diagnostics`;
- наличие release checklist;
- наличие проверяемых команд: `tools\Verify-ProjectTemplate.ps1`, `tools\Run-Tests.ps1`, `tools\Verify-UserDocumentation.ps1`, `tools\Verify-WindowsExport.ps1`, `tools\Verify-LinuxExport.ps1`, `tools\Verify-MacOSExport.ps1`.

## Критерии приёмки

- Troubleshooting guide создан в `docs/documentation/documentation/troubleshooting-release-checklist.md`.
- User guide ссылается на troubleshooting guide и содержит release checklist section.
- Документация покрывает import, build, shader, export, mobile lifecycle and runtime diagnostics issues.
- Документация не публикует secrets, реальные credentials, private keys или signing payloads.
- `tools\Verify-UserDocumentation.ps1` проверяет новую страницу и проходит локально.
