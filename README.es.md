<p align="right"><a href="README.md">English</a> · <a href="README.ru.md">Русский</a> · <a href="README.zh-CN.md">简体中文</a> · Español · <a href="README.pt-BR.md">Português (BR)</a></p>

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
  <a href="#installation"><img alt="Versión de .NET necesaria para compilar" src="https://img.shields.io/badge/dynamic/xml?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;url=https%3A%2F%2Fraw.githubusercontent.com%2Fedwardgushchin%2FElectron2D%2Fmain%2FElectron2D.csproj&amp;query=substring-after%28%2FProject%2FPropertyGroup%2FTargetFramework%5Bnot%28%40Condition%29%5D%5B1%5D%2C+%27net%27%29&amp;label=.NET&amp;suffix=+SDK&amp;color=A63B75" height="28"></a>
  <a href="#license"><img alt="Licencia del motor" src="https://img.shields.io/badge/dynamic/regex?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;url=https%3A%2F%2Fraw.githubusercontent.com%2Fedwardgushchin%2FElectron2D%2Fmain%2Flicence%2FElectron2D-LICENSE.txt&amp;search=%5E%5Cs%2A%28%5CS%2B%29%5Cs%2BLicense&amp;replace=%241&amp;label=Licencia&amp;color=A63B75" height="28"></a>
  <a href="https://github.com/edwardgushchin/Electron2D/releases"><img alt="Última versión publicada" src="https://img.shields.io/github/v/release/edwardgushchin/Electron2D?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;label=Versi%C3%B3n&amp;color=A63B75" height="28"></a>
</p>

<p align="center">
  <a href="https://github.com/edwardgushchin/Electron2D/commits/main"><img alt="Último commit en main" src="https://img.shields.io/github/last-commit/edwardgushchin/Electron2D/main?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;label=%C3%9Altimo+commit&amp;display_timestamp=committer&amp;color=A63B75" height="28"></a>
  <a href="https://github.com/edwardgushchin/Electron2D/actions/workflows/ci.yml"><img alt="Estado de la compilación automática" src="https://img.shields.io/github/check-runs/edwardgushchin/Electron2D/main?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;label=Compilaci%C3%B3n&amp;nameFilter=Build" height="28"></a>
  <a href="https://github.com/edwardgushchin/Electron2D/actions/workflows/ci.yml"><img alt="Pruebas (GitHub Actions)" src="https://img.shields.io/github/check-runs/edwardgushchin/Electron2D/main?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;label=Pruebas&amp;nameFilter=Tests" height="28"></a>
</p>

<p align="center">
  <a href="#quick-start">Empezar</a> ·
  <a href="#features">Funciones</a> ·
  <a href="#platforms">Plataformas</a> ·
  <a href="#documentation">Documentación</a> ·
  <a href="#feedback-and-contributing">Participar</a>
</p>

<p align="center"><img src="docs/design/assets/sprite/readme-features.svg" width="16" height="16" align="absmiddle" alt=""> <a href="https://github.com/edwardgushchin/Electron2D">Danos una estrella en GitHub</a> - ¡nos motiva mucho!</p>

<a id="about"></a>

## <img src="docs/design/assets/sprite/readme-about.svg" width="24" height="24" align="texttop" alt=""> Acerca del proyecto

Electron2D es un **motor 2D libre y multiplataforma, escrito en C#, para crear juegos en colaboración con agentes de IA**.

Crea mundos y mecánicas de juego con las herramientas habituales de .NET y asistentes de IA como Codex o Claude Code.

<a id="features"></a>

## <img src="docs/design/assets/sprite/readme-features.svg" width="24" height="24" align="texttop" alt=""> Funciones

- [Gráficos](docs/domains/rendering.md). Sprites y atlas, cámaras, paralaje, dibujo de formas y texto. Importación de shaders HLSL y GLSL para materiales.
- [Escenas y animación](docs/domains/scene.md). Objetos y niveles reutilizables, animación por fotogramas, animación de propiedades y temporizadores.
- [Física](docs/domains/physics.md). Cuerpos rígidos, colisiones, áreas, consultas de intersección, articulaciones y resortes.
- [Interfaz de juego](docs/domains/scene.md). Botones, campos de texto, desplazamiento, contenedores de diseño, fuentes y temas.
- [Audio](docs/domains/audio.md). WAV, MP3 y Ogg Vorbis, audio posicional, mezcla, efectos y grabación.
- [Controles](docs/domains/input.md). Teclado, ratón, entrada táctil y mandos. Asignación de entradas a acciones del juego.
- [Búsqueda de rutas](docs/domains/navigation.md). Rutas en una cuadrícula o entre puntos definidos, teniendo en cuenta obstáculos y costes de desplazamiento.
- [Recursos](docs/domains/resources.md). Carga de imágenes, fuentes y audio. Gradientes, curvas y texturas procedurales.
- [Localización](docs/domains/localization.md). Traducciones, formas plurales y selección de idioma.
- [Redes](docs/domains/networking.md). TCP, UDP, sockets locales y conexiones cifradas con TLS.

Los materiales con shaders requieren el renderizador GPU. El renderizador de compatibilidad admite gráficos 2D básicos. Consulta los enlaces anteriores para conocer los detalles y las limitaciones.

<a id="quick-start"></a>

## <img src="docs/design/assets/sprite/readme-quick-start.svg" width="24" height="24" align="texttop" alt=""> Inicio rápido

Empieza con el ejemplo «Ventana y entrada». Estos comandos son para Linux x64 con Wayland.

<a id="installation"></a>

### Requisitos

- [SDK de .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0) y Git.

Los componentes nativos privados se restauran como dependencias de NuGet. Consulta la [distribución de paquetes nativos](docs/native-packaging.md) para conocer la disponibilidad del primer paquete y las instrucciones de recompilación nativa completa.

### Compilar y ejecutar

Clona el repositorio y compila la biblioteca:

```bash
git clone https://github.com/edwardgushchin/Electron2D.git
cd Electron2D
dotnet build Electron2D.csproj -c Release
```

Publica el ejemplo con su propio entorno de ejecución de .NET e inícialo:

```bash
dotnet publish examples/HostExample/HostExample.csproj -c Release -r linux-x64 --self-contained true -o /tmp/electron2d-host-example
/tmp/electron2d-host-example/HostExample
```

Se abrirá una ventana. Las flechas mueven un objeto del juego y sus coordenadas aparecen en la terminal. Escape o cerrar la ventana termina la aplicación. Este ejemplo no dibuja la escena; muestra cómo iniciar el motor y procesar la entrada.

[Código del ejemplo](examples/HostExample/Program.cs) · [Instrucciones de ejecución](examples/HostExample/README.md)

### Usar Electron2D en tu juego

Crea un proyecto de consola de .NET 10 junto al directorio `Electron2D` y añade una referencia al proyecto del motor. Ejecuta estos comandos desde la raíz del repositorio:

```bash
dotnet new console -n MyGame -o ../MyGame --framework net10.0
dotnet add ../MyGame/MyGame.csproj reference Electron2D.csproj
```

Sustituye el contenido de `MyGame/Program.cs` por este código:

```csharp
using Electron2D;

var window = new Window
{
    Title = "Mi juego",
    Size = new Vector2i(960, 540)
};

return Engine.Run(window);
```

Ejecuta la aplicación:

```bash
dotnet run --project ../MyGame/MyGame.csproj -c Release
```

Añade objetos del juego a la ventana con `AddChild`. El ejemplo anterior muestra la actualización por fotogramas y el manejo del teclado.

La compilación del motor genera `Electron2D.dll`. Al publicar el juego se incluyen la biblioteca del motor y las dependencias nativas; una publicación autónoma también incluye el entorno de ejecución de .NET.

<a id="platforms"></a>

## <img src="docs/design/assets/sprite/readme-platforms.svg" width="24" height="24" align="texttop" alt=""> Plataformas

Las plataformas objetivo del juego y las comprobaciones realizadas se indican por separado. El editor visual está destinado a Windows, Linux y macOS.

| Plataforma objetivo del juego | Comprobado en este repositorio |
| --- | --- |
| Windows, x86 / x64 / ARM64 | Todavía no se ha comprobado la ejecución |
| Linux, x64 / ARM64 | En x64 se han comprobado ventanas, entrada y renderizado con Wayland. También se ha comprobado el renderizado con XWayland. ARM64 aún no se ha comprobado |
| macOS, x64 / ARM64 | Todavía no se ha comprobado la ejecución |
| Android, teléfonos y tabletas | Se han comprobado el renderizado, los materiales con shaders y la física en un teléfono ARM64 |
| Android TV | Se han comprobado el renderizador de compatibilidad y la física en un televisor de 32 bits |
| iOS y tvOS, dispositivos y simuladores | Se ha preparado la compilación automática de la biblioteca. Todavía no se han realizado comprobaciones en dispositivos |
| Navegadores | Se han comprobado el renderizado y la física en una aplicación de prueba independiente. Todavía no se ha implementado la ejecución de juegos en el navegador |

Se ha compilado el conjunto completo de bibliotecas nativas de texto y audio para Linux x64. Su compilación e integración en otras plataformas sigue siendo una tarea aparte. Las comprobaciones de Android y navegador abarcan escenarios concretos.

Los indicadores cubren los 18 RID: compilación con analizadores, pruebas completas sin ventana en Linux o portables en escritorio, y aplicaciones de contrato trimmed/AOT, Android, simuladores Apple y navegador. Los dispositivos Apple físicos y la aceptación nativa completa de otras plataformas siguen separados. Consulta el [alcance del CI](docs/platform-verification.md#automated-rid-checks).

Consulta los modelos de dispositivos, los comandos y el alcance de las comprobaciones en el [informe de plataformas](docs/platform-verification.md).

<a id="development"></a>

## <img src="docs/design/assets/sprite/readme-development.svg" width="24" height="24" align="texttop" alt=""> Desarrollo del motor

Actualmente puedes usar Electron2D mediante C# y .NET. Las plantillas `PackedScene` admiten [archivos tipados de recursos y escenas](docs/components/resource-files.md), incluida la carga en un proceso nuevo. El editor visual y los comandos para gestionar proyectos de juego siguen previstos.

La colaboración con IA forma parte de la [arquitectura del motor](docs/decisions/agent-native.md#adr-0090): las operaciones de proyecto, la ejecución de escenarios de juego y la comprobación de imágenes deben estar disponibles mediante herramientas documentadas. El conjunto completo de herramientas aún está por implementar.

Las próximas tareas y el estado de los métodos se detallan en el [plan de desarrollo](docs/coverage/index.md). El comportamiento implementado se describe en la referencia de la API.

<a id="documentation"></a>

## <img src="docs/design/assets/sprite/readme-documentation.svg" width="24" height="24" align="texttop" alt=""> Documentación

| Necesitas | Dónde consultar |
| --- | --- |
| Encontrar una clase o un método | [Referencia de la API](docs/inventory.md) |
| Entender un subsistema | [Índice de documentación](docs/README.md) |
| Preparar shaders | [Herramienta de importación de HLSL y GLSL](tools/shaders/README.md) |
| Entender la arquitectura y las decisiones | [Decisiones de arquitectura](docs/decisions/index.md) |
| Elegir una tarea de desarrollo | [Plan de desarrollo](docs/coverage/index.md) |

<a id="examples"></a>

Hay instrucciones independientes para reproducir las comprobaciones de [Android](tests/Electron2D.AndroidProbe/README.md) y [WebGPU](tests/Electron2D.WebGpuProbe/README.md). La prueba de WebGPU comprueba las capacidades del navegador por separado del motor.

<a id="feedback-and-contributing"></a>

## <img src="docs/design/assets/sprite/readme-contributing.svg" width="24" height="24" align="texttop" alt=""> Participa en el proyecto

[Hacer una pregunta](https://github.com/edwardgushchin/Electron2D/discussions/categories/q-a) · [Reportar un problema](https://github.com/edwardgushchin/Electron2D/issues/new/choose) · [Guía de contribución](CONTRIBUTING.md) · [Ayuda](SUPPORT.md) · [Código de conducta](CODE_OF_CONDUCT.md) · [Seguridad](SECURITY.md)

Informa de errores y propón funciones en [GitHub Issues](https://github.com/edwardgushchin/Electron2D/issues). En un informe de error, indica la versión o el commit del motor, el sistema operativo y el renderizador. Adjunta un ejemplo mínimo y la salida del error.

Envía correcciones mediante [pull requests](https://github.com/edwardgushchin/Electron2D/pulls). Antes de empezar, lee la [guía de mantenimiento](docs/maintaining.md) y las decisiones de arquitectura del tema correspondiente. Actualiza el código, las pruebas y la documentación juntos.

Ejecuta las comprobaciones principales desde la raíz del repositorio:

```bash
dotnet run --project tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release
tools/coverage/check.sh
```

<a id="contributors"></a>

El proyecto está a cargo de [Eduard Gushchin](https://github.com/edwardgushchin). Todos los autores de cambios aparecen en la [página de colaboradores de GitHub](https://github.com/edwardgushchin/Electron2D/graphs/contributors).

<a id="license"></a>

## <img src="docs/design/assets/sprite/readme-license.svg" width="24" height="24" align="texttop" alt=""> Licencia

Electron2D se distribuye bajo la [licencia MIT](licence/Electron2D-LICENSE.txt). Puedes usar el motor en juegos comerciales; conserva el aviso de derechos de autor y el texto de la licencia.

Las licencias de las dependencias se enumeran en los [avisos de componentes de terceros](licence/THIRD_PARTY_NOTICES.md). Al distribuir el juego, incluye los textos de licencia que exijan esas dependencias desde el directorio `licence/`.
