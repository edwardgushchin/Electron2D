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

2026-06-21T20:16:00+03:00 - Blocked по окружению, задача не завершена и не переносится в `completed-tasks/`. Текущий host - Windows; `xcodebuild` и `simctl` недоступны, `DEVELOPER_DIR`/`XCODE_ROOT` не заданы, iOS workload не установлен. Критерии требуют Xcode project generation и device/simulator smoke, поэтому закрывать задачу без macOS/Xcode/iOS simulator или device нельзя.

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
- Зависимости: T-0088, T-0089, T-0090, T-0091, T-0092, T-0094, T-0136, T-0221, T-0222, T-0223, T-0224, T-0225
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

2026-06-24T22:12:00+03:00 - После отказа принять `T-0224` в зависимости `T-0093` возвращена `T-0092`. Пока `releaseVerificationTargets` содержит все шесть платформ, iOS-проверка на macOS/Xcode является обязательной частью критического пути, а blocked artifact не может быть бессрочным обходом перед финальным релизным проходом.

## T-0104 [ ] P0: Провести полный release candidate gate для `0.1.0 Preview`

- Создана: 2026-06-20T16:16:20+03:00
- Состояние: open
- Приоритет: P0
- Зависимости: T-0093, T-0096, T-0097, T-0103, T-0111, T-0113, T-0128, T-0189, T-0221, T-0225
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

## T-0166 [ ] P0: Ужесточить Platformer после аудита Godot-переносимости

- Создана: 2026-06-23T21:02:00+03:00
- Состояние: tracking
- Приоритет: P0
- Зависимости: T-0221, T-0222, T-0223, T-0225
- Ссылки:
  - Спецификация: `docs/examples/platformer.md`
  - Документация: `docs/examples/platformer.md`
  - Исходный код: `examples/platformer/scripts/PlatformerGame.cs`
  - Связанный runtime rendering: `src/Electron2D/Graphics/Rendering/CanvasSubmissionContext.cs`; `docs/rendering/camera-viewport-presentation-baseline.md`
  - Тесты: `tests/Electron2D.Tests.Integration/PlatformerProjectTests.cs`; `tests/Electron2D.Tests.Integration/ViewportPresentationTests.cs`

### Самодостаточное описание

Пользователь указал, что текущий `PlatformerGame.cs` слишком легко даёт ложноположительный результат: он проверяет названия подсистем, но слабо доказывает реальное поведение ввода, one-way tile collision, прыжка, pause, animation и save. После повторного аудита 2026-06-24 эта задача больше не является самостоятельным implementation scope: она стала tracking-задачей, которая закрывается только после дочерних задач по runtime rendering, frame scheduling, настоящему Platformer, black-box acceptance, visual gate и live performance.

Нужное поведение: Platformer остаётся валидным проектом Electron2D и становится законченной приёмочной игрой. Публичный 2D coordinate contract уже определён совместимостью с Godot 4.7: `+Y` направлен вниз, `Vector2.Up = (0, -1)`, gravity platformer положительная по `Y`, jump velocity отрицательная по `Y`. Не создавать отдельную задачу выбора иной системы координат и не строить Platformer/Editor поверх `Y-up` модели.

### Критерии приёмки

- [x] `T-0219` принят: интерактивный runtime rendering path не создаёт framebuffer/presentation resource каждый кадр и использует общий rendering path.
- [x] `T-0220` принят: frame scheduler использует `DeltaTime`, accumulator, deadline wait и deterministic bounded mode.
- [ ] `T-0222` принят: Platformer пересобран как законченная приёмочная игра.
- [ ] `T-0223` принят: black-box acceptance доказывает полный gameplay path без прямой мутации состояния verifier-ом.
- [ ] `T-0225` принят: visual/screenshot gate проверяет несколько игровых состояний через runtime probes и кадры, а не только наличие PNG.
- [ ] `T-0221` принят: live performance gate прошёл на актуальном Platformer.
- [ ] Исторические hardening-исправления от 2026-06-23 не используются как самостоятельное доказательство готовности игры.

### Подзадачи

- [x] Довести runtime FPS blockers через `T-0219` и `T-0220`.
- [ ] Выполнить переименование Platformer через `T-0212` до полной пересборки игры.
- [ ] Выполнить gameplay, acceptance, visual и performance дочерние задачи.
- [ ] Вернуть `T-0166` к пользовательской приёмке только после принятия дочерних задач.

### Заметки агента

2026-06-23T21:02:00+03:00 - Задача создана по текущему аудиту пользователя. Основное ограничение: не расширять scope до полной совместимости с Godot 4.7 и не менять координатный контракт движка без отдельной спецификации.

2026-06-23T21:18:00+03:00 - Scope расширен пользователем: оставшийся сбой Windows export packaging с отсутствующим `packs/scenes/main.e2dpkg` нужно исправить в рамках этой же задачи.

2026-06-23T21:30:00+03:00 - Implementation завершён, но задача оставлена active до пользовательской приёмки. `dotnet run --project examples/reference-platformer/ReferencePlatformer.csproj -- --verify` проверен отдельно и не применим к текущему reference project shape: `OutputType` равен `Library`, запуском владеют CLI/export routes.

2026-06-23T21:46:00+03:00 - После напоминания пользователя про screenshots просмотрены local playable, exported playable и empty-template PNG. Найден и исправлен визуальный дефект: `CanvasLayer` наследовал camera transform, из-за чего HUD обрезался; pause menu был вне screen-space layer, а `zIndex` панели перекрывал её же детей. Добавлены regression tests для `CanvasLayer` и структуры reference platformer UI; screenshots после правки показывают целый HUD и читаемый pause menu. Empty-template export screenshot остаётся чёрным по смыслу пустого шаблона.

2026-06-23T22:41:00+03:00 - После повторного аудита пользователя исправлены оставшиеся поведенческие проблемы в рамках `T-0166`: добавлен `ProcessMode` и `SceneTree.Paused`, `PlatformerGame.cs` переключает дерево сцены вместо декоративного `Visible`; kinematic solver сохраняет floor contact между кадрами без ручной записи `Player.Position`; reference scene получила отдельный обычный collision floor и скрытый служебный `PlatformTileMap`; runtime host задаёт root `Viewport.Size` до `_Ready()`, из-за чего `Camera2D` центрирует world по реальному viewport. Добавлены focused tests для pause/process mode, repeated floor contact, runtime viewport size и динамического camera follow. Новый screenshot `.temp/reference-platformer/reference-platformer-playable-pause-physics.png` просмотрен: HUD и pause menu в screen-space, player не обрезан, лишние декоративные обрезания по краям убраны, строка HUD стала `CHECKPOINT-01 COINS 1 X 9`. Связанный integration-набор 26/26, `CleanRuntimeBaselineTests` и source license verifier прошли.

2026-06-24T14:32:00+03:00 - Целевое публичное имя примера теперь `Platformer`. Полная замена путей `reference-platformer`, типов `ReferencePlatformer*`, тестов, manifests и release matrix выполняется в `T-0212`; исторические заметки этой задачи не переписывать до фактической миграции.

2026-06-24T19:20:00+03:00 - Пользователь отклонил состояние `ready for acceptance`: текущий `Platformer` остаётся демонстрацией подсистем, а не законченной игрой, и одновременно страдает от серьёзных FPS-проблем в `RuntimeHost`. Прежние отмеченные пункты остаются историей выполненного hardening scope, но пользовательская приёмка `T-0166` заблокирована до исправления runtime rendering/frame pacing, реального performance gate и нового игрового вертикального среза.

2026-06-24T19:45:00+03:00 - `T-0166` переведена в tracking-задачу по замечанию пользователя. Старые `[x]` из scope 2026-06-23 больше не являются активными критериями приёмки; они остаются в исторических заметках как уже выполненный hardening, но текущая приёмка определяется дочерними задачами `T-0219`, `T-0220`, `T-0221`, `T-0222`, `T-0223` и `T-0225`.

2026-06-25T20:01:03+03:00 - Пользователь принял `T-0219`; первый критерий tracking-задачи отмечен выполненным, а активные зависимости `T-0166` теперь начинаются с `T-0220`.

2026-06-26T01:38:51+03:00 - Внешний аудит r04 принял `T-0220`; критерий frame scheduler отмечен выполненным, а активные зависимости `T-0166` теперь начинаются с `T-0222`, `T-0223`, `T-0225` и `T-0221`.

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
- [ ] `dotnet run --project eng\Electron2D.Build -- verify licenses` проходит.

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
- [ ] `dotnet run --project eng\Electron2D.Build -- verify licenses` и `dotnet run --project eng\Electron2D.Build -- verify docs` проходят.

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

## T-0206 [ ] P0: Перевести repository automation с PowerShell на C#

- Создана: 2026-06-24T14:32:00+03:00
- Состояние: tracking
- Приоритет: P0
- Зависимости: нет
- Ссылки:
  - Доменный документ: `docs/release-management/ci-matrix.md`; `docs/release-management/release-packaging.md`
  - Будущий исходный код: `eng/Electron2D.Build/`
  - Связанные задачи: T-0207, T-0208, T-0209, T-0210, T-0213, T-0214, T-0215, T-0228

### Самодостаточное описание

Tracking-задача для полной замены repository automation на единый внутренний C# tool. Цель - убрать PowerShell как обязательный слой проверки, упаковки, документации, CI и release preparation. Это не часть публичного `e2d` CLI и не должно попадать в release package.

### Критерии приёмки

- [x] Создана и закрыта `T-0207`.
- [x] Создана и закрыта tracking-задача `T-0208`.
- [ ] Создана и закрыта `T-0209`.
- [ ] Создана и закрыта `T-0210`.
- [x] Создана и закрыта `T-0228`.
- [ ] C# verifier с allowlist подтверждает, что tracked production paths не содержат PowerShell scripts, `pwsh` workflow steps или `.ps1`-команды.
- [ ] Отдельная diagnostic-команда может показать оставшиеся исторические или миграционные упоминания, но они не считаются блокером, если явно разрешены allowlist-ом.

### Подзадачи

- [x] Выполнить `T-0207`.
- [x] Выполнить `T-0208`.
- [ ] Выполнить `T-0209`.
- [ ] Выполнить `T-0210`.
- [x] Выполнить `T-0228`.

## T-0209 [ ] P0: Перенести export и release packaging на C#

- Создана: 2026-06-24T14:32:00+03:00
- Состояние: blocked
- Приоритет: P0
- Зависимости: T-0207
- Ссылки:
  - Доменный документ: `docs/release-management/release-packaging.md`; `docs/export/`
  - Будущий исходный код: `eng/Electron2D.Build/`
  - Тесты: `tests/Electron2D.Tests.Integration/`

### Самодостаточное описание

Перенести export verifiers и release packaging в C# repository tool. Packaging должен строить publish outputs, archives, checksums, manifests, platform checks и draft release preparation без PowerShell.

### Критерии приёмки

- [ ] До кода обновлены `docs/release-management/release-packaging.md` и затронутые export-документы.
- [ ] Failing tests фиксируют package naming, archive layout, checksum, manifest и forbidden file policy.
- [ ] `dotnet run --project eng/Electron2D.Build -- package --rid win-x64` создаёт Windows artifact.
- [ ] `dotnet run --project eng/Electron2D.Build -- package --rid linux-x64` создаёт Linux artifact.
- [ ] `dotnet run --project eng/Electron2D.Build -- package --rid osx-arm64` создаёт macOS artifact.
- [ ] `dotnet run --project eng/Electron2D.Build -- release verify` проверяет draft-release policy без публикации релиза.
- [ ] Platform export checks больше не зависят от `.ps1`.
- [ ] После implementation focused packaging/export tests проходят.

### Подзадачи

- [ ] Составить карту export/release PowerShell scripts.
- [ ] Реализовать package commands.
- [ ] Реализовать release verify command.
- [ ] Обновить docs/tasks/CI.

## T-0210 [ ] P0: Переключить CI, AGENTS и документацию на C# tool и удалить PowerShell

- Создана: 2026-06-24T14:32:00+03:00
- Состояние: blocked
- Приоритет: P0
- Зависимости: T-0208, T-0209
- Ссылки:
  - Доменный документ: `docs/release-management/ci-matrix.md`; `docs/repository/repository-layout.md`
  - Исходный код: `.github/workflows/`; `AGENTS.md`; `docs/`; `tools/`
  - Будущий исходный код: `eng/Electron2D.Build/`

### Самодостаточное описание

После переноса verifier-ов и packaging на C# переключить CI, AGENTS, release docs, export docs, active tasks и generated docs на команды `eng/Electron2D.Build`, затем удалить tracked PowerShell scripts и активные ссылки на PowerShell.

### Критерии приёмки

- [ ] `.github/workflows/*.yml` больше не содержит `pwsh`, `.ps1` или PowerShell-команды.
- [ ] `AGENTS.md` и локальные навыки больше не требуют PowerShell.
- [ ] Доменные документы и активные задачи больше не содержат PowerShell-команды как целевой workflow.
- [ ] Все tracked `tools/*.ps1` удалены или заменены C# commands.
- [ ] C# verifier с allowlist подтверждает, что tracked production paths не содержат `.ps1`, `pwsh` или PowerShell-команд как активного workflow.
- [ ] Historical notes, migration docs и rejection notes допускаются только как явно перечисленные allowlist entries и не попадают в production/release package paths.
- [ ] CI проходит на Windows, Linux и macOS через C# repository tool.

### Подзадачи

- [ ] Переключить CI.
- [ ] Переключить AGENTS/docs/tasks.
- [ ] Удалить PowerShell scripts.
- [ ] Выполнить финальный scoped C# verifier.

## T-0221 [ ] P0: Восстановить performance gate для настоящего Platformer и RuntimeHost

- Создана: 2026-06-24T19:20:00+03:00
- Состояние: blocked
- Приоритет: P0
- Зависимости: T-0215, T-0223, T-0225
- Ссылки:
  - Доменный документ: `docs/quality/performance-verification.md`
  - Доменный документ: `docs/release-management/performance-budgets.md`
  - Доменный документ: `docs/examples/platformer.md`
  - Legacy verifier: `tools/Verify-ReferencePerformance.ps1`
  - C# performance command: `eng/Electron2D.Build`
  - Artifact: `data/quality/performance-reference-metrics.json`
  - Тесты: `tests/Electron2D.Tests.Integration/ReferencePerformanceVerificationTests.cs`
  - Тесты: `tests/Electron2D.Tests.Integration/RuntimeHostTests.cs`

### Самодостаточное описание

`T-0102` уже находится в локальном архиве как выполненная задача, но повторный аудит показал, что текущий performance gate нельзя считать достаточным: он опирается на статический artifact и короткие маркеры, а не доказывает реальную производительность интерактивного Platformer. Новый gate должен заново открыть рабочий scope без переписывания архивной истории `T-0102`.

Нужное поведение: generic runner, metrics schema, timeout и structured diagnostics принадлежат `T-0215`; эта задача владеет Platformer scenario, threshold policy, warm-up/measurement policy и evidence. Release-сборка настоящего Platformer измеряется в окне 1280x720. После прогрева не меньше 10 секунд выполняется измерение не меньше 60 секунд. Gate фиксирует P50/P95/P99 frame time, steady-state managed allocations, рост managed/private memory, GC collections, `input`, `physics`, `process`, `render-plan`, `submit`, `present`, количество draw commands, batches и texture switches. `0 B/frame` означает отсутствие повторяющихся управляемых выделений памяти .NET на каждом кадре после прогрева.

### Критерии приёмки

- [ ] До изменения кода `docs/quality/performance-verification.md` описывает новый live measurement contract и явно помечает прежний static artifact как недостаточный для текущего аудита.
- [ ] `T-0215` предоставляет C# performance runner, metrics schema, timeout и structured diagnostics; `T-0221` не создаёт новый PowerShell-only gate поверх будущего C# tooling.
- [ ] Добавлены сначала падающие tests, которые не дают пройти gate на текущем `RuntimeHost` с per-frame framebuffer allocation и post-render sleep.
- [ ] C# performance command из `T-0215` запускает свежий measurement run, привязанный к commit/build identity, а не только читает старый JSON.
- [ ] Обязательный сценарий `Platformer` запускается в Release, 1280x720, с warm-up не меньше 10 секунд и measurement window не меньше 60 секунд.
- [ ] Artifact фиксирует CPU, GPU, RAM, OS build, graphics driver, power plan, debugger attached = false, window visibility/focus и VSync/frame limiter mode.
- [ ] Artifact фиксирует Release commit SHA, build identity и точный command line.
- [ ] Artifact описывает метод измерения allocations и memory counters.
- [ ] Gate требует `p95FrameTimeMs <= 16.67`, фиксирует `p50`, `p95`, `p99`, `average`, `min`, `max` и итоговый pass/fail.
- [ ] Gate требует `steadyManagedAllocatedBytesPerFrame = 0` после прогрева и проверяет отсутствие постоянного роста managed memory.
- [ ] Artifact включает slope managed/private memory, missed-frame count, Gen0/Gen1/Gen2 collections во время measurement window, draw commands, batches, texture switches и time breakdown `input`/`physics`/`process`/`render-plan`/`submit`/`present`.
- [ ] Artifact содержит пути к screenshot/video/metrics evidence и не засчитывает пустой или визуально рассогласованный кадр.
- [ ] `T-0093` и `T-0104` не могут быть приняты, если этот gate не прошёл на актуальном Platformer.
- [ ] После зелёных проверок доменный документ и artifact обновлены фактическими командами и результатами.

### Подзадачи

- [ ] Обновить performance domain document под live measurement.
- [ ] Добавить red tests/verifier checks против текущего ложноположительного gate.
- [ ] Подключить `Platformer` scenario к generic runner/schema из `T-0215`.
- [ ] Реализовать сбор свежих runtime metrics, environment metadata и commit/build identity.
- [ ] Привязать `Platformer` scenario к результатам `T-0223` и `T-0225`.
- [ ] Запустить focused C# performance command и записать результат в дневник.

### Заметки агента

2026-06-24T19:20:00+03:00 - Создано как восстановление performance gate после повторного аудита. Архивную `T-0102` не переписывать; новая задача блокирует `T-0093` и release gate до реального измерения.

2026-06-24T19:45:00+03:00 - Ownership уточнён по аудиту пользователя: `T-0215` владеет generic C# performance runner, metrics schema, timeout и structured diagnostics; `T-0221` владеет Platformer scenario, thresholds, warm-up/measurement policy и evidence.

## T-0222 [ ] P0: Пересобрать Platformer как законченную приёмочную игру

- Создана: 2026-06-24T19:20:00+03:00
- Состояние: open
- Приоритет: P0
- Зависимости: нет
- Ссылки:
  - Доменный документ: `docs/examples/platformer.md`
  - Доменный документ: `docs/examples/reference-game-assets.md`
  - Исходный код: `examples/platformer/scenes/main.scene.json`
  - Исходный код: `examples/platformer/scripts/PlatformerGame.cs`
  - Assets: `data/assets/reference-games/platformer/`
  - Тесты: `tests/Electron2D.Tests.Integration/PlatformerProjectTests.cs`

### Самодостаточное описание

Повторный аудит пользователя признал текущий Platformer не игрой, а smoke-fixture подсистем. В `TileMapLayer` фактически две клетки, видимая земля собрана отдельными декоративными объектами и не совпадает с collision geometry, часть сегментов скрыта, checkpoint/star/flag не имеют игрового поведения, coins и checkpoint выставляются по координате или напрямую, `walk` использует одинаковые idle-кадры, scripted `jump` двигает персонажа вправо, а world, camera, HUD и декоративный уровень живут в рассогласованных пространствах.

Нужное поведение: после переименования `T-0212` Platformer должен стать небольшим, но законченным вертикальным срезом игры на 45-90 секунд прохождения. Это рабочая замена преждевременно закрытого scope `T-0094`, без переписывания архивной истории: новый активный task фиксирует, что пользовательская приёмка текущего результата не состоялась. Игра использует публичный 2D coordinate contract, совместимый с Godot 4.7: `+Y` вниз, `Vector2.Up = (0, -1)`, положительная platformer gravity по `Y`, отрицательная jump velocity по `Y`.

### Критерии приёмки

- [ ] До изменения сцен/кода `docs/examples/platformer.md` переписан как целевая спецификация настоящей игры, а не verifier showcase.
- [ ] Добавлены сначала падающие tests, которые ловят текущие две клетки `TileMapLayer`, декоративную землю вне collision, ручные coins/checkpoint и отсутствие реальной walk animation.
- [ ] Уровень рассчитан на 45-90 секунд прохождения и содержит старт, обычные платформы, one-way platforms, разрывы, опасность, checkpoint, collectibles и цель.
- [ ] Deterministic replay проходит objective path в диапазоне 45-90 секунд и сохраняется как acceptance evidence.
- [ ] Видимый уровень и collision geometry задаются единым игровым `TileMapLayer` или явно синхронизированной парой данных; декоративные `TextureRect` не расходятся с физикой.
- [ ] Все world-объекты находятся внутри одного `World`; camera, player, hazards, collectibles, checkpoint и goal используют одно координатное пространство.
- [ ] Coins являются настоящими collectible objects с collision/trigger behavior; код не присваивает `coins = 1` по координате или из verifier-а.
- [ ] Checkpoint является настоящим trigger object; сохранение прогресса происходит после игрового взаимодействия, а не через прямой вызов verifier-а.
- [ ] Есть явные состояния `Playing`, `Paused`, `Dead`, `Won` с документированными переходами.
- [ ] Есть title/start scene и result/restart flow либо другой реальный scene switch, проверяемый runtime path.
- [ ] Есть смерть, respawn, restart и детерминированное восстановление checkpoint/coins после reload.
- [ ] Semantics checkpoint и собранных coins детерминированы: повторный pickup не дублирует coin, checkpoint restore не теряет уже сохранённое состояние.
- [ ] Есть goal и наблюдаемое win state.
- [ ] Player имеет разные idle/run/jump/fall frames, корректное переключение animation state и разворот спрайта по направлению.
- [ ] Animation state transition вызывается только при смене состояния, а не повторно каждый движущийся кадр.
- [ ] `jump` не двигает персонажа вправо, а остановка движения возвращает animation в `idle`.
- [ ] Camera имеет limits, dead zone или документированное поведение следования, pixel snapping и не оставляет пустую чёрную половину кадра.
- [ ] HUD находится только в `CanvasLayer`; pause menu, звук и save/load остаются частью игрового loop.
- [ ] HUD обновляется только при изменении отображаемого состояния и не создаёт новую строку каждый кадр.
- [ ] Production gameplay class не содержит `RunHeadlessVerification`, `RunPlayableScript` или аналогичные acceptance-only entrypoints; acceptance hooks вынесены в тестовый/runner слой.
- [ ] После зелёных проверок доменный документ описывает фактическую игру, ограничения, screenshots и команды проверки.

### Подзадачи

- [ ] Переписать доменный документ под вертикальный срез игры.
- [ ] Добавить red tests на scene geometry, gameplay objects и animation frames.
- [ ] Выполнить `T-0212` до изменения уровня и gameplay code.
- [ ] Пересобрать scene data и gameplay script без ручных verifier shortcuts.
- [ ] Проверить визуальный кадр Platformer и исправить camera/world/UI alignment.
- [ ] Запустить focused Platformer checks и обновить дневник.

### Заметки агента

2026-06-24T19:20:00+03:00 - Создано по пользовательскому audit rejection. Текущий `T-0166` больше не готов к приёмке; этот task должен сделать Platformer игрой, а не набором subsystem markers.

2026-06-24T19:45:00+03:00 - Приоритет повышен до P0 и добавлена зависимость от `T-0212`: полную переделку игры нужно делать уже в каталоге/проекте `Platformer`, иначе новый уровень, tests, scripts и docs придётся переименовывать повторно.

2026-06-26T01:38:51+03:00 - После принятия `T-0220` внешним аудитом r04 все зависимости этой задачи закрыты; `T-0222` переведена из `blocked` в `open`.

## T-0223 [ ] P0: Переписать Platformer acceptance без самопроверки

- Создана: 2026-06-24T19:20:00+03:00
- Состояние: blocked
- Приоритет: P0
- Зависимости: T-0222
- Ссылки:
  - Доменный документ: `docs/examples/platformer.md`
  - Доменный документ: `docs/runtime/headless-runtime-automation.md`
  - Verifier: `tools/Verify-Platformer.ps1`
  - Исходный код: `examples/platformer/scripts/PlatformerGame.cs`
  - Тесты: `tests/Electron2D.Tests.Integration/PlatformerProjectTests.cs`

### Самодостаточное описание

Текущий verifier слишком часто проверяет сам себя: он может вручную задавать скорость, вызывать checkpoint/save, засчитывать coin/checkpoint по координате и считать `CharacterMoved` истинным даже по стартовой ненулевой позиции. `Playable=True` не доказывает прохождение игрового цикла. Visual/screenshot analysis вынесен в отдельную `T-0225`, чтобы acceptance оставался black-box gameplay проверкой.

Нужное поведение: acceptance должен наблюдать реальный gameplay path через runtime launch, `InputMap` events и игровые объекты. Verifier может подавать ввод и читать состояние, но не имеет права напрямую выставлять checkpoint, coins, save payload или победу.

### Критерии приёмки

- [ ] До изменения verifier-а `docs/examples/platformer.md` описывает acceptance sequence и запрещённые прямые мутации игрового состояния.
- [ ] Добавлены сначала падающие tests, которые ловят текущие ложноположительные `Playable=True`, `CharacterMoved` и прямую выдачу coins/checkpoint/save.
- [ ] `Playable=True` требует наблюдаемую последовательность `runtime launch -> InputMap events -> movement -> jump -> landing on one-way platform -> coin pickup -> checkpoint activation -> death -> respawn -> process shutdown -> process relaunch -> save restore -> goal`.
- [ ] Verifier не вызывает игровые методы так, чтобы обходить collision/trigger path, и не присваивает checkpoint, coins, save payload или win state напрямую.
- [ ] `CharacterMoved` считается истинным только после реального смещения от input/runtime step, а не из-за ненулевой стартовой позиции.
- [ ] Save verification читает файл, созданный после игрового checkpoint/collectible path, и сравнивает payload с состоянием игры.
- [ ] Acceptance выполняет и смерть/respawn, и отдельный relaunch процесса с восстановлением сохранения; `death or reload` не допускается как ослабленный вариант.
- [ ] Acceptance artifact включает machine-readable event trace по ключевым gameplay events, frame index, timestamp и итоговую причину pass/fail.
- [ ] После зелёных проверок доменный документ фиксирует фактический acceptance contract и команды проверки.

### Подзадачи

- [ ] Уточнить acceptance sequence в доменном документе.
- [ ] Добавить red tests на ложноположительный verifier.
- [ ] Переписать play-script/verifier path на наблюдение gameplay events.
- [ ] Запустить focused Platformer verifier и записать результат в дневник.

### Заметки агента

2026-06-24T19:20:00+03:00 - Создано по пользовательскому audit rejection. Без этой задачи `T-0222` может снова стать зелёным boolean-маркером вместо доказательства, что пример действительно играется.

2026-06-24T19:45:00+03:00 - Приоритет повышен до P0, acceptance сделан полностью black-box, а screenshot/visual gate вынесен в отдельную `T-0225`.

## T-0225 [ ] P0: Вынести Platformer visual gate в отдельные screenshots и runtime probes

- Создана: 2026-06-24T19:45:00+03:00
- Состояние: blocked
- Приоритет: P0
- Зависимости: T-0222, T-0223
- Ссылки:
  - Доменный документ: `docs/examples/platformer.md`
  - Доменный документ: `docs/testing/scene-visual-testing.md`
  - Verifier: `tools/Verify-Platformer.ps1`
  - Тесты: `tests/Electron2D.Tests.Integration/PlatformerProjectTests.cs`
  - Тесты: `tests/Electron2D.Tests.Integration/ViewportPresentationTests.cs`

### Самодостаточное описание

Старый screenshot gate проверял слишком мало: наличие PNG и отсутствие грубо пустого кадра не доказывают, что камера, world, HUD и gameplay state согласованы. При этом один пиксельный анализ крупной тёмной области будет давать ложные срабатывания на законных тёмных сценах.

Нужное поведение: visual gate должен проверять несколько игровых состояний Platformer через сочетание runtime probes и кадров. Runtime probes, то есть машинно-читаемые данные из запуска игры, фиксируют player/world/camera/viewport/HUD geometry, текущий state и event trace. Pixel analysis используется поверх этих данных, а не вместо них.

### Критерии приёмки

- [ ] До изменения verifier-а `docs/examples/platformer.md` и `docs/testing/scene-visual-testing.md` описывают visual gate как отдельный контракт от black-box gameplay acceptance.
- [ ] Gate создаёт кадры start, checkpoint, death, pause и goal.
- [ ] Для каждого кадра artifact содержит runtime probes: player bounds, camera transform, viewport size, HUD bounds, current state, frame index и event timestamp.
- [ ] Pixel analysis проверяет пустые/обрезанные области с учётом runtime probes и не падает только из-за законной тёмной сцены.
- [ ] Gate ловит рассогласование `World`/camera/HUD, скрытую служебную геометрию, обрезанный уровень и HUD, который движется вместе с камерой.
- [ ] Gate не засчитывает screenshot, если PNG создан не из scene tree/rendering path актуального `e2d run`.
- [ ] Focused tests отклоняют специально подготовленные malformed fixtures с рассогласованными `World`/camera/HUD probes, обрезанным viewport и пустым кадром, а актуальные кадры Platformer проходят.
- [ ] После зелёных проверок artifact paths и команды проверки записаны в доменный документ и дневник.

### Подзадачи

- [ ] Описать visual gate contract отдельно от gameplay acceptance.
- [ ] Добавить red tests на start/checkpoint/death/pause/goal screenshots и runtime probes.
- [ ] Реализовать сбор probes и pixel analysis поверх probes.
- [ ] Запустить focused visual checks и записать результат в дневник.

### Заметки агента

2026-06-24T19:45:00+03:00 - Создано по замечанию пользователя: screenshot gate нужно отделить от `T-0223`, иначе black-box acceptance снова смешается с слабым анализом одного PNG.

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

## ROADMAP

Обновлено: 2026-06-30T12:11:05+03:00.

Этот раздел задаёт рекомендуемый порядок выполнения активных задач из `TASKS.md`. Он не заменяет поля `Зависимости`, не закрывает задачи и не создаёт новый backlog. Если пользователь меняет приоритет, dependency graph или scope, этот раздел нужно обновить вместе с соответствующими задачами.

### 0. Пользовательская приёмка уже реализованных задач

На 2026-06-30T12:11:05+03:00 `T-0208` принята внешним аудитом r03 и перенесена в `completed-tasks/2026/06 Июнь.md`. `T-0215` ранее принята внешним аудитом r07 и перенесена в тот же архив. Ранее `T-0207`, `T-0220`, `T-0219`, `T-0212`, `T-0224`, `T-0167`, `T-0168`, `T-0169`, `T-0170`, `T-0173`, `T-0216` и `T-0217` также были приняты пользователем или внешним аудитом и перенесены в архив.

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

Закрыто: `T-0207` создала внутренний C#-инструмент репозитория `eng/Electron2D.Build`; `T-0228` добавила детерминированную сборку и проверку внешнего audit package; `T-0213` перенесла README/docs verifier-ы и generated documentation index на C#-поверхность этого инструмента; `T-0229` закрепила статический tracked `AUDIT-REQUEST.md`; `T-0214` перенесла API/Wiki/license/manifest verifier-ы на C#-поверхность и принята внешним аудитом r05; `T-0230` добавила штатный текст сообщения внешнему аудитору из `AUDIT-REQUEST.md` и обязательное «Глубокое исследование»; `T-0231` разделила локальный индекс документации на manifest/NDJSON-шарды и SQLite-кэш; `T-0232` закрепила общую LF-политику и стабильное восстановление audit package; `T-0233` разграничила `AGENTS.md`, `AUDIT-REQUEST.md` и локальный `goal-task-loop.md`, чтобы внешний аудит не дублировался ручными браузерными правилами; `T-0215` перенесла test runner, проверку бюджетов производительности и проверку эталонных метрик на C#-команды; `T-0208` закрыла tracking переноса тестовых, документационных и API-проверок на C# после accepted дочерних задач.

1. `T-0209` — перенести export verifiers и release packaging на C# repository tool.
2. `T-0210` — переключить CI, AGENTS и документацию на C# tool после `T-0209`.
3. `T-0206` — закрыть tracking полной миграции repository automation после `T-0210`.

Editor foundation:

1. `T-0175` — привести Project Settings и canonical `.e2d` к одному файловому контракту.
2. `T-0174` — усилить window smoke diagnostics; зависит только от уже неактивной `T-0171`.
3. `T-0176` -> `T-0177` -> `T-0178` — тема, текст/scrolling/DPI, interaction/focus/pointer lifecycle.

### 3. Настоящий Platformer

Этот поток блокирует повторную приёмку `T-0166`, smoke/soak `T-0093` и release gate `T-0104`.

1. `T-0222` — open: пересобрать Platformer как законченную приёмочную игру.
2. `T-0223` — black-box acceptance через runtime launch, InputMap events, death/respawn, process relaunch, save restore и goal.
3. `T-0225` — visual gate по кадрам start/checkpoint/death/pause/goal и runtime probes.
4. После `T-0223` и `T-0225`: `T-0221` — live performance gate Platformer через C# performance runner.
5. `T-0166` — закрыть как tracking epic только после принятия дочерних задач.

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
2. После `T-0209`, `T-0210` и `T-0110`: `T-0111`.

### 6. Convergence и preview release

1. `T-0189` — финальный Editor gate, если зрелый Editor остаётся обязательным для `0.1.0 Preview`.
2. `T-0092` — iOS arm64 export и проверка на macOS/Xcode, iOS-симуляторе или устройстве. Пока `releaseVerificationTargets` включает все шесть платформ, эта задача находится в критическом пути до `T-0093`; blocked artifact объясняет проблему окружения, но не заменяет реальную релизную проверку.
3. `T-0093` — Tier 1 smoke/soak; при выполнении лучше разложить на дочерние Windows/Linux/macOS/Android/iOS/WebAssembly checks согласно `T-0224`.
4. `T-0104` — release candidate gate поверх реальных packages, Editor gate и live Platformer metrics.
5. `T-0105` — post-preview risk register после `T-0104`.
