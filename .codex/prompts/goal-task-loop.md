Выполнить <task-id>: получить primary/control `VERDICT: ACCEPT`, закрыть actionable `RISKS_AND_NOTES`, обновить `TASKS.md`, дневник, архив/release notes, удалить audit temp/ZIP, сделать Conventional Commit.

ПРАВИЛА: русский. Не выдумывай проверки, ZIP, SHA, status, verdict или факты аудита. Чужие изменения не трогай. `TASKS.md` и дневник веди по `AGENTS.md`. Точный контракт: `docs/release-management/audit-package.md`.

PREFLIGHT: прочитай `AGENTS.md`, `TASKS.md` по <task-id>, зависимости, `docs/<domain>/`, `docs/release-management/audit-package.md`, прошлые `docs/verdicts/<domain>/`, дневник. Зафиксируй HEAD, baseline, domain, scope, критерии, checks.

WORKER: документ -> failing tests -> implementation -> green checks -> sync. Если есть worker, дай scope, paths, запреты, критерии, checks; пока он активен, repo не меняй. Потом проверь отчёт/diff/scope.

CHECKS: сначала минимум релевантных проверок, затем `git diff --check`. После мелкой правки аудиторского запроса, локального prompt-а или разбора отчёта: `dotnet run --project eng/Electron2D.Build -- verify audit-contracts`. При source changes: `dotnet run --project eng/Electron2D.Build -- verify licenses`. Перед архивом: `dotnet run --project eng/Electron2D.Build -- verify audit-followups`.

LOCAL LOOP: внешний аудит не inner debugging loop. После мелкой правки - `Fast` regression без `audit package`, clean repo, restore или внешней отправки; для аудиторских контрактов это `verify audit-contracts`. Перед упаковкой - `Medium`: focused suite, docs/followups/licenses и `git diff --check`. `Heavy` (`audit package`, `audit package verify`, `audit package message`, `audit submit`) только перед отправкой. Перед `Heavy`: failure -> code/test -> command -> proof. Два повтора failure class: не увеличивай `rNN`; запиши отказ, добавь regression, зелёные `Fast`/`Medium`. В дневник пиши числа: `Fast: 3 теста / 1m13s; Medium: 4 теста / 3m18s; Heavy: не запускался`.

PACKAGE: ZIP собирай только `audit package --task <task-id> --iteration rNN --baseline <sha> --config <path> --out .temp/audit`; ручная сборка/правка запрещена. После первого `audit package`/`audit submit` scope заморожен: новые идеи, задачи, roadmap и критерии только follow-up.

VERIFY: перед отправкой clean-repo проверка `audit package verify --zip <path> --baseline <sha> --repo <clean-repo-path>`. Если падает - исправь и собери новую итерацию.

SUBMIT: только штатно:
`dotnet run --project eng/Electron2D.Build -- audit submit ... --browser-backend codex-chrome`
r01 без reuse. URL оставь в `.temp/audit/<task-id>/conversation-url.txt`; в `TASKS.md` можно указать этот путь, но не сам URL. После `NEEDS_FIXES`: `--reuse-conversation`; если нельзя, `--new-conversation` и причина. После primary ACCEPT: чистый control ZIP той же rNN/области без `previousVerdictChain`, `blockerClosureList`, verdict-отчётов; проверь и отправь с `--control-audit`. Изменилась область - новый primary. Ручная отправка/чтение verdict запрещены. Готовый результат принимает только сохранённый файл `--out`.

VERDICT: ACCEPT действителен только из сохранённого полного отчёта, где первая непустая строка ровно `VERDICT: ACCEPT`. Первый ACCEPT запускает control; задача готова только после control ACCEPT. `NEEDS_FIXES`: зафиксируй blocker-ы, исправь scope, повтори checks/package/verify/submit как новую rNN. Если control вернул `NEEDS_FIXES`, включи control verdict в `previousVerdictChain`, закрой blocker-ы через `blockerClosureList` и вернись к primary loop.

ACCEPT/DONE: готово, когда критерии закрыты, primary/control отчёты сохранены как `VERDICT: ACCEPT`, actionable `RISKS_AND_NOTES` закрыты tracked note, `accepted-risk`, `duplicate`, `not-actionable` или `promoted-blocker`. После ACCEPT изменение области требует новый primary. Обнови `TASKS.md`, архив, дневник, release notes. Удали audit temp/ZIP, выполни `git diff --check` и `git status`, сделай Conventional Commit без push. Финал: verdict, iteration, проверки, путь отчёта, commit, чистое состояние Git.
