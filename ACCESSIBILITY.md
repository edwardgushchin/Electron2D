# Accessibility in Electron2D

Electron2D welcomes contributors and users with different access needs. This statement covers the repository, documentation and current engine UI capabilities. It does not certify a game built with the engine.

## Documentation and contribution channels

Repository guidance is published as Markdown with headings, descriptive links and text code examples. The main README is available in English, Russian, Simplified Chinese, Spanish and Brazilian Portuguese. The logo has an accessible text alternative. GitHub hosts the issue forms, discussions and code review interface.

When changing documentation or visuals:

- Use meaningful headings and link labels, and provide text alternatives for informative images.
- Include commands, errors and instructions as text; avoid conveying required information only in a screenshot or by color.
- Keep diagrams understandable through nearby text and identify the result a reader should expect.
- Follow the [identity guide](docs/design/identity.md) for contrast, typography, target sizes and visible keyboard focus.

## Current runtime scope and limitations

[Control](docs/classes/Control.md) provides focused keyboard input and configurable focus navigation. These APIs do not establish compatibility with assistive technology. Runtime accessibility integration remains a documented gap; an operating-system accessibility or screen-reader bridge is not currently verified. The visual editor is not implemented, so editor accessibility has not been tested.

There has been no formal repository-wide WCAG audit or assistive-technology acceptance run. The [platform report](docs/platform-verification.md) records native and rendered checks; those checks do not prove keyboard-only, screen-reader or other assistive-technology acceptance. Applications must assess their own input, text, audio, contrast, motion and platform accessibility needs.

## Report an accessibility barrier

Use the [documentation form](https://github.com/edwardgushchin/Electron2D/issues/new?template=03-documentation.yml) for inaccessible repository content, or the [bug form](https://github.com/edwardgushchin/Electron2D/issues/new?template=01-bug.yml) for a reproducible engine barrier. For help choosing a workaround, use [Q&A](https://github.com/edwardgushchin/Electron2D/discussions/categories/q-a).

Include the affected page or engine commit, OS, browser or host, assistive technology and version when relevant, reproduction steps, and expected and actual behavior. You do not need to disclose a diagnosis or other personal information. A minimal text description is useful even when a screenshot is unavailable.

Maintainers assess reports through the normal contribution workflow. There is no guaranteed response or fix deadline; accepted improvements require scoped implementation and executable verification.
