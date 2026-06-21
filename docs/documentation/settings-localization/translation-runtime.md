# Translation resource, locale switching и `Tr`

Текущий runtime добавляет минимальную модель локализации для `0.1.0 Preview`:

- `Translation` - resource с сообщениями для одной локали.
- `TranslationServer` - общий process-wide registry переводов и текущей локали.
- `Object.Tr(string message, string context = "")` - удобный перевод из любого object/node.
- `Label` рисует `Tr(Text)` и обновляет cached draw commands после смены locale или набора переводов.

## `Translation`

`Translation.Locale` хранит нормализованное имя локали. Дефис заменяется на подчёркивание, language part приводится к lowercase, region part - к uppercase. Например, `pt-BR` становится `pt_BR`.

`AddMessage(srcMessage, xlatedMessage, context)` добавляет или заменяет перевод. Один и тот же source key может иметь разные переводы для разных context values. `GetMessage()` возвращает перевод или пустую строку, если key/context в этом resource отсутствует. `EraseMessage()` удаляет запись без ошибки, если её нет. `GetMessageList()` возвращает отсортированный список уникальных source keys.

## `TranslationServer`

При первом обращении сервер определяет начальную локаль из `CultureInfo.CurrentUICulture`. Пустое значение приводится к `en`.

`SetLocale()` задаёт текущую локаль вручную. `AddTranslation()` и `RemoveTranslation()` управляют загруженными ресурсами. `Clear()` очищает registry, но не сбрасывает текущую locale. `GetLoadedLocales()` возвращает отсортированный список локалей, для которых есть registered translations.

`Translate()` выполняет lookup в таком порядке:

1. точная текущая локаль, например `fr_CA`;
2. базовый язык текущей локали, например `fr`;
3. fallback locale `en`;
4. исходный key.

Такой fallback делает отсутствующие ключи предсказуемыми: UI показывает source key, а не пустую строку.

## `Label`

`Label.Text` остаётся source key. Во время `_Draw()` label вызывает `Tr(Text)` и отправляет в canvas уже переведённый текст. `Label._Process()` отслеживает внутреннюю версию translation state и вызывает `QueueRedraw()`, когда locale или registry переводов изменились. Поэтому видимый text обновляется на следующем processed frame без ручного redraw.

## Ограничения

- File import переводов не реализован.
- Сохранение выбранной locale в project/user settings относится к следующей задаче.
- Plural forms, pseudolocalization и extraction tooling не реализованы.
- Locale matching ограничен exact/base-language/`en` fallback.

## Проверки

```powershell
dotnet test tests\Electron2D.Tests.Unit\Electron2D.Tests.Unit.csproj --filter "FullyQualifiedName~TranslationTests" --no-restore -m:1
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~LabelSubmitsTranslatedTextAfterLocaleChange" --no-restore -m:1
```
