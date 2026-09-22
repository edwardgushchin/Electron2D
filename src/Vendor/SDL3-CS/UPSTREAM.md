# SDL3-CS core, shadercross and image binding source

Source: https://github.com/edwardgushchin/SDL3-CS
Release: v3.4.16.1
Commit: f2d8303906a1eb2591e77a155b37cab2171ee7d2

The complete upstream SDL3-CS/SDL, SDL3-CS/ShaderCross and SDL3-CS/Image trees are compiled into Electron2D.dll.
Local adaptation makes top-level binding types internal and suppresses CS0649 on
the native-initialized storage callback table. The upstream license is retained.
Refresh with tools/update-sdl3-cs.sh and a release tag, then inspect the diff and
run the engine, test, coverage, and native example checks.
