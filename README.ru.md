<p align="right"><a href="README.md">English</a> · Русский · <a href="README.zh-CN.md">简体中文</a> · <a href="README.es.md">Español</a> · <a href="README.pt-BR.md">Português (BR)</a></p>

<h1 align="center">
  <picture>
    <source media="(prefers-color-scheme: dark) and (max-width: 480px)" srcset="docs/design/assets/sprite/logo-compact-dark.svg">
    <source media="(prefers-color-scheme: light) and (max-width: 480px)" srcset="docs/design/assets/sprite/logo-compact-light.svg">
    <source media="(prefers-color-scheme: dark)" srcset="docs/design/assets/sprite/logo-primary-dark.svg">
    <source media="(prefers-color-scheme: light)" srcset="docs/design/assets/sprite/logo-primary-light.svg">
    <img alt="Electron2D — Agent-native cross-platform 2D game engine" src="docs/design/assets/sprite/logo-primary-light.svg" width="640" height="148">
  </picture>
</h1>

<p align="center">
  <a href="#installation"><img alt="Версия .NET для сборки проекта" src="https://img.shields.io/badge/dynamic/xml?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;url=https%3A%2F%2Fraw.githubusercontent.com%2Fedwardgushchin%2FElectron2D%2Fmain%2FElectron2D.csproj&amp;query=substring-after%28%2FProject%2FPropertyGroup%2FTargetFramework%5Bnot%28%40Condition%29%5D%5B1%5D%2C+%27net%27%29&amp;label=.NET&amp;suffix=+SDK&amp;color=A63B75" height="28"></a>
  <a href="#license"><img alt="Лицензия движка" src="https://img.shields.io/badge/dynamic/regex?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;url=https%3A%2F%2Fraw.githubusercontent.com%2Fedwardgushchin%2FElectron2D%2Fmain%2Flicence%2FElectron2D-LICENSE.txt&amp;search=%5E%5Cs%2A%28%5CS%2B%29%5Cs%2BLicense&amp;replace=%241&amp;label=%D0%9B%D0%B8%D1%86%D0%B5%D0%BD%D0%B7%D0%B8%D1%8F&amp;color=A63B75" height="28"></a>
  <a href="https://github.com/edwardgushchin/Electron2D/releases"><img alt="Последний опубликованный релиз" src="https://img.shields.io/badge/dynamic/xml?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;url=https%3A%2F%2Fgithub.com%2Fedwardgushchin%2FElectron2D%2Freleases.atom&amp;query=concat%28substring-after%28string%28%28%2F%2F%2A%5Blocal-name%28%29%3D%22entry%22%5D%5B1%5D%2F%2A%5Blocal-name%28%29%3D%22link%22%5D%2F%40href%29%5B1%5D%29%2C+%22%2Ftag%2F%22%29%2C+substring%28%22%D0%BD%D0%B5%D1%82+%D1%80%D0%B5%D0%BB%D0%B8%D0%B7%D0%BE%D0%B2%22%2C+1+div+not%28%2F%2F%2A%5Blocal-name%28%29%3D%22entry%22%5D%29%29%29&amp;label=%D0%A0%D0%B5%D0%BB%D0%B8%D0%B7&amp;color=A63B75" height="28"></a>
</p>

<p align="center">
  <a href="https://github.com/edwardgushchin/Electron2D/commits/main"><img alt="Последний коммит в main" src="https://img.shields.io/github/last-commit/edwardgushchin/Electron2D/main?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;label=%D0%9F%D0%BE%D1%81%D0%BB%D0%B5%D0%B4%D0%BD%D0%B8%D0%B9+%D0%BA%D0%BE%D0%BC%D0%BC%D0%B8%D1%82&amp;display_timestamp=committer&amp;color=A63B75" height="28"></a>
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

<p align="center">⭐ <a href="https://github.com/edwardgushchin/Electron2D">Поставьте нам звезду на GitHub</a> - это очень мотивирует!</p>

<a id="about"></a>

## <img src="docs/design/assets/sprite/readme-about.svg" width="24" height="28" align="absmiddle" alt=""> О проекте

Electron2D - **свободный кроссплатформенный 2D-движок на C# для совместной разработки игр человеком и ИИ-агентами**.

Создавайте игровые миры и механики с привычными инструментами .NET и ИИ-помощниками вроде Codex или Claude Code.

<a id="features"></a>

## <img src="docs/design/assets/sprite/readme-features.svg" width="24" height="28" align="absmiddle" alt=""> Возможности

- [Графика](docs/domains/rendering.md). Спрайты и атласы, камеры, параллакс, рисование фигур и текста. Импорт шейдеров HLSL и GLSL для материалов.
- [Сцены и анимация](docs/domains/scene.md). Повторно используемые объекты и уровни, покадровая анимация, анимация свойств и таймеры.
- [Физика](docs/domains/physics.md). Твёрдые тела, столкновения, области, проверка пересечений, шарниры и пружины.
- [Игровой интерфейс](docs/domains/scene.md). Кнопки, поля ввода, прокрутка, контейнеры для размещения элементов, шрифты и темы оформления.
- [Звук](docs/domains/audio.md). WAV, MP3 и Ogg Vorbis, позиционный звук, микширование, эффекты и запись.
- [Управление](docs/domains/input.md). Клавиатура, мышь, касания и контроллеры. Привязка ввода к игровым действиям.
- [Поиск пути](docs/domains/navigation.md). Маршруты по сетке и между заданными точками с учётом препятствий и стоимости перемещения.
- [Ресурсы](docs/domains/resources.md). Загрузка изображений, шрифтов и аудио. Градиенты, кривые и процедурные текстуры.
- [Локализация](docs/domains/localization.md). Переводы, формы множественного числа и выбор языка.
- [Сеть](docs/domains/networking.md). TCP, UDP и локальные сокеты, TLS-шифрование соединений.

Шейдерные материалы требуют GPU-рендерера. Совместимый рендерер поддерживает базовую 2D-графику. Подробности и ограничения описаны в документации по ссылкам выше.

<a id="quick-start"></a>

## <img src="docs/design/assets/sprite/readme-quick-start.svg" width="24" height="28" align="absmiddle" alt=""> Быстрый старт

Начните с примера «Управление персонажем». Вы увидите персонажа и сможете перемещать его стрелками. Команды .NET ниже используются на Windows, Linux и macOS; результаты проверки запуска приведены в [таблице платформ](#platforms).

<a id="installation"></a>

### Что понадобится

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) и Git.

Подключайте движок через NuGet: пакет `Electron2D` и пакеты нужных платформ, например `Electron2D.Windows`, `Electron2D.Linux` или `Electron2D.MacOS`. Нативные зависимости восстанавливаются автоматически. [Платформенные пакеты и правила версий](docs/native-packaging.md).

Пакеты текущей версии `0.1.0-alpha.1` ещё готовятся к публикации.

### Сборка и запуск

Клонируйте репозиторий и соберите библиотеку:

```bash
git clone https://github.com/edwardgushchin/Electron2D.git
cd Electron2D
dotnet build Electron2D.csproj -c Release
```

Запустите пример:

```bash
dotnet run --project examples/CharacterMovement
```

Откроется сцена с розовым персонажем на сетке. Стрелки перемещают его в пределах поля, Escape или закрытие окна завершают приложение. Попробуйте изменить скорость в `Player.cs` и запустить пример снова.

![Управление персонажем Electron2D: персонаж на сетке и подсказки управления](docs/images/character-movement.png)

[Исходный код примера](examples/CharacterMovement/CharacterMovementScene.cs) · [Подробности запуска](examples/CharacterMovement/README.md)

### Подключение к своей игре

Создайте консольный проект .NET 10 рядом с каталогом `Electron2D` и добавьте ссылку на движок. Команды выполняются из корня репозитория:

```bash
dotnet new console -n MyGame -o ../MyGame --framework net10.0
dotnet add ../MyGame/MyGame.csproj reference Electron2D.csproj
```

Добавьте пакет своей платформы через NuGet. Выберите одну команду:

| Платформа | Команда |
| --- | --- |
| Windows | `dotnet add ../MyGame/MyGame.csproj package Electron2D.Windows --prerelease` |
| Linux | `dotnet add ../MyGame/MyGame.csproj package Electron2D.Linux --prerelease` |
| macOS | `dotnet add ../MyGame/MyGame.csproj package Electron2D.MacOS --prerelease` |

Замените содержимое `MyGame/Program.cs` следующим кодом:

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
dotnet run --project ../MyGame/MyGame.csproj -c Release
```

Игровые объекты добавляются в окно через `AddChild`. Обработка кадров и клавиатуры показана в примере выше.

Сборка движка создаёт `Electron2D.dll`. При публикации игры .NET включает библиотеку движка и нативные зависимости; автономная публикация также включает среду .NET.

<a id="platforms"></a>

## <img src="docs/design/assets/sprite/readme-platforms.svg" width="24" height="28" align="absmiddle" alt=""> Платформы

В таблице указано, что проверено на каждой целевой платформе игры. Визуальный редактор предназначен для Windows, Linux и macOS.

| Целевая платформа игры | Проверено в репозитории |
| --- | --- |
| Windows, x86 / x64 / ARM64 | Запуск ещё не проверен |
| Linux, x64 / ARM64 | На x64 проверены окно, ввод и отрисовка через Wayland. Отрисовка также проверена через XWayland. ARM64 ещё не проверен |
| macOS, x64 / ARM64 | Запуск ещё не проверен |
| Android, телефоны и планшеты | На одном телефоне ARM64 проверены отрисовка, шейдерные материалы и физика |
| Android TV | На одном 32-битном телевизоре проверены совместимый рендерер и физика |
| iOS и tvOS, устройства и симуляторы | Подготовлена автоматическая сборка библиотеки. Проверки на устройствах ещё не проводились |
| Браузеры | Отрисовка и физика проверены в отдельном тестовом приложении. Запуск игры в браузере ещё не реализован |

Проверки Android и браузера пока относятся к отдельным сценариям. Доступность нативных библиотек для выбранной платформы описана в [инструкциях по их доставке](docs/native-packaging.md).

Бейджи сборки и тестов показывают состояние автоматических проверок кода и тестовых приложений. Запуск на реальных устройствах проверяется отдельно. [Подробности автоматических проверок](docs/platform-verification.md#automated-rid-checks).

Модели устройств, команды и границы проверок перечислены в [отчёте о платформах](docs/platform-verification.md).

<a id="development"></a>

## <img src="docs/design/assets/sprite/readme-development.svg" width="24" height="28" align="absmiddle" alt=""> Разработка движка

Сцены можно сохранять в файлы и использовать повторно, в том числе при следующем запуске игры. Для этого служат `PackedScene` и [типизированные файлы ресурсов и сцен](docs/components/resource-files.md). Визуальный редактор пока показывает стартовое окно; редактирование игровых проектов и команды управления ими ещё не реализованы.

Для ИИ-помощников в [архитектуре движка](docs/decisions/agent-native.md#adr-0090) предусмотрены команды работы с проектом и запуска игровых сценариев с проверкой изображения. Полный набор этих инструментов ещё предстоит реализовать.

Следующие задачи и состояние отдельных методов доступны в [плане разработки](docs/coverage/index.md). Реализованное поведение описано в справочнике API.

<a id="documentation"></a>

## <img src="docs/design/assets/sprite/readme-documentation.svg" width="24" height="28" align="absmiddle" alt=""> Документация

| Нужно | Где читать |
| --- | --- |
| Найти класс или метод | [Справочник API](docs/inventory.md) |
| Разобраться в подсистеме | [Оглавление документации](docs/README.md) |
| Подготовить шейдеры | [Инструмент импорта HLSL и GLSL](tools/shaders/README.md) |
| Понять архитектуру и принятые решения | [Архитектурные решения](docs/decisions/index.md) |
| Выбрать задачу для разработки | [План разработки](docs/coverage/index.md) |

<a id="examples"></a>

Для воспроизведения проверок есть отдельные инструкции для [Android](tests/Electron2D.AndroidProbe/README.md) и [WebGPU](tests/Electron2D.WebGpuProbe/README.md). Тест WebGPU проверяет возможности браузера отдельно от движка.

<a id="feedback-and-contributing"></a>

## <img src="docs/design/assets/sprite/readme-contributing.svg" width="24" height="28" align="absmiddle" alt=""> Участие в проекте

[Задать вопрос](https://github.com/edwardgushchin/Electron2D/discussions/categories/q-a) · [Сообщить о проблеме](https://github.com/edwardgushchin/Electron2D/issues/new/choose) · [Руководство для участников](CONTRIBUTING.md) · [Помощь](SUPPORT.md) · [Правила общения](CODE_OF_CONDUCT.md) · [Безопасность](SECURITY.md)

Сообщайте об ошибках и предлагайте новые возможности в [GitHub Issues](https://github.com/edwardgushchin/Electron2D/issues). Для ошибки укажите версию или коммит движка, операционную систему и рендерер. Приложите минимальный пример и вывод ошибки.

Исправления присылайте через [пул-реквесты](https://github.com/edwardgushchin/Electron2D/pulls). Перед работой прочитайте [правила сопровождения](docs/maintaining.md) и архитектурные решения по выбранной теме. Код, тесты и документацию меняйте вместе.

Основные проверки запускаются из корня репозитория:

```bash
dotnet run --project tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release
tools/coverage/check.sh
```

<a id="contributors"></a>

Проект ведёт [Эдуард Гущин](https://github.com/edwardgushchin). Все авторы изменений перечислены на [странице участников](https://github.com/edwardgushchin/Electron2D/graphs/contributors).

<a id="license"></a>

## <img src="docs/design/assets/sprite/readme-license.svg" width="24" height="28" align="absmiddle" alt=""> Лицензия

Electron2D распространяется по [лицензии MIT](licence/Electron2D-LICENSE.txt). Движок можно использовать в коммерческих играх; сохраняйте уведомление об авторских правах и текст лицензии.

Лицензии зависимостей перечислены в [уведомлениях о сторонних компонентах](licence/THIRD_PARTY_NOTICES.md). При распространении игры приложите требуемые ими тексты из каталога `licence/`.
