VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:
- Проверены `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `repo-file-hashes.json`, весь `T-0209.patch`, доступный previous verdict file `docs/verdicts/release-management/t-0209-audit-r04.md` из diff, а также сырые evidence из `evidence/T-0209-r05/checks/*`, включая TRX `package-tests`.
- По `metadata.scopeTaskIds` пакет относится к одиночной задаче `T-0209`; `metadata.scopeSummary`, `AUDIT-MANIFEST.md`, `repo-file-hashes.json` и diff в целом согласованно описывают область как перенос локальной сборки релизных архивов и `release verify` во внутренний C#-инструмент без PowerShell как активного пути. Признаков hidden combined scope не найдено.
- `metadata.previousVerdictChain` содержит `docs/verdicts/release-management/t-0209-audit-r04.md`; файл доступен в текущем diff, его текст присутствует полным самостоятельным блоком без видимых сокращений, redaction markers или partial copy. Предыдущие blocker-ы r04 прочитаны и сопоставлены с `metadata.blockerClosureList`.
- Пакет нельзя принять, потому что закрытие предыдущего blocker-а B1 r04 заявлено сильнее, чем реализовано, а focused tests не доказывают закрытие ключевых fail-closed веток, связанных с инвентарём манифеста и неполным набором локальных релизных файлов.

BLOCKERS:
- B1
  - File/symbol: `eng/Electron2D.Build/ReleasePackageCommand.cs:268-290` (`VerifyTarget`), `eng/Electron2D.Build/ReleasePackageCommand.cs:480-486` (`VerifyManifestFile`), `eng/Electron2D.Build/ReleasePackageCommand.cs:612-626` (`VerifyManifestInventory`), `AUDIT-MANIFEST.md:153-156`, `metadata/audit-package.input.json` → `blockerClosureList[0]`.
  - Criterion: `previous blockers closure`, `implementation content review`, `documentation review`. Заявленное закрытие B1 r04 требует, чтобы `release verify` сверял один и тот же file-level inventory манифеста с каталогом подготовки и архивом.
  - Evidence: В `AUDIT-MANIFEST.md:154` и в `metadata.blockerClosureList[0]` записано, что B1 закрыт, потому что `release-manifest.json` теперь содержит детерминированные `files`, а `release verify` «сверяет этот список с каталогом подготовки и архивом». Но код делает две независимые проверки: staging manifest проверяется только против staging contents через `VerifyManifestFile(... GetPackageOutputFiles(packageRoot) ...)` (`ReleasePackageCommand.cs:480-486`, `612-626`), а manifest внутри архива проверяется только против archive contents через `VerifyManifestInventory(... GetArchiveOutputFiles(archiveEntries) ...)` (`ReleasePackageCommand.cs:286-290`, `612-626`). Сравнения staging manifest ↔ archive manifest или единого авторитетного inventory ↔ обе стороны в коде нет.
  - Impact: Предыдущий blocker B1 r04 фактически не доказан закрытым. Если staging package и archive будут изменены по-разному, но каждая сторона останется самосогласованной со своей копией манифеста, `release verify` это не поймает. Это оставляет риск расхождения между каталогом подготовки и конечным архивом при формально успешной проверке.
  - Fix: Сделать один авторитетный inventory обязательным для обеих сторон: либо сравнивать staging manifest с содержимым staging и archive одновременно, либо требовать byte-identical/structurally identical manifest в staging и archive и затем сверять единый список с обоими наборами файлов.
  - Verification: Добавить focused test, где archive manifest и archive contents намеренно расходятся со staging manifest/staging contents, но остаются внутренне согласованными между собой; `release verify` должен завершаться ошибкой. Повторно приложить `package-tests` evidence с этим сценарием.

- B2
  - File/symbol: `tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs` hunk в `T-0209.patch:2004-2026` (замена прежнего `ReleaseVerifyFailsClosedWithoutArtifacts`), `tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs` hunk в `T-0209.patch:2191-2207` (`PackageReleaseVerifyRejectsManifestWithoutFileInventory`), `eng/Electron2D.Build/ReleasePackageCommand.cs:225-238` (`E2D-BUILD-RELEASE-ARTIFACT-MISSING`), `eng/Electron2D.Build/ReleasePackageCommand.cs:612-626` (`E2D-BUILD-RELEASE-MANIFEST-INVENTORY`), `evidence/T-0209-r05/checks/package-tests/trx/test-result-001.trx:8-25,44-46,76-98`.
  - Criterion: `test coverage review`, `previous blockers closure`. B3 r04 и `metadata.blockerClosureList[2]` требуют доказуемого покрытия важных fail-closed веток, включая проверку file list и релизных инвариантов.
  - Evidence: В реализации есть отдельные failure paths `E2D-BUILD-RELEASE-ARTIFACT-MISSING` (`ReleasePackageCommand.cs:225-238`) и `E2D-BUILD-RELEASE-MANIFEST-INVENTORY` (`ReleasePackageCommand.cs:612-626`). В diff прежний тест `ReleaseVerifyFailsClosedWithoutArtifacts` удалён/заменён позитивным сценарием (`T-0209.patch:2004-2026`). В r05 TRX перечислены 18 прошедших тестов, среди которых есть `PackageReleaseVerifyRejectsManifestWithoutFileInventory`, `...ChecksumMismatch`, `...MissingRequired*`, `...ForbiddenFile*`, `PackageFailsClosedWhenDotnetPackFails` и `...PublishFails`, но нет сценария на missing-artifact branch и нет сценария, где `files` присутствует, но inventory неверен; единственный manifest-negative test покрывает schema regression без `files`, а не `E2D-BUILD-RELEASE-MANIFEST-INVENTORY` mismatch.
  - Impact: Focused suite не страхует две ключевые защитные ветки `release verify`: отказ на неполном наборе локальных релизных файлов и отказ на неверном file-level inventory при формально валидной форме манифеста. Из-за этого B3 r04 закрыт не полностью доказательством, а package evidence может остаться зелёным при регрессии именно в тех ветках, которые должны быть fail-closed.
  - Fix: Вернуть focused test на `release verify` без обязательных артефактов и добавить отдельный test на manifest inventory mismatch при наличии корректной схемы `outputs[*].files`. Лучше всего совместить второй сценарий с расхождением staging/archive inventory, чтобы одновременно закрыть B1.
  - Verification: Повторно выполнить `dotnet test tests/Electron2D.Tests.Integration/Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~RepositoryBuildToolTests.Package" --no-build --no-restore -v:minimal --logger "trx;LogFileName=T-0209-package-tests.trx"` и приложить TRX, где явно присутствуют passed tests для missing-artifact и inventory-mismatch failure paths.

EVIDENCE_REVIEW:
- Проверены служебные файлы пакета:
  - `AUDIT-MANIFEST.md` — metadata, scope, inventory, previous verdict chain, blocker closure list, checks.
  - `metadata/audit-package.input.json` — `metadata.scopeTaskIds`, `metadata.scopeSummary`, `metadata.previousVerdictChain`, `metadata.blockerClosureList`, checks, allowlist.
  - `repo-file-hashes.json` — список файлов репозитория в области изменения.
  - `AUDIT-REQUEST.md` — контракт внешнего аудита.
- Проверены изменённые файлы из diff:
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
  - generated docs index files:
    - `data/documentation/electron2d-local-docs-index.json`
    - `data/documentation/local-docs-index/documentation.ndjson`
  - previous verdict file:
    - `docs/verdicts/release-management/t-0209-audit-r04.md`
- Проверены evidence-папки и результаты:
  - `dotnet-build-integration`
  - `package-tests` и `package-tests/trx/test-result-001.trx`
  - `package-win-x64`
  - `package-linux-x64`
  - `package-osx-arm64`
  - `release-verify`
  - `update-docs-check`
  - `verify-docs`
  - `license-headers`
  - `git-diff-check`
- По evidence подтверждено, что заявленные локальные проверки r05 завершились с ожидаемым кодом `0`, включая focused suite `18/18`, три `package --rid`, `release verify`, `update docs --check`, `verify docs`, `dotnet build`, `license-headers` и `git diff --check`.
- Выполнен secret scanning по patch, metadata, manifest и evidence на типовые токены, private keys, passwords, secret-like assignments и локальные абсолютные пути. Реальные секреты не обнаружены.

RISKS_AND_NOTES:
- Реальных секретов, приватных ключей, токенов, паролей и несанаitized absolute local paths в проверенных материалах не найдено. В новом исходнике присутствует только публичный e-mail автора в license header `ReleasePackageCommand.cs`, что не выглядит секретом.
- Доступный previous verdict file r04 присутствует в пакете и был прочитан; по текущему diff не видно признаков его укорачивания или переоформления внутри самого архива.
- Non-blocking note: `CreateZipArchive` нормализует timestamp записей (`ReleasePackageCommand.cs:775-783`), а `CreateTarGzArchive` использует `TarFile.CreateFromDirectory` без аналогичной нормализации (`ReleasePackageCommand.cs:795-800`). Если позже потребуется строгая воспроизводимость tar.gz по байтам, это останется отдельным техническим риском.
- Non-blocking note: `metadata.repoFileAllowlist` не перечисляет `docs/verdicts/release-management/t-0209-audit-r04.md`, хотя этот файл есть в diff, `repo-file-hashes.json` и `metadata.previousVerdictChain`. Поскольку файл явно доступен и был проверен, это выглядит как drift упаковочной metadata, а не как доказуемый blocker текущей реализации.

CLOSURE_DECISION:
- Задача остаётся открытой до исправлений, потому что пакет не доказывает полное закрытие предыдущей внешней претензии B1 r04: `release verify` пока не сверяет один авторитетный file-level inventory одновременно с каталогом подготовки и архивом. Дополнительно focused tests не покрывают missing-artifact и real inventory-mismatch ветки, поэтому закрытие B3 r04 тоже не подтверждено достаточным regression evidence.
- После исправления реализации `release verify` и расширения focused suite отдельными сценариями на missing-artifact и inventory mismatch пакет можно повторно подавать на внешний аудит.
