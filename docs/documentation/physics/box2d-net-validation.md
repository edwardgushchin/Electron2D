# Box2D.NET platform/AOT validation

`Box2D.NET 3.1.654` проверяется как candidate backend для будущего `PhysicsServer2D`. Это не production backend и не зависимость runtime-проекта `src/Electron2D/Electron2D.csproj`.

## Что проверяет smoke

Smoke-проект `tests/Electron2D.Tests.PhysicsBox2DSmoke`:

- ссылается на NuGet package `Box2D.NET 3.1.654`;
- создаёт `B2World` с gravity;
- создаёт static ground body;
- создаёт dynamic body с circle shape;
- выполняет warmup ticks;
- измеряет allocations per tick через `GC.GetAllocatedBytesForCurrentThread()`;
- выполняет measured ticks через `B2Worlds.b2World_Step`;
- проверяет, что dynamic body опускается вниз под gravity;
- выводит `AllocatedBytesPerTick`, `AllocatedBytes`, `Ticks`, `WarmupTicks`, `InitialY` и `FinalY`.

## Команда проверки

Release/JIT smoke:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-Box2DPhysicsCandidate.ps1
```

Release/JIT + NativeAOT smoke на текущем RuntimeIdentifier:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-Box2DPhysicsCandidate.ps1 -NativeAot
```

Для явной платформы:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-Box2DPhysicsCandidate.ps1 -NativeAot -RuntimeIdentifier win-x64
```

## Desktop matrix

GitHub Actions запускает `Verify-Box2DPhysicsCandidate.ps1 -NativeAot` на desktop matrix:

| Платформа | JIT | NativeAOT | Статус |
| --- | --- | --- | --- |
| Windows x64 | required | required | активный CI gate |
| Linux x64 | required | required | активный CI gate |
| macOS | required | required | активный CI gate |

Локальная проверка в текущей сессии выполняется на текущей developer machine. Остальные desktop платформы подтверждаются CI после push/PR.

## Mobile gaps

Android arm64 Release/AOT и iOS arm64 Release/AOT остаются mobile gap до задач export/toolchain. Эта проверка не утверждает, что Box2D.NET уже прошёл Android/iOS device или simulator run.

| Платформа | Статус |
| --- | --- |
| Android arm64 Release/AOT | gap до mobile export validation |
| iOS arm64 Release/AOT | gap до mobile export validation |

## Текущий смысл результата

Прохождение verifier означает:

- package `Box2D.NET 3.1.654` можно восстановить и выполнить в Release/JIT;
- при `-NativeAot` smoke можно опубликовать и выполнить как NativeAOT artifact для текущего RuntimeIdentifier;
- basic world/body/shape/step path работает;
- allocations per tick измерены и видны в output;
- public API Electron2D не раскрывает Box2D handles, потому что smoke-проект не является runtime dependency.

Полный upstream Box2D test suite, callback stress, 10-50 тысяч contacts и deferred deletion during physics step пока не входят в этот baseline.

## Локальный результат 2026-06-21

Текущая developer machine: `win-x64`.

Команда:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-Box2DPhysicsCandidate.ps1 -NativeAot
```

Результат:

| Режим | Ticks | WarmupTicks | AllocatedBytes | AllocatedBytesPerTick | InitialY | FinalY |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Release/JIT | 240 | 60 | 111512 | 464.633 | 4 | 0 |
| NativeAOT win-x64 | 240 | 60 | 85040 | 354.333 | 4 | 0 |

Оба режима создали `B2World`, static ground body, dynamic circle body, выполнили `B2Worlds.b2World_Step` и подтвердили движение dynamic body вниз под gravity.
