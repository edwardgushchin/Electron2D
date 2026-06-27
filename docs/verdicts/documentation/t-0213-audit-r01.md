# Аудит T-0213 r01: README/docs verifier-ы в C# repository tool

- Задача: `T-0213`
- Домен: `documentation`
- Актуально на: 2026-06-26T15:25:49+03:00
- Область проверки: `T-0213-audit-r01.zip`, `AUDIT-MANIFEST.md`, `SHA256SUMS.txt`, `patch/T-0213.patch`, workflow-файлы, `eng/Electron2D.Build`, focused tests, documentation-domain и generated local docs index.
- Статус вывода: `VERDICT: NEEDS_FIXES`
- Предыдущий аудит: нет.
- Следующий аудит: `docs/verdicts/documentation/t-0213-audit-r02.md`.

## Полный ответ аудитора

```text
VERDICT: NEEDS_FIXES

Blocker: AUDIT-MANIFEST.md, критерий: manifest должен быть пригоден для строгой проверки archive inventory/evidence/restore. Evidence: manifest повреждён управляющими символами: eng/Electron2D.Build записан как \x1bng/Electron2D.Build, tests/... как \tests/... с tab вместо t, verify readme/verify docs как \x0berify .... Из-за этого manifest не является машинно-проверяемым inventory и не может надёжно подтверждать task-owned paths. Fix: пересобрать AUDIT-MANIFEST.md в normal UTF-8 text без control chars, с точными POSIX archive paths и командами. Проверка: python - <<'PY' ... assert no chars < 32 except \\t/\\n/\\r if tabs are intentionally disallowed then no \\t either ... PY, затем сравнить manifest file list с zipinfo -1 T-0213-audit-r01.zip и sha256sum -c SHA256SUMS.txt.

Blocker: archive evidence, критерий: patch/manifest/checksum/workflow files и archive contents должны быть достаточны для проверки r01, включая red/green/checks. Evidence: архив содержит только narrative в повреждённом AUDIT-MANIFEST.md; отдельных inspectable logs/TRX/stdout files для red TDD, green focused tests, verify readme, verify docs, update docs --check, update docs, Verify-LocalDocumentation.ps1, Verify-SourceLicenseHeaders.ps1, Verify-Tasks.ps1 и git diff --check нет. При строгом аудите это не доказывает заявленные exit codes и не позволяет отличить фактический прогон от ручной записи. Fix: добавить evidence/ или checks/ с raw logs/exit-code files/TRX для baseline-red и final-green, плюс краткий индекс evidence в manifest. Проверка: manifest должен ссылаться на существующие evidence-файлы; sha256sum -c SHA256SUMS.txt должен покрывать их; каждый заявленный check должен иметь inspectable command, exit code и stdout/stderr или TRX.

Blocker: tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs, критерий: tests должны фиксировать не только route success, но и заявленный контракт verify readme, verify docs, update docs --check, update docs. Evidence: новые тесты проверяют в основном happy-path diagnostics: VerifyReadmeRunsRepositoryReadmeContract ожидает только E2D-BUILD-README-VERIFY-PASSED; VerifyDocsRunsLocalDocumentationContourAndChecksGeneratedIndexMetadata проверяет только presence двух codes; UpdateDocsCheckRunsGeneratedDocumentationIndexCheck и UpdateDocsRefreshesGeneratedDocumentationIndex проверяют только success code. Нет negative fixture tests на duplicate tagline, forbidden README wording, non-allowlisted SVG, исторические упоминания вне README, stale generated index, missing command metadata, missing API reference, missing source path. Fix: добавить fixture-based tests через временный минимальный repo или unit-level verifier harness: README fail/pass cases, docs index fail/pass cases, stale index check, update docs mutation check без загрязнения основной working tree. Проверка: новые tests должны падать на прежнем skeleton/недостаточном verifier-е и проходить после реализации; final run должен включать raw evidence.

Blocker: eng/Electron2D.Build/LocalDocumentationVerifier.cs, критерий: verify docs должен проверять generated docs index/schema/API references. Evidence: VerifySources требует только наличие ключа sources.wiki, но если sources.wiki существует с неправильным JSON kind, например string/array/null, код не добавляет schema error; блок проверки generator/compatibilityPage выполняется только при wiki.ValueKind == Object. Это оставляет дыру в schema validation. Fix: явно требовать sources.wiki как object и падать, если kind не object; аналогично закрепить тестом. Проверка: fixture index с "wiki": "bad" должен давать non-zero и diagnostic вроде E2D-BUILD-DOCS-INDEX-SOURCES; валидный index должен проходить.

Blocker: docs/documentation/repository-readme.md ↔ RepositoryReadmeVerifier.cs, критерий: доменные документы должны быть согласованы с фактическим поведением verifier-а. Evidence: доменный документ показывает Platforms table rows с backtick-wrapped statuses, например | Windows | `✅ Done` | `✅ Done` |, а verifier требует raw rows без backticks: | Windows | ✅ Done | ✅ Done |. Это прямое расхождение спецификации и реализации. Fix: выбрать один контракт: либо убрать backticks из доменного table example, либо изменить verifier на acceptance ровно того формата, который описан в документе. Проверка: focused test на README platforms table должен соответствовать обновлённому доменному документу.
```
