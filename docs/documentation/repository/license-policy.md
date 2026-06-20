# Политика MIT License и source headers

Electron2D распространяется по MIT License. Корневой файл `LICENSE` содержит полный текст лицензии, а `src/Electron2D/Electron2D.csproj` публикует NuGet metadata `PackageLicenseExpression=MIT`.

Текст лицензии сверяется с [SPDX MIT License](https://spdx.org/licenses/MIT). Так как у MIT License нет единого обязательного standard header, проект использует собственный полный header для C# и PowerShell source-файлов.

## Что проверяется

Проверка `tools/Verify-SourceLicenseHeaders.ps1` читает отслеживаемые Git файлы:

- `*.cs`;
- `*.ps1`.

Сгенерированные файлы и build output не входят в проверку, потому что они не являются вручную написанным исходным кодом проекта и находятся вне Git.

## Требование к файлам

Каждый проверяемый файл должен начинаться с MIT license header:

- для C# используется `/* ... */`;
- для PowerShell используется `<# ... #>`.

Header содержит название проекта, `MIT License`, copyright, `SPDX-License-Identifier: MIT` и полный текст MIT License. Такой формат выбран по аналогии с SDL: при копировании отдельного source-файла условия лицензии остаются внутри файла.

## Команды

Локальная проверка:

```powershell
powershell -ExecutionPolicy Bypass -File tools/Verify-SourceLicenseHeaders.ps1
```

CI запускает эту проверку отдельным шагом перед тестами.
