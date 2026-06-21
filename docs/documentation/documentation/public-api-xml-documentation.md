# XML documentation публичного API

Текущая реализация вводит проверяемое правило XML-документации для публичного API Electron2D.

## Правило

Каждый public type/member runtime assembly должен иметь C# XML documentation. Для полноты используются:

- `<summary>`;
- `<remarks>`;
- `<param name="...">`;
- `<typeparam name="...">`;
- `<returns>`;
- `<value>`;
- `<exception cref="...">`;
- `<threadsafety>`;
- `<since>`;
- `<seealso cref="..."/>`.

Применимость зависит от member kind: например, у `void` method не требуется `<returns>`, а у method без параметров не требуется `<param>`.

## Verifier

Команда report mode:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-PublicApiXmlDocs.ps1
```

Команда strict fail mode:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-PublicApiXmlDocs.ps1 -FailOnIssues
```

Report mode нужен для безопасного доменного прохода по текущему API. Strict mode должен стать CI gate после заполнения текущих gaps.

## Текущий статус

Аудит от 2026-06-21 показал `366` warnings `CS1591` при генерации XML documentation file. Дополнительный verifier checked `1252` public symbols и нашёл `1013` XML documentation issues.

CI запускает `tools\Verify-PublicApiXmlDocs.ps1` только в report mode. Strict mode пока не подключается к CI: он будет падать до заполнения документации.

Текущая ближайшая работа по `T-0106`:

1. пройти public API по доменам;
2. заполнить XML documentation comments;
3. запустить verifier в strict mode;
4. подключить strict mode к CI;
5. закрыть `T-0106` только после зелёного strict gate.
