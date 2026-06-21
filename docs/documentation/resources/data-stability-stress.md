# Stress data stability для scene/resource pipeline

Статус: реализованный release-gate baseline.
Задача: `T-0042`.
Обновлено: 2026-06-21.

## Что реализовано

Добавлен stress gate для данных `0.1.0 Preview`. Он проверяет, что scene/resource serializers и import cache не теряют значения, ссылки и cache artifacts в сценариях, которые часто ломают проектные форматы.

Текущий набор проверок находится в `tests/Electron2D.Tests.Integration/DataStabilityStressTests.cs`.

## Проверяемые сценарии

### 100 save/load cycles

`SerializedResourceTextSerializer` и `SceneFileTextSerializer` проходят 100 последовательных циклов:

```text
serialize -> deserialize -> serialize
```

Финальный canonical JSON должен совпасть с исходным. Проверка включает arrays, dictionaries, enum, nullable и resource reference slots.

### Rename/move resources

Проверяется перенос `.e2res` source asset на новый `res://...` path с тем же UID:

- старый source path импортируется;
- файл переносится на новый path и сохраняет UID;
- новый path импортируется;
- старый path попадает в prune report;
- cache artifact с тем же UID не удаляется prune-этапом;
- manifest указывает на новый source path и прежний UID.

В рамках T-0042 исправлен defect import cache: prune старого source entry больше не удаляет cache file, если этот file уже удерживается новым manifest entry. Это важно для rename/move ресурсов с одинаковым UID.

### Import cache rebuild

Если cache root удалён, следующий import пересоздаёт manifest и artifacts из source files. UID остаётся прежним, а generated data не появляется внутри source root.

### Corruption diagnostics

Если source asset повреждён после успешного import:

- item получает status `Failed`;
- error message содержит диагностику malformed input;
- предыдущий валидный artifact остаётся на диске;
- manifest не теряет старую валидную запись.

## Текущие ограничения

- Это internal release gate, а не public loader/saver.
- Stress gate пока покрывает text documents и `.e2res` import cache; binary/audio/gpu asset stability добавляются в соответствующих importer/export задачах.
- Проверка corruption diagnostics сейчас ориентирована на malformed JSON `.e2res`.

## Проверки

Сфокусированная проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~DataStabilityStressTests" --no-restore -m:1
```

Полная проверка проекта:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1
```
