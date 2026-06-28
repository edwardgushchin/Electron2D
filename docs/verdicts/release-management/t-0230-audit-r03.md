# T-0230 audit r03

- Задача: T-0230
- Домен: release-management
- Актуально на: 2026-06-28
- Область проверки: внешний acceptance audit r03 для закрытия r01 blocker-ов B1-B3, operator workflow evidence, baseline availability evidence и команды `audit package message --zip <path>`.
- Статус вывода: VERDICT: NEEDS_FIXES
- Предыдущий аудит: `docs/verdicts/release-management/t-0230-audit-r01.md`
- Следующий аудит: r04 после закрытия blocker-ов B1-B2

VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:

Проверены структура архива, SHA256SUMS.txt, AUDIT-MANIFEST.md, metadata/audit-package.input.json, repo-file-hashes.json, T-0230.patch и все raw-evidence артефакты в evidence/.
Формальная целостность архива в целом хорошая: инвентарь совпадает с фактическим содержимым ZIP, контрольные суммы сходятся, AUDIT-MANIFEST.md согласован с repo-file-hashes.json, previous verdict docs/verdicts/release-management/t-0230-audit-r01.md включён в restore model и сохраняется дословно.
Изменение нельзя принять, потому что заявленное baseline availability evidence и operator workflow evidence не доказывают exact final ZIP, переданный на внешний аудит. Архив содержит evidence-файлы, но по коду они создаются до финальной пересборки ZIP и собираются не как raw выполнение задокументированных операторских CLI-команд, а как in-process shortcut через внутренние методы.

BLOCKERS:

B1

File/symbol: eng/Electron2D.Build/AuditPackageCommand.cs; логика CreatePackageAsync, RefreshManifestAndChecksums, WriteDeterministicZip, CreateOperatorWorkflowEvidenceAsync; артефакты evidence/T-0230-r03/checks/audit-package-verify/* и evidence/T-0230-r03/checks/audit-package-message/*.
Criterion: baseline availability evidence, audit package verify evidence и audit package message evidence должны доказывать текущий финальный архив, а не промежуточную версию до последней пересборки.
Evidence: в CreatePackageAsync сначала собирается архив без operator evidence, затем вызывается WriteDeterministicZip(zipPath, archiveFiles). После этого выполняется CreateOperatorWorkflowEvidenceAsync(...), то есть verify/message относятся к уже записанному промежуточному ZIP. Затем этот ZIP удаляется, в archiveFiles добавляются operatorEvidenceFiles, заново вызывается RefreshManifestAndChecksums(...), и только потом пишется финальный ZIP. Значит, audit-package-verify и audit-package-message не были выполнены на тех байтах, которые реально лежат в итоговом T-0230-audit-r03.zip. Это подтверждается и содержательно: финальный AUDIT-MANIFEST.md уже перечисляет operator-evidence пути, которых не было в ZIP на момент запуска CreateOperatorWorkflowEvidenceAsync.
Impact: архив не содержит доказательства успешного audit package verify и audit package message именно для exact final artifact, который отправляется внешнему аудитору. Главный контракт пакета — доказать финальный артефакт через восстановление и raw evidence — остаётся незакрытым.
Fix: переработать модель сборки так, чтобы operator evidence относился к неизменяемому финальному payload. Практически это требует либо вынести operator evidence в отдельный слой/обёртку, либо перестроить формат так, чтобы verify/message выполнялись над уже окончательным артефактом, байты которого после этого больше не меняются.
Verification: новый архив должен сопровождаться raw evidence, достоверно полученным уже после финальной стабилизации содержимого. Тесты должны падать, если verify/message запускаются до финальной записи ZIP или если после их запуска меняются AUDIT-MANIFEST.md, SHA256SUMS.txt или набор archive entries.

B2

File/symbol: eng/Electron2D.Build/AuditPackageCommand.cs; RunAuditVerifyForEvidenceAsync, RunAuditMessageForEvidence, WriteCommandEvidenceFiles, CreateOperatorWorkflowEvidenceAsync; соответствующие command.txt и metadata.json в evidence/T-0230-r03/checks/audit-package-verify/ и .../audit-package-message/.
Criterion: operator workflow evidence должно быть raw и переносимым доказательством именно задокументированных операторских команд dotnet run --project eng/Electron2D.Build -- audit package verify --zip <path> --baseline <sha> --repo <clean-repo-path> и dotnet run --project eng/Electron2D.Build -- audit package message --zip <path>, а для verify — ещё и доказательством обязательного внешнего clean baseline input.
Evidence: RunAuditVerifyForEvidenceAsync не запускает CLI-команду из command.txt, а создаёт new AuditPackageCommand(...) и вызывает VerifyPackageAsync(...) напрямую. RunAuditMessageForEvidence не запускает dotnet run ... audit package message, а вызывает CreatePackageMessage(...) напрямую. При этом WriteCommandEvidenceFiles(...) записывает command.txt, будто были выполнены реальные dotnet run команды. Дополнительно verify evidence не использует внешний операторский --repo <clean-repo-path>, а делает собственный CreateCleanCloneAsync(repoRoot, config.Baseline, ...) внутри сборщика. То есть evidence оформлен как raw evidence операторского пути, но фактически это синтетическая запись внутреннего вызова методов и внутреннего clean clone shortcut.
Impact: архив формально выглядит полным, но не доказывает реальное исполнение задокументированного operator workflow. Если CLI wiring, process startup, текущая рабочая директория, кодировка stdout/stderr, разрешение путей, или реальный внешний --repo путь расходятся с прямым вызовом внутренних методов, текущий пакет этого не обнаружит.
Fix: получать operator workflow evidence только из реальных subprocess-запусков задокументированных команд, а не из прямых вызовов методов. Для verify должен использоваться отдельно подготовленный clean repo, переданный именно через --repo, а raw файлы (command.txt, stdout.txt, stderr.txt, exit-code.txt, duration-ms.txt, metadata.json) должны импортироваться as-is без синтетического переписывания источника выполнения.
Verification: новый архив должен содержать raw evidence, полученное от реальных CLI subprocess run-ов. Тесты должны явно проверять, что operator evidence нельзя получить через in-process shortcut и что verify path использует внешний --repo, а не внутренний CreateCleanCloneAsync.

EVIDENCE_REVIEW:

Проверены корневые файлы архива: AUDIT-MANIFEST.md, AUDIT-REQUEST.md, SHA256SUMS.txt, T-0230.patch, metadata/audit-package.input.json, repo-file-hashes.json.
SHA256SUMS.txt успешно подтверждает все файлы архива, кроме самого SHA256SUMS.txt, что соответствует заявленному restore model.
Инвентарь Archive Inventory в AUDIT-MANIFEST.md полностью совпадает с фактическим содержимым ZIP.
Раздел Repository File Inventory в AUDIT-MANIFEST.md совпадает с repo-file-hashes.json.
metadata.previousVerdictChain содержит docs/verdicts/release-management/t-0230-audit-r01.md. Этот файл найден в patch, repo-file-hashes.json и AUDIT-MANIFEST.md.
Verbatim preservation previous verdict проверен: содержимое docs/verdicts/release-management/t-0230-audit-r01.md, извлечённое из patch, даёт SHA-256 681e9eeabd5b8589413d2d7c5af1b2111865d88813f143591f41eb2733525da6, что совпадает с repo-file-hashes.json и AUDIT-MANIFEST.md.
metadata.blockerClosureList содержит явные closure entries для предыдущих B1-B3. Список закрытий прочитан и сопоставлен с предыдущим verdict, но фактическое закрытие не подтверждено из-за текущих blocker-ов B1-B2.
Проверены все raw evidence каталоги в evidence/T-0230-r03/checks/: audit-package-message, audit-package-verify, git-diff-check, source-license-headers, t-0230-focused-tests, update-docs-check, verify-docs, verify-tasks.
У всех evidence check-ов metadata.json согласован с exit-code.txt, duration-ms.txt, stdout.txt, stderr.txt и, где есть, с trx файлами. Ненулевые durationMs действительно присутствуют.
t-0230-focused-tests содержит полный stdout, ненулевую длительность и trx/test-result-001.trx. TRX подтверждает 12 passed tests, включая:
AuditPackageIncludesOperatorWorkflowEvidenceForVerifyAndMessage
AuditPackageWritesCheckMetadataThatMatchesRawEvidenceFiles
AuditPackageGeneratesAndVerifiesMinimalFixtureRepository
AuditPackageDocumentationRequiresMessageDeepResearchAndAttachedPackage
AuditPackageMessageWritesRequestBodyWithoutGeneratedWrapper
негативные кейсы для missing zip, metadata, request, required markers, invalid taskId/iteration и filename mismatch.
audit-package-message/stdout.txt совпадает с AUDIT-REQUEST.md после удаления первого Markdown H1, то есть заявленная форма текста сообщения подтверждена.
Выполнен secret/path scan по архиву, patch, metadata и evidence: явных реальных секретов, приватных ключей, токенов и абсолютных локальных путей машины не найдено.
Обязательный независимый restore step на отдельной чистой baseline-копии в этой среде не был воспроизведён, потому что внешний clean repo не был предоставлен как вход аудита. По текущему контракту это допустимо только если archive-contained baseline availability evidence надёжно доказывает финальный ZIP; именно это и не выполнено по B1-B2.

RISKS_AND_NOTES:

Формальная самосогласованность архива высокая: inventory, checksums, previous verdict chain, closure list и raw evidence metadata оформлены аккуратно.
Документация и focused tests хорошо покрывают форму запроса, обязательность режима Глубокое исследование, наличие operator-evidence файлов, ненулевой durationMs и базовые failure cases audit package message.
Основной незакрытый риск не в форме, а в доказательности: текущие тесты не ловят два ключевых дефекта этой итерации — запуск verify/message до финальной пересборки ZIP и подмену raw operator workflow evidence прямыми вызовами внутренних методов.
Вне-scope замечание, но важное для архитектуры: текущая self-referential модель “встроить verify/message evidence внутрь того же ZIP, который они должны доказать” требует отдельного контрактного решения. Пока это не решено, архив легко сделать формально полным, но трудно сделать строго доказуемым.

CLOSURE_DECISION:

Задача остаётся открытой до исправлений, потому что финальный T-0230-audit-r03.zip не сопровождается достоверным доказательством exact-final audit package verify и audit package message, а текущий operator workflow evidence является синтетическим и предфинальным.
Для закрытия задачи нужно:
переработать формат operator evidence так, чтобы он относился к неизменяемому финальному артефакту;
собирать audit package verify и audit package message evidence только из реальных CLI subprocess run-ов;
доказать использование отдельной clean baseline копии именно через реальный --repo, а не через внутренний shortcut;
добавить тесты, которые блокируют предфинальную генерацию evidence и in-process подмену операторских команд.
