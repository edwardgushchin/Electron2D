VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:
- Проверены `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `repo-file-hashes.json`, весь `T-0209.patch`, изменённые код/тесты/документы из patch и сырые evidence из `evidence/T-0209-r04/checks/*`, включая `package-tests` TRX.
- По `metadata.scopeTaskIds` область пакета — одиночная задача `T-0209`; `metadata.scopeSummary`, `AUDIT-MANIFEST.md`, allowlist в metadata и список файлов в diff согласованы между собой. Признаков hidden combined scope не найдено.
- `metadata.previousVerdictChain` и `metadata.blockerClosureList` пусты, поэтому проверка previous verdict files, verbatim preservation и previous blockers closure в этом пакете неприменима.
- Изменение нельзя принять, потому что реализованный и протестированный контракт расходится с собственными обновлёнными доменными документами: релизный манифест не фиксирует фактический список файлов, а документация заявляет проверку export-документации командой `release verify`, которой в коде нет. Дополнительно release-verify tests не покрывают ключевые отказные ветки.

BLOCKERS:
- B1
  - File/symbol: `eng/Electron2D.Build/ReleasePackageCommand.cs:379-395` (`WriteReleaseManifest`), `eng/Electron2D.Build/ReleasePackageCommand.cs:528-546` (`VerifyManifestOutputs`), `docs/release-management/release-packaging.md:66-75`, `tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:5903-5918` (`AssertReleaseManifestShape`).
  - Criterion: Доменный контракт требует, чтобы `release-manifest.json` фиксировал «список включённых выходных файлов `dotnet publish` и пакета библиотеки среды выполнения», а tests должны закрывать manifest/layout contract.
  - Evidence: В документе `docs/release-management/release-packaging.md:66-75` явно требуется список включённых файлов. В коде `WriteReleaseManifest` записывает только три агрегированных записи по каталогам — `library/`, `editor/`, `tools/e2d/` — без перечисления фактически включённых файлов (`ReleasePackageCommand.cs:381-393`). Проверка манифеста тоже подтверждает только наличие этих трёх каталогов (`ReleasePackageCommand.cs:528-546`). Тестовый helper повторяет это же упрощение и не требует file-level inventory (`RepositoryBuildToolTests.cs:5915-5918`).
  - Impact: Манифест не выполняет собственный документированный контракт и не пригоден как точное доказательство состава релизного архива. Регрессии внутри `editor/` или `tools/e2d/` могут пройти `release verify` и tests, пока каталог просто не пуст.
  - Fix: Перевести `outputs` в детерминированный список фактически включённых файлов/артефактов релиза, а `release verify` — в проверку этого списка против staging и содержимого архива. Обновить tests так, чтобы они падали при несовпадении file-level inventory.
  - Verification: Повторно выполнить `dotnet test tests/Electron2D.Tests.Integration/Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~RepositoryBuildToolTests.Package" --no-build --no-restore -v:minimal` и приложить evidence/generated `release-manifest.json`, где перечислены конкретные файлы, а не только каталоги.

- B2
  - File/symbol: `docs/export/export-guide.md:67-72`, `docs/export/export-guide.md:185-190`, `eng/Electron2D.Build/Program.cs:203-213`, `eng/Electron2D.Build/ReleasePackageCommand.cs:196-216`, `evidence/T-0209-r04/checks/release-verify/stdout.txt`.
  - Criterion: Documentation review требует, чтобы документация соответствовала фактическому поведению инструмента.
  - Evidence: `docs/export/export-guide.md:67-72` утверждает, что `eng\Electron2D.Build` «должен проверять релизные файлы настольных платформ и export-документацию». Ниже, в разделе «Проверка документации», документ предписывает запускать `dotnet run --project eng/Electron2D.Build -- release verify` (`docs/export/export-guide.md:185-190`). Но код `Program.cs:203-213` маршрутизирует `release verify` только в `ReleasePackageCommand.VerifyAsync`, а этот метод лишь итерирует три runtime identifier и проверяет локальные release artifacts (`ReleasePackageCommand.cs:196-216`). В evidence `release-verify/stdout.txt` есть только сообщение `E2D-BUILD-RELEASE-VERIFY-PASSED` про local release artifacts, без каких-либо doc-проверок.
  - Impact: Обновлённая документация обещает проверку export-документации, которой реализация не делает. Это вводит в заблуждение сопровождающего и позволяет пропустить документарные регрессии, если следовать инструкции из docs.
  - Fix: Либо исправить docs так, чтобы они ссылались на реально существующие проверки документации (`update docs --check`, `verify docs` и/или исторический verifier до `T-0210`), либо реализовать заявленную export-doc verification в C# и подтвердить её evidence.
  - Verification: Обновлённые документы без ложного утверждения о doc-проверке через `release verify`, либо новый evidence/тесты на C#-команду, которая действительно читает и проверяет export-документацию.

- B3
  - File/symbol: `tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:1156-1215`, `tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:5903-5918`, `evidence/T-0209-r04/checks/package-tests/trx/test-result-001.trx`.
  - Criterion: Test coverage review требует покрытия важных веток поведения, ограничений и заявленных релизных инвариантов: checksum, manifest, forbidden file policy и release-verify failure paths.
  - Evidence: По TRX `evidence/T-0209-r04/checks/package-tests/trx/test-result-001.trx` в focused suite прошло 11 тестов. Из новых `release verify` negative-path tests присутствует только `PackageReleaseVerifyRejectsForbiddenFilesInStagingAndArchive`, но код этого теста мутирует только staging-путь `artifacts/release/0.1.0-preview/win-x64/package/dev-diary/notes.md` (`RepositoryBuildToolTests.cs:1195-1215`) и не покрывает archive-side rejection, хотя название теста заявляет и staging, и archive. В suite также нет тестов на `E2D-BUILD-RELEASE-CHECKSUM-MISMATCH`, `E2D-BUILD-RELEASE-MANIFEST-SCHEMA`, отсутствие обязательных путей, либо на сбои/timeout `dotnet pack`/`dotnet publish`. Helper `AssertReleaseManifestShape` дополнительно проверяет лишь directory-level manifest shape (`RepositoryBuildToolTests.cs:5903-5918`).
  - Impact: Ключевые отказные ветки `release verify` и `package` остаются без regression-net. Это особенно критично, потому что именно эти ветки должны гарантировать fail-closed поведение при повреждённых checksum/manifest/archive policy.
  - Fix: Добавить focused integration tests минимум для: archive forbidden entry, checksum mismatch, malformed/underspecified manifest, missing required path в staging/archive и неуспешных `pack/publish` steps. Исправить misleading test name или реально покрыть обе ветки.
  - Verification: Повторный `package-tests` с расширенным TRX, где есть отдельные passed tests на соответствующие diagnostic codes и archive-side failure cases.

EVIDENCE_REVIEW:
- Проверены служебные файлы пакета:
  - `AUDIT-MANIFEST.md` — scope, inventory, checks, file list.
  - `metadata/audit-package.input.json` — `metadata.scopeTaskIds`, `metadata.scopeSummary`, `metadata.previousVerdictChain`, `metadata.blockerClosureList`, allowlist, checks.
  - `repo-file-hashes.json` — список репозиторных файлов в области изменения.
  - `AUDIT-REQUEST.md` — контракт внешнего аудита.
- Проверены изменённые репозиторные файлы по patch:
  - `eng/Electron2D.Build/Program.cs`
  - `eng/Electron2D.Build/ReleasePackageCommand.cs`
  - `tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`
  - `docs/release-management/release-packaging.md`
  - `docs/release-management/ci-matrix.md`
  - `docs/export/export-guide.md`
  - `docs/export/windows-x64-export.md`
  - `docs/export/linux-x64-export.md`
  - `docs/export/macos-arm64-export.md`
  - `TASKS.md`
  - `dev-diary/2026/06 Июнь/30-06-2026.md`
  - regenerated docs index files:
    - `data/documentation/electron2d-local-docs-index.json`
    - `data/documentation/local-docs-index/documentation.ndjson`
- Проверены evidence-папки и их результаты:
  - `dotnet-build-integration`
  - `package-tests`
  - `package-win-x64`
  - `package-linux-x64`
  - `package-osx-arm64`
  - `release-verify`
  - `update-docs-check`
  - `verify-docs`
  - `license-headers`
  - `git-diff-check`
- Отдельно просмотрены:
  - `evidence/T-0209-r04/checks/package-tests/trx/test-result-001.trx`
  - `stdout/stderr/command/env/exit-code` для перечисленных checks.
- Выполнен secret scanning по patch и evidence на типовые токены, private keys, password/token assignments и абсолютные локальные пути.

RISKS_AND_NOTES:
- Область задачи согласована: single-task scope `T-0209`, признаков лишних файлов вне allowlist не найдено.
- `metadata.previousVerdictChain` пуст; hidden blocker через отсутствующие previous verdict files не доказан.
- По secret scanning реальные секреты, приватные ключи, токены, пароли и несанаitized absolute local paths не обнаружены. Найден только публичный e-mail автора в license header нового файла (`eduardgushchin@yandex.ru`), что не выглядит секретом.
- Все заявленные checks в evidence завершились с ожидаемым кодом `0`, но успешные command transcripts не снимают несоответствия контракта манифеста, документации и покрытия tests.

CLOSURE_DECISION:
- Задача остаётся открытой до исправлений, потому что текущий пакет не доказывает выполнение собственного контракта `release-manifest.json`, обновлённая документация описывает несуществующую doc-проверку через `release verify`, а focused integration tests не закрывают ключевые fail-closed ветки `package`/`release verify`. После исправления этих трёх пунктов и повторного прогона evidence пакет можно подавать на повторный внешний аудит.
