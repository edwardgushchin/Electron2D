"""Compile only the memory-query ABI of the pinned SDL GPU implementation."""

import argparse
import hashlib
from pathlib import Path
import re

SOURCES = {
    "src/gpu/SDL_sysgpu.h": "86f74c57f00529fc9531f680340be114414688c4809b724eac894d862f86fb6e",
    "src/gpu/vulkan/SDL_gpu_vulkan.c": "28b42c4286805a6cff952bd698de29659180132ae881eedd3f6bf24d16583361",
}


def block(text, marker):
    start = text.index(marker)
    brace = text.index("{", start)
    depth = 0
    for index in range(brace, len(text)):
        depth += (text[index] == "{") - (text[index] == "}")
        if depth == 0:
            return text[start:text.index(";", index) + 1]
    raise ValueError("Incomplete native declaration: " + marker)


def generate(source):
    for name, expected in SOURCES.items():
        if hashlib.sha256((source / name).read_bytes()).hexdigest() != expected:
            raise ValueError("The SDL memory ABI source differs from pinned 3.4.18: " + name)
    gpu = (source / "src/gpu/SDL_sysgpu.h").read_text(encoding="utf-8")
    vulkan = (source / "src/gpu/vulkan/SDL_gpu_vulkan.c").read_text(encoding="utf-8")
    renderer = vulkan[vulkan.index("struct VulkanRenderer\n{"):
                      vulkan.index("    SDL_Mutex *allocatorLock;") + len("    SDL_Mutex *allocatorLock;")] + "\n};"
    pointers = sorted(set(re.findall(r"^    ([A-Za-z_]\w*) \*", renderer, re.M)) -
                      {"SDL_Mutex", "VulkanMemoryAllocator"})
    forward = "\n".join("typedef struct " + name + " " + name + ";"
                        for name in pointers if not name.startswith("Vk"))
    return "\n".join([
        vulkan[:vulkan.index('#include "SDL_internal.h"')].rstrip(),
        "/* Generated from pinned SDL 3.4.18. */",
        "#include <SDL3/SDL.h>", "#include <SDL3/SDL_revision.h>",
        "#define VK_NO_PROTOTYPES", "#define VK_ENABLE_BETA_EXTENSIONS",
        '#include "vulkan/vulkan.h"',
        "typedef struct SDL_GPURenderer SDL_GPURenderer;",
        block(gpu, "struct SDL_GPUDevice\n{"),
        "typedef struct VulkanBuffer VulkanBuffer;", "typedef struct VulkanTexture VulkanTexture;",
        "typedef struct VulkanMemoryAllocation VulkanMemoryAllocation;",
        "typedef struct VulkanFenceHandle VulkanFenceHandle;",
        vulkan[vulkan.index("typedef struct VulkanMemoryFreeRegion"):vulkan.index("// Memory structures")],
        block(vulkan, "typedef struct VulkanExtensions"), block(vulkan, "typedef struct VulkanFencePool"),
        forward, renderer, "typedef struct VulkanRenderer VulkanRenderer;", "",
    ])


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    text = generate(args.source)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(text, encoding="utf-8")


if __name__ == "__main__":
    main()
