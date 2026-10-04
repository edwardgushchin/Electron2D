<p align="right"><a href="README.md">English</a> · <a href="README.ru.md">Русский</a> · <a href="README.zh-CN.md">简体中文</a> · <a href="README.es.md">Español</a> · Português (BR)</p>

<p align="center">
  <picture>
    <source media="(prefers-color-scheme: dark) and (max-width: 480px)" srcset="docs/design/assets/sprite/logo-compact-dark.svg">
    <source media="(prefers-color-scheme: light) and (max-width: 480px)" srcset="docs/design/assets/sprite/logo-compact-light.svg">
    <source media="(prefers-color-scheme: dark)" srcset="docs/design/assets/sprite/logo-primary-dark.svg">
    <source media="(prefers-color-scheme: light)" srcset="docs/design/assets/sprite/logo-primary-light.svg">
    <img alt="Electron2D — motor de jogos 2D multiplataforma criado para agentes" src="docs/design/assets/sprite/logo-primary-light.svg" width="900">
  </picture>
</p>

<p align="center">
  <a href="https://github.com/edwardgushchin/Electron2D/graphs/contributors">Colaboradores</a> ·
  <a href="https://github.com/edwardgushchin/Electron2D/commits/main">Commits</a> ·
  <a href="licence/Electron2D-LICENSE.txt">Licença MIT</a>
</p>

<p align="center">
  <img alt="Criado para agentes · Multiplataforma · 2D · Em desenvolvimento" src="docs/design/assets/sprite/readme-badges.svg" width="405">
</p>

<p align="center">
  <a href="#about">Sobre</a> ·
  <a href="#features">Recursos</a> ·
  <a href="#platforms">Plataformas</a> ·
  <a href="#installation">Instalação</a> ·
  <a href="#quick-start">Início rápido</a> ·
  <a href="#documentation">Documentação</a> ·
  <a href="#examples">Exemplos</a> ·
  <a href="#feedback-and-contributing">Contato</a> ·
  <a href="#license">Licença</a>
</p>

<p align="center">
  ⭐ <a href="https://github.com/edwardgushchin/Electron2D">Dê uma estrela ao projeto no GitHub</a> para acompanhar seu desenvolvimento.
</p>

<a id="about"></a>

## 🧭 Sobre

Electron2D é um **motor de jogos 2D agent-native e multiplataforma**.

Ele foi projetado para que desenvolvedores e agentes de programação trabalhem nos mesmos jogos por meio de operações programáticas documentadas: criar e editar projetos, compilar, executar e verificar os resultados. As alterações e verificações devem poder ser revisadas pelo desenvolvedor. A [arquitetura voltada a agentes](docs/decisions/agent-native.md#adr-0090) define operações de criação compartilhadas pela CLI e pelo editor, simulação sem interface gráfica e verificação em lote da saída renderizada.

Uma API pública única do motor atende jogos para desktop, dispositivos móveis, TV e navegador. Crie cenas com nós e recursos usando renderização, entrada, física, áudio e interface gráfica no mesmo motor.

**Estado do desenvolvimento:** o motor, os exemplos, os testes e a documentação da API estão em desenvolvimento ativo. O editor visual, uma CLI unificada para projetos, a persistência de cenas em arquivos e um fluxo público de captura de imagens ainda não foram implementados. As plataformas previstas e as execuções verificadas estão relacionadas separadamente abaixo.

<a id="features"></a>

## ✨ Recursos

- **Fluxo de trabalho com agentes** — O projeto prevê que agentes de programação possam inspecionar e editar cenas, recursos e configurações do projeto, além de compilar, executar e verificar jogos por meio de operações programáticas documentadas. As ferramentas pendentes constam no [roteiro de implementação](docs/coverage/index.md).
- **Motor multiplataforma** — Uma API para jogos de desktop, dispositivos móveis, TV e navegador. Consulte as [plataformas previstas e verificações atuais](#platforms).
- **Cenas baseadas em nós** — Hierarquias de `Node`, `CanvasItem` e `Entity`, agendamento de cenas, temporizadores, interpolações e instâncias de `PackedScene` reutilizáveis em memória.
- **Renderização 2D** — Sprites, animação, câmeras, texturas, texto, desenho em canvas e materiais de shaders HLSL/GLSL com API tipada. Consulte os [recursos e limites de renderização](docs/domains/rendering.md).
- **Componentes de interface** — Controles, contêineres, rótulos, botões, entrada de texto, navegação por foco e temas tipados. Consulte o [domínio de cenas](docs/domains/scene.md).
- **Física 2D** — Corpos, áreas, formas de colisão, consultas e juntas, com simulação em passo fixo. Consulte o [domínio de física](docs/domains/physics.md).
- **Áudio** — Reprodução de WAV, MP3 e Ogg Vorbis, fluxos procedurais, barramentos de saída e APIs de gravação. Consulte o [comportamento do áudio e os limites de verificação](docs/domains/audio.md).
- **Recursos e E/S** — Imagens, fontes, carregamento de recursos, configuração tipada, acesso a arquivos e localização. Consulte os domínios de [recursos](docs/domains/resources.md) e [núcleo](docs/domains/core.md).
- **Lógica de jogo em C#** — Escreva classes C# comuns com propriedades, recursos e eventos usando as ferramentas .NET habituais.

Esta lista reúne áreas de funcionalidade implementadas e a direção do produto. Os documentos vinculados registram as lacunas restantes na API e nos backends.

<a id="platforms"></a>

## 🖥️ Plataformas

| Plataforma | Objetivo do editor | Objetivo do motor | Verificação atual |
| --- | --- | --- | --- |
| Windows | Planejado | x86, x64, ARM64 | Pacotes nativos mapeados; execução no Windows ainda não verificada |
| Linux | Planejado; X11 e Wayland | x64, ARM64 | Verificados o host e a renderização no Linux x64 Wayland e o renderizador via XWayland; execução em ARM64 pendente |
| macOS | Planejado | x64, ARM64 | Pacotes nativos mapeados; execução no macOS ainda não verificada |
| Android | — | ABIs de celulares e tablets | Canvas, shaders GPU e física CPU verificados em celular ARM64; outros dispositivos e cenários ainda precisam de verificação |
| Android TV | — | ABIs do Android | Renderização alternativa do canvas e física CPU verificadas em TV de 32 bits; o aparelho não tem backend Vulkan |
| iOS | — | RIDs de dispositivo e simulador | Pacotes nativos mapeados; CI preparada para compilar a biblioteca no macOS, mas ainda não executada |
| tvOS | — | RIDs de dispositivo e simulador | Pacotes nativos mapeados; CI preparada para compilar a biblioteca no macOS, mas ainda não executada |
| Web | — | `browser-wasm` | Sondas isoladas de canvas/física e WebGPU independente; host e backend de navegador do produto pendentes |

O editor é destinado a sistemas desktop. A existência de um pacote nativo ou uma compilação bem-sucedida não comprovam, por si só, o suporte a uma plataforma. As verificações do Android cobrem apenas os aparelhos e caminhos gráficos e físicos citados; ciclo de vida, entrada, áudio, armazenamento e empacotamento de lançamento exigem verificações próprias.

A [matriz de verificação de plataformas](docs/platform-verification.md) registra aparelhos, renderizadores, comandos e limites. Linux Wayland é o ambiente de verificação nativa obrigatório no momento. Materiais de shader exigem o caminho GPU; o renderizador de compatibilidade rejeita explicitamente materiais incompatíveis.

<a id="installation"></a>

## 📦 Instalação

Instale o **SDK do .NET 10** e clone e compile o motor a partir da raiz do repositório:

```bash
git clone https://github.com/edwardgushchin/Electron2D.git
cd Electron2D
dotnet build Electron2D.csproj -c Release
```

O motor é compilado como **`Electron2D.dll`**. Referencie `Electron2D.csproj` no projeto do seu aplicativo. O projeto fornece as dependências de plataforma durante a restauração e a publicação. Uma publicação autossuficiente contém o executável do jogo, `Electron2D.dll`, o ambiente .NET e as bibliotecas nativas aplicáveis.

<a id="quick-start"></a>

## 🚀 Início rápido

Execute o exemplo existente de **janela e entrada** no Linux Wayland:

```bash
dotnet publish examples/HostExample/HostExample.csproj -c Release -r linux-x64 --self-contained true -o /tmp/electron2d-host-example
/tmp/electron2d-host-example/HostExample
```

Mantenha as teclas de direção pressionadas para mover o nó da cena; pressione Escape ou feche a janela para sair. O exemplo informa o movimento no terminal e não desenha a cena.

Seu [ponto de entrada](examples/HostExample/Program.cs) configura `Window`, adiciona a cena e chama `Engine.Run`. O motor cuida dos eventos, do tempo dos quadros e do encerramento. Consulte o [guia do exemplo](examples/HostExample/README.md) para ver o fluxo completo.

<a id="documentation"></a>

## 📚 Documentação

- **[Índice da documentação](docs/README.md)** — Guias de domínios e componentes.
- **[Referência da API](docs/inventory.md)** — Tipos de produção implementados e páginas de suas classes.
- **[Arquitetura](docs/decisions/index.md)** — Decisões atuais sobre produto e motor.
- **[Roteiro de implementação](docs/coverage/index.md)** — Recursos implementados, adaptados e ausentes.
- **[Verificação de plataformas](docs/platform-verification.md)** — Evidências de execução e limites das plataformas.
- **[Identidade visual](docs/design/identity.md)** — Direção Sprite aprovada, arquivos do logotipo, cores e tipografia.

<a id="examples"></a>

## 🎮 Exemplos

- **[Janela e entrada](examples/HostExample/README.md)** — Exemplo executável que usa apenas a API pública, com janela, nó de cena, teclado e saída limpa.

A [sonda para dispositivo Android](tests/Electron2D.AndroidProbe/README.md) e a [sonda de shaders no navegador](tests/Electron2D.WebGpuProbe/README.md) são ferramentas de verificação em `tests/`, não exemplos de jogos. Elas documentam separadamente os caminhos gráficos e físicos testados.

<a id="feedback-and-contributing"></a>

## 💬 Contato e contribuições

Use o [GitHub Issues](https://github.com/edwardgushchin/Electron2D/issues) para relatar bugs, sugerir recursos ou comentar o design. Ao relatar um problema do motor, informe a revisão, a plataforma, o renderizador e um exemplo mínimo que o reproduza.

[Pull requests](https://github.com/edwardgushchin/Electron2D/pulls) são bem-vindos. Antes de alterar o comportamento, leia o [guia de manutenção](docs/maintaining.md) e as decisões arquiteturais pertinentes. Atualize a documentação da API e as verificações junto com a mudança.

Execute as verificações a partir da raiz do repositório:

```bash
dotnet run --project tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release
tools/coverage/check.sh
```

<a id="contributors"></a>

## 👥 Colaboradores

Electron2D é mantido por Eduard Gushchin. Consulte o [gráfico de colaboradores](https://github.com/edwardgushchin/Electron2D/graphs/contributors) do repositório.

<a id="license"></a>

## 📄 Licença

O código próprio do Electron2D é distribuído sob a [licença MIT](licence/Electron2D-LICENSE.txt). As dependências preservam suas próprias licenças; consulte os [avisos de terceiros](licence/THIRD_PARTY_NOTICES.md). Os textos das licenças ficam em `licence/` e acompanham as publicações de aplicativos quando aplicável.
