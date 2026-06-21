# Архитектура и платформенный стек Electron2D

Статус: целевая спецификация.
Источник: перенесено из корневого `GOAL.md`.
Последнее обновление: 2026-06-20.
Связанные документы: [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md).

Electron2D - кроссплатформенный игровой 2D движок. Представляет из себя .NET библиотеку и инструменты разработки.

В основе движка лежит библиотека SDL3 и мой собственный враппер к ней SDL3-CS https://github.com/edwardgushchin/SDL3-CS

В качестве рендера используется SDL_GPU. Для шейдеров используем SDL_shadercross, но так как SDL отдельно отмечает ограниченную совместимость SDL_GPU с Android-устройствами из-за различий в Vulkan-драйверах и наборах возможностей, поэтому для Андройд в этой ветке делаем какой-то фолбэк.

Поддерживаемые платформы: Windows, Linux, macOS, Android, iOS

Архитектура: Node-like (все есть Noda)
Публичный API: копия Godot. В движке представлены такие направления как:
1. https://docs.godotengine.org/en/stable/classes/index.html#nodes
2. https://docs.godotengine.org/en/stable/classes/index.html#resources (кроме OpenXR)
3. https://docs.godotengine.org/en/stable/classes/index.html#variant-types
4. https://docs.godotengine.org/en/stable/classes/index.html#other-objects (только нужные)
5. https://docs.godotengine.org/en/stable/classes/index.html#editor-only (только нужные)

Движок полностью задокументирован и имеет собственную Wiki на GitHub

Редактор Electron2D.Editor написан на самом Electron2D.

С этим уточнением предыдущая рекомендация **`SDL3 + bgfx + miniaudio + PhysicsFS` больше не проходит ваш фильтр**. Например, SharpBgfx архивирован и фактически не даёт поддерживаемого мобильного стека, Veldrid давно не развивается как публичный проект, а актуальный .NET-движок поверх miniaudio — SoundFlow — с января 2026 года переведён автором в режим длительной паузы. ([GitHub][1])

Под «нормальным wrapper» для Electron2D я бы понимал не просто набор `[DllImport]`, а пакет, который одновременно:

* синхронизирован с конкретной версией native-библиотеки;
* содержит native binaries для всех нужных RID/ABI;
* поддерживает Android и статическую линковку либо XCFramework для iOS;
* совместим с trimming и NativeAOT;
* имеет тесты, примеры и активного сопровождающего;
* не заставляет пользователя самостоятельно собирать C/C++-часть.

По этому критерию ваш SDL3-CS уже закрывает непропорционально большую часть движка: SDL3, SDL_image, SDL_ttf, SDL_mixer и SDL_shadercross, включая Windows, Linux, macOS, Android и iOS. ([GitHub][2])

## Рекомендуемый стек Electron2D

| Подсистема Electron2D                  | Реализация                                                | Решение                            |
| -------------------------------------- | --------------------------------------------------------- | ---------------------------------- |
| `DisplayServer`, окно, ввод, lifecycle | SDL3 через SDL3-CS                                        | Оставить                           |
| `RenderingServer`                      | SDL_GPU через SDL3-CS                                     | Основной backend                   |
| Android compatibility renderer         | Сначала SDL_GPU compatibility profile, затем SDL_Renderer | Оставить внутри SDL3-CS            |
| Компиляция шейдеров                    | SDL_shadercross через SDL3-CS                             | Оставить                           |
| `AudioServer`                          | SDL_mixer 3 через SDL3-CS                                 | Оставить                           |
| `TextServer`                           | SDL_ttf 3 через SDL3-CS                                   | Оставить                           |
| Загрузка изображений                   | SDL_image 3 через SDL3-CS                                 | Оставить                           |
| `PhysicsServer2D`                      | Box2D.NET, чистый C#                                      | Кандидат после AOT/mobile-тестов   |
| Полигональные операции                 | Clipper2, чистый C#                                       | Использовать без triangulation API |
| Триангуляция                           | собственная либо Cutear                                   | Пилотировать                       |
| Multiplayer transport                  | `System.Net` + LiteNetLib                                 | Использовать                       |
| Сцены, ресурсы, сериализация           | собственная managed-реализация                            | Не отдавать сторонней библиотеке   |
| Navigation 2D                          | собственный сервер поверх geometry primitives             | Реализовывать в C#                 |

## Android fallback для SDL_GPU

Здесь я бы не начинал сразу с отдельного OpenGL ES backend.

SDL рекомендует для широкой совместимости Android отключать необязательные возможности Vulkan при создании GPU device:

* clip distance;
* depth clamping;
* indirect draw `firstInstance`;
* anisotropic filtering.

Все эти возможности управляются properties при `SDL_CreateGPUDeviceWithProperties`. SDL прямо указывает, что отключение optional Vulkan features расширяет поддержку старых и проблемных Android-устройств. ([SDL3][3])

Получается трёхуровневая схема.

### Уровень 1: SDL_GPU standard

Для:

* Windows;
* Linux;
* macOS;
* iOS;
* Android-устройств с нормальным Vulkan implementation.

Полный набор Electron2D:

* пользовательские шейдеры;
* render passes;
* render targets;
* post-processing;
* GPU particles;
* advanced blending;
* batching;
* instancing, где доступно.

### Уровень 2: SDL_GPU mobile compatibility

На Android устройство создаётся сразу с консервативным профилем:

```text
clip_distance                = false
depth_clamping               = false
indirect_draw_first_instance = false
anisotropy                   = false
```

После создания выполняется небольшой smoke test:

1. создание swapchain;
2. создание RGBA8 texture и render target;
3. загрузка простого vertex/fragment shader;
4. создание pipeline;
5. отрисовка треугольника;
6. копирование либо presentation результата.

Проверять только успешный возврат `SDL_CreateGPUDevice` недостаточно: часть дефектных драйверов проявляется при создании pipeline или первой отправке command buffer.

Возможности движка при этом не обязаны урезаться концептуально. Необязательные GPU-функции можно эмулировать или отключать через feature flags:

```csharp
RenderingDeviceFeatures.AnisotropicFiltering
RenderingDeviceFeatures.IndirectFirstInstance
RenderingDeviceFeatures.DepthClamp
RenderingDeviceFeatures.ClipDistance
```

### Уровень 3: SDL_Renderer compatibility backend

Если SDL_GPU не создаётся либо не проходит smoke test, активируется backend поверх `SDL_Renderer`.

Он сможет поддержать основную 2D-функциональность:

* `Sprite2D`;
* `AnimatedSprite2D`;
* `TileMap`;
* `NinePatchRect`;
* `Polygon2D`;
* линии и примитивы;
* clipping/scissor;
* canvas transforms;
* render textures;
* стандартные blend modes;
* текст;
* UI и весь редактор.

Но необходимо честно зафиксировать ограничения:

* произвольные `Shader` и `ShaderMaterial` недоступны;
* нет compute;
* ограниченный post-processing;
* часть CanvasItem-эффектов придётся эмулировать;
* сложные particles и lighting могут работать в упрощённом режиме;
* нельзя гарантировать визуальную идентичность SDL_GPU backend.

В настройках проекта это можно представить в согласованном виде:

```text
rendering/renderer/rendering_method:
    mobile
    compatibility

rendering/renderer/fallback_policy:
    automatic
    fail_if_unavailable
```

Где:

* `mobile` — SDL_GPU с мобильным feature profile;
* `compatibility` — SDL_Renderer;
* `automatic` — попытка SDL_GPU, затем fallback;
* `fail_if_unavailable` — полезно для игр, которые требуют custom shaders.

## Нужен ли отдельный OpenGL ES backend

Только если требуется **сохранить пользовательские шейдеры на Android без Vulkan**.

Наиболее реалистичный кандидат при вашем условии — `Silk.NET.OpenGLES`. Silk.NET остаётся активным проектом .NET Foundation, имеет сгенерированные OpenGL ES bindings и свежие релизы. Но ветка 2.x сейчас получает ограниченное сопровождение, пока развивается 3.0, а полноценную Android/iOS/NativeAOT-гарантию в качестве готового game-engine backend проект не предоставляет. Поэтому это не готовая зависимость, а кандидат на отдельный технический spike. ([GitHub][4])

Схема могла бы быть такой:

```text
SDL3-CS
├── SDL_CreateWindow(... SDL_WINDOW_OPENGL)
├── SDL_GL_CreateContext
├── SDL_GL_GetProcAddress
└── Silk.NET.OpenGLES
```

Перед принятием потребуется проверить:

* Android ARM64 Release;
* NativeAOT/trimming;
* отсутствие runtime code generation;
* загрузку функций через SDL;
* context loss при сворачивании приложения;
* восстановление GPU-ресурсов;
* shader compilation на Adreno, Mali и PowerVR;
* отсутствие конфликта с SDL-managed lifecycle.

До прохождения этого набора тестов я бы не добавлял Silk.NET в основной dependency graph.

Sokol.NET формально выглядит привлекательнее: заявляет Android, iOS, NativeAOT, OpenGL ES, Metal и D3D11. Но это небольшой проект, развитие которого напрямую зависит от внутренних проектов одного сопровождающего. Кроме того, он приносит собственный application layer, который Electron2D не нужен. Его можно рассматривать только как экспериментальный renderer backend, не как основу движка. ([GitHub][5])

## Физика: не native Box2D wrapper, а чистый C#

При вашем правиле существующие native Box2D wrappers я бы не принимал:

* один предоставляет главным образом сгенерированные низкоуровневые bindings без полноценного high-level wrapper;
* другой представляет собой тонкую P/Invoke-обвязку;
* ни один из рассмотренных вариантов не демонстрирует законченный пакет native-библиотек для Android и iOS уровня SDL3-CS. ([GitHub][6])

Предпочтительнее `ikpil/Box2D.NET`:

* чистая C#-реализация;
* повторяет современный API Box2D;
* не требует native packaging;
* активно обновляется;
* распространяется под MIT. ([GitHub][7])

Однако я бы присвоил ему статус **candidate**, а не автоматически включал в production. Его публично заявленная матрица не даёт достаточной гарантии Android/iOS/NativeAOT, поэтому Electron2D должен сам подтвердить:

```text
Windows x64        JIT + NativeAOT
Linux x64          JIT + NativeAOT
macOS arm64        JIT + NativeAOT
Android arm64      Release/AOT
iOS arm64          Release/AOT
```

Кроме платформенного smoke test нужны:

* полный upstream Box2D test suite;
* callback stress tests;
* 10–50 тысяч contacts;
* создание и удаление тел во время шага через deferred queue;
* trimming;
* отсутствие reflection-зависимостей;
* измерение allocations per physics tick.

### Изоляция физики

Публичный API не должен содержать типов Box2D:

```text
RigidBody2D
StaticBody2D
CharacterBody2D
Area2D
CollisionShape2D
PhysicsMaterial
PhysicsDirectSpaceState2D
```

Они должны обращаться к:

```csharp
internal interface IPhysicsServer2DBackend
{
    Rid BodyCreate();
    void BodySetTransform(Rid body, Transform2D transform);
    void BodySetVelocity(Rid body, Vector2 velocity);
    void SpaceStep(double delta);
}
```

Box2D objects хранятся только внутри `Box2DPhysicsServer2D`.

Отдельно: `CharacterBody2D` не стоит реализовывать как обычное динамическое Box2D-тело. Для согласованного поведения нужен собственный kinematic solver:

* `MoveAndSlide`;
* `MoveAndCollide`;
* floor detection;
* wall/ceiling classification;
* floor snapping;
* safe margin;
* platform velocity propagation.

Box2D при этом используется для broadphase, shape casts, contacts и остальных физических тел.

## Текст и шрифты

Отдельные FreeType/HarfBuzz wrappers не нужны. SDL_ttf 3 уже предоставляет:

* shaping;
* направление текста;
* script и language;
* fallback fonts;
* SDF;
* text engines для SDL_Renderer и SDL_GPU.

Это хорошо соответствует будущему `TextServer`, а backend можно переключать вместе с renderer. ([SDL3][8])

Архитектура:

```text
FontFile Resource
FontVariation Resource
Label Node
RichTextLabel Node
        ↓
TextServer
        ↓
SDL_ttf
        ├── GPUTextEngine
        └── RendererTextEngine
```

Glyph layout и кэширование стоит контролировать на уровне `TextServer`, чтобы Node API не зависел от SDL_ttf.

## Аудио

SDL_mixer 3 уже достаточен как низкоуровневый backend для:

* декодирования;
* streaming;
* tracks;
* looping;
* fades;
* gain;
* panning;
* playback frequency;
* effects и post-mix callbacks.

Поверх него Electron2D должен самостоятельно реализовать согласованную модель:

```text
AudioStreamPlayer
AudioStreamPlayer2D
AudioBus
AudioEffect
AudioServer
```

Не следует отображать SDL_mixer track непосредственно в публичный API. `AudioServer` должен хранить собственные voice handles и маршрутизацию bus graph. Также нужно фиксировать возможности по фактически обёрнутой версии SDL_mixer, поскольку документация более новых development-релизов может содержать функции, которых ещё нет в вашем текущем SDL3-CS package. ([SDL3][9])

## Geometry и Navigation 2D

Для полигональных операций подходит официальный C#-вариант Clipper2:

* union;
* intersection;
* difference;
* XOR;
* polygon offsetting.

Но текущий репозиторий содержит прямое предупреждение автора о проблемах в недавно добавленной triangulation implementation. Поэтому использовать Clipper2 для boolean/offset операций можно, а его triangulation API пока нельзя делать production-зависимостью. ([GitHub][10])

Для триангуляции:

1. простой собственный ear clipping для простых полигонов;
2. отдельно протестированный tessellator для отверстий и self-intersections;
3. Cutear как возможный pure-C# кандидат после fuzz testing.

Cutear свежий и позиционируется как высокопроизводительный C# port Mapbox Earcut, но пока слишком молод, чтобы принимать его без собственного корпуса геометрических тестов. ([GitHub][11])

На этом можно построить:

```text
NavigationServer2D
├── NavigationRegion2D
├── NavigationPolygon
├── NavigationAgent2D
├── NavigationObstacle2D
├── polygon boolean operations
├── triangulation
├── adjacency graph
└── A* / funnel algorithm
```

Для A*, grid navigation и funnel algorithm native library вообще не требуется.

## Сеть

Базовые согласованные классы рациональнее реализовать на BCL:

```text
HTTPClient       → HttpClient
HTTPRequest      → HttpClient
WebSocketPeer    → ClientWebSocket
TCPServer        → Socket / TcpListener
StreamPeerTCP    → Socket / NetworkStream
PacketPeerUDP    → Socket
```

Для игрового UDP transport подходит LiteNetLib:

* чистый managed code;
* reliable и unreliable channels;
* sequencing;
* fragmentation;
* NAT punch;
* IPv6;
* заявленная работа в Android/iOS-средах;
* активные релизы в 2026 году. ([GitHub][12])

Его можно использовать под `MultiplayerPeer`, но не стоит называть реализацию `ENetMultiplayerPeer`, поскольку LiteNetLib не является ENet wire-compatible протоколом.

## Ключевая архитектурная граница

Поскольку публичный API повторяет Godot, внутреннюю архитектуру также стоит строить вокруг server abstractions:

```text
Nodes / Resources
        ↓
RenderingServer
PhysicsServer2D
AudioServer
TextServer
NavigationServer2D
DisplayServer
Input
ResourceLoader
ResourceSaver
        ↓
Backend implementations
```

Например:

```text
RenderingServer
├── SDLGpuRenderingBackend
├── SDLRendererCompatibilityBackend
└── GLES3RenderingBackend       // опционально позднее
```

Node не должен знать, какой backend активен:

```csharp
public sealed class Sprite2D : Node2D
{
    public Texture2D? Texture { get; set; }

    internal override void SubmitCanvasItem(RenderingServer server)
    {
        server.CanvasItemSetTexture(RenderingRid, Texture?.Rid ?? default);
        server.CanvasItemSetTransform(RenderingRid, GlobalTransform);
    }
}
```

Это особенно важно для Electron2D.Editor: поскольку редактор работает на самом движке, compatibility backend должен уметь гарантированно отображать весь редактор, даже когда конкретные игровые эффекты проекта недоступны.

## Итог

Для Electron2D я бы зафиксировал такой production baseline:

```text
SDL3-CS
├── SDL3             platform, input, lifecycle
├── SDL_GPU          primary rendering
├── SDL_Renderer     Android compatibility rendering
├── SDL_shadercross  shader compilation
├── SDL_image        image decoding
├── SDL_ttf          text backend
└── SDL_mixer        audio backend

Pure managed
├── Box2D.NET        physics candidate
├── Clipper2         polygon operations
├── own/Cutear       triangulation
├── LiteNetLib       realtime networking
└── System.*         HTTP, WebSocket, serialization, compression
```

**Отдельный OpenGL ES backend стоит вводить только тогда, когда выяснится, что SDL_Renderer fallback слишком сильно ограничивает ShaderMaterial и визуальную совместимость.** До этого SDL3-CS плюс managed-библиотеки дают наиболее контролируемый стек без заброшенных wrappers и без собственного сопровождения чужого C/C++ interop.

[1]: https://github.com/MikePopoloski/SharpBgfx "GitHub - MikePopoloski/SharpBgfx: C# bindings for the bgfx graphics library · GitHub"
[2]: https://github.com/edwardgushchin/SDL3-CS "GitHub - edwardgushchin/SDL3-CS: This is SDL3#, a C# wrapper for SDL3. · GitHub"
[3]: https://wiki.libsdl.org/SDL3/FAQDevelopment?utm_source=chatgpt.com "SDL3/FAQDevelopment"
[4]: https://github.com/dotnet/Silk.NET "GitHub - dotnet/Silk.NET: The high-speed OpenGL, OpenCL, OpenAL, OpenXR, GLFW, SDL, Vulkan, Assimp, WebGPU, and DirectX bindings library your mother warned you about. · GitHub"
[5]: https://github.com/elix22/Sokol.NET "GitHub - elix22/Sokol.NET: Cross-platform graphics framework for C# with .NET NativeAOT | Desktop • Mobile • Web | Direct3D • Metal • OpenGL • WebGL · GitHub"
[6]: https://github.com/BeanCheeseBurrito/Box2D.NET/ "GitHub - BeanCheeseBurrito/Box2D.NET: Auto-generated C# bindings for Box2D 3.0 · GitHub"
[7]: https://github.com/ikpil/Box2D.NET "GitHub - ikpil/Box2D.NET: Box2D.NET - a C# port of Box2D, is a 2D physics engine for games, .NET, Unity3D, servers · GitHub"
[8]: https://wiki.libsdl.org/SDL3_ttf/QuickReference?utm_source=chatgpt.com "SDL3_ttf/QuickReference"
[9]: https://wiki.libsdl.org/SDL3_mixer/FrontPage "SDL3_mixer/FrontPage - SDL Wiki"
[10]: https://github.com/AngusJohnson/Clipper2 "GitHub - AngusJohnson/Clipper2: Polygon Clipping, Offsetting & Triangulation in  C++, C# and Delphi · GitHub"
[11]: https://github.com/oberbichler/Cutear "GitHub - oberbichler/Cutear: C# port of the Mapbox Earcut polygon triangulation library for .NET Standard 2.0/2.1 and .NET 10 · GitHub"
[12]: https://github.com/revenantx/litenetlib "GitHub - RevenantX/LiteNetLib: Lite reliable UDP library for Mono and .NET · GitHub"
