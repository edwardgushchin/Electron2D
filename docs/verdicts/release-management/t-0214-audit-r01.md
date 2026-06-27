# Внешний аудит T-0214 r01

- Задача: T-0214
- Домен: release-management
- Актуально на: 2026-06-27T19:20:05+03:00
- Область проверки: verified audit package `T-0214-audit-r01.zip`; перенос проверок API, Wiki, лицензий и манифестов на C#-команды `eng/Electron2D.Build`.
- Статус вывода: VERDICT: NEEDS_FIXES
- Предыдущий аудит: нет
- Следующий аудит: T-0214 audit r02 после исправления B1-B4.

```text
VERDICT: NEEDS_FIXES
TASK_ASSESSMENT
Пакет T-0214 r01 проверен как внешний verified audit package по контракту AUDIT-REQUEST.md внутри архива и только в заявленном scope T-0214: перенос API/Wiki/license/manifest policy checks в C# repository tool eng/Electron2D.Build.
Проверены структура архива, AUDIT-MANIFEST.md, SHA256SUMS.txt, metadata/audit-package.input.json, repo-file-hashes.json, T-0214.patch, evidence-команды, TRX, task/domain docs и task-owned file set. Контрольные суммы архива согласованы; заявленные green checks имеют actualExitCode=0; focused TRX содержит 12 passed тестов; machine-local paths, .git, bin, obj, вложенные архивы и явные секреты в evidence/package не обнаружены.
Однако пакет не проходит acceptance: есть доказуемые blocker-ы в task state и в реализации Wiki/manifest verifier-ов. Основная проблема не в упаковке, а в том, что часть заявленных C# checks может давать ложный green при непроверенном или устаревшем Wiki/template output.
BLOCKERS
B1
file/symbol: TASKS.md, section T-0214
нарушенный критерий: задача должна быть приведена в согласованное состояние для acceptance-аудита; workflow/task metadata не должны противоречить closure state и evidence.
evidence: в task section T-0214 все checklist items отмечены как [x], а последняя запись дневника фиксирует перевод задачи в ready for acceptance, но поле состояния в той же секции остаётся - Состояние: blocked. Это внутренняя несогласованность audit package: задача одновременно заявлена как готовая к acceptance и как blocked.
impact: внешний аудитор не может принять closure state, потому что task workflow всё ещё формально маркирует задачу как заблокированную.
fix: обновить state T-0214 в TASKS.md до фактического состояния перед аудитом, например ready for acceptance, либо явно зафиксировать причину сохранения blocked и не подавать пакет как closure-ready.
verification: повторно выполнить task verifier, включая tools/Verify-Tasks.ps1, и приложить обновлённое evidence, где секция T-0214 не содержит противоречия между state, checklist и diary.
B2
file/symbol: eng/Electron2D.Build/RepositoryPolicyVerifiers.cs, ApiWikiCommand.Run
нарушенный критерий: dotnet run --project eng/Electron2D.Build -- update wiki --check должен проверять API manifest и Wiki output, как заявлено в scope T-0214 и domain docs.
evidence: реализация update wiki --check без --output выполняет shape-check API manifest, парсит manifest и затем при отсутствии OutputPath возвращает success с сообщением о verified Wiki generation. В этой ветке не создаётся временный Wiki output, не проверяется .github/wiki, не сравниваются сгенерированные страницы и не выявляется stale/missing Wiki content. Evidence update-wiki-check/stdout.txt подтверждает только manifest shape pass и общий E2D-BUILD-WIKI-CHECK-PASSED по пути data/api/electron2d-api-manifest.json, без generated page count, output path или content comparison.
impact: обязательная команда может проходить успешно при отсутствующем, устаревшем или непроверенном Wiki output. Это ложный green для одного из ключевых acceptance-критериев T-0214.
fix: изменить no-output --check semantics: либо генерировать expected Wiki output во временную директорию и валидировать его содержимое, либо требовать явный --output и завершаться nonzero без него. Сообщение success должно соответствовать реально выполненной проверке.
verification: добавить negative test, где Wiki output отсутствует или устарел, а update wiki --check возвращает nonzero. Повторить evidence для update wiki --check; stdout должен показывать, что проверялся именно Wiki output, а не только форма manifest.
B3
file/symbol: eng/Electron2D.Build/RepositoryPolicyVerifiers.cs, ApiWikiCommand.VerifyWikiOutput / WriteWikiOutput
нарушенный критерий: Wiki verifier/update должны детерминированно проверять и генерировать API Wiki reference output, а не только наличие файлов-маркеров.
evidence: VerifyWikiOutput проверяет наличие ожидаемых страниц, generated marker, отсутствие .md) links, наличие Full name на type pages и extra generated pages. Он не генерирует expected page body и не сравнивает actual Wiki pages с ожидаемым содержимым. WriteWikiOutput записывает минимальные stub pages: для required pages — marker и заголовок файла, для type pages — marker, заголовок и таблицу с Full name. Это не доказывает корректность generated API reference content. Существующие focused tests покрывают missing page и invalid manifest shape, но не покрывают stale body/member/category content при сохранённом marker и Full name.
impact: устаревшая или усечённая Wiki page с правильным marker и Full name может пройти --check; non-check update способен перезаписать Wiki минимальными stub pages и всё равно получить green verifier.
fix: реализовать deterministic render expected Wiki content из API manifest и сравнивать actual pages с expected content в check mode. Non-check update должен писать тот же полный deterministic output. Проверка должна ловить stale members/categories/index/body, а не только файл/маркер.
verification: добавить negative tests, которые меняют содержимое generated type page/index/member section при сохранении marker и Full name, и ожидать nonzero. Добавить positive round-trip: update wiki --output .github/wiki, затем update wiki --check --output .github/wiki должен проходить только для byte-identical или строго эквивалентного expected output.
B4
file/symbol: eng/Electron2D.Build/RepositoryPolicyVerifiers.cs, ProjectTemplateVerifier.LoadJsonObject
нарушенный критерий: verify manifests / verify project-template должны проверять форму project/template/task manifest files как C# policy checks.
evidence: LoadJsonObject возвращает null, если JSON root не является object, но не добавляет diagnostic. Callers затем выходят или пропускают дальнейшие checks. В результате валидный JSON с неверным root type, например [], строка или число, может пройти без structured error для файлов вроде project.e2d.json, .electron2d/tasks/board.e2tasks или welcome.e2task. Existing negative test покрывает invalid property внутри object, но не покрывает non-object JSON roots.
impact: manifest shape verifier не является надёжным gate: malformed-but-valid JSON manifests могут пройти acceptance checks.
fix: LoadJsonObject должен добавлять structured diagnostic при любом non-object root. Для каждого manifest/task file нужно явно валидировать required fields/schema shape, включая пустой object и неверный root type.
verification: добавить focused tests для [], "text", числа и {}/missing required fields по каждому relevant manifest/task file; повторить verify manifests и verify project-template evidence.
EVIDENCE_REVIEW
Архив и checksum layer в целом согласованы: SHA256SUMS.txt покрывает payload entries, кроме самого checksum-файла; metadata и manifest указывают T-0214 r01, baseline 5f702646abdaba6b21504ad49a9f9fd849b94fb4, domain release-management; previous verdict chain и blocker closure list пустые, что ожидаемо для r01.
Patch scope соответствует заявленной области T-0214: eng/Electron2D.Build, integration tests, release-management/documentation docs, local docs index, release notes и TASKS.md. Явных unrelated binary/temp/build artifacts в archive payload не выявлено.
Evidence содержит green runs для focused repository build tests и заявленных commands: update wiki --check, update wiki --check --output .github/wiki, update api-manifest --wiki-path .github/wiki --check, verify api-compatibility, verify licenses, verify manifests, verify project-template, verify release-metadata, verify docs, update docs --check, legacy source-license/tasks checks и git diff --check. Focused TRX показывает 12 passed tests.
Эти green checks не закрывают acceptance полностью, потому что B2–B4 показывают ложноположительные verifier paths: Wiki output verification не доказывает актуальность generated Wiki content, а manifest verifier пропускает non-object JSON roots. B1 дополнительно блокирует closure из-за несогласованного task state.
CLOSURE_DECISION
T-0214 r01 не принимается.
Задача может быть повторно подана на acceptance только после исправления B1–B4, добавления focused negative/positive tests на указанные failure modes, пересборки audit package и включения нового evidence, где обязательные C# commands проверяют именно заявленные API/Wiki/license/manifest contracts, а TASKS.md содержит согласованный closure state.
```
