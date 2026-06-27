# T-0213 audit r03

- Задача: T-0213
- Домен: documentation
- Актуально на: 2026-06-27T09:22:25+03:00
- Область проверки: acceptance audit приложенного архива `T-0213-audit-r03.zip`; архив, manifest, checks/evidence, patch restore от baseline, закрытие r01/r02 blocker-ов и соответствие T-0213 scope.
- Статус вывода: VERDICT: NEEDS_FIXES
- Предыдущий аудит: docs/verdicts/documentation/t-0213-audit-r02.md
- Следующий аудит: docs/verdicts/documentation/t-0213-audit-r04.md

## Ответ аудитора

VERDICT: NEEDS_FIXES

Blocker: evidence/T-0213-r03/checks/repository-build-tool-tests/trx/test-result-001.trx, критерий: архив/evidence должны соответствовать AUDIT-MANIFEST.md Forbidden Policy — .git, bin, obj, temp paths, nested archives, .env, secrets and tokens are forbidden; evidence: текущий TRX содержит secret/token/private-key-shaped literals в testName/UnitTest name: api_key = not-a-real-api-key-value line 18/352, token=not-a-real-token-value line 31/324, -----BEGIN PRIVATE KEY----- line 60/128, password=not-a-real-password-value line 69/356; fix: убрать secret-like значения из сериализуемых test display names/TRX — заменить theory parameter display на безопасные case labels либо генерировать проверочные payload-строки из фрагментов внутри теста без попадания literal в TRX/package, затем пересобрать deterministic audit package; проверка: grep -RIn --binary-files=text -E '-----BEGIN .*PRIVATE KEY|\b(api[_-]?key|token|password)\s*[:=]' evidence/ T-0213.patch AUDIT-MANIFEST.md AUDIT-REQUEST.md repo-file-hashes.json не должен находить текущих evidence/source-попаданий, после этого sha256sum -c SHA256SUMS.txt, git apply --check T-0213.patch от baseline eb35f5c954f3aabf4b89ce111a5466c5ba73912b, dotnet test tests/Electron2D.Tests.Integration/Electron2D.Tests.Integration.csproj --filter FullyQualifiedName~RepositoryBuildToolTests, dotnet run --project eng/Electron2D.Build -- verify readme, dotnet run --project eng/Electron2D.Build -- verify docs, dotnet run --project eng/Electron2D.Build -- update docs --check.
