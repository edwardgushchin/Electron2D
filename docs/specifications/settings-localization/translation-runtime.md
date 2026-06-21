# Translation resource, locale switching и `Tr`

Статус: целевая спецификация для `T-0075`.
Обновлено: 2026-06-21.

## Назначение

Electron2D `0.1.0 Preview` должен иметь минимальный runtime-контракт локализации для игр, редактора и будущих настроек проекта. Контракт включает resource с переводами, общий сервер переводов, определение начальной локали из окружения процесса, ручную смену локали, перевод через `Tr(string key)` и предсказуемый fallback для отсутствующих ключей.

## Public API

`Translation` наследуется от `Resource` и хранит набор сообщений для одной локали:

- `Locale` - нормализованная строка локали. Дефис в locale name приводится к подчёркиванию, например `en-US` хранится как `en_US`.
- `AddMessage(string srcMessage, string xlatedMessage, string context = "")` - добавляет или заменяет перевод.
- `EraseMessage(string srcMessage, string context = "")` - удаляет перевод, если он есть.
- `GetMessage(string srcMessage, string context = "")` - возвращает перевод или пустую строку, если ключа нет.
- `GetMessageList()` - возвращает стабильный отсортированный список исходных ключей.

`TranslationServer` управляет загруженными переводами процесса:

- `GetLocale()` возвращает текущую локаль.
- `SetLocale(string locale)` задаёт текущую локаль вручную.
- `AddTranslation(Translation translation)` добавляет resource в registry.
- `RemoveTranslation(Translation translation)` удаляет resource из registry.
- `Clear()` удаляет все загруженные переводы без сброса текущей локали.
- `GetLoadedLocales()` возвращает стабильный отсортированный список локалей, для которых загружены переводы.
- `Translate(string message, string context = "")` возвращает переведённую строку.

`Object.Tr(string message, string context = "")` делегирует перевод в `TranslationServer.Translate()`, поэтому `Node.Tr(string key)` доступен без отдельного node-only API.

## Locale lookup

При старте процесса `TranslationServer` определяет локаль через `CultureInfo.CurrentUICulture`. Пустая или invariant culture приводится к `en`. Имена локалей нормализуются только по разделителю `-`/`_`; расширенная таблица языков и стран остаётся отдельной задачей.

Для `Translate()` порядок lookup такой:

1. точная текущая локаль, например `fr_CA`;
2. базовый язык текущей локали, например `fr`;
3. fallback locale `en`;
4. исходный ключ `message`.

Если `message` пустой, возвращается пустая строка. Если ключ отсутствует во всех загруженных переводах, возвращается исходный ключ, чтобы UI оставался предсказуемым и debug-friendly.

## Visible UI update

`Label` использует `Tr(Text)` при отрисовке текста. Смена локали или набора загруженных переводов должна инвалидировать cached draw commands для label на следующем processed frame, чтобы видимый текст обновлялся без ручного `QueueRedraw()`.

## Ограничения `T-0075`

- File-level import формата переводов не реализуется в этой задаче.
- Project/user settings для сохранения выбранной локали остаются задачей `T-0076`.
- Plural forms, pseudolocalization и editor translation extraction не входят в этот baseline.
- Locale matching ограничен exact/base-language/fallback lookup без региональной таблицы.

## Критерии приёмки

- Unit tests покрывают `Translation` resource, context-aware messages, locale normalization, loaded locales и predictable fallback.
- Integration tests показывают, что `Label` меняет submitted text после `TranslationServer.SetLocale()` на следующем processed frame.
- Public API compatibility table включает новые public types.
- Текущая documentation описывает implemented behavior и ограничения.
