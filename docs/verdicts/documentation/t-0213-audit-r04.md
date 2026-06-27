# T-0213 audit r04

- Задача: T-0213
- Домен: documentation
- Актуально на: 2026-06-27T10:45:22+03:00
- Область проверки: acceptance audit приложенного архива `T-0213-audit-r04.zip`; архив, manifest, checks/evidence, patch restore от baseline, закрытие r01/r02/r03 blocker-ов и соответствие T-0213 scope.
- Статус вывода: VERDICT: NEEDS_FIXES
- Предыдущий аудит: docs/verdicts/documentation/t-0213-audit-r03.md
- Следующий аудит: docs/verdicts/documentation/t-0213-audit-r05.md

## Ответ аудитора

VERDICT: NEEDS_FIXES

Blocker: T-0213.patch, критерий: r03/r04 secret-scan closure допускает verbatim secret-like строки только внутри previousVerdictChain historical verdict diff-блоков; ordinary task-owned patch content не должен содержать private-key-shaped literals. evidence: T-0213.patch вне diff-блоков docs/verdicts/documentation/t-0213-audit-r01.md/r02.md/r03.md содержит task-owned hunk tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs со строкой - return string.Concat("-----BEGIN ", "PRIVATE KEY-----");, которая матчится проверкой grep -nE -- '-----BEGIN .*PRIVATE KEY|\b(api[_-]?key|token|password)\s*[:=]' T-0213.patch; TRX текущего r04 уже чистый по r03 pattern, но patch artifact всё ещё несёт private-key-shaped literal вне разрешённого previous-verdict исключения. fix: пересобрать r04 так, чтобы secret-scan исключение применялось только к previousVerdictChain, а ordinary task-owned patch/current-file/evidence/TRX оставались чистыми; если policy намеренно разрешает deleted baseline lines, это должно быть явно зафиксировано в docs/release-management/audit-package.md, покрыто focused test и отражено в audit package verify, иначе patch должен не содержать такой literal. проверка: grep -nE -- '-----BEGIN .*PRIVATE KEY|\b(api[_-]?key|token|password)\s*[:=]' T-0213.patch evidence AUDIT-MANIFEST.md AUDIT-REQUEST.md repo-file-hashes.json metadata/audit-package.input.json должен возвращать только разрешённые previousVerdictChain historical verdict hits либо ничего; затем sha256sum -c SHA256SUMS.txt, git apply --check T-0213.patch от baseline eb35f5c954f3aabf4b89ce111a5466c5ba73912b, focused RepositoryBuildToolTests, verify readme, verify docs, update docs --check.
