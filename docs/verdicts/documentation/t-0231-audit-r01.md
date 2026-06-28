# T-0231 audit r01

- Задача: T-0231
- Домен: documentation
- Актуально на: 2026-06-28T16:04:16+03:00
- Область проверки: внешний аудит приложенного архива `T-0231-audit-r01.zip` и sidecar-архива `T-0231-audit-r01.operator-workflow.zip`; проверялись manifest, metadata, checksum-модель, patch restore, evidence, заявленная область `T-0231` и связь с `T-0232`.
- Статус вывода: VERDICT: NEEDS_FIXES
- Предыдущий аудит: нет
- Следующий аудит: T-0231 audit r02 после исправления blocker-ов r01.

## Ответ аудитора

```text
VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:
- Проверены структура основного ZIP и sidecar ZIP, инвентарь `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `repo-file-hashes.json`, `SHA256SUMS.txt`, patch и сырые evidence в обоих архивах. Контрольные суммы внутри обоих ZIP согласованы с их фактическим содержимым; sidecar корректно привязан к текущему основному ZIP по SHA-256, hash `AUDIT-MANIFEST.md`, hash `SHA256SUMS.txt` и hash списка записей.
- Проверены `audit package message evidence` и `audit package verify evidence` из sidecar ZIP. Команда `audit package verify` записана как реальный CLI-вызов с `--zip`, `--baseline` и placeholder `--repo <clean-repo-path>`; команда `audit package message` выводит текст аудиторского запроса без ручного пересказа.
- Выполнена независимая частичная реконструкция восстановленной модели: для подмножества repo-owned файлов удалось воспроизвести patched-версии и подтвердить их SHA-256 по `repo-file-hashes.json` после применения соответствующих hunks из `T-0231.patch`. Это подтвердило внутреннюю согласованность части restore-модели, но не снимает blocker-ы ниже.
- Изменение нельзя принять как `T-0231`, потому что архив фактически содержит смешанный changelist как минимум по двум задачам: `T-0231` и `T-0232`, при этом package metadata и audit contract продолжают объявлять пакет как единичную задачу `T-0231` в домене `documentation`.

BLOCKERS:
- B1
  - File/symbol: `evidence/T-0231-r01/archive-only/TASKS.md:2141-2179`, `evidence/T-0231-r01/archive-only/TASKS.md:2191-2228`, `AUDIT-MANIFEST.md:13-36`, `evidence/T-0231-r01/archive-only/RELEASE-NOTES.md:96-106`.
  - Criterion: `scope scanning`; архив должен доказывать заявленное изменение текущей задачи, а не смешивать его с другой задачей без явного объявления комбинированной области.
  - Evidence: `TASKS.md:2141-2179` определяет `T-0231` как задачу про разделение локального индекса документации на NDJSON-шарды и добавление SQLite FTS-кэша. Отдельно `TASKS.md:2191-2228` определяет `T-0232` как задачу про общую LF-политику репозитория и стабилизацию `audit package verify`. При этом `AUDIT-MANIFEST.md:13-36` включает в один diff как документы/код `T-0231` (`data/documentation/...`, `docs/documentation/local-documentation-pipeline.md`, `src/Electron2D.Cli/Program.cs`, `eng/Electron2D.Build/LocalDocumentationVerifier.cs`), так и явные файлы `T-0232` (`.gitattributes`, `.gitignore`, `docs/repository-policy/line-endings.md`, `eng/Electron2D.Build/AuditPackageCommand.cs`, `eng/Electron2D.Build/LineEndingVerifier.cs`, line-ending focused tests). `RELEASE-NOTES.md:96-106` тоже разделяет `T-0231` и `T-0232` как разные локальные обновления, но пакет всё равно отгружён как `T-0231`.
  - Impact: внешний verdict по этому ZIP фактически примет не только заявленную документационную задачу `T-0231`, но и отдельную инфраструктурную задачу `T-0232`. Это нарушает границу области задачи и делает verdict неоднозначным для истории репозитория и последующего closure.
  - Fix: либо пересобрать `T-0231` package только из файлов, принадлежащих `T-0231`, либо явным образом оформить комбинированный пакет с корректным task/contract naming, manifest, metadata, request и evidence, чтобы аудит принимал именно тот набор изменений, который реально лежит в diff.
  - Verification: новый audit package должен либо иметь diff, ограниченный acceptance criteria `T-0231`, либо явно объявлять multi-task scope и содержать согласованное описание этого scope во всех основных артефактах (`AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `AUDIT-REQUEST.md`, release/task evidence).

- B2
  - File/symbol: `metadata/audit-package.input.json:2-6`, `metadata/audit-package.input.json:310-311`, `evidence/T-0231-r01/archive-only/TASKS.md:2146`, `evidence/T-0231-r01/archive-only/TASKS.md:2189`, `evidence/T-0231-r01/archive-only/TASKS.md:2244`.
  - Criterion: архив не должен требовать скрытых допущений и ручных догадок; closure и зависимость, влияющие на решение о приёмке, должны быть явно и воспроизводимо объявлены, а не восстанавливаться косвенно из архивных заметок.
  - Evidence: `metadata/audit-package.input.json:2-6` продолжает объявлять пакет как `taskId = "T-0231"` и `domain = "documentation"`, а `metadata/audit-package.input.json:310-311` оставляет `previousVerdictChain` и `blockerClosureList` пустыми. Одновременно `TASKS.md:2146` прямо указывает зависимость `T-0231` от `T-0232`; `TASKS.md:2189` говорит, что до исправления `T-0232` новый внешний аудит `T-0231` не отправляется; `TASKS.md:2244` фиксирует, что только после повторной независимой проверки восстановления `T-0231` задача `T-0232` переводится в `ready for acceptance`.
  - Impact: пакет в его текущем machine-readable виде не объясняет, что в нём одновременно закрывается зависимость `T-0232`, и не даёт явной closure-модели для этой части diff. Аудитор должен догадаться об этом из `TASKS.md`, хотя базовые package metadata продолжают утверждать, что это просто `T-0231`. Такой package нельзя считать самодостаточным доказательством корректного closure-решения.
  - Fix: либо выпустить отдельный audit package для `T-0232` и оставить в `T-0231` только собственную область, либо явно зафиксировать bundled dependency closure в package contract: обновить manifest/input/request так, чтобы они недвусмысленно описывали, что именно принимается и почему second-task changes входят в текущий payload.
  - Verification: после исправления в архиве должна быть либо отдельная accepted dependency package-ссылка/цепочка для `T-0232`, либо однозначное явное описание combined closure в `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json` и `AUDIT-REQUEST.md`, без необходимости читать локальные task notes для понимания реального scope.

EVIDENCE_REVIEW:
- Проверены корневые артефакты основного ZIP: `AUDIT-MANIFEST.md`, `AUDIT-REQUEST.md`, `SHA256SUMS.txt`, `T-0231.patch`, `metadata/audit-package.input.json`, `repo-file-hashes.json`.
- Проверена целостность основного ZIP: все записи из `AUDIT-MANIFEST.md` присутствуют в архиве; `SHA256SUMS.txt` покрывает все файлы архива, кроме самого `SHA256SUMS.txt`, и их фактические SHA-256 совпадают.
- Проверен sidecar ZIP `T-0231-audit-r01.operator-workflow.zip`: `OPERATOR-WORKFLOW.md`, `payload/sha256.txt`, `payload/metadata.json`, `payload/archive-entries.txt`, `payload/AUDIT-MANIFEST.sha256`, `payload/SHA256SUMS.sha256`, evidence для `audit-package-message` и `audit-package-verify`.
- Проверена связь sidecar с payload: SHA-256 основного ZIP, hash `AUDIT-MANIFEST.md`, hash `SHA256SUMS.txt` и hash списка записей совпадают между sidecar и фактическим текущим main ZIP.
- Проверены операторские команды:
  - `audit package verify --zip .temp/audit/T-0231-audit-r01.zip --baseline 57f336e0772ca0ee381c5e47ae817bbcb34470c0 --repo <clean-repo-path>`
  - `audit package message --zip .temp/audit/T-0231-audit-r01.zip`
- Проверены сырые main evidence для `verify docs`, `update docs --check`, `cli-docs-search`, `cli-docs-example`, `verify line-endings`, focused docs/CLI tests, focused line-ending tests, focused shard tests, vulnerability checks, `git diff --check`, `verify-tasks`, `source-license`.
- Проверена цепочка previous verdict-ов: `metadata.previousVerdictChain` пуст, `metadata.blockerClosureList` пуст; следовательно, обязательные проверки `verbatim preservation` и `previous blockers closure` в этом пакете формально неприменимы.
- Выполнен secret/path scan по архивам и evidence: реальных секретов, приватных ключей и несанацированных абсолютных локальных путей в operator workflow evidence не обнаружено; placeholder `<clean-repo-path>` использован корректно.

RISKS_AND_NOTES:
- По содержанию архивов видно, что sidecar evidence и основная checksum-модель собраны аккуратно; проблема не в формальной полноте ZIP-структуры, а в том, что payload принят к аудиту под неверной и неполной scope-декларацией.
- В этом sandbox не была завершена полная независимая реконструкция всех 24 repo-owned файлов через полноценную внешнюю clean baseline repo, потому что обязательный внешний baseline не был отдельно предоставлен как локальный репозиторий в рамках разговора. Это не является blocker-ом пакета само по себе, поскольку контракт допускает `external baseline REQUIRED input`, но и не отменяет двух blocker-ов выше, которые уже доказуемы по самим архивам.
- Остаточный note: evidence и docs убедительно показывают, что T-0231-функциональность с NDJSON/SQLite и T-0232-исправления line endings действительно реализованы и тестировались; именно поэтому пакет особенно важно разнести или явно переобъявить, а не принимать «как T-0231».

CLOSURE_DECISION:
- Задача не может быть закрыта в текущем виде, потому что архив не доказывает «только заявленное изменение `T-0231`». Он приносит в verdict дополнительную задачу `T-0232`, но не объявляет это машинно и контрактно на уровне основного package metadata.
- До исправления package scope и явного closure-контракта принятие этого ZIP создаст двусмысленную историю: один verdict формально закроет сразу две разные задачи с разными acceptance criteria и разными доменными рисками.
- Закрытие возможно только после пересборки архива с корректной областью задачи либо после явного, согласованного и самодостаточного объявления комбинированного scope во всех основных артефактах пакета.
```
