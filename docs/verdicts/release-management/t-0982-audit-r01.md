VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:

* Проверен пакет `T-0982` итерации `r01`. Область пакета одиночная: исправление sanitizer-а imported preflight evidence так, чтобы локальные пути к корню репозитория заменялись читаемым `<repo>` без посимвольной вставки placeholder-а, а уже сломанные placeholder-per-character artifacts отклонялись.
* Реализация в целом находится в правильной области: изменён `AuditPackageCommand`, добавлены focused integration tests, обновлён доменный документ, generated docs index, карточка задачи и дневник. Preflight evidence показывает успешную сборку build tool, успешные focused tests `2/2`, успешные проверки документации, follow-up accounting, лицензий и `git diff --check`.
* Изменение нельзя принять, потому что текущий sanitizer может частично замаскировать нерепозиторный абсолютный путь, если этот путь начинается с той же строки, что и `repoRoot`, но фактически не является путём внутри репозитория. Это прямо нарушает ограничение текущей задачи: sanitizer не должен скрывать command/evidence output вместо замены только локальных absolute repo-root paths.

Техническая привязка:

* `metadata.taskId`: `T-0982`
* `metadata.iteration`: `r01`
* `metadata.scopeTaskIds`: `["T-0982"]`
* `metadata.scopeSummary`: `Fix audit package imported preflight evidence sanitization so local repo-root paths become readable <repo> placeholders atomically and placeholder-per-character artifacts are rejected.`
* Проверенные основные файлы: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`, `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`, `repo-after/docs/release-management/audit-package.md`, `repo-after/TASKS.md`, `repo-after/data/dev-diary/2026/07 Июль/07-07-2026.md`, `repo-after/data/documentation/electron2d-local-docs-index.json`
* Проверенные metadata/evidence: `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `metadata/repo-file-snapshots.json`, `repo-file-hashes.json`, `T-0982.patch`, `evidence/T-0982-r01/preflight/*`
* `metadata.previousVerdictChain`: `[]`
* `metadata.blockerClosureList`: `[]`
* `combined scope`: не используется, область одиночная

BLOCKERS:

* B1

  * Что не так: sanitizer заменяет найденную строку `repoRoot` без проверки, что совпадение является настоящим путём к корню репозитория или дочерним путём внутри него. Проверяется только символ после совпадения. При этом пробел считается допустимой границей. Поэтому строка вида `C:\work\repo backup\logs.txt` при `repoRoot = C:\work\repo` будет преобразована в `<repo> backup\logs.txt`. Такой путь не находится внутри репозитория, но после частичной замены drive-prefix исчезает, и последующая проверка локальных машинных путей уже не сможет его поймать.
  * Почему это важно: текущая задача явно запрещает sanitizer-у скрывать вывод вместо замены только локальных absolute repo-root paths. Документация, обновлённая этой же задачей, также обещает, что нерепозиторные машинные пути остаются блокерами. Текущее поведение создаёт обратное: часть нерепозиторного абсолютного пути может быть замаскирована как `<repo>`, и audit package станет выглядеть переносимым, хотя исходный evidence содержал локальный путь вне репозитория.
  * Что исправить: заменить строковый prefix-replace на проверку полноценного path-token. Минимально нужно не заменять `repoRoot`, если после него идёт символ, который может быть продолжением соседнего абсолютного пути, например пробел в имени sibling-каталога вроде `repo backup`. Безопасный путь: заменять только точное значение `repoRoot` как отдельный токен или `repoRoot` с немедленным `/`/`\` как дочерний путь, а остальные absolute machine paths оставлять на существующий blocker `E2D-BUILD-AUDIT-ABSOLUTE-PATH`.
  * Как проверить исправление: добавить focused regression, где imported preflight `output.txt` содержит нерепозиторный путь с общим prefix-ом, например `fixture.RepositoryRoot + " backup" + Path.DirectorySeparatorChar + "logs.txt"` и slash-normalized аналог. Ожидаемый результат — `audit package` завершается с `E2D-BUILD-AUDIT-ABSOLUTE-PATH`, а не создаёт ZIP с `<repo> backup/...`. Сохранить существующие позитивные проверки для настоящих child paths внутри repo root и для уже существующего `<repo>/already-normalized.txt`.
  * Проверка опровержения: проверены новые tests `AuditPackageSanitizesPreflightEvidenceRepositoryRootAtomically` и `AuditPackageRejectsBrokenPreflightPlaceholderPerCharacterEvidence`, доменный документ и preflight evidence. Tests покрывают обычный текст, настоящий child path внутри `fixture.RepositoryRoot`, slash-normalized child path и broken `<repo>/n<repo>/a<repo>/m`, но не покрывают sibling/non-repo absolute path с тем же prefix-ом. Документация не снимает проблему, а наоборот фиксирует, что нерепозиторные машинные пути должны оставаться блокерами.
  * Техническая привязка:

    * `File/symbol`: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`, `ReplaceRepoRootPathCandidate`, строки 1385-1423; `IsRepoRootPathCandidateBoundary`, строки 1425-1435
    * `Criterion`: `repo-after/TASKS.md`, строки 3728-3731 и 3759-3766; `repo-after/docs/release-management/audit-package.md`, строка 451
    * `Evidence`: `ReplaceRepoRootPathCandidate` ищет `candidate` через `IndexOf` и принимает замену, если `IsRepoRootPathCandidateBoundary(text, index + candidate.Length)` вернул `true`; `IsRepoRootPathCandidateBoundary` считает whitespace допустимой границей. Проверки начала token-а и проверки, что совпадение не является prefix-ом нерепозиторного absolute path, нет.
    * `Impact`: sanitizer может скрыть локальный абсолютный путь вне репозитория, что нарушает безопасность и переносимость evidence текущей задачи.
    * `Fix`: ввести path-token проверку и negative regression для нерепозиторного path-prefix случая.
    * `Verification`: focused integration test через production `audit package` path должен падать с `E2D-BUILD-AUDIT-ABSOLUTE-PATH` на sibling path с общим prefix-ом; существующие позитивные tests должны продолжать проходить.

EVIDENCE_REVIEW:

* Полные snapshots присутствуют для всех изменённых файлов, перечисленных в metadata. `metadata/repo-file-snapshots.json` помечает каждый важный файл как `fullContentIncluded: true`, поэтому аудит выполнялся по `repo-after/`, а patch использовался только как карта изменений.
* Проверена реализация sanitizer-а в `AuditPackageCommand`: imported preflight evidence теперь готовится через staging-copy, текстовые artifacts проходят `SanitizePreflightEvidenceText`, `metadata.json` нормализует `command`, а broken placeholder pattern отклоняется через `E2D-BUILD-AUDIT-PREFLIGHT-SANITIZER`.
* Проверены тесты. Добавлены два focused integration tests: позитивный тест на читаемую замену настоящего repo-root child path и стабильность существующего `<repo>` placeholder-а; негативный тест на `<repo>/n<repo>/a<repo>/m<repo>/e`. Покрытие основной регрессии есть, но оно не закрывает blocker B1 по нерепозиторному path-prefix случаю.
* Проверена документация. `docs/release-management/audit-package.md` обновлён правилом для imported text preflight artifacts, включая `output.txt`/`result.txt`, атомарную замену локального repo root на `<repo>`, отказ от placeholder-per-character artifacts и сохранение нерепозиторных machine paths как blockers.
* Проверены preflight evidence artifacts. Все заявленные проверки завершились exit code `0`: build tool build, focused sanitizer tests, `verify audit-contracts`, `update docs --check`, `verify docs`, `verify audit-followups`, `verify licenses`, `git diff --check`, а также sanitizer fixture. Evidence fixture показывает читаемые `<repo>` placeholders и не содержит локального repo-root path.
* Проверены область и лишние правки. Изменения ограничены release-management/audit-package scope: C# build tool, integration tests, доменный документ, generated docs index, `TASKS.md` и dev diary. Публичный runtime API, audit submit browser automation, saved verdict reports и исторические audit ZIP не изменялись.
* Проверены секреты и локальные данные в пределах архива. Реальных секретов, приватных ключей, токенов или локальных абсолютных путей в текущих included artifacts не найдено. Найденная проблема B1 относится не к уже попавшему в ZIP секрету, а к доказуемому дефекту sanitizer logic, который может скрывать нерепозиторные local paths в новых packages.
* Проверены прошлые verdict-файлы. `metadata.previousVerdictChain` пуст, `metadata.blockerClosureList` пуст, поэтому проверка verbatim preservation и closure matrix для прошлых `NEEDS_FIXES` не применима к этой первой итерации.

Техническая привязка:

* `implementation content review`: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`
* `test coverage review`: `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`
* `documentation review`: `repo-after/docs/release-management/audit-package.md`, `repo-after/data/documentation/electron2d-local-docs-index.json`
* `task compliance review`: `repo-after/TASKS.md`, `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`
* `secret scanning`: `T-0982.patch`, `repo-after/*`, `evidence/T-0982-r01/preflight/*`
* `scope scanning`: `repo-file-hashes.json`, `metadata/repo-file-snapshots.json`, `AUDIT-MANIFEST.md`
* `evidence gap`: нет по полноте snapshots; есть поведенческий blocker B1 по sanitizer contract
* Проверенные evidence paths: `evidence/T-0982-r01/preflight/t0982-build-tool-build/*`, `t0982-focused-sanitizer-tests/*`, `t0982-sanitizer-fixture/*`, `t0982-verify-audit-contracts/*`, `t0982-update-docs-check/*`, `t0982-verify-docs/*`, `t0982-verify-audit-followups/*`, `t0982-verify-licenses/*`, `t0982-git-diff-check/*`

RISKS_AND_NOTES:

* None.

CLOSURE_DECISION:

* Задача остаётся открытой. Текущий пакет закрывает исходную placeholder-per-character регрессию для обычного случая и имеет полезные tests/docs, но sanitizer всё ещё может скрыть класс нерепозиторных absolute paths, начинающихся с той же строки, что и repo root. До исправления B1 принятие `T-0982` небезопасно, потому audit package evidence может выглядеть переносимым после частичной маскировки локального пути вне репозитория.
