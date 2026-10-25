<p align="right"><a href="README.md">English</a> · Русский · <a href="README.zh-CN.md">简体中文</a> · <a href="README.es.md">Español</a> · <a href="README.pt-BR.md">Português (BR)</a></p>

<h1 align="center">
  <picture>
    <source media="(prefers-color-scheme: dark) and (max-width: 480px)" srcset="docs/design/assets/sprite/logo-compact-dark.svg">
    <source media="(prefers-color-scheme: light) and (max-width: 480px)" srcset="docs/design/assets/sprite/logo-compact-light.svg">
    <source media="(prefers-color-scheme: dark)" srcset="docs/design/assets/sprite/logo-primary-dark.svg">
    <source media="(prefers-color-scheme: light)" srcset="docs/design/assets/sprite/logo-primary-light.svg">
    <img alt="Electron2D - Agent-native cross-platform 2D game engine" src="docs/design/assets/sprite/logo-primary-light.svg" width="640" height="148">
  </picture>
</h1>

<p align="center">
  <a href="#installation"><img alt="Версия .NET для сборки проекта" src="https://img.shields.io/badge/dynamic/xml?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;url=https%3A%2F%2Fraw.githubusercontent.com%2Fedwardgushchin%2FElectron2D%2Fmain%2FElectron2D.csproj&amp;query=substring-after%28%2FProject%2FPropertyGroup%2FTargetFramework%5Bnot%28%40Condition%29%5D%5B1%5D%2C+%27net%27%29&amp;label=.NET&amp;suffix=+SDK&amp;color=brightgreen" height="28"></a>
  <a href="#license"><img alt="Лицензия движка" src="https://img.shields.io/badge/dynamic/regex?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;url=https%3A%2F%2Fraw.githubusercontent.com%2Fedwardgushchin%2FElectron2D%2Fmain%2Flicence%2FElectron2D-LICENSE.txt&amp;search=%5E%5Cs%2A%28%5CS%2B%29%5Cs%2BLicense&amp;replace=%241&amp;label=%D0%9B%D0%B8%D1%86%D0%B5%D0%BD%D0%B7%D0%B8%D1%8F&amp;color=brightgreen" height="28"></a>
  <a href="https://github.com/edwardgushchin/Electron2D/releases"><img alt="Последний опубликованный релиз" src="https://img.shields.io/badge/dynamic/xml?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;url=https%3A%2F%2Fgithub.com%2Fedwardgushchin%2FElectron2D%2Freleases.atom&amp;query=concat%28substring-after%28string%28%28%2F%2F%2A%5Blocal-name%28%29%3D%22entry%22%5D%5B1%5D%2F%2A%5Blocal-name%28%29%3D%22link%22%5D%2F%40href%29%5B1%5D%29%2C+%22%2Ftag%2F%22%29%2C+substring%28%22%D0%BD%D0%B5%D1%82+%D1%80%D0%B5%D0%BB%D0%B8%D0%B7%D0%BE%D0%B2%22%2C+1+div+not%28%2F%2F%2A%5Blocal-name%28%29%3D%22entry%22%5D%29%29%29&amp;label=%D0%A0%D0%B5%D0%BB%D0%B8%D0%B7&amp;color=brightgreen" height="28"></a>
</p>

<p align="center">
  <a href="https://github.com/edwardgushchin/Electron2D/commits/main"><img alt="Последний коммит в main" src="https://img.shields.io/github/last-commit/edwardgushchin/Electron2D/main?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;label=%D0%9F%D0%BE%D1%81%D0%BB%D0%B5%D0%B4%D0%BD%D0%B8%D0%B9+%D0%BA%D0%BE%D0%BC%D0%BC%D0%B8%D1%82&amp;display_timestamp=committer&amp;color=brightgreen" height="28"></a>
  <a href="https://github.com/edwardgushchin/Electron2D/actions/workflows/ci.yml"><img alt="Статус автоматической сборки" src="https://img.shields.io/github/check-runs/edwardgushchin/Electron2D/main?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;label=%D0%A1%D0%B1%D0%BE%D1%80%D0%BA%D0%B0&amp;nameFilter=Build" height="28"></a>
  <a href="https://github.com/edwardgushchin/Electron2D/actions/workflows/ci.yml"><img alt="Тесты (GitHub Actions)" src="https://img.shields.io/github/check-runs/edwardgushchin/Electron2D/main?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;label=%D0%A2%D0%B5%D1%81%D1%82%D1%8B&amp;nameFilter=Tests" height="28"></a>
</p>

<p align="center">
  <a href="#quick-start">Начать</a> ·
  <a href="#features">Возможности</a> ·
  <a href="#platforms">Платформы</a> ·
  <a href="#documentation">Документация</a> ·
  <a href="#feedback-and-contributing">Участие</a>
</p>

<p align="center">⭐ <a href="https://github.com/edwardgushchin/Electron2D">Поставьте нам звезду на GitHub</a> - это мотивирует!</p>

<a id="about"></a>

## <img src="docs/design/assets/sprite/readme-about.svg" width="24" height="28" align="absmiddle" alt=""> О проекте

Electron2D - **свободный кроссплатформенный 2D-движок на C# для совместной разработки игр человеком и ИИ-агентами**.

Создавайте игровые миры и механики с привычными инструментами .NET. В работе с кодом помогут Codex, Claude Code и другие ИИ-помощники.

<a id="features"></a>

## <img src="docs/design/assets/sprite/readme-features.svg" width="24" height="28" align="absmiddle" alt=""> Возможности

Это обзор основных возможностей, которые уже есть в движке. API отдельных подсистем ещё развивается; подробности и ограничения описаны по ссылкам.

- [Графика](docs/domains/rendering.md). Спрайты и атласы, камеры и параллакс, фигуры и текст, отсечение, внеэкранные области отрисовки, освещение, тени и HDR. Материалы HLSL/GLSL, визуальные графы шейдеров и CPU/GPU-частицы.
- [Сцены](docs/domains/scene.md). Иерархии узлов, переиспользуемые и связанные сцены, группы, события, отложенные вызовы и таймеры. Сцены можно создавать на C#, сохранять вместе с ресурсами и повторно загружать.
- [Анимация](docs/components/scene-animation.md). Покадровая анимация, треки свойств, кривые Безье, твины, графы смешивания и машины состояний. [Скелеты и IK](docs/components/skeletal-animation.md) управляют костями и деформацией полигонов и мешей с весами вершин.
- [Меши](docs/components/meshes.md) и [тайловые карты](docs/components/tiles.md). Индексированная и процедурная геометрия, массовое рисование экземпляров меша, анимированные атласы тайлов, соединение участков ландшафта, квадратные, изометрические и шестиугольные сетки со слоями столкновений и навигации.
- [Физика](docs/domains/physics.md). CPU- и независимый GPU-бэкенд, твёрдые тела и управляемые персонажи, области, силы, шарниры и пружины. Запросы лучами и формами, непрерывное обнаружение столкновений и [контрольные точки состояния](docs/components/physics-space-checkpoints.md).
- [Игровой интерфейс](docs/domains/scene.md) и [текст](docs/components/text.md). Кнопки, меню, списки, деревья, контейнеры размещения, прокрутка, редакторы текста и кода, форматированный текст. Темы, формирование Unicode-текста, двунаправленная вёрстка и резервные шрифты.
- [Звук](docs/domains/audio.md). WAV, MP3 и Ogg Vorbis, процедурные потоки, микрофон и позиционное воспроизведение. Звуковые шины, микширование, эффекты и запись.
- [Воспроизведение видео](docs/components/video-playback.md) и [запись роликов](docs/components/movie-writing.md). Видео Ogg Theora со звуком; запись отрисованных кадров и смешанного звука в PNG/WAV, AVI или OGV без привязки к реальному времени.
- [Управление](docs/domains/input.md). Клавиатура, мышь, касания, контроллеры, привязка игровых действий, сочетания клавиш и текстовый ввод с IME-композицией.
- [Окна и экраны](docs/domains/display.md). Оконный и полноэкранный режимы, несколько областей отрисовки, встроенные и нативные всплывающие окна, сведения об экранах, масштабирование и управление выводом кадров.
- [Навигация](docs/domains/navigation.md). A-star по графу точек и сетке, полигональные регионы, переходы между ними, построение навигации из геометрии, движение агентов по пути и взаимное избегание препятствий.
- [Ресурсы](docs/domains/resources.md). Типизированная загрузка и сохранение, фоновая загрузка, изображения, шрифты, аудио, градиенты, кривые и шумовые текстуры. [Пакеты PCK/ZIP](docs/components/resource-packs.md) подключают игровые файлы к файловой системе ресурсов.
- [Базовые сервисы и файлы](docs/domains/core.md). Настройки проекта, JSON/XML и конфигурационные файлы, работа с каталогами, сжатие, шифрование и ZIP-архивы. Математика, случайные числа, фоновые задачи и диагностика выполнения.
- [Локализация](docs/domains/localization.md). Домены и контексты переводов, каталоги PO/MO, резервные языки, целочисленные правила множественного числа, псевдолокализация и выбор локализованных ресурсов.
- [Сеть](docs/domains/networking.md). TCP/UDP и локальные сокеты, TLS/DTLS, HTTP/HTTPS, WebSocket и ENet. [Каналы данных WebRTC](docs/components/webrtc.md), типизированные RPC, полномочия узлов, создание сетевых объектов и [репликация свойств](docs/components/scene-replication.md).
- [Автоматизация проектов](tools/Batch/README.md). Создание и сборка C#-проектов, сохранение и загрузка сцен, запуск сценариев без графики и захват реально отрисованных кадров со структурированными отчётами. Для захвата нужны нативное окно и рендерер.

Шейдерные материалы, GPU-частицы, освещение с тенями и HDR требуют GPU-рендерера; совместимый рендерер поддерживает свой описанный в документации набор 2D-возможностей. Физический бэкенд выбирается независимо от рендерера. Поддержка и границы проверки указаны в документации подсистем и [отчёте по платформам](docs/platform-verification.md).

<a id="quick-start"></a>

## <img src="docs/design/assets/sprite/readme-quick-start.svg" width="24" height="28" align="absmiddle" alt=""> Быстрый старт

Начните с [CharacterMovement](examples/CharacterMovement/README.md). На компьютере персонаж перемещается стрелками, на телефоне его можно перетаскивать пальцем, на телевизоре работают кнопки направления на пульте. Один пример предназначен для всех целевых платформ движка.

<a id="installation"></a>

### Что понадобится

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0). Git нужен только для сборки из исходников.

Установите через NuGet основной пакет `Electron2D` и пакет целевой платформы из [таблицы ниже](#use-electron2d-in-your-game). Нативные зависимости NuGet загрузит автоматически. [Правила версий платформенных пакетов](docs/native-packaging.md).

Для предварительных версий добавляйте `--prerelease`. Используйте совпадающие версии движка и платформенных пакетов. Опубликованные версии доступны в [NuGet](https://www.nuget.org/packages/Electron2D).

### Сборка и запуск

Чтобы создать свой проект, перейдите к разделу [Создание игрового проекта](#use-electron2d-in-your-game). Ниже показано, как собрать и запустить готовый пример из репозитория.

Для сборки из исходников клонируйте репозиторий и соберите библиотеку:

```bash
git clone https://github.com/edwardgushchin/Electron2D.git
cd Electron2D
dotnet build Electron2D.csproj -c Release
```

Запустите пример на компьютере:

```bash
dotnet run --project examples/CharacterMovement
```

Откроется окно 800 на 600 с персонажем на сетке. Размер окна и персонажа на настольных платформах фиксирован. Стрелки перемещают его в пределах поля, Escape или закрытие окна завершают приложение. Попробуйте изменить скорость в `Player.cs` и запустить пример снова.

Для macOS откройте собранный бандл приложения, как показано в [инструкции](examples/CharacterMovement/README.md#macos). Запуск на [Android](examples/CharacterMovement/README.md#android-phone-or-tablet), [Android TV](examples/CharacterMovement/README.md#android-tv), [iOS](examples/CharacterMovement/README.md#ios), [Apple TV](examples/CharacterMovement/README.md#apple-tv-tvos) и в [браузере](examples/CharacterMovement/README.md#web) описан в README примера.

![CharacterMovement: персонаж на сетке](docs/images/character-movement.png)

[Исходный код примера](examples/CharacterMovement/CharacterMovementScene.cs) · [Подробности запуска](examples/CharacterMovement/README.md)

<a id="use-electron2d-in-your-game"></a>

### Создание игрового проекта

Для игры под Windows, Linux или macOS создайте консольный проект .NET 10 и добавьте в него пакет движка:

```bash
dotnet new console -n MyGame --framework net10.0
cd MyGame
dotnet add package Electron2D --prerelease
```

Можно вместо этого использовать исходники движка. В таком случае выполните следующие команды из корня репозитория:

```bash
dotnet new console -n MyGame -o ../MyGame --framework net10.0
dotnet add ../MyGame/MyGame.csproj reference Electron2D.csproj
cd ../MyGame
```

Затем установите пакет целевой платформы:

| Платформа | Команда |
| --- | --- |
| Windows | `dotnet add package Electron2D.Windows --prerelease` |
| Linux | `dotnet add package Electron2D.Linux --prerelease` |
| macOS | `dotnet add package Electron2D.MacOS --prerelease` |
| Web | `dotnet add package Electron2D.Web --prerelease` |
| Android и Android TV | `dotnet add package Electron2D.Android --prerelease` |
| iOS | `dotnet add package Electron2D.iOS --prerelease` |
| Apple TV (tvOS) | `dotnet add package Electron2D.tvOS --prerelease` |

Для нескольких целевых платформ добавьте каждый нужный пакет. Для Web, Android, iOS и tvOS понадобятся соответствующие наборы инструментов .NET (workloads) и платформенные точки входа приложения. Готовые проекты приложений в платформенные пакеты не входят. Консольный пример ниже предназначен для Windows, Linux и macOS.

Замените содержимое `Program.cs` следующим кодом:

```csharp
using Electron2D;

var window = new Window
{
    Title = "Моя игра",
    Size = new Vector2i(800, 600)
};

return Engine.Run(window);
```

Запустите приложение:

```bash
dotnet build -c Release
dotnet run -c Release
```

Игровые объекты добавляются в окно через `AddChild`. Обработка кадров и клавиатуры показана в примере выше.

Сборка движка создаёт `Electron2D.dll`. При публикации игры .NET включает библиотеку движка и нативные зависимости; автономная публикация также включает среду .NET.

#### Телефоны, телевизоры и браузер

Для этих целей возьмите за основу проект [CharacterMovement](examples/CharacterMovement/README.md#start-your-own-project). Скопируйте его исходники и платформенные файлы в `examples/MyGame`, переименуйте проект в `MyGame.csproj` и задайте свой идентификатор приложения. В README примера приведены команды копирования, настройки иконки и установки на устройство.

После создания проекта выберите цель:

| Цель | Команда из корня репозитория | Установка и запуск |
| --- | --- | --- |
| Android, телефон или планшет ARM64 | `dotnet build examples/MyGame/MyGame.csproj -c Release -r android-arm64` | [USB, APK и тач](examples/CharacterMovement/README.md#android-phone-or-tablet) |
| Android TV, ARM 32-bit | `dotnet build examples/MyGame/MyGame.csproj -c Release -r android-arm` | [ADB по сети и пульт](examples/CharacterMovement/README.md#android-tv) |
| Web | `dotnet publish examples/MyGame/MyGame.csproj -c Release -r browser-wasm -o bin/my-game/web` | [HTTP-сервер и Chrome](examples/CharacterMovement/README.md#web) |
| iOS, симулятор на Mac ARM64 | `dotnet build examples/MyGame/MyGame.csproj -c Release -r iossimulator-arm64 -p:EnableCodeSigning=false` | [Симулятор и подписанное приложение](examples/CharacterMovement/README.md#ios) |
| Apple TV, симулятор на Mac ARM64 | `dotnet build examples/MyGame/MyGame.csproj -c Release -r tvossimulator-arm64 -p:EnableCodeSigning=false` | [Симулятор и устройство tvOS](examples/CharacterMovement/README.md#apple-tv-tvos) |

У телефонов и телевизоров бывают другие архитектуры; все варианты перечислены в README примера. Сборка iOS и tvOS требует macOS и Xcode.

На телефоне и телевизоре приложение занимает весь экран. В браузере доступна кнопка полноэкранного режима. Поле подстраивается под соотношение сторон, а персонаж, текст и сетка масштабируются вместе.

<a id="platforms"></a>

## <img src="docs/design/assets/sprite/readme-platforms.svg" width="24" height="28" align="absmiddle" alt=""> Платформы

В таблице указано, что проверено на каждой целевой платформе игры. Визуальный редактор предназначен для Windows, Linux и macOS.

| Целевая платформа игры | Проверено в репозитории |
| --- | --- |
| Windows, x86 / x64 / ARM64 | Все тесты движка без окна и доступные проверки trimming и AOT завершились успешно в CI. Отрисовка ещё не проверена |
| Linux, x64 / ARM64 | Все тесты движка без окна и проверки trimming и AOT завершились успешно на обеих архитектурах. На x64 проверены окно, ввод и отрисовка через Wayland, а также отрисовка через XWayland. Отрисовка на ARM64 ещё не проверена |
| macOS, x64 / ARM64 | Все тесты движка без окна и проверки trimming и AOT завершились успешно в CI. Отрисовка ещё не проверена |
| Android, телефоны и планшеты | На одном телефоне ARM64 проверены запуск CharacterMovement, полноэкранная сцена и перетаскивание тачем. Шейдерные материалы и физика проверены отдельно |
| Android TV | На одном 32-битном телевизоре проверены запуск CharacterMovement и управление пультом. Совместимый рендерер и физика проверены отдельно |
| iOS и tvOS, устройства и симуляторы | Проверки с нативными библиотеками завершились успешно во всех четырёх профилях симуляторов. Приложения для устройств собираются без подписи; запуск на реальных устройствах и отрисовка ещё не проверены |
| Браузеры | В локальном Chrome проверены CharacterMovement, адаптивная сцена и ввод. Портретный размер и тач дополнительно проверены в эмуляции; физический мобильный браузер не проверялся |

Проверки CharacterMovement относятся к указанным устройствам и Chrome. Доступность нативных библиотек для выбранной платформы описана в [инструкциях по их доставке](docs/native-packaging.md).

Бейджи сборки и тестов показывают состояние автоматических проверок кода и тестовых приложений. Запуск на реальных устройствах проверяется отдельно. [Подробности автоматических проверок](docs/platform-verification.md#automated-rid-checks).

Модели устройств, команды и границы проверок перечислены в [отчёте о платформах](docs/platform-verification.md).

<a id="development"></a>

## <img src="docs/design/assets/sprite/readme-development.svg" width="24" height="28" align="absmiddle" alt=""> Состояние проекта

Electron2D находится на стадии alpha. Публичный API ещё развивается и может меняться между релизами.

Игровые сцены можно создавать в коде, сохранять через `PackedScene` и повторно загружать из [файлов ресурсов и сцен](docs/components/resource-files.md). Визуальный редактор пока показывает стартовое окно; редактирование игровых проектов в нём ещё не реализовано.

<a id="documentation"></a>

## <img src="docs/design/assets/sprite/readme-documentation.svg" width="24" height="28" align="absmiddle" alt=""> Документация

| Нужно | Где читать |
| --- | --- |
| Найти класс или метод | [Справочник API](docs/inventory.md) |
| Разобраться в подсистеме | [Оглавление документации](docs/README.md) |
| Подготовить шейдеры | [Инструмент импорта HLSL и GLSL](tools/shaders/README.md) |
| Автоматизировать сборку проектов, сценарии и захват кадров | [Пакетный запуск проектов](tools/Batch/README.md) |
| Подготовить релиз | [Порядок выпуска](docs/releasing.md) |

<a id="examples"></a>

Для воспроизведения проверок есть отдельные инструкции для [Android](tests/Electron2D.AndroidProbe/README.md) и [WebGPU](tests/Electron2D.WebGpuProbe/README.md). Тест WebGPU проверяет возможности браузера отдельно от движка.

<a id="feedback-and-contributing"></a>

## <img src="docs/design/assets/sprite/readme-contributing.svg" width="24" height="28" align="absmiddle" alt=""> Участие в проекте

[Задать вопрос](https://github.com/edwardgushchin/Electron2D/discussions/categories/q-a) · [Сообщить о проблеме](https://github.com/edwardgushchin/Electron2D/issues/new/choose) · [Руководство для участников](CONTRIBUTING.md) · [Помощь](SUPPORT.md) · [Правила общения](CODE_OF_CONDUCT.md) · [Безопасность](SECURITY.md)

Сообщайте об ошибках и предлагайте новые возможности в [GitHub Issues](https://github.com/edwardgushchin/Electron2D/issues). Для ошибки укажите версию или коммит движка, операционную систему и рендерер. Приложите минимальный пример и вывод ошибки.

Исправления присылайте через [пул-реквесты](https://github.com/edwardgushchin/Electron2D/pulls). Порядок работы описан в [руководстве для участников](CONTRIBUTING.md).

<a id="contributors"></a>

Проект ведёт [Эдуард Гущин](https://github.com/edwardgushchin). Все авторы изменений перечислены на [странице участников](https://github.com/edwardgushchin/Electron2D/graphs/contributors).

<a id="license"></a>

## <img src="docs/design/assets/sprite/readme-license.svg" width="24" height="28" align="absmiddle" alt=""> Лицензия

Electron2D распространяется по [лицензии MIT](licence/Electron2D-LICENSE.txt). Движок можно использовать в коммерческих играх; сохраняйте уведомление об авторских правах и текст лицензии.

Лицензии зависимостей перечислены в [уведомлениях о сторонних компонентах](licence/THIRD_PARTY_NOTICES.md). При распространении игры приложите требуемые ими тексты из каталога `licence/`.
