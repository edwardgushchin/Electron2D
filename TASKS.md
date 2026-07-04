## T-0092 [ ] P0: Подготовить экспорт iOS arm64: Xcode-проект, Metal, касания, безопасную область экрана и подписание на macOS

- Создана: 2026-06-20T16:16:20+03:00
- Состояние: ready for acceptance
- Приоритет: P0
- Зависимости: T-0087, T-0023, T-0051, T-0224
- Ссылки:
  - Upstream commit: нет
  - Спецификация: `docs/export/`
  - Документация: `docs/export/`
  - Исходный код: `src/Electron2D/`; `src/Electron2D/Runtime/`; `examples/`

### Самодостаточное описание

Задача относится к домену «Export pipeline и платформы». Цель - подготовить экспорт iOS arm64: Xcode-проект, путь рендера через Metal, касания, безопасную область экрана (`safe area`), жизненный цикл приложения и подписание на macOS. iOS является полноценной платформой запуска и экспорта Electron2D и обязательным релизным блокером `0.1.0 Preview`, потому что `T-0224` закрепила все шесть платформ в `releaseVerificationTargets`. Нужное поведение: результат должен быть проверяемым автоматическими тестами или документированной проверкой на macOS/Xcode и соответствовать критериям: проверка на устройстве или симуляторе задокументирована; автоматизация App Store Connect исключена.

Важное решение из документации: поддержка платформы означает не только сборку, но запуск приложения, рендер, ввод, звук, ресурсы, сохранение данных, жизненный цикл и завершение. Ограничение для агента: не выполнять подписание, установку или публикацию с реальными учётными данными; отсутствующие SDK и учётные данные должны давать понятную ошибку. Не менять несвязанные файлы, не выносить iOS из матрицы запуска и экспорта, не считать отчёт о недоступном окружении успешной релизной проверкой и не закрывать задачу без проверяемого результата.

### Критерии приёмки

- [ ] iOS export создаёт Xcode project с предсказуемой структурой targets, assets, Info.plist, entitlements и build settings.
- [ ] arm64 target выбран явно; simulator/device target различаются в планировщике и диагностике.
- [ ] Rendering backend для iOS использует Metal-compatible path или возвращает стабильную ошибку, если host/toolchain не поддерживает сборку.
- [ ] Touch input, safe area, pause/resume, background/foreground lifecycle и shutdown описаны в доменном документе и покрыты доступной smoke-проверкой.
- [ ] Signing configuration генерируется как non-secret настройки и placeholders; реальные credentials, provisioning profiles и App Store Connect automation не выполняются агентом.
- [ ] Device или simulator smoke выполнен на macOS/Xcode либо задача остаётся `blocked` с artifact, который фиксирует недоступные `xcodebuild`/`simctl`/workload prerequisites.
- [ ] Если задача меняет код, готов или обновлен доменный документ в `docs/<domain>/`, соответствующий фактическому изменению.
- [ ] Если задача меняет код, добавлены или обновлены тесты, покрывающие новое поведение.
- [ ] Если задача меняет исполняемый код, API, доменную модель, конфигурацию или пользовательское поведение, тот же доменный документ в `docs/<domain>/` отражает фактический результат изменения кода.
- [ ] Команда тестирования или проверки задокументирована.

### Подзадачи

- [ ] Уточнить контракт задачи по связанным спецификациям и текущему коду.
  - [ ] Сверить post-preview iOS contract, архитектурный стек и доменную спецификацию.
    - [ ] Зафиксировать расхождения, ограничения и неочевидные решения в дневнике разработки.
- [ ] Подготовить минимальное изменение, которое закрывает критерии приёмки.
  - [ ] Добавить или обновить автоматические проверки до изменения production-кода, если задача меняет поведение.
    - [ ] Убедиться, что проверка действительно покрывает заявленный критерий, а не только happy path.
- [ ] Завершить реализацию, документацию и проверку.
  - [ ] Обновить `docs/export/`, если задача меняет исполняемое поведение или публичный контракт.
    - [ ] Записать итоговые команды проверки и результат в дневник разработки.

### Заметки агента

2026-06-21T20:16:00+03:00 - Blocked по окружению, задача не завершена и не переносится в `data/completed-tasks/`. Текущий host - Windows; `xcodebuild` и `simctl` недоступны, `DEVELOPER_DIR`/`XCODE_ROOT` не заданы, iOS workload не установлен. Критерии требуют Xcode project generation и device/simulator smoke, поэтому закрывать задачу без macOS/Xcode/iOS simulator или device нельзя.

Для разблокировки нужен macOS host с Xcode, iOS simulator или подключённым iOS device, установленный iOS/.NET workload и user-provided signing setup. App Store Connect automation остаётся out of scope.

2026-06-23T16:31:00+03:00 - После закрытия `T-0096` dependency graph снова проверен. Пользователь сообщил «разблокировал», но текущий host остаётся Windows: `xcodebuild` и `simctl` не найдены, `DEVELOPER_DIR`/`XCODE_ROOT` не заданы. Android SDK/телефон не снимают iOS blocker, потому что критерии `T-0092` требуют macOS/Xcode и iOS device или simulator smoke.

2026-06-23T16:39:00+03:00 - Выполнен частичный проверяемый прогресс без закрытия задачи: добавлены спецификация `docs/export/ios-arm64-export.md`, iOS planner, transient Xcode staging builder и blocked smoke artifact writer. Focused tests и export-regression tests прошли, но `T-0092` остаётся активной, потому что на текущем Windows host не выполнен обязательный macOS/Xcode simulator или device smoke.

2026-06-23T16:47:00+03:00 - Выполнен дополнительный локально проверяемый прогресс без закрытия задачи: diagnostic code для отсутствующего Xcode в generic toolchain validation отделён от `E2D-EXPORT-IOS-0001`, который закреплён за неверным iOS target в planner. Новый код `E2D-EXPORT-IOS-0013` синхронизирован в specifications/documentation и покрыт focused test. `T-0092` всё ещё требует macOS/Xcode simulator или device smoke.

2026-06-23T16:59:00+03:00 - Выполнен дополнительный локально проверяемый прогресс без закрытия задачи: добавлены CLI routes `e2d export plan-ios`, `e2d export build-ios --skip-publish true` и `e2d export run-ios`, которые возвращают stable JSON envelope, создают transient Xcode staging project и пишут blocked smoke artifact с `E2D-EXPORT-IOS-0011` без queue generic export job. Focused CLI tests, iOS/export tests, documentation verifiers, source checks и Release build прошли. `T-0092` остаётся активной до реального macOS/Xcode simulator или device smoke.

2026-06-23T17:01:00+03:00 - Повторный blocker audit после commit `34dac5f`: локально проверяемый planner/staging/CLI/blocked-artifact scope выполнен. Оставшийся критерий `T-0092` требует macOS host с Xcode и iOS simulator или device smoke. На текущем Windows host `xcodebuild` и `simctl` недоступны, `DEVELOPER_DIR`/`XCODE_ROOT` не заданы; прежний dependency graph делал `T-0092` блокером `T-0093`, `T-0104`, `T-0105`, `T-0110`, `T-0111`.

2026-06-24T13:36:00+03:00 - УСТАРЕЛО в части формулировки runtime matrix: эта заметка вывела iOS из обязательного preview path, но не должна читаться как удаление iOS из runtime/export targets. Актуальное решение от 2026-06-24T19:45:00+03:00: iOS остаётся полноценным runtime/export target, а обязательность iOS smoke/soak для конкретного релиза должна быть решена через `T-0224`.

2026-06-24T22:12:00+03:00 - После отказа пользователя принять `T-0224` приоритет повышен до `P0`: продуктовый релизный документ теперь требует все шесть платформ в `releaseVerificationTargets`, поэтому реальная проверка iOS на macOS/Xcode входит в критический путь `T-0093` и `T-0104`. Отчёт о недоступном окружении остаётся допустимой диагностикой для разработки, но не закрывает финальную релизную проверку.

## T-0093 [ ] P0: Настроить платформенную быструю проверку первого уровня и 30-минутные длительные прогоны для preview projects

- Создана: 2026-06-20T16:16:20+03:00
- Состояние: blocked
- Приоритет: P0
- Зависимости: T-0088, T-0089, T-0090, T-0091, T-0092, T-0094, T-0136, T-0224
- Внешние блокеры: ProjectTaskManager Platformer в `examples/platformer/.electron2d/tasks/` (`T-0222`, `T-0223`, `T-0225`, `T-0221`, `T-0166`).
- Ссылки:
  - Upstream commit: нет
  - Спецификация: `docs/releases/0.1.0-preview.md`; `docs/export/`
  - Документация: `docs/export/`
  - Исходный код: `src/Electron2D/`; `src/Electron2D/Runtime/`; `examples/`

### Самодостаточное описание

Задача относится к домену «Export pipeline и платформы». Цель - настроить быструю платформенную проверку первого уровня и 30-минутные длительные прогоны для приёмочных проектов из релизной матрицы, включая `Platformer` после переименования. Текущее состояние: единый export pipeline и обязательный набор быстрых и длительных проверок нужно синхронизировать через `T-0224`, потому что матрица запуска и экспорта включает Windows, Linux, macOS, Android, iOS и WebAssembly, а платформы редактора ограничены desktop. Нужное поведение: результат должен закрыть релизный контракт `0.1.0 Preview`, быть проверяемым автоматическими тестами или документированной проверкой и соответствовать критериям: каждая обязательная preview-платформа проверяет запуск, рендеринг, ввод, звук, ресурсы, переключение сцены, сохранение данных, паузу, возобновление и завершение.

Важное решение из документации: поддержка платформы означает не только сборку, но запуск приложения, рендер, ввод, звук, ресурсы, сохранение данных, жизненный цикл и завершение. Ограничение для агента: не выполнять подписание, установку или публикацию с реальными учётными данными; отсутствующие SDK и учётные данные должны давать понятную ошибку. Не менять несвязанные файлы, не расширять scope за пределы `0.1.0 Preview`, не закрывать задачу без проверяемого результата и не подменять обязательные тесты или документацию устным утверждением.

### Критерии приёмки

- [ ] Platform matrix зафиксирована в доменном документе и различает `runtimeTargets` Windows/Linux/macOS/Android/iOS/WebAssembly, desktop `editorTargets` Windows/Linux/macOS и release verification tier `0.1.0 Preview`; для каждой обязательной preview-платформы указаны architecture, host prerequisites и expected smoke/soak commands.
- [ ] Каждая обязательная preview-платформа проверяет запуск, рендеринг, ввод, звук, ресурсы, переключение сцены, сохранение данных, паузу, возобновление и завершение.
- [ ] Soak threshold описывает длительность 30 минут, допустимый рост памяти, допустимый FPS/frametime диапазон, crash/hang criteria и итоговый pass/fail rule.
- [ ] Artifact schema фиксирует stdout/stderr, JSON summary, screenshots или runtime probes, memory samples, package manifest и blocked-environment diagnostics.
- [ ] Smoke/soak запускаются на валидных проектах `Electron2D.Editor` из canonical release matrix, включая `Platformer` после `T-0212`, а не на standalone demo или surrogate.
- [ ] Перед smoke/soak подтверждено, что `Platformer` больше не является smoke-fixture подсистем: игровой цикл, geometry/camera/HUD alignment, collectible/checkpoint/death/goal path, black-box acceptance и visual gate приняты по `T-0222`, `T-0223` и `T-0225`.
- [ ] Production package contents для каждой платформы проверяет, что `.electron2d/tasks/**` и другие `EditorMetadata` не включены в runtime asset pack, APK, AAB, app bundle или desktop distribution.
- [ ] Если задача меняет код, готов или обновлен доменный документ в `docs/<domain>/`, соответствующий фактическому изменению.
- [ ] Если задача меняет код, добавлены или обновлены тесты, покрывающие новое поведение.
- [ ] Если задача меняет исполняемый код, API, доменную модель, конфигурацию или пользовательское поведение, тот же доменный документ в `docs/<domain>/` отражает фактический результат изменения кода.
- [ ] Команда тестирования или проверки задокументирована.

### Подзадачи

- [ ] Уточнить контракт задачи по связанным спецификациям и текущему коду.
  - [ ] Сверить требования `0.1.0 Preview`, архитектурный стек и доменную спецификацию.
    - [ ] Зафиксировать расхождения, ограничения и неочевидные решения в дневнике разработки.
- [ ] Подготовить минимальное изменение, которое закрывает критерии приёмки.
  - [ ] Добавить или обновить автоматические проверки до изменения production-кода, если задача меняет поведение.
    - [ ] Убедиться, что проверка действительно покрывает заявленный критерий, а не только happy path.
- [ ] Завершить реализацию, документацию и проверку.
  - [ ] Обновить `docs/export/`, если задача меняет исполняемое поведение или публичный контракт.
    - [ ] Записать итоговые команды проверки и результат в дневник разработки.

### Заметки агента

2026-06-22T00:49:00+03:00 - Задача заблокирована до готовности законченных preview projects и реальных ассетов. Tier 1 smoke/soak должен проверять launch, rendering, input, audio, resources, scene switch, save data, pause/resume и shutdown на полноценном проекте из release matrix, а не на временном demo или surrogate с placeholder resources.

2026-06-22T01:26:00+03:00 - Пользователь перенёс примеры игр в самый последний этап `0.1.0 Preview`, поэтому `T-0093` остаётся заблокированной до финальной фазы preview projects. Не заменять smoke/soak временным demo.

2026-06-24T14:32:00+03:00 - После rejection README/release scope и решения удалить UI-heavy не использовать формулировки «две reference games» или «обе reference games». Проверки должны ссылаться на `Platformer` и canonical release matrix.

2026-06-23T16:31:00+03:00 - Историческое состояние до уточнения целей preview: `T-0094`, `T-0095` и `T-0096` закрыты, но `T-0093` всё ещё заблокирована зависимостью `T-0092`: без iOS device/simulator smoke нельзя считать Tier 1 platform smoke/soak полным.

2026-06-24T13:36:00+03:00 - УСТАРЕЛО в части формулировки runtime matrix: эта заметка ограничила обязательный preview smoke Windows/Linux/macOS/Android и вывела iOS из зависимостей `T-0093`, но не должна читаться как исключение iOS из runtime/export targets. Актуальное решение от 2026-06-24T19:45:00+03:00: iOS и WebAssembly остаются полноценными runtime/export targets; `T-0224` должна развести полный runtime target set и обязательный release verification tier.

2026-06-24T19:20:00+03:00 - Повторный аудит пользователя заблокировал `T-0093`: текущий `Platformer` не считается законченной игрой, а `RuntimeHost` даёт тяжёлый per-frame rendering path и некорректный frame pacing. До smoke/soak нужны `T-0219`, `T-0220`, `T-0221`, `T-0222`, `T-0223`, `T-0224` и `T-0225`.

2026-06-30T12:21:00+03:00 - По `T-0234` активные Platformer-задачи перенесены в проектный `ProjectTaskManager`; в этом корневом task-е они остаются внешним блокером через `examples/platformer/.electron2d/tasks/board.e2tasks`, а не локальными зависимостями корневого `TASKS.md`.

2026-06-24T22:12:00+03:00 - После отказа принять `T-0224` в зависимости `T-0093` возвращена `T-0092`. Пока `releaseVerificationTargets` содержит все шесть платформ, iOS-проверка на macOS/Xcode является обязательной частью критического пути, а blocked artifact не может быть бессрочным обходом перед финальным релизным проходом.

## T-0104 [ ] P0: Провести полный release candidate gate для `0.1.0 Preview`

- Создана: 2026-06-20T16:16:20+03:00
- Состояние: open
- Приоритет: P0
- Зависимости: T-0093, T-0096, T-0097, T-0103, T-0111, T-0113, T-0128, T-0189
- Внешние блокеры: ProjectTaskManager Platformer в `examples/platformer/.electron2d/tasks/` (`T-0222`, `T-0223`, `T-0225`, `T-0221`, `T-0166`).
- Ссылки:
  - Upstream commit: нет
  - Спецификация: `docs/releases/0.1.0-preview.md`; `docs/quality/`
  - Документация: `docs/quality/`
  - Исходный код: `src/Electron2D/`; `src/Electron2D/Runtime/`; `examples/`

### Самодостаточное описание

Задача относится к домену «Производительность, стабильность и релизный gate». Цель - Провести полный release candidate gate для `0.1.0 Preview`. Текущее состояние: нет формализованного performance, leak, soak и release candidate gate, хотя спецификация требует доказать стабильность данных, FPS и ресурсную чистоту. Нужное поведение: результат должен закрыть релизный контракт `0.1.0 Preview`, быть проверяемым автоматическими тестами или документированной проверкой и соответствовать критериям: сторонний разработчик может пройти путь install -> project -> scene -> C# code -> run -> debug -> export -> play.

Важное решение из документации: релиз готов только после проверок FPS, отсутствия постоянных allocations, отсутствия утечек и устойчивости save/load/import cycles. Ограничение для агента: не засчитывать узкий smoke test как доказательство полного release gate. Не менять несвязанные файлы, не расширять scope за пределы `0.1.0 Preview`, не закрывать задачу без проверяемого результата и не подменять обязательные тесты или документацию устным утверждением.

### Критерии приёмки

- [ ] Release candidate commit SHA, version, build number и source branch зафиксированы в gate artifact до запуска проверок.
- [ ] Точный набор команд release gate зафиксирован в доменном документе и выполнен в указанном порядке.
- [ ] Сторонний разработчик может пройти путь install -> project -> scene -> C# code -> run -> debug -> export -> play.
- [ ] Итоговый release manifest содержит список artifacts, hashes, platform matrix, preview projects, smoke/soak results, blocked checks и одно общее решение `pass`/`fail`.
- [ ] Editor co-development и headless AI acceptance benchmarks из `T-0128` пройдены и включены в release gate.
- [ ] Финальный Editor gate `T-0189` пройден, если `0.1.0 Preview` сохраняет требование пути через зрелый Editor UI.
- [ ] Release packages и draft release workflow из `T-0111` готовы до проверки пути install -> project -> scene -> code -> run -> debug -> export -> play.
- [ ] `Platformer` из canonical release matrix подтверждён как валидный проект `Electron2D.Editor`: Project Manager open, project validation, save/reopen, run/debug/export и package contents checks прошли.
- [ ] Release gate не засчитывает `Platformer`, пока не пройдены реальный игровой вертикальный срез `T-0222`, black-box acceptance `T-0223`, visual/screenshot gate `T-0225` и live performance gate `T-0221`.
- [ ] Production package contents для всех release targets проверены и не содержат `.electron2d/tasks/**` или другие `EditorMetadata`.
- [ ] Если задача меняет код, готов или обновлен доменный документ в `docs/<domain>/`, соответствующий фактическому изменению.
- [ ] Если задача меняет код, добавлены или обновлены тесты, покрывающие новое поведение.
- [ ] Если задача меняет исполняемый код, API, доменную модель, конфигурацию или пользовательское поведение, тот же доменный документ в `docs/<domain>/` отражает фактический результат изменения кода.
- [ ] Команда тестирования или проверки задокументирована.

### Подзадачи

- [ ] Уточнить контракт задачи по связанным спецификациям и текущему коду.
  - [ ] Сверить требования `0.1.0 Preview`, архитектурный стек и доменную спецификацию.
    - [ ] Зафиксировать расхождения, ограничения и неочевидные решения в дневнике разработки.
- [ ] Подготовить минимальное изменение, которое закрывает критерии приёмки.
  - [ ] Добавить или обновить автоматические проверки до изменения production-кода, если задача меняет поведение.
    - [ ] Убедиться, что проверка действительно покрывает заявленный критерий, а не только happy path.
- [ ] Завершить реализацию, документацию и проверку.
  - [ ] Обновить `docs/quality/`, если задача меняет исполняемое поведение или публичный контракт.
    - [ ] Записать итоговые команды проверки и результат в дневник разработки.

### Заметки агента

2026-06-24T19:20:00+03:00 - Release gate дополнительно заблокирован повторным аудитом Platformer/FPS. Старый static performance artifact и текущий subsystem fixture не являются доказательством релизной готовности; нужен fresh gate `T-0221` поверх исправленного runtime и настоящей игры.

2026-06-30T12:21:00+03:00 - По `T-0234` Platformer-зависимости release gate перенесены в проектный `ProjectTaskManager`; `T-0104` проверяет их как внешний блокер через доску `examples/platformer/.electron2d/tasks/board.e2tasks`.

2026-06-24T19:45:00+03:00 - Dependency graph усилен после аудита пользователя: `T-0104` не может доказывать путь install -> project -> scene -> code -> run -> debug -> export -> play без `T-0111` release packages/draft workflow и `T-0189` финального Editor gate. Если зрелый Editor будет выведен из `0.1.0 Preview`, это должно быть отдельным решением с изменением dependency graph.

## T-0105 [ ] P1: Подготовить post-preview список рисков и explicit exclusions, не блокирующих `0.1.0`

- Создана: 2026-06-20T16:16:20+03:00
- Состояние: in progress
- Приоритет: P1
- Зависимости: T-0104
- Ссылки:
  - Upstream commit: нет
  - Спецификация: `docs/releases/0.1.0-preview.md`; `docs/quality/`
  - Документация: `docs/quality/`
  - Исходный код: `src/Electron2D/`; `src/Electron2D/Runtime/`; `examples/`

### Самодостаточное описание

Задача относится к домену «release risk register». Цель - подготовить документальный список post-preview рисков и explicit exclusions, которые сознательно не блокируют `0.1.0 Preview`. Это не performance/leak implementation task и не замена release candidate gate.

Нужное поведение: каждый риск описан как запись risk register: id, short title, affected area, impact, likelihood, mitigation, owner/next decision point и решение `accepted`, `deferred` или `excluded`. Out-of-scope items из release spec отражены в committed release documentation и не смешаны с активными задачами `0.1.0`.

### Критерии приёмки

- [ ] Risk register создан или обновлён в committed доменном документе, а не только в ignored `RELEASE-NOTES.md`.
- [ ] Каждая запись содержит id, affected area, impact, likelihood, mitigation, owner/next decision point и decision state.
- [ ] Explicit exclusions не дублируются как активные `0.1.0` задачи и имеют ссылку на post-preview или deferred decision.
- [ ] Release notes получают только approved summary, а полный рабочий register остаётся в доменном документе.
- [ ] Команда тестирования или проверки задокументирована.

### Подзадачи

- [ ] Уточнить контракт задачи по связанным спецификациям и текущему коду.
  - [ ] Сверить требования `0.1.0 Preview`, архитектурный стек и доменную спецификацию.
    - [ ] Зафиксировать расхождения, ограничения и неочевидные решения в дневнике разработки.
- [ ] Подготовить минимальное изменение, которое закрывает критерии приёмки.
  - [ ] Добавить или обновить автоматические проверки до изменения production-кода, если задача меняет поведение.
    - [ ] Убедиться, что проверка действительно покрывает заявленный критерий, а не только happy path.
- [ ] Завершить реализацию, документацию и проверку.
  - [ ] Обновить `docs/quality/`, если задача меняет исполняемое поведение или публичный контракт.
    - [ ] Записать итоговые команды проверки и результат в дневник разработки.

### Заметки агента

- 2026-06-21T01:23:00+03:00 - Историческая заметка из прежнего контекста попала в задачу ошибочно и не задаёт текущий scope `T-0105`.
- Граница scope после аудита 2026-06-24: это документальный risk register для post-preview рисков и явных exclusions; unrelated `Variant`/serialization work не входит в задачу.
- 2026-06-23T16:41:00+03:00 - Пользователь запросил выполнить `T-0105`, затем `T-0110` и `T-0111`, но `T-0105` остаётся заблокированной зависимостью `T-0104`. Не начинать без закрытия `T-0104` или явного изменения dependency graph.

## T-0110 [ ] P0: Переписать публичный README как страницу продукта

- Создана: 2026-06-20T22:10:06+03:00
- Состояние: blocked
- Приоритет: P0
- Зависимости: T-0212, T-0213
- Ссылки:
  - Upstream commit: нет
  - Доменный документ: `docs/documentation/repository-readme.md`
  - Референсы:
    - `https://github.com/EggyStudio/3DEngine`
    - `https://github.com/godotengine/godot`
    - `https://github.com/sjoerdev/concrete`
    - `https://github.com/edwardgushchin/SDL3-CS`
  - Исходный код: `README.md`; `CODE_OF_CONDUCT.md`; `LICENSE`
  - Будущий verifier: `eng/Electron2D.Build -- verify readme`

### Самодостаточное описание

Задача относится к домену «Project presentation». Корневой `README.md` должен быть публичной страницей продукта, а не отчётом агента о внутренних проверках. Целевой формат берёт структуру SDL3-CS: центрированный hero, аккуратные badges, верхняя навигация, просьба поставить звезду, emoji-акценты и человеческие разделы About, Features, Platforms, Installation, Quick Start, Documentation, Examples, Feedback, Contributors и License.

README должен полностью перейти на английский язык, объяснять Electron2D как cross-platform 2D game engine for .NET и не раскрывать локальную систему задач, release gate, verifier scripts, PowerShell-команды или внутренние baseline-формулировки.

### Критерии приёмки

- [x] Hero и badges центрированы как в reference README.
- [x] README содержит SDL3-CS-style emoji accents: star callout и emoji-prefixed section headings со стабильными anchors.
- [x] Tagline отображается ровно один раз: он встроен в SVG и не повторяется отдельным `<h3>`.
- [x] Оставлен только один version badge; status badge, task badges и внутренние task IDs отсутствуют.
- [x] Отсутствует отдельный раздел Status.
- [x] В README отсутствуют `C#-first`, `baseline`, `release gate`, `TASKS.md`, `PowerShell`, `.ps1`, `pwsh` и Roadmap.
- [x] About объясняет продукт двумя конкретными предложениями для внешнего читателя и не использует agent-only формулировки.
- [x] Features описывают пользовательские возможности, а не названия типов или внутренних подсистем.
- [x] Первым пунктом Features стоит `Agent-native workflow` с понятным объяснением editor/shared project model, diagnostics и undo history.
- [x] Features содержит `Trello-style task board` для совместной работы через колонки, карточки, assignees, labels, review states и editor-visible project context.
- [x] Platforms оформлен как таблица `Platform / Editor / Runtime` с emoji-состояниями `✅ Done`, `🕓 Planned`, `❌ Not planned`: Windows, Linux и macOS имеют Editor и Runtime `Done`, Android имеет Runtime `Done` и Editor `Not planned`, iOS и Web имеют Runtime `Planned` и Editor `Not planned`.
- [x] Quick Start запускает Editor обычными `dotnet` командами, а не repository verifier.
- [x] Documentation содержит только ссылку на `https://github.com/edwardgushchin/Electron2D/wiki`.
- [x] Examples содержит только `Platformer`.
- [ ] Название `Platformer` ведёт на GitHub-каталог `https://github.com/edwardgushchin/Electron2D/tree/main/examples/platformer` после `T-0212`.
- [x] Feedback содержит реальные ссылки на GitHub Issues и Pull Requests.
- [x] Contributors содержит ссылку на contributors graph.
- [x] README полностью на английском языке.
- [ ] Rendered GitHub preview или локальный preview artifact проверен визуально.
- [ ] C# README verifier из `T-0213` выполнен и подтвердил этот README contract.

### Подзадачи

- [x] Переписать `docs/documentation/repository-readme.md` под публичный README contract.
- [x] Переписать `README.md` в продуктовой структуре.
- [ ] Выполнить C# README verifier, реализованный в `T-0213`, и подтвердить успешный результат.
- [ ] Визуально проверить rendered README preview.

### Заметки агента

Создано по запросу пользователя: итоговый README должен выглядеть как сильные reference-проекты и взять из них лучшие идеи, включая блоки feedback/contributions, contributors и license.

2026-06-22T10:29:30+03:00 - Блокер пользователя: выполнять эту задачу только в самом конце 0.1.0 Preview, после завершения runtime, editor, export, documentation и release-ready checks.

2026-06-23T16:41:00+03:00 - Повторная проверка по запросу пользователя: `T-0110` всё ещё зависит от незакрытой `T-0104`, поэтому не готова к выполнению без изменения dependency graph.

2026-06-24T13:55:00+03:00 - Rejected 2026-06-24T14:32: README выглядел как внутренний отчёт агента, использовал task IDs, release gate, baseline, PowerShell verifier commands и UI-heavy example. Все `[x]` сняты, состояние возвращено в `open`, новый контракт требует C# verifier и удаления UI-heavy references.

2026-06-24T14:52:00+03:00 - Контракт уточнён после повторного review: дублированный `<h3>` запрещён, tagline должен отображаться ровно один раз, iOS/WebAssembly удалены из README Platforms, Quick Start добавлен отдельным разделом. `T-0110` больше не зависит от удаления UI-heavy project; для принятия README остаются C# verifier, визуальный preview и ссылка на `examples/platformer` после `T-0212`.

2026-06-24T15:05:00+03:00 - Контракт уточнён: `data/templates/electron2d-empty` удалён из публичных examples и остаётся внутренним шаблоном Project Manager. `T-0110` зависит от `T-0212` и `T-0213`, а не от всей tracking-задачи `T-0208`.

2026-06-24T15:43:00+03:00 - README Features дополнен публичной фичей `Trello-style task board`: доска задач для совместной работы через колонки, карточки, assignees, labels, review states и editor-visible project context.

2026-06-24T15:50:00+03:00 - README Platforms уточнён: Linux и macOS считаются Editor and runtime targets, Android остаётся runtime target, iOS добавлена как future runtime target без включения в mandatory `0.1.0 Preview` gate.

2026-06-24T15:55:00+03:00 - README Examples уточнён: видимое имя примера `Platformer` ведёт на текущий GitHub-каталог `examples/reference-platformer`; старое имя не показывается в тексте. `T-0110` больше не зависит от `T-0212`, потому что физическое переименование каталога остаётся отдельной задачей.

2026-06-24T15:59:00+03:00 - Решение пользователя уточнено: `reference-platformer` нельзя оставлять даже в URL README. Ссылка `Platformer` теперь указывает на будущий GitHub-каталог `examples/platformer`, а `T-0110` снова зависит от `T-0212`.

2026-06-24T15:53:00+03:00 - README дополнен SDL3-CS-style emoji accents: добавлена просьба `⭐ Star us on GitHub - it motivates us a lot!`, публичные section headings получили emoji, а навигация сохранена через явные HTML anchors.

2026-06-24T15:57:00+03:00 - README Platforms заменён на таблицу `Platform / Editor / Runtime` с emoji-состояниями `✅ Done`, `🕓 Planned`, `❌ Not planned`: Linux и macOS отмечены как Editor+Runtime, Android как Runtime-only, iOS и Web как planned Runtime-only. Отдельная строка Legend не используется.

2026-06-24T18:09:00+03:00 - README Features уточнён: `Cross-platform runtime` теперь прямо указывает, что iOS и Web являются future runtime targets; таблица Platforms уже содержит Web как planned Runtime-only.

2026-06-24T18:16:00+03:00 - Выполнен commit `44f42cb5 docs: update future runtime targets` и push в `origin/main`: README и доменный контракт теперь синхронно указывают iOS и Web как planned future runtime targets.

2026-06-24T22:12:00+03:00 - После отказа пользователя принять `T-0224` README возвращён к действующему контракту `T-0110`: таблица Platforms снова имеет только столбцы `Platform / Editor / Runtime`, статусы `✅ Done`, `🕓 Planned`, `❌ Not planned`, iOS и Web остаются запланированными платформами запуска. Релизная проверка всех шести платформ фиксируется в `docs/releases/0.1.0-preview.md` и не выносится отдельным столбцом в публичный README.

2026-06-24T22:36:00+03:00 - После повторного отказа принять `T-0224` README Examples синхронизирован с текущим статусом Platformer: описание больше не называет пример завершённой игрой и теперь использует формулировку `A 2D platformer example built with Electron2D.`. Это сохраняет контракт `T-0110` и не называет Platformer завершённой приёмочной игрой до `T-0222`, `T-0223` и `T-0225`.

2026-06-24T22:49:00+03:00 - После повторного отказа принять `T-0224` в доменном документе README убрана нормативная коллизия: iOS и Web больше не описаны как исключённые из обязательной релизной проверки `0.1.0 Preview`. Документ теперь разделяет публичный README-статус и релизный набор `releaseVerificationTargets`, который задаётся в `docs/releases/0.1.0-preview.md`.

## T-0111 [ ] P0: Подготовить cross-platform release packaging и draft GitHub Release workflow

- Создана: 2026-06-20T22:13:00+03:00
- Состояние: blocked
- Приоритет: P0
- Зависимости: T-0209, T-0210, T-0110
- Ссылки:
  - Upstream commit: нет
  - Спецификация: `docs/release-management/release-packaging.md`; `docs/releases/0.1.0-preview.md`; `docs/release-management/`
  - Документация: `docs/release-management/release-packaging.md`; `docs/release-management/ci-matrix.md`
  - Будущий исходный код: `.github/workflows/release.yml`; `eng/Electron2D.Build/`
  - Будущий verifier: `eng/Electron2D.Build -- release verify`

### Самодостаточное описание

Задача относится к домену «Release engineering». Нужно подготовить cross-platform packaging и controlled GitHub Release draft workflow для `0.1.0 Preview`, но не называть репозиторий production-ready до прохождения настоящего release gate `T-0104`.

Реализация должна использовать единый C# repository tool `eng/Electron2D.Build`, а не PowerShell scripts. GitHub Release publication запрещена без отдельной явной команды пользователя; автоматизация по умолчанию должна поддерживать safe rehearsal/draft-only режим.

### Критерии приёмки

- [ ] Создана спецификация cross-platform release packaging без PowerShell-контракта.
- [ ] Artifact matrix явно перечисляет OS/architecture targets: Windows, Linux, macOS и поддерживаемые архитектуры для каждого desktop artifact.
- [ ] C# repository tool умеет собирать release artifacts для всех поддерживаемых desktop OS/architecture targets.
- [ ] Release artifacts включают библиотеку, редактор и developer tools, если они реализованы к моменту release candidate.
- [ ] Каждый artifact имеет предсказуемое имя с версией, OS, architecture и checksum.
- [ ] GitHub Actions использует C# repository tool и может подготовить GitHub Release draft для tag/version только в режиме, который не публикует релиз без отдельной явной команды пользователя.
- [ ] Добавлен C# dry-run/rehearsal verifier, который проверяет структуру артефактов без публикации релиза.
- [ ] Документация release process описывает prerequisites, tag policy, rollback, artifact matrix и ручные проверки.
- [ ] В tracked repository нет новых PowerShell release scripts или `pwsh` workflow steps для этой задачи.
- [ ] Команда локальной проверки или CI dry-run задокументирована как `dotnet run --project eng/Electron2D.Build -- release verify`.

### Подзадачи

- [ ] Определить release artifact matrix для Windows/Linux/macOS и архитектур.
- [ ] Добавить packaging commands в `eng/Electron2D.Build`.
- [ ] Добавить GitHub Actions workflow для release build и controlled GitHub Release publication, где публикация требует отдельного ручного разрешения.
- [ ] Добавить checksum generation и artifact manifest.
- [ ] Добавить release dry-run verifier на C#.
- [ ] Обновить README, release documentation и local release drafts после C# tooling.

### Заметки агента

Создано по запросу пользователя: финальный релиз должен собираться в GitHub Actions, упаковываться под поддерживаемые desktop платформы/архитектуры и готовиться как GitHub Release под версию. Важное ограничение пользователя от 2026-06-20: не публиковать GitHub Release без отдельной явной команды.

2026-06-22T10:29:30+03:00 - Блокер пользователя: выполнять эту задачу только в самом конце 0.1.0 Preview, после завершения runtime, editor, export, documentation и release-ready checks.

2026-06-23T16:41:00+03:00 - Повторная проверка по запросу пользователя: `T-0111` всё ещё зависит от незакрытых `T-0104` и `T-0110`, поэтому не готова к выполнению без изменения dependency graph.

2026-06-24T13:55:00+03:00 - Rejected 2026-06-24T14:32: PowerShell-based packaging/verifier/workflow не принимаются. Состояние возвращено в `blocked`, все `[x]` сняты, зависимость перенесена на C# automation tasks `T-0209` и `T-0210`.

## T-0172 [ ] P0: Отслеживать runtime UI gaps для дальнейшего видимого UI редактора

- Создана: 2026-06-24T01:21:00+03:00
- Состояние: tracking
- Приоритет: P0
- Зависимости: T-0171
- Дочерние задачи: T-0176, T-0177, T-0178, T-0179
- Ссылки:
  - Доменный документ: `docs/ui/basic-controls.md`; `docs/ui/control-layout-core.md`; `docs/ui/containers.md`; `docs/editor/editor-project-shell.md`
  - Исходный код: `src/Electron2D/Graphics/UI/Control.cs`; `src/Electron2D/Graphics/UI/Label.cs`; `src/Electron2D/Graphics/UI/Controls/Button.cs`; `src/Electron2D/Graphics/UI/Container.cs`; `src/Electron2D/Graphics/UI/BoxContainer.cs`; `src/Electron2D/Graphics/UI/Theme.cs`
  - Тесты: `tests/Electron2D.Tests.Integration/BasicControlInteractionTests.cs`; `tests/Electron2D.Tests.Integration/EditorProjectShellTests.cs`

### Самодостаточное описание

Уточнённый dogfooding-контракт разрешает `Electron2D.Editor` пользоваться internal host API, но видимый интерфейс редактора обязан проходить через общий runtime UI stack. `T-0171` принята без закрытия всех перечисленных gaps; эта tracking-задача держит общий контекст и считается завершённой только после приёмки дочерних задач `T-0176`-`T-0179`. Конкретное production-изменение нужно выполнять в дочерней задаче, а не в `T-0172`.

Блокирующие gaps:

- тема и шрифт по умолчанию, чтобы обычный `Label` и `Button` могли показывать текст без локального обхода `ShellFont` в `Electron2D.Editor`;
- render clipping для `Control.ClipContents`, а не только ограничение GUI hit-testing;
- text wrapping, overrun и ellipsis для `Label`/текстовых controls;
- hover/focus/pressed/disabled visual states и redraw при смене hover;
- allocation-free layout hot path для `Container` и `BoxContainer` без LINQ/массивов каждый кадр;
- follow-up после первых пяти пунктов: `ScrollContainer`, popup/modal focus, keyboard navigation, DPI scaling и split containers.

### Критерии приёмки

- [ ] `T-0176` принята пользователем.
- [ ] `T-0177` принята пользователем.
- [ ] `T-0178` принята пользователем.
- [ ] `T-0179` принята пользователем.
- [ ] Ни одна задача видимого UI редактора не обходит отсутствующие runtime UI возможности editor-only renderer, hit-test, focus, clipping или input routing.

### Подзадачи

- [ ] Держать `T-0176`-`T-0179` в актуальном порядке зависимостей.
- [ ] Не принимать задачи видимого UI редактора, если они обходят незакрытую runtime UI возможность.
- [ ] После приёмки дочерних задач обновить итоговый статус `T-0172`.

### Заметки агента

2026-06-24T01:21:00+03:00 - Задача заведена как follow-up после уточнения dogfooding-контракта: internal editor host допустим, но runtime UI gaps должны закрываться в общем UI stack, а не editor-only workaround.

2026-06-24T02:06:00+03:00 - После приёмки `T-0171` формулировка уточнена: `T-0172` не блокирует принятую `T-0171`, а блокирует последующие функции видимого UI редактора, которые нуждаются в перечисленных runtime UI возможностях.

2026-06-24T02:20:00+03:00 - По диагнозу пользователя `T-0172` оставлена как umbrella-задача по основе runtime UI. Исполнение разбито на gate-задачи `T-0176`-`T-0179`: тема/шрифты/иконки, текст/clipping/scrolling/DPI, interaction states/focus и настольные элементы управления. Задачи уровня редактора `T-0180`-`T-0183` зависят от этих gates и не должны заменять их editor-only drawing или отдельным hit-test.

2026-06-24T03:20:00+03:00 - Задача переведена в состояние `tracking`: production scope перенесён в `T-0176`-`T-0179`, чтобы первая дочерняя задача не зависела от закрытия родительского эпика.

## T-0174 [ ] P1: Усилить завершение и диагностику window smoke при timeout

- Создана: 2026-06-24T01:58:00+03:00
- Состояние: in progress
- Приоритет: P1
- Зависимости: T-0171
- Ссылки:
  - Доменный документ: `docs/editor/editor-project-shell.md`
  - Исходный код: `src/Electron2D.Editor/Program.cs`; `src/Electron2D.Editor/Shell/WindowSmoke.cs`; `src/Electron2D.Editor/Shell/WindowHost.cs`
  - Тесты: `tests/Electron2D.Tests.Integration/EditorProjectShellTests.cs`

### Самодостаточное описание

Во время проверки `T-0171` первый xUnit-запуск window smoke один раз завершился timeout, хотя прямой запуск `--window-smoke .temp\editor-window-runtime-ui-debug` и повторный focused xUnit run прошли. Это не блокирует принятую `T-0171`, но показывает пробел в надёжности smoke harness: при timeout тест обязан гарантированно завершать дочерний процесс редактора, сохранять доступные stdout/stderr и diagnostic artifacts, а также ясно записывать причину остановки.

Нужное поведение: window smoke не должен оставлять зависшие дочерние процессы и не должен терять диагностику, если окно или раннер зависли до штатного завершения. При срабатывании timeout тест должен завершать дерево дочерних процессов, сохранить последние доступные логи, пути к screenshot/analysis artifacts и отдельную timeout-запись, чтобы следующий агент мог понять, где остановился smoke.

### Критерии приёмки

- [ ] `docs/editor/editor-project-shell.md` описывает timeout-поведение window smoke и ожидаемые diagnostic artifacts.
- [ ] Добавлен focused failing test или контролируемая проверка harness timeout path.
- [ ] При timeout завершается дочерний процесс редактора и его потомки, без orphan-процессов.
- [ ] stdout/stderr, exit reason, elapsed time и пути к последним artifacts сохраняются в smoke diagnostics.
- [ ] Повторный focused window smoke проходит стабильно без зависших процессов.
- [ ] `dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorWindowSmokeRunCreatesRealWindowAndWritesVisualArtifacts" -v q` проходит.
- [ ] `dotnet run --project eng/Electron2D.Build -- verify licenses` проходит.

### Подзадачи

- [ ] Специфицировать timeout diagnostics в доменном документе.
- [ ] Добавить тест или harness hook для воспроизводимого timeout path.
- [ ] Реализовать гарантированное завершение process tree.
- [ ] Сохранить stdout/stderr и artifact paths при timeout.
- [ ] Запустить focused smoke checks и license verifier.

### Заметки агента

2026-06-24T01:58:00+03:00 - Задача заведена по итогам приёмки `T-0171`: первый xUnit timeout фиксируется как отдельный reliability follow-up, а не как причина переоткрывать runtime UI dogfooding.

2026-06-25T20:16:00+03:00 - Статус возвращён в `open` перед началом `T-0220`, потому что текущая сессия не работает над window smoke, а в `TASKS.md` должна быть только одна активная задача `in progress`.

## T-0175 [ ] P0: Устранить конфликт файлового контракта Project Settings и canonical `.e2d`

- Создана: 2026-06-24T02:20:00+03:00
- Состояние: open
- Приоритет: P0
- Зависимости: нет
- Ссылки:
  - Доменный документ: `docs/editor/project-manager.md`; `docs/editor/project-settings-ui.md`; `docs/project-system/project-text-formats.md`; `docs/export/export-preset-model.md`
  - Исходный код: `src/Electron2D.Editor/`; `src/Electron2D.ProjectSystem/`; `src/Electron2D.Export/`; `data/templates/electron2d-empty/`
  - Тесты: `tests/Electron2D.Tests.Integration/EditorProjectManagerTests.cs`; `tests/Electron2D.Tests.Integration/EditorProjectSettingsUiTests.cs`

### Самодостаточное описание

Перед привязкой `Project Settings` к живым элементам управления нужно убрать расхождение в файловом контракте. `Project Manager` уже определяет canonical manifest как named `<ProjectName>.e2d`, допускает embedded `exportPresets` и считает `project.e2d.json` legacy fallback. При этом `docs/editor/project-settings-ui.md` и smoke-контракт всё ещё требуют запись `project.e2d.json` и отдельного `export_presets.e2export.json`.

Нужное поведение: Project Settings UI, project template, Project Manager, export preset model и smoke-тесты должны редактировать один canonical project manifest `<ProjectName>.e2d`. Раздел `exportPresets` должен жить внутри `.e2d` для новых проектов и reference games. `project.e2d.json` и отдельный `export_presets.e2export.json` можно читать только как legacy fallback или миграционный вход, если рядом нет named `.e2d`.

### Критерии приёмки

- [ ] Доменные документы `project-manager`, `project-settings-ui`, `project-text-formats` и `export-preset-model` описывают один файловый контракт без противоречий.
- [ ] Добавлены сначала падающие focused tests, которые фиксируют, что Project Settings сохраняет изменения в `<ProjectName>.e2d` и embedded `exportPresets`, а не в `project.e2d.json` и отдельный `export_presets.e2export.json`.
- [ ] Production code Project Settings smoke и related stores пишут canonical `.e2d`, затем перечитывают тот же файл и подтверждают round-trip.
- [ ] Legacy `project.e2d.json` остаётся read fallback только при отсутствии named `.e2d`; новые template/reference projects не создают legacy project manifest.
- [ ] Если рядом есть named `<ProjectName>.e2d` и legacy `project.e2d.json`, canonical named `.e2d` имеет приоритет, а legacy-файл не перезаписывается без явной миграции.
- [ ] Секреты signing/export не попадают в `.e2d`: допускаются только non-secret references вроде `env:...`.
- [ ] Focused Project Manager и Project Settings tests проходят.
- [ ] `dotnet run --project eng/Electron2D.Build -- verify licenses` и `dotnet run --project eng/Electron2D.Build -- verify docs` проходят.

### Подзадачи

- [ ] Согласовать expected contract в доменных документах до изменения кода.
- [ ] Добавить tests на canonical `.e2d` write/read и legacy fallback.
- [ ] Перевести Project Settings smoke на embedded `exportPresets`.
- [ ] Обновить template/reference проверки и export preset documentation.
- [ ] Зафиксировать фактическое поведение и команды проверки в тех же документах.

### Заметки агента

2026-06-24T02:20:00+03:00 - Задача создана по диагнозу пользователя: до живого Project Settings UI нельзя продолжать работу, пока Project Manager и Project Settings UI описывают разные project files.

2026-06-24T03:20:00+03:00 - Зависимости от `T-0167`-`T-0169` сняты: файловый контракт Project Settings не зависит от file association или pixel-format исправлений. Добавлен precedence rule для ситуации, где named `.e2d` и legacy `project.e2d.json` существуют одновременно.

## T-0176 [ ] P0: Основа UI A - тема по умолчанию, шрифты и иконки

- Создана: 2026-06-24T02:20:00+03:00
- Состояние: open
- Приоритет: P0
- Зависимости: T-0171
- Ссылки:
  - Доменный документ: `docs/ui/theme-tooltips.md`; `docs/resources/font-import.md`; `docs/resources/texture-image-import.md`; `docs/editor/editor-project-shell.md`
  - Исходный код: `src/Electron2D/Graphics/UI/Theme.cs`; `src/Electron2D/Graphics/UI/Control.cs`; `src/Electron2D/Graphics/UI/Label.cs`; `src/Electron2D/Graphics/UI/Controls/Button.cs`; `src/Electron2D.Editor/`
  - Тесты: `tests/Electron2D.Tests.Integration/FontImportTests.cs`; `tests/Electron2D.Tests.Integration/BasicControlInteractionTests.cs`; `tests/Electron2D.Tests.Integration/EditorProjectShellTests.cs`

### Самодостаточное описание

Цель - закрыть первый gate основы UI: базовые runtime controls и будущие desktop controls должны получать тему, шрифт и icon lookup через общий механизм, без локальной настройки в `Electron2D.Editor`. Текущий локальный `ShellFont` в `src/Electron2D.Editor/Application.cs` является обходом отсутствующего API шрифта по умолчанию и должен исчезнуть после появления общей фабрики темы и шрифта по умолчанию.

Нужное поведение: runtime имеет глобальную тему по умолчанию, anti-aliased UI font, отдельный monospace font для code/terminal, state style resources, базовый icon resource pipeline, glyph/icon caching и fallback-правила, которые работают для игр, отладочных панелей и редактора. Зелёный/красный цвет остаётся семантическим для run/stop actions, а не постоянным фоном больших панелей.

### Критерии приёмки

- [ ] Доменный документ обновлён до кода: тема и шрифт по умолчанию, monospace font scope, icons, style resources, cache behavior, font asset licensing и fallback glyph rules описаны как runtime UI contract.
- [ ] Добавлены сначала падающие tests для поиска темы/шрифта по умолчанию, anti-aliased text availability, icon lookup, state style resources, кириллицы, fallback glyphs и отсутствия editor-local font workaround.
- [ ] Runtime поставляет воспроизводимый UI font asset с документированной лицензией; текст на латинице и кириллице не зависит от случайно установленного системного шрифта.
- [ ] Fallback glyph lookup показывает отсутствующие символы предсказуемо и не ломает layout при русском UI.
- [ ] `Electron2D.Editor` больше не создаёт локальный `ShellFont` для стартового shell: текст видимых controls берётся через runtime theme/font path.
- [ ] `Button`, `Label`, базовые test controls и editor shell получают применимые normal/hover/pressed/focused/disabled/selected resources из общей темы; конкретные desktop controls подтверждают это в `T-0194`-`T-0197`.
- [ ] Icon resource pipeline поддерживает 16 px toolbar icons, severity icons и node/resource/script type icons без editor-only drawing.
- [ ] Glyph/icon caching не пересоздаёт ресурсы при неизменном UI.
- [ ] Focused UI/theme tests, editor smoke, local documentation verifier и source license verifier проходят.

### Подзадачи

- [ ] Специфицировать фабрику темы/шрифта по умолчанию и fallback lookup.
- [ ] Добавить minimal UI font, monospace font boundary, лицензию font asset и проверки латиницы/кириллицы.
- [ ] Реализовать state style resources и icon lookup/cache.
- [ ] Убрать editor-local font workaround из shell path.
- [ ] Проверить editor real-window smoke на читаемость текста.

### Заметки агента

2026-06-24T02:20:00+03:00 - Задача выделена из `T-0172` как первый обязательный gate перед дальнейшим видимым Editor UI.

## T-0177 [ ] P0: Основа UI B - текст, clipping, scrolling и DPI

- Создана: 2026-06-24T02:20:00+03:00
- Состояние: blocked
- Приоритет: P0
- Зависимости: T-0176
- Ссылки:
  - Доменный документ: `docs/ui/control-layout-core.md`; `docs/ui/containers.md`; `docs/ui/basic-controls.md`; `docs/rendering/text-backend-baseline.md`; `docs/settings-localization/unicode-ime-rtl-text.md`
  - Исходный код: `src/Electron2D/Graphics/UI/`; `src/Electron2D/Rendering/`; `src/Electron2D/Resources/`
  - Тесты: `tests/Electron2D.Tests.Integration/ControlLayoutCoreTests.cs`; `tests/Electron2D.Tests.Integration/ContainerLayoutTests.cs`

### Самодостаточное описание

Цель - сделать runtime UI пригодным для длинных путей, русских labels, Inspector-строк, деревьев и прокручиваемых панелей на 100-200% DPI. Сейчас `ClipContents` ограничивает hit-test, но полноценный render clipping/scissor и text overrun behavior остаются недостающим foundation.

Нужное поведение: text clipping, wrapping, ellipsis, корректные minimum sizes, child clipping, scissor regions, scroll offsets, wheel input, programmatic ensure-visible, DPI-aware metrics и layout/repaint invalidation работают в общей runtime UI системе `SceneTree`/`Control`, а не в отдельной реализации редактора. Визуальный `ScrollBar` как отдельный desktop control закрывается позже в `T-0179`.

### Критерии приёмки

- [ ] Доменные документы обновлены до кода: text overrun, wrapping, ellipsis, render clipping, scroll behavior и DPI scale описаны в одном месте с ограничениями.
- [ ] Добавлены сначала падающие tests для render clipping, ellipsis, wrapping, long project paths, русских labels, minimum size и DPI scale 100%, 125%, 150%, 200%.
- [ ] `Control.ClipContents` ограничивает не только pointer hit-test, но и фактическую отрисовку потомков через renderer/backend scissor.
- [ ] `ScrollContainer` поддерживает scroll offsets, wheel input и programmatic ensure-visible; text overflow не ломает layout Scene Tree, FileSystem и Inspector.
- [ ] Layout/repaint invalidation проверяется счётчиками: после initial layout за 120 неизменённых кадров не происходит новых measure/layout passes и не растут связанные allocation counters.
- [ ] Real-window visual artifacts проверяют 1280x720 и 1920x1080 без text overflow.
- [ ] Focused UI tests, source license verifier и local documentation verifier проходят.

### Подзадачи

- [ ] Специфицировать text overrun modes и clipping semantics.
- [ ] Реализовать renderer scissor для clipped controls.
- [ ] Подключить wrapping/ellipsis/minimum-size tests.
- [ ] Расширить scrolling и DPI checks.
- [ ] Проверить editor shell на длинных путях и русском тексте.

### Заметки агента

2026-06-24T02:20:00+03:00 - Задача выделена из пользовательского списка runtime foundation, чтобы не переносить overflow и DPI проблемы в каждый editor dock отдельно.

2026-06-24T03:49:00+03:00 - Scope уточнён: `T-0177` не требует визуальный `ScrollBar`, потому что этот control входит в настольные элементы управления `T-0179`. Здесь остаются scroll offsets, clipping, wheel input и ensure-visible.

## T-0178 [ ] P0: Основа UI C - состояния interaction, focus и pointer lifecycle

- Создана: 2026-06-24T02:20:00+03:00
- Состояние: blocked
- Приоритет: P0
- Зависимости: T-0177
- Ссылки:
  - Доменный документ: `docs/input/input-dispatch-ui-focus.md`; `docs/ui/basic-controls.md`; `docs/ui/structured-controls.md`; `docs/editor/editor-project-shell.md`
  - Исходный код: `src/Electron2D/Input/`; `src/Electron2D/Graphics/UI/`; `src/Electron2D.Editor/`
  - Тесты: `tests/Electron2D.Tests.Integration/BasicControlInteractionTests.cs`; `tests/Electron2D.Tests.Integration/InputDispatchControlTests.cs`

### Самодостаточное описание

Цель - закрыть общий lifecycle взаимодействия controls: hover, press, focus, disabled, selected state, pointer capture, keyboard traversal, popup/modal ownership, tooltip delay, drag-and-drop и shortcuts. Без этого editor docks вынуждены снова изобретать ручной hit-test и состояния поверх shell.

Нужное поведение: мышь, touch, клавиатура и shortcuts проходят через общий GUI routing runtime. Активный workspace влияет на shortcut dispatch, но не ломает focus ownership. Popup/modal ownership забирает и возвращает focus предсказуемо. Drag-and-drop имеет единый generic lifecycle: drag source, payload, target enter/leave, preview, drop, cancel и cleanup. Конкретные controls `Tree`, `ItemList`, `TabContainer`, `PopupMenu`, `LineEdit`, `ScrollContainer` и editor-операции Scene Tree, FileSystem, Tasks board и viewport проверяются в `T-0194`-`T-0197` и editor-задачах.

### Критерии приёмки

- [ ] Доменные документы описывают mouse enter/leave, pointer capture, double click, wheel, cursor shapes, focus traversal, keyboard activation, popup/modal focus ownership, tooltip delay и drag-and-drop lifecycle.
- [ ] Добавлены сначала падающие tests на hover redraw, pressed/captured pointer, disabled base controls, selected state, Tab/Shift+Tab traversal, popup/modal focus ownership, generic drag lifecycle и workspace-aware shortcuts.
- [ ] Foundation tests используют базовые test controls и минимальные popup/drag fixtures; готовность `Tree`, `ItemList`, `TabContainer`, `PopupMenu`, `LineEdit` и `ScrollContainer` к общему state/focus path подтверждается в `T-0194`-`T-0197`.
- [ ] Focus ring и selected state видны через theme resources и не меняют layout размеров.
- [ ] Generic focus traversal, keyboard activation и shortcut routing проверены на базовых controls и не зависят от конкретного editor dock или будущих desktop controls.
- [ ] Generic drag-and-drop lifecycle поддерживает source, payload, target enter/leave, preview, drop, cancel и cleanup без editor-only input path.
- [ ] Focused input/UI tests, real-window interaction smoke и source license verifier проходят.

### Подзадачи

- [ ] Специфицировать state machine для pointer/focus/keyboard.
- [ ] Добавить tests для hover/press/focus/disabled/selected transitions.
- [ ] Реализовать popup/modal focus ownership и tooltip delay.
- [ ] Подключить shortcut dispatch с учётом active workspace.
- [ ] Добавить общий drag-and-drop lifecycle.

### Заметки агента

2026-06-24T02:20:00+03:00 - Задача отделяет поведение controls от визуальной переработки shell: новые editor panels не должны реализовывать собственный focus или pointer routing.

2026-06-24T03:20:00+03:00 - При реализации foundation-часть нужно проверять на generic controls. Editor-specific keyboard workflows для Scene Tree, FileSystem и Inspector должны закрываться в `T-0183`/`T-0185`, чтобы эта задача не разрослась до полноценного shell workflow.

2026-06-24T03:49:00+03:00 - Критерии приведены к этой границе: `T-0178` больше не требует reparent/reorder, resource-to-scene placement или movement task cards. Она даёт только общий drag/focus/shortcut lifecycle.

2026-06-24T13:36:00+03:00 - Убрана инверсия зависимости с `T-0194`-`T-0197`: foundation проверяет механизмы на базовых test controls, а конкретные desktop controls доказывают использование общего state/focus path в своих дочерних задачах.

## T-0179 [ ] P0: Основа UI D - настольные элементы управления для Editor

- Создана: 2026-06-24T02:20:00+03:00
- Состояние: tracking
- Приоритет: P0
- Зависимости: T-0178
- Дочерние задачи: T-0194, T-0195, T-0196, T-0197
- Ссылки:
  - Доменный документ: `docs/ui/basic-controls.md`; `docs/ui/structured-controls.md`; `docs/ui/containers.md`; `docs/editor/editor-shell-layout.md`
  - Исходный код: `src/Electron2D/Graphics/UI/`; `src/Electron2D.Editor/`
  - Тесты: `tests/Electron2D.Tests.Integration/StructuredControlInteractionTests.cs`; `tests/Electron2D.Tests.Integration/BasicControlInteractionTests.cs`; `tests/Electron2D.Tests.Integration/ContainerLayoutTests.cs`; `tests/Electron2D.Tests.Integration/EditorProjectShellTests.cs`; `tests/Electron2D.Tests.Integration/EditorScriptWorkspaceTests.cs`

### Самодостаточное описание

Цель tracking-задачи - собрать набор runtime controls, на котором можно строить настоящий Editor: menu bar, popup menu, toolbar buttons, checkboxes, line edits, spin boxes, option buttons, tabs, trees, lists, scroll containers, split containers, text/code editors, separators, tooltips и progress indicators. Исполняемая работа разделена на `T-0194`-`T-0197`; эту задачу нельзя брать как один production scope.

Нужное поведение: перечисленные controls имеют layout, rendering, input, focus, keyboard navigation, clipping, states и theme lookup на уровне runtime. Editor-specific `DockHost`, property editors и timeline могут быть internal composite controls, но не должны обходить общий runtime UI stack.

### Критерии приёмки

- [ ] `T-0194` принята пользователем.
- [ ] `T-0195` принята пользователем.
- [ ] `T-0196` принята пользователем.
- [ ] `T-0197` принята пользователем.
- [ ] Общий manifest настольных controls в доменных документах согласован с фактически принятыми дочерними задачами.

### Подзадачи

- [ ] Вести зависимости и scope дочерних задач.
- [ ] После приёмки дочерних задач обновить итоговый manifest и статус `T-0179`.

### Заметки агента

2026-06-24T02:20:00+03:00 - Задача завершает runtime UI foundation перед editor-level задачами. До её закрытия визуальная полировка Script, Tasks, Project Settings и specialized editors не должна расширяться.

2026-06-24T03:20:00+03:00 - Scope остаётся крупным milestone control-library. Перед implementation рекомендуется разбить его на menus/toolbars/tabs, form controls, tree/list/scroll/split и text/code editing; `CodeEdit` можно выделить ближе к `T-0184`.

2026-06-24T03:49:00+03:00 - Задача переведена в `tracking`; дочерние production-задачи: `T-0194` меню/toolbars/tabs, `T-0195` form controls, `T-0196` tree/list/scroll/split, `T-0197` text/code editing и индикаторы.

## T-0180 [ ] P0: Создать Editor Design System и `EditorTheme`

- Создана: 2026-06-24T02:20:00+03:00
- Состояние: blocked
- Приоритет: P0
- Зависимости: T-0176, T-0177, T-0178, T-0179
- Ссылки:
  - Доменный документ: `docs/editor/godot4-editor-reference.md`; `docs/editor/editor-shell-layout.md`; `docs/ui/theme-tooltips.md`
  - Визуальный референс: `docs/editor/references/godot-4.7-2d-workspace.md`; PNG `docs/editor/references/godot-4.7-2d-workspace.png`, 1920 x 1032 px, SHA-256 `CE8C799A6AE23423957A904BCA1CD4D28922461DE235A9A5D398864BDE10A84F`.
  - Исходный код: `src/Electron2D.Editor/`; `src/Electron2D/Graphics/UI/Theme.cs`
  - Тесты: `tests/Electron2D.Tests.Integration/EditorProjectShellTests.cs`

### Самодостаточное описание

После runtime primitives нужен единый `EditorTheme`, а не набор случайных цветов и размеров. Design system задаёт визуальную дисциплину редактора: compact menu row, 13-14 px UI font, 12 px secondary text, 16 px icons, 28-30 px toolbar buttons, 30-32 px panel headers, 32 px document tabs, 34-36 px workspace toolbar, spacing grid 4/8 px, спокойные поверхности и semantic accent colors.

Нужное поведение: Editor использует три уровня поверхности - базовый shell, docks/panels и raised controls/popups. Большие цветовые пятна в docks заменяются тонкими separators. Uppercase убирается из обычных labels: `Scene`, `Inspector`, `FileSystem` вместо постоянного uppercase. Текстовые прямоугольные кнопки в toolbar заменяются icon/button controls с tooltip.

### Критерии приёмки

- [ ] Доменный документ фиксирует `EditorTheme`: metrics, colors, typography, icon sizes, state resources, panel surfaces, spacings и semantic accent rules.
- [ ] Добавлены tests/analysis, которые проверяют ключевые theme tokens и отсутствие hard-coded editor colors там, где должен использоваться `EditorTheme`.
- [ ] Menu, workspace switcher, run controls, docks, bottom panel, tabs и form controls получают оформление через `EditorTheme`.
- [ ] Верхний UI становится компактным: menu, workspace switcher и run controls помещаются в одну строку без крупных прямоугольных placeholders.
- [ ] Uppercase не используется для обычных labels; допускаются только редкие technical badges и micro-headings.
- [ ] Иконки используются для run/stop/add/remove/filter/search/node/resource/script/severity actions, с tooltip для неочевидных команд.
- [ ] Real-window screenshots 1280x720 и 1920x1080 подтверждают читаемость, отсутствие text overflow и визуальную иерархию поверхностей.

### Подзадачи

- [ ] Специфицировать theme tokens и metrics.
- [ ] Добавить `EditorTheme` и icon registry.
- [ ] Перевести shell controls на theme tokens.
- [ ] Убрать постоянный uppercase из labels.
- [ ] Проверить screenshots и JSON analysis.

### Заметки агента

2026-06-24T02:20:00+03:00 - Задача создана отдельно от foundation: runtime controls должны сначала уметь состояния, текст, DPI и icons, после чего editor-level theme задаёт дисциплину конкретного продукта.

## T-0181 [ ] P0: Каркас редактора - docks, команды, нижняя панель и сохранение раскладки

- Создана: 2026-06-24T02:20:00+03:00
- Состояние: blocked
- Приоритет: P0
- Зависимости: T-0179, T-0180
- Ссылки:
  - Доменный документ: `docs/editor/editor-shell-layout.md`; `docs/editor/editor-project-shell.md`; `docs/editor/agent-workspace-panel.md`
  - Исходный код: `src/Electron2D.Editor/`; `src/Electron2D/Graphics/UI/`
  - Тесты: `tests/Electron2D.Tests.Integration/EditorProjectShellTests.cs`; `tests/Electron2D.Tests.Integration/EditorAgentWorkspacePanelTests.cs`

### Самодостаточное описание

Цель - сделать каркас редактора поверх runtime controls: dock groups, splitters, tab groups, bottom panel, command routing, shortcut context и layout save/restore. Это не визуальный smoke-screen, а рабочий механизм, который должен использовать Scene Tree, FileSystem, Inspector, нижнюю вкладку `Agent`, Script, Tasks и specialized editors.

Нужное поведение: docks можно изменять по размеру, скрывать, переносить, максимизировать и восстанавливать. Bottom panel по умолчанию занимает collapsed strip около 30-32 px, раскрывается до 180-240 px, имеет tabs, badges, close/maximize controls и status area справа. Framework даёт общий механизм `RevealBottomPanel(tab, stealFocus: false)`, но не решает, когда раскрывать `Output`, `Debugger` или `Agent`: эти триггеры принадлежат workflow-задачам. Command routing знает active workspace и не теряет shortcuts при переходах.

### Критерии приёмки

- [ ] Доменный документ описывает `DockHost`, dock groups, splitters, bottom panel, command routing, persistence schema и shortcut context.
- [ ] Добавлены сначала падающие tests для resize docks, tab movement, bottom collapsed/expanded state, splitter hit targets, command routing и layout round-trip.
- [ ] Dock layout сохраняет left/right dock width, bottom height, collapsed state, selected bottom tabs, active Agent inner tab, active workspace, open documents, selection, zoom и Agent Workspace state.
- [ ] Bottom panel не занимает большую пустую область по умолчанию, содержит вкладку `Agent` после `Debugger` и поддерживает общий API/command `RevealBottomPanel(tab, stealFocus: false)` для workflow-задач.
- [ ] `RevealBottomPanel(..., stealFocus: false)` раскрывает нужную вкладку без перехвата keyboard focus; конкретные auto-reveal triggers для `Output`, `Debugger` и `Agent` остаются в `T-0183`, `T-0200` и `T-0188`.
- [ ] При нехватке ширины bottom tabs уходят в overflow-menu, а не сжимаются до нечитаемого состояния.
- [ ] Status area показывает renderer, FPS, build/run state, SDK state и version/branch там, где данные доступны.
- [ ] Все видимые framework controls строятся из runtime controls, без manual shell renderer и без `ShellRegion` hit-test.
- [ ] Real-window interaction smoke подтверждает resize, collapse/expand, keyboard shortcuts и persistence after restart.

### Подзадачи

- [ ] Специфицировать dock/persistence/command contract.
- [ ] Добавить failing tests для dock layout и bottom panel.
- [ ] Реализовать `DockHost` как internal composite control поверх runtime UI.
- [ ] Подключить command routing и shortcut context.
- [ ] Проверить real-window resize/persistence screenshots.

### Заметки агента

2026-06-24T02:20:00+03:00 - Задача отделяет reusable editor framework от productized shell: сначала нужен механизм docks/commands, затем конкретная раскладка shell.

2026-06-24T13:36:00+03:00 - Scope уточнён: `T-0181` владеет механизмом раскрытия bottom panel и no-focus-steal, но не владеет моментом автоматического раскрытия `Output`, `Debugger` или `Agent`. Триггеры остаются в workflow-задачах.

## T-0182 [ ] P0: Довести shell до продукта - заменить placeholders реальными controls и read-only binding

- Создана: 2026-06-24T02:20:00+03:00
- Состояние: blocked
- Приоритет: P0
- Зависимости: T-0175, T-0180, T-0181
- Ссылки:
  - Доменный документ: `docs/editor/editor-shell-layout.md`; `docs/editor/editor-project-shell.md`; `docs/editor/project-manager.md`; `docs/editor/scene-tree-dock.md`; `docs/editor/file-system-dock.md`; `docs/editor/inspector.md`; `docs/editor/viewport-2d.md`; `docs/editor/run-output-workflow.md`
  - Визуальный референс: `docs/editor/references/godot-4.7-2d-workspace.md`; PNG `docs/editor/references/godot-4.7-2d-workspace.png`, 1920 x 1032 px, SHA-256 `CE8C799A6AE23423957A904BCA1CD4D28922461DE235A9A5D398864BDE10A84F`.
  - Исходный код: `src/Electron2D.Editor/`; `src/Electron2D/Graphics/UI/`
  - Тесты: `tests/Electron2D.Tests.Integration/EditorProjectShellTests.cs`

### Самодостаточное описание

Текущий shell выглядит как проверяемый каркас раскладки: крупные прямоугольные кнопки, смешанные document tabs, цветные docks с подписями, диагностический текст вместо viewport и пустой bottom panel. Эта задача заменяет placeholders на настоящую композицию shell из runtime controls и подключает read-only/model binding к уже существующим editor models.

Нужное поведение: верхняя строка содержит `Scene`, `Project`, `Debug`, `Editor`, `Help`, workspace switcher `[2D] [Script] [Game] [Tasks]` и run controls в компактной строке. Document tabs разделены по workspace: сцены в 2D, C# files в Script, selected task в Tasks. Левые и правые docks показывают реальные Scene/FileSystem/Inspector/Node panels в read-only режиме, а bottom panel содержит вкладку `Agent` как место процессного контекста. Настоящие изменения scene document, transform edit, save/run workflow остаются в `T-0183`; live jobs/actions/transport агента остаются в `T-0188`.

### Критерии приёмки

- [ ] Доменный документ обновлён до ожидаемого shell contract и после implementation отражает фактическое состояние.
- [ ] Добавлены сначала падающие real-window tests/analysis, которые фиксируют отсутствие manual shell renderer, placeholder labels и смешанных tabs.
- [ ] Top shell использует compact controls, icons/tooltips, separated document tabs и workspace-specific toolbar.
- [ ] Left docks показывают Scene Tree и FileSystem controls с search/filter, headers, tree/list rows, icons и read-only selection state; операции редактирования дерева и drag/drop остаются в `T-0183`, `T-0190` и `T-0191`.
- [ ] Right docks показывают только `Inspector` и `Node`, без `Agent Workspace` и без больших цветных пустых областей; property edit остаётся в `T-0183`.
- [ ] Bottom panel содержит вкладку `Agent`; открытая вкладка показывает заголовок `Agent Workspace` и read-only placeholder/model state без live job transport, без выполнения `Send Review`, `Undo AI`, `Cancel` или `Stop`.
- [ ] Автоматическое раскрытие `Agent`, live diagnostics/actions/jobs и agent transport не входят в эту задачу и остаются критериями `T-0188`.
- [ ] Bottom tabs имеют overflow-menu при нехватке ширины и не сжимаются до нечитаемого состояния.
- [ ] Central `2D` workspace показывает read-only viewport surface with toolbar, grid/world axes/zoom status вместо текста `ACTIVE WORKSPACE`; scene edit и transform handles остаются в `T-0183`.
- [ ] Bottom panel collapsed by default, имеет tabs, badges и status area; auto-open behavior для ошибок остаётся в workflow-задачах.
- [ ] Переключение workspaces сохраняет read-only shell state: active workspace, open document tabs, selected dock tab и layout placement.
- [ ] Real-window screenshots на 1280x720 и 1920x1080 подтверждают layout, states, pointer/keyboard checks и отсутствие `3D`, `AssetLib`, GDScript UI.

### Подзадачи

- [ ] Специфицировать productized shell layout и tab separation.
- [ ] Подключить shell к read-only model state: project, documents, selection snapshot, diagnostics summary, dock layout и command availability.
- [ ] Заменить placeholder panels runtime controls.
- [ ] Добавить viewport toolbar/grid placeholder, который уже является настоящим control layer.
- [ ] Проверить screenshots и analysis в real window.

### Заметки агента

2026-06-24T02:20:00+03:00 - Задача не должна превращаться в перекраску текущего shell: цель - удалить placeholders и связать shell с живым состоянием редактора.

## T-0183 [ ] P0: Вертикальный срез 2D-редактирования - открыть проект, изменить узел, сохранить и запустить

- Создана: 2026-06-24T02:20:00+03:00
- Состояние: blocked
- Приоритет: P0
- Зависимости: T-0175, T-0182
- Ссылки:
  - Доменный документ: `docs/editor/project-manager.md`; `docs/editor/scene-tree-dock.md`; `docs/editor/file-system-dock.md`; `docs/editor/viewport-2d.md`; `docs/editor/inspector.md`; `docs/editor/run-output-workflow.md`; `docs/editor/editor-shell-layout.md`
  - Визуальный референс: `docs/editor/references/godot-4.7-2d-workspace.md`; PNG `docs/editor/references/godot-4.7-2d-workspace.png`, 1920 x 1032 px, SHA-256 `CE8C799A6AE23423957A904BCA1CD4D28922461DE235A9A5D398864BDE10A84F`.
  - Исходный код: `src/Electron2D.Editor/`; `src/Electron2D.ProjectSystem/`; `src/Electron2D/`
  - Тесты: `tests/Electron2D.Tests.Integration/EditorSceneTreeDockTests.cs`; `tests/Electron2D.Tests.Integration/EditorFileSystemDockTests.cs`; `tests/Electron2D.Tests.Integration/EditorViewport2DTests.cs`; `tests/Electron2D.Tests.Integration/EditorInspectorTests.cs`; `tests/Electron2D.Tests.Integration/EditorRunWorkflowTests.cs`

### Самодостаточное описание

Первый принимаемый экран должен закрыть один рабочий цикл: открыть проект, выбрать узел, изменить transform, undo/redo, сохранить сцену, запустить текущую сцену, увидеть Output и остановить запуск. Пока этот вертикальный срез не завершён, Script, Tasks, specialized editors и Project Settings нельзя визуально полировать дальше: иначе они создадут собственные временные tabs, списки, property rows и input paths.

Нужное поведение workflow:

1. Пользователь открывает валидный `.e2d` проект.
2. Scene Tree выбирает `Player`.
3. `SelectionService` обновляет selection.
4. Viewport показывает outline/gizmo и grid.
5. Inspector показывает `Transform`.
6. Изменение `Position.X` обновляет scene document и viewport.
7. `Ctrl+Z`/`Ctrl+Y` синхронно откатывают/повторяют операцию.
8. Dirty marker появляется на scene tab.
9. `Ctrl+S` сохраняет scene runtime format.
10. `Run Scene` запускает сцену, Output показывает процесс, `Stop` завершает session.

### Критерии приёмки

- [ ] Доменные документы для Scene Tree, FileSystem, 2D Viewport, Inspector, Run/Output и shell обновлены до реализации и после неё отражают фактические проверки.
- [ ] Добавлен end-to-end сначала падающий test/smoke для full workflow open -> select -> edit transform -> undo/redo -> save -> run scene -> output -> stop.
- [ ] Scene Tree показывает настоящий root и child nodes, поддерживает selection и синхронизирует выбранный `Player`; context menu, inline rename и drag/reparent остаются follow-up после первого среза.
- [ ] FileSystem показывает реальные project files, search/filter и resource icons/status; drag resource to scene остаётся follow-up после первого среза.
- [ ] 2D viewport показывает grid, origin axes, scene content, selected object outline, базовые transform handles и zoom status; полный snapping/gizmo набор остаётся follow-up.
- [ ] Inspector показывает selected object header, search, collapsible `Transform` group, number editors, revert/reset icon и validation state.
- [ ] Selection синхронизирована между Scene Tree, Viewport и Inspector через общий service, а не через локальные snapshot copies.
- [ ] Undo/redo, dirty marker и save работают через общий undo/document service и сохраняют тот же scene format, который использует runtime.
- [ ] `Run Scene`/`Stop` управляют реальной run session, а Output автоматически раскрывается при запуске или ошибке.
- [ ] Docks resizable, layout persistence работает, а keyboard shortcuts покрывают `select/edit/save/run/stop`; полный keyboard-only workflow остаётся частью финального acceptance gate.
- [ ] Real-window screenshots и JSON analysis получены на Windows для 1280x720 и 1920x1080; Linux/macOS screenshots не входят в приёмку первого среза и остаются обязательными только для финального gate `T-0189`.
- [ ] `3D`, `AssetLib` и GDScript UI отсутствуют.

### Подзадачи

- [ ] Специфицировать end-to-end workflow и сервисы selection/document/undo/diagnostics/run/dock/command.
- [ ] Добавить failing full-workflow smoke.
- [ ] Подключить Scene Tree, Viewport и Inspector к одному selection/document state.
- [ ] Реализовать transform edit, dirty marker, save и undo/redo.
- [ ] Подключить Run Scene, Output auto-show и Stop.
- [ ] Выполнить real-window visual acceptance и записать artifacts.

### Заметки агента

2026-06-24T02:20:00+03:00 - Эта задача является главным следующим шагом после основы UI и productized shell: визуальное приближение к референсу должно стать следствием живого workflow, а не отдельной имитацией.

2026-06-24T03:20:00+03:00 - Scope сужен до первого исполнимого цикла `open -> select Player -> edit Position.X -> undo/redo -> save -> run -> stop`. Context menu, inline rename, drag/reparent, drag resource placement, полный gizmo/snapping набор и полный keyboard-only режим должны оформляться отдельными follow-up задачами после этого среза.

2026-06-24T13:14:00+03:00 - Cross-platform gate уточнён: `T-0183` проверяет первый вертикальный срез на Windows; обязательные Linux/macOS screenshots относятся к финальной приёмке `T-0189`, чтобы первый workflow не блокировался отсутствием дополнительных host-окружений.

## T-0184 [ ] P0: Подключить Script workspace к живому интерфейсу после 2D-среза

- Создана: 2026-06-24T02:20:00+03:00
- Состояние: tracking
- Приоритет: P0
- Зависимости: T-0183
- Дочерние задачи: T-0198, T-0199, T-0200, T-0201
- Ссылки:
  - Доменный документ: `docs/editor/script-workspace.md`; `docs/scripting/editor-script-workflow.md`; `docs/scripting/editor-language-services.md`; `docs/scripting/managed-debugger.md`
  - Исходный код: `src/Electron2D.Editor/`; `src/Electron2D.Tooling/`; `src/Electron2D.Scripting/`
  - Тесты: `tests/Electron2D.Tests.Integration/EditorScriptWorkspaceTests.cs`; `tests/Electron2D.Tests.Integration/EditorScriptLanguageServicesTests.cs`; `tests/Electron2D.Tests.Integration/EditorManagedDebuggerTests.cs`

### Самодостаточное описание

После живого 2D workflow нужно перевести Script workspace из snapshot/model-first состояния в постоянный UI на общих controls. Эта tracking-задача держит общий сценарий, а production scope разделён на code editor, language services UI, debugger UI и integration gate.

### Критерии приёмки

- [ ] `T-0198` принята пользователем.
- [ ] `T-0199` принята пользователем.
- [ ] `T-0200` принята пользователем.
- [ ] `T-0201` принята пользователем.
- [ ] Script workspace не создаёт отдельный tab/input framework за пределами общих controls.

### Подзадачи

- [ ] Вести зависимости code editor, language services UI и debugger UI.
- [ ] После приёмки дочерних задач обновить итоговый статус `T-0184`.

### Заметки агента

2026-06-24T02:20:00+03:00 - До `T-0183` эту задачу не начинать, чтобы Script workspace не закрепил временные tabs, lists и input paths.

2026-06-24T03:20:00+03:00 - Приоритет поднят до P0, потому что `T-0189` остаётся P0-gate и зависит от этой задачи. При реализации желательно разделить работу на code editor, language services UI и debugger UI либо оформить отдельные дочерние задачи.

2026-06-24T03:49:00+03:00 - Задача переведена в `tracking`; дочерние production-задачи: `T-0198` code editor, `T-0199` language services UI, `T-0200` debugger UI, `T-0201` integration gate.

## T-0185 [ ] P0: Подключить Tasks workspace к живому интерфейсу после 2D-среза

- Создана: 2026-06-24T02:20:00+03:00
- Состояние: blocked
- Приоритет: P0
- Зависимости: T-0183
- Ссылки:
  - Доменный документ: `docs/editor/project-tasks-board.md`; `docs/project-system/project-task-manager.md`
  - Исходный код: `src/Electron2D.Editor/`; `src/Electron2D.ProjectSystem/`
  - Тесты: `tests/Electron2D.Tests.Integration/EditorProjectTasksBoardTests.cs`; `tests/Electron2D.Tests.Integration/ProjectTaskManagerTests.cs`

### Самодостаточное описание

Tasks workspace должен стать полноценной ручной доской задач внутри Editor, а не snapshot поверх локального `TASKS.md`. Он должен использовать общие runtime controls, drag-and-drop lifecycle, Inspector details, command routing и trusted human acceptance actions.

Базовые переходы статусов:

| Из статуса | Допустимые действия |
| ---------- | ------------------- |
| `Backlog` | `Ready`, `Cancelled` |
| `Ready` | `In Progress`, `Blocked`, `Cancelled` |
| `In Progress` | `Review`, `Blocked`, `Cancelled` |
| `Blocked` | `Ready`, `In Progress`, `Cancelled` |
| `Review` | `Awaiting Acceptance`, `In Progress` через `Request changes`, `Cancelled` |
| `Awaiting Acceptance` | `Done` только ручным `Accept`, `In Progress` через `Request changes`, `Cancelled` |
| `Done` | `Archive` |
| `Cancelled` | `Archive`, `Ready` только через явное `Restore` |

### Критерии приёмки

- [ ] Доменный документ фиксирует live board contract, manual acceptance boundary и связь с `.electron2d/tasks/**`.
- [ ] Добавлены сначала падающие tests для board columns, filters, card selection, drag-and-drop, Inspector task details и trusted actions.
- [ ] Board показывает `Backlog`, `Ready`, `In Progress`, `Blocked`, `Review`, `Awaiting Acceptance`, `Done`, `Cancelled` без превращения Tasks в dock или bottom panel.
- [ ] Status transitions строго следуют таблице; недопустимый переход не меняет task document и показывает validation feedback.
- [ ] Сортировка карточек сохраняется через stable rank/order field, не зависит от текущего visual order после перезапуска и корректно обновляется при drag-and-drop.
- [ ] `Accept`, `Request changes`, `Cancel`, `Create`, `Edit`, `Archive` и `Hard delete` доступны как ручные Editor actions; AI не получает action `Done`.
- [ ] `Hard delete` требует явного confirmation dialog с именем или ID задачи и не выполняется одной случайной кнопкой.
- [ ] Real-window screenshot/analysis подтверждает keyboard/pointer workflow, absence of overflow и сохранение state при переключении workspaces.

### Подзадачи

- [ ] Специфицировать live Tasks board и trusted action boundary.
- [ ] Зафиксировать status transition table, rank persistence и hard-delete confirmation.
- [ ] Подключить task manager state к runtime controls.
- [ ] Реализовать drag-and-drop и Inspector details.
- [ ] Проверить real-window screenshot и tests.

### Заметки агента

2026-06-24T02:20:00+03:00 - Задача поставлена после `T-0183`, чтобы Tasks workspace использовал уже готовые dock/command/selection patterns, а не создавал отдельный UI framework.

2026-06-24T03:20:00+03:00 - Приоритет поднят до P0, потому что `T-0189` остаётся P0-gate и зависит от этой задачи.

## T-0186 [ ] P0: Подключить Project Settings к живым элементам управления на canonical `.e2d`

- Создана: 2026-06-24T02:20:00+03:00
- Состояние: blocked
- Приоритет: P0
- Зависимости: T-0175, T-0183
- Ссылки:
  - Доменный документ: `docs/editor/project-settings-ui.md`; `docs/editor/project-manager.md`; `docs/project-system/project-text-formats.md`; `docs/export/export-preset-model.md`
  - Исходный код: `src/Electron2D.Editor/`; `src/Electron2D.ProjectSystem/`; `src/Electron2D.Export/`
  - Тесты: `tests/Electron2D.Tests.Integration/EditorProjectSettingsUiTests.cs`

### Самодостаточное описание

После устранения файлового конфликта и готового 2D workflow Project Settings должен перейти от smoke-frame/write-through модели к живым элементам управления внутри shell. UI должен редактировать canonical `<ProjectName>.e2d`, embedded `exportPresets`, Input Map, display, renderer и physics settings через общие controls.

### Критерии приёмки

- [ ] Доменный документ обновлён до контракта живых элементов управления и canonical `.e2d` behavior.
- [ ] Добавлены сначала падающие tests для user edits через controls, keyboard save, revert, validation errors и round-trip `.e2d`.
- [ ] Sections `Main Scene`, `Display`, `Renderer`, `Physics`, `Input Map`, `Export Presets` используют runtime form controls, search/filter where useful и validation messages.
- [ ] `Save` записывает валидные изменения в canonical `<ProjectName>.e2d` и dirty state пропадает только после успешной записи.
- [ ] `Apply` применяет только поддерживаемые runtime/editor settings без записи файла; настройки, требующие reload/restart, явно помечаются и не притворяются применёнными.
- [ ] При validation error `Save` и `Apply` не меняют файл и не применяют частичное состояние; dirty state и сообщения ошибок остаются видимыми.
- [ ] `Revert` возвращает UI к последнему сохранённому состоянию без записи secrets и без создания legacy files для новых проектов.
- [ ] Real-window screenshot/analysis подтверждает pointer/keyboard workflow, no overflow и отсутствие запрещённых UI entries.

### Подзадачи

- [ ] Специфицировать live Project Settings controls.
- [ ] Подключить canonical `.e2d` store и validation.
- [ ] Реализовать form controls, save/revert и error display.
- [ ] Проверить focused tests и real-window artifacts.

### Заметки агента

2026-06-24T02:20:00+03:00 - Задача зависит от `T-0175`, потому что нельзя строить живой save/apply UI поверх устаревающего файлового контракта.

2026-06-24T03:20:00+03:00 - Приоритет поднят до P0, потому что `T-0189` остаётся P0-gate и зависит от этой задачи. Перед кодом нужно точно разделить `Save` и `Apply`, validation error flow и момент применения renderer/physics изменений.

## T-0187 [ ] P0: Подключить specialized editors к живому интерфейсу после 2D-среза

- Создана: 2026-06-24T02:20:00+03:00
- Состояние: tracking
- Приоритет: P0
- Зависимости: T-0183
- Дочерние задачи: T-0202, T-0203, T-0204, T-0205
- Ссылки:
  - Доменный документ: `docs/editor/specialized-editors.md`; `docs/animation/spriteframes-animatedsprite2d.md`; `docs/rendering/tilemap-layer-runtime-api.md`; `docs/animation/animation-player-tracks.md`
  - Исходный код: `src/Electron2D.Editor/`; `src/Electron2D/`
  - Тесты: `tests/Electron2D.Tests.Integration/EditorSpecializedEditorsTests.cs`; `tests/Electron2D.Tests.Integration/SpriteFramesAnimatedSprite2DTests.cs`; `tests/Electron2D.Tests.Integration/TileMapLayerRuntimeTests.cs`; `tests/Electron2D.Tests.Integration/AnimationPlayerTracksTests.cs`

### Самодостаточное описание

Specialized editors для `SpriteFrames`, `TileMap` и `AnimationPlayer` должны открываться внутри `2D` workspace и использовать тот же shell, docks, bottom panel, command/document/undo services и runtime controls. Эта tracking-задача не исполняется одним большим scope: каждый specialized editor и общий integration gate вынесены в дочерние задачи.

### Критерии приёмки

- [ ] `T-0202` принята пользователем.
- [ ] `T-0203` принята пользователем.
- [ ] `T-0204` принята пользователем.
- [ ] `T-0205` принята пользователем.
- [ ] Specialized editors не становятся отдельными утилитами и не создают собственный временный UI framework.

### Подзадачи

- [ ] Вести зависимости `SpriteFrames`, `TileMap`, `AnimationPlayer` и integration gate.
- [ ] После приёмки дочерних задач обновить итоговый статус `T-0187`.

### Заметки агента

2026-06-24T02:20:00+03:00 - До `T-0183` эти редакторы не полировать визуально, чтобы не размножать временные tabs и property rows.

2026-06-24T03:20:00+03:00 - Приоритет поднят до P0, потому что `T-0189` остаётся P0-gate и зависит от этой задачи. Перед реализацией стоит разделить scope на `SpriteFrames`, `TileMap`, `AnimationPlayer` и общий integration gate.

2026-06-24T03:49:00+03:00 - Задача переведена в `tracking`; дочерние production-задачи: `T-0202` SpriteFrames, `T-0203` TileMap, `T-0204` AnimationPlayer, `T-0205` integration gate.

## T-0188 [ ] P0: Подключить Agent Workspace к живому состоянию и editor workflows

- Создана: 2026-06-24T02:20:00+03:00
- Состояние: blocked
- Приоритет: P0
- Зависимости: T-0183
- Ссылки:
  - Доменный документ: `docs/editor/agent-workspace-panel.md`; `docs/architecture/agent-native-workflow.md`; `docs/tooling/editor-capability-manifest.md`
  - Исходный код: `src/Electron2D.Editor/`; `src/Electron2D.Tooling/`
  - Тесты: `tests/Electron2D.Tests.Integration/EditorAgentWorkspacePanelTests.cs`

### Самодостаточное описание

`Agent Workspace` сейчас описан как snapshot panel во вкладке `Agent` нижней панели. После 2D vertical slice нужно подключить его к live editor services: current task, changeset, diagnostics, artifacts, jobs, cancellation, route, stale markers и navigation targets должны обновляться в реальном окне без отдельного чата и без права AI принимать задачи за человека.

### Критерии приёмки

- [ ] Доменный документ обновлён до live-state contract и границы human acceptance.
- [ ] Добавлены сначала падающие tests для live updates, diagnostics navigation, artifact links, cancel/stop actions и сохранения состояния при переключении workspaces.
- [ ] Панель находится в bottom panel как вкладка `Agent`, остаётся resizable/hideable/maximizable и доступна в `2D`, `Script`, `Game`, `Tasks`.
- [ ] Высота bottom panel, раскрытое состояние и активная внутренняя вкладка `Agent Workspace` сохраняются между запусками; старое placement `RightBelowInspectorNode` мигрирует в `BottomPanel/Agent`.
- [ ] Actions `Send Review`, `Undo AI`, `Cancel`, `Stop` работают через command service; action `Done` отсутствует.
- [ ] `Send Review`, `Undo AI`, `Cancel` и `Stop` имеют разные области действия: review request, откат agent changeset, отмена active job и остановка agent session/connection.
- [ ] Автоматическое раскрытие `Agent` при handshake error, job failure или review request не забирает keyboard focus.
- [ ] Внутренняя `Diagnostics` показывает только diagnostics агентского процесса, а глобальная bottom `Diagnostics` остаётся общепроектной.
- [ ] При нехватке ширины bottom tabs уходят в overflow-menu, а не сжимаются.
- [ ] Changeset entries открывают соответствующую scene/node/resource/script/project settings surface в shell.
- [ ] Real-window screenshot/analysis подтверждает live state, no overflow, keyboard focus и absence of forbidden AI acceptance action.

### Подзадачи

- [ ] Специфицировать live Agent Workspace state.
- [ ] Подключить diagnostics/artifacts/jobs/current task services.
- [ ] Реализовать navigation targets и commands.
- [ ] Проверить persistence и screenshots.

### Заметки агента

2026-06-24T02:20:00+03:00 - Задача оставлена после `T-0183`, чтобы Agent Workspace ссылался на настоящие editor surfaces, а не только на machine-readable snapshot.

2026-06-24T02:28:00+03:00 - Placement уточнён пользователем: `Agent Workspace` не должен быть правым dock под Inspector/Node. Он становится вкладкой `Agent` нижней панели; справа остаётся только объектный контекст `Inspector | Node`.

2026-06-24T03:20:00+03:00 - Приоритет поднят до P0, потому что `T-0189` остаётся P0-gate и зависит от этой задачи. Дополнительно закреплены no-focus-steal, разделение agent/project diagnostics и overflow-menu для bottom tabs.

## T-0189 [ ] P0: Провести финальную приёмку зрелого Editor UI в реальном окне

- Создана: 2026-06-24T02:20:00+03:00
- Состояние: blocked
- Приоритет: P0
- Зависимости: T-0183, T-0184, T-0185, T-0186, T-0187, T-0188, T-0190, T-0191, T-0192, T-0193
- Ссылки:
  - Доменный документ: `docs/editor/godot4-editor-reference.md`; `docs/editor/editor-shell-layout.md`; `docs/editor/editor-project-shell.md`
  - Визуальный референс: `docs/editor/references/godot-4.7-2d-workspace.md`; PNG `docs/editor/references/godot-4.7-2d-workspace.png`, 1920 x 1032 px, SHA-256 `CE8C799A6AE23423957A904BCA1CD4D28922461DE235A9A5D398864BDE10A84F`.
  - Исходный код: `src/Electron2D.Editor/`; `src/Electron2D/Graphics/UI/`
  - Тесты: `tests/Electron2D.Tests.Integration/EditorProjectShellTests.cs`; `tests/Electron2D.Tests.Integration/EditorShellLayoutTests.cs`; `tests/Electron2D.Tests.Integration/EditorSceneTreeDockTests.cs`; `tests/Electron2D.Tests.Integration/EditorFileSystemDockTests.cs`; `tests/Electron2D.Tests.Integration/EditorViewport2DTests.cs`; `tests/Electron2D.Tests.Integration/EditorInspectorTests.cs`; `tests/Electron2D.Tests.Integration/EditorProjectTasksBoardTests.cs`; `tests/Electron2D.Tests.Integration/EditorAgentWorkspacePanelTests.cs`

### Самодостаточное описание

Эта gate-задача фиксирует момент, когда Editor выходит из стадии каркаса раскладки. Acceptance должен проверять реальное окно, а не только snapshots или подготовительный harness. Референс - информационная архитектура и уровень зрелости Godot 4: menu/workspace/docks/viewport/bottom workflow, states, focus, keyboard navigation, context menus, drag-and-drop, DPI и cross-platform screenshots. Копировать `3D`, `AssetLib` или GDScript нельзя.

### Критерии приёмки

- [ ] Ни один видимый widget не рисуется manual shell renderer; все видимые controls проходят через runtime `SceneTree`/`Control`.
- [ ] Все применимые интерактивные controls имеют обязательные состояния согласно control manifest; для `Label`, separators и декоративных panels явно указано, какие состояния неприменимы.
- [ ] Все docks изменяют размер и сохраняют layout.
- [ ] Selection синхронизирована между Scene Tree, Viewport и Inspector.
- [ ] Context menus, inline rename и drag-and-drop работают в реальном окне.
- [ ] Keyboard-only workflow покрывает основные операции.
- [ ] Русский и английский UI не имеют text overflow.
- [ ] Проверены DPI 100%, 125%, 150% и 200%.
- [ ] Проверены размеры минимум 1280x720 и 1920x1080.
- [ ] Реальные window screenshots получены на Windows, Linux и macOS; если host недоступен, задача остаётся blocked с явной причиной.
- [ ] Переключение workspaces не теряет selection, zoom, open documents и Agent state.
- [ ] Bottom panel автоматически показывает значимую ошибку, но не занимает экран без причины.
- [ ] Автоматическое раскрытие `Agent` не забирает keyboard focus.
- [ ] Внутренняя `Diagnostics` в `Agent Workspace` и глобальная bottom `Diagnostics` имеют разные источники данных и не смешивают сообщения.
- [ ] Bottom tabs при нехватке ширины уходят в overflow-menu.
- [ ] `3D`, `AssetLib` и GDScript отсутствуют.
- [ ] Acceptance выполняется по реальному окну; documented harness допускается только как подготовительная проверка layout/state и не заменяет screenshot окна.

### Подзадачи

- [ ] Специфицировать финальный acceptance matrix и artifact layout.
- [ ] Зафиксировать manifest controls/states и числовые pass/fail thresholds для DPI, overflow и forbidden UI checks.
- [ ] Добавить automated checks для DPI, sizes, states, overflow и forbidden UI.
- [ ] Запустить real-window сценарии на Windows.
- [ ] Зафиксировать Linux/macOS screenshots или blocker по окружению.
- [ ] Обновить доменные документы итоговыми evidence links и ограничениями.

### Заметки агента

2026-06-24T02:20:00+03:00 - Задача оформляет Definition of Done из пользовательского диагноза как отдельный gate, чтобы после vertical slice и последующих workspace подключений оставалась проверяемая граница готовности видимого Editor UI.

## T-0190 [ ] P0: Реализовать интерактивные операции Scene Tree

- Создана: 2026-06-24T03:49:00+03:00
- Состояние: blocked
- Приоритет: P0
- Зависимости: T-0183, T-0196
- Ссылки:
  - Доменный документ: `docs/editor/scene-tree-dock.md`; `docs/editor/editor-shell-layout.md`
  - Исходный код: `src/Electron2D.Editor/`; `src/Electron2D/Graphics/UI/`
  - Тесты: `tests/Electron2D.Tests.Integration/EditorSceneTreeDockTests.cs`

### Самодостаточное описание

После первого 2D-среза Scene Tree должен получить полноценные интерактивные операции поверх общих controls: context menu, inline rename, reorder/reparent drag-and-drop, multi-selection и indicators для visibility/lock/script. Эта задача закрывает именно операции дерева сцены, которые сознательно исключены из `T-0183`.

### Критерии приёмки

- [ ] Доменный документ обновлён до кода и описывает context menu, inline rename, reorder/reparent, multi-selection и indicators.
- [ ] Добавлены сначала падающие tests для context menu action, inline rename validation, drag reorder/reparent и undo/redo.
- [ ] Production implementation выполнена в runtime-control/editor service path без отдельного input или renderer обхода.
- [ ] После зелёных проверок тот же доменный документ обновлён фактическим поведением, ограничениями, командой проверки `dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorSceneTreeDockTests" -v q` и artifact path `.temp/editor-scene-tree-operations/`.
- [ ] Операции работают через общий selection/document/undo command path и не обходят runtime UI focus/input.
- [ ] Invalid rename или forbidden reparent показывают validation error и не меняют scene document.
- [ ] Real-window screenshot/analysis подтверждает Scene Tree operations без text overflow.

### Подзадачи

- [ ] Специфицировать операции Scene Tree.
- [ ] Добавить failing interaction tests.
- [ ] Подключить commands, undo/redo и validation.
- [ ] Проверить real-window interaction artifacts.

## T-0191 [ ] P0: Реализовать drag-and-drop FileSystem в Scene

- Создана: 2026-06-24T03:49:00+03:00
- Состояние: blocked
- Приоритет: P0
- Зависимости: T-0183, T-0196
- Ссылки:
  - Доменный документ: `docs/editor/file-system-dock.md`; `docs/editor/viewport-2d.md`
  - Исходный код: `src/Electron2D.Editor/`; `src/Electron2D.ProjectSystem/`
  - Тесты: `tests/Electron2D.Tests.Integration/EditorFileSystemDockTests.cs`; `tests/Electron2D.Tests.Integration/EditorViewport2DTests.cs`

### Самодостаточное описание

FileSystem должен позволять перетащить resource или scene entry в 2D viewport/Scene Tree и создать корректный scene edit через общий drag-and-drop lifecycle. Эта задача закрывает resource placement, который исключён из первого среза `T-0183`.

### Критерии приёмки

- [ ] Доменный документ обновлён до кода и описывает payload, valid targets, preview, drop, cancel и validation errors.
- [ ] Добавлены сначала падающие tests для valid resource drop, invalid payload, cancel и undo/redo placement.
- [ ] Production implementation выполнена через общий drag-and-drop lifecycle и scene editing service без editor-only input path.
- [ ] После зелёных проверок тот же доменный документ обновлён фактическим поведением, ограничениями, командой проверки `dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorFileSystemDockTests|FullyQualifiedName~EditorViewport2DTests" -v q` и artifact path `.temp/editor-filesystem-drag-drop/`.
- [ ] Drop в viewport создаёт или обновляет scene node через scene editing service, dirty marker и undo stack.
- [ ] Drop в недопустимую область не меняет document и показывает понятный feedback.
- [ ] Real-window screenshot/analysis подтверждает pointer path и отсутствие layout break.

### Подзадачи

- [ ] Специфицировать FileSystem drag payload.
- [ ] Добавить failing drag/drop tests.
- [ ] Подключить scene editing service и undo.
- [ ] Проверить real-window artifacts.

## T-0192 [ ] P0: Реализовать полный transform gizmo и snapping-сценарий

- Создана: 2026-06-24T03:49:00+03:00
- Состояние: blocked
- Приоритет: P0
- Зависимости: T-0183, T-0181
- Ссылки:
  - Доменный документ: `docs/editor/viewport-2d.md`; `docs/editor/inspector.md`
  - Исходный код: `src/Electron2D.Editor/`; `src/Electron2D/Graphics/UI/`
  - Тесты: `tests/Electron2D.Tests.Integration/EditorViewport2DTests.cs`; `tests/Electron2D.Tests.Integration/EditorInspectorTests.cs`

### Самодостаточное описание

Первый 2D-срез требует только базовый outline/handles. Эта задача добавляет полноценный viewport transform workflow: select/move/rotate/scale tools, snapping controls, handles, ruler/grid integration, camera preview/collision overlays и синхронизацию с Inspector.

### Критерии приёмки

- [ ] Доменный документ обновлён до кода и описывает tool modes, handles, snapping settings, overlays и Inspector sync.
- [ ] Добавлены сначала падающие tests для move/rotate/scale, snapping on/off, undo/redo и Inspector round-trip.
- [ ] Production implementation выполнена в viewport/Inspector/document services без отдельного gizmo input path.
- [ ] После зелёных проверок тот же доменный документ обновлён фактическим поведением, ограничениями, командой проверки `dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorViewport2DTests|FullyQualifiedName~EditorInspectorTests" -v q` и artifact path `.temp/editor-viewport-transform-gizmo/`.
- [ ] Gizmo operations обновляют scene document через общий undo/document service.
- [ ] Snapping работает предсказуемо при zoom/pan и не меняет selection.
- [ ] Real-window screenshots 1280x720 и 1920x1080 подтверждают читаемые handles/toolbar без text overflow.

### Подзадачи

- [ ] Специфицировать viewport tool modes.
- [ ] Добавить failing transform workflow tests.
- [ ] Реализовать handles/snapping/overlays.
- [ ] Проверить screenshots и undo/redo.

## T-0193 [ ] P0: Реализовать основной keyboard-only workflow 2D-редактора

- Создана: 2026-06-24T03:49:00+03:00
- Состояние: blocked
- Приоритет: P0
- Зависимости: T-0183, T-0185, T-0188, T-0190, T-0191, T-0192
- Ссылки:
  - Доменный документ: `docs/editor/editor-shell-layout.md`; `docs/input/input-dispatch-ui-focus.md`
  - Исходный код: `src/Electron2D.Editor/`; `src/Electron2D/Input/`; `src/Electron2D/Graphics/UI/`
  - Тесты: `tests/Electron2D.Tests.Integration/EditorProjectShellTests.cs`

### Самодостаточное описание

Финальный UI gate требует keyboard-only workflow, но первый 2D-срез закрывает только базовые shortcuts. Эта задача описывает и проверяет основной keyboard path для 2D-редактора: меню, workspace switcher, Scene Tree, FileSystem, Inspector, bottom panel, Agent и run workflow. Script workspace, Project Settings и specialized editors проверяют свои keyboard-сценарии в собственных задачах и финальном gate `T-0189`.

### Критерии приёмки

- [ ] Доменный документ обновлён до кода и содержит keyboard scenario matrix и focus order для основного 2D workflow.
- [ ] Добавлены сначала падающие tests для Tab/Shift+Tab traversal, menu activation, workspace switch, tree navigation, Inspector edit, bottom tab switch и run/stop shortcuts.
- [ ] Production implementation выполнена через общий focus/command routing без mouse-only fallback для основных операций.
- [ ] После зелёных проверок тот же доменный документ обновлён фактическим поведением, ограничениями, командой проверки `dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorProjectShellTests" -v q` и artifact path `.temp/editor-keyboard-workflow/`.
- [ ] Keyboard focus ring видим, не меняет layout и не теряется при popup/modal/Agent auto-open.
- [ ] Keyboard-only workflow работает без mouse interaction для основных операций 2D-редактора `open -> select -> edit -> save -> run -> stop`.
- [ ] Real-window analysis фиксирует active focus target после каждого шага.

### Подзадачи

- [ ] Специфицировать focus order и shortcut matrix.
- [ ] Добавить failing keyboard workflow tests.
- [ ] Подключить missing focus/command handling.
- [ ] Проверить real-window keyboard analysis.

### Заметки агента

2026-06-24T13:36:00+03:00 - Название и scope уточнены: это основной keyboard-only workflow 2D-редактора, а не полный keyboard path всех workspaces. Script, Project Settings и specialized editors закрывают свои сценарии в отдельных задачах.

## T-0194 [ ] P0: Реализовать меню, toolbar и вкладки

- Создана: 2026-06-24T03:49:00+03:00
- Состояние: blocked
- Приоритет: P0
- Зависимости: T-0178
- Ссылки:
  - Доменный документ: `docs/ui/structured-controls.md`; `docs/editor/editor-shell-layout.md`
  - Исходный код: `src/Electron2D/Graphics/UI/`; `src/Electron2D.Editor/`
  - Тесты: `tests/Electron2D.Tests.Integration/StructuredControlInteractionTests.cs`

### Самодостаточное описание

Дочерняя задача `T-0179`: реализовать и переаттестовать controls для menu bar, popup menu, toolbar buttons, tooltips, tab bar и tab container. Эти controls нужны верхнему shell, document tabs, workspace switcher и bottom panel.

### Критерии приёмки

- [ ] Доменные документы обновлены до кода и описывают `MenuBar`, `PopupMenu`, `ToolButton`, tooltip presenter, `TabBar` и `TabContainer`.
- [ ] Добавлены сначала падающие tests для pointer, keyboard, focus, disabled state, selected tab, overflow и clipping.
- [ ] Production implementation выполнена в runtime UI controls, а editor использует эти controls без собственного drawing/input path.
- [ ] После зелёных проверок те же доменные документы обновлены фактическим поведением, ограничениями, командой проверки `dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~StructuredControlInteractionTests" -v q` и artifact path `.temp/ui-desktop-menu-tabs/`.
- [ ] Controls получают styles/icons из theme и не используют editor-only drawing.
- [ ] Public runtime API имеет полную XML documentation, если добавляются public types.
- [ ] Focused UI tests, documentation verifier и source license verifier проходят.

### Подзадачи

- [ ] Специфицировать controls и API границу.
- [ ] Добавить failing structured control tests.
- [ ] Реализовать controls и theme states.
- [ ] Проверить docs/API/license.

## T-0195 [ ] P0: Реализовать form controls для Inspector и Project Settings

- Создана: 2026-06-24T03:49:00+03:00
- Состояние: blocked
- Приоритет: P0
- Зависимости: T-0178
- Ссылки:
  - Доменный документ: `docs/ui/basic-controls.md`; `docs/ui/structured-controls.md`; `docs/editor/inspector.md`; `docs/editor/project-settings-ui.md`
  - Исходный код: `src/Electron2D/Graphics/UI/`; `src/Electron2D.Editor/`
  - Тесты: `tests/Electron2D.Tests.Integration/StructuredControlInteractionTests.cs`; `tests/Electron2D.Tests.Integration/EditorInspectorTests.cs`

### Самодостаточное описание

Дочерняя задача `T-0179`: реализовать form controls для реальных свойств и настроек: `CheckBox`, `LineEdit`, `SpinBox`, `OptionButton`, numeric editing, validation state и reset/revert affordances.

### Критерии приёмки

- [ ] Доменные документы обновлены до кода и описывают form controls, validation messages и keyboard interaction.
- [ ] Добавлены сначала падающие tests для bool, number, enum, text input, disabled state, validation error и revert/reset action.
- [ ] Production implementation выполнена в runtime UI controls и пригодна для Inspector/Project Settings без отдельного editor input path.
- [ ] После зелёных проверок те же доменные документы обновлены фактическим поведением, ограничениями, командой проверки `dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~StructuredControlInteractionTests|FullyQualifiedName~EditorInspectorTests" -v q` и artifact path `.temp/ui-form-controls/`.
- [ ] Controls пригодны для Inspector `Transform` и Project Settings sections без отдельного editor input path.
- [ ] Public API documentation полная для новых public runtime members.
- [ ] Focused tests, local documentation verifier и source license verifier проходят.

### Подзадачи

- [ ] Специфицировать form control contract.
- [ ] Добавить failing tests.
- [ ] Реализовать controls, validation и theme states.
- [ ] Проверить Inspector/Project Settings smoke use.

## T-0196 [ ] P0: Реализовать tree, list, scroll и split controls

- Создана: 2026-06-24T03:49:00+03:00
- Состояние: blocked
- Приоритет: P0
- Зависимости: T-0177, T-0178
- Ссылки:
  - Доменный документ: `docs/ui/structured-controls.md`; `docs/ui/containers.md`
  - Исходный код: `src/Electron2D/Graphics/UI/`; `src/Electron2D.Editor/`
  - Тесты: `tests/Electron2D.Tests.Integration/StructuredControlInteractionTests.cs`; `tests/Electron2D.Tests.Integration/ContainerLayoutTests.cs`

### Самодостаточное описание

Дочерняя задача `T-0179`: реализовать `Tree`, `ItemList`, визуальный `ScrollBar`, `ScrollContainer` integration и `SplitContainer`. Эти controls нужны Scene Tree, FileSystem, Inspector, Tasks board, docks и bottom panel resizing.

### Критерии приёмки

- [ ] Доменные документы обновлены до кода и описывают tree/list rows, selection, icons, scrolling, scrollbar, splitters и overflow behavior.
- [ ] Добавлены сначала падающие tests для selection, keyboard navigation, wheel, scrollbar drag, ensure-visible, splitter drag и clipping.
- [ ] Production implementation выполнена в runtime UI controls/containers и покрывает editor use cases без локальных list/splitter implementations.
- [ ] После зелёных проверок те же доменные документы обновлены фактическим поведением, ограничениями, командой проверки `dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~StructuredControlInteractionTests|FullyQualifiedName~ContainerLayoutTests" -v q` и artifact path `.temp/ui-tree-list-scroll-split/`.
- [ ] Controls работают с длинными путями и русским текстом без text overflow.
- [ ] Splitter pointer target и visible line соответствуют design metrics.
- [ ] Focused UI/container tests, docs verifier и source license verifier проходят.

### Подзадачи

- [ ] Специфицировать tree/list/scroll/split controls.
- [ ] Добавить failing tests.
- [ ] Реализовать controls и integration.
- [ ] Проверить editor shell use cases.

## T-0197 [ ] P0: Реализовать TextEdit, CodeEdit и индикаторы

- Создана: 2026-06-24T03:49:00+03:00
- Состояние: blocked
- Приоритет: P0
- Зависимости: T-0177, T-0178, T-0196
- Ссылки:
  - Доменный документ: `docs/ui/structured-controls.md`; `docs/editor/script-workspace.md`
  - Исходный код: `src/Electron2D/Graphics/UI/`; `src/Electron2D.Editor/`
  - Тесты: `tests/Electron2D.Tests.Integration/StructuredControlInteractionTests.cs`; `tests/Electron2D.Tests.Integration/EditorScriptWorkspaceTests.cs`

### Самодостаточное описание

Дочерняя задача `T-0179`: реализовать базовые text/code editing controls и вспомогательные UI elements: `TextEdit`, `CodeEdit`, separators и progress indicators. Полная интеграция Script workspace остаётся в `T-0198`-`T-0201`.

### Критерии приёмки

- [ ] Доменные документы обновлены до кода и описывают text edit, code edit, caret, selection, scrolling, undo hooks, separators и progress indicators.
- [ ] Добавлены сначала падающие tests для caret movement, selection, typing, scrolling, copy/paste-safe command hooks и progress state.
- [ ] Production implementation выполнена в runtime text/code editing controls и не добавляет GDScript-specific UI.
- [ ] После зелёных проверок те же доменные документы обновлены фактическим поведением, ограничениями, командой проверки `dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~StructuredControlInteractionTests|FullyQualifiedName~EditorScriptWorkspaceTests" -v q` и artifact path `.temp/ui-text-code-edit/`.
- [ ] `CodeEdit` поддерживает базовую line gutter, monospace font и text clipping без GDScript-specific UI.
- [ ] Public API documentation полная для новых public runtime members.
- [ ] Focused tests, docs verifier и source license verifier проходят.

### Подзадачи

- [ ] Специфицировать text/code edit controls.
- [ ] Добавить failing tests.
- [ ] Реализовать controls и indicators.
- [ ] Проверить Script workspace smoke prerequisites.

## T-0198 [ ] P0: Подключить Script code editor к живым controls

- Создана: 2026-06-24T03:49:00+03:00
- Состояние: blocked
- Приоритет: P0
- Зависимости: T-0183, T-0197
- Ссылки:
  - Доменный документ: `docs/editor/script-workspace.md`; `docs/scripting/editor-script-workflow.md`
  - Исходный код: `src/Electron2D.Editor/`; `src/Electron2D.Scripting/`
  - Тесты: `tests/Electron2D.Tests.Integration/EditorScriptWorkspaceTests.cs`

### Самодостаточное описание

Дочерняя задача `T-0184`: подключить `.cs` document tabs, `CodeEdit`, caret/selection, dirty/save state и document service к реальному Script workspace без внешней IDE и без отдельного tab/input framework.

### Критерии приёмки

- [ ] Доменные документы обновлены до кода и описывают live code editor contract.
- [ ] Добавлены сначала падающие tests для open `.cs`, caret/focus, edit, dirty marker, save/reopen и workspace switch state.
- [ ] Production implementation подключает `CodeEdit`, document service и dirty/save state без внешней IDE и без отдельного tab/input framework.
- [ ] После зелёных проверок те же доменные документы обновлены фактическим поведением, ограничениями, командой проверки `dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorScriptWorkspaceTests" -v q` и artifact path `.temp/editor-script-code-editor/`.
- [ ] C# tabs отделены от scene tabs.
- [ ] `CodeEdit` использует runtime controls, common focus и common scrolling.
- [ ] Real-window screenshot/analysis подтверждает editor surface без GDScript UI.

### Подзадачи

- [ ] Специфицировать code editor workflow.
- [ ] Добавить failing Script workspace tests.
- [ ] Подключить document service/save.
- [ ] Проверить real-window artifacts.

## T-0199 [ ] P0: Подключить UI для C# language services

- Создана: 2026-06-24T03:49:00+03:00
- Состояние: blocked
- Приоритет: P0
- Зависимости: T-0198
- Ссылки:
  - Доменный документ: `docs/scripting/editor-language-services.md`; `docs/editor/script-workspace.md`
  - Исходный код: `src/Electron2D.Editor/`; `src/Electron2D.CSharpLanguageServices/`
  - Тесты: `tests/Electron2D.Tests.Integration/EditorScriptLanguageServicesTests.cs`

### Самодостаточное описание

Дочерняя задача `T-0184`: подключить completion popup, hover/Quick Info, signature help, diagnostics navigation, rename/format/code-action preview и stale response handling к живому Script workspace.

### Критерии приёмки

- [ ] Доменный документ обновлён до кода и описывает UI contract language services.
- [ ] Добавлены сначала падающие tests для completion, hover, signature help, diagnostics navigation и stale marker.
- [ ] Production implementation подключает language services UI к live Script workspace без отдельного popup/focus path.
- [ ] После зелёных проверок те же доменные документы обновлены фактическим поведением, ограничениями, командой проверки `dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorScriptLanguageServicesTests" -v q` и artifact path `.temp/editor-language-services-ui/`.
- [ ] Popup/focus/keyboard behavior использует общий runtime UI path.
- [ ] Diagnostics открывают правильный файл и позицию в Script workspace.
- [ ] Real-window screenshot/analysis подтверждает popup и diagnostics без text overflow.

### Подзадачи

- [ ] Специфицировать language services UI.
- [ ] Добавить failing language service UI tests.
- [ ] Подключить popup/diagnostics/navigation.
- [ ] Проверить real-window artifacts.

## T-0200 [ ] P0: Подключить UI debugger в Script workspace

- Создана: 2026-06-24T03:49:00+03:00
- Состояние: blocked
- Приоритет: P0
- Зависимости: T-0198
- Ссылки:
  - Доменный документ: `docs/scripting/managed-debugger.md`; `docs/editor/script-workspace.md`
  - Исходный код: `src/Electron2D.Editor/`; `src/Electron2D.ManagedDebugging/`
  - Тесты: `tests/Electron2D.Tests.Integration/EditorManagedDebuggerTests.cs`

### Самодостаточное описание

Дочерняя задача `T-0184`: подключить debugger controls, breakpoint gutter, current line highlight, call stack, locals, watches, exception panel и debug output к живому Script workspace и bottom panel.

### Критерии приёмки

- [ ] Доменный документ обновлён до кода и описывает debugger UI contract и DAP boundary.
- [ ] Добавлены сначала падающие tests для breakpoints, current line, stack, locals/watches, pause/continue/stop и stale debug state.
- [ ] Production implementation подключает debugger UI к command routing, Script workspace и bottom panel без конфликта с run controls.
- [ ] После зелёных проверок те же доменные документы обновлены фактическим поведением, ограничениями, командой проверки `dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorManagedDebuggerTests" -v q` и artifact path `.temp/editor-debugger-ui/`.
- [ ] Debug controls используют command routing и не конфликтуют с Run Scene/Stop.
- [ ] Debug output отображается в bottom panel без перехвата фокуса.
- [ ] Real-window screenshot/analysis подтверждает debugger UI без GDScript UI.

### Подзадачи

- [ ] Специфицировать debugger UI.
- [ ] Добавить failing debugger UI tests.
- [ ] Подключить gutter, panels и commands.
- [ ] Проверить focused tests и screenshots.

## T-0201 [ ] P0: Провести integration gate Script workspace

- Создана: 2026-06-24T03:49:00+03:00
- Состояние: blocked
- Приоритет: P0
- Зависимости: T-0198, T-0199, T-0200
- Ссылки:
  - Доменный документ: `docs/editor/script-workspace.md`; `docs/scripting/editor-script-workflow.md`
  - Исходный код: `src/Electron2D.Editor/`
  - Тесты: `tests/Electron2D.Tests.Integration/EditorScriptWorkspaceTests.cs`; `tests/Electron2D.Tests.Integration/EditorScriptLanguageServicesTests.cs`; `tests/Electron2D.Tests.Integration/EditorManagedDebuggerTests.cs`

### Самодостаточное описание

Дочерняя задача `T-0184`: проверить, что code editor, language services и debugger UI работают как единый Script workspace и не создают временные framework paths.

### Критерии приёмки

- [ ] Единственный Script workspace сохраняет open documents, caret, diagnostics, debug state и dirty markers при переключении workspace.
- [ ] Доменные документы обновлены до integration scenario до кода, а сначала падающий integration smoke фиксирует совместную работу code editor, language services и debugger UI.
- [ ] Production implementation синхронизирует cross-surface state без временного tab/input framework.
- [ ] Completion, diagnostics navigation и debugger controls работают вместе в одном real-window scenario.
- [ ] Screenshot/analysis подтверждает отсутствие GDScript UI, text overflow и layout conflicts.
- [ ] После зелёных проверок те же доменные документы обновлены фактическим поведением, ограничениями, командой проверки `dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorScriptWorkspaceTests|FullyQualifiedName~EditorScriptLanguageServicesTests|FullyQualifiedName~EditorManagedDebuggerTests" -v q` и artifact path `.temp/editor-script-workspace-gate/`.
- [ ] Focused integration tests и documentation verifiers проходят.

### Подзадачи

- [ ] Специфицировать integration scenario.
- [ ] Добавить failing integration smoke.
- [ ] Исправить cross-surface state sync.
- [ ] Выполнить focused checks.

## T-0202 [ ] P0: Подключить SpriteFrames editor к живому UI

- Создана: 2026-06-24T03:49:00+03:00
- Состояние: blocked
- Приоритет: P0
- Зависимости: T-0183, T-0196
- Ссылки:
  - Доменный документ: `docs/editor/specialized-editors.md`; `docs/animation/spriteframes-animatedsprite2d.md`
  - Исходный код: `src/Electron2D.Editor/`; `src/Electron2D/`
  - Тесты: `tests/Electron2D.Tests.Integration/EditorSpecializedEditorsTests.cs`

### Самодостаточное описание

Дочерняя задача `T-0187`: реализовать live editor для `SpriteFrames` внутри `2D` workspace с preview, frame list, import/resource sync, undo/redo и save/reopen.

### Критерии приёмки

- [ ] Доменный документ обновлён до кода и описывает SpriteFrames live editor.
- [ ] Добавлены сначала падающие tests для open, add/remove/reorder frames, preview, undo/redo и save/reopen.
- [ ] Production implementation подключает SpriteFrames editor к общим controls, document service и undo stack без отдельной утилиты.
- [ ] После зелёных проверок те же доменные документы обновлены фактическим поведением, ограничениями, командой проверки `dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorSpecializedEditorsTests|FullyQualifiedName~SpriteFramesAnimatedSprite2DTests" -v q` и artifact path `.temp/editor-spriteframes-ui/`.
- [ ] Editor использует общие tabs, list/scroll controls, toolbar icons и document service.
- [ ] Изменения сохраняются в runtime resource text document.
- [ ] Real-window screenshot/analysis подтверждает layout без text overflow.

### Подзадачи

- [ ] Специфицировать SpriteFrames UI.
- [ ] Добавить failing tests.
- [ ] Реализовать live controls и document sync.
- [ ] Проверить screenshot и round-trip.

## T-0203 [ ] P0: Подключить TileMap editor к живому UI

- Создана: 2026-06-24T03:49:00+03:00
- Состояние: blocked
- Приоритет: P0
- Зависимости: T-0183, T-0196
- Ссылки:
  - Доменный документ: `docs/editor/specialized-editors.md`; `docs/rendering/tilemap-layer-runtime-api.md`
  - Исходный код: `src/Electron2D.Editor/`; `src/Electron2D/`
  - Тесты: `tests/Electron2D.Tests.Integration/EditorSpecializedEditorsTests.cs`

### Самодостаточное описание

Дочерняя задача `T-0187`: реализовать live TileMap editor внутри `2D` workspace с palette, brush/erase/select tools, layer sync, undo/redo и save/reopen.

### Критерии приёмки

- [ ] Доменный документ обновлён до кода и описывает TileMap live editor.
- [ ] Добавлены сначала падающие tests для palette select, paint, erase, layer change, undo/redo и save/reopen.
- [ ] Production implementation подключает TileMap editor к общим controls, viewport overlay, document service и undo stack без отдельной утилиты.
- [ ] После зелёных проверок те же доменные документы обновлены фактическим поведением, ограничениями, командой проверки `dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorSpecializedEditorsTests|FullyQualifiedName~TileMapLayerRuntimeTests" -v q` и artifact path `.temp/editor-tilemap-ui/`.
- [ ] Editor использует общие split/list/scroll/tool controls и viewport overlay.
- [ ] Изменения сохраняются в runtime scene/resource text documents.
- [ ] Real-window screenshot/analysis подтверждает pointer workflow и отсутствие `3D` UI.

### Подзадачи

- [ ] Специфицировать TileMap UI.
- [ ] Добавить failing tests.
- [ ] Реализовать palette/tools/document sync.
- [ ] Проверить screenshot и round-trip.

## T-0204 [ ] P0: Подключить AnimationPlayer editor к живому UI

- Создана: 2026-06-24T03:49:00+03:00
- Состояние: blocked
- Приоритет: P0
- Зависимости: T-0183, T-0196
- Ссылки:
  - Доменный документ: `docs/editor/specialized-editors.md`; `docs/animation/animation-player-tracks.md`
  - Исходный код: `src/Electron2D.Editor/`; `src/Electron2D/`
  - Тесты: `tests/Electron2D.Tests.Integration/EditorSpecializedEditorsTests.cs`

### Самодостаточное описание

Дочерняя задача `T-0187`: реализовать live AnimationPlayer editor с timeline, tracks, keyframes, playback preview, undo/redo и save/reopen внутри общего shell.

### Критерии приёмки

- [ ] Доменный документ обновлён до кода и описывает AnimationPlayer live editor.
- [ ] Добавлены сначала падающие tests для add/remove track, add/move keyframe, playback preview, undo/redo и save/reopen.
- [ ] Production implementation подключает AnimationPlayer editor к общим controls, timeline/document service и undo stack без отдельной утилиты.
- [ ] После зелёных проверок те же доменные документы обновлены фактическим поведением, ограничениями, командой проверки `dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorSpecializedEditorsTests|FullyQualifiedName~AnimationPlayerTracksTests" -v q` и artifact path `.temp/editor-animationplayer-ui/`.
- [ ] Timeline использует общие controls, scrolling, focus и command routing.
- [ ] Изменения сохраняются в runtime animation text document.
- [ ] Real-window screenshot/analysis подтверждает timeline layout без overflow.

### Подзадачи

- [ ] Специфицировать AnimationPlayer UI.
- [ ] Добавить failing tests.
- [ ] Реализовать timeline/tracks/document sync.
- [ ] Проверить screenshot и round-trip.

## T-0205 [ ] P0: Провести integration gate specialized editors

- Создана: 2026-06-24T03:49:00+03:00
- Состояние: blocked
- Приоритет: P0
- Зависимости: T-0202, T-0203, T-0204
- Ссылки:
  - Доменный документ: `docs/editor/specialized-editors.md`
  - Исходный код: `src/Electron2D.Editor/`
  - Тесты: `tests/Electron2D.Tests.Integration/EditorSpecializedEditorsTests.cs`

### Самодостаточное описание

Дочерняя задача `T-0187`: проверить, что `SpriteFrames`, `TileMap` и `AnimationPlayer` editors работают внутри одного `2D` workspace, используют общий shell/document/undo stack и не конфликтуют между собой.

### Критерии приёмки

- [ ] Один project scenario открывает все три specialized editors и сохраняет/reopens изменения.
- [ ] Доменный документ обновлён до integration scenario до кода, а сначала падающий integration smoke фиксирует совместную работу `SpriteFrames`, `TileMap` и `AnimationPlayer`.
- [ ] Production implementation синхронизирует document tabs, undo/redo, dirty markers и resource sync для всех трёх редакторов.
- [ ] Undo/redo, dirty markers, document tabs и resource sync работают единообразно.
- [ ] Screenshot/analysis подтверждает layout, controls, keyboard save и отсутствие `3D`, `AssetLib`, GDScript UI.
- [ ] После зелёных проверок тот же доменный документ обновлён фактическим поведением, ограничениями, командой проверки `dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorSpecializedEditorsTests" -v q` и artifact path `.temp/editor-specialized-editors-gate/`.
- [ ] Focused specialized editors tests и documentation verifiers проходят.

### Подзадачи

- [ ] Специфицировать integration scenario.
- [ ] Добавить failing integration smoke.
- [ ] Исправить cross-editor state sync.
- [ ] Выполнить focused checks.

## T-0226 [ ] P0: Привести Texture2D к контракту Godot 4.7

- Создана: 2026-06-25T14:32:09+03:00
- Состояние: open
- Приоритет: P0
- Зависимости: нет
- Ссылки:
  - Доменный документ: `docs/rendering/texture-resource-baseline.md`
  - Доменный документ: `docs/rendering/rendering-server.md`
  - Исходный код: `src/Electron2D/Graphics/Rendering/Texture2D.cs`
  - Исходный код: `src/Electron2D/Graphics/Rendering/ImageTexture.cs`
  - Новый исходный код: `src/Electron2D/Graphics/Rendering/Image.cs`
  - Новый исходный код: `src/Electron2D/Graphics/Rendering/PlaceholderTexture2D.cs`
  - Тесты: `tests/Electron2D.Tests.Unit/Texture2DTests.cs`
  - Тесты: `tests/Electron2D.Tests.Integration/CanvasImmediateDrawingSubmissionTests.cs`
  - API manifest: `data/api/`
  - Wiki: `docs/`

### Самодостаточное описание

Текущий `Texture2D` является базовым ресурсом для размера, прозрачности, mipmap-данных и проверки прозрачности пикселя. Это полезный минимальный слой, но он не соответствует выбранному API-подмножеству Godot 4.7: отсутствуют `GetFormat()`, `GetImage()`, `CreatePlaceholder()`, `Draw()`, `DrawRect()`, `DrawRectRegion()` и отдельный внутренний переопределяемый контракт отрисовки для наследников текстур.

Нужное поведение: `Texture2D` должен стать публичной базой, через которую конкретные texture resources могут вернуть копию изображения, формат пикселей, placeholder resource и отправить команды отрисовки в общий путь через `RenderingServer`/canvas submission. Публичный API не должен раскрывать внутренний модуль показа кадра. `Image` должен хранить пиксели и `Image.Format`, достаточные для `Texture2D.GetImage()` и `ImageTexture`, а `PlaceholderTexture2D` должен быть корректным placeholder resource для сериализации, инспектора и будущего редактора.

### Критерии приёмки

- [ ] До изменения production-кода `docs/rendering/texture-resource-baseline.md` описывает целевой контракт `Texture2D`, `Image`, `Image.Format`, placeholder texture и поведение методов отрисовки.
- [ ] Добавлены сначала падающие тесты публичного API, которые проверяют наличие `GetFormat()`, `GetImage()`, `CreatePlaceholder()`, `Draw()`, `DrawRect()` и `DrawRectRegion()`.
- [ ] Добавлен публичный `Image` с минимальным `Image.Format`, размерами, неизменяемостью, семантикой копирования и доступом к RGBA pixels без раскрытия внутренних массивов.
- [ ] `ImageTexture` может возвращать копию своих пикселей через `GetImage()` и корректный `GetFormat()`.
- [ ] `Texture2D.CreatePlaceholder()` возвращает `PlaceholderTexture2D`, сохраняющий размер и прозрачность исходной texture.
- [ ] `Texture2D.Draw(...)`, `DrawRect(...)` и `DrawRectRegion(...)` отправляют команды отрисовки через общий canvas/rendering path, а не через публичный API конкретного внутреннего модуля показа кадра.
- [ ] Для наследников добавлен внутренний переопределяемый контракт отрисовки, который позволяет `AtlasTexture` корректировать source/destination без дублирования logic в presenter-ах.
- [ ] `DrawRectRegion()` покрывает обычный source rect, частично обрезанный source rect и destination rect с разворотом.
- [ ] API manifest, Wiki, список публичного API и XML documentation обновлены для всех новых public types/members.
- [ ] После зелёных проверок доменный документ обновлён фактическим поведением, ограничениями и командами проверки.

### Подзадачи

- [ ] Уточнить доменный документ `Texture2D` под набор публичных методов Godot 4.7.
- [ ] Добавить красные тесты на набор публичных методов и контракт отрисовки.
- [ ] Реализовать `Image`, `Image.Format`, `PlaceholderTexture2D` и новые методы `Texture2D`.
- [ ] Подключить методы отрисовки к общему rendering path.
- [ ] Обновить API manifest, Wiki, список публичного API и XML documentation.
- [ ] Запустить focused unit/integration tests, source license verifier и `git diff --check`.

### Заметки агента

2026-06-25T14:32:09+03:00 - Создано по вердикту пользователя: изолированная совместимость `AtlasTexture` будет ложной, пока базовый `Texture2D` не имеет API и контракта отрисовки Godot 4.7.

## T-0227 [ ] P0: Привести AtlasTexture к контракту Godot 4.7

- Создана: 2026-06-25T14:32:09+03:00
- Состояние: blocked
- Приоритет: P0
- Зависимости: T-0226
- Ссылки:
  - Доменный документ: `docs/rendering/texture-resource-baseline.md`
  - Доменный документ: `docs/rendering/rendering-server.md`
  - Исходный код: `src/Electron2D/Graphics/Rendering/AtlasTexture.cs`
  - Исходный код: `src/Electron2D/Runtime/Application/RuntimeHost.cs`
  - Исходный код: `src/Electron2D/Runtime/Application/RuntimeFramePresenter.cs`
  - Исходный код: `src/Electron2D/Runtime/Application/RuntimeSdlRendererFramePresenter.cs`
  - Тесты: `tests/Electron2D.Tests.Unit/Texture2DTests.cs`
  - Тесты: `tests/Electron2D.Tests.Integration/RuntimeHostTests.cs`

### Самодостаточное описание

Текущий `AtlasTexture` хранит `Atlas`, `Region`, `Margin` и `FilterClip`, но не выполняет полный контракт: `Margin` почти не влияет на размер и отрисовку, `FilterClip` не меняет выборку пикселей, `IsPixelOpaque()` не учитывает `Margin.Position`, а текущий механизм разрешения texture resource поддерживает только один уровень `AtlasTexture -> ImageTexture`. Это несовместимо с выбранным API-подмножеством Godot 4.7, где `Atlas` может быть любым `Texture2D`, включая другой `AtlasTexture`.

Нужное поведение: `AtlasTexture` должен работать как представление области другой texture с margin и правилами обрезки. Вложенные атласы должны разрешаться рекурсивно через общий механизм, а изменения `Atlas`, `Region`, `Margin`, `FilterClip` или вложенного ресурса должны инвалидировать texture cache. Методы отрисовки из `T-0226` должны корректировать source и destination так же, как базовый контракт Godot: обрезка source меняет видимый destination, а destination с разворотом остаётся корректным.

### Критерии приёмки

- [ ] `Atlas` принимает любой `Texture2D`, включая вложенный `AtlasTexture`.
- [ ] Прямая self-reference запрещена.
- [ ] `Region.Size` округляется вниз.
- [ ] Нулевая ось `Region.Size` берётся из соответствующей оси `Atlas`; поведение отсутствующего `Atlas` зафиксировано по исходному коду Godot.
- [ ] `Margin.Size` добавляется к итоговому размеру при ненулевом размере region.
- [ ] `Margin.Position` смещает видимую область и opacity mapping.
- [ ] `FilterClip` реально ограничивает выборку пикселей и предотвращает чтение соседних пикселей atlas region.
- [ ] `GetImage()` возвращает копию вырезанного региона исходной texture.
- [ ] `Draw(...)`, `DrawRect(...)` и `DrawRectRegion(...)` корректируют source и destination при частично обрезанном source и destination с разворотом.
- [ ] Основной и запасной presenter-ы используют один общий механизм, поддерживающий вложенные atlas resources.
- [ ] Изменение `Atlas`, `Region`, `Margin`, `FilterClip` или вложенного ресурса инвалидирует texture cache.
- [ ] Тесты проверяют поля `Margin`, дробный `Region`, вложенный atlas, частично обрезанный source, destination с разворотом и отсутствие чтения соседних пикселей.
- [ ] После зелёных проверок доменный документ обновлён фактическим поведением, ограничениями и командами проверки.

### Подзадачи

- [ ] Уточнить контракт `AtlasTexture` в `docs/rendering/texture-resource-baseline.md`.
- [ ] Добавить красные тесты на `Margin`, обрезку, вложенный atlas, область отрисовки и инвалидацию texture cache.
- [ ] Реализовать `AtlasTexture` поверх контракта отрисовки `T-0226`.
- [ ] Вынести общий рекурсивный механизм разрешения texture resource для presenter-ов.
- [ ] Запустить focused unit/runtime tests, source license verifier и `git diff --check`.

### Заметки агента

2026-06-25T14:32:09+03:00 - Создано как зависимая задача после `T-0226`. `T-0219` не может быть окончательно принят, пока механизм разрешения texture resource содержит специальную ветку только для `AtlasTexture -> ImageTexture`.

## T-0238 [ ] P1: Требовать полный инженерный audit и обязательное закрытие `RISKS_AND_NOTES`

- Создана: 2026-07-01T21:08:53+03:00
- Состояние: open
- Приоритет: P1
- Зависимости: T-0237
- Ссылки:
  - Запрос аудитору: `docs/release-management/AUDIT-REQUEST.md`
  - Доменный документ: `docs/release-management/audit-package.md`
  - Prompt цикла задачи: `.codex/prompts/goal-task-loop.md`
  - Исходный код: `eng/Electron2D.Build/AuditSubmitCommand.cs`; `eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`
  - Тесты: `tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`

### Самодостаточное описание

Каждый внешний audit run - primary r01, повторный primary r02+ и независимый control audit - должен выполнять полный review текущего audit package в пределах `metadata.scopeTaskIds`, `metadata.scopeSummary` и файлов declared scope. Аудитор должен проверять implementation/test/documentation/task compliance review, новые дефекты в текущей области, новые секреты, локальные пути, недостоверные evidence, regressions в тестах и документации, scope leaks и риски, созданные текущим изменением или исправлениями.

Primary r02+ дополнительно проверяет `metadata.previousVerdictChain`, previous verdict files, `metadata.blockerClosureList` и закрытие каждого previous blocker-а, но этот слой не заменяет полный review текущего package scope. Control audit после primary `ACCEPT` является независимым full current-scope review чистого контрольного ZIP для той же принятой области: без `metadata.previousVerdictChain`, без `metadata.blockerClosureList` и без сохранённых verdict-отчётов `docs/verdicts/`, а не проверкой того, что primary audit уже вернул `ACCEPT`.

Каждый content review должен быть не только полным по области, но и инженерно содержательным для игрового движка. Внешний аудит проверяет пригодность по производительности для горячих путей выполнения: игрового цикла, отрисовки, ввода, жизненного цикла `SceneTree`/`Node`, ресурсов, физики, запуска и экспорта; соответствие утверждённому 2D-профилю Godot 4.7 для задач Public API; полноценность публичного API как рабочего внутреннего механизма (`backend path`), а не витринной сигнатуры; реалистичность тестов; C# code style и лучшие практики по `AGENTS.md`; архитектурную согласованность с моделью узлов и ресурсов, общими путями отрисовки, ввода и ресурсов, а также идеей Electron2D как рабочего движка для небольшой законченной 2D-игры.

Для Public API задач аудит не должен сверять Godot 4.7 “по памяти”: нужен проверяемый источник parity evidence, например pinned upstream basis, API manifest diff, Wiki compatibility table или другой приложенный evidence. При этом parity проверяется только в пределах утверждённого 2D-профиля задачи; отсутствие API вне профиля не является blocker-ом, если out-of-scope явно подтверждён доменным документом или API manifest.

При этом acceptance-аудит конкретной задачи не должен блокироваться любой старой проблемой всего репозитория. Нужна явная классификация findings: текущие blocker-ы задачи блокируют verdict, а предсуществующие или out-of-scope проблемы записываются как follow-up findings с предложением существующей или новой задачи. Исключение составляют global safety issues: реальные секреты, потеря данных, license/build/release corruption и похожие критические проблемы могут блокировать текущий audit независимо от scope.

`RISKS_AND_NOTES` не является свободным текстом, который можно прочитать и забыть. Каждая actionable-запись из `RISKS_AND_NOTES` должна получить closure до закрытия текущей задачи: ссылка на существующую задачу, новая задача, `accepted-risk`, `duplicate`, `not-actionable` или promotion в blocker текущей задачи. Реализация follow-up задачи не требуется для закрытия текущей задачи, если finding не является blocker-ом текущего scope.

Closure state по умолчанию хранится в tracked `TASKS.md`: в секции закрываемой задачи или в созданной/обновлённой follow-up задаче. Отдельный tracked follow-up index допустим только если он явно описан в `docs/release-management/audit-package.md`. Closure note обязан ссылаться на saved report path и finding id, например `docs/verdicts/<domain>/<task-id>-audit-rNN.md` + `FOLLOW_UP_FINDING F1`, чтобы источник follow-up можно было восстановить без истории чата.

Closure verifier/helper должен иметь обязательную командную поверхность: либо расширять стандартную `verify docs`, либо предоставлять отдельную команду `verify audit-followups`, которая добавлена в `goal-task-loop.md` и финальные checks T-0238. Уникальный ключ finding-а - пара `(saved report path, finding id)`, потому `FOLLOW_UP_FINDING F1` из primary report и `FOLLOW_UP_FINDING F1` из control report являются разными findings.

Actionable semantics должны быть явными: `FOLLOW_UP_FINDING` считается actionable по умолчанию; `OUT_OF_SCOPE_NOTE` и `INFO_NOTE` не требуют closure, если не помечены как actionable; `ACCEPTED_RISK` является closure state только при наличии rationale и next decision point; global safety issue не является note и должен оставаться blocker-ом. Для `accepted-risk` closure формат должен быть совместим с risk-register моделью `T-0105`: source/id, affected area, impact, likelihood, mitigation, owner/next decision point и decision state.

### Критерии приёмки

- [ ] `docs/release-management/AUDIT-REQUEST.md` прямо говорит, что каждый external audit run - primary r01, primary r02+ и control audit - выполняет full current-scope review, а не только delivery-layer review или previous blockers closure.
- [ ] `AUDIT-REQUEST.md` говорит, что r02+ дополнительно проверяет `metadata.previousVerdictChain`, previous verdict files, `metadata.blockerClosureList` и closure previous blocker-ов, но эта проверка не заменяет full content review.
- [ ] `AUDIT-REQUEST.md` говорит, что control audit после primary `ACCEPT` является независимым full current-scope review чистого контрольного ZIP для той же принятой области, без прошлых verdict-отчётов в пакете и без rubber-stamp проверки primary verdict-а.
- [ ] `AUDIT-REQUEST.md` требует в каждом external audit run выполнять обязательные инженерные оси review: производительность, Public API parity, функциональную полноценность Public API, реалистичность тестов, C# code style/best practices и архитектурную согласованность.
- [ ] `AUDIT-REQUEST.md` явно говорит, что engineering axes применяются в пределах текущего package scope; старые repo-wide проблемы вне scope оформляются как follow-up, если они не внесены текущим изменением и не являются global safety issue.
- [ ] `AUDIT-REQUEST.md` говорит, что performance finding является `current-task blocker`, если текущий change добавляет или ухудшает горячий путь выполнения: игровой цикл, отрисовку, ввод, жизненный цикл `SceneTree`/`Node`, загрузку или импорт ресурсов, обновление физики, запуск/экспорт или Public API, вызываемый в кадре.
- [ ] `AUDIT-REQUEST.md` говорит, что для задач Public API аудитор обязан проверить соответствие утверждённому 2D-профилю Godot 4.7: публичное наследование, свойства, методы, overloads, events/signals, enum/constants, типы параметров и возвращаемых значений, значения по умолчанию, XML documentation, API manifest, Wiki compatibility и наблюдаемое поведение.
- [ ] Для Public API задач `AUDIT-REQUEST.md` требует проверяемую поверхность parity: pinned upstream basis для Godot 4.7, API manifest diff, Wiki compatibility table или другой приложенный evidence; отсутствие такого evidence считается evidence gap.
- [ ] `AUDIT-REQUEST.md` говорит, что Public API parity проверяется только в пределах утверждённого 2D-профиля задачи; отсутствие API вне профиля не является blocker-ом, если out-of-scope явно подтверждён доменным документом или API manifest.
- [ ] `AUDIT-REQUEST.md` говорит, что Public API без рабочего backend path, observable behavior, теста/примера и документации является blocker-ом, даже если форма API похожа на Godot.
- [ ] `AUDIT-REQUEST.md` различает performance blocker, performance evidence gap и follow-up: текущий change в горячем пути выполнения без нужных измерений или с очевидной регрессией блокирует задачу; performance-заявление без benchmark/runtime evidence является evidence gap; предсуществующий performance-долг вне scope уходит в structured `FOLLOW_UP_FINDING`.
- [ ] `AUDIT-REQUEST.md` уточняет performance evidence tiers: если задача заявляет performance improvement, нужны measurement/evidence; если задача меняет hot path без performance claim, аудитор проверяет очевидные regression-ы и требует benchmark/runtime evidence только при нетривиальном влиянии на frame/update/render path; если задача не касается runtime hot path, performance review ограничивается sanity check.
- [ ] `AUDIT-REQUEST.md` требует проверять test realism: тесты должны проходить через production code path или стабильный внутренний контракт, который реально используется runtime/editor/tooling; smoke-only, fixture-only, helper-only проверки, test-only branches или `does not throw` tests, не доказывающие заявленное поведение, являются blocker-ом для текущей задачи.
- [ ] `AUDIT-REQUEST.md` считает blocker-ом production branch, flag, fixture hook или fake backend, добавленный только для прохождения теста, если он не является документированным стабильным контрактом runtime/editor/tooling.
- [ ] `AUDIT-REQUEST.md` требует C# style/best-practices review по `AGENTS.md`: public XML documentation, role-based naming, resource lifetime, nullable/exception contracts, отсутствие placeholder comments, лишней reflection/dynamic logic, stringly-typed protocols вместо существующих моделей и test-only shortcuts.
- [ ] `AUDIT-REQUEST.md` требует architecture coherence review: изменение не должно обходить модель узлов и ресурсов, `ProjectWorkspace`, общий путь отрисовки, ввода и ресурсов, release/export contracts или идею Electron2D как рабочего движка для небольшой законченной 2D-игры.
- [ ] Performance/API/test-realism/style/architecture findings, которые не блокируют текущий scope, всё равно должны оформляться как structured `FOLLOW_UP_FINDING` с suggested existing/new task и closure state до архивирования текущей задачи.
- [ ] `AUDIT-REQUEST.md` классифицирует findings как `current-task blocker`, `follow-up finding`, `out-of-scope note` и `global safety blocker`.
- [ ] `AUDIT-REQUEST.md` требует структурировать `RISKS_AND_NOTES` как `FOLLOW_UP_FINDING F1`, `OUT_OF_SCOPE_NOTE N1`, `ACCEPTED_RISK R1`, `INFO_NOTE I1` или другой явно документированный non-blocking тип.
- [ ] `AUDIT-REQUEST.md` явно говорит, что finding является `current-task blocker`, если он внесён текущим изменением, находится в changed/affected files и нарушает acceptance criteria, делает тесты/документацию/evidence недостоверными, ломает previous blocker closure или создаёт secret/security/data-loss/license/build/release risk.
- [ ] `AUDIT-REQUEST.md` явно говорит, что finding является `follow-up finding` только если он предсуществовал, не ухудшен текущим изменением, не мешает acceptance criteria текущей задачи и не относится к global safety class.
- [ ] `AUDIT-REQUEST.md` требует записывать follow-up findings в `RISKS_AND_NOTES` с `Finding id`, `File/symbol`, `Problem`, `Why not blocker for current task`, `Suggested existing task` или `Suggested new task`, `Suggested priority` и `Verification idea`.
- [ ] `AUDIT-REQUEST.md` требует, чтобы `Suggested new task` был zero-context: title, priority, affected domain, short acceptance sketch и verification idea.
- [ ] `.codex/prompts/goal-task-loop.md` говорит агенту после final accepted state - primary `VERDICT: ACCEPT` плюс control `VERDICT: ACCEPT` - triage-ить follow-up findings: обновить подходящую активную задачу или создать новую, но не смешивать их с закрытием текущей задачи.
- [ ] `.codex/prompts/goal-task-loop.md` различает post-accept bookkeeping и изменение принятого scope: создание/обновление follow-up task по `RISKS_AND_NOTES` после final `ACCEPT` не требует нового audit текущей задачи, если не меняет code/docs/tests/evidence/criteria принятого изменения.
- [ ] `.codex/prompts/goal-task-loop.md` говорит, что после final accepted state агент обязан закрыть все actionable entries из `RISKS_AND_NOTES` через tracked task update, new task, `accepted-risk`, `duplicate`, `not-actionable` или promotion-to-blocker decision.
- [ ] Текущая задача не может быть перенесена в `data/completed-tasks/**`, пока actionable `FOLLOW_UP_FINDING` из saved primary/control reports не имеет closure note в `TASKS.md` или в отдельном tracked follow-up index.
- [ ] `eng/Electron2D.Build` содержит task/docs verifier или closure helper, который отклоняет архивирование задачи или состояние `closed`, если saved primary/control audit reports содержат actionable `FOLLOW_UP_FINDING` без closure note в `TASKS.md` или явно описанном tracked follow-up index.
- [ ] Closure helper/verifier имеет обязательную командную поверхность: либо расширяет `verify docs`, либо предоставляет `verify audit-followups`, добавленную в `.codex/prompts/goal-task-loop.md` и финальные checks T-0238.
- [ ] Closure helper считает finding уникальным по паре `(saved report path, finding id)`, поэтому `FOLLOW_UP_FINDING F1` из primary report и `FOLLOW_UP_FINDING F1` из control report являются разными findings.
- [ ] Parser/helper явно определяет actionable semantics: `FOLLOW_UP_FINDING` actionable по умолчанию; `OUT_OF_SCOPE_NOTE` и `INFO_NOTE` не требуют closure без explicit actionable marker; `ACCEPTED_RISK` закрывает finding только при наличии rationale и next decision point; global safety issue остаётся blocker-ом.
- [ ] `accepted-risk` closure совместим с risk-register моделью `T-0105`: source/id, affected area, impact, likelihood, mitigation, owner/next decision point и decision state.
- [ ] Closure note по каждому actionable finding указывает saved report path, finding id, closure state и target task/rationale: `tracked-existing`, `tracked-new`, `accepted-risk`, `duplicate`, `not-actionable` или `promoted-blocker`.
- [ ] `audit submit` validator допускает structured follow-up findings в `RISKS_AND_NOTES` и не путает их с numbered blockers `B1`..`Bn`.
- [ ] `audit submit` validator допускает `VERDICT: ACCEPT` с `FOLLOW_UP_FINDING F1` в `RISKS_AND_NOTES`, но не считает такой report достаточным для task closure без последующего agent-side follow-up closure step.
- [ ] Focused tests покрывают: `VERDICT: ACCEPT` с `FOLLOW_UP_FINDING F1` в `RISKS_AND_NOTES` допустим; `VERDICT: ACCEPT` с `B1` в `BLOCKERS` остаётся запрещённым; parser/extractor находит follow-up findings в `RISKS_AND_NOTES`; closure helper отличает closed и unclosed follow-up findings; `B1`-like текст в `RISKS_AND_NOTES` допустим только как явно not-blocking follow-up context.
- [ ] Focused tests покрывают actionable `OUT_OF_SCOPE_NOTE` и `INFO_NOTE` с explicit marker: без closure note `verify audit-followups` падает, после closure note проходит.
- [ ] `audit submit --download-report-only` сохраняет уже готовую отчётную карточку, даже если ниже по обсуждению есть видимый Deep Research iframe без готового отчёта; page-level export прокручивает единственную report-card export-кнопку в видимую область и не использует координатный клик.
- [ ] `audit submit` по умолчанию отправляет ZIP как обычный ChatGPT-запрос: текст из `AUDIT-REQUEST.md` не содержит `@Глубокое исследование`, режим `Глубокое исследование` не выбирается, а verdict извлекается только из нового ответа ассистента текущей отправки через штатную кнопку `copy-turn-action-button` под этим ответом, controlled system/browser clipboard proof с успешным sentinel или captured `writeText()`/`write(...)`/`copy` event value и без baseline-восстановления Markdown из DOM `innerText`/`textContent`; произвольный старый clipboard text не является источником verdict-а.
- [ ] `audit submit --deep-research` выбирает пункт `Глубокое исследование` через кнопку `+` не только по видимому тексту, но и по стабильным connector-атрибутам `connector_openai_deep_research`; выбор выполняется до вставки текста, а после вставки команда повторно проверяет отдельную выбранную плашку.
- [ ] `audit submit --new-conversation` штатно создаёт новый primary chat проекта после локально зафиксированной невозможности продолжать старое обсуждение через поддержанный submit-путь, всегда использует корневой URL проекта из конфигурации, отклоняет любой явно переданный `--project-url` и не совмещается с `--reuse-conversation`, `--control-audit` или `--download-report-only`.
- [ ] `FillPromptExpression` сохраняет выбранную плашку `Глубокое исследование` при вставке текста prompt-а для всех поддержанных вариантов selected-pill metadata, включая `[data-keyword="Deep Research"][data-inline-selection-pill]`.
- [ ] `audit submit` принимает `VERDICT: ACCEPT`, если первая строка полного отчёта равна `VERDICT: ACCEPT`, обязательные секции присутствуют в нужном порядке, `TASK_ASSESSMENT:` явно называет текущие `metadata.taskId` и `metadata.iteration`, а `BLOCKERS:` не содержит `B1`..`Bn`; `CLOSURE_DECISION:` не требует специальной фразы после `ACCEPT`.
- [ ] Все режимы `audit submit`, которые записывают verdict Markdown, принимают только строгий repo-relative `--out`: primary `docs/verdicts/<domain>/<task-id>-audit-rNN.md`, control `docs/verdicts/<domain>/<task-id>-audit-control-rNN.md`, точные сегменты `docs/verdicts`, точное расширение `.md`, без произвольного префикса, absolute path, пустых сегментов, `.`, `..` и вариаций регистра фиксированных сегментов или расширения; обычный submit сверяет filename с текущими `taskId`/`iteration` из ZIP и primary/control mode, а `--download-report-only` принимает только primary filename.
- [ ] `audit submit --control-audit` до подключения к браузеру отклоняет ZIP с массивами `metadata.previousVerdictChain` или `metadata.blockerClosureList` любой длины кроме `0`, а также ZIP, где `repo-after/`, `repo-before/`, `repo-file-hashes.json` включая `deletedRepoFiles` или `metadata/repo-file-snapshots.json` включают сохранённые verdict-отчёты `docs/verdicts/`.
- [ ] `DeepResearchSelectedExpression` отвергает фактические plain строки меню ChatGPT `.__menu-item` и `[data-fill][tabindex]`, а также их потомков, чтобы меню не считалось выбранной плашкой `Глубокое исследование`.
- [ ] Цикл выбора `Глубокое исследование` покрыт поведенческим тестом через внутренний драйвер production logic: если пункт меню уже открыт, команда кликает его и не нажимает `+` повторно в этом цикле.
- [ ] `audit submit --deep-research` после baseline старых Deep Research targets может выбрать последний готовый non-baseline target в reused-чате, но скачанный Markdown всё равно проходит строгую проверку текущих `taskId` и `iteration`.
- [ ] `audit submit --deep-research` не экспортирует старый ready target, если после него есть более новый non-baseline Deep Research target, который ещё генерируется; в этом случае команда ждёт текущий отчёт.
- [ ] Markdown-export selector принимает фактические plain строки меню ChatGPT `.__menu-item` и `[data-fill][tabindex]`, чтобы скачивание отчёта не зависело от старого `role="menuitem"`.
- [ ] `--dump-dom-only` покрыт поведенческим regression-тестом production-последовательности через внутренний драйвер: тест проверяет, что `frame-tree.json`, `target-info.json`, `deep-research-selected-result.json` и `deep-research-selected-diagnostics.json` записываются до iframe-зависимых шагов.
- [ ] `--dump-dom-only` до подключения к Chrome отклоняет `--out` под `docs/verdicts/**/<task-id>-audit-rNN.md`, чтобы DOM-dump summary не мог попасть на место сохранённого verdict report.
- [ ] Если кнопка `Экспорт` нажата внутри выбранного Deep Research frame или target, пункт Markdown menu можно искать в основном DOM страницы, потому ChatGPT может отрисовать это меню поверх всей страницы; fallback не должен нажимать другую кнопку экспорта или выбирать чужую отчётную карточку.
- [ ] После реализации проходят focused integration tests, documentation checks, license verifier и `git diff --check`.

### Подзадачи

- [ ] Уточнить `AUDIT-REQUEST.md` и `audit-package.md`: каждый audit run делает full current-scope engineering review, r02+ добавляет previous-blocker layer, control audit остаётся независимым full review, findings классифицируются как blocker/follow-up/out-of-scope/global safety, а `RISKS_AND_NOTES` требует closure state.
- [ ] Добавить в `AUDIT-REQUEST.md` обязательные инженерные оси review: производительность, Public API/Godot 4.7 parity для Public API задач, функциональная полноценность Public API, test realism, C# style/best practices и architecture coherence.
- [ ] Уточнить evidence boundaries: Public API parity требует pinned evidence и ограничивается утверждённым 2D-профилем; performance findings делятся на blocker, evidence gap и follow-up; test-only shortcuts блокируют текущую задачу.
- [ ] Реализовать task/docs verifier или closure helper для unclosed actionable `FOLLOW_UP_FINDING`, включая обязательную командную поверхность, проверку saved report path, finding id, closure state, unique key `(saved report path, finding id)` и actionable semantics.
- [ ] Обновить `goal-task-loop.md`: triage follow-up findings после final primary/control `ACCEPT`, с отдельным правилом для post-accept bookkeeping и запретом архивировать задачу до closure всех actionable notes.
- [ ] Добавить focused tests на валидатор отчёта и документационный контракт.
- [ ] Закрыть primary r01 blockers: убрать scope leak по `T-0104`, добавить focused coverage для actionable `OUT_OF_SCOPE_NOTE`/`INFO_NOTE` и пересобрать r02 с `previousVerdictChain`.
- [x] Закрыть primary r02 blocker: добавить поведенческий DOM regression, который исполняет тот же JavaScript экспорта из собранного `Electron2D.Build.dll` на контролируемой странице с видимым, но неготовым Deep Research iframe и кнопкой экспорта отчётной карточки, расположенной вне видимой области.
- [x] Исправить ложный отказ r03 submit: `CLOSURE_DECISION` с фразой "проверяемый пакет T-0238 r03 можно закрыть" должен проходить строгий валидатор ACCEPT-отчёта.
- [x] Уточнить control audit по требованию пользователя: после primary `ACCEPT` контроль должен получать чистый контрольный ZIP той же принятой области без прошлых verdict-отчётов, `previousVerdictChain` и `blockerClosureList`; `audit submit --control-audit` должен отклонять нарушение до запуска браузера.
- [x] Закрыть pre-send отказ r05: `audit submit` должен выбирать Deep Research по connector-метаданным, если пункт меню не даёт стабильного видимого текста.
- [x] Закрыть r10 export failure: `audit submit --download-report-only` должен принимать верхнюю кнопку отчёта с явным `aria-haspopup="menu"` и подписью `Экспорт`/`Export`/`Скачать`/`Download` даже без прежнего SVG-path, а Markdown-пункт экспорта должен поддерживать ARIA `menuitem`.
- [x] Закрыть pre-send отказ r22: `audit submit --new-conversation` должен надёжно выбирать настоящий пункт `Глубокое исследование` через меню `+`, кликать интерактивную строку меню внутри широкого popover-контейнера, не считать текст `@Глубокое исследование` выбранным режимом и сохранять connector-плашку при заполнении prompt-а даже без старого `data-inline-selection-pill`.
- [x] Закрыть primary r24 blockers B1/B2: `DeepResearchItemPointExpression` должен вычислять область меню от той же видимой и доступной кнопки `+`, которую нажимает `DeepResearchMenuPointExpression`, а `DeepResearchSelectedExpression` не должен считать скрытую connector-метку внутри prompt-а выбранным режимом.
- [x] Закрыть primary r25 blockers B1/B2: `DeepResearchSelectedExpression` не должен считать сам контейнер prompt-а выбранной плашкой из-за вложенного `button` или `role="menuitem"` с connector metadata, а `audit-package.md` должен описывать один непротиворечивый контракт selected-state без обязательности старого `data-inline-selection-pill` для новых вариантов интерфейса.
- [x] Закрыть primary r27 blockers B1/B2: `DeepResearchSelectedExpression` должен отвергать connector metadata внутри интерактивных предков `button`, `role="button"`, `role="menuitem"` и `role="option"`, включая inline-marker branch, а `AUDIT-REQUEST.md` должен явно требовать zero-context состав для `Suggested new task`: заголовок, приоритет, домен, краткий критерий приёмки и идею проверки.
- [x] Закрыть r28 pre-send отказ: цикл выбора `Глубокое исследование` сначала проверяет уже открытый пункт меню и не закрывает его повторным нажатием `+`; selector принимает фактические строки меню ChatGPT `.__menu-item` / `data-fill` + `tabindex` без `role="menuitem"`.
- [x] Закрыть primary r29 blockers B1/B2: selected-state отвергает plain строки меню `.__menu-item` / `[data-fill][tabindex]` и их потомков, закрытие r28 submit-loop failure доказано поведенческим тестом внутреннего driver-контракта, Markdown-export selector принимает такие же plain строки меню, а обычный downloader path выбирает последний готовый non-baseline target после baseline и проверяет текущую итерацию строгим валидатором.
- [x] Зафиксировать r30 local stale-download failure и helper-level guard: `AuditSubmitReadyTargetSelectionWaitsForNewestNonBaselineTarget` доказал, что выбор target id ждёт более новый `ready=False` target, но этот helper-only шаг был недостаточен без r31/r32 driver-flow closure ниже.
- [x] Закрыть primary r31 blockers B1/B2: ordinary submit теперь блокирует page-level export fallback, когда более новый non-baseline target ещё генерируется; behavior-level regression `AuditSubmitReportCandidateFlowBlocksPageFallbackWhileNewestTargetGenerates` исполняет production candidate flow через внутренний driver-контракт и доказывает, что page fallback не вызывается до готовности newest target.
- [x] Закрыть primary r33 `FOLLOW_UP_FINDING F1`: source-level проверка `--dump-dom-only` заменена поведенческим regression-тестом `AuditSubmitDumpDomOnlyWritesBaseDomBeforeDeepResearchFrameDiagnostics`, который вызывает production-путь `DumpDomFromUrlAsync` через внутренний `IAuditSubmitDomDumpDriver` и проверяет созданные diagnostic JSON-файлы на диске.
- [x] Закрыть r34 local export recovery failure: Markdown menu fallback разрешён для выбранных Deep Research frame/target surfaces после клика их кнопки `Экспорт`, потому реальный DOM может отрисовать меню в основном документе страницы; regression `AuditSubmitAllowsPageMarkdownMenuFallbackForSelectedDeepResearchSurface` сначала падал на старом запрете и теперь проходит.
- [x] Закрыть r34 local dump/report mix-up: `--dump-dom-only` больше не принимает `--out` вида `docs/verdicts/**/<task-id>-audit-rNN.md`, поэтому диагностический DOM summary не может быть записан вместо сохранённого внешнего отчёта; regression `AuditSubmitDumpDomOnlyRejectsVerdictOutputBeforeBrowserLaunch` сначала падал на попытке подключиться к Chrome и теперь проходит до браузера.
- [x] Закрыть r35 local Deep Research menu toggle failure: если composer-меню кнопки `+` уже открыто, но пункт `Глубокое исследование` ещё не найден, `audit submit` ждёт следующую проверку и не нажимает `+` повторно, чтобы не закрыть меню на медленной отрисовке; regression `AuditSubmitDeepResearchSelectionWaitsWhenComposerMenuIsAlreadyOpen` сначала падал на лишнем `TryOpenMenuAsync`, затем прошёл.
- [x] Закрыть primary r36 blockers B1/B2/B3: page-level Markdown fallback для выбранного Deep Research frame/target запрещён до клика выбранной кнопки `Экспорт`, `audit-package.md` описывает тот же порядок без противоречий, а `DeepResearchComposerMenuOpenExpression` считает меню открытым только по видимым строкам меню возле кнопки `+`, а не по одному `aria-expanded="true"`.
- [x] Закрыть r37 local pre-send Deep Research expanded-menu failure: если кнопка `+` уже сообщает `aria-expanded="true"`, но строки меню ещё не видны, `audit submit` ждёт следующую проверку и не нажимает `+` повторно; это не считается открытым меню и не нарушает r36 B3.
- [x] Закрыть r38 local pre-send Deep Research slow-menu failure: после успешного клика по кнопке `+`, если пункт `Глубокое исследование` ещё не найден и строки меню не видны, `audit submit` несколько циклов ждёт появления меню и не нажимает `+` повторно, чтобы не закрыть меню на медленной странице.
- [x] По требованию пользователя после отката к r45 сделать обычный ChatGPT-запрос базовым режимом `audit submit`: убрать `@Глубокое исследование` из отправляемого текста, оставить Deep Research только через `--deep-research`, выбирать кнопку `Глубокое исследование` до вставки текста в этом резервном режиме и добавить проверки ordinary/deep порядка отправки.

### Заметки агента

2026-07-01T21:08:53+03:00 - Создано по замечанию пользователя: повторный аудит должен искать новые проблемы в текущем package scope, но старые или out-of-scope проблемы должны попадать в follow-up tracking, а не бесконечно блокировать acceptance текущей задачи.

2026-07-01T21:47:00+03:00 - Уточнено по замечанию пользователя: full current-scope review и follow-up classification должны применяться к каждому external audit run, включая primary r01 и control audit; r02+ отличается только дополнительной проверкой previous verdict chain и blocker closure.

2026-07-01T21:57:00+03:00 - Уточнено по замечанию пользователя: actionable entries из `RISKS_AND_NOTES` должны закрываться triage-решением до архивирования текущей задачи; реализация follow-up задач не требуется, но каждая запись должна получить linked task, new task, accepted-risk, duplicate, not-actionable или promotion-to-blocker state.

2026-07-01T22:33:43+03:00 - Уточнено по замечанию пользователя: полный audit должен быть инженерным для игрового движка, а не только формально полным по файлам. В T-0238 добавлены обязательные оси review: производительность, Godot 4.7 Public API parity для Public API задач, функциональная полноценность Public API, реалистичность тестов, C# code style/best practices и архитектурная согласованность с идеей Electron2D.

2026-07-01T22:44:07+03:00 - Уточнено по приложенной оценке T-0238: Public API parity должен опираться на pinned evidence и ограничиваться утверждённым 2D-профилем задачи; performance findings делятся на blocker/evidence gap/follow-up; test-only branches, flags, fixture hooks или fake backend-ы без стабильного контракта считаются blocker-ом; инженерные findings вне текущего scope всё равно требуют structured `FOLLOW_UP_FINDING` и closure state.

2026-07-01T22:51:21+03:00 - Уточнено по новой оценке T-0238: closure `RISKS_AND_NOTES` должен быть tool-enforced, а не только prompt rule. В задачу добавлен verifier/helper gate, основной формат closure note через `TASKS.md`, обязательная ссылка на saved report path + finding id и уточнение performance evidence tiers, чтобы benchmark требовался для performance claim или нетривиального hot path change, а не для любой задачи.

2026-07-01T22:57:15+03:00 - Уточнено по новой оценке T-0238: closure verifier/helper должен иметь обязательную командную поверхность через `verify docs` или `verify audit-followups`, finding уникален по паре `(saved report path, finding id)`, actionable semantics зафиксированы для `FOLLOW_UP_FINDING`, `OUT_OF_SCOPE_NOTE`, `INFO_NOTE`, `ACCEPTED_RISK` и global safety blockers, а `accepted-risk` closure должен быть совместим с risk-register моделью `T-0105`.

2026-07-01T23:54:00+03:00 - Реализован локальный scope T-0238 перед внешним аудитом: `AUDIT-REQUEST.md` требует full current-scope engineering review для primary/control запусков, `audit-package.md` описывает `verify audit-followups`, `.codex/prompts/goal-task-loop.md` требует post-accept triage actionable `RISKS_AND_NOTES`, а `eng/Electron2D.Build` добавляет CLI-команду `verify audit-followups`. После worker-а добавлен RED/GREEN regression test: Git-workspace с untracked saved verdict report теперь не пропускается verifier-ом, потому команда объединяет tracked пути с фактическими файлами под `docs/verdicts/**/*.md`. Локально прошли focused tests 7/7, marker/prompt tests 15/15, `dotnet build eng\Electron2D.Build\Electron2D.Build.csproj --no-restore -v:minimal`, `update docs`, `update docs --check`, `verify docs`, `verify audit-followups`, `verify licenses` и `git diff --check` с прежним предупреждением Git о будущей LF-нормализации `docs/release-management/audit-package.md`. Внешняя упаковка и отправка ещё не выполнялись.

2026-07-02T00:35:00+03:00 - Primary audit r01 сохранён штатной командой как `docs/verdicts/release-management/t-0238-audit-r01.md` с `VERDICT: NEEDS_FIXES`. Blocker B1 закрыт: несвязанный перевод `T-0104` в `in progress` удалён из diff, `T-0104` снова `open`. Blocker B2 закрыт: добавлены focused tests для actionable `OUT_OF_SCOPE_NOTE` и `INFO_NOTE`, где `verify audit-followups` падает без closure note и проходит после closure note. Дополнительно исправлен `audit submit --download-report-only`: готовая report-card выше по обсуждению теперь не блокируется видимым неготовым Deep Research iframe ниже, а page-level export прокручивает единственную найденную report-card export-кнопку и кликает DOM-методом без координатного обхода. Локальный conversation state хранится в `.temp/audit/T-0238/conversation-url.txt`; конкретный URL в `TASKS.md` не записывается.

2026-07-02T01:16:00+03:00 - Primary audit r02 сохранён штатной командой как `docs/verdicts/release-management/t-0238-audit-r02.md` с `VERDICT: NEEDS_FIXES`. Blocker B1 закрыт тестом `AuditSubmitDownloadReportOnlyPageFallbackExecutesProductionDomExportWhenFrameIsNotReady`: тест получает `DeepResearchReportTargetReadyExpression` и `ReportExportButtonClickExpression` из собранной `Electron2D.Build.dll`, исполняет их на контролируемой DOM-модели и проверяет, что неготовый Deep Research iframe не выбирается как отчётная поверхность, а экспорт из основного DOM страницы прокручивает и кликает единственную кнопку экспорта отчётной карточки, расположенную вне видимой области. Следующий пакет должен быть r03 с `previousVerdictChain` для r01 и r02 и closure B1 в `blockerClosureList`.

2026-07-02T02:33:00+03:00 - Primary r03 был отправлен штатной командой и report фактически готов, но не сохранён как `docs/verdicts/release-management/t-0238-audit-r03.md`: `audit submit --download-report-only` скачал Markdown с первой строкой `VERDICT: ACCEPT`, затем строгий валидатор вернул `E2D-BUILD-AUDIT-SUBMIT-REPORT-INVALID`, потому `CLOSURE_DECISION` сформулирован как "Проверяемый пакет T-0238 r03 можно закрыть...", а прежнее регулярное выражение принимало только форму "пакет можно закрыть" без уточнения между словами. Снят DOM dump `.temp/audit/T-0238/dom-dump-r03-primary`; это диагностический временный артефакт, не saved verdict. Исправлен валидатор и добавлен regression в `AuditSubmitReportExtractorRequiresSingleOpenedReportCardCandidate`; следующий внешний пакет должен быть r04, потому код изменился после r03 package.

2026-07-02T02:48:50+03:00 - По требованию пользователя уточнён control audit: контрольная проверка с нулевым контекстом не должна получать прошлые verdict-отчёты, `previousVerdictChain` или `blockerClosureList`, потому такие материалы могут сместить фокус аудитора с поиска проблем на чтение прошлых решений. `audit submit --control-audit` теперь до подключения к браузеру отклоняет ZIP с непустыми `metadata.previousVerdictChain` или `metadata.blockerClosureList`, а также с сохранёнными verdict-отчётами `docs/verdicts/` в `repo-after/`, `repo-before/` или `repo-file-hashes.json`. После primary `ACCEPT` нужен отдельно собранный и проверенный чистый контрольный ZIP той же rNN и той же принятой области; если меняются код, документы, тесты, доказательства или критерии, требуется новый primary package.

2026-07-02T03:55:00+03:00 - Primary audit r04 сохранён штатным `audit submit --download-report-only` как `docs/verdicts/release-management/t-0238-audit-r04.md` с `VERDICT: NEEDS_FIXES`. Blocker B1 закрывается строгой проверкой длины JSON-массивов: `metadata.previousVerdictChain` и `metadata.blockerClosureList` должны иметь ровно 0 элементов, поэтому whitespace-only, `null`, числовые и object elements больше не проходят. Blocker B2 закрывается рекурсивной проверкой строк в `repo-file-hashes.json` и `metadata/repo-file-snapshots.json`, включая `deletedRepoFiles`, `repo-after/docs/verdicts/` и `repo-before/docs/verdicts/`. Добавлены focused tests на все bypass-сценарии; следующий пакет должен быть r05 с r01/r02/r04 в `previousVerdictChain` и closure B1/B2 r04 в `blockerClosureList`.

2026-07-02T04:34:00+03:00 - Primary r05 package был собран и проверен на отдельной clean repo, но primary submit не отправил ZIP: `audit submit --reuse-conversation` трижды завершился до отправки диагностикой `E2D-BUILD-AUDIT-SUBMIT-DEEP-RESEARCH-MISSING`, потому пункт Deep Research не удалось выбрать из меню `+`. Снят штатный DOM dump `.temp/audit/T-0238/dom-dump-r05-submit-missing-deep-research`; dump показал доступные composer и кнопку `composer-plus-btn`, но меню выбора в нём уже закрыто. Исправлен pre-send selector: `DeepResearchItemPointExpression` теперь распознаёт пункт по `data-id`, `data-system-hint-type` и `data-keyword` для `connector_openai_deep_research`; добавлен behavior-level тест `AuditSubmitDeepResearchItemSelectorUsesConnectorMetadataWhenVisibleTextIsMissing`, который исполняет production JS на синтетическом DOM без видимого текста пункта. Следующий внешний пакет должен быть r06, потому код изменился после r05 package; r05 не имеет saved verdict report и не добавляется в `previousVerdictChain`.

2026-07-02T05:07:00+03:00 - Primary r06 package был собран и проверен, но primary submit снова завершился до отправки ZIP диагностикой `E2D-BUILD-AUDIT-SUBMIT-DEEP-RESEARCH-MISSING`; saved r06 verdict report не создан. Дополнительная диагностика через `--dump-dom-only` не завершилась из-за recoverable CDP detachment и тайм-аутов `Page.enable`, поэтому r06 не даёт внешнего verdict. Закрыт следующий вероятный pre-send дефект: `DeepResearchSelectedExpression` больше не требует, чтобы выбранный connector был потомком `#prompt-textarea`, и распознаёт `connector_openai_deep_research` как соседний pill-элемент рядом с composer по `data-id`, `data-system-hint-type` или `data-keyword`. Добавлен behavior-level тест `AuditSubmitDeepResearchSelectedSelectorFindsConnectorMetadataSiblingNearPrompt`; следующий внешний пакет должен быть r07, потому код изменился после verified r06 package.

2026-07-02T05:38:00+03:00 - Primary r07 package был собран, проверен и отправлен дальше прежнего pre-send blocker-а: sidecar `conversation-url-r07.txt` записан. Основной submit завершился диагностикой `E2D-BUILD-AUDIT-SUBMIT-CONVERSATION-MISSING`, затем две штатные попытки `--download-report-only` по r07 sidecar завершились `E2D-BUILD-AUDIT-SUBMIT-REPORT-EXPORT-MISSING`; saved r07 verdict report не создан. Локальная проверка показала, что Markdown-файл не появился ни в `.temp/audit`, ни в пользовательской папке Downloads. Закрыт следующий export-path дефект: report export теперь принимает подписи `Скачать`/`Download` для верхней кнопки с иконкой скачивания, принимает Markdown-пункты вида `Скачать как Markdown` / `Download as Markdown` и после клика верхней кнопки проверяет прямой Markdown-download без промежуточного пункта меню. Добавлен behavior-level тест `AuditSubmitMarkdownMenuSelectorAcceptsDownloadMarkdownLabel`; следующий внешний пакет должен быть r08, потому код изменился после verified r07 package.

2026-07-02T06:23:00+03:00 - Primary r08 package был собран, проверен и отправлен; sidecar `conversation-url-r08.txt` записан. Основной submit снова завершился `E2D-BUILD-AUDIT-SUBMIT-CONVERSATION-MISSING`; первая read-only попытка дала CDP detach, затем страница была ещё `REPORT-STILL-GENERATING`, а после ожидания готовой страницы `--download-report-only` снова завершился `E2D-BUILD-AUDIT-SUBMIT-REPORT-EXPORT-MISSING`; saved r08 verdict report не создан. Markdown-файл не появился в управляемом temp или Downloads. Закрыт следующий export-path дефект: DOM-активация кнопки экспорта и Markdown-пункта теперь отправляет `pointerdown`/`mousedown`/`pointerup`/`mouseup` на найденный DOM-элемент перед `element.click()`, оставаясь без координатного клика по карточке. Targeted export/menu tests прошли 3/3; следующий внешний пакет должен быть r09, потому код изменился после verified r08 package.

2026-07-02T07:36:00+03:00 - Primary r09 package был собран, проверен и отправлен; sidecar `conversation-url-r09.txt` записан. Основной submit завершился `E2D-BUILD-AUDIT-SUBMIT-CONVERSATION-MISSING`; первая read-only попытка вернула `E2D-BUILD-AUDIT-SUBMIT-REPORT-STILL-GENERATING`, повтор после ожидания завершился `E2D-BUILD-AUDIT-SUBMIT-REPORT-EXPORT-MISSING`; saved r09 verdict report не создан. Штатный DOM dump `.temp/audit/T-0238/dom-dump-r09-report-export-missing` показал, что в одном обсуждении накоплены старые ready Deep Research targets, а page-level DOM не содержит настоящей export-кнопки отчёта. Закрыт следующий tooling дефект: `--download-report-only` больше не игнорирует все targets существующего обсуждения, read-only target selection выбирает последний ready target при нескольких готовых targets, selected-state Deep Research требует `data-inline-selection-pill`, readiness gate принимает `Скачать`/`Download` и offscreen export/download buttons, а direct top-button Markdown download branch покрыт behavior-level тестом. Следующий внешний пакет должен быть r10, потому код, тесты и документ изменились после verified r09 package.

2026-07-02T08:18:00+03:00 - Primary r10 package был собран, проверен на отдельной clean repo и отправлен; sidecar `conversation-url-r10.txt` записан. Основной submit завершился `E2D-BUILD-AUDIT-SUBMIT-CONVERSATION-MISSING`; read-only recovery сначала вернул `E2D-BUILD-AUDIT-SUBMIT-REPORT-STILL-GENERATING`, а после ожидания завершился `E2D-BUILD-AUDIT-SUBMIT-REPORT-EXPORT-MISSING`; saved r10 verdict report не создан. Штатный DOM dump `.temp/audit/T-0238/dom-dump-r10-report-export-missing` показал готовый Deep Research frame с отчётным текстом и верхней кнопкой `Экспорт` (`aria-haspopup="menu"`), но без прежнего SVG-path, по которому selector раньше подтверждал экспорт. Закрыт следующий tooling дефект: report export readiness/click selector принимает явную menu-popup подпись экспорта без старой SVG-подписи, а Markdown menu selector поддерживает ARIA `menuitem`. Добавлены RED/GREEN behavior-level tests `AuditSubmitReportExportButtonAcceptsMenuPopupLabelWithoutLegacySvgPath` и `AuditSubmitMarkdownMenuSelectorAcceptsAriaMenuItemLabel`; следующий внешний пакет должен быть r11, потому код и тесты изменились после verified r10 package.

2026-07-02T09:02:00+03:00 - Primary r11 package был собран, проверен и отправлен; sidecar `conversation-url-r11.txt` записан. Основной submit завершился `E2D-BUILD-AUDIT-SUBMIT-CONVERSATION-MISSING`; две read-only recovery попытки по sidecar не дошли до export и завершились `E2D-BUILD-AUDIT-SUBMIT-CODEX-CHROME-PROTOCOL` из-за повторных `Page.enable` timeout после recoverable CDP detachment; штатный `--dump-dom-only` также упал на `Detached while handling command`. Saved r11 verdict report не создан. При проверке read-only ветки найден дополнительный дефект: `DownloadReportFromUrlAsync` всё ещё вызывал `SnapshotDeepResearchTargetIdsAsync` до навигации в существующий conversation URL, из-за чего уже готовые Deep Research targets могли попадать в ignore-set вопреки заявленному r10/r11 контракту. Закрыто RED/GREEN тестом `AuditSubmitDownloadReportOnlyDoesNotBaselineIgnoreExistingConversationTargets`; следующий внешний пакет должен быть r12, потому код и тесты изменились после verified r11 package.

2026-07-02T09:40:00+03:00 - Primary r12 package был собран, проверен и отправлен; sidecar `conversation-url-r12.txt` записан. Основной submit завершился `E2D-BUILD-AUDIT-SUBMIT-CODEX-CHROME-PROTOCOL` после повторных `Page.enable` timeout; первая read-only recovery попытка вернула `E2D-BUILD-AUDIT-SUBMIT-REPORT-STILL-GENERATING`, а повтор после ожидания сохранил Markdown с `VERDICT: NEEDS_FIXES`, который по содержанию оказался старой отчётной карточкой r04, а не результатом текущего r12 package. Этот stale output удалён как невалидный текущий verdict report и не добавляется в `previousVerdictChain`. Закрыт следующий tooling дефект: `audit submit --download-report-only` теперь выводит ожидаемые task/iteration из стандартного имени `--out` и отклоняет Markdown, который явно ссылается на evidence или ZIP другой итерации, диагностикой `E2D-BUILD-AUDIT-SUBMIT-REPORT-STALE` до записи файла. `AUDIT-REQUEST.md` дополнительно требует в `TASK_ASSESSMENT` явно называть `metadata.taskId` и `metadata.iteration`. Следующий внешний пакет должен быть r13, потому код, тесты и документы изменились после verified r12 package.

2026-07-02T11:50:13+03:00 - r13 не был отправлен во внешний аудит: локальная проверка `audit package verify` на отдельной clean repo остановилась с `E2D-BUILD-AUDIT-RESTORE-MISMATCH`, потому после восстановления остались лишние игнорируемые файлы `eng/Electron2D.Build/obj/Debug/net10.0/...`. Закрыто локально без роста внешнего `rNN`: `audit package verify` теперь включает `core.longPaths=true`, защищает ожидаемые восстановленные игнорируемые файлы от удаления родительского каталога и рекурсивно удаляет лишние игнорируемые каталоги. По замечанию пользователя `AUDIT-REQUEST.md` заменён на человекочитаемую редакцию: русский текст стал основным, служебные имена оставлены как техническая привязка, follow-up поля видимы по-русски (`Идентификатор`, `Где найдено`, `Почему не блокирует текущую задачу`, `Куда перенести`, `Рекомендуемый приоритет`, `Как проверить`). Локальный коридор перед следующей упаковкой прошёл без нового ZIP: сборки `eng\Electron2D.Build` и integration tests успешны; cleanup regression tests 4/4; `AuditSubmit` 74/74; documentation/source contract tests 14/14; static request/package tests 26/26; `AuditPackageMessage` tests пройдены малыми группами 2/2, 1/1, 1/1 и 4/4; `update docs`, `update docs --check`, `verify docs`, `verify audit-followups`, `verify licenses` и `git diff --check` успешны. `git diff --check` сохранил только предупреждение Git о будущей LF-нормализации `docs/release-management/audit-package.md`. Следующий внешний пакет должен быть r14, потому код, тесты и документы изменились после неотправленного r13 package.

2026-07-02T12:32:46+03:00 - r14 package был собран и проверен на clean repo, но внешний запуск нельзя считать валидным: два primary submit падали с `E2D-BUILD-AUDIT-SUBMIT-CODEX-CHROME-PROTOCOL`, затем появился `conversation-url-r14.txt`, а `--download-report-only` вернул `E2D-BUILD-AUDIT-SUBMIT-REPORT-STILL-GENERATING`; пользователь подтвердил, что в созданном обсуждении не проставлен видимый `@Глубокое исследование`. r14 verdict report не сохранён и не добавляется в `previousVerdictChain`. Закрываем причину локально: `AUDIT-REQUEST.md` теперь начинается с `@Глубокое исследование`, `audit package` требует этот маркер как обязательный, `audit-package.md` больше не запрещает текстовый префикс и фиксирует, что `audit package message`/`audit submit` отправляют текст, начинающийся с `@Глубокое исследование`. Локально после правки прошли сборки `eng\Electron2D.Build` и integration tests, focused contract tests 19/19, `update docs`, `update docs --check`, `verify docs`, `verify audit-followups`, `verify licenses` и `git diff --check` с прежним LF warning. Следующий внешний пакет должен быть r15, потому код, тесты и документы изменились после verified r14 package.

2026-07-02T12:58:00+03:00 - r15 package был собран штатным C#-инструментом, проверен на отдельной clean repo и `audit package message` подтвердил первую строку `@Глубокое исследование`. Primary submit r15 с `--reuse-conversation` завершился до сохранения отчёта диагностикой `E2D-BUILD-AUDIT-SUBMIT-CODEX-CHROME-PROTOCOL`; пользователь по скриншотам подтвердил, что режим `Глубокое исследование` в этом discussion фактически не применился, вероятно из-за лимита старого чата. r15 verdict report не создан и не добавляется в `previousVerdictChain`. Закрываем причину локально через штатный путь: `audit submit` получает явный флаг `--new-conversation`, который открывает новый primary chat проекта после локально зафиксированного лимита старого обсуждения, но не допускает concrete conversation URL и несовместим с `--reuse-conversation`, `--control-audit` и `--download-report-only`. Локально после правки прошли сборки `eng\Electron2D.Build` и integration tests, focused submit state tests 7/7, `update docs`, `update docs --check`, `verify docs`, `verify audit-followups`, `verify licenses` и `git diff --check` с прежним LF warning. Следующий внешний пакет должен быть r16, потому код, документы и тесты изменились после verified r15 package.

2026-07-02T13:16:00+03:00 - Primary audit r16 сохранён штатной командой как `docs/verdicts/release-management/t-0238-audit-r16.md` с `VERDICT: NEEDS_FIXES`. Blocker B1: `--new-conversation` разрешал любой non-conversation `--project-url`, поэтому не гарантировал открытие корневого URL проекта. Blocker B2: focused tests доказывали только обход state gate и запрет concrete conversation URL, но не проверяли фактический project-root path и non-conversation URL bypass. Закрыто локально: `--new-conversation` теперь полностью запрещает явный `--project-url` и использует только настроенный корневой URL проекта; focused tests расширены проверкой рассчитанного `ProjectUrl` и отказом для корректного project root, `https://chatgpt.com/`, другого project root, внешнего host и concrete conversation URL. Локально после правки прошли сборки `eng\Electron2D.Build` и integration tests, focused submit URL tests 12/12, `update docs`, `update docs --check`, `verify docs`, `verify audit-followups`, `verify licenses` и `git diff --check` с прежним LF warning. Следующий внешний пакет должен быть r17.

2026-07-02T13:42:00+03:00 - r17 package был собран, проверен на clean repo и отправлен через `audit submit --new-conversation`, но пользователь показал, что фактически ушёл обычный prompt: в сообщении виден только текст `@Глубокое исследование`, без отдельной плашки выбранного режима `Глубокое исследование`. Процесс r17 submit остановлен, `docs/verdicts/release-management/t-0238-audit-r17.md` не создан и r17 не добавляется в `previousVerdictChain`. Причина закрыта локально: selected-state больше не принимает обычный видимый текст рядом с composer, `@Глубокое исследование` документируется только как текстовая подсказка, а перед `ClickSendAsync` добавлена повторная проверка отдельной selection pill с `connector_openai_deep_research` после заполнения prompt-а. Локально после правки прошли сборки `eng\Electron2D.Build` и integration tests, focused Deep Research selection / submit URL tests 13/13, `update docs`, `update docs --check`, `verify docs`, `verify audit-followups`, `verify licenses` и `git diff --check` с прежним LF warning. Следующий внешний пакет должен быть r18.

2026-07-02T14:04:00+03:00 - Primary audit r18 сохранён штатной командой как `docs/verdicts/release-management/t-0238-audit-r18.md` с `VERDICT: NEEDS_FIXES`. Blocker B1: `FillPromptExpression` не сохранял English selected-pill variant `[data-keyword="Deep Research"][data-inline-selection-pill]`, хотя `DeepResearchSelectedExpression` считал его валидным, и fallback `element.textContent = ''` мог удалить плашку перед отправкой. Blocker B2: focused tests проверяли selector/order, но не исполняли production `FillPromptExpression` на DOM-фикстуре с выбранной плашкой. Закрываем локально: `FillPromptExpression` использует тот же набор selected-pill selectors, включая English keyword variant, а новый behavioral test исполняет production `FillPromptExpression` и `DeepResearchSelectedExpression`, проверяя, что вложенная English плашка сохраняется после вставки текста. Следующий внешний пакет должен быть r19 после зелёного локального коридора.

2026-07-02T14:18:00+03:00 - Локальный коридор закрытия r18 B1/B2 прошёл до новой внешней итерации: `dotnet build eng\Electron2D.Build\Electron2D.Build.csproj --no-restore -v:minimal` - passed, 0 warnings; `dotnet build tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --no-restore -v:minimal` - passed, 0 warnings; focused Deep Research/new-conversation tests - 14/14 passed; `update docs` - updated; `update docs --check` - passed; `verify docs` - passed; `verify audit-followups` - passed, 0 actionable findings / 80 saved reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed with Git LF warning for `docs/release-management/audit-package.md`. Следующий package r19 должен включить сохранённые r01/r02/r04/r16/r18 в `previousVerdictChain` и closure r18 B1/B2 в `blockerClosureList`.

2026-07-02T14:41:00+03:00 - Primary audit r19 сохранён штатной командой как `docs/verdicts/release-management/t-0238-audit-r19.md` с `VERDICT: NEEDS_FIXES`. Аудит подтвердил закрытие r18 B1/B2, но нашёл новый blocker B1: обычная ветка `SubmitAndWaitForReportAsync` потеряла baseline-снимок существующих Deep Research targets и стала передавать пустой ignore-set, хотя это допустимо только для `--download-report-only`. Blocker B2: focused tests не покрывали обратную гарантию ordinary submit path. Закрыто локально через RED/GREEN: добавлен тест `AuditSubmitOrdinarySubmitBaselinesExistingDeepResearchTargetsBeforeSend`, который сначала упал на пустом ignore-set, затем обычная ветка снова стала вызывать `SnapshotDeepResearchTargetIdsAsync(...)` до `EnableDeepResearchAsync` и передавать полученный `ignoredDeepResearchTargetIds` в `WaitForReportAsync`; read-only тест `AuditSubmitDownloadReportOnlyDoesNotBaselineIgnoreExistingConversationTargets` остаётся отдельной обратной гарантией. После исправления сборки `eng\Electron2D.Build` и integration tests прошли без предупреждений, focused Deep Research/ordinary-submit/read-only/new-conversation tests прошли 16/16. Следующий package должен быть r20 и включить r19 в `previousVerdictChain`.

2026-07-02T14:42:00+03:00 - Полный локальный коридор после закрытия r19 B1/B2 прошёл перед новой упаковкой: `update docs` - updated; `update docs --check` - passed; `verify docs` - passed; `verify audit-followups` - passed, 0 actionable findings / 81 saved reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed with Git LF warning for `docs/release-management/audit-package.md`. Следующий r20 config должен добавить `docs/verdicts/release-management/t-0238-audit-r19.md` в `previousVerdictChain` и closure r19 B1/B2 в `blockerClosureList`.

2026-07-02T14:58:00+03:00 - Primary audit r20 сохранён штатной командой как `docs/verdicts/release-management/t-0238-audit-r20.md` с `VERDICT: NEEDS_FIXES`. Аудит подтвердил ordinary submit fix, но нашёл blocker B1: `verify audit-followups` игнорировал saved `NEEDS_FIXES` reports; blocker B2: ordinary-submit baseline был доказан только source-level regex-тестом; blocker B3: активная задача `T-0240` была scope leak для пакета, где `metadata.scopeTaskIds` содержит только `T-0238`. Закрываем локально: verifier теперь читает `VERDICT: ACCEPT` и `VERDICT: NEEDS_FIXES`; добавлен behavior-level internal-contract test для выбора ready target с ignored baseline; `T-0240` временно удалена из active `TASKS.md` и roadmap до final ACCEPT, чтобы текущий audit package оставался чистой областью `T-0238`.

- audit-followup-closure:
  - source: docs/verdicts/release-management/t-0238-audit-r01.md
  - id: FOLLOW_UP_FINDING F1
  - state: accepted-risk
  - target: T-0105
  - rationale: Семантическая проверка существования `tracked-existing` и `tracked-new` target task остаётся отдельным усилением verifier-а; текущая задача закрывает обязательную форму closure notes и uniqueness по saved report path, а не полный semantic inventory resolver.
  - affected area: release-management audit follow-up closure verification
  - impact: Неверно указанный target в closure note может скрыть follow-up долг до отдельной проверки.
  - likelihood: medium
  - mitigation: `verify audit-followups` остаётся обязательной финальной проверкой, а риск должен быть пересмотрен при `T-0105` risk-register pass или при отдельной задаче на semantic validation closure targets.
  - owner/next decision point: T-0105 risk register
  - decision state: accepted for T-0238 until T-0105 or a dedicated follow-up task revisits semantic target validation.

- audit-followup-closure:
  - source: docs/verdicts/release-management/t-0238-audit-r02.md
  - id: FOLLOW_UP_FINDING F1
  - state: duplicate
  - target: docs/verdicts/release-management/t-0238-audit-r01.md FOLLOW_UP_FINDING F1
  - rationale: Повторяет semantic validation closure target debt из r01; отдельное решение риска уже записано для r01 F1.

- audit-followup-closure:
  - source: docs/verdicts/release-management/t-0238-audit-r04.md
  - id: FOLLOW_UP_FINDING F1
  - state: duplicate
  - target: docs/verdicts/release-management/t-0238-audit-r01.md FOLLOW_UP_FINDING F1
  - rationale: Повторяет semantic validation closure target debt из r01; отдельное решение риска уже записано для r01 F1.

- audit-followup-closure:
  - source: docs/verdicts/release-management/t-0238-audit-r16.md
  - id: FOLLOW_UP_FINDING F1
  - state: duplicate
  - target: docs/verdicts/release-management/t-0238-audit-r01.md FOLLOW_UP_FINDING F1
  - rationale: Повторяет semantic validation closure target debt из r01; отдельное решение риска уже записано для r01 F1.

- audit-followup-closure:
  - source: docs/verdicts/release-management/t-0238-audit-r18.md
  - id: FOLLOW_UP_FINDING F1
  - state: duplicate
  - target: docs/verdicts/release-management/t-0238-audit-r01.md FOLLOW_UP_FINDING F1
  - rationale: Повторяет semantic validation closure target debt из r01; отдельное решение риска уже записано для r01 F1.

- audit-followup-closure:
  - source: docs/verdicts/release-management/t-0238-audit-r19.md
  - id: FOLLOW_UP_FINDING F1
  - state: duplicate
  - target: docs/verdicts/release-management/t-0238-audit-r01.md FOLLOW_UP_FINDING F1
  - rationale: Повторяет semantic validation closure target debt из r01; отдельное решение риска уже записано для r01 F1.

- audit-followup-closure:
  - source: docs/verdicts/release-management/t-0238-audit-r20.md
  - id: FOLLOW_UP_FINDING F1
  - state: duplicate
  - target: docs/verdicts/release-management/t-0238-audit-r01.md FOLLOW_UP_FINDING F1
  - rationale: Повторяет semantic validation closure target debt из r01; отдельное решение риска уже записано для r01 F1.

- audit-followup-closure:
  - source: docs/verdicts/release-management/t-0238-audit-r27.md
  - id: FOLLOW_UP_FINDING F1
  - state: tracked-existing
  - target: T-0238
  - rationale: Замечание о source-level проверке `--dump-dom-only` оставлено внутри текущей открытой задачи как P2 follow-up evidence concern. Текущий scope уже содержит production DOM dump diagnostics, реальные diagnostic dump evidence по r26/r27 и source-level guard против возврата блокирующего ожидания; поведенческая fixture для mock browser/client остаётся улучшением тестовой строгости, но не меняет acceptance blockers B1/B2.

- audit-followup-closure:
  - source: docs/verdicts/release-management/t-0238-audit-r29.md
  - id: FOLLOW_UP_FINDING F1
  - state: tracked-existing
  - target: T-0238
  - rationale: Повторное замечание о source-level проверке `--dump-dom-only` оставлено в текущей открытой задаче как P2 follow-up evidence concern. Оно не блокирует закрытие r29 B1/B2, потому r29 blockers относятся к selected-state для plain menu rows и к поведенческому доказательству выбора уже открытого `Глубокое исследование`; dump diagnostics ordering остаётся отдельным усилением тестовой строгости внутри T-0238 до финального accepted state.

- audit-followup-closure:
  - source: docs/verdicts/release-management/t-0238-audit-r31.md
  - id: FOLLOW_UP_FINDING F1
  - state: tracked-existing
  - target: T-0238
  - rationale: Повторное замечание о source-level проверке `--dump-dom-only` остаётся P2 follow-up внутри текущей открытой задачи. Оно не блокирует закрытие r31 B1/B2, потому r31 blockers относятся к ordinary stale-download path и недостающему behavior-level regression для production polling/export flow; этот regression теперь добавлен для downloader, а dump-dom diagnostics ordering остаётся отдельным улучшением тестовой строгости.

- audit-followup-closure:
  - source: docs/verdicts/release-management/t-0238-audit-r32.md
  - id: FOLLOW_UP_FINDING F1
  - state: tracked-existing
  - target: T-0238
  - rationale: Повторное замечание о source-level проверке `--dump-dom-only` остаётся P2 follow-up внутри текущей открытой задачи. Оно не блокирует закрытие r32 B1/B2, потому r32 blockers относятся к evidence filter и внутренней согласованности `TASKS.md`; dump-dom diagnostics ordering остаётся отдельным улучшением тестовой строгости.

- audit-followup-closure:
  - source: docs/verdicts/release-management/t-0238-audit-r33.md
  - id: FOLLOW_UP_FINDING F1
  - state: tracked-existing
  - target: T-0238
  - rationale: Замечание больше не оставлено как отложенный долг: в текущей задаче добавлен поведенческий regression `AuditSubmitDumpDomOnlyWritesBaseDomBeforeDeepResearchFrameDiagnostics`, который вызывает production-путь `DumpDomFromUrlAsync` через внутренний `IAuditSubmitDomDumpDriver` и проверяет запись `frame-tree.json`, `target-info.json`, `deep-research-selected-result.json`, `deep-research-selected-diagnostics.json` и `summary.txt` в реальном временном каталоге. Так как это изменение внесено после primary `ACCEPT` r33, следующий внешний пакет должен быть r34 с `docs/verdicts/release-management/t-0238-audit-r33.md` в `previousVerdictChain` и этой closure-note в `blockerClosureList`.

2026-07-02T15:07:00+03:00 - Полный локальный коридор после закрытия r20 B1/B2/B3 прошёл перед новой упаковкой: сборка `eng\Electron2D.Build` - passed, 0 warnings; сборка integration tests - passed, 0 warnings; новые RED-тесты сначала падали на прежней реализации; focused suite с `NEEDS_FIXES` follow-up closure, behavior-level ignored target selection, ordinary/read-only submit, Deep Research selection/fillprompt и new-conversation path прошёл 18/18; `update docs` - updated; `update docs --check` - passed; `verify docs` - passed; `verify audit-followups` - passed, 7 actionable findings / 82 saved reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed with Git LF warning for `docs/release-management/audit-package.md`. Следующий r21 config должен добавить `docs/verdicts/release-management/t-0238-audit-r20.md` в `previousVerdictChain` и closure r20 B1/B2/B3 в `blockerClosureList`.

2026-07-02T15:30:00+03:00 - Primary audit r21 сохранён штатной командой как `docs/verdicts/release-management/t-0238-audit-r21.md` с `VERDICT: NEEDS_FIXES`. Аудит подтвердил закрытие r20 B1/B2/B3, но нашёл blocker B1: `audit submit --control-audit` допускал control ZIP без `metadata.previousVerdictChain` или без `metadata.blockerClosureList`, хотя контракт требует обязательные пустые массивы `[]`. Закрыто локально через RED/GREEN: добавлен focused test `AuditSubmitControlAuditRejectsMissingMetadataArrayBeforeBrowserLaunch`, который сначала доходил до `CODEX-CHROME-UNAVAILABLE`, затем `ValidateControlAuditMetadataArrayEmpty` стал отклонять отсутствующее поле диагностикой `E2D-BUILD-AUDIT-SUBMIT-CONTROL-CONTEXT` до подключения к браузеру. После исправления сборки `eng\Electron2D.Build` и integration tests прошли без предупреждений; control-audit focused tests прошли 11/11; объединённый focused suite по control guard, followups, ordinary/read-only submit, Deep Research selection/fillprompt и new-conversation path прошёл 29/29; `update docs` - updated; `update docs --check` - passed; `verify docs` - passed; `verify audit-followups` - passed, 7 actionable findings / 83 saved reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed with Git LF warning for `docs/release-management/audit-package.md`. Следующий r22 config должен добавить `docs/verdicts/release-management/t-0238-audit-r21.md` в `previousVerdictChain` и closure r21 B1 в `blockerClosureList`.

2026-07-02T15:57:00+03:00 - r22 package был собран и проверен на clean repo, но primary submit дважды завершился до отправки диагностикой `E2D-BUILD-AUDIT-SUBMIT-DEEP-RESEARCH-MISSING`; `docs/verdicts/release-management/t-0238-audit-r22.md` и conversation URL не созданы, поэтому r22 не добавляется в `previousVerdictChain`. Пользователь уточнил, что текст `@Глубокое исследование` не гарантирует настоящий режим и что нужно надёжно выбирать его кнопкой `+`. Закрыто локально через RED/GREEN: добавлены тесты, где selector сначала кликал широкий popover вместо строки `Глубокое исследование`, selected-state не принимал connector-плашку без `data-inline-selection-pill`, а `FillPromptExpression` мог удалить такую плашку. После исправления новый focused набор прошёл 4/4, расширенный focused suite по submit/control/follow-up контрактам прошёл 43/43, сборки `eng\Electron2D.Build` и integration tests прошли без предупреждений, `update docs --check`, `verify docs`, `verify audit-followups`, `verify licenses` и `git diff --check` прошли. Следующий внешний package должен быть r23, потому код и тесты изменились после verified r22 package.

2026-07-02T16:04:00+03:00 - r23 package был создан штатной командой, но не получил успешный обязательный `audit package verify`: первая попытка использовала ещё не созданный clean repo path, вторая увидела текущую грязную рабочую копию вместо отдельного Git checkout, после создания worktree повтор с относительным ZIP path не нашёл архив, а затем `.temp/audit` оказался недоступен. Внешняя отправка r23 не выполнялась, verdict report и conversation URL не созданы, r23 не добавляется в `previousVerdictChain`. Следующий package должен быть r24 с тем же содержательным scope r22 fix и с заранее подготовленным clean worktree.

2026-07-02T16:35:00+03:00 - Primary audit r24 сохранён штатной командой как `docs/verdicts/release-management/t-0238-audit-r24.md` с `VERDICT: NEEDS_FIXES`. Аудит подтвердил общий scope и прошлые closures, но нашёл два blocker-а в заявленном закрытии r22: B1 — `DeepResearchItemPointExpression` вычислял `plusRect` по первому `button[data-testid="composer-plus-btn"]`, а не по той же видимой и доступной кнопке `+`, которую реально нажимает `DeepResearchMenuPointExpression`; B2 — `DeepResearchSelectedExpression` имел ранний `return true` для любой connector metadata внутри prompt-а без проверки видимости. Закрыто локально через RED/GREEN: добавлен regression с hidden/disabled stale plus перед видимой кнопкой `+`, и regression со скрытой connector-меткой внутри prompt-а; production JS теперь ищет visible/enabled plus для bounds и требует видимость nested connector metadata. Следующий package должен быть r25 с `docs/verdicts/release-management/t-0238-audit-r24.md` в `previousVerdictChain` и closure r24 B1/B2 в `blockerClosureList`.

2026-07-02T16:40:00+03:00 - Полный локальный коридор после закрытия r24 B1/B2 прошёл: сборка `eng\Electron2D.Build` - passed, 0 warnings; сборка integration tests - passed, 0 warnings; RED regressions сначала падали на текущей реализации; focused suite по submit/control/follow-up контрактам прошёл 45/45; `update docs` - updated; `update docs --check` - passed; `verify docs` - passed; `verify audit-followups` - passed, 7 actionable findings / 84 saved reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed without warnings. Следующий package r25 должен добавить r24 в `previousVerdictChain` и closure r24 B1/B2 в `blockerClosureList`.

2026-07-02T17:07:00+03:00 - Primary audit r25 сохранён штатной командой как `docs/verdicts/release-management/t-0238-audit-r25.md` с `VERDICT: NEEDS_FIXES`. Аудит подтвердил общий scope и прошлые closures, но нашёл два blocker-а: B1 — `DeepResearchSelectedExpression` всё ещё мог принять сам prompt-контейнер за выбранную плашку, если внутри prompt был видимый `button` или `role="menuitem"` с connector metadata; B2 — `docs/release-management/audit-package.md` противоречиво описывал `data-inline-selection-pill`, хотя текущий интерфейс может показывать валидную плашку без этого старого маркера. Закрыто локально через RED/GREEN: добавлен regression для вложенного menu item внутри prompt-а, сначала он падал `Expected: False / Actual: True`; production JS больше не рассматривает сам prompt как candidate selected pill, а документ описывает единое правило: валидна видимая плашка с connector metadata рядом с полем или внутри него, но не обычный текст, скрытый элемент, `button`, `role="button"`, `role="menuitem"`, menu/listbox/popover residue. Focused Deep Research suite прошёл 11/11. Следующий package должен быть r26 с `docs/verdicts/release-management/t-0238-audit-r25.md` в `previousVerdictChain` и closure r25 B1/B2 в `blockerClosureList`.

2026-07-02T17:13:00+03:00 - Полный локальный коридор после закрытия r25 B1/B2 прошёл: сборка `eng\Electron2D.Build` - passed, 0 warnings; сборка integration tests - passed, 0 warnings; новый RED regression сначала падал на прежней реализации; focused suite по submit/control/follow-up контрактам прошёл 46/46; `update docs` - updated; `update docs --check` - passed; `verify docs` - passed; `verify audit-followups` - passed, 7 actionable findings / 85 saved reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed without warnings. Следующий package r26 должен добавить r25 в `previousVerdictChain` и closure r25 B1/B2 в `blockerClosureList`.

2026-07-02T17:40:00+03:00 - r26 package был собран штатным C#-инструментом, проверен на clean repo `.temp/audit-clean/T-0238-r26-20260702-171833-lf`, и `audit package message` подтвердил первую строку `@Глубокое исследование`, но primary submit был остановлен до сохранения verdict-а после пользовательского замечания, что в отправленном сообщении нет настоящей плашки `Глубокое исследование`. Saved report `docs/verdicts/release-management/t-0238-audit-r26.md` отсутствует, поэтому r26 не добавляется в `previousVerdictChain`. Диагностический dump `.temp/audit/T-0238/dom-dump-r26-deep-research-missing-fixed` показал причину: `DeepResearchSelectedExpression` возвращал `true` из-за широких ancestor-`div` на всю страницу или область чата с вложенной старой Deep Research-плашкой из r25; для r25 root text содержал `ZIP-архив / Глубокое исследование / @Глубокое исследование`, а для r26 только `ZIP-архив / @Глубокое исследование`. Закрыто локально: `--dump-dom-only` больше не ждёт Deep Research frame до записи базового DOM и пишет `deep-research-selected-result.json`/`deep-research-selected-diagnostics.json`; selected-state теперь принимает вложенный connector без прямых metadata только для компактной плашки, а не для широкого контейнера страницы или старого сообщения. Повторный dump `.temp/audit/T-0238/dom-dump-r26-deep-research-missing-after-selector-fix` на той же странице дал `deep-research-selected-result.json = false` и `acceptedCandidateCount = 0`. Следующий package должен быть r27, потому код, тесты и документ изменились после verified r26 package.

2026-07-02T17:44:00+03:00 - Полный локальный коридор после r26 missing-pill fix прошёл: сборка `eng\Electron2D.Build` - passed, 0 warnings; сборка integration tests - passed, 0 warnings; focused suite по submit/control/follow-up контрактам прошёл 48/48; `update docs` - updated; `update docs --check` - passed; `verify docs` - passed; `verify audit-followups` - passed, 7 actionable findings / 85 saved reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed without warnings. Следующий package r27 должен сохранять прежнюю saved verdict chain r01/r02/r04/r16/r18/r19/r20/r21/r24/r25, не включать r26 как verdict, и добавить procedural closure для r26 missing-pill failure.

2026-07-02T18:06:00+03:00 - Primary audit r27 сохранён штатной командой как `docs/verdicts/release-management/t-0238-audit-r27.md` с `VERDICT: NEEDS_FIXES`. Аудит подтвердил r26 missing-pill fix, но нашёл B1: selected-state всё ещё принимал connector metadata внутри интерактивных предков и inline-marker branch без общего фильтра; B2: `AUDIT-REQUEST.md` не закреплял zero-context состав для `Suggested new task`. Закрыто локально через RED/GREEN: новые tests сначала упали на 5/5 сценариях (`button`, `role=button`, `role=menuitem`, `role=option`, интерактивная inline-плашка), затем production JS стал отвергать candidates с интерактивным предком до любых connector checks; `AUDIT-REQUEST.md` теперь требует для `Suggested new task` рабочий заголовок, приоритет, затронутый домен, краткий критерий приёмки и идею проверки. Targeted tests после исправления прошли 6/6. Следующий package должен быть r28 с `docs/verdicts/release-management/t-0238-audit-r27.md` в `previousVerdictChain`, closure r27 B1/B2 в `blockerClosureList` и closure note для r27 `FOLLOW_UP_FINDING F1`.

2026-07-02T18:10:00+03:00 - Полный локальный коридор после закрытия r27 B1/B2 прошёл: сборка `eng\Electron2D.Build` - passed, 0 warnings; сборка integration tests - passed, 0 warnings; focused suite по submit/control/follow-up контрактам и читаемому audit request прошёл 54/54; `update docs` - updated; `update docs --check` - passed; `verify docs` - passed; `verify audit-followups` - passed, 8 actionable findings / 86 saved reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed without warnings. Следующий package r28 должен добавить r27 в `previousVerdictChain` и closure r27 B1/B2, не добавляя r26 как saved verdict.

2026-07-02T18:23:00+03:00 - r28 package был собран штатным C#-инструментом и проверен на clean repo `.temp/audit-clean/T-0238-r28-20260702-181831-lf`, но primary submit не создал saved verdict: первая попытка завершилась локальным `E2D-BUILD-AUDIT-SUBMIT-CODEX-CHROME-PROTOCOL` до сохранения conversation URL, повторная попытка завершилась pre-send `E2D-BUILD-AUDIT-SUBMIT-DEEP-RESEARCH-MISSING`. `docs/verdicts/release-management/t-0238-audit-r28.md` и `conversation-url-r28.txt` отсутствуют, поэтому r28 не добавляется в `previousVerdictChain`. Повторная проверка пользовательской ссылки штатным `--download-report-only` вернула `E2D-BUILD-AUDIT-SUBMIT-REPORT-STALE`: экспортированный отчёт ссылается на evidence `T-0238 r27`, а не r28. Диагностика живого меню показала, что пункт `Глубокое исследование` был виден, но на медленной отрисовке submit-цикл мог повторно нажать `+` и закрыть уже открывшееся меню; дополнительно реальная строка меню отрисована как `.__menu-item` / `data-fill` + `tabindex` без `role="menuitem"`. Код, тесты и доменный документ изменены после r28 package, поэтому r28 больше нельзя отправлять как текущий audit package; после полного локального коридора нужен новый package r29 с тем же saved verdict chain до r27 и closure r27/r28 tooling failure.

2026-07-02T18:52:00+03:00 - Полный локальный коридор после r28 submit fix прошёл: сборка `eng\Electron2D.Build` - passed, 0 warnings; сборка integration tests - passed, 0 warnings; focused suite по submit/control/follow-up/readability контрактам прошёл 56/56; `update docs` - updated; `update docs --check` - passed; `verify docs` - passed; `verify audit-followups` - passed, 8 actionable findings / 86 saved reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Следующий package r29 должен добавить r27 в `previousVerdictChain`, закрыть r27 B1/B2 и r28 pre-send tooling failure, не добавлять r26/r28 как saved verdict reports.

2026-07-02T19:34:00+03:00 - Primary audit r29 сохранён штатной командой как `docs/verdicts/release-management/t-0238-audit-r29.md` с `VERDICT: NEEDS_FIXES`: обычный submit завис без сохранённого файла, после чего DOM dump `.temp/audit/T-0238/dom-dump-r29-download-stuck` показал несколько готовых Deep Research targets по старым и текущей отправкам (`r25`, `r27`, `r29`), а отчёт был восстановлен штатным `--download-report-only` по сохранённому conversation URL. Blocker B1 закрыт: `DeepResearchSelectedExpression` и диагностическое выражение теперь отвергают plain строки меню `.__menu-item` / `[data-fill][tabindex]` и их потомков, а regression проверяет plain row с прямой connector metadata. Blocker B2 закрыт: source-level regex заменён поведенческим тестом `AuditSubmitDeepResearchSelectionClicksAlreadyOpenMenuItemBeforeTogglingPlus`, который вызывает production selection loop через внутренний driver-контракт и подтверждает, что при уже открытом пункте меню команда кликает его, а не нажимает `+` повторно. Дополнительно исправлен сам downloader path: обычный submit после baseline может выбрать последний готовый non-baseline target в reused-чате и затем отсеять stale Markdown строгой проверкой текущих `taskId`/`iteration`; Markdown-export selector принимает plain строки меню `.__menu-item` / `[data-fill][tabindex]`. Focused regression suite после исправления прошёл 22/22. Следующий package должен быть r30 с `docs/verdicts/release-management/t-0238-audit-r29.md` в `previousVerdictChain`, closure r29 B1/B2 в `blockerClosureList` и closure note для r29 `FOLLOW_UP_FINDING F1`.

2026-07-02T19:39:00+03:00 - Полный локальный коридор после закрытия r29 B1/B2 и исправления обычного downloader path прошёл: сборка `eng\Electron2D.Build` - passed, 0 warnings; сборка integration tests - passed, 0 warnings; расширенный focused suite по submit/control/follow-up/readability контрактам прошёл 64/64; `update docs` - updated; `update docs --check` - passed; `verify docs` - passed; `verify audit-followups` - passed, 9 actionable findings / 87 saved reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Следующий package r30 должен добавить r29 в `previousVerdictChain`, закрыть r29 B1/B2, включить closure note для r29 `FOLLOW_UP_FINDING F1` и не использовать `audit package verify` как отладчик.

2026-07-02T19:54:00+03:00 - r30 package был создан штатным C#-инструментом и verified на clean repo `.temp/audit-clean/T-0238-r30-20260702-194558-lf`, но primary submit r30 не создал saved verdict: команда отправила ZIP, сохранила `conversation-url-r30.txt`, затем экспортировала старый r29 Markdown и штатно остановилась с `E2D-BUILD-AUDIT-SUBMIT-REPORT-STALE`, потому отчёт ссылался на evidence `T-0238 r29`, а не r30. DOM dump `.temp/audit/T-0238/dom-dump-r30-stale-r29` показал три старых ready Deep Research targets и один более новый target `ready=False`, то есть текущий r30 отчёт ещё генерировался. Исправлено локально: target-selection policy теперь выбирает latest ready target только если он последний среди non-baseline targets; если после него есть более новый неготовый target, ordinary submit ждёт дальше и не скачивает старый report. Regression `AuditSubmitReadyTargetSelectionWaitsForNewestNonBaselineTarget` покрывает этот случай; focused target-selection tests прошли 3/3. Так как source/tests/docs изменились после verified r30 ZIP, r30 не может быть принят как текущий package и не добавляется в `previousVerdictChain`; после полного локального коридора нужен r31 с той же saved verdict chain до r29 и closure r30 local stale-download failure.

2026-07-02T20:01:00+03:00 - Полный локальный коридор после закрытия r30 local stale-download failure прошёл: сборка `eng\Electron2D.Build` - passed, 0 warnings; сборка integration tests - passed, 0 warnings; расширенный focused suite по submit/control/follow-up/readability контрактам прошёл 65/65; `update docs` - updated; `update docs --check` - passed; `verify docs` - passed; `verify audit-followups` - passed, 9 actionable findings / 87 saved reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Следующий package r31 должен добавить r29 в `previousVerdictChain`, не добавлять r30 без saved verdict, закрыть r29 B1/B2 и r30 local stale-download failure.

2026-07-02T20:32:00+03:00 - Primary audit r31 сохранён штатной командой как `docs/verdicts/release-management/t-0238-audit-r31.md` с `VERDICT: NEEDS_FIXES`. Аудит подтвердил закрытие r29 B1/B2, но нашёл B1/B2 в r30 stale-download closure: helper-level ожидание newest target не блокировало page-level export fallback, а тесты не исполняли production polling/export path. Закрыто локально: `DownloadReportCandidatesAsync` теперь использует внутренний driver-контракт, получает состояние `WaitForNewerTarget` и при нём возвращает пустой selected result без вызова page-level export fallback; скачивание через target выполняется только когда newest target готов. Behavior-level regression `AuditSubmitReportCandidateFlowBlocksPageFallbackWhileNewestTargetGenerates` доказывает оба сценария: при `ready=false` page fallback не вызывается, при готовом target скачивание идёт через target surface. Focused regression tests прошли 4/4. Следующий package должен быть r32 с `docs/verdicts/release-management/t-0238-audit-r31.md` в `previousVerdictChain`, closure r31 B1/B2 и closure note для r31 `FOLLOW_UP_FINDING F1`.

2026-07-02T20:39:00+03:00 - Полный локальный коридор после закрытия r31 B1/B2 прошёл: сборка `eng\Electron2D.Build` - passed, 0 warnings; сборка integration tests - passed, 0 warnings; расширенный focused suite по submit/control/follow-up/readability/candidate-flow контрактам прошёл 66/66; `update docs` - updated; `update docs --check` - passed; `verify docs` - passed; `verify audit-followups` - passed, 10 actionable findings / 88 saved reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Следующий package r32 должен добавить r31 в `previousVerdictChain`, закрыть r31 B1/B2 и не добавлять r30 без saved verdict.

2026-07-02T21:05:00+03:00 - Primary audit r32 сохранён штатным `--download-report-only` после первоначального unhandled submit failure как `docs/verdicts/release-management/t-0238-audit-r32.md` с `VERDICT: NEEDS_FIXES`. Аудит подтвердил, что code-level driver-flow закрывает stale-download behavior, но нашёл B1: r32 package evidence не запускал `AuditSubmitReportCandidateFlowBlocksPageFallbackWhileNewestTargetGenerates`, потому configured filter в `.temp/audit/T-0238/audit-package.r32.input.json` остался старым и stdout показывал 65/65 вместо локально выполненных 66/66. B2: `TASKS.md` противоречиво утверждал, что helper-only regression сам закрывал r30 stale-download failure. Закрыто локально: r30 subtask переписан как helper-level guard, а финальное закрытие stale-download path остаётся в r31/r32 driver-flow пункте; следующий package r33 должен добавить r32 в `previousVerdictChain`, closure r32 B1/B2, обновлённый focused filter с `AuditSubmitReportCandidateFlow` и closure note для r32 `FOLLOW_UP_FINDING F1`.

2026-07-02T21:11:00+03:00 - Полный локальный коридор после закрытия r32 B1/B2 прошёл: сборка `eng\Electron2D.Build` - passed, 0 warnings; сборка integration tests - passed, 0 warnings; focused suite с явным `AuditSubmitReportCandidateFlow` filter прошёл 66/66; `update docs` - updated; `update docs --check` - passed; `verify docs` - passed; `verify audit-followups` - passed, 11 actionable findings / 89 saved reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Следующий package r33 должен включить r32 в `previousVerdictChain`, closure r32 B1/B2 и evidence stdout 66/66.

2026-07-02T21:28:00+03:00 - Primary audit r33 сохранён штатной командой как `docs/verdicts/release-management/t-0238-audit-r33.md` с `VERDICT: ACCEPT`, но control audit не запущен: `verify audit-followups` сразу нашёл новый actionable `FOLLOW_UP_FINDING F1` из r33 без closure-note. Закрыто локально через RED/GREEN: новый тест `AuditSubmitDumpDomOnlyWritesBaseDomBeforeDeepResearchFrameDiagnostics` сначала упал на отсутствии production driver-контракта, затем `DumpDomFromUrlAsync` получил внутренний `IAuditSubmitDomDumpDriver`, а тест стал вызывать production-последовательность дампа и проверять созданные diagnostic JSON-файлы на диске. Так как код, тесты, доменный документ и `TASKS.md` изменились после primary `ACCEPT` r33, следующий внешний package должен быть r34 с `docs/verdicts/release-management/t-0238-audit-r33.md` в `previousVerdictChain` и closure r33 F1.

2026-07-02T21:50:00+03:00 - Полный локальный коридор после закрытия r33 F1 прошёл: сборка `eng\Electron2D.Build` - passed, 0 warnings; сборка `tests\Electron2D.Tests.Integration` - passed, 0 warnings; focused suite с поведенческим DOM dump regression прошёл 66/66; `update docs` - updated; `update docs --check` - passed; `verify docs` - passed; `verify audit-followups` - passed, 12 actionable findings / 90 saved reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Следующий package r34 должен добавить `docs/verdicts/release-management/t-0238-audit-r33.md` в `previousVerdictChain`, закрыть r33 F1 и включить focused evidence с `AuditSubmitDumpDomOnlyWritesBaseDomBeforeDeepResearchFrameDiagnostics`.

2026-07-02T22:19:00+03:00 - r34 package был создан штатным C#-инструментом и verified на clean repo `.temp/audit-clean/T-0238-r34-20260702-215931-lf`, но primary submit не создал saved verdict и не записал `conversation-url-r34.txt`: команда завершилась до успешной отправки/сохранения состояния из-за повторных `Page.enable` timeout при CDP reattach. Read-only recovery по сохранённому URL сначала вернул `E2D-BUILD-AUDIT-SUBMIT-REPORT-EXPORT-MISSING`. Штатный DOM dump `.temp/audit/T-0238/dom-dump-r34-export-missing` показал, что r34 в обсуждении отсутствует, а старые готовые Deep Research targets есть; у последнего r33 target кнопка `Экспорт` находится внутри Deep Research frame, но Markdown menu может отрисовываться в основном DOM страницы. Закрыто локально через RED/GREEN: `AuditSubmitAllowsPageMarkdownMenuFallbackForSelectedDeepResearchSurface` сначала упал на запрете page-level Markdown menu для selected frame/target surfaces, затем `CanUsePageLevelMarkdownMenu` разрешил такой fallback только для пункта Markdown после выбранной кнопки экспорта. Повторный read-only recovery больше не падает на export missing: он скачал старый r33 report и штатно остановился как `E2D-BUILD-AUDIT-SUBMIT-REPORT-STALE`, потому текущий `--out` ожидал r34. Так как код, тесты и доменный документ изменились после verified r34 ZIP, r34 больше нельзя отправлять; следующий package должен быть r35 с тем же saved verdict chain до r33 и closure r34 local export recovery failure.

2026-07-02T22:39:00+03:00 - Удалён ошибочно созданный `docs/verdicts/release-management/t-0238-audit-r34.md`: это был DOM-dump summary от `--dump-dom-only`, а не сохранённый внешний отчёт. Причина закрыта локально через RED/GREEN: `AuditSubmitDumpDomOnlyRejectsVerdictOutputBeforeBrowserLaunch` сначала доказал, что команда доходила до Chrome и могла писать диагностику в verdict-path, затем `audit submit` стал отклонять `--dump-dom-only --out docs/verdicts/**/<task-id>-audit-rNN.md` до подключения к браузеру. r34 по-прежнему не добавляется в `previousVerdictChain`, потому валидного saved verdict report нет; следующий внешний package должен быть r35 после полного локального коридора.

2026-07-02T22:55:00+03:00 - Полный локальный коридор после запрета dump summary в verdict-path прошёл: `dotnet build eng\Electron2D.Build\Electron2D.Build.csproj --no-restore -v:minimal`, `dotnet build tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --no-restore -v:minimal`, focused `dotnet test ... --filter "FullyQualifiedName~AuditSubmit"` прошёл 111/111, `update docs`, `update docs --check`, `verify docs`, `verify audit-followups`, `verify licenses` и `git diff --check` прошли. Можно готовить r35; r34 остаётся без saved verdict report и не включается в `previousVerdictChain`.

2026-07-02T23:24:00+03:00 - r35 package был создан штатным C#-инструментом и verified на clean repo `.temp/audit-clean/T-0238-r35-20260702-2305-lf`, но primary submit не отправил ZIP и не создал saved verdict report: команда остановилась до прикрепления файла с `E2D-BUILD-AUDIT-SUBMIT-DEEP-RESEARCH-MISSING`. Диагностический DOM dump `.temp/audit/T-0238/dom-dump-r35-deep-research-missing` подтвердил, что composer и кнопка `+` доступны, старые Deep Research-плашки находятся в истории и правильно отвергаются, а текущая selected-плашка отсутствует. Причина закрыта локально через RED/GREEN: `AuditSubmitDeepResearchSelectionWaitsWhenComposerMenuIsAlreadyOpen` сначала показал лишний повторный `TryOpenMenuAsync`, затем `audit submit` стал распознавать уже открытое composer-меню и ждать пункт `Глубокое исследование` без повторного нажатия `+`. Так как код, тесты и доменный документ изменились после verified r35 ZIP, r35 не может быть отправлен как текущий package и не добавляется в `previousVerdictChain`; после полного локального коридора нужен r36.

2026-07-02T23:34:00+03:00 - Полный локальный коридор после закрытия r35 Deep Research menu toggle failure прошёл: `dotnet build eng\Electron2D.Build\Electron2D.Build.csproj --no-restore -v:minimal`, `dotnet build tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --no-restore -v:minimal`, focused `dotnet test ... --filter "FullyQualifiedName~AuditSubmit"` прошёл 112/112, `update docs`, `update docs --check`, `verify docs`, `verify audit-followups`, `verify licenses` и `git diff --check` прошли. Можно готовить r36; `previousVerdictChain` остаётся до r33, а r34/r35 не включаются, потому valid saved verdict reports для них нет.

2026-07-02T23:57:00+03:00 - Primary audit r36 сохранён штатной командой как `docs/verdicts/release-management/t-0238-audit-r36.md` с `VERDICT: NEEDS_FIXES`. Аудит подтвердил общий scope, но нашёл B1/B2/B3: page-level Markdown fallback для выбранной Deep Research surface был разрешён до клика выбранной кнопки `Экспорт`, документ противоречиво описывал этот порядок, а production-детектор открытого composer-меню принимал один `aria-expanded="true"` без видимых строк меню. Закрыто локально через RED/GREEN: `ClickMarkdownMenuItemAsync` теперь получает состояние выбранной export-кнопки и не вызывает page-level Markdown fallback до её успешного клика, `docs/release-management/audit-package.md` описывает тот же порядок, а `DeepResearchComposerMenuOpenExpression` требует видимые menu/listbox/plain-row элементы возле кнопки `+`. Новые regression-тесты `AuditSubmitMarkdownMenuClickBlocksPageFallbackBeforeSelectedExportButtonClick` и `AuditSubmitDeepResearchComposerMenuOpenRequiresVisibleMenuRowsNearPlus` сначала падали на старой реализации, затем прошли 2/2. Следующий package должен быть r37 с `docs/verdicts/release-management/t-0238-audit-r36.md` в `previousVerdictChain` и closure r36 B1/B2/B3.

2026-07-03T00:05:00+03:00 - Полный локальный коридор после закрытия r36 B1/B2/B3 прошёл: `dotnet build eng\Electron2D.Build\Electron2D.Build.csproj --no-restore -v:minimal`, `dotnet build tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --no-restore -v:minimal`, focused `dotnet test ... --filter "FullyQualifiedName~AuditSubmit"` прошёл 114/114, `update docs`, `update docs --check`, `verify docs`, `verify audit-followups` для 12 actionable findings across 91 saved audit reports, `verify licenses` для 661 source files и `git diff --check` прошли. Можно готовить r37; r36 включается в `previousVerdictChain`, а r34/r35 остаются без valid saved verdict reports.

2026-07-03T00:31:00+03:00 - r37 package был создан штатным C#-инструментом и verified на clean repo `.temp/audit-clean/T-0238-r37-20260703-002037-lf`, но primary submit не отправил ZIP и не создал saved verdict report: команда остановилась до прикрепления файла с `E2D-BUILD-AUDIT-SUBMIT-DEEP-RESEARCH-MISSING`. Штатный DOM dump `.temp/audit/T-0238/dom-dump-r37-deep-research-missing` подтвердил пустой prompt, отсутствие текущей selected-плашки и старые Deep Research targets/плашки в истории; причина закрыта локально через RED/GREEN: `AuditSubmitDeepResearchSelectionWaitsWhenComposerPlusIsExpandedBeforeRowsRender` сначала показал, что loop повторно нажимал `+` в состоянии `aria-expanded=true` без видимых строк меню, затем production loop получил отдельный check `IsComposerMenuExpandedAsync` и ждёт следующую проверку без повторного клика. Так как код, тесты и доменный документ изменились после verified r37 ZIP, r37 не может быть отправлен как текущий package и не добавляется в `previousVerdictChain`; после полного локального коридора нужен r38.

2026-07-03T00:36:00+03:00 - Полный локальный коридор после закрытия r37 expanded-menu pre-send failure прошёл: `dotnet build eng\Electron2D.Build\Electron2D.Build.csproj --no-restore -v:minimal`, `dotnet build tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --no-restore -v:minimal`, focused `dotnet test ... --filter "FullyQualifiedName~AuditSubmit"` прошёл 115/115, `update docs`, `update docs --check`, `verify docs`, `verify audit-followups` для 12 actionable findings across 91 saved audit reports, `verify licenses` для 661 source files и `git diff --check` прошли. Можно готовить r38; r36 остаётся последним saved verdict в `previousVerdictChain`, r37 не включается из-за отсутствия saved report.

2026-07-03T00:52:00+03:00 - r38 package был создан штатным C#-инструментом и verified на clean repo `.temp/audit-clean/T-0238-r38-20260703-003749-lf`, но primary submit снова остановился до отправки ZIP с `E2D-BUILD-AUDIT-SUBMIT-DEEP-RESEARCH-MISSING`; `docs/verdicts/release-management/t-0238-audit-r38.md` не создан. Штатный DOM dump `.temp/audit/T-0238/dom-dump-r38-deep-research-missing` подтвердил, что текущий prompt пустой и выбранной плашки нет; старые плашки `Глубокое исследование` из истории, включая r25 и r36, детектор отверг правильно. Закрыта следующая локальная причина: после собственного успешного клика по `+` production loop теперь несколько циклов ждёт появления пункта меню и не нажимает `+` повторно, даже если строки меню ещё не видны и `aria-expanded` не успел стать надёжным сигналом. Regression `AuditSubmitDeepResearchSelectionWaitsAfterOpeningMenuBeforeRetogglingPlus` прошёл 1/1; этот тест не запускался как RED до правки, поэтому он фиксируется как regression coverage без заявления о RED. Так как код, тесты и доменный документ изменились после verified r38 ZIP, r38 не может быть отправлен как текущий package и не добавляется в `previousVerdictChain`; после полного локального коридора нужен r39.

2026-07-03T01:05:00+03:00 - Полный локальный коридор после r38-fix прошёл в новой сессии: `dotnet build eng\Electron2D.Build\Electron2D.Build.csproj --no-restore -v:minimal` - passed, 0 warnings; `dotnet build tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --no-restore -v:minimal` - passed, 0 warnings; focused `dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~AuditSubmit" --no-build --no-restore` прошёл 116/116; `update docs`, `update docs --check`, `verify docs`, `verify audit-followups` для 12 actionable findings across 91 saved audit reports, `verify licenses` для 661 source files и `git diff --check` прошли. Можно готовить r39; r36 остаётся последним saved verdict в `previousVerdictChain`, r37/r38 не включаются из-за отсутствия saved reports.

2026-07-03T02:01:00+03:00 - r39 package уже был создан и verified до process-contract правок, но submit остановился до отправки ZIP с `E2D-BUILD-AUDIT-SUBMIT-DEEP-RESEARCH-MISSING`; saved verdict report `docs/verdicts/release-management/t-0238-audit-r39.md` не создан. Штатный DOM dump `.temp/audit/T-0238/dom-dump-r39-deep-research-missing` показал пустой текущий prompt, отсутствие выбранной плашки и только старые Deep Research targets/плашки в истории. По новому правилу остановки это повторяющийся класс локального отказа, поэтому новый submit без локальной стабилизации запрещён. Закрытие выполнено локально: pre-send путь теперь сначала прикрепляет ZIP, вставляет prompt, затем заново выбирает `Глубокое исследование`, повторно проверяет выбранный режим и только после этого отправляет сообщение; это защищает от сброса выбранного инструмента после прикрепления файла или заполнения prompt-а. Новый behavior-level internal driver test `AuditSubmitPromptSubmissionSelectsDeepResearchAfterAttachmentAndPromptFill` исполняет production `SubmitPromptAsync` и проверяет порядок `AttachFilesAsync` -> `FillPromptAsync` -> `EnableDeepResearchAsync` -> `RequireDeepResearchSelectedAsync` -> `ClickSendAsync`; вместе с ним прошёл focused Deep Research selection slice 5/5. Так как код, тесты и доменный документ изменились после verified r39 ZIP, r39 нельзя отправлять как текущий package и нельзя включать в `previousVerdictChain`; после полного medium-коридора нужен следующий package.

2026-07-03T02:06:00+03:00 - Medium-коридор после закрытия r39 local pre-send failure прошёл: `dotnet build eng\Electron2D.Build\Electron2D.Build.csproj --no-restore -v:minimal` - passed, 0 warnings; `dotnet build tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --no-restore -v:minimal` - passed, 0 warnings; focused `AuditSubmitPromptSubmissionSelectsDeepResearchAfterAttachmentAndPromptFill` + `AuditSubmitOrdinarySubmitBaselinesExistingDeepResearchTargetsBeforeSend` прошли 2/2; focused `dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~AuditSubmit" --no-build --no-restore` прошёл 117/117; `update docs` - updated; `update docs --check` - passed; `verify docs` - passed; `verify audit-followups` - passed for 12 actionable findings across 91 saved audit reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Можно готовить следующий package; r36 остаётся последним saved verdict в `previousVerdictChain`, r37/r38/r39 не включаются из-за отсутствия saved reports.

2026-07-03T02:25:00+03:00 - Primary audit r40 сохранён штатной командой как `docs/verdicts/release-management/t-0238-audit-r40.md` с `VERDICT: NEEDS_FIXES`. Аудит подтвердил одиночную область `T-0238`, отсутствие scope leak по `T-0240` и зелёные evidence checks, но нашёл B1 в r40-closure: после позднего выбора `Глубокое исследование` production path повторно проверял только выбранный режим, но не доказывал, что в текущем поле отправки сохранились полный текст prompt-а и ровно одна плашка основного audit ZIP. Новый order-test был признан недостаточным, потому он проверял только последовательность вызовов через proxy-driver, а не реальное состояние composer перед `ClickSendAsync`.

2026-07-03T02:30:00+03:00 - r40 B1 закрыт локально: `SubmitPromptAsync` теперь после `RequireDeepResearchSelectedAsync` вызывает отдельный production guard `RequirePromptPayloadReadyAsync`, который перечитывает текущее состояние поля отправки и падает с `E2D-BUILD-AUDIT-SUBMIT-PAYLOAD-MISSING`, если полный текст сообщения или видимая плашка основного audit ZIP потеряны перед отправкой. Добавлен regression `AuditSubmitPromptPayloadReadyRequiresPromptTextAndAuditZipChip`, который исполняет production `PromptPayloadReadyExpression` на контролируемой DOM-модели и проверяет положительный случай, пустой prompt, отсутствующую ZIP-плашку и ZIP-плашку в истории сообщений; `AuditSubmitPromptSubmissionSelectsDeepResearchAfterAttachmentAndPromptFill` обновлён и требует guard до `ReadConversationMessageCountAsync` и `ClickSendAsync`. Узкий focused test по двум regression прошёл 2/2; полный medium-коридор ещё нужно выполнить перед r41.

2026-07-03T02:37:00+03:00 - Medium-коридор после закрытия r40 B1 прошёл: `dotnet build eng\Electron2D.Build\Electron2D.Build.csproj --no-restore -v:minimal` - passed, 0 warnings; `dotnet build tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --no-restore -v:minimal` - passed, 0 warnings; focused `AuditSubmitPromptPayloadReadyRequiresPromptTextAndAuditZipChip` + `AuditSubmitPromptSubmissionSelectsDeepResearchAfterAttachmentAndPromptFill` прошли 2/2; focused `dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~AuditSubmit" --no-build --no-restore` прошёл 118/118; `update docs` - updated; `update docs --check` - passed; `verify docs` - passed; `verify audit-followups` - passed for 12 actionable findings across 92 saved audit reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Можно готовить r41; `previousVerdictChain` должен включать saved reports до r36 и r40, а r37/r38/r39 не включаются из-за отсутствия saved reports.

2026-07-03T02:52:00+03:00 - Primary audit r41 сохранён штатной командой как `docs/verdicts/release-management/t-0238-audit-r41.md` с `VERDICT: NEEDS_FIXES`. Аудит подтвердил одиночную область `T-0238`, saved chain до r36 плюс r40, зелёные evidence checks и отсутствие новых scope leak/secret issues, но нашёл B1: `PromptPayloadReadyExpression` проверял рядом с prompt-ом любой небольшой видимый элемент с текстом имени ZIP, поэтому обычная метка рядом с полем могла ложно считаться реальной плашкой прикреплённого audit ZIP. Это оставило r40 B1 незакрытым по сути, потому контракт требовал ровно одну видимую плашку основного ZIP, а не произвольный текст.

2026-07-03T02:56:00+03:00 - r41 B1 закрыт локально: `PromptPayloadReadyExpression` теперь ищет не произвольный nearby-текст с именем ZIP, а attachment-specific root рядом с полем отправки. Валидная ZIP-плашка должна иметь признаки прикреплённого файла через `attachment`/`file`/`upload`-атрибуты, русские признаки прикрепления файла или кнопку удаления внутри компактной плашки; история сообщений и обычная текстовая метка рядом с prompt-ом отвергаются. Regression `AuditSubmitPromptPayloadReadyRequiresPromptTextAndAuditZipChip` расширен отрицательным случаем `includePlainFilenameLabel`, который исполняет production `PromptPayloadReadyExpression` и требует `false` для обычного `div` с именем ZIP без признаков прикреплённого файла. Узкий focused test по двум regression прошёл 2/2; полный medium-коридор ещё нужно выполнить перед r42.

2026-07-03T03:00:00+03:00 - Medium-коридор после закрытия r41 B1 прошёл: `dotnet build eng\Electron2D.Build\Electron2D.Build.csproj --no-restore -v:minimal` - passed, 0 warnings; `dotnet build tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --no-restore -v:minimal` - passed, 0 warnings; focused `AuditSubmitPromptPayloadReadyRequiresPromptTextAndAuditZipChip` + `AuditSubmitPromptSubmissionSelectsDeepResearchAfterAttachmentAndPromptFill` прошли 2/2; focused `dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~AuditSubmit" --no-build --no-restore` прошёл 118/118; `update docs` - updated; `update docs --check` - passed; `verify docs` - passed; `verify audit-followups` - passed for 12 actionable findings across 93 saved audit reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Можно готовить r42; `previousVerdictChain` должен включать saved reports до r36, r40 и r41, а r37/r38/r39 не включаются из-за отсутствия saved reports.

2026-07-03T03:13:00+03:00 - Primary audit r42 сохранён штатной командой как `docs/verdicts/release-management/t-0238-audit-r42.md` с `VERDICT: NEEDS_FIXES`. Report содержит B1 о якобы отсутствующих snapshots для `AuditFollowupVerifier.cs` и других файлов `eng/Electron2D.Build`, но это фактически неверно: `.temp/audit/T-0238-audit-r42.zip` содержит `repo-after/eng/Electron2D.Build/AuditFollowupVerifier.cs`, `AuditPackageCommand.cs`, `AuditSubmitCodexChromeCommand.cs`, `AuditSubmitCommand.cs`, `Program.cs`, а `metadata/repo-file-snapshots.json` содержит записи для этих файлов с `afterSnapshot: repo-after/...` и `fullContentIncluded: true`. `audit package verify` r42 на clean worktree `.temp/audit-clean/T-0238-r42-20260703-030516-lf` уже прошёл с `E2D-BUILD-AUDIT-PACKAGE-VERIFIED`, что дополнительно подтверждает согласованность snapshot index. Поэтому r42 B1 закрывается как неверное external finding без изменения кода, тестов или доменного документа; следующий package r43 должен добавить r42 в `previousVerdictChain` и явно включить этот evidence closure в `blockerClosureList`.

2026-07-03T03:13:00+03:00 - `RISKS_AND_NOTES` r42 разобраны: `FOLLOW_UP_FINDING F1` про отсутствие GPL-заголовков закрыт как `not-actionable`, потому Electron2D распространяется под MIT, все проверяемые C#-файлы имеют MIT header, а `verify licenses` прошёл для 661 source files; требование GPL прямо противоречит repository policy. `FOLLOW_UP_FINDING F2` про публичную XML-документацию закрыт как `not-actionable`, потому перечисленные типы в `eng/Electron2D.Build` являются внутренними build-tool implementation types, не публичным API Electron2D; `verify docs` и сборки прошли без этого как acceptance issue. `OUT_OF_SCOPE_NOTE N1` не имеет explicit `Actionable: true/yes/[actionable]`, поэтому по контракту T-0238 не требует closure note.

- audit-followup-closure:
  - source: docs/verdicts/release-management/t-0238-audit-r42.md
  - id: FOLLOW_UP_FINDING F1
  - state: not-actionable
  - target: T-0238
  - rationale: Finding неверен для этого репозитория: Electron2D использует MIT license header, а не GPL. Проверяемые C#-файлы уже имеют project MIT header, `dotnet run --project eng\Electron2D.Build --no-build -- verify licenses` прошёл для 661 source files, поэтому заводить задачу на GPL-заголовки нельзя.

- audit-followup-closure:
  - source: docs/verdicts/release-management/t-0238-audit-r42.md
  - id: FOLLOW_UP_FINDING F2
  - state: not-actionable
  - target: T-0238
  - rationale: Finding неверно классифицирует internal build-tool implementation types как публичный API. Перечисленные `AuditPackageCommand`, `AuditSubmitCommand`, `AuditSubmitCodexChromeCommand`, `Program` и `AuditFollowupVerifier` находятся в `eng/Electron2D.Build` и не являются public Electron2D API; текущие `verify docs`, сборки и focused tests прошли, а требование XML comments для этих internal команд не относится к acceptance scope T-0238.

2026-07-03T03:18:00+03:00 - Closure-only коридор после r42 false blocker прошёл: прямой ZIP/metadata check подтвердил наличие `repo-after/eng/Electron2D.Build/AuditFollowupVerifier.cs` и snapshot entry с `fullContentIncluded: true`; `verify audit-followups` прошёл для 14 actionable findings across 94 saved audit reports после closure r42 F1/F2; `update docs` - updated; `update docs --check` - passed; `verify docs` - passed; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Код, тесты и доменный документ после r42 не менялись; следующий package r43 должен добавить r42 в `previousVerdictChain`, закрыть r42 B1 как invalid evidence finding и включить closure notes r42 F1/F2.

2026-07-03T03:32:00+03:00 - r43 package был создан штатным C#-инструментом и verified на clean repo `.temp/audit-clean/T-0238-r43-20260703-032809-lf`, но primary submit не создал saved verdict report и не записал `conversation-url-r43.txt`: две попытки `audit submit --reuse-conversation` остановились до подтверждённой отправки ZIP с `E2D-BUILD-AUDIT-SUBMIT-CODEX-CHROME-PROTOCOL`. Первая попытка завершилась `Detached while handling command`, повторная попытка завершилась `CDP reattach failed after recoverable detachment` с несколькими timeout `Page.enable`. Это повтор одного класса локального отказа на reused-обсуждении, поэтому третий retry r43 запрещён правилом остановки. Так как отправка не подтверждена и saved report отсутствует, r43 не включается в `previousVerdictChain`. Следующий package r44 должен сохранить тот же scope и closure r42, добавить closure r43 local reused-conversation CDP failure, а submit выполнить штатным поддержанным путём `--new-conversation`, потому текущее reused-обсуждение локально нестабильно на уровне CDP reattach до отправки.

2026-07-03T03:56:00+03:00 - r44 package был создан штатным C#-инструментом и verified на clean repo `.temp/audit-clean/T-0238-r44-20260703-034821-lf`, но primary submit через `--new-conversation` остановился до подтверждённой отправки ZIP, без saved verdict report и без `conversation-url-r44.txt`, с `E2D-BUILD-AUDIT-SUBMIT-PAYLOAD-MISSING`: проверка перед отправкой не увидела одновременно текст сообщения и основной audit ZIP в поле отправки. Это локальный pre-send failure, поэтому r44 не включается в `previousVerdictChain`. Отказ закрыт локально без внешнего аудита: `RequirePromptPayloadReadyAsync` теперь ждёт до `30` секунд готовности поля отправки, возвращает последнюю структурную причину отказа, допускает замену ведущей подсказки `@Глубокое исследование` выбранным режимом и дедуплицирует одну реальную ZIP-плашку, когда имя файла встречается в корне плашки и во вложенном элементе. Regression `AuditSubmitPromptPayloadReadyRequiresPromptTextAndAuditZipChip` расширен этими случаями, а отрицательные проверки пустого поля, отсутствующей ZIP-плашки, обычной текстовой метки и ZIP-плашки из истории сохранены. Узкий focused test вместе с `AuditSubmitPromptSubmissionSelectsDeepResearchAfterAttachmentAndPromptFill` прошёл 2/2; перед следующим package нужен полный medium-коридор, r44 в chain не добавлять.

2026-07-03T04:03:00+03:00 - Medium-коридор после закрытия r44 pre-send failure прошёл: `dotnet build eng\Electron2D.Build\Electron2D.Build.csproj --no-restore -v:minimal` - passed, 0 warnings; `dotnet build tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --no-restore -v:minimal` - passed, 0 warnings; focused `AuditSubmitPromptPayloadReadyRequiresPromptTextAndAuditZipChip` + `AuditSubmitPromptSubmissionSelectsDeepResearchAfterAttachmentAndPromptFill` прошли 2/2; focused `dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~AuditSubmit" --no-build --no-restore` прошёл 118/118 за 5 m 20 s; `update docs` - updated; `update docs --check` - passed; `verify docs` - passed; `verify audit-followups` - passed for 14 actionable findings across 94 saved audit reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Можно готовить r45: `previousVerdictChain` должен включать saved reports до r36 плюс r40/r41/r42, r43/r44 не включаются из-за отсутствия saved verdict reports, а `blockerClosureList` должен содержать closure r43 reused-conversation CDP failure и r44 payload readiness failure.

2026-07-03T11:10:00+03:00 - После отката repo-owned файлов к принятому r45 состоянию сохранён primary report `docs/verdicts/release-management/t-0238-audit-r45.md` с `VERDICT: ACCEPT`, но control-путь остановлен до saved control report: пользователь изменил требование к submit-механизму. Новый baseline должен быть обычным ChatGPT-запросом без `@Глубокое исследование`; режим `Глубокое исследование` остаётся только явным резервным `--deep-research` и в нём кнопка выбирается до вставки текста. Это меняет код, документы, тесты и request text после primary `ACCEPT`, поэтому r45 нельзя продолжать control audit; следующий внешний запуск должен быть новым primary package после зелёного локального коридора.

2026-07-03T11:39:09+03:00 - Закрыто локально пользовательское изменение baseline submit: `AUDIT-REQUEST.md` больше не содержит `@Глубокое исследование`; `audit package` не требует этот маркер; `audit submit` по умолчанию не выбирает Deep Research и валидирует новый ответ ассистента текущей отправки; `--deep-research` сохранён как явный резервный режим и выбирает кнопку `Глубокое исследование` до вставки текста. Medium-коридор прошёл: `dotnet build eng\Electron2D.Build\Electron2D.Build.csproj --no-restore -v:minimal` - passed, 0 warnings; `dotnet build tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --no-restore -v:minimal` - passed, 0 warnings; focused `AuditSubmit`/`AuditPackageMessage`/documentation suite - 134/134 passed; `update docs` - updated; `update docs --check` - passed; `verify docs` - passed; `verify audit-followups` - passed for 14 actionable findings across 95 saved audit reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Следующий package должен быть новым primary r57: включить saved reports до r45, не включать r46-r56 как verdict chain, потому это локальные/исторические попытки без saved verdict reports текущего отката.

2026-07-03T11:54:12+03:00 - Первый запуск `audit package` r57 остановился локально до создания ZIP: сначала команда была вызвана с Windows-разделителем в `--out`, затем после исправления аргумента финальная проверка архива дала `E2D-BUILD-AUDIT-ABSOLUTE-PATH` на `repo-after/docs/verdicts/release-management/t-0238-audit-r45.md`. Причина не в новом `AUDIT-REQUEST.md`, а в сохранённом внешнем r45 report: `OUT_OF_SCOPE_NOTE` цитирует синтетический пример Windows-пути с диском и сегментом `Users/example/source.md`. Локальное закрытие: финальная проверка содержимого ZIP теперь применяет то же ограниченное исключение к `repo-after/` и `repo-before/` снимкам файлов из `previousVerdictChain`, которое уже действует для предварительной проверки previous verdict и его diff-блоков; обычные файлы задачи, `AUDIT-REQUEST.md`, evidence, manifest и metadata продолжают блокироваться при Windows drive path. Добавлен regression `AuditPackageAllowsPlaceholderWindowsUserPathsInPreviousVerdicts`; перед повторным r57 нужен новый полный Medium.

2026-07-03T11:59:48+03:00 - Medium-коридор после closure r57 local packaging false positive прошёл: `dotnet build eng\Electron2D.Build\Electron2D.Build.csproj --no-restore -v:minimal` - passed, 0 warnings; `dotnet build tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --no-restore -v:minimal` - passed, 0 warnings; focused `AuditPackageAllowsPlaceholderWindowsUserPathsInPreviousVerdicts` - 1/1 passed; focused `AuditSubmit`/`AuditPackageMessage`/`AuditPackageCopiesStaticRequestVerbatim`/`AuditRequestRequiresReadableRussianReportExplanations`/`AuditPackageDocumentationDefines`/`AuditPackageAllowsPlaceholderWindowsUserPathsInPreviousVerdicts` suite - 135/135 passed; `update docs` - updated; `update docs --check` - passed; `verify docs` - passed; `verify audit-followups` - passed for 14 actionable findings across 95 saved audit reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Следующий r57 package должен использовать обновлённый check-фильтр 135/135 и оставаться новым primary после r45 ACCEPT, без r46-r56 в `previousVerdictChain`.

2026-07-03T12:07:52+03:00 - Повторный `audit package` r57 снова остановился локально до создания ZIP с тем же диагностическим кодом `E2D-BUILD-AUDIT-ABSOLUTE-PATH`, но уже на `T-0238.patch`: новый regression и task note сами содержали непрерывный синтетический Windows drive path. Закрытие: test fixture теперь собирает этот текст через `string.Concat`, чтобы runtime-проверка оставалась прежней, но patch не содержал машинный путь; task note и r57 config описывают пример как Windows-путь с диском и сегментом `Users/example/source.md`, без непрерывного drive-prefix. После правки пройдены build integration tests, focused regression 1/1, `update docs`, `update docs --check`, `verify docs`, `verify audit-followups`, `verify licenses` и `git diff --check`; перед package остаётся синхронизировать индекс после этой заметки.

2026-07-03T12:17:47+03:00 - r57 package был создан штатной командой и отдельно verified на clean worktree `.temp/audit-clean/T-0238-r57-20260703-121315-lf`; `audit package message` подтвердил, что первая строка начинается с обычного текста `Вы проводите внешний аудит...`, без `@Глубокое исследование`. Primary submit r57 был запущен обычным ChatGPT-запросом без `--deep-research`, но пользователь увидел, что сразу после открытия project URL команда прокручивает страницу к середине. Submit-процесс был остановлен до saved verdict report `docs/verdicts/release-management/t-0238-audit-r57.md`; локальный `conversation-url-r57.txt` уже успел появиться, поэтому r57 не использовать как сохранённый verdict и не добавлять в `previousVerdictChain`. Закрытие: из `SubmitAndWaitForReportAsync` удалён ранний `ScrollConversationToBottomAsync` между ожиданием поля сообщения и отправкой prompt-а; добавлен source-level regression `AuditSubmitDoesNotScrollProjectPageBeforePromptSubmission`, доменный документ теперь прямо говорит, что обычный submit ждёт поле сообщения без предварительного прокручивания страницы проекта. Перед новым package нужен полный Medium.

2026-07-03T12:23:02+03:00 - Medium-коридор после scroll-fix прошёл: `dotnet build eng\Electron2D.Build\Electron2D.Build.csproj --no-restore -v:minimal` - passed, 0 warnings; `dotnet build tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --no-restore -v:minimal` - passed, 0 warnings; focused `AuditSubmitDoesNotScrollProjectPageBeforePromptSubmission` - 1/1 passed; focused `AuditSubmit`/`AuditPackageMessage`/`AuditPackageCopiesStaticRequestVerbatim`/`AuditRequestRequiresReadableRussianReportExplanations`/`AuditPackageDocumentationDefines`/`AuditPackageAllowsPlaceholderWindowsUserPathsInPreviousVerdicts` suite - 136/136 passed; `update docs` - updated; `update docs --check` - passed; `verify docs` - passed; `verify audit-followups` - passed for 14 actionable findings across 95 saved audit reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Следующий package должен быть r58: `previousVerdictChain` включает saved reports до r45, r46-r57 не включаются, потому r57 не имеет saved verdict report.

2026-07-03T12:43:00+03:00 - Primary audit r58 сохранён штатной командой как `docs/verdicts/release-management/t-0238-audit-r58.md` с `VERDICT: NEEDS_FIXES`. Blocker B1: в раннем разделе `docs/release-management/audit-package.md` остался старый pre-prompt scroll contract. B2: scroll-fix был закрыт source-level тестом, а нужен поведенческий driver/harness. B3: `verify audit-followups` не распознавал `FOLLOW_UP_FINDING` с Markdown-выделением, поэтому пропустил r45 F1/F2. B4: previous verdict exception отключил secret scan слишком широко. Локальное закрытие начато: ранний документный контракт исправлен, добавляется driver-based project preparation test, parser расширяется под Markdown-выделение, previous verdict exception сужается до проверки машинных путей, а secret scan возвращается.

- audit-followup-closure:
  - source: docs/verdicts/release-management/t-0238-audit-r45.md
  - id: FOLLOW_UP_FINDING F1
  - state: duplicate
  - target: docs/release-management/audit-package.md
  - rationale: Общие правила внешнего аудита уже перенесены в доменный документ `docs/release-management/audit-package.md` и локальный prompt `.codex/prompts/goal-task-loop.md`; оставшийся подробный текст в `TASKS.md` является историческими closure notes текущей T-0238 и не должен переписываться или переноситься внутри этой же audit-итерации, потому это изменило бы доказательную историю пакета.

- audit-followup-closure:
  - source: docs/verdicts/release-management/t-0238-audit-r45.md
  - id: FOLLOW_UP_FINDING F2
  - state: not-actionable
  - target: T-0238
  - rationale: Перечисленные типы находятся в `eng/Electron2D.Build` и являются внутренней реализацией repository build tool, а не публичным API Electron2D; обязательное XML-документирование публичного API из `AGENTS.md` к ним не применяется как acceptance-критерий T-0238, при этом `verify docs` и `verify licenses` остаются обязательными проверками.

2026-07-03T13:03:15+03:00 - Medium-коридор после закрытия r58 B1-B4 прошёл: `dotnet build eng\Electron2D.Build\Electron2D.Build.csproj --no-restore -v:minimal` - passed, 0 warnings; `dotnet build tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --no-restore -v:minimal` - passed, 0 warnings; focused closure slice `AuditSubmitDoesNotScrollProjectPageBeforePromptSubmission`/`AuditPackageDocumentationDefinesNoPrePromptScrollInOrdinarySubmit`/`AuditWorkflowFollowupParserFindsMarkdownFormattedStructuredRiskMarkers`/`AuditPackageRejectsSecretValuesInPreviousVerdicts`/`AuditPackageAllowsPlaceholderWindowsUserPathsInPreviousVerdicts` - 5/5 passed; focused `AuditSubmit`/`AuditPackageMessage`/static request/documentation/parser/secret suite - 139/139 passed; `update docs` - updated; `update docs --check` - passed; `verify docs` - passed; `verify audit-followups` - passed for 16 actionable findings across 96 saved audit reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Следующий package должен быть r59: включить r58 в `previousVerdictChain`, закрыть B1-B4 в `blockerClosureList`, не включать r57 как saved report, отправлять primary через `--reuse-conversation` в r58-чат. Если primary r59 вернёт `VERDICT: ACCEPT`, control audit должен идти в новом чате с clean-control ZIP: пустые `metadata.previousVerdictChain`/`metadata.blockerClosureList` и без сохранённых md verdict-отчётов в архиве.

2026-07-03T13:26:30+03:00 - Локальный r59 package был остановлен до создания ZIP: после включения secret scan для previous verdict-файлов команда корректно сканирует saved r58 report, но внешний текст r58 содержит синтетический пример значения-заглушки `<non-placeholder>` рядом с маркером `token`, поэтому старое правило считало его конкретным секретом. Saved report вручную не изменялся. Закрытие: список разрешённых redacted placeholder-значений расширен значением `<non-placeholder>`, добавлен regression `AuditPackageAllowsPlaceholderSecretValuesInPreviousVerdicts`, а отрицательный regression `AuditPackageRejectsSecretValuesInPreviousVerdicts` продолжает отклонять конкретное значение. Одновременно пользователь уточнил операторское правило: исправительная primary-итерация после r58 `NEEDS_FIXES` должна идти в тот же primary-чат через `--reuse-conversation`, а control audit после primary `ACCEPT` должен идти в новый чат проекта с clean-control ZIP, пустыми `metadata.previousVerdictChain`/`metadata.blockerClosureList` и без сохранённых Markdown verdict-отчётов в архиве. Контракт усилен в `docs/release-management/audit-package.md`, `.codex/prompts/goal-task-loop.md`, `AGENTS.md`; добавлен regression `AuditSubmitControlAuditUsesConfiguredProjectRoot`, который фиксирует, что `--control-audit` стартует с project root, а существующий `AuditSubmitControlAuditRejectsConversationUrlBeforeBrowserLaunch` оставляет запрет на старый conversation URL. Medium-срез после этих правок прошёл: `dotnet build eng\Electron2D.Build\Electron2D.Build.csproj --no-restore -v:minimal` - passed, 0 warnings; `dotnet build tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --no-restore -v:minimal` - passed, 0 warnings; focused control/placeholder/docs slice - 6/6 passed; focused `AuditSubmit`/package message/static request/documentation/parser/secret suite - 143/143 passed. Перед r59 package ещё нужно выполнить generated docs/update/followups/licenses/diff checks для текущего дерева.

2026-07-03T13:27:51+03:00 - Завершён полный Medium после записи 13:26: `dotnet run --project eng\Electron2D.Build --no-build -- update docs` - updated; `update docs --check` - passed; `verify docs` - passed; `verify audit-followups` - passed for 16 actionable findings across 96 saved audit reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Перед r59 package нужно ещё раз синхронизировать generated docs index после этой новой записи `TASKS.md`, затем собирать r59 с r58 в `previousVerdictChain` и отправлять primary через `--reuse-conversation`.

2026-07-03T14:01:31+03:00 - r59 primary report сохранён как `docs/verdicts/release-management/t-0238-audit-r59.md` с `VERDICT: NEEDS_FIXES`. Единственный blocker B1: secret scanner разрешал значение, которое начинается с разрешённой заглушки и затем содержит конкретный текст, например placeholder + concrete suffix. Закрытие: `IsSecretCandidatePlaceholderBoundary` теперь принимает только завершающее обрамление/пунктуацию до конца значения и больше не считает пробел безопасной границей после placeholder; одиночный `<non-placeholder>` остаётся допустимой redacted-заглушкой. Добавлены regression `AuditPackageRejectsPlaceholderSecretValuesWithConcreteSuffixInPreviousVerdicts` и `AuditPackageRejectsPlaceholderSecretValuesWithConcreteSuffixInTaskOwnedFiles`, а `AuditPackageAllowsPlaceholderSecretValuesInPreviousVerdicts` переписан так, чтобы placeholder стоял всем значением. `docs/release-management/audit-package.md` теперь прямо говорит: текст после заглушки не является безопасным продолжением и остаётся blocker-ом. Fast/Medium после closure r59 B1 прошёл: `dotnet build eng\Electron2D.Build\Electron2D.Build.csproj --no-restore -v:minimal` - passed, 0 warnings; `dotnet build tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --no-restore -v:minimal` - passed, 0 warnings; focused secret placeholder suffix slice - 5/5 passed; focused `AuditSubmit`/package message/static request/documentation/parser/secret suite - 145/145 passed. Перед r60 package нужно выполнить generated docs/update/followups/licenses/diff checks. r60 должен включить r59 в `previousVerdictChain`, добавить closure r59 B1 в `blockerClosureList`, не включать r57 как saved report и отправляться primary через `--reuse-conversation`.

2026-07-03T14:02:56+03:00 - Завершён полный Medium после closure r59 B1: `update docs` - updated; `update docs --check` - passed; `verify docs` - passed; `verify audit-followups` - passed for 16 actionable findings across 97 saved audit reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Перед r60 package нужно ещё раз синхронизировать generated docs index после этой новой записи `TASKS.md`.

2026-07-03T14:05:50+03:00 - Первый локальный r60 package остановился до создания ZIP с `E2D-BUILD-AUDIT-SECRET-DETECTED` на `docs/release-management/audit-package.md`: после исправления r59 B1 доменный документ ещё содержал старые inline-примеры assignment-строк с placeholder-значениями и продолжением обычного текста в той же строке. Закрытие: эти примеры переписаны как описание ключа и значения без assignment-синтаксиса, а текст контракта теперь прямо говорит, что placeholder должен занимать всё значение; текст после замещающего значения не считается безопасной заглушкой. Focused docs/secret slice `AuditPackageAllowsAuditPackageDomainDocumentSecretScanDescription`/`AuditPackageDocumentationRequiresMessageDeepResearchAndAttachedPackage`/`AuditPackageRejectsPlaceholderSecretValuesWithConcreteSuffixInPreviousVerdicts`/`AuditPackageRejectsPlaceholderSecretValuesWithConcreteSuffixInTaskOwnedFiles` прошёл 4/4. Перед повторным r60 package нужно обновить generated docs index и пройти docs/followups/licenses/diff checks.

2026-07-03T14:15:06+03:00 - Второй локальный r60 package остановился до создания ZIP с тем же failure class `E2D-BUILD-AUDIT-SECRET-DETECTED`, теперь на saved r58 report `docs/verdicts/release-management/t-0238-audit-r58.md`: сохранённый внешний отчёт r58 содержит нейтральную reviewer phrase `<non-placeholder> or private key marker`, а saved verdict вручную не редактируется. Применён stop-loss: `rNN` не увеличивался, внешний submit не запускался, сначала добавлен локальный regression и пройден Medium. Закрытие: reviewer phrase разрешена только как точное полное allowlist-значение, без возврата prefix-логики; placeholder + concrete suffix всё ещё отклоняется. Добавлен regression `AuditPackageAllowsKnownReviewerPlaceholderPhraseInPreviousVerdicts`; fixture path укорочен после Windows path-length отказа. Fast/Medium после второго r60 local failure прошёл: build build-tool - passed, 0 warnings; build integration tests - passed, 0 warnings; focused reviewer/placeholder/secret slice - 6/6 passed; focused `AuditSubmit`/package message/static request/documentation/parser/secret suite - 146/146 passed. Перед повторным r60 package нужно выполнить generated docs/update/followups/licenses/diff checks.

2026-07-03T14:23:14+03:00 - Уточнено закрытие второго r60 local failure после проверки фактической r58 строки: сохранённый r58 report содержит русскую фразу `или private key marker` внутри инструкции аудитора, а не английское `or`. Allowlist ограничен этой reviewer phrase с пунктуационной границей; общее whitespace-prefix правило не возвращалось. Regression `AuditPackageAllowsKnownReviewerPlaceholderPhraseInPreviousVerdicts` обновлён на фактическую форму r58. Повторный Fast/Medium прошёл: build build-tool - passed, 0 warnings; build integration tests - passed, 0 warnings; focused reviewer/placeholder/secret slice - 6/6 passed; focused `AuditSubmit`/package message/static request/documentation/parser/secret suite - 146/146 passed. Перед повторным r60 package нужно выполнить generated docs/update/followups/licenses/diff checks.

2026-07-03T14:32:00+03:00 - Уточнено закрытие r60 local failure после проверки сохранённого r59 report: внешний отчёт r59 содержит синтетические reviewer-примеры `concrete-secret`/`concrete-value` рядом с замещающими значениями `<redacted>` и `<non-placeholder>`. Saved report вручную не редактировался. Закрытие осталось узким: общая проверка placeholder не разрешает пробел и произвольный suffix, task-owned файлы по-прежнему отклоняют placeholder + concrete suffix, а предыдущие external reports получают только точечный allowlist для фактических reviewer phrase prefixes из r58/r59. Старая английская форма `or private key marker` удалена из общего списка placeholder-значений, чтобы allowlist соответствовал сохранённому evidence. Regression `AuditPackageAllowsKnownReviewerPlaceholderPhraseInPreviousVerdicts` покрывает r58/r59 reviewer phrases, а `AuditPackageRejectsPlaceholderSecretValuesWithConcreteSuffixInPreviousVerdicts` и `AuditPackageRejectsPlaceholderSecretValuesWithConcreteSuffixInTaskOwnedFiles` сохраняют reject для произвольных suffix. Wide Medium перед этой записью прошёл: focused `AuditSubmit`/package message/static request/documentation/parser/secret suite - 146/146 passed. Перед повторным r60 package нужно пересобрать после сужения allowlist, пройти focused/Medium и синхронизировать generated docs index.

2026-07-03T14:40:00+03:00 - Повторный Medium после удаления старой английской placeholder phrase прошёл: `dotnet build eng\Electron2D.Build\Electron2D.Build.csproj --no-restore -v:minimal` - passed, 0 warnings; `dotnet build tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --no-restore -v:minimal` - passed, 0 warnings; focused reviewer/placeholder/secret slice - 6/6 passed; focused `AuditSubmit`/package message/static request/documentation/parser/secret suite - 146/146 passed. Локально сохранённого `T-0238-audit-r60.zip` и `docs/verdicts/release-management/t-0238-audit-r60.md` ещё нет; следующий безопасный шаг - `update docs`, `update docs --check`, `verify docs`, `verify audit-followups`, `verify licenses`, `git diff --check`, затем r60 package/clean verify/message и primary submit через `--reuse-conversation` в saved r59 chat.

2026-07-03T14:43:00+03:00 - Полный локальный коридор перед r60 package прошёл: `update docs` - updated; `update docs --check` - passed; `verify docs` - passed; `verify audit-followups` - passed for 16 actionable findings across 97 saved audit reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Из-за этой новой записи перед package нужен финальный `update docs`/`update docs --check`; затем r60 package должен использовать `.temp/audit/T-0238/audit-package.r60.input.json` с r59 в `previousVerdictChain`, closure r59 B1 и r60 local stop-loss closure notes, а primary submit должен идти через `--reuse-conversation`.

2026-07-03T14:49:00+03:00 - Повторный r60 package остановился до ZIP с `E2D-BUILD-AUDIT-SECRET-DETECTED` на `repo-before/docs/release-management/audit-package.md`: текущий `repo-after` документ уже исправлен, но baseline-снимок содержит старое inline-описание synthetic placeholder examples. Submit не запускался и `rNN` не увеличивался. Закрытие: legacy allowance ограничен только `repo-before/` snapshot entries и только случаями, где placeholder закрыт punctuation/backtick перед обычным пояснением; `repo-after`, patch/current files и task-owned файлы остаются strict, а concrete secret assignment в `repo-before` всё равно блокирует package. Старый test `AuditPackageAllowsSyntheticSecretPlaceholdersFollowedByProse` заменён на reject-семантику `AuditPackageRejectsSyntheticSecretPlaceholdersFollowedByProseInTaskOwnedFiles`; добавлены `AuditPackageAllowsLegacyPlaceholderProseInRepoBeforeSnapshots` и `AuditPackageRejectsConcreteSecretAssignmentsInRepoBeforeSnapshots`. Повторный Medium прошёл: build build-tool - passed, 0 warnings; build integration tests - passed, 0 warnings; focused repo-before/reviewer/secret slice - 8/8 passed; focused `AuditSubmit`/package message/static request/documentation/parser/secret suite - 149/149 passed. Перед повторным r60 package нужно обновить `.temp/audit/T-0238/audit-package.r60.input.json`, синхронизировать generated docs index и пройти docs/followups/licenses/diff checks.

2026-07-03T14:58:00+03:00 - Полный локальный коридор после repo-before closure прошёл: `.temp/audit/T-0238/audit-package.r60.input.json` обновлён; `update docs` - updated; `update docs --check` - passed; `verify docs` - passed; `verify audit-followups` - passed for 16 actionable findings across 97 saved audit reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Из-за этой записи перед r60 package нужен финальный `update docs`/`update docs --check`; затем можно повторять r60 package без увеличения итерации и без внешнего submit до успешного ZIP/clean verify.

2026-07-03T15:24:00+03:00 - Primary r60 сохранён штатной командой как `docs/verdicts/release-management/t-0238-audit-r60.md` с `VERDICT: NEEDS_FIXES`. Blocker B1: reviewer allowlist был prefix-based и разрешал произвольный suffix после reviewer-фразы. Blocker B2: reviewer allowlist применялся глобально, а не только к immutable previous verdict reports. Blocker B3: legacy placeholder prose allowance для `repo-before/` был широким для всех baseline-снимков, а не точечным known-safe случаем. Закрытие: reviewer allowlist теперь сравнивает только точные normalized values и включается только для файлов из `previousVerdictChain`, их `repo-before/`/`repo-after` snapshots и patch-блоков; task-owned files, evidence, manifest, request, metadata и обычная документация такого исключения не получают. `repo-before` legacy allowance сужен до точного значения старой строки `docs/release-management/audit-package.md`; произвольный suffix или concrete secret в другом `repo-before` snapshot блокирует package. Добавлены regressions `AuditPackageRejectsReviewerPlaceholderPhraseSuffixInPreviousVerdicts`, `AuditPackageRejectsReviewerPlaceholderPhraseInTaskOwnedFiles`, обновлён `AuditPackageAllowsKnownReviewerPlaceholderPhraseInPreviousVerdicts`, а `AuditPackageAllowsLegacyPlaceholderProseInRepoBeforeSnapshots` использует фактическую старую строку документа. Fast/Medium после closure r60 B1-B3 прошёл: build build-tool - passed, 0 warnings; build integration tests - passed, 0 warnings; focused B1-B3 secret slice - 9/9 passed; focused `AuditSubmit`/package message/static request/documentation/parser/secret suite - 151/151 passed. Следующий package должен быть r61: включить r60 в `previousVerdictChain` и `repoFileAllowlist`, добавить closure B1-B3 r60 в `blockerClosureList`, пройти docs/followups/licenses/diff checks и отправлять primary через `--reuse-conversation`.

2026-07-03T15:27:00+03:00 - Полный локальный коридор после записи r60 closure прошёл: `.temp/audit/T-0238/audit-package.r61.input.json` создан с r60 в `previousVerdictChain` и closure B1-B3; `update docs` - updated; `update docs --check` - passed; `verify docs` - passed; `verify audit-followups` - passed for 16 actionable findings across 98 saved audit reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Из-за этой записи перед r61 package нужен финальный `update docs`/`update docs --check`, затем package/clean verify/message/primary submit через `--reuse-conversation`.

2026-07-03T15:36:00+03:00 - Первый r61 package остановился до ZIP на saved r58 report: exact reviewer allowlist использовал сокращённую reviewer-фразу, а scanner берёт весь остаток строки после того, как ключ `token` получает замещающее значение `...`. Submit не запускался, rNN не увеличивался. Закрытие: `PreviousVerdictReviewerSecretPlaceholderValues` обновлён до полных normalized values из saved r58/r59/r60; positive regression `AuditPackageAllowsKnownReviewerPlaceholderPhraseInPreviousVerdicts` синхронизирован с этими полными строками. Повторный Fast/Medium прошёл: build build-tool - passed, 0 warnings; build integration tests - passed, 0 warnings; focused B1-B3 secret slice - 9/9 passed; focused `AuditSubmit`/package message/static request/documentation/parser/secret suite - 151/151 passed. Перед повторным r61 package нужно заново выполнить docs/followups/licenses/diff checks и синхронизировать generated docs index.

2026-07-03T15:39:00+03:00 - Docs/followups/licenses/diff corridor после exact-value fix прошёл: `update docs` - updated; `update docs --check` - passed; `verify docs` - passed; `verify audit-followups` - passed for 16 actionable findings across 98 saved audit reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Из-за этой записи перед r61 package нужен финальный `update docs`/`update docs --check`; затем можно повторять r61 package без увеличения rNN и без внешнего submit до успешного ZIP/clean verify.

2026-07-03T15:41:00+03:00 - Повторный r61 package остановился до ZIP на `TASKS.md`: новая closure note сама содержала inline assignment-синтаксис с ключом `token` и замещающим значением. Submit не запускался, rNN не увеличивался. Закрытие: запись переписана без assignment-синтаксиса, смысл сохранён как описание ключа и значения. Перед повторным package нужно синхронизировать generated docs index.

2026-07-03T15:48:00+03:00 - Следующий r61 package остановился до ZIP на `eng/Electron2D.Build/AuditPackageCommand.cs`: точные allowlist-значения были записаны в исходнике с буквальным assignment-синтаксисом reviewer examples. Submit не запускался, rNN не увеличивался. Закрытие: машинные строки allowlist и соответствующие тестовые строки разбиты через `string.Concat`/helper values, поэтому runtime values остались точными, но source files больше не содержат запрещённый assignment-паттерн. Проверка `rg` по `AuditPackageCommand.cs`, `RepositoryBuildToolTests.cs`, `audit-package.md` и `TASKS.md` не нашла таких паттернов. Повторный Fast/Medium прошёл: build build-tool - passed, 0 warnings; build integration tests - passed, 0 warnings; focused B1-B3 secret slice - 9/9 passed; focused `AuditSubmit`/package message/static request/documentation/parser/secret suite - 151/151 passed. Перед package нужно заново выполнить docs/followups/licenses/diff checks.

2026-07-03T15:51:00+03:00 - Docs/followups/licenses/diff corridor после удаления assignment-паттернов из source/task прошёл: `update docs` - updated; `update docs --check` - passed; `verify docs` - passed; `verify audit-followups` - passed for 16 actionable findings across 98 saved audit reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Из-за этой записи перед r61 package нужен финальный `update docs`/`update docs --check`, затем можно повторять r61 package.

2026-07-03T15:58:00+03:00 - Следующий r61 package остановился до ZIP на `T-0238.patch`: exact allowlist source diff содержал непрерывный пример Windows-пути из saved r58 report. Submit не запускался, rNN не увеличивался. Закрытие: путь в allowlist и positive regression разбит через `string.Concat("C", ":/...")`; runtime exact value сохранён, но patch больше не содержит машинный путь. `rg` по изменённым source/task/docs не нашёл буквальные Windows-path или assignment-паттерны. Повторный Fast/Medium прошёл: build build-tool - passed, 0 warnings; build integration tests - passed, 0 warnings; focused B1-B3 secret slice - 9/9 passed; focused `AuditSubmit`/package message/static request/documentation/parser/secret suite - 151/151 passed. Перед package нужно заново выполнить docs/followups/licenses/diff checks.

2026-07-03T16:05:00+03:00 - Следующий r61 package остановился до ZIP на `repo-before/docs/release-management/audit-package.md`: baseline содержит вторую known-safe historical placeholder phrase для TRX display-name примера. Submit не запускался, rNN не увеличивался. Закрытие: `LegacyRepoBeforeSecretPlaceholderValues` расширен вторым точным normalized value из baseline, а positive regression `AuditPackageAllowsLegacyPlaceholderProseInRepoBeforeSnapshots` теперь покрывает обе старые строки. Повторный Fast/Medium прошёл: build build-tool - passed, 0 warnings; build integration tests - passed, 0 warnings; focused B1-B3 secret slice - 9/9 passed; focused `AuditSubmit`/package message/static request/documentation/parser/secret suite - 151/151 passed. Перед package нужно заново выполнить docs/followups/licenses/diff checks.

2026-07-03T16:24:00+03:00 - Primary r61 сохранён как `docs/verdicts/release-management/t-0238-audit-r61.md` с `VERDICT: NEEDS_FIXES`. Единственный blocker B1: stale `[InlineData("@Глубокое исследование")]` в `AuditPackageFailsWhenStaticRequestLacksRequiredMarkers` всё ещё требовал старый Deep Research marker как обязательный static request marker. Закрытие: obsolete InlineData удалён; ordinary ChatGPT baseline остаётся защищён существующими positive checks, которые проверяют отсутствие `@Глубокое исследование` в request/message. Проверки: `AuditPackageFailsWhenStaticRequestLacksRequiredMarkers` - 14/14 passed; расширенный focused `AuditSubmit`/package message/static request/documentation/parser/secret suite с этим тестом - 165/165 passed. Следующий package должен быть r62: включить r61 в `previousVerdictChain`, добавить closure r61 B1, пройти docs/followups/licenses/diff checks и отправлять primary через `--reuse-conversation`.

2026-07-03T16:56:00+03:00 - r62 package создан штатной командой и отдельно verified на clean worktree `.temp/audit-clean/T-0238-r62-20260703-164844-lf`; `audit package message` подтвердил ordinary prompt без `@Глубокое исследование`. Primary submit r62 через `--reuse-conversation` не сохранил verdict report: `audit submit` завершился локальным `E2D-BUILD-AUDIT-SUBMIT-REPORT-INVALID`, потому валидатор требовал от ordinary `ACCEPT`-ответа словарно распознанную фразу в `CLOSURE_DECISION`, разрешающую закрытие задачи, текущего изменения или проверяемого пакета. Это не `VERDICT: ACCEPT` T-0238 и не saved external report. Решение пользователя: не требовать специальную формулировку в `CLOSURE_DECISION`; первой строки `VERDICT: ACCEPT` достаточно, если полный отчёт содержит обязательные секции и `BLOCKERS` не содержит `B1`..`Bn`. Закрытие локального отказа: дополнительный `CLOSURE_DECISION` gate удаляется из валидатора и документации. Следующий package должен быть r63: r62 не добавлять в `previousVerdictChain`, потому сохранённого Markdown-отчёта нет; `scopeSummary` должен отметить локальный format-invalid отказ и его закрытие через упрощение валидатора.

2026-07-03T17:22:00+03:00 - Medium после удаления дополнительного `CLOSURE_DECISION` gate прошёл: build integration tests - passed, 0 warnings; focused validator/static request slice - passed; расширенный focused `AuditSubmit`/package message/static request/documentation/parser/secret suite - 165/165 passed; `update docs` - updated; `update docs --check` - passed; `verify docs` - passed; `verify audit-followups` - passed for 16 actionable findings across 99 saved audit reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Перед r63 package нужно ещё раз синхронизировать generated docs index после этой записи, затем собрать r63 без r62 в `previousVerdictChain` и отправить primary через `--reuse-conversation`.

2026-07-03T17:50:00+03:00 - Primary r63 сохранён штатной командой как `docs/verdicts/release-management/t-0238-audit-r63.md` с `VERDICT: NEEDS_FIXES`. Blocker B1: после решения пользователя "ACCEPT достаточно" код и тесты сняли словарный gate `CLOSURE_DECISION`, но ранний и фактический разделы `docs/release-management/audit-package.md` и критерий T-0238 всё ещё требовали явную фразу о закрытии. Blocker B2: `ValidateReportMatchesSubmitIteration` отклонял старые evidence/ZIP ссылки, но не требовал явных текущих `metadata.taskId` и `metadata.iteration`, поэтому markerless report мог сохраниться. Закрытие: доменный документ и критерий T-0238 синхронизированы с правилом `VERDICT: ACCEPT` + обязательные секции + текущие `metadata.taskId`/`metadata.iteration` + отсутствие `B1`..`Bn` в `BLOCKERS`; `CLOSURE_DECISION` больше не является словарным барьером. `ValidateReportMatchesSubmitIteration` теперь после stale-checks требует текущую metadata-пару и принимает обычный Markdown-вариант с backtick-ами. Тест `AuditSubmitRejectsDownloadedReportThatOnlyReferencesPreviousIteration` теперь отклоняет markerless report с `E2D-BUILD-AUDIT-SUBMIT-REPORT-INVALID`, а `AuditPackageDocumentationDefinesStrictVerdictExtractionRule` закрепляет новый текст документации и отсутствие старого требования. Focused B1/B2 checks прошли: build integration tests - passed, 0 warnings; `AuditSubmitRejectsDownloadedReportThatOnlyReferencesPreviousIteration`/`AuditPackageDocumentationDefinesStrictVerdictExtractionRule`/`AuditSubmitDownloadReportOnlyValidatesDownloadedMarkdown`/`AuditSubmitReportExtractorRequiresSingleAllowedReportCandidate` - 4/4 passed. Следующий package должен быть r64: включить r63 в `previousVerdictChain` и `repoFileAllowlist`, добавить closure B1/B2 r63 в `blockerClosureList`, пройти полный Medium и отправлять primary через `--reuse-conversation`.

2026-07-03T18:02:00+03:00 - До r64 закрыт локальный blocker ordinary submit: сохранённые ordinary reports r58+ теряли Markdown-форматирование, потому путь обычного ответа читал DOM как плоский текст. Закрытие: `audit submit` для ordinary ChatGPT baseline теперь извлекает только новый ответ ассистента текущей отправки через Markdown-рендерер DOM: сохраняет списки, inline-code, fenced code blocks, выделение и ссылки, а элементы управления вроде кнопки `Копировать ответ` не попадают в отчёт. Добавлен regression `AuditSubmitOrdinaryAssistantExtractionPreservesMarkdownFormatting`, который проверяет Markdown-структуру ordinary assistant message без использования буфера обмена или внешней кнопки копирования. Focused checks прошли: build integration tests - passed, 0 warnings; `AuditSubmitOrdinaryAssistantExtractionPreservesMarkdownFormatting`/`AuditSubmitReportExtractorRequiresSingleAllowedReportCandidate`/`AuditSubmitRejectsDownloadedReportThatOnlyReferencesPreviousIteration`/`AuditPackageDocumentationDefinesStrictVerdictExtractionRule`/`AuditPackageDocumentationRequiresMessageDeepResearchAndAttachedPackage` - 5/5 passed. Перед r64 package нужно повторить полный Medium после синхронизации generated docs index.

2026-07-03T18:14:00+03:00 - Medium после Markdown-исправления ordinary submit прошёл. В ходе широкого focused suite найден и исправлен хрупкий source-level assert: старый Deep Research тест запрещал literal `'main'` во всём `AuditSubmitCodexChromeCommand.cs`, а Markdown renderer законно использует `main` как HTML block tag. Guard сужен до `ReportExportButtonClickExpression`, где запрет относится к выбору export-кнопки, а не к independent Markdown renderer. Итоговые проверки: build `eng/Electron2D.Build` - passed, 0 warnings; `update docs` - updated; `update docs --check` - passed; build integration tests - passed, 0 warnings; failed static guard rerun - passed 1/1; расширенный focused `AuditSubmit`/package message/static request/documentation/parser/secret suite - 166/166 passed; `verify docs` - passed; `verify audit-followups` - passed for 16 actionable findings across 100 saved audit reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Перед r64 package нужно ещё раз синхронизировать generated docs index после этой записи, затем включить r63 в `previousVerdictChain`/`repoFileAllowlist` и отправлять primary через `--reuse-conversation`.

2026-07-03T18:46:00+03:00 - Primary r64 создан, clean-verified на `.temp/audit-clean/T-0238-r64-20260703-182152-lf` и отправлен штатно через `--reuse-conversation`; отчёт сохранён как `docs/verdicts/release-management/t-0238-audit-r64.md` с `VERDICT: NEEDS_FIXES`. Blocker B1: `--download-report-only` мог пропустить проверку текущих `metadata.taskId`/`metadata.iteration`, если `--out` не похож на стандартный verdict-файл. Blocker B2: даже при включённой проверке текущая metadata-пара искалась по всему отчёту, а не только в `TASK_ASSESSMENT:`. Закрытие: `--download-report-only` теперь до подключения к браузеру требует стандартный `--out` вида `docs/verdicts/<domain>/<task-id>-audit-rNN.md`; `ValidateReportMatchesSubmitIteration` проверяет текущие `metadata.taskId` и `metadata.iteration` только внутри секции `TASK_ASSESSMENT:`. Доменный документ и критерий T-0238 синхронизированы с этим правилом. Добавлены regressions: `AuditSubmitDownloadReportOnlyRequiresStandardVerdictOutputBeforeBrowserLaunch` для `--out report.md`, расширенный `AuditSubmitRejectsDownloadedReportThatOnlyReferencesPreviousIteration` для metadata markers вне `TASK_ASSESSMENT:` и обновлённый `AuditPackageDocumentationDefinesStrictVerdictExtractionRule`. Focused B1/B2 checks прошли: build integration tests - passed, 0 warnings; focused `AuditSubmitDownloadReportOnlyRequiresStandardVerdictOutputBeforeBrowserLaunch`/`AuditSubmitRejectsDownloadedReportThatOnlyReferencesPreviousIteration`/`AuditPackageDocumentationDefinesStrictVerdictExtractionRule`/`AuditSubmitDownloadReportOnlyValidatesDownloadedMarkdown`/`AuditSubmitOrdinaryAssistantExtractionPreservesMarkdownFormatting` - 5/5 passed. Следующий package должен быть r65: включить r64 в `previousVerdictChain` и `repoFileAllowlist`, добавить closure B1/B2 r64 в `blockerClosureList`, пройти полный Medium и отправлять primary через `--reuse-conversation`.

2026-07-03T18:58:00+03:00 - Medium после закрытия r64 B1/B2 прошёл. В ходе широкого focused suite найден старый тест `AuditSubmitDownloadReportOnlyDoesNotRequireZipBeforeBrowserLaunch`, который всё ещё использовал `--out report.md`; fixture обновлена на стандартный `docs/verdicts/release-management/t-0238-audit-r64.md`, чтобы тест проверял отсутствие требования `--zip`, а не уже запрещённый нестандартный output. Итоговые проверки: build `eng/Electron2D.Build` - passed, 0 warnings; `update docs` - updated; `update docs --check` - passed; build integration tests - passed, 0 warnings; failed fixture rerun - passed 1/1; расширенный focused `AuditSubmit`/package message/static request/documentation/parser/secret suite - 167/167 passed; `verify docs` - passed; `verify audit-followups` - passed for 16 actionable findings across 101 saved audit reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Перед r65 package нужно ещё раз синхронизировать generated docs index после этой записи, затем включить r64 в `previousVerdictChain`/`repoFileAllowlist`.

2026-07-03T19:28:00+03:00 - Primary r65 создан, clean-verified на `.temp/audit-clean/T-0238-r65-20260703-190501-lf` и отправлен штатно через `--reuse-conversation`; отчёт сохранён как `docs/verdicts/release-management/t-0238-audit-r65.md` с `VERDICT: NEEDS_FIXES`. Единственный blocker B1: закрытие r64 B1 было неполным, потому `IsAuditVerdictOutputPath` принимал путь с произвольным префиксом перед `docs/verdicts/` и путь без доменного сегмента, если имя файла было похоже на verdict. Закрытие: для `--download-report-only` добавлен отдельный строгий parser `IsCanonicalAuditVerdictOutputPath`, который принимает только относительный путь ровно из четырёх сегментов `docs/verdicts/<domain>/<task-id>-audit-rNN.md`, без абсолютного пути, пустых сегментов, `.` или `..`. Старый широкий `IsAuditVerdictOutputPath` оставлен только для защитного запрета dump в похожие verdict-пути. Доменный документ и критерий T-0238 теперь явно требуют строгий repo-relative `--out` без произвольного префикса, без пропущенного доменного сегмента и без `..`. Regression `AuditSubmitDownloadReportOnlyRequiresStandardVerdictOutputBeforeBrowserLaunch` расширен cases `report.md`, `scratch/docs/verdicts/release-management/t-0238-audit-r65.md`, `docs/verdicts/t-0238-audit-r65.md`, `docs/verdicts/release-management/../t-0238-audit-r65.md`; positive стандартного пути остаётся в `AuditSubmitDownloadReportOnlyDoesNotRequireZipBeforeBrowserLaunch`. Focused B1 checks прошли: build integration tests - passed, 0 warnings; focused canonical output path / placement / docs slice - 7/7 passed. Следующий package должен быть r66: включить r65 в `previousVerdictChain` и `repoFileAllowlist`, добавить closure B1 r65 в `blockerClosureList`, пройти полный Medium и отправлять primary через `--reuse-conversation`.

2026-07-03T19:35:00+03:00 - Medium после закрытия r65 B1 прошёл: build `eng/Electron2D.Build` - passed, 0 warnings; `update docs` - updated; `update docs --check` - passed; расширенный focused `AuditSubmit`/package message/static request/documentation/parser/secret suite - 170/170 passed; `verify docs` - passed; `verify audit-followups` - passed for 16 actionable findings across 102 saved audit reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Перед r66 package нужно ещё раз синхронизировать generated docs index после этой записи, затем включить r65 в `previousVerdictChain`/`repoFileAllowlist`.

2026-07-03T20:11:00+03:00 - Primary r66 создан, clean-verified на `.temp/audit-clean/T-0238-r66-20260703-194234-lf` и отправлен штатно через `--reuse-conversation`; отчёт сохранён как `docs/verdicts/release-management/t-0238-audit-r66.md` с `VERDICT: NEEDS_FIXES`. Единственный blocker B1: строгая проверка `--out` применялась только к `--download-report-only`, а обычный `audit submit` мог записать verdict текущего ZIP в произвольный путь или поверх previous verdict. Закрытие: обычный submit после чтения ZIP, состояния и message вызывает `ValidateAuditSubmitOutputPath`; все режимы записи verdict Markdown принимают только строгий repo-relative `--out`, а имя файла должно совпадать с `taskId`/`iteration` ZIP и режимом primary/control. Добавлены regressions `AuditSubmitRequiresOutputPathToMatchZipBeforeBrowserLaunch` и `AuditSubmitAcceptsCanonicalOutputPathBeforeBrowserLaunch`; focused output-path checks прошли 15/15. Следующий package должен быть r67: включить r66 в `previousVerdictChain` и `repoFileAllowlist`, добавить closure B1 r66 в `blockerClosureList`, пройти полный Medium и отправлять primary через `--reuse-conversation`.

2026-07-03T20:25:00+03:00 - Medium после закрытия r66 B1 прошёл. На первом широком запуске обнаружены два старых тестовых входа, которые проверяли `--new-conversation` и отсутствие Chrome pipe через нестандартный `--out`; они обновлены на canonical primary path, чтобы тестировать нужный preflight, а не уже запрещённый путь сохранения. Итоговые проверки: build `eng/Electron2D.Build` - passed, 0 warnings; `update docs` - updated; `update docs --check` - passed; build integration tests - passed, 0 warnings; focused fixture rerun - 3/3 passed; широкий focused `AuditSubmit`/package message/static request/documentation/parser/secret suite - 178/178 passed; `verify docs` - passed; `verify audit-followups` - passed for 16 actionable findings across 103 saved audit reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Перед r67 package нужно ещё раз синхронизировать generated docs index после этой записи, затем включить r66 в `previousVerdictChain`/`repoFileAllowlist`.

2026-07-03T20:58:00+03:00 - Primary r67 создан, clean-verified на `.temp/audit-clean/T-0238-r67-20260703-203357-lf` и отправлен штатно через `--reuse-conversation`; отчёт сохранён как `docs/verdicts/release-management/t-0238-audit-r67.md` с `VERDICT: NEEDS_FIXES`. Blocker B1: `--download-report-only` принимал control-style filename `docs/verdicts/<domain>/<task-id>-audit-control-rNN.md`, хотя этот режим несовместим с `--control-audit` и не проходит control state gate. Blocker B2: canonical parser принимал неверный регистр фиксированных сегментов `docs/verdicts` и расширение вроде `.MD`. Закрытие: `--download-report-only` теперь принимает только primary filename; `ResolveCanonicalAuditVerdictOutputPathIdentity` требует точные сегменты `docs/verdicts` и точное расширение `.md`. Тесты сначала подтвердили красный результат на новых cases, затем после правки focused output-path/documentation slice прошёл 20/20. Следующий package должен быть r68: включить r67 в `previousVerdictChain` и `repoFileAllowlist`, добавить closure B1/B2 r67 в `blockerClosureList`, пройти полный Medium и отправлять primary через `--reuse-conversation`.

2026-07-03T21:05:00+03:00 - Medium после закрытия r67 B1/B2 прошёл: build `eng/Electron2D.Build` - passed, 0 warnings; `update docs` - updated; `update docs --check` - passed; build integration tests - passed, 0 warnings; широкий focused `AuditSubmit`/package message/static request/documentation/parser/secret suite - 185/185 passed; `verify docs` - passed; `verify audit-followups` - passed for 16 actionable findings across 104 saved audit reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Перед r68 package нужно ещё раз синхронизировать generated docs index после этой записи, затем включить r67 в `previousVerdictChain`/`repoFileAllowlist`.

2026-07-03T21:27:00+03:00 - Созданный локальный `T-0238-audit-r68.zip` не отправлен и считается устаревшим: пользователь уточнил, что ordinary baseline должен сохранять Markdown только через штатную кнопку копирования ответа под новым assistant-сообщением и `navigator.clipboard.readText()`, а не через собственный DOM-to-Markdown renderer. Закрытие локального blocker-а: ordinary polling теперь использует `LastAssistantCopyButtonPointExpression`, кликает найденную copy-кнопку через управляемый Chrome, читает clipboard Markdown, валидирует и сохраняет его; старый `LastAssistantMessageMarkdownExpression` и его тестовая fixture удалены. Добавлены regressions `AuditSubmitOrdinaryAssistantCopyButtonSelectorTargetsCurrentResponse` и `AuditSubmitOrdinaryPollingUsesCopyActionClipboardMarkdown`; focused ordinary-copy/docs/source guard checks прошли 5/5. Следующий внешний package остаётся r68, но его нужно пересобрать заново после полного Medium; старый локальный r68 ZIP не использовать и не отправлять.

2026-07-03T21:45:00+03:00 - Medium после перевода ordinary submit на штатную copy-кнопку прошёл: build `eng/Electron2D.Build` - passed, 0 warnings; `update docs` - updated; `update docs --check` - passed; build integration tests - passed, 0 warnings; широкий focused `AuditSubmit`/package message/static request/documentation/parser/secret suite - 186/186 passed за 4 m 46 s с `--blame-hang-timeout 10m`; `verify docs` - passed; `verify audit-followups` - passed for 16 actionable findings across 104 saved audit reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Перед отправкой r68 нужно повторно синхронизировать generated docs index после этой записи, пересобрать старый локальный r68 ZIP с явным `--force`, затем выполнить clean-verify и отправить primary через `--reuse-conversation`.

2026-07-03T22:12:00+03:00 - Повторный r68 ZIP был пересобран с `--force`, clean-verified на `.temp/audit-clean/T-0238-r68-20260703-215403-lf`, а `audit package message` подтвердил отсутствие `@Глубокое исследование`. Primary submit r68 через `--reuse-conversation` не создал saved report: после сохранения `conversation-url-r68.txt` команда завершилась локальной ошибкой `E2D-BUILD-AUDIT-SUBMIT-CODEX-CHROME-PROTOCOL` / `Timed out after 10000ms waiting for CDP command Runtime.evaluate`. Это не внешний verdict и не новая итерация. Закрытие локального failure class: ordinary polling теперь считает разовый recoverable CDP timeout во время copy/clipboard чтения временным пропуском polling-цикла, но при устойчивом повторе 30 секунд падает с `E2D-BUILD-AUDIT-SUBMIT-ORDINARY-COPY-UNAVAILABLE`. Добавлены regressions `AuditSubmitOrdinaryPollingTreatsSingleCopyTimeoutAsTransient` и `AuditSubmitOrdinaryPollingFailsPersistentCopyTimeoutWithLocalDiagnostic`; focused ordinary-copy timeout slice прошёл 5/5. Следующий внешний submit остаётся r68, но package нужно снова пересобрать после полного Medium, потому код и доменный документ изменились после clean-verified ZIP.

2026-07-03T22:19:00+03:00 - Medium после закрытия локального r68 CDP timeout failure class прошёл: build `eng/Electron2D.Build` - passed, 0 warnings; `update docs` - updated; `update docs --check` - passed; build integration tests - passed, 0 warnings; широкий focused `AuditSubmit`/package message/static request/documentation/parser/secret suite - 188/188 passed за 4 m 57 s с `--blame-hang-timeout 10m`; `verify docs` - passed; `verify audit-followups` - passed for 16 actionable findings across 104 saved audit reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Перед повторным r68 package нужно ещё раз синхронизировать generated docs index после этой записи, обновить package config closure для локального CDP timeout и пересобрать ZIP с `--force`.

2026-07-03T22:45:00+03:00 - Повторный primary submit r68 после transient timeout guard снова не создал saved report: команда завершилась `E2D-BUILD-AUDIT-SUBMIT-ORDINARY-COPY-UNAVAILABLE` после устойчивых `Runtime.evaluate` timeout при чтении `navigator.clipboard.readText()`. Это второй повтор того же локального failure class, поэтому stop-loss запрещает новые submit/package attempts без локальной стабилизации. Закрытие: ordinary copy path теперь перед кликом штатной `copy-turn-action-button` ставит временный hook на аргумент `navigator.clipboard.writeText()`, затем пытается `navigator.clipboard.readText()` с JS-level `Promise.race` timeout; если `readText()` не отдаёт текст, сохраняется Markdown, который сама copy-кнопка передала в `writeText()`. Собственный DOM-to-Markdown renderer не возвращался. Доменный документ и static guards обновлены; focused ordinary-copy/docs/source guard checks прошли 6/6. Следующий внешний submit остаётся r68, но только после полного Medium и новой пересборки package.

2026-07-03T22:52:00+03:00 - Medium после hook-fix для штатной copy-кнопки прошёл: build `eng/Electron2D.Build` - passed, 0 warnings; `update docs` - updated; `update docs --check` - passed; build integration tests - passed, 0 warnings; широкий focused `AuditSubmit`/package message/static request/documentation/parser/secret suite - 188/188 passed за 5 m 8 s с `--blame-hang-timeout 10m`; `verify docs` - passed; `verify audit-followups` - passed for 16 actionable findings across 104 saved audit reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Перед новым r68 package нужно ещё раз синхронизировать generated docs index после этой записи и обновить package config closure про `writeText()` capture.

2026-07-03T23:16:00+03:00 - Следующий r68 submit после `writeText()` hook снова не создал saved report: `audit submit` завершился `E2D-BUILD-AUDIT-SUBMIT-CLIPBOARD-UNAVAILABLE`, потому `navigator.clipboard.readText()` отдал внутренний timeout, а hook не увидел `navigator.clipboard.writeText()` вызов. Stop-loss остаётся активным: новых submit/package попыток без локальной стабилизации не делать. Закрытие локального failure class расширено: после клика штатной copy-кнопки ordinary path теперь сначала читает реальный системный буфер обмена, предварительно записав уникальный sentinel и отвергая неизменившийся sentinel как stale-текст; браузерные `navigator.clipboard.readText()` и `writeText()` capture остаются резервом. Для Windows это реализовано в `SystemClipboardTextAccess` через `OpenClipboard`/`SetClipboardData`/`GetClipboardData`, без PowerShell clipboard commands и без DOM-to-Markdown renderer. Доменный документ и source guards обновлены; focused ordinary-copy/docs/source guard checks прошли 6/6. Следующий внешний submit остаётся r68, но только после полного Medium и новой пересборки package.

2026-07-03T23:23:00+03:00 - Medium после перехода ordinary copy path на системный clipboard с sentinel прошёл: build `eng/Electron2D.Build` - passed, 0 warnings; `update docs` - updated; `update docs --check` - passed; build integration tests - passed, 0 warnings; широкий focused `AuditSubmit`/package message/static request/documentation/parser/secret suite - 188/188 passed за 5 m 9 s с `--blame-hang-timeout 10m`; `verify docs` - passed; `verify audit-followups` - passed for 16 actionable findings across 104 saved audit reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Перед новым r68 package нужно ещё раз синхронизировать generated docs index после этой записи и обновить package config closure про системный clipboard.

2026-07-03T23:46:00+03:00 - Primary r68 сохранён штатной командой как `docs/verdicts/release-management/t-0238-audit-r68.md` с `VERDICT: NEEDS_FIXES`. Blocker B1: system clipboard принимался даже если sentinel не удалось установить, поэтому старый clipboard text мог стать report candidate. Blocker B2: browser `readText()` мог вернуть sentinel и быть принят до captured `writeText()` value; при повторяющемся sentinel это могло вести к долгому ожиданию. Blocker B3: `audit-package.md` и критерий T-0238 противоречили друг другу по режимному clipboard source. Закрытие: system clipboard теперь принимается только при успешном sentinel и тексте, отличном от sentinel; browser/captured clipboard fallback стал sentinel-aware; документация и критерий T-0238 задают единый contract: ordinary path использует controlled copy action proof, arbitrary old clipboard text запрещён, `--deep-research`/`--download-report-only` берут Markdown только из export/download path. Добавлены regressions `AuditSubmitSystemClipboardRequiresInstalledSentinelBeforeAcceptingText` и `AuditSubmitClipboardReadRejectsSentinelBeforeCapturedFallback`; focused B1/B2/B3 slice прошёл 8/8. Следующий package должен быть r69: включить r68 в `previousVerdictChain` и `repoFileAllowlist`, добавить closure B1/B2/B3 r68 в `blockerClosureList`, пройти полный Medium и отправлять primary через `--reuse-conversation`.

2026-07-03T23:53:00+03:00 - Medium после закрытия r68 B1/B2/B3 прошёл: build `eng/Electron2D.Build` - passed, 0 warnings; `update docs` - updated; `update docs --check` - passed; build integration tests - passed, 0 warnings; широкий focused `AuditSubmit`/package message/static request/documentation/parser/secret suite - 190/190 passed за 5 m 7 s с `--blame-hang-timeout 10m`; `verify docs` - passed; `verify audit-followups` - passed for 16 actionable findings across 105 saved audit reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Перед r69 package нужно ещё раз синхронизировать generated docs index после этой записи, создать r69 config с r68 в previous verdict chain и closure B1/B2/B3.

2026-07-04T00:22:00+03:00 - Primary r69 создан, clean-verified на `.temp/audit-clean/T-0238-r69-20260704-000116-lf` и отправлен штатно через `--reuse-conversation`; отчёт сохранён как `docs/verdicts/release-management/t-0238-audit-r69.md` с `VERDICT: NEEDS_FIXES`. Единственный blocker B1: если системный буфер обмена не смог принять sentinel, ordinary submit больше не принимал системный буфер, но всё ещё доверял `navigator.clipboard.readText()` как самостоятельному источнику и мог принять старый browser clipboard text до проверки captured `writeText()` текущей copy-кнопки. Закрытие: browser `readText()` теперь доверяется только когда sentinel был успешно установлен; если sentinel недоступен, `readText()` считается диагностическим путём без права принять отчёт, а валидным доказательством остаётся captured Markdown, который текущая copy-кнопка передала в `navigator.clipboard.writeText()`. Доменный документ синхронизирован с этим правилом. Добавлены regressions `AuditSubmitBrowserClipboardReadRequiresSentinelProof` и `AuditSubmitBrowserClipboardReadRejectsStaleTextWhenSentinelMissing`; focused ordinary copy/clipboard/docs slice прошёл: build integration tests - passed, 0 warnings; 10/10 focused tests passed. Следующий package должен быть r70: включить r69 в `previousVerdictChain` и `repoFileAllowlist`, добавить closure B1 r69 в `blockerClosureList`, пройти полный Medium и отправлять primary через `--reuse-conversation`.

2026-07-04T00:49:00+03:00 - Medium после закрытия r69 B1 прошёл: build `eng\Electron2D.Build` - passed, 0 warnings; `update docs` - updated; `update docs --check` - passed; build integration tests - passed, 0 warnings; широкий focused `AuditSubmit`/package message/static request/documentation/parser/secret suite - 192/192 passed за 5 m 11 s с `--blame-hang-timeout 10m`; `verify docs` - passed; `verify audit-followups` - passed for 16 actionable findings across 106 saved audit reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Перед r70 package нужно ещё раз синхронизировать generated docs index после этой записи, затем создать r70 config с r69 в previous verdict chain и closure B1.

2026-07-04T01:35:00+03:00 - Primary r70 сохранён штатной командой как `docs/verdicts/release-management/t-0238-audit-r70.md` с `VERDICT: ACCEPT`; после этого создан clean-control ZIP в отдельном каталоге `.temp/audit-control`, без `docs/verdicts/**` entries, с пустыми `metadata.previousVerdictChain` и `metadata.blockerClosureList`, clean-verified на `.temp/audit-clean/T-0238-r70-control-20260704-010506-lf` и отправлен через `--control-audit` в новый чат без `--reuse-conversation`. Control report сохранён как `docs/verdicts/release-management/t-0238-audit-control-r70.md` с `VERDICT: NEEDS_FIXES`. Blocker B1: clean-control ZIP не включал сами Markdown verdict-отчёты, но переносил previous verdict context через `data/documentation/electron2d-local-docs-index.json` и `data/documentation/local-docs-index/documentation.ndjson`, потому локальный индекс документации индексировал `docs/verdicts/**`. Blocker B2: `ValidateControlAuditCleanContext` проверял metadata, прямые snapshot paths, `repo-file-hashes.json` и `metadata/repo-file-snapshots.json`, но не сканировал содержимое generated documentation snapshots. Закрытие: `LocalDocumentationVerifier` больше не включает `docs/verdicts/**` в generated local docs index/shards, а `--control-audit` guard до подключения к браузеру сканирует `repo-after/data/documentation/**` и `repo-before/data/documentation/**` на конкретные пути saved verdict reports и блокирует отправку при утечке. Доменный документ синхронизирован; добавлены regressions `UpdateDocsExcludesSavedAuditVerdictsFromGeneratedDocumentationIndex` и `AuditSubmitControlAuditRejectsSavedVerdictReferencesInGeneratedDocumentationBeforeBrowserLaunch`. Focused closure checks прошли: build integration tests - passed, 0 warnings; 3/3 focused tests passed; `update docs` - updated; `update docs --check` - passed; `rg` подтвердил отсутствие `docs/verdicts/` в `data/documentation/electron2d-local-docs-index.json` и `data/documentation/local-docs-index/documentation.ndjson`. Следующий package должен быть r71 primary: включить r70 primary и r70 control reports в `previousVerdictChain` и `repoFileAllowlist`, добавить closure control-r70 B1/B2 в `blockerClosureList`, пройти полный Medium и отправлять primary через `--reuse-conversation` после control NEEDS_FIXES.

2026-07-04T01:49:00+03:00 - Medium после закрытия control-r70 B1/B2 прошёл: build `eng\Electron2D.Build` - passed, 0 warnings; `update docs` - updated; `update docs --check` - passed; `rg` подтвердил отсутствие `docs/verdicts/` в generated docs index/shard; build integration tests - passed, 0 warnings; широкий focused `AuditSubmit`/package message/static request/documentation/parser/secret suite плюс `UpdateDocsExcludesSavedAuditVerdictsFromGeneratedDocumentationIndex` - 194/194 passed за 4 m 55 s с `--blame-hang-timeout 10m`; `verify docs` - passed; `verify audit-followups` - passed for 16 actionable findings across 108 saved audit reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Перед r71 package нужно ещё раз синхронизировать generated docs index после этой записи, создать r71 config с r70 primary/control reports и closure B1/B2 control-r70.

2026-07-04T02:07:00+03:00 - Primary r71 создан, clean-verified на `.temp/audit-clean/T-0238-r71-20260704-014802-lf` и отправлен штатно через `--reuse-conversation`; отчёт сохранён как `docs/verdicts/release-management/t-0238-audit-r71.md` с `VERDICT: NEEDS_FIXES`. Blocker B1: новый `--control-audit` guard сканировал `repo-before/data/documentation/**` и мог сам заблокировать будущий clean-control ZIP, потому старый baseline generated docs index ещё содержал исторические `docs/verdicts/**` строки. Blocker B2: guard был слишком узким: проверял только `data/documentation/**` и только стандартные имена `*-audit-rNN.md`, но не активные текстовые артефакты ZIP и нестандартные Markdown verdict paths. Закрытие: guard заменён на active text surface check: он сканирует `AUDIT-MANIFEST.md`, `AUDIT-REQUEST.md`, `repo-after/**`, `evidence/**`, `metadata/**`, `repo-file-hashes.json`, `SHA256SUMS.txt` и patch на конкретные Markdown-пути под `docs/verdicts/`, включая нестандартные имена; исторические `repo-before/**` snapshots не считаются текущим контекстом, но прямые `repo-before/docs/verdicts/**` entries и metadata/snapshot-index ссылки остаются запрещены. Доменный документ синхронизирован. Regressions сначала упали 5/5 на старом поведении, затем прошли: `AuditSubmitControlAuditAllowsHistoricGeneratedDocumentationBeforeSnapshotWhenAfterIsClean`, `AuditSubmitControlAuditRejectsSavedVerdictReferencesInTextArtifactsBeforeBrowserLaunch`, `AuditSubmitControlAuditRejectsNonstandardSavedVerdictPathReferencesBeforeBrowserLaunch`; вместе со старым generated-docs guard и documentation test focused slice прошёл 7/7. Следующий package должен быть r72: включить r71 в `previousVerdictChain` и `repoFileAllowlist`, добавить closure B1/B2 r71 в `blockerClosureList`, пройти полный Medium и отправлять primary через `--reuse-conversation`. Для будущего clean-control ZIP после primary `ACCEPT` активные файлы, содержащие историю прошлых verdict reports, например `TASKS.md`, нельзя включать в контрольный ZIP как текущий текстовый контекст; контрольный ZIP должен сохранять accepted code/docs/tests/tooling scope, но удалять previous verdict context.

2026-07-04T02:15:00+03:00 - Medium после закрытия r71 B1/B2 прошёл: build `eng\Electron2D.Build` - passed, 0 warnings; `update docs` - updated; `update docs --check` - passed; `rg` подтвердил отсутствие `docs/verdicts/` в generated docs index/shard; build integration tests - passed, 0 warnings; широкий focused `AuditSubmit`/package message/static request/documentation/parser/secret suite плюс `UpdateDocsExcludesSavedAuditVerdictsFromGeneratedDocumentationIndex` - 199/199 passed за 5 m 1 s с `--blame-hang-timeout 10m`; `verify docs` - passed; `verify audit-followups` - passed for 16 actionable findings across 109 saved audit reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Перед r72 package нужно ещё раз синхронизировать generated docs index после этой записи, создать r72 config с r71 report и closure B1/B2 r71, затем отправлять primary через `--reuse-conversation`.

2026-07-04T02:41:00+03:00 - Primary r72 создан, clean-verified на `.temp/audit-clean/T-0238-r72-20260704-022458-lf` и отправлен штатно через `--reuse-conversation`; отчёт сохранён как `docs/verdicts/release-management/t-0238-audit-r72.md` с `VERDICT: NEEDS_FIXES`. Blocker B1: broad active text scan считал любые `repo-after/**` text snapshots контекстом и блокировал `RepositoryBuildToolTests.cs`, потому тестовые fixtures содержат синтетические `docs/verdicts/**.md` пути. Blocker B2: patch сканировался целиком и блокировал удалённые исторические строки generated docs, хотя `repo-after/data/documentation/**` уже очищен. Blocker B3: документ не объяснял различие между контекстными артефактами, test fixtures и историческими строками patch. Закрытие: `--control-audit` content guard теперь сканирует только context-bearing text artifacts: `AUDIT-MANIFEST.md`, `AUDIT-REQUEST.md`, `repo-after/data/documentation/**`, `evidence/**`, `metadata/**`, `repo-file-hashes.json`, `SHA256SUMS.txt` и только добавленные строки patch; обычные source/test snapshots под `repo-after/**`, исторические `repo-before/**` snapshots и удалённые/context строки patch не считаются текущим previous verdict context. Прямые `repo-before/docs/verdicts/**` entries, repo-file/snapshot metadata links, generated docs leaks, evidence/request/manifest leaks и patch additions остаются blocker-ами. Доменный документ синхронизирован. Regressions сначала упали на B1/B2, затем прошли: `AuditSubmitControlAuditAllowsSyntheticVerdictPathsInRepoOwnedTestFixturesBeforeBrowserLaunch`, `AuditSubmitControlAuditAllowsPatchRemovedSavedVerdictReferencesBeforeBrowserLaunch`, `AuditSubmitControlAuditRejectsPatchAddedSavedVerdictReferencesBeforeBrowserLaunch`; combined guard/documentation slice прошёл 10/10. Следующий package должен быть r73: включить r72 в `previousVerdictChain` и `repoFileAllowlist`, добавить closure B1/B2/B3 r72 в `blockerClosureList`, пройти полный Medium и отправлять primary через `--reuse-conversation`.

2026-07-04T02:48:00+03:00 - Medium после закрытия r72 B1/B2/B3 прошёл: build `eng\Electron2D.Build` - passed, 0 warnings; `update docs` - updated; `update docs --check` - passed; `rg` подтвердил отсутствие `docs/verdicts/` в generated docs index/shard; build integration tests - passed, 0 warnings; широкий focused `AuditSubmit`/package message/static request/documentation/parser/secret suite плюс `UpdateDocsExcludesSavedAuditVerdictsFromGeneratedDocumentationIndex` - 202/202 passed за 5 m 12 s с `--blame-hang-timeout 10m`; `verify docs` - passed; `verify audit-followups` - passed for 16 actionable findings across 110 saved audit reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Перед r73 package нужно ещё раз синхронизировать generated docs index после этой записи, создать r73 config с r72 report и closure B1/B2/B3 r72, затем отправлять primary через `--reuse-conversation`.

2026-07-04T03:15:00+03:00 - Primary r73 создан, clean-verified на `.temp/audit-clean/T-0238-r73-20260704-025703-lf` и отправлен штатно через `--reuse-conversation`; отчёт сохранён как `docs/verdicts/release-management/t-0238-audit-r73.md` с `VERDICT: NEEDS_FIXES`. Blocker B1: patch addition scan всё ещё блокировал synthetic `docs/verdicts/**.md` fixture paths, когда они добавлены в test source, хотя `repo-after/tests/**` snapshots уже разрешены. Blocker B2: модель clean-control для `TASKS.md` и process docs оставалась неявной: прямой content scan их не проверял, patch additions блокировал, а сам ledger несёт историю прошлых verdict reports. Закрытие: patch scan стал path-aware: добавленные строки проверяются только для context-bearing repo paths, сейчас `data/documentation/**` и `docs/release-management/AUDIT-REQUEST.md`; test/source patch additions с synthetic fixtures не считаются previous verdict context. `--control-audit` теперь отдельно отклоняет `TASKS.md` и `data/dev-diary/**` как mutable process-history files в clean-control ZIP. Доменный документ синхронизирован: clean-control переносит принятую область через metadata, domain docs, code/tests и generated evidence без ledger-файлов. Regressions сначала упали на B1/B2, затем прошли: усиленный `AuditSubmitControlAuditAllowsSyntheticVerdictPathsInRepoOwnedTestFixturesBeforeBrowserLaunch`, `AuditSubmitControlAuditRejectsTaskLedgerBeforeBrowserLaunch`; combined guard/documentation slice прошёл 11/11. Следующий package должен быть r74: включить r73 в `previousVerdictChain` и `repoFileAllowlist`, добавить closure B1/B2 r73 в `blockerClosureList`, пройти полный Medium и отправлять primary через `--reuse-conversation`.

2026-07-04T03:22:00+03:00 - Medium после закрытия r73 B1/B2 прошёл: build `eng\Electron2D.Build` - passed, 0 warnings; `update docs` - updated; `update docs --check` - passed; `rg` подтвердил отсутствие `docs/verdicts/` в generated docs index/shard; build integration tests - passed, 0 warnings; широкий focused `AuditSubmit`/package message/static request/documentation/parser/secret suite плюс `UpdateDocsExcludesSavedAuditVerdictsFromGeneratedDocumentationIndex` - 203/203 passed за 5 m 9 s с `--blame-hang-timeout 10m`; `verify docs` - passed; `verify audit-followups` - passed for 16 actionable findings across 111 saved audit reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Перед r74 package нужно ещё раз синхронизировать generated docs index после этой записи, создать r74 config с r73 report и closure B1/B2 r73, затем отправлять primary через `--reuse-conversation`.

2026-07-04T04:05:00+03:00 - Primary r74 создан, clean-verified на отдельной чистой копии и отправлен штатно через `--reuse-conversation`; отчёт сохранён как `docs/verdicts/release-management/t-0238-audit-r74.md` с `VERDICT: ACCEPT`. После primary ACCEPT собран clean-control ZIP той же принятой области с пустыми `metadata.previousVerdictChain` и `metadata.blockerClosureList`, без `docs/verdicts/**`, без `TASKS.md` и без `data/dev-diary/**`; control submit выполнен через `--control-audit` в новом чате проекта. Control report сохранён как `docs/verdicts/release-management/t-0238-audit-control-r74.md` с `VERDICT: NEEDS_FIXES`. Единственный blocker B1: production callers выбранных Deep Research frame/target surfaces разрешали page-level Markdown fallback в helper-е и документации, но фактически передавали `() => Task.FromResult(false)` как page-level Markdown click delegate, поэтому поддержанный сценарий "export button inside frame/target, Markdown menu overlay in main page DOM" не работал. `RISKS_AND_NOTES` в control r74 пустые.

2026-07-04T04:23:00+03:00 - Закрыт control-r74 B1: добавлен production-path regression `AuditSubmitSelectedDeepResearchSurfaceUsesPageMarkdownFallbackInProductionPath`, который сначала падал на отсутствии внутреннего driver seam-а, затем прошёл для `DeepResearchFrame`, `DeepResearchTarget` и `DeepResearchTargetFrame`. `AuditSubmitCodexChromeCommand` теперь проводит все выбранные Deep Research surfaces через `DownloadReportCandidatesFromSelectedDeepResearchSurfaceAsync` и `IAuditSubmitDeepResearchMarkdownExportDriver`; page-level Markdown click выполняется через основной DOM страницы после выбранного export click, а selected-surface callers больше не передают `Task.FromResult(false)`. Старый source guard уточнён: он проверяет, что выбранные frame/target callers идут через production seam и не отключают page fallback. Проверки: RED `AuditSubmitSelectedDeepResearchSurfaceUsesPageMarkdownFallbackInProductionPath` - failed 3/3 на текущем старом поведении; после правки `dotnet build tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj` - passed, 0 warnings; focused Deep Research export slice - passed 6/6. Следующий package должен быть r75 primary: включить `docs/verdicts/release-management/t-0238-audit-r74.md` и `docs/verdicts/release-management/t-0238-audit-control-r74.md` в `previousVerdictChain`/`repoFileAllowlist`, добавить closure control-r74 B1 в `blockerClosureList`, пройти полный Medium и отправлять primary через `--reuse-conversation`.

2026-07-04T04:41:00+03:00 - Medium после закрытия control-r74 B1 прошёл: build `eng\Electron2D.Build` - passed, 0 warnings; `update docs` - updated; `update docs --check` - passed; `rg` подтвердил отсутствие `docs/verdicts/` в generated docs index/shard; build integration tests - passed, 0 warnings; r74-style focused `AuditSubmit`/package message/static request/documentation/parser/secret suite плюс `UpdateDocsExcludesSavedAuditVerdictsFromGeneratedDocumentationIndex` - 206/206 passed за 4 m 48 s; `verify docs` - passed; `verify audit-followups` - passed for 16 actionable findings across 113 saved audit reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Ошибочный слишком широкий wildcard-фильтр `FullyQualifiedName~AuditPackage` был остановлен как локальный hang на package fixture и не использовался как gate. Перед r75 package нужно ещё раз синхронизировать generated docs index после этой записи, создать r75 config с r74 primary/control reports и closure B1 control-r74, затем отправлять primary через `--reuse-conversation`.

2026-07-04T05:12:53+03:00 - Primary r75 package создан, clean-verified на `.temp/audit-clean/T-0238-r75-20260704-044927-lf` и отправлен через `--reuse-conversation`, но saved report не создан: ordinary submit завершился локальной диагностикой `E2D-BUILD-AUDIT-SUBMIT-CLIPBOARD-UNAVAILABLE`, потому штатная copy action завершилась, `navigator.clipboard.readText()` ушёл в timeout, а прежний capture не увидел вызов `navigator.clipboard.writeText()`. Это не внешний verdict и не входит в `previousVerdictChain`. Закрытие локального failure class: ordinary copy capture теперь перехватывает не только `navigator.clipboard.writeText()`, но и `navigator.clipboard.write(...)` с `text/plain` Markdown payload той же copy-кнопки; DOM-to-Markdown renderer не возвращался. Доменный документ, критерий T-0238 и static guards синхронизированы; focused clipboard/docs/source guard slice прошёл 6/6 после RED `AuditSubmitClipboardCaptureInterceptsClipboardWriteMarkdown`, который сначала падал на отсутствии capture для `navigator.clipboard.write(...)`. Следующий package должен быть r76: не добавлять r75 в saved verdict chain, оставить r74 primary/control reports и closure control-r74 B1, добавить closure локального r75 extraction failure в `scopeSummary`/`blockerClosureList`, пройти полный Medium и отправлять primary через `--reuse-conversation`.

2026-07-04T05:19:39+03:00 - Medium после закрытия локального r75 clipboard `write(...)` extraction failure прошёл: build `eng\Electron2D.Build` - passed, 0 warnings; `update docs` - updated; `update docs --check` - passed; `rg` подтвердил отсутствие `docs/verdicts/` в generated docs index/shard; build integration tests - passed, 0 warnings; r75/r76 focused `AuditSubmit`/package message/static request/documentation/parser/secret suite плюс `UpdateDocsExcludesSavedAuditVerdictsFromGeneratedDocumentationIndex` - 207/207 passed за 4 m 44 s; `verify docs` - passed; `verify audit-followups` - passed for 16 actionable findings across 113 saved audit reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Перед r76 package нужно ещё раз синхронизировать generated docs index после этой записи, создать r76 config без r75 saved report в `previousVerdictChain`, затем отправлять primary через `--reuse-conversation`.

2026-07-04T05:49:28+03:00 - Primary r76 package создан, clean-verified на `.temp/audit-clean/T-0238-r76-20260704-052745-lf` и отправлен через `--reuse-conversation`, но saved report не создан: ordinary submit завершился локальной диагностикой `E2D-BUILD-AUDIT-SUBMIT-CLIPBOARD-UNAVAILABLE`, потому системный clipboard не дал новый Markdown, browser `readText()` получил `Read permission denied`, а capture не увидел `navigator.clipboard.writeText()`/`write(...)`. Это второй подряд локальный failure class после r75, поэтому stop-loss запрещает новый package/submit без локального driver/harness-теста и зелёного Medium. Закрытие: ordinary copy capture теперь дополнительно слушает browser `copy` event той же штатной copy-кнопки и берёт Markdown из `event.clipboardData`, выделенного текста или значения активного input/textarea, что покрывает `document.execCommand('copy')`/selection fallback без возврата DOM-to-Markdown renderer-а. RED `AuditSubmitClipboardCaptureInterceptsCopyEventSelectedMarkdown` сначала падал на отсутствии copy-event listener-а, затем focused clipboard/docs/source guard slice прошёл 7/7. Следующий package должен быть r77: не добавлять r75/r76 в saved verdict chain, оставить r74 primary/control reports и closure control-r74 B1, добавить closure локального r75/r76 extraction failure в `scopeSummary`/`blockerClosureList`, пройти полный Medium и отправлять primary через `--reuse-conversation`.

2026-07-04T05:55:56+03:00 - Medium после stop-loss fix для r75/r76 ordinary copy extraction failure прошёл: build `eng\Electron2D.Build` - passed, 0 warnings; `update docs` - updated; `update docs --check` - passed; `rg` подтвердил отсутствие `docs/verdicts/` в generated docs index/shard; build integration tests - passed, 0 warnings; r76/r77 focused `AuditSubmit`/package message/static request/documentation/parser/secret suite плюс `UpdateDocsExcludesSavedAuditVerdictsFromGeneratedDocumentationIndex` - 208/208 passed за 4 m 41 s; `verify docs` - passed; `verify audit-followups` - passed for 16 actionable findings across 113 saved audit reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Перед r77 package нужно ещё раз синхронизировать generated docs index после этой записи, создать r77 config без r75/r76 saved reports в `previousVerdictChain`, затем отправлять primary через `--reuse-conversation`.

2026-07-04T06:21:41+03:00 - Primary r77 package создан, clean-verified на `.temp/audit-clean/T-0238-r77-20260704-060258-lf` и отправлен через `--reuse-conversation`, но saved report не создан: ordinary submit снова завершился `E2D-BUILD-AUDIT-SUBMIT-CLIPBOARD-UNAVAILABLE` с `navigator.clipboard.readText(): Read permission denied` и отсутствующим captured `writeText()`/`write(...)`/`copy` event payload. Повторять package/submit запрещено до нового локального закрытия. Диагностика без чтения verdict-а показала, что conversation содержит много видимых `copy-turn-action-button`, а последняя copy-кнопка находится в видимой области и не disabled; вероятная причина - ChatGPT выбирает denied/no-op clipboard path до того, как command выдаёт странице clipboard permission. Закрытие: `GrantClipboardReadPermissionBestEffortAsync` теперь выполняется до установки capture hook и до `ClickAtAsync` по copy-кнопке; прежний grant перед browser `readText()` оставлен как резерв. RED source guard сначала подтвердил, что grant стоял после click, затем focused clipboard/docs/source guard slice прошёл 7/7. Следующий package должен быть r78: не добавлять r75/r76/r77 в saved verdict chain, оставить r74 primary/control reports и closure control-r74 B1, добавить closure локального r75-r77 extraction failure в `scopeSummary`/`blockerClosureList`, пройти полный Medium и отправлять primary через `--reuse-conversation`.

2026-07-04T06:28:22+03:00 - Medium после pre-click clipboard permission fix прошёл: build `eng\Electron2D.Build` - passed, 0 warnings; `update docs` - updated; `update docs --check` - passed; `rg` подтвердил отсутствие `docs/verdicts/` в generated docs index/shard; build integration tests - passed, 0 warnings; r77/r78 focused `AuditSubmit`/package message/static request/documentation/parser/secret suite плюс `UpdateDocsExcludesSavedAuditVerdictsFromGeneratedDocumentationIndex` - 208/208 passed за 4 m 40 s; `verify docs` - passed; `verify audit-followups` - passed for 16 actionable findings across 113 saved audit reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Перед r78 package нужно ещё раз синхронизировать generated docs index после этой записи, создать r78 config без r75/r76/r77 saved reports в `previousVerdictChain`, затем отправлять primary через `--reuse-conversation`.

2026-07-04T06:50:00+03:00 - Primary r78 package был создан, clean-verified на `.temp/audit-clean/T-0238-r78-20260704-063545-lf` и отправлен через `--reuse-conversation`, но saved report снова не создан: ordinary submit завершился `E2D-BUILD-AUDIT-SUBMIT-CLIPBOARD-UNAVAILABLE`, потому штатная copy action не дала Markdown через системный clipboard, page-level `navigator.clipboard.readText()` получил `Read permission denied`, а локальный capture не увидел `writeText()`/`write(...)`/`copy` event payload. Это повтор того же локального failure class после r75-r77, поэтому stop-loss снова запрещает r79 package/submit до нового локального driver/harness-теста и зелёного Medium. План закрытия: ordinary path должен после клика по штатной кнопке читать Markdown через backend clipboard API расширения `Codex Chrome Extension` командой `tab_clipboard_read_text`; системный буфер, page `navigator.clipboard.readText()` и capture-hook остаются fallback-диагностикой, а DOM-to-Markdown renderer не возвращается.

2026-07-04T07:00:00+03:00 - Закрыт локальный r78 ordinary copy extraction failure: добавлен протокольный regression `AuditSubmitCodexChromeClientReadsBackendClipboardMarkdown`, который поднимает локальный named pipe, вызывает `AuditSubmitCodexChromeClient.ReadBrowserSessionClipboardTextAsync`, проверяет JSON-RPC `executeUnhandledCommand` с `type: tab_clipboard_read_text` и принимает Markdown из ответа `{ text }`; тест сначала падал на отсутствии метода. Дополнительно добавлен guard `AuditSubmitOrdinaryCopyReadsBackendClipboardBeforeFallbacks`, который фиксирует порядок: после штатного copy-click ordinary path читает backend clipboard до системного/page fallback. Реализация теперь best-effort записывает sentinel в backend clipboard через `tab_clipboard_write_text`, кликает штатную кнопку копирования ответа и сначала читает Markdown через `tab_clipboard_read_text`; системный clipboard, page `navigator.clipboard.readText()` и capture-hook остаются fallback-диагностикой. DOM-to-Markdown renderer не возвращался. Focused ordinary copy/clipboard/docs/source guard slice прошёл 11/11. Перед r79 package обязателен полный Medium.

2026-07-04T07:05:00+03:00 - Medium после backend clipboard fix для r78 ordinary copy extraction failure прошёл: build `eng\Electron2D.Build` - passed, 0 warnings; `update docs` - updated; `update docs --check` - passed; `rg` не нашёл `docs/verdicts/` в generated docs index/shard; build integration tests - passed, 0 warnings; широкий focused `AuditSubmit`/package message/documentation/generated-docs suite - 181/181 passed за 3 m 24 s; `verify docs` - passed; `verify audit-followups` - passed for 16 actionable findings across 113 saved audit reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Stop-loss условие выполнено: есть локальный RED/GREEN protocol regression и зелёный Medium. Следующий package должен быть r79: не добавлять r75/r76/r77/r78 в saved verdict chain, оставить r74 primary/control reports и closure control-r74 B1, добавить closure локального r75-r78 extraction failure в `scopeSummary`/`blockerClosureList`, затем отправлять primary через `--reuse-conversation`.

2026-07-04T07:38:00+03:00 - Primary r79 package был создан, clean-verified на `.temp/audit-clean/T-0238-r79-20260704-071435-lf`, message проверен без `@Глубокое исследование` и отправлен через `--reuse-conversation`, но saved report не создан: ordinary submit снова завершился локальной диагностикой `E2D-BUILD-AUDIT-SUBMIT-CLIPBOARD-UNAVAILABLE`, потому штатная copy action не дала Markdown через системный clipboard, page-level `navigator.clipboard.readText()` получил `Read permission denied`, а capture не увидел `writeText()`/`write(...)` payload. Это не внешний verdict и не входит в `previousVerdictChain`. Последующая локальная проверка direct pipe показала, что установленная `Codex Chrome Extension` surface отвечает `Chrome does not support command "tab_clipboard_read_text"` / `"tab_clipboard_write_text"`, поэтому backend clipboard path из записи 07:00 был ложным локальным контрактом и удалён из реализации. Закрытие: ordinary path теперь ставит pre-load захват `navigator.clipboard.writeText()` / `navigator.clipboard.write(...)` через `Page.addScriptToEvaluateOnNewDocument` до навигации ChatGPT, очищает pre-load и late capture state перед нажатием штатной copy-кнопки и читает captured Markdown из pre-load или позднего hook-а; системный clipboard и page-level `navigator.clipboard.readText()` остаются fallback-диагностикой, DOM-to-Markdown renderer не возвращался. Focused regression slice прошёл 5/5: `AuditSubmitClipboardPreloadCaptureInterceptsEarlyBoundWriteTextMarkdown`, `AuditSubmitOrdinaryCopyResetsPreloadCaptureBeforeClick`, `AuditSubmitClipboardCaptureInterceptsClipboardWriteMarkdown`, `AuditSubmitClipboardCaptureInterceptsCopyEventSelectedMarkdown`, `AuditSubmitCodexChromeClicksDeepResearchTool`. Перед следующим внешним package обязателен новый Medium. Следующий package должен быть r80: не добавлять r75/r76/r77/r78/r79 в saved verdict chain, оставить r74 primary/control reports и closure control-r74 B1, добавить closure локального r75-r79 extraction failure в `scopeSummary`/`blockerClosureList`, затем отправлять primary через `--reuse-conversation`.

2026-07-04T07:51:00+03:00 - Medium после preload capture fix для r79 ordinary copy extraction failure прошёл: build `eng\Electron2D.Build` - passed, 0 warnings; `update docs` - updated; `update docs --check` - passed; `rg` не нашёл `docs/verdicts/` в generated docs index/shard; build integration tests - passed, 0 warnings; широкий focused `AuditSubmit`/package message/documentation/generated-docs suite - 181/181 passed за 3 m 19 s; `verify docs` - passed; `verify audit-followups` - passed for 16 actionable findings across 113 saved audit reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Stop-loss условие выполнено для повторившегося r75-r79 failure class: локальная direct pipe диагностика опровергла backend clipboard path, новый preload-capture regression зелёный, Medium зелёный. Перед r80 package нужно ещё раз синхронизировать generated docs после этой записи; r80 должен исключить r75-r79 из saved verdict chain и отправляться через `--reuse-conversation`.

2026-07-04T08:09:00+03:00 - Primary r80 package был создан, clean-verified на `.temp/audit-clean/T-0238-r80-20260704-075623-lf`, message проверен без `@Глубокое исследование` и отправлен через `--reuse-conversation`, но saved report не создан: ordinary submit снова завершился локальной диагностикой `E2D-BUILD-AUDIT-SUBMIT-CLIPBOARD-UNAVAILABLE`. Системный clipboard не дал новый Markdown, page-level `navigator.clipboard.readText()` получил `Read permission denied`, а pre-load/late capture не увидел `navigator.clipboard.writeText()`/`write(...)`. Это не внешний verdict и не входит в `previousVerdictChain`. Новый локальный RED показал недостающий browser copy-event путь: штатная copy-кнопка может писать Markdown через `event.clipboardData.setData('text/plain', markdown)`, при этом `getData()`, selection и active input не дают текста. Закрытие: ordinary copy capture теперь перехватывает `DataTransfer.prototype.setData` в pre-load и позднем hook-е; DOM-to-Markdown renderer не возвращался. RED `AuditSubmitClipboardCaptureInterceptsCopyEventSetDataMarkdown` сначала падал на отсутствии `setData` capture, затем прошёл; focused ordinary copy regression slice прошёл 6/6. Перед следующим внешним package обязателен новый Medium. Следующий package должен быть r81: не добавлять r75/r76/r77/r78/r79/r80 в saved verdict chain, оставить r74 primary/control reports и closure control-r74 B1, добавить closure локального r75-r80 extraction failure в `scopeSummary`/`blockerClosureList`, затем отправлять primary через `--reuse-conversation`.

2026-07-04T08:15:00+03:00 - Medium после `DataTransfer.setData` fix для r80 ordinary copy extraction failure прошёл: build `eng\Electron2D.Build` - passed, 0 warnings; `update docs` - updated; `update docs --check` - passed; `rg` не нашёл `docs/verdicts/` в generated docs index/shard; build integration tests - passed, 0 warnings; широкий focused `AuditSubmit`/package message/documentation/generated-docs suite - 182/182 passed за 3 m 23 s; `verify docs` - passed; `verify audit-followups` - passed for 16 actionable findings across 113 saved audit reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Stop-loss условие выполнено: есть локальный RED/GREEN regression на `event.clipboardData.setData('text/plain', markdown)` и зелёный Medium. Перед r81 package нужно ещё раз синхронизировать generated docs после этой записи; r81 должен исключить r75-r80 из saved verdict chain и отправляться через `--reuse-conversation`.

2026-07-04T08:34:00+03:00 - Primary r81 package был создан, clean-verified на `.temp/audit-clean/T-0238-r81-20260704-082145-lf`, message проверен без `@Глубокое исследование` и отправлен через `--reuse-conversation`, но saved report не создан: ordinary submit снова завершился локальной диагностикой `E2D-BUILD-AUDIT-SUBMIT-CLIPBOARD-UNAVAILABLE`. Кнопка copy была нажата координатным кликом, но системный clipboard не дал Markdown, page-level `navigator.clipboard.readText()` получил `Read permission denied`, а captured copy action payload не был найден. Это не внешний verdict и не входит в `previousVerdictChain`. Новый локальный RED закрыл следующий разрыв: координатный клик сам по себе не доказывал, что React handler штатной copy-кнопки отработал. Закрытие: ordinary path после координатного click и отсутствия нового системного Markdown вызывает DOM `click()` у той же copy-кнопки текущего assistant-сообщения, затем повторно проверяет системный clipboard и captured payload; это остаётся штатным copy action ChatGPT, DOM-to-Markdown renderer не возвращался. RED `AuditSubmitOrdinaryAssistantCopyButtonDomClickTargetsCurrentResponse` сначала падал на отсутствии `LastAssistantCopyButtonClickExpression`, затем прошёл; focused ordinary copy regression slice прошёл 8/8. Перед следующим внешним package обязателен новый Medium. Следующий package должен быть r82: не добавлять r75/r76/r77/r78/r79/r80/r81 в saved verdict chain, оставить r74 primary/control reports и closure control-r74 B1, добавить closure локального r75-r81 extraction failure в `scopeSummary`/`blockerClosureList`, затем отправлять primary через `--reuse-conversation`.

2026-07-04T08:40:00+03:00 - Medium после DOM-click fallback для r81 ordinary copy extraction failure прошёл: build `eng\Electron2D.Build` - passed, 0 warnings; `update docs` - updated; `update docs --check` - passed; `rg` не нашёл `docs/verdicts/` в generated docs index/shard; build integration tests - passed, 0 warnings; широкий focused `AuditSubmit`/package message/documentation/generated-docs suite - 183/183 passed за 3 m 21 s; `verify docs` - passed; `verify audit-followups` - passed for 16 actionable findings across 113 saved audit reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Stop-loss условие выполнено: есть локальный RED/GREEN regression на DOM `click()` той же copy-кнопки и зелёный Medium. Перед r82 package нужно ещё раз синхронизировать generated docs после этой записи; r82 должен исключить r75-r81 из saved verdict chain и отправляться через `--reuse-conversation`.

2026-07-04T09:01:00+03:00 - Primary audit r82 сохранён штатной командой как `docs/verdicts/release-management/t-0238-audit-r82.md` с `VERDICT: NEEDS_FIXES`. Blocker B1: browser copy-event fallback принимал global selection и полный `activeElement.value` / `activeElement.textContent` без доказательства, что это Markdown текущего assistant response или временный payload текущей copy-кнопки. Закрытие: ordinary copy-event fallback стал source-aware: selected text принимается только если anchor/focus находятся внутри текущего assistant-сообщения, временный active input/textarea принимается только как непустой selected range и не как active element до клика; полный `active.value` и `active.textContent` больше не являются источниками verdict-а. Добавлены RED/GREEN regressions `AuditSubmitClipboardCaptureRejectsStaleGlobalSelectionMarkdown`, `AuditSubmitClipboardCaptureAcceptsCurrentAssistantSelectionMarkdown`, `AuditSubmitClipboardCaptureRejectsFullActiveElementValueMarkdown`, `AuditSubmitClipboardCaptureAcceptsTemporaryActiveSelectionMarkdown`; focused ordinary copy slice прошёл 12/12. Следующий package должен быть r83: включить r82 в `previousVerdictChain` и `repoFileAllowlist`, добавить closure B1 r82 в `blockerClosureList`, пройти полный Medium и отправлять primary через `--reuse-conversation`.

2026-07-04T09:06:00+03:00 - Medium после r82 B1 source-aware copy-event fix прошёл: build `eng\Electron2D.Build` - passed, 0 warnings; `update docs` - updated; `update docs --check` - passed; `rg` не нашёл `docs/verdicts/` в generated docs index/shard; build integration tests - passed, 0 warnings; широкий focused `AuditSubmit`/package message/documentation/generated-docs suite - 187/187 passed за 3 m 25 s; `verify docs` - passed; `verify audit-followups` - passed for 16 actionable findings across 114 saved audit reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Следующий package должен быть r83: включить r82 в saved verdict chain, добавить closure r82 B1 и отправлять primary через `--reuse-conversation`.

2026-07-04T09:56:00+03:00 - Primary audit r83 сохранён штатной командой как `docs/verdicts/release-management/t-0238-audit-r83.md` с `VERDICT: ACCEPT`, затем clean-control ZIP был собран в `.temp/audit-control/T-0238-audit-r83.zip`, проверен на `.temp/audit-clean/T-0238-r83-control-20260704-093129-lf` и отправлен новым control chat через `--control-audit`; control report сохранён как `docs/verdicts/release-management/t-0238-audit-control-r83.md` с `VERDICT: NEEDS_FIXES`. Blocker control-r83 B1: после завершения ответа устойчивое отсутствие текущей assistant copy-кнопки трактовалось как ожидание и вело к общему timeout, а не к bounded ordinary copy diagnostic. Закрытие: `WaitForOrdinaryChatReportAsync` теперь отслеживает `null` от `CopyLatestAssistantMessageMarkdownAsync` после `IsGeneratingAsync=false`; если copy-кнопка текущего ответа остаётся недоступной не меньше `OrdinaryCopyFailureStableAge`, команда падает с `E2D-BUILD-AUDIT-SUBMIT-ORDINARY-COPY-UNAVAILABLE`, а не ждёт общий timeout. Добавлен RED/GREEN regression `AuditSubmitOrdinaryPollingFailsPersistentMissingCopyButtonWithLocalDiagnostic`; focused polling/copy slice прошёл 11/11. Следующий package должен быть r84: включить primary r83 и control-r83 в `previousVerdictChain` и `repoFileAllowlist`, добавить closure control-r83 B1 в `blockerClosureList`, пройти полный Medium и отправлять primary через `--reuse-conversation`.

2026-07-04T10:02:00+03:00 - Medium после control-r83 B1 missing-copy-button fix прошёл: build `eng\Electron2D.Build` - passed, 0 warnings; `update docs` - updated; `update docs --check` - passed; `rg` не нашёл `docs/verdicts/` в generated docs index/shard; build integration tests - passed, 0 warnings; широкий focused `AuditSubmit`/package message/documentation/generated-docs suite - 188/188 passed за 3 m 54 s; `verify docs` - passed; `verify audit-followups` - passed for 16 actionable findings across 116 saved audit reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Следующий package должен быть r84: включить primary r83 и control-r83 в saved verdict chain, добавить closure control-r83 B1 и отправлять primary через `--reuse-conversation`.

2026-07-04T10:21:00+03:00 - Primary audit r84 сохранён штатной командой как `docs/verdicts/release-management/t-0238-audit-r84.md` с `VERDICT: NEEDS_FIXES`. Blocker B1: `WaitForOrdinaryChatReportAsync` слишком широко трактует `null` от `CopyLatestAssistantMessageMarkdownAsync` как отсутствие copy-кнопки после завершения генерации; тот же `null` также означает, что новый assistant response текущей отправки ещё не появился или счётчик сообщений ещё меньше `messageCountBeforeSend + 2`. До следующего package нужно разделить эти состояния локальным driver/harness-тестом: ожидание ещё не появившегося ответа не должно запускать bounded diagnostic `E2D-BUILD-AUDIT-SUBMIT-ORDINARY-COPY-UNAVAILABLE`, а устойчивое отсутствие copy-кнопки у уже найденного текущего assistant response должно сохранять эту диагностику. Следующий package должен быть r85: включить primary r84 в `previousVerdictChain` и `repoFileAllowlist`, добавить closure r84 B1 в `blockerClosureList`, пройти полный Medium и отправлять primary через `--reuse-conversation`.

2026-07-04T10:26:00+03:00 - Закрытие r84 B1 локально реализовано через типизированный результат ordinary copy polling: `NoCurrentAssistantYet` означает, что новый assistant response текущей отправки ещё не появился или счётчик сообщений недостаточен, поэтому команда продолжает обычное ожидание; `CopyActionUnavailable` применяется только после доказанного появления текущего assistant response и запускает bounded diagnostic при устойчивом повторе. Добавлен RED/GREEN regression `AuditSubmitOrdinaryPollingWaitsWhenCurrentAssistantResponseHasNotAppeared`; вместе с прежним `AuditSubmitOrdinaryPollingFailsPersistentMissingCopyButtonWithLocalDiagnostic` он прошёл 2/2 и подтверждает обе стороны r84 B1. Доменный документ уточняет, что отсутствие нового ответа не запускает таймер отсутствующей copy-кнопки. Перед r85 обязателен полный Medium.

2026-07-04T10:40:00+03:00 - Medium после r84 B1 typed ordinary polling fix прошёл: build `eng\Electron2D.Build` - passed, 0 warnings; `update docs` - updated; `update docs --check` - passed; `rg` не нашёл `docs/verdicts/` в generated docs index/shard; build integration tests - passed, 0 warnings; широкий focused `AuditSubmit`/package message/documentation/generated-docs suite - 189/189 passed за 3 m 54 s; `verify docs` - passed; `verify audit-followups` - passed for 16 actionable findings across 117 saved audit reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Перед r85 package нужно ещё раз синхронизировать generated docs после этой записи; r85 должен включить r84 в saved verdict chain, добавить closure r84 B1 и отправляться через `--reuse-conversation`.

2026-07-04T10:56:00+03:00 - Первый локально созданный r85 package artifact не был отправлен: clean-verify был ошибочно запущен с пустым `--repo` под `.temp/audit-clean`, из-за чего текущая рабочая копия была восстановлена к baseline, а `.temp/audit` с ZIP/config был удалён как локальный artifact. Это не внешний verdict и не submit. Текущее tracked-состояние T-0238 восстановлено из clean checkout `.temp/audit-clean/T-0238-r84-20260704-100940-lf`, r84 B1 fix повторно наложен, focused pair `AuditSubmitOrdinaryPollingWaitsWhenCurrentAssistantResponseHasNotAppeared|AuditSubmitOrdinaryPollingFailsPersistentMissingCopyButtonWithLocalDiagnostic` снова прошёл 2/2. Перед новым r85 package нужно заново пройти Medium и использовать только существующий Git clean clone для `audit package verify`.

2026-07-04T11:01:00+03:00 - Medium после восстановления рабочей копии и повторного r84 B1 fix прошёл заново: build `eng\Electron2D.Build` - passed, 0 warnings; focused pair `AuditSubmitOrdinaryPollingWaitsWhenCurrentAssistantResponseHasNotAppeared|AuditSubmitOrdinaryPollingFailsPersistentMissingCopyButtonWithLocalDiagnostic` - 2/2 passed; `update docs` - updated; `update docs --check` - passed; `rg` не нашёл `docs/verdicts/` в generated docs index/shard; build integration tests - passed, 0 warnings; широкий focused `AuditSubmit`/package message/documentation/generated-docs suite - 189/189 passed за 3 m 57 s; `verify docs` - passed; `verify audit-followups` - passed for 16 actionable findings across 117 saved audit reports; `verify licenses` - passed for 661 source files; `git diff --check` - passed. Перед пересозданием r85 package нужно ещё раз синхронизировать generated docs после этой записи.

2026-07-04T11:58:00+03:00 - Primary audit r85 сохранён штатной командой как `docs/verdicts/release-management/t-0238-audit-r85.md` с `VERDICT: ACCEPT`, затем clean-control ZIP `.temp/audit-control/T-0238-audit-r85.zip` был собран без saved reports/`TASKS.md`/дневника, clean-verified на `.temp/audit-clean/T-0238-r85-control-20260704-113654-lf-git` и отправлен новым control chat через `--control-audit`; control report сохранён как `docs/verdicts/release-management/t-0238-audit-control-r85.md` с `VERDICT: NEEDS_FIXES`. Blocker control-r85 B1: payload-ready guard перед `ClickSendAsync` доказывает наличие ожидаемого ZIP, но не доказывает, что в composer нет второго attachment/file chip; лишний sidecar или другой файл может уйти вместе с основным ZIP. Следующий package должен быть r86 primary: включить primary r85 и control-r85 в `previousVerdictChain` и `repoFileAllowlist`, добавить closure control-r85 B1 в `blockerClosureList`, пройти полный Medium и отправлять через `--reuse-conversation`.

## T-0240 [ ] P1: Разделить быстрые, средние и тяжёлые проверки аудиторской автоматизации

- Создана: 2026-07-02T11:32:26+03:00
- Восстановлена: 2026-07-04T12:40:00+03:00
- Состояние: in progress
- Приоритет: P1
- Зависимости: T-0238
- Ссылки:
  - Доменный документ: `docs/release-management/audit-package.md`
  - Исходный код: `eng/Electron2D.Build/AuditPackageCommand.cs`
  - Исходный код: `eng/Electron2D.Build/AuditSubmitCommand.cs`
  - Исходный код: `eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`
  - Тесты: `tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`
  - Локальный процесс: `AGENTS.md`
  - Локальный процесс: `.codex/prompts/goal-task-loop.md`

### Самодостаточное описание

Во время стабилизации `T-0238` выяснилось, что маленькие правки `AUDIT-REQUEST.md` или аудиторской автоматизации часто тянут за собой часовые локальные прогоны: тесты создают временные Git-репозитории, собирают аудиторский ZIP-архив, запускают `audit package verify`, а иногда подвисают в дочерних `dotnet run`. Это превращает `rNN` и локальный цикл проверки в счётчик отладки инструмента, хотя итерации внешнего аудита должны соответствовать только проверяемым пакетам.

Нужно разделить проверки аудиторского процесса на понятные уровни:

1. быстрый уровень для маркеров, статического текста, парсеров и валидаторов без ZIP, чистой копии репозитория и дочерних `dotnet run`;
2. средний уровень для узких интеграционных тестов только по реально изменённому поведению;
3. тяжёлый уровень для `audit package`, `audit package verify` и репетиции восстановления, который запускается один раз перед следующей упаковкой, а не после каждой мелкой правки.

Нужное поведение: локальная правка `AUDIT-REQUEST.md`, `.codex/prompts/goal-task-loop.md` или валидатора отчёта должна иметь быстрый штатный проверочный путь, который агент запускает одной командой. Внутренний фильтр тестов может быть дополнительным удобным путём, но не заменяет команду. Этот путь не создаёт архив, не запускает восстановление чистой копии репозитория и технически не может уйти в тяжёлый путь упаковки. Тяжёлые проверки остаются обязательными перед внешней отправкой пакета, но перестают быть единственным способом отладки мелких расхождений в контрактных тестах.

### Целевая экономика цикла

`T-0240` закрывает не проблему "мало фильтров", а проблему "проверочный хвост стал дороже работы". Текущий дефект процесса: мелкая правка аудиторского текста занимает около 5 минут, но затем запускает примерно час локальных тестов, сборки пакета, проверки восстановления и повторных `checks` внутри `audit package`. После этого внешнее глубокое исследование (`Deep Research`) добавляет ещё около 10 минут. Такой цикл непригоден для итеративной работы: цена проверки в 10-15 раз выше цены самой правки.

Целевой результат: мелкая правка `AUDIT-REQUEST.md`, `.codex/prompts/goal-task-loop.md` или валидатора отчёта имеет быстрый локальный путь с числовой сводкой и понятным бюджетом. Тяжёлый путь упаковки, проверки, восстановления и отправки запускается только один раз, когда локально уже стабильно и пакет действительно готов к внешней отправке.

### Критерии приёмки

- [ ] `docs/release-management/audit-package.md` описывает уровни проверок аудиторского процесса: быстрый, средний и тяжёлый, с понятным назначением каждого уровня.
- [ ] `docs/release-management/audit-package.md` фиксирует целевую экономику цикла: быстрый путь нужен для микроправок аудиторского запроса, разбора отчёта и локального prompt-а, а тяжёлый путь нужен только как финальный барьер перед внешней отправкой.
- [ ] `.codex/prompts/goal-task-loop.md` включён в область `T-0240` и описывает быстрый, средний и тяжёлый локальный коридор: после мелкой правки `AUDIT-REQUEST.md`, `.codex/prompts/goal-task-loop.md` или валидатора сначала быстрый путь, затем релевантный средний тест, затем тяжёлый `audit package`/`audit package verify` только перед реальной внешней итерацией.
- [ ] Финальное поведение предоставляет штатную команду `dotnet run --project eng/Electron2D.Build -- verify audit-contracts` для быстрых проверок аудиторских контрактов. Стабильный xUnit-фильтр допустим только как дополнительный внутренний точечный путь, но не как замена команды. Команда проверяет `AUDIT-REQUEST.md`, `.codex/prompts/goal-task-loop.md`, `AGENTS.md`, обязательные маркеры, контракты разбора и проверки отчётов и не создаёт audit ZIP, не создаёт чистую копию репозитория, не запускает `audit package verify` и не вызывает дочерние `dotnet run`.
- [ ] `verify audit-contracts` печатает человекочитаемую итоговую сводку: уровень проверки, сколько проверок или тестов запущено, сколько прошло, сколько упало, сколько пропущено и сколько занял весь быстрый уровень.
- [ ] `verify audit-contracts` имеет задокументированный бюджет времени и объёма: ожидаемое число проверок или тестов, ожидаемое время, допустимый верхний предел и правило, когда превышение считается регрессией.
- [ ] Быстрый путь для мелкой правки аудиторского текста не должен быть дороже самой правки более чем в 1-2 раза без явного объяснения. Если правка заняла около 5 минут, быстрый локальный коридор не должен превращаться в 30-60 минут тестов.
- [ ] Тесты аудиторской автоматизации размечены стабильными xUnit `Trait`-метками или равнозначной репозиторной категоризацией: `AuditTier=Fast`, `AuditTier=Medium`, `AuditTier=Heavy`. Фильтры по имени допустимы только как дополнительный удобный путь, но не как основной контракт.
- [ ] Быстрый уровень (`AuditTier=Fast`) запрещает создание audit ZIP, распаковку ZIP, восстановление чистой копии репозитория, `audit package verify`, дочерние `dotnet run` и браузерную автоматизацию.
- [ ] Средний уровень (`AuditTier=Medium`) допускает узкие интеграционные тесты и контролируемый дочерний процесс только с явным тайм-аутом, но не допускает полное восстановление аудиторского пакета.
- [ ] Тяжёлый уровень (`AuditTier=Heavy`) содержит упаковку, проверку, восстановление и репетицию браузерного пути и запускается только перед внешней упаковкой или при изменении самой тяжёлой реализации.
- [ ] Дочерние `dotnet run`, `audit package` и `audit package verify` внутри интеграционных тестов имеют явный тайм-аут; помощник тайм-аута завершает не только родительский процесс, но и всё дерево дочерних процессов, чтобы зависшие `dotnet`, `vstest`, `git` или browser-helper процессы не оставались после теста.
- [ ] Диагностика тайм-аута содержит команду, имя теста, прошедшее время и относительный или санитизированный временный путь; домашний каталог пользователя, локальные абсолютные пути машины и секреты не попадают в отслеживаемые документы или аудиторские доказательства.
- [ ] `docs/release-management/audit-package.md` фиксирует, что локальные падения быстрых, средних или тяжёлых проверок, сборки аудиторского пакета или проверки пакета до внешней отправки не являются внешними аудиторскими итерациями, не увеличивают `rNN` как цепочку внешнего аудита и не попадают в `previousVerdictChain`; новая rNN создаётся только для проверенного пакета, который действительно готов к `submit`.
- [ ] Перед созданием внешнего аудиторского пакета документированный локальный коридор требует быстрый и релевантный средний уровень. Само создание пакета, `audit package verify`, восстановление чистой копии репозитория и браузерная репетиция относятся к тяжёлому уровню и выполняются как финальный пропускной барьер перед `audit submit`, не как отладчик после каждой правки.
- [ ] Средний и тяжёлый уровни печатают итоговую сводку: команда, уровень (`AuditTier`), число тестов или проверок, длительность, дочерние процессы и причина тайм-аута или сбоя, если она есть.
- [ ] `audit package` перед запуском настроенных `checks` печатает план: сколько проверок будет запущено, какие команды, какой тайм-аут у каждой команды, какие проверки являются доказательствами внутри пакета (`package evidence checks`) и почему они нужны именно там.
- [ ] `audit package` после настроенных `checks` печатает таблицу: имя проверки, команда, код выхода, длительность и пути к доказательствам `stdout`/`stderr`.
- [ ] `.codex/prompts/goal-task-loop.md` требует записывать в дневник числовую строку по уровням, например `Fast: 3 теста / 1m13s; Medium: 4 теста / 3m18s; Heavy: не запускался`, вместо общей фразы "точечные тесты прошли". Для аудиторской автоматизации записи без количества проверок и времени недостаточны.
- [ ] Если после мелкой правки аудиторского контракта быстрый уровень запускает существенно больше проверок, чем ожидается для `verify audit-contracts`, или длится дольше задокументированного бюджета, это считается регрессией `T-0240`, если нет явного объяснения, почему проверка относится к среднему или тяжёлому уровню.
- [ ] Локальный коридор перед упаковкой не должен без необходимости дублировать `checks`, которые `audit package` затем всё равно запускает как доказательства. Если проверка нужна именно как доказательство внутри ZIP, она помечается как проверка-доказательство пакета (`package evidence check`); если она нужна только для быстрой обратной связи, она остаётся вне пакета.
- [ ] Узкие тесты покрывают, что быстрая контрактная проверка и средний уровень не вызывают команды упаковки, восстановления, `AuditPackageCommand`, восстановления чистой копии репозитория (`clean repo restore`) и браузерную репетицию отправки, а тяжёлые тесты восстановления остаются доступными отдельным фильтром или командой.
- [ ] После реализации проходят `dotnet run --project eng/Electron2D.Build -- verify audit-contracts`, релевантная узкая интеграционная проверка, `verify docs`, `verify licenses` при изменениях исходного кода и `git diff --check`.

### Заметки агента

2026-07-02T11:32:26+03:00 - Создано по замечанию пользователя: правка и упаковка аудиторского архива не должны приводить к часовым тестам. Задача не отменяет обязательный `audit package verify` перед внешней отправкой, а разделяет быструю локальную обратную связь и тяжёлую финальную проверку.

2026-07-02T11:42:08+03:00 - Усилено по дополнительной оценке пользователя: задача должна дать штатную командную поверхность или стабильный репозиторный фильтр для быстрого коридора, обязательное машинное разделение `AuditTier=Fast/Medium/Heavy`, запреты уровней, тайм-аут всего дерева дочерних процессов, санитизированную диагностику и правило, что локальные неудачные попытки до `submit` не являются внешними итерациями `rNN`.

2026-07-02T11:52:17+03:00 - Уточнено по финальной рекомендации пользователя: команда `verify audit-contracts` стала обязательным итоговым поведением, xUnit-фильтр оставлен только дополнительным путём; локальный коридор теперь явно требует быстрый и средний уровень перед созданием внешнего пакета, а упаковка, проверка, восстановление и браузерная репетиция относятся к тяжёлому барьеру перед `audit submit`.

2026-07-02T12:02:18+03:00 - Усилено по разбору фактического T-0238 прогона: после точечного `AUDIT-REQUEST.md` набора 3/3 за 1m13s локальный коридор разросся минимум до 129 завершённых тестов, сборок, проверок, зависшего широкого запуска `AuditPackageMessage` и последующего повторного запуска упаковки. `T-0240` теперь требует числовые сводки по уровням, план до запуска настроенных `checks` и таблицу после них в `audit package`, дневниковую строку вида `Fast/Medium/Heavy`, правило регрессии для слишком тяжёлого быстрого пути и явное разделение проверок-доказательств пакета от проверок быстрой обратной связи.

2026-07-02T12:17:00+03:00 - Усилено по экономике цикла: `T-0240` теперь прямо фиксирует дефект "5 минут правки, около 60 минут локального хвоста, около 10 минут внешнего глубокого исследования" и требует, чтобы быстрый путь не был дороже мелкой правки более чем в 1-2 раза без явного объяснения.

2026-07-04T12:40:00+03:00 - Восстановлено по прямому запросу пользователя после временного удаления из active `TASKS.md`: задача снова является активной и снова стоит в roadmap после `T-0238` перед `T-0239`.

2026-07-04T13:14:00+03:00 - Начата реализация по прямому запросу пользователя после snapshot-коммита `6a89b98e`: первым срезом выбран штатный быстрый verifier `verify audit-contracts`, чтобы мелкие правки аудиторского запроса, локального prompt-а и разбора отчётов проверялись без сборки audit ZIP, чистой копии репозитория и дочерних `dotnet run`.

2026-07-04T13:50:00+03:00 - Реализован первый рабочий срез `T-0240`, задача остаётся открытой для оставшихся уровней и сводок `audit package`: добавлена команда `verify audit-contracts` с in-process проверкой `AUDIT-REQUEST.md`, `audit-package.md`, `.codex/prompts/goal-task-loop.md`, `AGENTS.md`, маркеров и парсеров отчётов; `Fast` результат текущего запуска: 25 checks / 22 ms, `Heavy: not-run`. Ускорена генерация package: archive-only evidence теперь выбирает минимальные корни поиска из `archiveOnlyEvidenceGlobs` и не начинает с полного обхода корня репозитория; sidecar subprocesses `audit package verify` и `audit package message` теперь запускаются с `--no-build --no-restore`. Проверки: build `eng\Electron2D.Build` - passed, 0 warnings; `verify audit-contracts` - passed; focused integration filter по `AuditWorkflowVerifyAuditContracts`, `AuditPackageSelectsArchiveOnlyEvidenceWithoutRepositoryWideScan`, `AuditPackageAllowsArchiveOnlyEvidenceUnderTempAuditEvidence`, `AuditPackageMessageOperatorWorkflowSidecarTargetsImmutableFinalPayload` - 5/5 passed; `update docs` - updated; `update docs --check` - passed; `verify docs` - passed; `verify licenses` - passed for 662 source files; `git diff --check` - passed.

2026-07-04T13:45:00+03:00 - После commit `1f768725` добавлен второй срез защиты от бесконечного audit loop: `audit package` теперь отклоняет широкие evidence-фильтры `dotnet test --filter` по семействам `AuditSubmit`, `AuditPackage`, `AuditPackageMessage`, `AuditRequest`, `AuditMessage` и `AuditWorkflow`, чтобы упаковка не повторяла большой средний прогон тестов внутри ZIP-доказательств. RED: `AuditPackageRejectsBroadAuditTestFiltersInEvidenceChecks` сначала доходил до реального `dotnet test` и падал с `E2D-BUILD-AUDIT-CHECK-FAILED`; GREEN: тот же тест проходит и получает `E2D-BUILD-AUDIT-CONFIG-INVALID` до запуска checks. Проверки: build `eng\Electron2D.Build` - passed, 0 warnings; `verify audit-contracts` - passed, Fast: 25 checks / 26 ms, Heavy: not-run; focused integration filter - 6/6 passed; `update docs` - updated; `update docs --check` - passed; `verify docs` - passed; `verify licenses` - passed for 663 source files; `git diff --check` - passed.

2026-07-04T13:51:00+03:00 - Добавлен третий срез прозрачности тяжёлого этапа: `audit package` печатает `E2D-BUILD-AUDIT-CHECKS-PLAN` перед запуском configured checks и `E2D-BUILD-AUDIT-CHECK-RESULT` после каждой проверки, включая имя, команду, рабочий каталог, тайм-аут, ожидаемый/фактический код, длительность и пути `stdout`/`stderr` внутри evidence. RED: `AuditPackagePrintsConfiguredChecksPlanAndResultSummary` не находил новые diagnostic-коды; GREEN: focused-набор проходит. Проверки: build `eng\Electron2D.Build` - passed, 0 warnings; `verify audit-contracts` - passed, Fast: 25 checks / 24 ms, Heavy: not-run; focused integration filter - 7/7 passed; `update docs` - updated; `update docs --check` - passed; `verify docs` - passed; `verify licenses` - passed for 663 source files; `git diff --check` - passed.

2026-07-04T14:27:00+03:00 - Добавлен четвёртый срез машинного разделения уровней: все 245 audit-тестов в `RepositoryBuildToolTests` получили ровно одну метку `AuditTier` (`Fast`: 22, `Medium`: 133, `Heavy`: 90); `test --integration-slice audit-package` теперь выбирает `AuditTier=Medium|AuditTier=Heavy`, а `repository-tooling` исключает все audit-уровни. Добавлены защитные тесты `AuditWorkflowAuditTestsDeclareAuditTierTraits` и `AuditWorkflowFastAuditTierDoesNotUseHeavyHelpers`; `verify audit-contracts` теперь читает `TestCommand.cs` и `RepositoryBuildToolTests.cs` и проверяет наличие tier-контракта. RED: новые tests сначала падали из-за 239 audit-тестов без уровня, broad `FullyQualifiedName` slice и двух прежних `Fast` tests, запускавших дочерний или тяжёлый путь. GREEN: `verify audit-contracts` прошёл `Fast: 31 checks / 25 ms; Heavy: not-run`; `AuditTier=Fast` прошёл 22/22 за 110 ms; focused Medium/guard набор прошёл 10/10 за 18 s; package-heavy tests `AuditPackageAllowsArchiveOnlyEvidenceUnderTempAuditEvidence`, `AuditPackageMessageOperatorWorkflowSidecarTargetsImmutableFinalPayload` и `AuditPackagePrintsConfiguredChecksPlanAndResultSummary` прошли по отдельности. Полный combined focused run был остановлен оболочкой после 184 s, после чего оставшиеся процессы текущего test run были закрыты; дальнейшая проверка выполнялась раздельно по уровням, чтобы не повторять слепой тяжёлый хвост.

2026-07-04T14:38:00+03:00 - Добавлен пятый срез явного разделения среднего и тяжёлого коридора: `test --integration-slice audit-medium` запускает только `AuditTier=Medium`, `test --integration-slice audit-heavy` запускает только `AuditTier=Heavy`, а `audit-package` сохранён как совместимый alias для `AuditTier=Medium|AuditTier=Heavy`. CI matrix больше не использует объединённый `audit-package` slice и вместо него содержит отдельные `audit-medium` и `audit-heavy`; `CiMatrixVerifier` и `verify audit-contracts` проверяют наличие новых срезов. RED: `TestCommandAuditTierIntegrationSlicesRunOnlySelectedAuditTier` сначала падал для обоих новых срезов. GREEN: тот же тест прошёл 2/2, verifier-тесты CI прошли 2/2, `verify ci-matrix` прошёл, `verify audit-contracts` прошёл `Fast: 31 checks / 47 ms; Heavy: not-run`, source-guards по audit-tier прошли 2/2. Документация обновлена: основным локальным маршрутом теперь считаются `audit-medium` и `audit-heavy`, а не слитный `audit-package`.

## T-0239 [ ] P2: Удалить неактивный screenshot recorder из внутренней реализации `audit submit`

- Создана: 2026-07-01T21:57:00+03:00
- Состояние: open
- Приоритет: P2
- Зависимости: T-0237
- Ссылки:
  - Доменный документ: `docs/release-management/audit-package.md`
  - Исходный код: `eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`
  - Исходный код: `eng/Electron2D.Build/AuditSubmitCommand.cs`
  - Тесты: `tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`
  - Источник: `RISKS_AND_NOTES` из `docs/verdicts/release-management/t-0237-audit-r04.md` и `docs/verdicts/release-management/t-0237-audit-control-r04.md`

### Самодостаточное описание

T-0237 убрала screenshot-доказательства из поддержанного audit workflow: `audit submit` больше не принимает параметр для каталога screenshot-ов, `AuditSubmitCommand.ParseOptions` передаёт `ScreenshotsDirectory: null`, документация говорит, что tool screenshots не являются доказательством состояния страницы, а внешние primary/control audit r04 приняли этот контракт. При этом во внутренней реализации `AuditSubmitCodexChromeCommand.cs` остался `AuditSubmitCodexChromeScreenshotRecorder` и вызовы `CaptureAsync`, которые сейчас фактически неактивны через CLI.

Нужное поведение: удалить или заменить no-op путём внутренний screenshot recorder и PNG capture helper так, чтобы audit submit code больше не содержал мёртвую screenshot-инфраструктуру, а диагностика оставалась на DOM dump, Markdown export validation, structured diagnostics и `--keep-tab-open-on-error`.

### Критерии приёмки

- [ ] `AuditSubmitCodexChromeCommand.cs` больше не содержит `AuditSubmitCodexChromeScreenshotRecorder`, PNG capture helper и вызовы `CaptureAsync` для audit submit workflow.
- [ ] `AuditSubmitCommand.cs` по-прежнему отклоняет старый screenshot directory option как invalid argument.
- [ ] `docs/release-management/audit-package.md` остаётся синхронизированным: `audit submit` не создаёт tool screenshots и не принимает их как evidence.
- [ ] Focused tests покрывают отсутствие публичного screenshot option и отсутствие screenshot recorder/capture plumbing в audit submit source.
- [ ] После реализации проходят focused integration tests, documentation checks, license verifier и `git diff --check`.

### Заметки агента

2026-07-01T21:57:00+03:00 - Создано как closure для actionable technical note из `RISKS_AND_NOTES` T-0237 r04: внутренний recorder не блокирует T-0237, потому публичный CLI больше не создаёт screenshots, но остаточную неактивную инфраструктуру нужно удалить отдельной задачей.

## ROADMAP

Обновлено: 2026-07-04T12:40:00+03:00.

Этот раздел задаёт рекомендуемый порядок выполнения активных задач из `TASKS.md`. Он не заменяет поля `Зависимости`, не закрывает задачи и не создаёт новый backlog. Если пользователь меняет приоритет, dependency graph или scope, этот раздел нужно обновить вместе с соответствующими задачами.

### 0. Пользовательская приёмка уже реализованных задач

На 2026-07-01T22:11:20+03:00 `T-0237` принята пользователем после primary/control external audit r04 и перенесена в `data/completed-tasks/2026/07 Июль.md`. Ранее `T-0236` принята внешним аудитом r02 и перенесена в тот же июльский архив. Ранее `T-0206` принята внешним аудитом r03 и перенесена в тот же июльский архив. Ранее `T-0210` принята внешним аудитом r20 и перенесена в тот же июльский архив. Ранее `T-0209` принята внешним аудитом r13 и перенесена в `data/completed-tasks/2026/06 Июнь.md`. Ранее в этот же архив перенесены `T-0235` после внешнего аудита r05, `T-0234` после внешнего аудита r04, `T-0208` после внешнего аудита r03 и `T-0215` после внешнего аудита r07. Ранее `T-0207`, `T-0220`, `T-0219`, `T-0212`, `T-0224`, `T-0167`, `T-0168`, `T-0169`, `T-0170`, `T-0173`, `T-0216` и `T-0217` также были приняты пользователем или внешним аудитом и перенесены в архив.

### 1. Решения перед новой игрой

Система координат уже определена совместимостью с Godot 4.7; отдельная задача по выбору системы координат не нужна.

Переименование публичного примера в `Platformer` принято и больше не блокирует активные задачи.

### 2. Foundational runtime и repository tooling

Эти потоки можно вести раздельно, но внутри каждого порядок закреплён зависимостями.

Runtime:

1. `T-0220` закрыта; следующий runtime performance gate идёт через `T-0221` после готового Platformer и завершённых acceptance/visual gates.

Texture public API:

1. `T-0226` — привести базовый `Texture2D` к контракту Godot 4.7: изображение, формат, placeholder resource и методы отрисовки.
2. `T-0227` — привести `AtlasTexture` к контракту Godot 4.7 и заменить одноуровневый механизм разрешения texture resource общим рекурсивным механизмом.

Repository tooling:

Закрыто: `T-0207` создала внутренний C#-инструмент репозитория `eng/Electron2D.Build`; `T-0228` добавила детерминированную сборку и проверку внешнего audit package; `T-0213` перенесла README/docs verifier-ы и generated documentation index на C#-поверхность этого инструмента; `T-0229` закрепила статический tracked `AUDIT-REQUEST.md`; `T-0214` перенесла API/Wiki/license/manifest verifier-ы на C#-поверхность и принята внешним аудитом r05; `T-0230` добавила штатный текст сообщения внешнему аудитору из `AUDIT-REQUEST.md` и обязательное «Глубокое исследование»; `T-0231` разделила локальный индекс документации на manifest/NDJSON-шарды и SQLite-кэш; `T-0232` закрепила общую LF-политику и стабильное восстановление audit package; `T-0233` разграничила `AGENTS.md`, `AUDIT-REQUEST.md` и локальный `goal-task-loop.md`, чтобы внешний аудит не дублировался ручными браузерными правилами; `T-0215` перенесла test runner, проверку бюджетов производительности и проверку эталонных метрик на C#-команды; `T-0208` закрыла tracking переноса тестовых, документационных и API-проверок на C# после accepted дочерних задач; `T-0209` перенесла локальную сборку релизных архивов и `release verify` на C#-команды; `T-0210` переключила CI, `AGENTS.md`, активные документы и оставшуюся автоматизацию репозитория на `eng/Electron2D.Build`; `T-0206` закрыла tracking полной миграции репозиторной автоматизации после внешнего аудита r03; `T-0236` стабилизировала CI после миграции автоматизации и разделила интеграционные тесты на быстрый и профильные тяжёлые срезы; `T-0237` добавила полные снимки изменённых файлов в audit ZIP, строгую проверку снимков и primary/control state machine для внешнего аудита.

Дополнительно закрыто: `T-0235` стабилизировала восстановление и извлечение внешнего отчёта `audit submit`.

Дальше выполнить `T-0238`: закрепить полный инженерный review для каждого primary и control audit, включая производительность, Public API/Godot 4.7 parity, реалистичность тестов, C# code style и архитектурную согласованность; обязательное tool-enforced closure actionable `RISKS_AND_NOTES`; отделение blocker-ов текущего acceptance-аудита от follow-up findings, чтобы не превращать каждую задачу в бесконечный repo-wide audit. После этого выполнить `T-0240`: разделить быстрые, средние и тяжёлые проверки аудиторской автоматизации и добавить штатную команду `verify audit-contracts`. Затем выполнить `T-0239`: удалить оставшуюся неактивную screenshot recorder plumbing из внутренней реализации `audit submit`.

Editor foundation:

1. `T-0175` — привести Project Settings и canonical `.e2d` к одному файловому контракту.
2. `T-0174` — усилить window smoke diagnostics; зависит только от уже неактивной `T-0171`.
3. `T-0176` -> `T-0177` -> `T-0178` — тема, текст/scrolling/DPI, interaction/focus/pointer lifecycle.

### 3. Настоящий Platformer

Миграция `T-0234` принята внешним аудитом r04. Дальше поток Platformer ведётся из проектной доски `examples/platformer/.electron2d/tasks/board.e2tasks`, а корневые release-задачи учитывают его как внешний блокер.

1. `T-0222` — следующая готовая карточка: пересобрать Platformer как законченную приёмочную игру.
2. Затем `T-0223` -> `T-0225` -> `T-0221` -> `T-0166` из `examples/platformer/.electron2d/tasks/board.e2tasks`.

### 4. Editor UI

Текущий основной порядок остаётся, но tracking-задачи `T-0179`, `T-0184` и `T-0187` закрываются после дочерних задач, а не исполняются как первый шаг.

1. После `T-0178`: параллельно `T-0194`, `T-0195`, `T-0196`.
2. После `T-0196`: `T-0197`.
3. `T-0179` — закрыть tracking desktop controls foundation после `T-0194`-`T-0197`.
4. После `T-0179`: `T-0180`.
5. `T-0181` -> `T-0182` -> `T-0183` — product shell и первый 2D workflow.
6. После `T-0183` можно параллельно вести `T-0185`, `T-0186`, `T-0188`, `T-0190`, `T-0191`, `T-0192`, `T-0198`, `T-0202`, `T-0203`, `T-0204`.
7. После `T-0190`, `T-0191`, `T-0192`, `T-0185`, `T-0188`: `T-0193`.
8. Script workspace: `T-0198` -> `T-0199`/`T-0200` -> `T-0201`, затем закрыть tracking `T-0184`.
9. Specialized editors: `T-0202`/`T-0203`/`T-0204` -> `T-0205`, затем закрыть tracking `T-0187`.

### 5. Automation, README и release preparation

1. После закрытой `T-0213`: `T-0110`.
2. После `T-0210` и `T-0110`: `T-0111`.

### 6. Convergence и preview release

1. `T-0189` — финальный Editor gate, если зрелый Editor остаётся обязательным для `0.1.0 Preview`.
2. `T-0092` — iOS arm64 export и проверка на macOS/Xcode, iOS-симуляторе или устройстве. Пока `releaseVerificationTargets` включает все шесть платформ, эта задача находится в критическом пути до `T-0093`; blocked artifact объясняет проблему окружения, но не заменяет реальную релизную проверку.
3. `T-0093` — Tier 1 smoke/soak; при выполнении лучше разложить на дочерние Windows/Linux/macOS/Android/iOS/WebAssembly checks согласно `T-0224`.
4. `T-0104` — release candidate gate поверх реальных packages, Editor gate и live Platformer metrics.
5. `T-0105` — post-preview risk register после `T-0104`.
