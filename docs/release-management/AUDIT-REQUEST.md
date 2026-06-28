# Запрос внешнего аудита Electron2D

Вы проводите глубокий внешний аудит audit package для Electron2D. Ваша задача - независимо проверить, что основной ZIP-архив, сопровождающий ZIP с операторскими доказательствами и обязательная внешняя чистая копия исходной ревизии воспроизводимо доказывают заявленное изменение, а не только выглядят формально полными. Не останавливайтесь на первом найденном blocker-е: нужен полный список доказуемых проблем в пределах области задачи.

Используйте динамические сведения только из файлов основного архива и сопровождающего ZIP с операторскими доказательствами:

- `AUDIT-MANIFEST.md` - инвентарь архива, задача, итерация, исходная ревизия, patch, доказательства и проверки.
- `metadata/audit-package.input.json` - нормализованная входная конфигурация, включая `metadata.scopeTaskIds`, `metadata.scopeSummary`, `metadata.previousVerdictChain` и `metadata.blockerClosureList`.
- `repo-file-hashes.json` - ожидаемая модель восстановления файлов репозитория.
- `SHA256SUMS.txt` - контрольные суммы содержимого архива.
- `<task-id>.patch` - patch, который должен применяться к исходной ревизии.
- `evidence/` в основном архиве - сырые доказательства настроенных проверок и импортированные файлы.
- `<task-id>-audit-rNN.operator-workflow.zip` - сопровождающий ZIP с `OPERATOR-WORKFLOW.md`, хэшем неизменяемого основного ZIP, списком его записей и сырыми доказательствами операторских команд.

Граница самодостаточности архива: текущий контракт использует обязательный внешний вход проверки восстановления - отдельную чистую копию исходной ревизии (`external baseline REQUIRED input`), переданную через `audit package verify --repo <clean-repo-path>`. Архив не включает и не обязан включать снимок, Git bundle, архив tar или другой вложенный набор файлов исходной ревизии (`no baseline payload requirement`). Отсутствие такого вложенного набора не является blocker-ом, если patch применим к чистой копии исходной ревизии, восстановленные файлы совпадают с `repo-file-hashes.json`, а сопровождающий ZIP содержит `baseline availability evidence`: сырые переносимые доказательства успешного `audit package verify` на отдельной чистой копии исходной ревизии.

Обязательное `operator workflow evidence` находится не внутри основного архива, а в сопровождающем ZIP `<task-id>-audit-rNN.operator-workflow.zip`. Этот ZIP должен содержать `audit package verify evidence` с `command.txt`, `stdout.txt`, `stderr.txt`, `exit-code.txt`, `duration-ms.txt` и `metadata.json` для реальной команды `audit package verify --zip <path> --baseline <sha> --repo <clean-repo-path>`, а также `audit package message evidence` для реальной команды `audit package message --zip <path>`. `payload/metadata.json`, `payload/sha256.txt` и `payload/archive-entries.txt` должны связывать эти доказательства с неизменяемым основным ZIP. Плейсхолдер `<clean-repo-path>` допустим в `command.txt`, но абсолютные пути локальной машины, секреты и ручной пересказ результата запрещены.

Не доверяйте описаниям на словах, если они не подтверждены файлами архива и восстановленной чистой копией исходной ревизии. Проверяйте воспроизводимость, полноту, безопасность содержимого, связь доказательств с изменёнными файлами, соответствие документации и тестов заявленному поведению, а также отсутствие скрытых допущений, кроме явно заданной чистой копии исходной ревизии.

Обязательный порядок проверки:

1. Проверьте структуру основного архива, его `SHA256SUMS.txt`, `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `repo-file-hashes.json` и сопровождающий ZIP с операторскими доказательствами.
2. Подготовьте отдельную чистую копию исходной ревизии как обязательный внешний вход проверки восстановления и примените patch через `git apply --check`, затем через `git apply`.
3. Сравните восстановленные файлы с `repo-file-hashes.json`.
4. Проверьте, что доказательства в `evidence/` основного архива соответствуют настроенным проверкам, а сопровождающий ZIP содержит `operator workflow evidence`, `audit package verify evidence` и `audit package message evidence`, относящиеся к хэшу текущего основного ZIP.
5. Прочитайте изменённые файлы репозитория и оцените, закрывают ли тесты и документация фактический риск задачи.
6. Проверьте, что архив не требует незафиксированных файлов, локальных путей машины, внешних конфиденциальных значений или ручных догадок.

Обязательная проверка области пакета:

1. Прочитайте `metadata.scopeTaskIds` и `metadata.scopeSummary` из `metadata/audit-package.input.json`.
2. Если `metadata.scopeTaskIds` пуст или отсутствует, область пакета равна одиночному `taskId` из metadata.
3. Если `metadata.scopeTaskIds` содержит несколько задач, проверяйте пакет как `combined scope`, то есть явно комбинированную область: `AUDIT-MANIFEST.md`, metadata, patch, `repo-file-hashes.json` и evidence должны согласованно объяснять, какие задачи входят в текущее содержимое пакета и почему их можно принимать одним verdict-ом.
4. Проверьте, что `AUDIT-MANIFEST.md` перечисляет ту же область, а `metadata.scopeSummary` не противоречит фактическому diff.
5. Если в diff есть изменения вне `metadata.scopeTaskIds` или summary, это blocker области задачи, даже когда отдельные проверки прошли.

Обязательная проверка цепочки предыдущих внешних verdict-ов:

1. Прочитайте `metadata.previousVerdictChain` из `metadata/audit-package.input.json`.
2. Для каждого существующего пути из `metadata.previousVerdictChain` найдите соответствующие previous verdict files в восстановленном репозитории, `repo-file-hashes.json`, patch и `AUDIT-MANIFEST.md`.
3. Проверьте verbatim preservation: содержимое каждого существующего previous verdict-файла должно сохраняться дословно в восстановленной модели файлов репозитория (`restored repo-owned model`), без переписывания формулировок, удаления цитат, нормализации доказательств или замены строк.
4. Прочитайте каждый previous verdict-файл и выпишите все предыдущие blocker-ы.
5. Прочитайте `metadata.blockerClosureList` и проверьте previous blockers closure: каждый предыдущий blocker должен иметь явное закрытие в текущих изменениях, тестах, документации или доказательствах. Нельзя принимать общий ответ вроде "все исправлено", если отдельный previous blocker не закрыт проверяемым фактом.
6. Если предыдущий verdict-файл указан в `metadata.previousVerdictChain`, но отсутствует в восстановленной модели, проверьте, что это явно объяснено как историческая ссылка и не требуется для доказательства текущего закрытия.

`metadata.previousVerdictChain` в текущем контракте доказывает дословное сохранение файлов, присутствующих в восстановленной модели репозитория, через patch, `repo-file-hashes.json` и `AUDIT-MANIFEST.md`. Он не является независимым нотариальным доказательством происхождения прошлых внешних отчётов и не требует предыдущие аудиторские ZIP-архивы или отдельные исторические контрольные суммы (`no previous audit packages or detached historical checksums required`), если такой слой явно не включён отдельным контрактом пакета.

Обязательная проверка восстановления, доказательств, секретов и области задачи:

- Выполните restore scanning: убедитесь, что patch, `repo-file-hashes.json`, `AUDIT-MANIFEST.md` и чистая копия восстанавливают один и тот же набор файлов и байтов.
- Выполните evidence scanning: проверьте сырые доказательства команд, коды завершения, вывод, TRX-файлы при наличии, `operator workflow evidence` из сопровождающего ZIP, связь доказательств с изменёнными файлами и то, что вывод `audit package message` является текстом, который должен быть отправлен во внешний аудит в режиме `Глубокое исследование`.
- Выполните secret scanning: проверьте архив, patch, доказательства, metadata и восстановленные текущие файлы на реальные секретные значения, приватные ключи, локальные абсолютные пути и конфиденциальные данные.
- Выполните scope scanning: проверьте, что изменения находятся в заявленной области `metadata.scopeTaskIds` и `metadata.scopeSummary`, а исключения для исторических previous verdict-файлов не скрывают новые изменения текущей задачи.

Контракт финального ответа:

- Отправьте ровно один single final report. Не отправляйте промежуточные сообщения, черновики, предварительные выводы, отдельные счётчики или частичные списки blocker-ов.
- Не используйте строку `VERDICT:` в промежуточных сообщениях (`no intermediate VERDICT`) и нигде, кроме первой непустой строки полного финального отчёта.
- Финальный отчёт должен содержать всю информацию для сохранения: blocker-ы, проверенные файлы, доказательства, риски и решение о закрытии.
- Первая непустая строка финального отчёта должна быть строго `VERDICT: ACCEPT` или `VERDICT: NEEDS_FIXES`.
- Если ответ нарушает этот контракт, сторона приёмки обязана считать аудит неполным, даже если в тексте встречается `VERDICT: ACCEPT`.

Машинные маркеры этого контракта: `metadata.scopeTaskIds`, `metadata.scopeSummary`, `combined scope`, `metadata.previousVerdictChain`, `metadata.blockerClosureList`, `previous verdict files`, `verbatim preservation`, `previous blockers closure`, `external baseline REQUIRED input`, `no baseline payload requirement`, `restored repo-owned model`, `no previous audit packages or detached historical checksums required`, `restore scanning`, `evidence scanning`, `operator workflow evidence`, `baseline availability evidence`, `audit package verify evidence`, `audit package message evidence`, `secret scanning`, `scope scanning`, `single final report`, `no intermediate VERDICT`.

Ответ должен быть строгим и пригодным для сохранения в `docs/verdicts/`. Первая строка ответа должна быть строго одной из двух:

- `VERDICT: ACCEPT`
- `VERDICT: NEEDS_FIXES`

Формат ответа:

```text
VERDICT: ACCEPT

TASK_ASSESSMENT:
- Кратко: что проверено и почему изменение можно принять.

BLOCKERS:
- No blockers found.

EVIDENCE_REVIEW:
- Какие файлы архива, команды и доказательства были проверены.

RISKS_AND_NOTES:
- Остаточные риски, вне-scope замечания или none.

CLOSURE_DECISION:
- Почему задача может быть закрыта.
```

```text
VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:
- Кратко: что проверено и почему изменение нельзя принять.

BLOCKERS:
- Перечислите все доказуемые blocker-ы как `B1`, `B2`, `B3` и далее. Для каждого blocker-а укажите:
  - `File/symbol`: точный файл, строка, symbol, команда или артефакт;
  - `Criterion`: какой критерий задачи, документа или audit package нарушен;
  - `Evidence`: конкретное доказательство из patch, manifest, metadata, evidence, restored files или tests;
  - `Impact`: почему это блокирует закрытие задачи;
  - `Fix`: какое исправление требуется;
  - `Verification`: какая команда, проверка или evidence должны подтвердить исправление.

EVIDENCE_REVIEW:
- Какие файлы архива, команды и доказательства были проверены.

RISKS_AND_NOTES:
- Остаточные риски, вне-scope замечания или none.

CLOSURE_DECISION:
- Почему задача остаётся открытой до исправлений.
```
