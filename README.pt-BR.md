<p align="right"><a href="README.md">English</a> · <a href="README.ru.md">Русский</a> · <a href="README.zh-CN.md">简体中文</a> · <a href="README.es.md">Español</a> · Português (BR)</p>

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

## <img src="docs/design/assets/sprite/readme-about.svg" width="24" height="24" align="texttop" alt=""> Sobre o projeto

Electron2D é um **motor 2D livre e multiplataforma em C# para desenvolver jogos em colaboração com agentes de IA**.

Crie mundos e mecânicas de jogo com as ferramentas habituais do .NET e assistentes de IA como Codex ou Claude Code.

<a id="features"></a>

## <img src="docs/design/assets/sprite/readme-features.svg" width="24" height="24" align="texttop" alt=""> Recursos

- [Gráficos](docs/domains/rendering.md). Sprites e atlas, câmeras, paralaxe, desenho de formas e texto. Importação de shaders HLSL e GLSL para materiais.
- [Cenas e animação](docs/domains/scene.md). Objetos e fases reutilizáveis, animação por quadros, animação de propriedades e temporizadores.
- [Física](docs/domains/physics.md). Corpos rígidos, colisões, áreas, consultas de interseção, articulações e molas.
- [Interface de jogo](docs/domains/scene.md). Botões, campos de texto, rolagem, contêineres de layout, fontes e temas.
- [Áudio](docs/domains/audio.md). WAV, MP3 e Ogg Vorbis, áudio posicional, mixagem, efeitos e gravação.
- [Controles](docs/domains/input.md). Teclado, mouse, toque e controles de jogo. Associação de entradas a ações do jogo.
- [Busca de caminhos](docs/domains/navigation.md). Rotas em uma grade ou entre pontos definidos, considerando obstáculos e custos de deslocamento.
- [Recursos](docs/domains/resources.md). Carregamento de imagens, fontes e áudio. Gradientes, curvas e texturas procedurais.
- [Localização](docs/domains/localization.md). Traduções, formas de plural e seleção de idioma.
- [Rede](docs/domains/networking.md). TCP, UDP, sockets locais e conexões criptografadas com TLS.

Materiais com shaders exigem o renderizador GPU. O renderizador de compatibilidade oferece gráficos 2D básicos. Consulte os links acima para ver os detalhes e as limitações.

<a id="quick-start"></a>

## <img src="docs/design/assets/sprite/readme-quick-start.svg" width="24" height="24" align="texttop" alt=""> Primeiros passos

Comece pelo exemplo «Janela e entrada». Estes comandos são para Linux x64 com Wayland.

<a id="installation"></a>

### Requisitos

- [SDK do .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0) e Git.

Os componentes nativos privados são restaurados como dependências do NuGet. Consulte a [distribuição dos pacotes nativos](docs/native-packaging.md) para a disponibilidade do primeiro pacote e as instruções de recompilação nativa completa.

### Compilar e executar

Clone o repositório e compile a biblioteca:

```bash
git clone https://github.com/edwardgushchin/Electron2D.git
cd Electron2D
dotnet build Electron2D.csproj -c Release
```

Publique o exemplo com seu próprio runtime do .NET e execute-o:

```bash
dotnet publish examples/HostExample/HostExample.csproj -c Release -r linux-x64 --self-contained true -o /tmp/electron2d-host-example
/tmp/electron2d-host-example/HostExample
```

Uma janela será aberta. As setas movem um objeto do jogo, e suas coordenadas aparecem no terminal. Escape ou fechar a janela encerra o aplicativo. Este exemplo não desenha a cena; mostra a inicialização do motor e o tratamento da entrada.

[Código do exemplo](examples/HostExample/Program.cs) · [Instruções de execução](examples/HostExample/README.md)

### Usar Electron2D no seu jogo

Crie um projeto de console do .NET 10 ao lado do diretório `Electron2D` e adicione uma referência ao projeto do motor. Execute estes comandos a partir da raiz do repositório:

```bash
dotnet new console -n MyGame -o ../MyGame --framework net10.0
dotnet add ../MyGame/MyGame.csproj reference Electron2D.csproj
```

Substitua o conteúdo de `MyGame/Program.cs` por este código:

```csharp
using Electron2D;

var window = new Window
{
    Title = "Meu jogo",
    Size = new Vector2i(960, 540)
};

return Engine.Run(window);
```

Execute o aplicativo:

```bash
dotnet run --project ../MyGame/MyGame.csproj -c Release
```

Adicione objetos do jogo à janela com `AddChild`. O exemplo acima mostra a atualização por quadros e o tratamento do teclado.

A compilação do motor gera `Electron2D.dll`. A publicação do jogo inclui a biblioteca do motor e as dependências nativas; uma publicação autocontida também inclui o runtime do .NET.

<a id="platforms"></a>

## <img src="docs/design/assets/sprite/readme-platforms.svg" width="24" height="24" align="texttop" alt=""> Plataformas

As plataformas de destino do jogo e as verificações realizadas são apresentadas separadamente. O editor visual é destinado a Windows, Linux e macOS.

| Plataforma de destino do jogo | Verificado neste repositório |
| --- | --- |
| Windows, x86 / x64 / ARM64 | A execução ainda não foi verificada |
| Linux, x64 / ARM64 | Em x64, janelas, entrada e renderização foram verificadas com Wayland. A renderização também foi verificada com XWayland. ARM64 ainda não foi verificado |
| macOS, x64 / ARM64 | A execução ainda não foi verificada |
| Android, celulares e tablets | Renderização, materiais com shaders e física foram verificados em um celular ARM64 |
| Android TV | O renderizador de compatibilidade e a física foram verificados em uma TV de 32 bits |
| iOS e tvOS, dispositivos e simuladores | A compilação automática da biblioteca foi preparada. As verificações em dispositivos ainda não foram realizadas |
| Navegadores | Renderização e física foram verificadas em um aplicativo de teste separado. A execução de jogos no navegador ainda não foi implementada |

O conjunto completo de bibliotecas nativas de texto e áudio foi compilado para Linux x64. A compilação e a integração nas outras plataformas continuam sendo tarefas separadas. As verificações de Android e navegador cobrem cenários específicos.

Os indicadores abrangem os 18 RID: compilações com analisadores, testes completos sem janela no Linux ou portáveis em desktop e aplicativos de contrato trimmed/AOT, Android, simuladores Apple e navegador. Dispositivos Apple físicos e a aceitação nativa completa de outras plataformas continuam separados. Veja o [escopo do CI](docs/platform-verification.md#automated-rid-checks).

Consulte os modelos dos dispositivos, os comandos e os limites das verificações no [relatório de plataformas](docs/platform-verification.md).

<a id="development"></a>

## <img src="docs/design/assets/sprite/readme-development.svg" width="24" height="24" align="texttop" alt=""> Desenvolvimento do motor

Atualmente, você pode usar o Electron2D por meio de C# e .NET. Os modelos `PackedScene` oferecem [arquivos tipados de recursos e cenas](docs/components/resource-files.md), incluindo carregamento em um novo processo. O editor visual e os comandos para gerenciar projetos de jogo continuam planejados.

A colaboração com IA faz parte da [arquitetura do motor](docs/decisions/agent-native.md#adr-0090): operações de projeto, execução de cenários de jogo e verificação de imagens devem estar disponíveis por meio de ferramentas documentadas. O conjunto completo dessas ferramentas ainda precisa ser implementado.

As próximas tarefas e o estado dos métodos estão no [plano de desenvolvimento](docs/coverage/index.md). O comportamento implementado está descrito na referência da API.

<a id="documentation"></a>

## <img src="docs/design/assets/sprite/readme-documentation.svg" width="24" height="24" align="texttop" alt=""> Documentação

| Você quer | Onde consultar |
| --- | --- |
| Encontrar uma classe ou um método | [Referência da API](docs/inventory.md) |
| Entender um subsistema | [Índice da documentação](docs/README.md) |
| Preparar shaders | [Ferramenta de importação de HLSL e GLSL](tools/shaders/README.md) |
| Entender a arquitetura e as decisões | [Decisões de arquitetura](docs/decisions/index.md) |
| Escolher uma tarefa de desenvolvimento | [Plano de desenvolvimento](docs/coverage/index.md) |

<a id="examples"></a>

Há instruções separadas para reproduzir as verificações de [Android](tests/Electron2D.AndroidProbe/README.md) e [WebGPU](tests/Electron2D.WebGpuProbe/README.md). O teste de WebGPU verifica os recursos do navegador separadamente do motor.

<a id="feedback-and-contributing"></a>

## <img src="docs/design/assets/sprite/readme-contributing.svg" width="24" height="24" align="texttop" alt=""> Contribua com o projeto

[Fazer uma pergunta](https://github.com/edwardgushchin/Electron2D/discussions/categories/q-a) · [Relatar um problema](https://github.com/edwardgushchin/Electron2D/issues/new/choose) · [Guia de contribuição](CONTRIBUTING.md) · [Ajuda](SUPPORT.md) · [Código de conduta](CODE_OF_CONDUCT.md) · [Segurança](SECURITY.md)

Relate erros e sugira recursos no [GitHub Issues](https://github.com/edwardgushchin/Electron2D/issues). Ao relatar um erro, informe a versão ou o commit do motor, o sistema operacional e o renderizador. Anexe um exemplo mínimo e a saída do erro.

Envie correções por meio de [pull requests](https://github.com/edwardgushchin/Electron2D/pulls). Antes de começar, leia o [guia de manutenção](docs/maintaining.md) e as decisões de arquitetura sobre o assunto. Atualize código, testes e documentação juntos.

Execute as verificações principais a partir da raiz do repositório:

```bash
dotnet run --project tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release
tools/coverage/check.sh
```

<a id="contributors"></a>

O projeto é mantido por [Eduard Gushchin](https://github.com/edwardgushchin). Todos os autores de alterações estão na [página de colaboradores do GitHub](https://github.com/edwardgushchin/Electron2D/graphs/contributors).

<a id="license"></a>

## <img src="docs/design/assets/sprite/readme-license.svg" width="24" height="24" align="texttop" alt=""> Licença

O Electron2D é distribuído sob a [licença MIT](licence/Electron2D-LICENSE.txt). Você pode usar o motor em jogos comerciais; preserve o aviso de direitos autorais e o texto da licença.

As licenças das dependências estão nos [avisos de componentes de terceiros](licence/THIRD_PARTY_NOTICES.md). Ao distribuir o jogo, inclua os textos de licença exigidos por essas dependências a partir do diretório `licence/`.
