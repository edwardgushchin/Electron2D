# Machine-readable API manifest

Текущая реализация поставляет tracked JSON manifest публичного runtime API:

```text
data/api/electron2d-api-manifest.json
```

Manifest создаётся из compiled assembly, XML documentation и GitHub Wiki compatibility table. В этом документе compiled assembly означает собранный `.dll` файл runtime, а XML documentation — файл, который C# build создаёт из XML comments в исходниках. Manifest не редактируется вручную как источник правды: при изменении public API, XML comments или compatibility status его нужно пересоздать генератором.

## Команды

Обновить manifest из текущей сборки и локального Wiki clone:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Update-ApiManifest.ps1 -WikiPath .github/wiki
```

Проверить, что tracked manifest синхронизирован:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Update-ApiManifest.ps1 -WikiPath .github/wiki -Check
```

Записать manifest во временный файл вместо canonical artifact:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Update-ApiManifest.ps1 -WikiPath .github/wiki -OutputPath .temp/api-manifest/probe.json
```

`tools\Update-ApiWiki.ps1 -OutputPath .github/wiki -Check` также вызывает manifest check. Поэтому GitHub Wiki API reference gate теперь проверяет не только Markdown pages, но и JSON API manifest.

## Что содержит manifest

Текущий файл содержит:

- `schemaVersion = 1`;
- `manifestVersion = 0.1.0-preview`;
- `engineVersion` из runtime assembly;
- `profileName = Electron2D 0.1.0 2D`;
- `godotBaseline = 4.7-stable`;
- `generatedFrom` с путями к assembly, XML documentation и `API-Compatibility.md`;
- `strictParitySummary` с нулевыми счётчиками для supported profile boundary;
- `statusSummary` с количеством public types по compatibility status;
- `supportedVariantTypes`;
- `types` с stable identifiers, inheritance, category, summary, profile status и members.

Stable identifiers имеют формы:

```text
electron2d://api/type/Electron2D.Node
electron2d://api/member/Electron2D.Node/Property/Name
electron2d://api/member/Electron2D.Node/Method/AddChild(...)
```

Эти identifiers предназначены для будущего `Editor Capability Manifest`, Inspector properties, signals и runtime operations. Они позволяют внешнему tooling ссылаться на public API без привязки к имени файла документации.

## Compatibility status

Status берётся из GitHub Wiki `API-Compatibility.md`:

- `Supported` становится `supported` и `parity_verified`;
- `Partial` становится `partial` и `not_verified`;
- `Experimental` становится `experimental` и `not_verified`;
- `Planned` становится `planned` и `not_verified`.

Только `supported` entries считаются частью supported/parity-verified profile. `partial`, `experimental` и `planned` помечаются `outOfProfile = true`, чтобы AI-агент мог fail-closed: не использовать такой API как подтверждённую часть строгого профиля.

## CI и Wiki

CI запускает manifest check после checkout GitHub Wiki clone:

```powershell
./tools/Update-ApiManifest.ps1 -WikiPath .github/wiki -Check
```

Затем CI запускает GitHub Wiki API reference check. Wiki generator читает manifest и добавляет на каждую generated type page блок:

```text
Godot 4.7 C# profile compatibility
Profile: Electron2D 0.1.0 2D
Status: Supported / Parity verified
Out of profile: no
```

Для `partial`, `experimental` и `planned` API тот же блок показывает непроверенный status и `Out of profile: yes`.

## CLI parity verifier

`e2d api compare-godot <type> --format json` читает tracked manifest и возвращает машинный результат проверки одного типа:

```powershell
e2d api compare-godot Control --format json
```

Команда использует общий CLI envelope:

- `command = api compare-godot`;
- `route = none`, потому что `ProjectWorkspace` не открывается;
- `data.mode = api.compareGodot`;
- `data.sourcePath = data/api/electron2d-api-manifest.json`;
- `data.type` содержит `fullName`, `id` и `profile`;
- `data.result.status` показывает `parity_verified` или `out_of_profile`;
- `data.strictParity` содержит шесть counters strict parity.

Для поддержанного profile type все strict parity counters равны `0`, `succeeded = true` и `exitCode = 0`. Для type вне утверждённого профиля команда возвращает `succeeded = false`, `exitCode = 1`, `data.result.status = out_of_profile` и diagnostic `E2D-CLI-0002`.
