<p align="right"><a href="README.md">English</a> · <a href="README.ru.md">Русский</a> · <a href="README.zh-CN.md">简体中文</a> · <a href="README.es.md">Español</a> · Português (BR)</p>

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
  <a href="#installation"><img alt="Versão do .NET necessária para compilar" src="https://img.shields.io/badge/dynamic/xml?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;url=https%3A%2F%2Fraw.githubusercontent.com%2Fedwardgushchin%2FElectron2D%2Fmain%2FElectron2D.csproj&amp;query=substring-after%28%2FProject%2FPropertyGroup%2FTargetFramework%5Bnot%28%40Condition%29%5D%5B1%5D%2C+%27net%27%29&amp;label=.NET&amp;suffix=+SDK&amp;color=A63B75" height="28"></a>
  <a href="#license"><img alt="Licença do motor" src="https://img.shields.io/badge/dynamic/regex?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;url=https%3A%2F%2Fraw.githubusercontent.com%2Fedwardgushchin%2FElectron2D%2Fmain%2Flicence%2FElectron2D-LICENSE.txt&amp;search=%5E%5Cs%2A%28%5CS%2B%29%5Cs%2BLicense&amp;replace=%241&amp;label=Licen%C3%A7a&amp;color=A63B75" height="28"></a>
  <a href="https://github.com/edwardgushchin/Electron2D/releases"><img alt="Última versão publicada" src="https://img.shields.io/badge/dynamic/xml?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;url=https%3A%2F%2Fgithub.com%2Fedwardgushchin%2FElectron2D%2Freleases.atom&amp;query=concat%28substring-after%28string%28%28%2F%2F%2A%5Blocal-name%28%29%3D%22entry%22%5D%5B1%5D%2F%2A%5Blocal-name%28%29%3D%22link%22%5D%2F%40href%29%5B1%5D%29%2C+%22%2Ftag%2F%22%29%2C+substring%28%22sem+vers%C3%B5es%22%2C+1+div+not%28%2F%2F%2A%5Blocal-name%28%29%3D%22entry%22%5D%29%29%29&amp;label=Vers%C3%A3o&amp;color=A63B75" height="28"></a>
</p>

<p align="center">
  <a href="https://github.com/edwardgushchin/Electron2D/commits/main"><img alt="Último commit na main" src="https://img.shields.io/github/last-commit/edwardgushchin/Electron2D/main?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;label=%C3%9Altimo+commit&amp;display_timestamp=committer&amp;color=A63B75" height="28"></a>
  <a href="https://github.com/edwardgushchin/Electron2D/actions/workflows/ci.yml"><img alt="Status da compilação automática" src="https://img.shields.io/github/check-runs/edwardgushchin/Electron2D/main?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;label=Compila%C3%A7%C3%A3o&amp;nameFilter=Build" height="28"></a>
  <a href="https://github.com/edwardgushchin/Electron2D/actions/workflows/ci.yml"><img alt="Testes (GitHub Actions)" src="https://img.shields.io/github/check-runs/edwardgushchin/Electron2D/main?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;label=Testes&amp;nameFilter=Tests" height="28"></a>
</p>

<p align="center">
  <a href="#quick-start">Começar</a> ·
  <a href="#features">Recursos</a> ·
  <a href="#platforms">Plataformas</a> ·
  <a href="#documentation">Documentação</a> ·
  <a href="#feedback-and-contributing">Contribuir</a>
</p>

<p align="center">⭐ <a href="https://github.com/edwardgushchin/Electron2D">Dê uma estrela no GitHub</a> - isso nos motiva muito!</p>

<a id="about"></a>

## <img src="docs/design/assets/sprite/readme-about.svg" width="24" height="28" align="absmiddle" alt=""> Sobre o projeto

Electron2D é um **motor 2D livre e multiplataforma em C# para desenvolver jogos em colaboração com agentes de IA**.

Crie mundos e mecânicas de jogo com as ferramentas habituais do .NET. Codex, Claude Code e outros assistentes de IA podem ajudar com o código.

<a id="features"></a>

## <img src="docs/design/assets/sprite/readme-features.svg" width="24" height="28" align="absmiddle" alt=""> Recursos

Este é um resumo dos principais recursos já implementados no motor. As APIs dos subsistemas continuam evoluindo; os links descrevem o que funciona e suas limitações.

- [Gráficos](docs/domains/rendering.md). Sprites e atlas, câmeras, paralaxe, desenho de formas e texto. Importação de shaders HLSL e GLSL para materiais.
- [Cenas e animação](docs/domains/scene.md). Hierarquias de objetos do jogo, salvamento e carregamento de cenas, animação por quadros, animação de propriedades e temporizadores.
- [Física](docs/domains/physics.md). Corpos rígidos, colisões, áreas, consultas de interseção, articulações e molas.
- [Interface de jogo](docs/domains/scene.md). Botões, campos de texto, rolagem, contêineres de layout, fontes e temas.
- [Áudio](docs/domains/audio.md). WAV, MP3 e Ogg Vorbis, áudio posicional, mixagem, efeitos e gravação.
- [Controles](docs/domains/input.md). Teclado, mouse, toque e controles de jogo. Associação de entradas a ações do jogo.
- [Busca de caminhos](docs/domains/navigation.md). Rotas em uma grade ou entre pontos definidos, considerando obstáculos e custos de deslocamento.
- [Recursos](docs/domains/resources.md). Carregamento de imagens, fontes e áudio. Gradientes, curvas e texturas procedurais.
- [Localização](docs/domains/localization.md). Traduções, seleção de idioma e formas de plural com regras definidas pelo aplicativo.
- [Rede](docs/domains/networking.md). TCP, UDP e sockets locais, conexões seguras com TLS e DTLS, HTTP/HTTPS, WebSocket e conexões multijogador por ENet.

Materiais com shaders exigem o renderizador GPU. O renderizador de compatibilidade oferece gráficos 2D básicos. Consulte os links acima para ver os detalhes e as limitações.

<a id="quick-start"></a>

## <img src="docs/design/assets/sprite/readme-quick-start.svg" width="24" height="28" align="absmiddle" alt=""> Primeiros passos

Comece pelo exemplo «Movimentação do personagem». Você verá um personagem que pode mover com as setas. Os comandos do .NET abaixo são usados no Windows, Linux e macOS; veja as verificações de execução na [tabela de plataformas](#platforms).

<a id="installation"></a>

### Requisitos

- [SDK do .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0). O Git só é necessário para compilar a partir do código-fonte.

Instale pelo NuGet o pacote principal `Electron2D` e o pacote da plataforma de destino com os [comandos da tabela abaixo](#use-electron2d-in-your-game). O NuGet restaura as dependências nativas automaticamente. [Regras de versões dos pacotes de plataforma](docs/native-packaging.md).

Pacotes de pré-lançamento exigem `--prerelease`. Use versões correspondentes do motor e dos pacotes de plataforma. Versão atual no NuGet: [`0.1.0-alpha`](https://www.nuget.org/packages/Electron2D/0.1.0-alpha).

### Compilar e executar

Para criar seu próprio projeto, veja [Criar um projeto de jogo](#use-electron2d-in-your-game). Os comandos abaixo compilam e executam o exemplo do repositório.

Para compilar a partir do código-fonte, clone o repositório e compile a biblioteca:

```bash
git clone https://github.com/edwardgushchin/Electron2D.git
cd Electron2D
dotnet build Electron2D.csproj -c Release
```

Execute o exemplo:

```bash
dotnet run --project examples/CharacterMovement
```

Uma cena será aberta com um personagem rosa sobre uma grade. As setas o movem dentro do campo; Escape ou fechar a janela encerra o aplicativo. Experimente alterar a velocidade em `Player.cs` e executar o exemplo novamente.

![Movimentação do personagem do Electron2D: personagem sobre uma grade e instruções de teclado](docs/images/character-movement.png)

[Código do exemplo](examples/CharacterMovement/CharacterMovementScene.cs) · [Instruções de execução](examples/CharacterMovement/README.md)

<a id="use-electron2d-in-your-game"></a>

### Criar um projeto de jogo

Para um jogo destinado a Windows, Linux ou macOS, crie um projeto de console do .NET 10 e adicione o pacote do motor:

```bash
dotnet new console -n MyGame --framework net10.0
cd MyGame
dotnet add package Electron2D --prerelease
```

Você também pode usar o código-fonte do motor. Nesse caso, execute estes comandos a partir da raiz do repositório:

```bash
dotnet new console -n MyGame -o ../MyGame --framework net10.0
dotnet add ../MyGame/MyGame.csproj reference Electron2D.csproj
cd ../MyGame
```

Em seguida, instale o pacote da plataforma de destino:

| Plataforma | Comando |
| --- | --- |
| Windows | `dotnet add package Electron2D.Windows --prerelease` |
| Linux | `dotnet add package Electron2D.Linux --prerelease` |
| macOS | `dotnet add package Electron2D.MacOS --prerelease` |
| Web | `dotnet add package Electron2D.Web --prerelease` |
| Android e Android TV | `dotnet add package Electron2D.Android --prerelease` |
| iOS | `dotnet add package Electron2D.iOS --prerelease` |
| Apple TV (tvOS) | `dotnet add package Electron2D.tvOS --prerelease` |

Para vários destinos, adicione cada pacote de plataforma necessário. Web, Android, iOS e tvOS precisam das cargas de trabalho do .NET correspondentes e de um projeto de aplicativo específico para a plataforma escolhida. Os pacotes de plataforma não incluem projetos de aplicativo prontos. O exemplo de console abaixo é para Windows, Linux e macOS.

Substitua o conteúdo de `Program.cs` por este código:

```csharp
using Electron2D;

var window = new Window
{
    Title = "Meu jogo",
    Size = new Vector2i(800, 600)
};

return Engine.Run(window);
```

Execute o aplicativo:

```bash
dotnet build -c Release
dotnet run -c Release
```

Adicione objetos do jogo à janela com `AddChild`. O exemplo acima mostra a atualização por quadros e o tratamento do teclado.

A compilação do motor gera `Electron2D.dll`. A publicação do jogo inclui a biblioteca do motor e as dependências nativas; uma publicação autocontida também inclui o runtime do .NET.

<a id="platforms"></a>

## <img src="docs/design/assets/sprite/readme-platforms.svg" width="24" height="28" align="absmiddle" alt=""> Plataformas

A tabela mostra o que foi verificado em cada plataforma do jogo. O editor visual é destinado a Windows, Linux e macOS.

| Plataforma de destino do jogo | Verificado neste repositório |
| --- | --- |
| Windows, x86 / x64 / ARM64 | As suítes completas sem interface e as verificações disponíveis de trimming/AOT passaram no CI. A renderização ainda não foi verificada |
| Linux, x64 / ARM64 | As suítes completas sem interface e as verificações de trimming/AOT passaram em ambas as arquiteturas. Em x64, janelas, entrada e renderização foram verificadas com Wayland, assim como a renderização com XWayland. A renderização em ARM64 ainda não foi verificada |
| macOS, x64 / ARM64 | As suítes completas sem interface e as verificações de trimming/AOT passaram no CI. A renderização ainda não foi verificada |
| Android, celulares e tablets | Renderização, materiais com shaders e física foram verificados em um celular ARM64 |
| Android TV | O renderizador de compatibilidade e a física foram verificados em uma TV de 32 bits |
| iOS e tvOS, dispositivos e simuladores | Os contratos nativos passaram nos quatro perfis de simulador. Os aplicativos para dispositivos são compilados sem assinatura; a execução em dispositivos físicos e a renderização ainda não foram verificadas |
| Navegadores | Renderização e física foram verificadas em um aplicativo de teste separado. A execução de jogos no navegador ainda não foi implementada |

As verificações de Android e navegador cobrem cenários específicos. Consulte a [distribuição de bibliotecas nativas](docs/native-packaging.md) para saber quais estão disponíveis na sua plataforma.

Os indicadores de compilação e testes mostram o estado das verificações automáticas do código e dos aplicativos de teste. A execução em dispositivos reais é verificada separadamente. [Detalhes das verificações automáticas](docs/platform-verification.md#automated-rid-checks).

Consulte os modelos dos dispositivos, os comandos e os limites das verificações no [relatório de plataformas](docs/platform-verification.md).

<a id="development"></a>

## <img src="docs/design/assets/sprite/readme-development.svg" width="24" height="28" align="absmiddle" alt=""> Estado do projeto

O Electron2D está em fase alfa. Sua API pública continua evoluindo e pode mudar entre versões.

Você pode criar cenas em código, salvá-las com `PackedScene` e carregá-las novamente de [arquivos de recursos e cenas](docs/components/resource-files.md). O editor visual mostra por enquanto uma tela inicial; a edição de projetos de jogo ainda não foi implementada.

<a id="documentation"></a>

## <img src="docs/design/assets/sprite/readme-documentation.svg" width="24" height="28" align="absmiddle" alt=""> Documentação

| Você quer | Onde consultar |
| --- | --- |
| Encontrar uma classe ou um método | [Referência da API](docs/inventory.md) |
| Entender um subsistema | [Índice da documentação](docs/README.md) |
| Preparar shaders | [Ferramenta de importação de HLSL e GLSL](tools/shaders/README.md) |

<a id="examples"></a>

Há instruções separadas para reproduzir as verificações de [Android](tests/Electron2D.AndroidProbe/README.md) e [WebGPU](tests/Electron2D.WebGpuProbe/README.md). O teste de WebGPU verifica os recursos do navegador separadamente do motor.

<a id="feedback-and-contributing"></a>

## <img src="docs/design/assets/sprite/readme-contributing.svg" width="24" height="28" align="absmiddle" alt=""> Contribua com o projeto

[Fazer uma pergunta](https://github.com/edwardgushchin/Electron2D/discussions/categories/q-a) · [Relatar um problema](https://github.com/edwardgushchin/Electron2D/issues/new/choose) · [Guia de contribuição](CONTRIBUTING.md) · [Ajuda](SUPPORT.md) · [Código de conduta](CODE_OF_CONDUCT.md) · [Segurança](SECURITY.md)

Relate erros e sugira recursos no [GitHub Issues](https://github.com/edwardgushchin/Electron2D/issues). Ao relatar um erro, informe a versão ou o commit do motor, o sistema operacional e o renderizador. Anexe um exemplo mínimo e a saída do erro.

Envie correções por meio de [pull requests](https://github.com/edwardgushchin/Electron2D/pulls). O processo de trabalho está descrito no [guia de contribuição](CONTRIBUTING.md).

<a id="contributors"></a>

O projeto é mantido por [Eduard Gushchin](https://github.com/edwardgushchin). Todos os autores de alterações estão na [página de colaboradores do GitHub](https://github.com/edwardgushchin/Electron2D/graphs/contributors).

<a id="license"></a>

## <img src="docs/design/assets/sprite/readme-license.svg" width="24" height="28" align="absmiddle" alt=""> Licença

O Electron2D é distribuído sob a [licença MIT](licence/Electron2D-LICENSE.txt). Você pode usar o motor em jogos comerciais; preserve o aviso de direitos autorais e o texto da licença.

As licenças das dependências estão nos [avisos de componentes de terceiros](licence/THIRD_PARTY_NOTICES.md). Ao distribuir o jogo, inclua os textos de licença exigidos por essas dependências a partir do diretório `licence/`.
