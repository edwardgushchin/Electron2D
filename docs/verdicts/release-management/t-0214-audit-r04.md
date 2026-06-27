# Внешний аудит T-0214 r04

- Задача: T-0214
- Домен: release-management
- Актуально на: 2026-06-27T22:50:27+03:00
- Область проверки: r04 audit package после r03 `VERDICT: NEEDS_FIXES`; проверка закрытия r01/r02/r03 blocker-ов, evidence-only `TASKS.md`, восстановления, previous verdict chain и текущей C#-поверхности API/Wiki/license/manifest verifier-ов.
- Статус вывода: VERDICT: NEEDS_FIXES
- Предыдущий аудит: `docs/verdicts/release-management/t-0214-audit-r03.md`
- Следующий аудит: r05 после решения B1-B2 по контракту audit package или evidence-based оспаривания вне области T-0214.

## Ответ аудитора verbatim

VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:
- Проверены структура архива, инвентарь, `SHA256SUMS.txt`, `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `repo-file-hashes.json`, `T-0214.patch`, весь каталог `evidence/`, TRX-файл и цепочка `metadata.previousVerdictChain`.
- Формальная целостность архива в целом хорошая: checksum-слой согласован, структура ZIP безопасна, `repo-file-hashes.json`, manifest и patch согласованы по одному и тому же набору из 17 repo-owned файлов, evidence для всех 14 checks присутствует и по metadata совпадает с `metadata.audit-package.input.json`.
- По содержанию patch/tests/docs видно, что автор действительно пытался закрыть прежние blocker-ы: в новом `eng/Electron2D.Build/RepositoryPolicyVerifiers.cs` есть детерминированная проверка Wiki-content, structured diagnostics для non-object JSON roots, C#-маршруты `verify licenses`, `verify manifests`, `verify release-metadata`, `verify project-template`, `verify api-compatibility`, `update wiki`, `update api-manifest`; в `tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs` добавлены focused tests на stale Wiki content, missing Wiki pages, invalid manifest shape, non-object JSON roots и guard tests против legacy PowerShell surface; `TASKS.md` вынесен в `evidence/.../archive-only/`, а не в repo-owned patch.
- Однако архив нельзя принять как самодостаточное и воспроизводимое доказательство именно потому, что две обязательные части проверки остаются непроверяемыми изнутри самого архива: восстановление baseline-ревизии и независимая проверка verbatim preservation для previous verdict files.

BLOCKERS:
- B1
  - File/symbol: `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `repo-file-hashes.json`, `T-0214.patch`, restore step `git apply --check`
  - Criterion: обязательные `restore scanning` и шаги порядка проверки требуют в отдельной чистой копии исходной ревизии применить patch через `git apply --check`, затем `git apply`, после чего сравнить восстановленные файлы с `repo-file-hashes.json`; архив должен быть самодостаточным и не требовать внешних догадок.
  - Evidence: архив содержит только идентификатор baseline `5f702646abdaba6b21504ad49a9f9fd849b94fb4` в `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json` и `repo-file-hashes.json`, но не содержит ни clean copy этой ревизии, ни bundle/tarball/snapshot, ни иного repo payload для её восстановления. `AUDIT-MANIFEST.md` при этом прямо заявляет: `T-0214.patch restores repository-owned files from baseline.` При попытке выполнить обязательный шаг в чистом git-репозитории `git apply --check /tmp/t0214/T-0214.patch` команда завершается ошибкой с сообщениями `No such file or directory` для всех изменяемых baseline-файлов (`data/documentation/electron2d-local-docs-index.json`, `docs/...`, `eng/...`, `tests/...`). Изнутри архива можно проверить только внутреннюю согласованность списков путей и hash-модели, но нельзя восстановить байты модифицированных baseline-файлов и выполнить полный restore scan.
  - Impact: acceptance нельзя закрыть, потому что главный воспроизводимый тест архива — применение patch к baseline и последующее сравнение восстановленных файлов с `repo-file-hashes.json` — не может быть выполнен в рамках контракта “используйте динамические сведения только из файлов архива”. Это также ломает независимую проверку связи evidence с реальным восстановленным состоянием репозитория.
  - Fix: приложить сам baseline в самодостаточной форме, достаточной для clean restore без внешнего доступа: например, минимальный baseline snapshot/bundle для всех путей, которые нужны `git apply --check` и `git apply`, либо иной архивный restore payload, из которого однозначно восстанавливается ревизия `5f702646abdaba6b21504ad49a9f9fd849b94fb4`.
  - Verification: на новой итерации в отдельной чистой копии baseline выполнить `git apply --check <task-id>.patch`, затем `git apply <task-id>.patch`, затем вычислить SHA-256 всех восстановленных repo-owned файлов и сверить их с `repo-file-hashes.json`; результаты этой проверки должны быть воспроизводимы только из содержимого архива.

- B2
  - File/symbol: `metadata/audit-package.input.json` поле `previousVerdictChain`; previous verdict files `docs/verdicts/release-management/t-0214-audit-r01.md`, `...r02.md`, `...r03.md`; `T-0214.patch`; `repo-file-hashes.json`
  - Criterion: обязательная проверка `verbatim preservation` требует независимо подтвердить, что содержимое каждого existing previous verdict file сохранено дословно как историческое внешнее свидетельство, без переписывания формулировок, удаления цитат, нормализации evidence или замены строк.
  - Evidence: `metadata.previousVerdictChain` перечисляет три historical external audit file path. В текущем patch все три файла идут как новые repo-owned additions (`new file mode 100644`), а `repo-file-hashes.json` и `AUDIT-MANIFEST.md` содержат только их текущие hashes внутри этого же архива. В архиве нет ни предыдущих audit packages, ни detached historical checksums этих файлов, ни import/evidence-артефактов, которые криптографически связывали бы нынешние `docs/verdicts/...r01.md`, `...r02.md`, `...r03.md` с реально существовавшими earlier external reports. Я смог проверить только внутреннюю согласованность: пути из `previousVerdictChain` присутствуют в manifest, patch и `repo-file-hashes.json`; reconstructed bytes для этих new files из patch совпадают с их текущими SHA-256 в `repo-file-hashes.json`. Но это не доказывает историческую дословность: архив показывает лишь текущую копию, а не независимый источник её происхождения.
  - Impact: требование `previous verdict files` + `verbatim preservation` не выполнено на уровне независимого доказательства. В текущем виде архив допускает ситуацию, когда автор встраивает переписанные historical reports в текущий patch, а аудитор не может это опровергнуть, оставаясь внутри архива. Тогда и `previous blockers closure` нельзя считать полностью независимым, потому что список прежних blocker-ов читается из текущих копий тех же файлов.
  - Fix: приложить внутри архива независимую историческую привязку для каждого previous verdict file: например, сами прошлые audit packages как archive-only evidence, либо отдельный immutable import с их original checksums/contents, либо иной проверяемый артефакт, позволяющий сравнить текущие repo-owned copies с оригиналами byte-for-byte.
  - Verification: на новой итерации сравнить текущие `docs/verdicts/...` с приложенными историческими источниками и показать byte-identical совпадение; только после этого повторно проверить `metadata.blockerClosureList` против реально подтверждённых прежних blocker-ов.

EVIDENCE_REVIEW:
- Проверены top-level файлы архива: `AUDIT-MANIFEST.md`, `AUDIT-REQUEST.md`, `SHA256SUMS.txt`, `T-0214.patch`, `metadata/audit-package.input.json`, `repo-file-hashes.json`.
- Проверка checksum-слоя прошла успешно: `sha256sum -c SHA256SUMS.txt` подтвердил все покрытые файлы архива; manifest корректно указывает, что `SHA256SUMS.txt` покрывает все archive files кроме самого себя.
- Проверена безопасная структура ZIP: duplicate paths, absolute paths, `..` segments, directory entries и nested archives не обнаружены.
- Проверена согласованность restore-модели на уровне набора путей: `AUDIT-MANIFEST.md` diff section, `T-0214.patch` diff headers и `repo-file-hashes.json` совпадают по одному и тому же набору из 17 repo-owned файлов; `deletedRepoFiles` пуст.
- Для added files удалось дополнительно проверить byte-level internal consistency: reconstructed contents из patch для `eng/Electron2D.Build/RepositoryPolicyVerifiers.cs` и трёх historical audit files из `previousVerdictChain` совпадают по SHA-256 с `repo-file-hashes.json`.
- Проверены все evidence checks из `metadata.audit-package.input.json`; их directories присутствуют, `metadata.json` в каждом check совпадает с ожидаемыми `name`, `fileName`, `arguments`, `cwd`, `expectedExitCode`, `timeoutSeconds`.
- Проверены raw evidence outputs и exit codes для:
  - `focused-repository-build-tests`
  - `git-diff-check`
  - `source-license-headers`
  - `tasks`
  - `update-api-manifest-check`
  - `update-docs-check`
  - `update-wiki-check`
  - `update-wiki-output-check`
  - `verify-api-compatibility`
  - `verify-docs`
  - `verify-licenses`
  - `verify-manifests`
  - `verify-project-template`
  - `verify-release-metadata`
- TRX `evidence/T-0214-r04/checks/focused-repository-build-tests/trx/test-result-001.trx` просмотрен: 40 executed / 40 passed. В нём есть именованные tests, прямо относящиеся к closure прежних blocker-ов, включая:
  - `UpdateWikiCheckWithoutOutputVerifiesGeneratedWikiOutput`
  - `UpdateWikiRoundTripWritesAndChecksDeterministicManifestContent`
  - `UpdateWikiCheckRejectsStaleGeneratedContent(...)`
  - `VerifyProjectTemplateRejectsInvalidJsonRootAndMissingRequiredFields(...)`
  - `VerifyManifestsRejectsNonObjectProjectTemplateJsonRoot`
  - `DomainDocumentsDoNotDeclareUnsupportedCSharpVerifyCommands`
  - `T0214DomainDocumentsDoNotDeclarePowerShellCompatibilityLayer`
  - `T0214DomainDocumentsDoNotDeclareLegacyPowerShellScriptsAsCurrentSurface`
- Проверена цепочка `metadata.previousVerdictChain`: все три пути присутствуют в manifest, patch и `repo-file-hashes.json`; из этих файлов выписаны все прежние blocker-ы:
  - r01: B1, B2, B3, B4
  - r02: B1, B2
  - r03: B1
- Проверен `metadata.blockerClosureList`: формально он содержит отдельное closure для каждого из 7 прежних blocker-ов. По содержанию текущих patch/tests/evidence видно, что автор целенаправленно адресовал эти места:
  - no-output `update wiki --check` теперь имеет temp-output semantics и report `Generated pages`
  - whole-page stale checks есть и в коде, и в tests
  - non-object JSON roots теперь дают structured diagnostics
  - domain docs переведены на C# surface и guard tests сканируют фиксированный список T-0214 domain documents
  - `TASKS.md` вынесен в `evidence/.../archive-only/`, а не в repo-owned patch
- Проведён `secret scanning`: private keys, obvious tokens, `.env`, `.secret`, AWS/GitHub/Slack token patterns и absolute machine paths в archive payload не найдены. Обнаруживаются только публичные авторские e-mail адреса в license headers/patch, что не выглядит как секрет.
- Проведён `scope scanning`: repo-owned diff ограничен release-management/documentation/build/tests scope и historical audit files; `TASKS.md` присутствует только как archive-only evidence, что действительно устраняет r03 scope-problem с попаданием `T-0092` в repo-owned patch.

RISKS_AND_NOTES:
- В evidence metadata у всех checks `duration-ms.txt` равен `0`, хотя по stdout/TRX видно реальное ненулевое время выполнения. Это не стало отдельным blocker-ом, потому что команды, stdout/stderr, exit codes и TRX для ключевых проверок присутствуют и согласованы, но как слой telemetry это снижает доверие к точности ancillary metadata.
- `git-diff-check` прошёл с кодом 0, но `stderr.txt` содержит предупреждения Git о будущем LF/CRLF conversion. Из-за отсутствия baseline внутри архива нельзя независимо проверить, влияют ли line-ending policies исходной ревизии на byte-level restore в разных средах; это усиливает B1, хотя само по себе предупреждение blocker-ом не считалось.
- По существу изменений package выглядит осмысленным, а не пусто-формальным: код, tests и docs адресуют именно те failure modes, которые поднимались в r01-r03. Отказ основан не на том, что изменений “нет”, а на том, что package не доводит доказательство до самодостаточного и независимого уровня в двух обязательных местах: baseline restore и historical verbatim proof.

CLOSURE_DECISION:
- Задача остаётся открытой до исправлений.
- Текущий архив можно считать внутренне согласованным и содержательно полезным, но не самодостаточным по контракту внешнего аудита.
- Для закрытия acceptance нужен новый audit package, в котором:
  - baseline source revision восстанавливается полностью из самого архива и позволяет реально выполнить `git apply --check` / `git apply` / hash-compare;
  - previous verdict files снабжены независимым историческим источником, позволяющим доказать their verbatim preservation byte-for-byte.
- Пока эти два требования не выполнены, внешнее принятие будет опираться на доверие к автору архива, а не на воспроизводимое доказательство из архивных файлов.
