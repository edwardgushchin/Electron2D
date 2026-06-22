# Machine-readable API manifest

Статус: целевая спецификация для `T-0118`.
Обновлено: 2026-06-22.

## Назначение

Electron2D должен поставлять версионированный JSON manifest публичного runtime API. Manifest нужен AI-агентам, CLI, Inspector, GitHub Wiki verifier-ам, генераторам и будущему language service, чтобы отличать реализованный и parity-verified профиль от API вне текущего профиля без чтения исходников движка.

В этом документе manifest означает машиночитаемый JSON-файл с описанием публичных типов и members. Он не является исходником реализации: файл пересоздаётся генератором из compiled assembly, XML documentation и GitHub Wiki compatibility table.

## Canonical artifact

Canonical tracked artifact:

```text
data/api/electron2d-api-manifest.json
```

Файл должен быть stable JSON: UTF-8 без BOM, LF line endings, отсортированные типы и members, deterministic property order. Любое изменение public API, XML documentation или compatibility status должно либо обновить этот файл, либо привести к падению проверки синхронизации.

## Источники данных

Generator обязан читать только проверяемые источники:

- compiled runtime assembly `src/Electron2D/bin/Debug/net10.0/Electron2D.dll`;
- XML documentation file, полученный при build из текущих C# XML comments;
- GitHub Wiki compatibility page `API-Compatibility.md` из локального clone `.github/wiki` или явно переданного пути.

Ручной список public API не допускается как основной источник manifest. Ручными данными могут быть только release-level metadata: `schemaVersion`, `manifestVersion`, `engineVersion`, `profileName` и `godotBaseline`.

## Schema shape

Manifest должен содержать:

- `schemaVersion` со значением `1`;
- `manifestVersion` со значением `0.1.0-preview`;
- `engineVersion`;
- `profileName` со значением `Electron2D 0.1.0 2D`;
- `godotBaseline` со значением `4.7-stable`;
- `generatedFrom` с путями к assembly, XML documentation и compatibility page;
- `strictParitySummary` с числовыми полями `missingTypes`, `missingMembers`, `signatureMismatches`, `inheritanceMismatches`, `defaultMismatches`, `unexpectedChanges`;
- `statusSummary` с количеством типов по статусам `supported`, `partial`, `experimental`, `planned`;
- `types`, отсортированный по `fullName`.

Каждый type entry должен содержать:

- stable `id` в форме `electron2d://api/type/{FullName}`;
- `fullName`, `namespace`, `name`, `kind`;
- `baseType` и `interfaces`;
- `xmlDocId`;
- `summary`;
- `category`;
- `profile` с `name`, `status`, `parity`, `outOfProfile`, `godotReference`, `notes`;
- `members`, отсортированный по stable member id.

Каждый member entry должен содержать:

- stable `id` в форме `electron2d://api/member/{DeclaringType}/{Kind}/{SignatureKey}`;
- `declaringType`, `name`, `kind`, `signature`, `returnType`;
- `parameters`;
- `xmlDocId`;
- `summary`;
- `profile` с тем же status/parity contract, что у declaring type, если member не имеет отдельного compatibility override.

## Статусы и parity

Compatibility status берётся из `API-Compatibility.md` и нормализуется в lowercase:

- `Supported` -> `supported`;
- `Partial` -> `partial`;
- `Experimental` -> `experimental`;
- `Planned` -> `planned`.

Для public types со статусом `supported` поле `profile.parity` должно быть `parity_verified`, а `outOfProfile` должно быть `false`.

Для `partial`, `experimental` и `planned` поле `outOfProfile` должно быть `true`, пока тип не входит в закрытый supported/parity-verified профиль. Это позволяет AI-агенту использовать только поддержанный профиль и видеть, почему остальная public surface недоступна как строгий profile contract.

Для future strict parity verifier-а `e2d api compare-godot <type>` manifest должен хранить нулевой `strictParitySummary` для supported profile и stable per-type parity fields. CLI adapter реализуется отдельной задачей и не является зависимостью generator-а.

## Generator и verifier

`tools/Update-ApiManifest.ps1` должен поддерживать:

- обычный режим: пересоздаёт `data/api/electron2d-api-manifest.json`;
- `-OutputPath <path>`: пишет manifest в заданный файл;
- `-Check`: генерирует expected manifest во временный каталог и сравнивает с target file;
- `-WikiPath <path>`: читает compatibility table из указанного Wiki clone или файла.

`-Check` должен завершаться ошибкой, если:

- manifest отсутствует;
- manifest устарел относительно public API, XML docs или compatibility page;
- любой public type из compiled assembly отсутствует в manifest;
- любой public type отсутствует в `API-Compatibility.md`;
- mandatory stable ids отсутствуют у types, properties, methods, constructors, fields, events или enum values.

`tools/Update-ApiWiki.ps1 -Check` должен использовать manifest verifier или вызывать его как отдельный шаг, чтобы GitHub Wiki/API reference gate проверял один и тот же public API contract.

CI должен запускать manifest check после checkout GitHub Wiki clone и до consolidated public API documentation audit.

## Критерии приёмки

- `data/api/electron2d-api-manifest.json` генерируется из compiled public surface, XML documentation и `API-Compatibility.md`.
- Manifest содержит stable identifiers для types и members, пригодные для Inspector, signals, runtime-операций и будущего Editor Capability Manifest.
- Manifest содержит machine-readable Godot `4.7-stable` C# parity fields для future `e2d api compare-godot <type>`.
- GitHub Wiki/API verifier или CI падает, если manifest не синхронизирован с public API.
- AI-агент может по manifest отличить `supported` + `parity_verified` профиль от `partial`, `experimental` и `planned` API без чтения исходников.
