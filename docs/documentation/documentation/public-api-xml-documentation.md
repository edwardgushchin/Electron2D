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

Команда полного audit gate:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-PublicApiDocumentationAudit.ps1 -WikiPath .github/wiki
```

Report mode нужен для безопасного доменного прохода по текущему API. Strict mode является CI gate после заполнения текущих gaps. Полный audit gate дополнительно сверяет GitHub Wiki API reference и public documentation Markdown, который входит в documentation pipeline.

## Что проверяется

`tools\Verify-PublicApiXmlDocs.ps1 -FailOnIssues` собирает runtime assembly, читает XML documentation file и проверяет `1258` public symbols. Проверка падает, если:

- отсутствует XML documentation у public symbol;
- отсутствуют обязательные `<summary>`, `<param>`, `<typeparam>`, `<returns>`, `<value>`, `<threadsafety>` или `<since>` по применимости member kind;
- multi-sentence `<summary>` не разбит на `<para>`;
- `<seealso>` не содержит `cref` или `href`;
- `<exception>` не содержит `cref` или текста;
- documentation text содержит placeholder или запрещённую публичную формулировку;
- generated XML output содержит bare `<inheritdoc />`.

`tools\Verify-PublicApiDocumentationAudit.ps1` запускает этот strict verifier, сверяет `.github/wiki` через `tools\Update-ApiWiki.ps1 -OutputPath .github/wiki -Check` и сканирует Markdown из `docs/specifications/documentation/`, `docs/documentation/documentation/` и `.github/wiki`.

## Текущий статус

Аудит от 2026-06-21 показал `366` warnings `CS1591` при генерации XML documentation file. После заполнения структурной документации strict verifier проверяет `1258` public symbols и показывает `0` XML documentation issues. Полный audit gate дополнительно проверяет `143` Markdown-файла public API documentation pipeline: локальную документацию, спецификации documentation-domain и локальный clone GitHub Wiki.

CI запускает `tools\Verify-PublicApiXmlDocs.ps1 -FailOnIssues` и `tools\Verify-PublicApiDocumentationAudit.ps1 -WikiPath .github/wiki`. Это означает, что новый недокументированный public API, неполные обязательные XML-теги, несинхронизированная Wiki reference или запрещённые публичные формулировки ломают CI.

Для нового public API порядок работы такой:

1. пройти public API по доменам;
2. заполнить XML documentation comments;
3. запустить verifier в strict mode;
4. обновить `.github/wiki` через `tools\Update-ApiWiki.ps1 -OutputPath .github/wiki`;
5. запустить полный audit gate;
6. поддерживать оба verifier-а как обязательный локальный и CI gate.
