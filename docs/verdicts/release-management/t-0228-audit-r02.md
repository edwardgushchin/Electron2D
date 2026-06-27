# T-0228 audit r02

- Задача: T-0228
- Домен: release-management
- Актуально на: 2026-06-27T04:18:57+03:00
- Область проверки: acceptance audit архива `T-0228-audit-r02.zip` после `VERDICT: NEEDS_FIXES` r01.
- Статус вывода: VERDICT: NEEDS_FIXES
- Предыдущий аудит: `docs/verdicts/release-management/t-0228-audit-r01.md`
- Следующий аудит: `docs/verdicts/release-management/t-0228-audit-r03.md`

```text
VERDICT: NEEDS_FIXES

Blocker: evidence/T-0228-r02/checks/focused-repository-build-tool-tests, критерий, raw evidence/TRX из r01 blocker-а должны быть включены в archive evidence, metadata.json должен перечислять TRX paths/SHA, а AUDIT-MANIFEST.md должен ссылаться на них; evidence, stdout.txt focused tests сообщает TRX audit-evidence\T-0228\r02\test-results\repository-build-tool-tests\eduar_CYBERVUPSEN_2026-06-26_23_49_18.trx, но в ZIP нет ни одного *.trx, metadata.json содержит "trxFiles": [], а manifest не содержит TRX evidence links; fix, исправить trxGlobs/matching для focused test results, включить фактический TRX в evidence/T-0228-r02/checks/focused-repository-build-tool-tests/trx/..., добавить SHA в metadata.json и ссылку в AUDIT-MANIFEST.md, затем пересобрать r03; проверка, unzip -l T-0228-audit-r03.zip | rg '\.trx$', jq '.trxFiles | length' evidence/T-0228-r03/checks/focused-repository-build-tool-tests/metadata.json должен быть > 0, после этого audit package verify должен пройти.

Blocker: T-0228.patch, критерий, r01 blocker по line-ending/formatting churn должен быть закрыт: task-owned patch должен содержать focused semantic hunks без массовой CRLF/formatting-перезаписи существующих файлов; evidence, текущий patch всё ещё размером 9,319,068 bytes и переписывает почти целиком существующие файлы: AGENTS.md +158/-152, data/documentation/electron2d-local-docs-index.json +79088/-79026, docs/releases/0.1.0-preview.md +1376/-1351, eng/Electron2D.Build/Program.cs +429/-421; в patch найдено 84,937 CRLF sequences; fix, восстановить исходные line endings существующих tracked файлов и оставить только содержательные изменения T-0228, не использовать git diff --check как единственное доказательство отсутствия churn; проверка, git diff --numstat <baseline> и git diff --word-diff <baseline> должны показывать точечные изменения, а не full-file rewrite, затем пересобрать patch и rerun audit package verify.

Blocker: T-0228.patch / dev-diary/2026/06 Июнь/26-06-2026.md, критерий, архив и repo-owned patch не должны содержать machine-local absolute paths; evidence, patch содержит локальный путь G:\Downloads\TASKS-with-T-0228.md в добавляемой дневниковой записи, при этом доменный контракт audit package запрещает абсолютные пути машины вне явно разрешённых metadata; fix, удалить или обезличить локальный путь из дневника, усилить validation на Windows drive paths вида [A-Z]:\.../[A-Z]:/..., а не только на repoRoot/tempRoot; проверка, rg -n '[A-Z]:\\\\|[A-Z]:/' T-0228.patch AUDIT-MANIFEST.md AUDIT-REQUEST.md metadata/audit-package.input.json evidence не должен находить локальные drive paths, затем audit package verify должен пройти.
```
