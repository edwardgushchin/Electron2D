# Box2D.NET platform/AOT validation

## Цель

`Box2D.NET` рассматривается только как candidate backend для будущего `PhysicsServer2D`, а не как уже выбранная production-зависимость runtime. До реализации тел, форм, queries и `CharacterBody2D` нужно иметь отдельный validation gate, который подтверждает, что candidate можно собрать и выполнить в режимах, важных для `0.1.0 Preview`.

Основание: архитектурная спецификация выбирает чистую C# реализацию `ikpil/Box2D.NET` как предпочтительный candidate, но требует собственной проверки Electron2D по desktop JIT/NativeAOT, mobile Release/AOT gaps и allocations per physics tick. Текущий публичный источник пакета: [NuGet `Box2D.NET` 3.1.654](https://packages.nuget.org/packages/Box2D.NET), upstream: [ikpil/Box2D.NET](https://github.com/ikpil/Box2D.NET).

## Контракт validation gate

В репозитории должен быть отдельный smoke-проект:

```text
tests/Electron2D.Tests.PhysicsBox2DSmoke/Electron2D.Tests.PhysicsBox2DSmoke.csproj
```

Он должен:

- ссылаться на NuGet package `Box2D.NET` версии `3.1.654`;
- не быть зависимостью `src/Electron2D/Electron2D.csproj`;
- создавать реальный `B2World`, static ground body, dynamic body и circle shape;
- выполнить warmup ticks, затем measured ticks через `B2Worlds.b2World_Step`;
- проверить, что dynamic body движется вниз под gravity;
- измерить `GC.GetAllocatedBytesForCurrentThread()` вокруг measured physics ticks;
- вывести stable text lines с `AllocatedBytes`, `AllocatedBytesPerTick`, `Ticks`, `WarmupTicks`, `PackageVersion` и final body position;
- завершиться non-zero exit code при провале smoke.

Обязательный verifier:

```text
tools/Verify-Box2DPhysicsCandidate.ps1
```

Он должен:

- запускать smoke-проект в Release/JIT на текущей машине;
- по опции `-NativeAot` публиковать smoke-проект с `PublishAot=true` под текущий или явно заданный RuntimeIdentifier и запускать published executable;
- проверять, что спецификация и документация перечисляют desktop matrix, mobile gaps и allocation measurement;
- не требовать Android/iOS toolchain на developer workstation;
- писать понятный итоговый статус.

CI на desktop matrix должен запускать verifier с `-NativeAot`, чтобы Windows, Linux и macOS подтверждали JIT + NativeAOT как часть обычной проверки.

## Платформенная матрица

Обязательная desktop validation:

| Платформа | JIT | NativeAOT | Источник доказательства |
| --- | --- | --- | --- |
| Windows x64 | required | required | GitHub Actions `windows-latest` + local Windows run |
| Linux x64 | required | required | GitHub Actions `ubuntu-latest` |
| macOS arm64/x64 runner | required | required | GitHub Actions `macos-latest` |

Mobile gaps для `0.1.0 Preview`:

| Платформа | Статус |
| --- | --- |
| Android arm64 Release/AOT | gap до задач mobile export/toolchain |
| iOS arm64 Release/AOT | gap до задач mobile export/toolchain |

Mobile gaps должны быть явно указаны в документации и release notes: это не считается пройденной mobile validation.

## Не входит в задачу

- production `Box2DPhysicsServer2D`;
- добавление `Box2D.NET` в runtime package graph;
- upstream Box2D test suite;
- 10-50 тысяч contacts stress;
- callback stress tests;
- deferred body deletion during step;
- Android/iOS device или simulator run;
- публичные `Box2D` handles.

## Критерии приёмки

- Infrastructure test подтверждает наличие smoke-проекта, verifier, pinned package и документации.
- Release/JIT smoke проходит на текущей машине.
- NativeAOT smoke проходит на текущей машине или blocker фиксируется как platform/toolchain issue.
- `Verify-Box2DPhysicsCandidate.ps1` проверяет documentation fragments и умеет запускать `-NativeAot`.
- Документация реализации содержит текущий local result, desktop CI intent, mobile gaps и allocation measurement.
- Public API verifier по-прежнему не видит публичных `Box2D` типов.
- Source license verifier проходит для новых C# и PowerShell файлов.
