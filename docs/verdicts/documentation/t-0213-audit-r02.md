# Аудит T-0213 r02: README/docs verifier-ы в C# repository tool

- Задача: `T-0213`
- Домен: `documentation`
- Актуально на: 2026-06-26T17:04:39+03:00
- Область проверки: `T-0213-audit-r02.zip`, `AUDIT-MANIFEST.md`, `SHA256SUMS.txt`, `patch/T-0213.patch`, evidence, workflow-файлы, `eng/Electron2D.Build`, focused tests, documentation-domain и generated local docs index.
- Статус вывода: `VERDICT: NEEDS_FIXES`
- Предыдущий аудит: `docs/verdicts/documentation/t-0213-audit-r01.md`.
- Следующий аудит: `docs/verdicts/documentation/t-0213-audit-r03.md`.

## Полный ответ аудитора

```text
VERDICT: NEEDS_FIXES

Blocker: patch/T-0213.patch, restore model от baseline 030871c7ef29fb2943e49eac62d76506d6984465; критерий: patch должен воспроизводимо восстанавливать repository-owned изменения из архива. Evidence: новые файлы в patch имеют неверные hunk headers: @@ -0,0 +25 @@, @@ -0,0 +696 @@, @@ -0,0 +74 @@, @@ -0,0 +510 @@ вместо формата @@ -0,0 +1,N @@. git apply --numstat patch/T-0213.patch считает для docs/verdicts/documentation/t-0213-audit-r01.md, eng/Electron2D.Build/LocalDocumentationVerifier.cs, eng/Electron2D.Build/RepositoryPaths.cs, eng/Electron2D.Build/RepositoryReadmeVerifier.cs только по 1 added line. При test-apply этих new-file hunks создаются однострочные файлы: LocalDocumentationVerifier.cs = 1 line /*, RepositoryReadmeVerifier.cs = 1 line /*, RepositoryPaths.cs = 1 line /*, verdict r01 = только заголовок. Такой restore не компилирует C# tool, не восстанавливает r01 verdict и ломает verify docs/generated index hash chain. Fix: пересобрать patch/T-0213.patch штатным git diff/git format-patch без ручной порчи hunk headers; для new files должны быть headers вида @@ -0,0 +1,696 @@ и полный body. Проверка: на чистом baseline 030871c7ef29fb2943e49eac62d76506d6984465 выполнить git apply --check patch/T-0213.patch, затем git apply patch/T-0213.patch; проверить wc -l/hash restored new files против archive files, затем запустить dotnet test ... --filter FullyQualifiedName~RepositoryBuildToolTests, dotnet run --project eng/Electron2D.Build -- verify readme, verify docs, update docs --check, Verify-LocalDocumentation.ps1, Verify-SourceLicenseHeaders.ps1, Verify-Tasks.ps1, git diff --check.
```
