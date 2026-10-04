<p align="right"><a href="README.md">English</a> · <a href="README.ru.md">Русский</a> · <a href="README.zh-CN.md">简体中文</a> · Español · <a href="README.pt-BR.md">Português (BR)</a></p>

<p align="center">
  <picture>
    <source media="(prefers-color-scheme: dark) and (max-width: 480px)" srcset="docs/design/assets/sprite/logo-compact-dark.svg">
    <source media="(prefers-color-scheme: light) and (max-width: 480px)" srcset="docs/design/assets/sprite/logo-compact-light.svg">
    <source media="(prefers-color-scheme: dark)" srcset="docs/design/assets/sprite/logo-primary-dark.svg">
    <source media="(prefers-color-scheme: light)" srcset="docs/design/assets/sprite/logo-primary-light.svg">
    <img alt="Electron2D — motor de juegos 2D multiplataforma concebido para agentes" src="docs/design/assets/sprite/logo-primary-light.svg" width="900">
  </picture>
</p>

<p align="center">
  <a href="https://github.com/edwardgushchin/Electron2D/graphs/contributors">Colaboradores</a> ·
  <a href="https://github.com/edwardgushchin/Electron2D/commits/main">Commits</a> ·
  <a href="licence/Electron2D-LICENSE.txt">Licencia MIT</a>
</p>

<p align="center">
  <img alt="Pensado para agentes · Multiplataforma · 2D · En desarrollo" src="docs/design/assets/sprite/readme-badges.svg" width="405">
</p>

<p align="center">
  <a href="#about">Acerca del proyecto</a> ·
  <a href="#features">Funciones</a> ·
  <a href="#platforms">Plataformas</a> ·
  <a href="#installation">Instalación</a> ·
  <a href="#quick-start">Inicio rápido</a> ·
  <a href="#documentation">Documentación</a> ·
  <a href="#examples">Ejemplos</a> ·
  <a href="#feedback-and-contributing">Comentarios</a> ·
  <a href="#license">Licencia</a>
</p>

<p align="center">
  ⭐ <a href="https://github.com/edwardgushchin/Electron2D">Dale una estrella al proyecto en GitHub</a> para seguir su desarrollo.
</p>

<a id="about"></a>

## 🧭 Acerca del proyecto

Electron2D es un **motor de juegos 2D agent-native y multiplataforma**.

Está diseñado para que los desarrolladores y los agentes de programación trabajen en los mismos juegos mediante operaciones programáticas documentadas: crear y editar proyectos, compilarlos, ejecutarlos y verificar los resultados. El desarrollador debe poder revisar los cambios y las verificaciones. La [arquitectura orientada a agentes](docs/decisions/agent-native.md#adr-0090) define operaciones de creación compartidas por la CLI y el editor, simulación sin interfaz gráfica y verificación por lotes del resultado renderizado.

Una única API pública del motor está dirigida a juegos de escritorio, móviles, televisión y navegador. Crea escenas a partir de nodos y recursos, con renderizado, entrada, física, audio e interfaces gráficas en el mismo motor.

**Estado del desarrollo:** el motor, los ejemplos, las pruebas y la documentación de la API están en desarrollo activo. El editor visual, una CLI unificada para proyectos, la persistencia de escenas en archivos y un flujo público de captura aún no están implementados. Más abajo se distinguen las plataformas previstas de las ejecuciones verificadas.

<a id="features"></a>

## ✨ Funciones

- **Flujo de trabajo con agentes** — El diseño busca que los agentes de programación puedan inspeccionar y editar escenas, recursos y ajustes del proyecto, y después compilar, ejecutar y verificar juegos mediante operaciones programáticas documentadas. Las herramientas pendientes figuran en la [hoja de ruta de implementación](docs/coverage/index.md).
- **Motor multiplataforma** — Una API para juegos de escritorio, móviles, televisión y navegador. Consulta las [plataformas previstas y las verificaciones actuales](#platforms).
- **Escenas basadas en nodos** — Jerarquías de `Node`, `CanvasItem` y `Entity`, planificación de escenas, temporizadores, interpolaciones y objetos `PackedScene` reutilizables en memoria.
- **Renderizado 2D** — Sprites, animaciones, cámaras, texturas, texto, dibujo en lienzo y materiales de sombreadores HLSL/GLSL con API tipada. Consulta las [capacidades y limitaciones del renderizado](docs/domains/rendering.md).
- **Componentes de interfaz** — Controles, contenedores, etiquetas, botones, entrada de texto, navegación del foco y temas tipados. Consulta el [dominio de escenas](docs/domains/scene.md).
- **Física 2D** — Cuerpos, áreas, formas de colisión, consultas y uniones, con simulación de paso fijo. Consulta el [dominio de física](docs/domains/physics.md).
- **Audio** — Reproducción de WAV, MP3 y Ogg Vorbis, flujos procedurales, buses de salida y API de grabación. Consulta el [comportamiento del audio y sus límites de verificación](docs/domains/audio.md).
- **Recursos y E/S** — Imágenes, fuentes, carga de recursos, configuración tipada, acceso a archivos y localización. Consulta los dominios de [recursos](docs/domains/resources.md) y [núcleo](docs/domains/core.md).
- **Lógica de juego en C#** — Escribe clases C# habituales con propiedades, recursos y eventos utilizando herramientas conocidas de .NET.

Esta lista combina áreas de funcionalidad implementadas con la dirección del producto. Los documentos enlazados registran las carencias que quedan en la API y los backends.

<a id="platforms"></a>

## 🖥️ Plataformas

| Plataforma | Objetivo del editor | Objetivo del motor | Verificación actual |
| --- | --- | --- | --- |
| Windows | Previsto | x86, x64, ARM64 | Paquetes nativos identificados; ejecución en Windows pendiente |
| Linux | Previsto; X11 y Wayland | x64, ARM64 | Comprobados el host y el renderizado en Linux x64 Wayland, y el renderizador mediante XWayland; ejecución en ARM64 pendiente |
| macOS | Previsto | x64, ARM64 | Paquetes nativos identificados; ejecución en macOS pendiente |
| Android | — | ABI de teléfonos y tabletas | Comprobados el lienzo, los sombreadores GPU y la física CPU en un teléfono ARM64; quedan otros dispositivos y escenarios por verificar |
| Android TV | — | ABI de Android | Comprobados el renderizado alternativo del lienzo y la física CPU en un televisor de 32 bits; ese dispositivo no tiene backend Vulkan |
| iOS | — | RID de dispositivo y simulador | Paquetes nativos identificados; CI preparada para compilar la biblioteca en macOS, pero aún no ejecutada |
| tvOS | — | RID de dispositivo y simulador | Paquetes nativos identificados; CI preparada para compilar la biblioteca en macOS, pero aún no ejecutada |
| Web | — | `browser-wasm` | Pruebas aisladas de lienzo/física y WebGPU independiente; host y backend del producto para navegador pendientes |

El editor está previsto para sistemas de escritorio. La existencia de un paquete nativo o una compilación correcta no demuestra por sí sola la compatibilidad con una plataforma. Las pruebas de Android solo cubren los dispositivos y las rutas gráficas y físicas indicadas; el ciclo de vida, la entrada, el audio, el almacenamiento y el empaquetado final requieren verificaciones adicionales.

La [matriz de verificación de plataformas](docs/platform-verification.md) registra dispositivos, renderizadores, comandos y límites. Linux Wayland es actualmente la comprobación nativa obligatoria. Los materiales de sombreadores requieren la ruta GPU; el renderizador de compatibilidad rechaza expresamente los materiales no admitidos.

<a id="installation"></a>

## 📦 Instalación

Instala el **SDK de .NET 10** y clona y compila el motor desde la raíz del repositorio:

```bash
git clone https://github.com/edwardgushchin/Electron2D.git
cd Electron2D
dotnet build Electron2D.csproj -c Release
```

El motor se compila como **`Electron2D.dll`**. Añade una referencia a `Electron2D.csproj` en tu proyecto de aplicación. El proyecto aporta las dependencias de plataforma durante la restauración y la publicación. Una publicación autónoma contiene el ejecutable del juego, `Electron2D.dll`, el entorno de .NET y las bibliotecas nativas correspondientes.

<a id="quick-start"></a>

## 🚀 Inicio rápido

Ejecuta el ejemplo existente de **ventana y entrada** en Linux Wayland:

```bash
dotnet publish examples/HostExample/HostExample.csproj -c Release -r linux-x64 --self-contained true -o /tmp/electron2d-host-example
/tmp/electron2d-host-example/HostExample
```

Mantén pulsadas las teclas de dirección para mover el nodo de la escena; pulsa Escape o cierra la ventana para salir. El ejemplo informa del movimiento en la terminal y no dibuja la escena.

Su [punto de entrada](examples/HostExample/Program.cs) configura `Window`, añade la escena y llama a `Engine.Run`. El motor controla los eventos, los tiempos de fotograma y el cierre. Consulta la [guía del ejemplo](examples/HostExample/README.md) para ver el flujo completo.

<a id="documentation"></a>

## 📚 Documentación

- **[Índice de documentación](docs/README.md)** — Guías de dominios y componentes.
- **[Referencia de API](docs/inventory.md)** — Tipos de producción implementados y páginas de sus clases.
- **[Arquitectura](docs/decisions/index.md)** — Decisiones actuales sobre el producto y el motor.
- **[Hoja de ruta de implementación](docs/coverage/index.md)** — Funciones implementadas, adaptadas y pendientes.
- **[Verificación de plataformas](docs/platform-verification.md)** — Evidencia de ejecución y límites de plataforma.
- **[Identidad visual](docs/design/identity.md)** — Dirección Sprite aprobada, archivos del logotipo, colores y tipografía.

<a id="examples"></a>

## 🎮 Ejemplos

- **[Ventana y entrada](examples/HostExample/README.md)** — Ejemplo ejecutable que solo usa la API pública, con ventana, nodo de escena, teclado y salida limpia.

La [prueba en dispositivo Android](tests/Electron2D.AndroidProbe/README.md) y la [prueba de sombreadores en navegador](tests/Electron2D.WebGpuProbe/README.md) son herramientas de verificación bajo `tests/`, no ejemplos de juegos. Documentan por separado las rutas gráficas y físicas comprobadas.

<a id="feedback-and-contributing"></a>

## 💬 Comentarios y contribuciones

Usa [GitHub Issues](https://github.com/edwardgushchin/Electron2D/issues) para informar de errores, proponer funciones o comentar el diseño. Al informar de un problema del motor, indica la revisión, la plataforma, el renderizador y un caso mínimo que lo reproduzca.

Se agradecen los [pull requests](https://github.com/edwardgushchin/Electron2D/pulls). Antes de cambiar el comportamiento, lee la [guía de mantenimiento](docs/maintaining.md) y las decisiones arquitectónicas pertinentes. Actualiza la documentación de la API y sus verificaciones junto con el cambio.

Ejecuta las comprobaciones desde la raíz del repositorio:

```bash
dotnet run --project tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release
tools/coverage/check.sh
```

<a id="contributors"></a>

## 👥 Colaboradores

Electron2D está mantenido por Eduard Gushchin. Consulta el [gráfico de colaboradores](https://github.com/edwardgushchin/Electron2D/graphs/contributors) del repositorio.

<a id="license"></a>

## 📄 Licencia

El código propio de Electron2D se distribuye bajo la [licencia MIT](licence/Electron2D-LICENSE.txt). Las dependencias conservan sus respectivas licencias; consulta los [avisos de terceros](licence/THIRD_PARTY_NOTICES.md). Los textos de las licencias se guardan en `licence/` y acompañan a las publicaciones de aplicaciones cuando corresponde.
