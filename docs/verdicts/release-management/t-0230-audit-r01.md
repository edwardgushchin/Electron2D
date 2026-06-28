# T-0230 audit r01

- Задача: T-0230
- Домен: release-management
- Актуально на: 2026-06-28
- Область проверки: внешний acceptance audit r01 для команды `audit package message --zip <path>` и отправки текста `AUDIT-REQUEST.md` через «Глубокое исследование».
- Статус вывода: VERDICT: NEEDS_FIXES
- Предыдущий аудит: нет
- Следующий аудит: r02 после закрытия blocker-ов B1-B3

VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:

Проверена структура архива, SHA256SUMS.txt, AUDIT-MANIFEST.md, metadata/audit-package.input.json, repo-file-hashes.json, T-0230.patch и все артефакты в evidence/.
Внутренняя формальная целостность архива в основном соблюдена: инвентарь сходится, контрольные суммы сходятся, набор путей в manifest / patch / repo-file-hashes.json согласован, metadata.previousVerdictChain и metadata.blockerClosureList пусты, явных секретов и абсолютных локальных путей в самом архиве не найдено.
Изменение нельзя принять, потому что архив не даёт воспроизводимого доказательства по обязательной цепочке восстановления: обязательный внешний baseline для git apply --check и сравнения с repo-file-hashes.json фактически недоступен, а evidence для ключевого операторского пути (audit package verify и audit package message именно на этом ZIP) отсутствует или слишком слабое, чтобы заменить независимую проверку.

BLOCKERS:

B1

File/symbol: AUDIT-MANIFEST.md (baseline: f7ecdc82fafb6f8cb446dc7f3e43e1a7ddbfb563), metadata/audit-package.input.json (baseline с тем же SHA), T-0230.patch -> новый файл dev-diary/2026/06 Июнь/28-06-2026.md, строка с записью создан commit 'f7ecdc82 ...' без push.
Criterion: обязательный restore scanning с external baseline REQUIRED input; отсутствие baseline payload само по себе допустимо только если отдельная чистая копия исходной ревизии реально доступна внешнему проверяющему.
Evidence: архив требует внешний clean repo на baseline f7ecdc82..., но в восстановленной модели через patch одновременно зафиксировано, что этот baseline commit был создан «без push». То есть ZIP не содержит baseline payload, а единственное явно указанное исходное состояние задокументировано как непубликованное.
Impact: внешний аудитор не может независимо получить обязательную чистую копию исходной ревизии и, следовательно, не может воспроизводимо выполнить обязательные шаги git apply --check, git apply и сравнение восстановленных файлов с repo-file-hashes.json. Это ломает главный контракт архива: доказать изменение через восстановление, а не только через наличие patch и manifest.
Fix: либо дать baseline, который реально доступен внешнему проверяющему, либо включить разрешённый контрактом дополнительный слой доставки baseline (например, явный baseline payload / bundle / tar) и обновить документацию пакета под этот контракт, либо сменить baseline на публично доступную ревизию и пересобрать ZIP.
Verification: на отдельной чистой копии доступного baseline выполнить:
git rev-parse HEAD
git apply --check T-0230.patch
git apply T-0230.patch
сравнение всех путей/хэшей из repo-file-hashes.json с восстановленным деревом.

B2

File/symbol: AUDIT-MANIFEST.md разделы Checks и Evidence Links; каталог evidence/T-0230-r01/checks/; T-0230.patch в TASKS.md и docs/release-management/audit-package.md, где зафиксирован обязательный путь audit package verify + audit package message --zip <path> перед внешней отправкой.
Criterion: evidence scanning и связь доказательств с проверяемым поведением; архив должен не только выглядеть формально полным, но и доказывать заявленное изменение и обязательный операторский путь.
Evidence: в evidence/ есть только audit-package-message-tests, verify-docs, update-docs-check, verify-tasks, source-license-headers, git-diff-check. Нет ни одного raw evidence-артефакта для:
dotnet run --project eng/Electron2D.Build -- audit package verify --zip T-0230-audit-r01.zip --baseline ... --repo <clean-repo-path>
dotnet run --project eng/Electron2D.Build -- audit package message --zip T-0230-audit-r01.zip При этом сам task/documentation через patch прямо требуют именно verified package и вставку текста из audit package message --zip <path> в отправляемое внешнее сообщение.
Impact: архив не доказывает, что именно этот ZIP был проверен штатным audit package verify и что текст для внешнего аудитора действительно был получен штатной командой audit package message, а не собран вручную. При недоступном baseline это отсутствие уже нельзя компенсировать независимым воспроизведением.
Fix: добавить в evidence/ сырые артефакты запуска обеих команд именно на финальном T-0230-audit-r01.zip: command.txt, stdout.txt, stderr.txt, exit-code.txt, metadata.json, и включить их в manifest.
Verification: после пересборки архива manifest должен перечислять raw evidence для audit package verify и audit package message; stdout.txt команды message должен содержать финальный текст, а verify — успешное завершение на чистой baseline-копии.

B3

File/symbol: evidence/T-0230-r01/checks/audit-package-message-tests/command.txt, stdout.txt, metadata.json; также metadata.json во всех шести checks.
Criterion: raw evidence должен соответствовать командам и результатам и быть достаточно сильным, чтобы связать доказательства с изменёнными файлами и реальным выполнением check-ов.
Evidence:
audit-package-message-tests/command.txt выполняет dotnet test ... --filter FullyQualifiedName~AuditPackageMessage *> $null; ... Write-Output 'AuditPackageMessage focused tests passed.', то есть полностью глушит реальный вывод тестового раннера.
audit-package-message-tests/stdout.txt содержит только строку AuditPackageMessage focused tests passed..
audit-package-message-tests/metadata.json не ссылается ни на один TRX ("trxFiles": []).
Во всех metadata.json поле durationMs равно 0, включая dotnet test, verify docs, update docs --check и PowerShell-скрипты, что делает метаданные о выполнении фактически синтетическими и неинформативными.
Impact: пакет не хранит сырые результаты ключевого тестового прогона и не даёт проверить, какие именно тесты реально были исполнены, сколько их было и каким был их настоящий runner output. При этом невозможность восстановить baseline уже убирает независимую компенсацию через повторный запуск. В итоге evidence для core-change слишком слабое.
Fix: перестать глушить test runner output в archive evidence либо сохранять TRX/полный stdout/stderr focused-run; записывать реальные duration values; при необходимости импортировать TRX в архив и перечислять его в trxFiles.
Verification: в новом архиве audit-package-message-tests должен содержать либо полный dotnet test stdout/stderr, либо TRX с перечислением исполненных тестов, а durationMs должен быть ненулевым и согласованным с реальным запуском.

EVIDENCE_REVIEW:

Проверены корневые файлы архива: AUDIT-MANIFEST.md, AUDIT-REQUEST.md, SHA256SUMS.txt, T-0230.patch, metadata/audit-package.input.json, repo-file-hashes.json.
Проверена структура ZIP и инвентарь manifest: набор archive paths согласован; SHA256SUMS.txt корректно покрывает содержимое архива, кроме самого SHA256SUMS.txt.
Проверено согласование restore model:
AUDIT-MANIFEST.md перечисляет ровно те repo-owned файлы, которые присутствуют в patch и repo-file-hashes.json;
patch меняет 7 repo-owned путей, и те же 7 путей перечислены в restore manifest.
Проверена mandatory previous-verdict chain логика:
metadata.previousVerdictChain = [];
metadata.blockerClosureList = [];
дополнительных previous verdict files для verbatim preservation / previous blockers closure в этом пакете нет.
Проверены raw evidence-артефакты по всем заявленным checks:
audit-package-message-tests
verify-docs
update-docs-check
verify-tasks
source-license-headers
git-diff-check
Прочитаны изменённые файлы через patch:
eng/Electron2D.Build/AuditPackageCommand.cs
eng/Electron2D.Build/Program.cs
tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs
docs/release-management/audit-package.md
data/documentation/electron2d-local-docs-index.json
TASKS.md
dev-diary/2026/06 Июнь/28-06-2026.md
Выполнен secret/path scan по архиву, patch, metadata и evidence: явных реальных секретов, приватных ключей и локальных абсолютных путей машины в самом архиве не найдено.
Mandatory restore step до конца не доведён по причине B1: baseline-ревизия задокументирована как unpushed, поэтому независимая чистая копия исходного commit не может быть воспроизводимо подготовлена из материалов пакета.

RISKS_AND_NOTES:

TASKS.md и dev-diary внутри restored model не согласованы по статусу T-0230: task section остаётся in progress с незакрытым подпунктом про сборку verified package и отправку внешнего аудита, тогда как diary пишет про перевод в ready for acceptance. Это не основной blocker само по себе, но усиливает вывод, что пакет не даёт чистого доказательства завершённого operator path.
Формальная самосогласованность архива хорошая: inventory, checksums, manifest metadata и restore manifest между собой не конфликтуют.
Previous verdict preservation / closure в этой итерации не применялись, потому что цепочка предыдущих verdict-ов пуста.
Явных вне-scope изменений за пределами заявленных repo-owned путей patch не показывает.

CLOSURE_DECISION:

Задача остаётся открытой до исправлений, потому что текущий audit package не проходит главный критерий воспроизводимого внешнего восстановления: обязательный внешний baseline фактически недоступен, а evidence не доказывает прохождение обязательного operator path на этом конкретном ZIP.
Принятие возможно только после того, как:
baseline для external baseline REQUIRED input станет реально доступным внешнему проверяющему;
этот exact ZIP или его пересобранная замена будет сопровождаться raw evidence запуска audit package verify и audit package message;
focused test evidence перестанет быть сведённым к подавленному выводу и синтетическим durationMs: 0.
