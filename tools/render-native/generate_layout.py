"""Compile the bounded private ABI of the pinned SDL GPU implementation."""

import argparse
import hashlib
from pathlib import Path
import re

SOURCES = {
    "src/gpu/vulkan/SDL_gpu_vulkan_vkfuncs.h": "75921837d29c19f4c3b3b0e66e3c1db97a589cb2e928985c4e844948857e1258",
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
    functions = (source / "src/gpu/vulkan/SDL_gpu_vulkan_vkfuncs.h").read_text(encoding="utf-8")
    renderer = block(vulkan, "struct VulkanRenderer\n{").replace('#include "SDL_gpu_vulkan_vkfuncs.h"', functions)
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
        gpu[gpu.index("#define MAX_TEXTURE_SAMPLERS_PER_STAGE"):gpu.index("typedef struct TextureCommonHeader")],
        block(gpu, "typedef struct TextureCommonHeader"),
        block(gpu, "typedef struct GraphicsPipelineCommonHeader"),
        "typedef struct VulkanBuffer VulkanBuffer;", "typedef struct VulkanTexture VulkanTexture;",
        "typedef struct VulkanMemoryAllocation VulkanMemoryAllocation;",
        "typedef struct VulkanFenceHandle VulkanFenceHandle;",
        vulkan[vulkan.index("typedef struct VulkanMemoryFreeRegion"):vulkan.index("// Memory structures")],
        "#ifdef E2D_RENDER_FILTERING", block(vulkan, "static VkFormat SDLToVK_TextureFormat[]"), "#endif",
        block(vulkan, "typedef struct VulkanSampler"),
        block(vulkan, "typedef struct VulkanExtensions"), block(vulkan, "typedef struct VulkanFencePool"),
        forward, renderer, "typedef struct VulkanRenderer VulkanRenderer;",
        block(vulkan, "typedef struct VulkanShader\n{"),
        re.search(r"typedef uint32_t DescriptorSetLayoutID;", vulkan).group(0),
        block(vulkan, "typedef struct DescriptorSetLayout\n{"),
        block(vulkan, "typedef struct VulkanGraphicsPipelineResourceLayout\n{"),
        block(vulkan, "typedef struct VulkanGraphicsPipeline\n{"),

        vulkan[vulkan.index("typedef struct VulkanCommandBuffer\n{"):vulkan.index("    VkCommandBuffer commandBuffer;") + len("    VkCommandBuffer commandBuffer;")] + "\n} VulkanCommandBuffer;", "",
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
