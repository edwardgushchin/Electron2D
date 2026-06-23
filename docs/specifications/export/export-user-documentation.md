# Export user documentation

Статус: целевая спецификация для `T-0098`.
Обновлено: 2026-06-23.

## Назначение

Пользовательская export-документация Electron2D `0.1.0 Preview` должна дать проверяемый путь для поддерживаемых desktop export targets, описать WebAssembly browser package/smoke baseline и честно обозначить mobile/web execution gaps. Документация не должна выдавать Android/iOS export или remote WebAssembly hosting как готовый release path до закрытия соответствующих platform tasks и реальных smoke checks.

## Обязательные страницы

Документация должна включать:

- общий export guide в `docs/documentation/export/export-guide.md`;
- platform page для `WindowsX64`;
- platform page для `LinuxX64`;
- platform page для `MacOSArm64`;
- status page для `AndroidArm64`;
- status page для `IosArm64`.
- status page для `WebAssemblyBrowser`.

## Содержание

Общий export guide должен описывать:

- текущую target matrix;
- различие между проверенным desktop export и заблокированным mobile export;
- различие между WebAssembly browser package/smoke baseline, внешним `dotnet publish` и future browser automation workflow;
- формат `export_presets.e2export.json`;
- SDK/toolchain requirements;
- signing и credential references без secret values;
- known limitations;
- команды проверки.

Каждая platform page должна описывать:

- target name и runtime identifier;
- host requirements;
- SDK/toolchain prerequisites;
- signing или отсутствие signing в текущем verifier;
- ограничения;
- локальную или CI-проверку.

Mobile status pages должны описывать SDK, signing, credentials и known limitations как требования будущей реализации, но не должны содержать команду, которая выглядит как готовый mobile release export.

WebAssembly status page должна описывать `browser-wasm`, static package layout, CLI commands `plan-web`, `build-web`, `run-web`, browser policies, smoke criteria, WebAssembly build tools, local smoke artifact и ограничения remote hosting/browser automation.

## Secret safety

Документация не должна содержать реальные passwords, tokens, private keys, certificates, keystore payloads, provisioning profiles или signing secrets. Разрешены только placeholder values и opaque references вроде имени переменной окружения или имени секрета CI.

## Проверяемость

`tools\Verify-ExportDocumentation.ps1` должен проверять:

- наличие всех обязательных страниц;
- наличие обязательных разделов и platform fragments;
- отсутствие запрещённых фрагментов, похожих на реальные секреты;
- отсутствие формулировок, обещающих готовый Android/iOS export;
- отсутствие формулировок, обещающих готовый remote WebAssembly hosting или automated browser launch без отдельного browser smoke artifact;
- наличие ссылок на desktop verifier scripts.

## Критерии приёмки

- Export guide и platform pages существуют.
- Desktop pages описывают SDK/toolchain, signing/credentials policy, limitations и verifier commands.
- Android/iOS pages описывают SDK/signing/status requirements и явно фиксируют, что mobile export не является готовым release path.
- WebAssembly page описывает `WebAssemblyBrowser`, `browser-wasm`, package layout, CLI plan/build/run commands, browser policies, smoke artifact и ограничения.
- User guide ссылается на export guide.
- `tools\Verify-ExportDocumentation.ps1` проходит локально и подключён к CI.
