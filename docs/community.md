# GitHub community structure

Last updated: 2026-10-05

This page describes the repository's contribution channels and their configuration. Runtime behavior, roadmap scope and platform support remain in their owning documents.

## Repository entry points

The five README editions preserve the Sprite identity and provide links to questions, issue forms, contributions, support, conduct and security. The [root license](../LICENSE) is a byte-for-byte copy of the canonical [Electron2D license](../licence/Electron2D-LICENSE.txt), allowing GitHub to recognize the existing MIT terms. Keep both copies synchronized; third-party notices retain their separate licenses.

[CONTRIBUTING.md](../CONTRIBUTING.md) explains checkout, scope, validation and review. [SUPPORT.md](../SUPPORT.md) routes questions and reports. [CODE_OF_CONDUCT.md](../CODE_OF_CONDUCT.md) defines participation and moderation. [SECURITY.md](../SECURITY.md) routes vulnerabilities to private reporting. These files are at the repository root so GitHub exposes them in its community profile and navigation.

## Issues and pull requests

Four native GitHub forms under `.github/ISSUE_TEMPLATE/` cover bugs, feature requests, documentation corrections and measured performance problems. Bugs and performance reports require a revision, environment and reproduction; feature requests require a use case and proposed behavior; documentation reports require an affected page. Blank issues are disabled. The chooser links to Q&A, Ideas and private security reports.

The PR template asks for the problem, resulting behavior, exact verification and affected documentation. `.github/CODEOWNERS` routes review to the repository owner. It does not independently enforce a branch protection rule.

Existing labels are retained. Additional label families are:

| Family | Meaning |
| --- | --- |
| `performance` | A measured performance or allocation problem |
| `status: needs triage` | Incoming report awaiting classification |
| `status: confirmed` | Reproduced, accepted scope |
| `status: needs information` | Missing reproduction or environment evidence |
| `status: blocked` | Waiting on a named dependency or decision |
| `priority: P0` through `priority: P3` | Critical, high, normal and low maintainer priorities |
| `area:` | Core, scene, rendering, GUI, physics, audio, input, navigation, networking, resources, tooling or build |
| `platform:` | Linux, Windows, macOS, Android, Apple mobile or browser |

Maintainers assign severity and applicable area/platform labels after reviewing evidence. Keep one current status and one priority per issue; area/platform labels may be combined. The area selected in a form is report data, not an automatic label assignment.

## Discussions

| Section | Category | Format and purpose |
| --- | --- | --- |
| News | Announcements | Maintainer announcements and links to verified updates |
| Help and learning | Q&A | Questions with accepted answers |
| Help and learning | Guides and tutorials | Reproducible tutorials with prerequisites and expected results |
| Community | General | Community conversation |
| Community | Show and tell | Games, experiments, media and lessons learned |
| Development | Ideas | Early proposals and alternatives |
| Development | Development | Technical design and implementation discussions |
| Development | Polls | Native voting on community questions |

Seven category forms under `.github/DISCUSSION_TEMPLATE/` match the category slugs exactly. Polls uses GitHub's native poll composer. Creating or editing a category/section requires GitHub's management UI; the YAML files configure forms, not the category list.

## Development project

[Electron2D Development](https://github.com/users/edwardgushchin/projects/3) is public and linked to the repository's Projects tab. All work provides a table view; Board provides a board view. Its statuses are Triage, Backlog, In progress, In review and Done. Five initial draft cards point to existing documented gaps: agent-native workflows, native target integration, 2D renderer integration, asset workflows and the visual editor. They have no dates or implementation commitment. The [coverage roadmap](coverage/index.md), [decisions](decisions/index.md) and [platform report](platform-verification.md) remain authoritative.

The generated Wiki Home and sidebar link to getting started and Q&A; Home also links to the contribution guide. Its public API reference is generated from the assembly and XML, retaining the existing overview/syntax/member-reference format.

## Releases, automation and security

`.github/release.yml` groups future generated release notes into features, fixes, performance, documentation, build/tooling and other changes. There is no published stable release yet. This setup does not publish a package. Release contents and support claims need their own release checks; a community setup change does not publish an engine release.

The existing Build and Tests workflows remain responsible for executable CI. This setup does not alter their matrix or equate GitHub community completeness with passing runtime checks. Merged PR branches are automatically deleted and GitHub offers an update-branch action when their base changes.

Private vulnerability reporting and dependency vulnerability alerts are enabled. Secret scanning and push protection remain enabled. The private form is `.github/VULNERABILITY_REPORT.yml`. Security findings use the private channel rather than public issue templates.

The [Sprite social card](design/assets/sprite/github-social-preview.png) has an editable SVG and is designed for GitHub's 1280×640 link preview. It is uploaded in repository settings; committing the artwork alone does not change that setting. GitHub Pages, a package publication and sponsorship links require actual deployable artifacts or an owner-provided destination; their absence does not imply those services exist.

## Maintenance checks

After changing forms, validate their YAML against [GitHub's form schema](https://docs.github.com/en/communities/using-templates-to-encourage-useful-issues-and-pull-requests/syntax-for-githubs-form-schema), check referenced labels and category slugs against the live repository, and inspect the issue chooser and rendered forms on the default branch. Check all five README link sets and confirm the community profile detects its files. Follow the mandatory Release, coverage and Wiki checks in [AGENTS.md](../AGENTS.md) before committing repository changes.
