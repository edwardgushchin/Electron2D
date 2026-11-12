"""Compile the bounded private ABI of the pinned SDL GPU implementation."""

import argparse
import hashlib
from pathlib import Path
import re

SOURCES = {
    "src/gpu/vulkan/SDL_gpu_vulkan_vkfuncs.h": "75921837d29c19f4c3b3b0e66e3c1db97a589cb2e928985c4e844948857e1258",
    "src/gpu/d3d12/SDL_gpu_d3d12.c": "6cd3964004aff0aef48b2959131fb0eb6f3ad8dfd1d9befc571385f7e00c1e4d",
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


def prefix(text, marker, last, alias=""):
    declaration = block(text, marker)
    end = declaration.index(last) + len(last)
    return declaration[:end] + "\n}" + (" " + alias if alias else "") + ";"


def d3d12_layout(source):
    text = (source / "src/gpu/d3d12/SDL_gpu_d3d12.c").read_text(encoding="utf-8")
    return [
        "#if defined(E2D_RENDER_DRIVER_D3D12) && defined(SDL_PLATFORM_WIN32)",
        "#define __REQUIRED_RPCNDR_H_VERSION__ 475",
        "#include <d3d12.h>", "#include <dxgi1_6.h>", "#include <dxgidebug.h>",
        "#ifdef __IDXGIInfoQueue_INTERFACE_DEFINED__", "#define HAVE_IDXGIINFOQUEUE", "#endif",
        "#define USE_PIX_RUNTIME",
        *re.findall(r"^typedef void\(WINAPI\* pfn(?:BeginEventOnCommandList|EndEventOnCommandList|SetMarkerOnCommandList)\).*;", text, re.M),
        block(text, "typedef struct WinPixEventRuntimeFns"),
        prefix(text, "struct D3D12Renderer\n{", "ID3D12CommandQueue *commandQueue;"),
        "typedef struct D3D12Renderer D3D12Renderer;",
        "typedef struct D3D12Texture D3D12Texture;",
        "typedef struct D3D12TextureSubresource D3D12TextureSubresource;",
        "typedef struct D3D12DescriptorHeap D3D12DescriptorHeap;",
        "typedef struct D3D12StagingDescriptorPool D3D12StagingDescriptorPool;",
        "typedef struct D3D12StagingDescriptor D3D12StagingDescriptor;",
        block(text, "struct D3D12StagingDescriptor\n{"),
        block(text, "typedef struct D3D12TextureContainer\n{"),
        block(text, "typedef struct D3D12TextureSubresource\n{"),
        block(text, "struct D3D12Texture\n{"),
        block(text, "typedef struct D3D12Sampler\n{"),
        block(text, "typedef enum D3D12BufferType"),
        "typedef struct D3D12Buffer D3D12Buffer;",
        "typedef struct D3D12BufferContainer D3D12BufferContainer;",
        prefix(text, "struct D3D12BufferContainer\n{", "D3D12Buffer *activeBuffer;"),
        prefix(text, "struct D3D12Buffer\n{", "ID3D12Resource *handle;"),
        prefix(text, "struct D3D12GraphicsPipeline\n{", "ID3D12PipelineState *pipelineState;"),
        prefix(text, "struct D3D12ComputePipeline\n{", "ID3D12PipelineState *pipelineState;"),
        block(text, "static DXGI_FORMAT SDLToD3D12_TextureFormat[]"),
        block(text, "static DXGI_FORMAT SDLToD3D12_DepthFormat[]"),
        "#undef USE_PIX_RUNTIME", "#undef HAVE_IDXGIINFOQUEUE", "#endif", "",
    ]


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
    pointers = sorted(set(pointers) | {"VulkanCommandPool", "VulkanComputePipeline", "VulkanPresentData", "VulkanUniformBuffer"})
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
        block(gpu, "typedef struct ComputePipelineCommonHeader"),
        "typedef struct VulkanBuffer VulkanBuffer;", "typedef struct VulkanBufferContainer VulkanBufferContainer;", "typedef struct VulkanUniformBuffer VulkanUniformBuffer;", "typedef struct VulkanTexture VulkanTexture;",
        "typedef struct VulkanTextureContainer VulkanTextureContainer;",
        "typedef struct VulkanMemoryAllocation VulkanMemoryAllocation;",
        "typedef struct VulkanFenceHandle VulkanFenceHandle;",
        vulkan[vulkan.index("typedef struct VulkanMemoryFreeRegion"):vulkan.index("// Memory structures")],
        block(vulkan, "typedef enum VulkanBufferType"), block(vulkan, "struct VulkanBuffer\n{"), block(vulkan, "struct VulkanBufferContainer\n{"), block(vulkan, "struct VulkanUniformBuffer\n{"),
        block(vulkan, "typedef struct VulkanTextureSubresource\n{"),
        block(vulkan, "struct VulkanTexture\n{"),
        block(vulkan, "struct VulkanTextureContainer\n{"),
        "#ifdef E2D_RENDER_FILTERING", block(vulkan, "static VkFormat SDLToVK_TextureFormat[]"), "#endif",
        block(vulkan, "typedef struct VulkanSampler"),
        block(vulkan, "typedef struct VulkanExtensions"), block(vulkan, "typedef struct VulkanFencePool"),
        re.search(r"#define MAX_UBO_SECTION_SIZE\s+\d+", vulkan).group(0),
        forward, renderer, "typedef struct VulkanRenderer VulkanRenderer;",
        block(vulkan, "typedef struct VulkanShader\n{"),
        re.search(r"typedef uint32_t DescriptorSetLayoutID;", vulkan).group(0),
        re.search(r"#define DESCRIPTOR_POOL_SIZE\s+\d+", vulkan).group(0),
        block(vulkan, "typedef struct DescriptorSetPool\n{"),
        block(vulkan, "typedef struct DescriptorSetCache\n{"),
        block(vulkan, "typedef struct DescriptorSetLayout\n{"),
        block(vulkan, "typedef struct VulkanGraphicsPipelineResourceLayout\n{"),
        block(vulkan, "typedef struct VulkanGraphicsPipeline\n{"),
        block(vulkan, "typedef struct VulkanComputePipelineResourceLayout\n{"),
        block(vulkan, "typedef struct VulkanComputePipeline\n{"),

        *d3d12_layout(source),
        vulkan[vulkan.index("typedef struct VulkanCommandBuffer\n{"):vulkan.index("    Sint32 texturesUsedInPendingTransfersCapacity;") + len("    Sint32 texturesUsedInPendingTransfersCapacity;")] + "\n} VulkanCommandBuffer;", "",
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
