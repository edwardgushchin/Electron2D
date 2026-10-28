#include <SDL3/SDL.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include "memory-layout.h"
extern SDL_GPUTexture *e2d_render_view_region(SDL_GPUDevice *, uint32_t, SDL_GPUTexture *, uint32_t *, uint32_t *);
extern int e2d_render_view_support(SDL_GPUDevice *, uint32_t);
extern SDL_GPUTexture *e2d_render_view_create(SDL_GPUDevice *, uint32_t, SDL_GPUTexture *, SDL_GPUTextureFormat, uint32_t, uint32_t, uint32_t, uint32_t, uint32_t);
extern SDL_GPUTexture *e2d_render_texture_create(SDL_GPUDevice *, uint32_t, const SDL_GPUTextureCreateInfo *, const SDL_GPUTextureFormat *, uint32_t);
extern void e2d_render_view_destroy(SDL_GPUDevice *, uint32_t, SDL_GPUTexture *);
extern void e2d_render_views_collect(SDL_GPUDevice *, uint32_t);
extern void e2d_render_views_release(SDL_GPUDevice *, uint32_t);
typedef struct Memory { uint64_t textures, buffers, total; } Memory;
extern int e2d_render_memory(SDL_GPUDevice *, uint32_t, Memory *);
#define check(x) do { if (!(x)) { fprintf(stderr,"FAIL line %d: %s: %s\n",__LINE__,#x,SDL_GetError()); abort(); } } while(0)
static void clear(SDL_GPUDevice *d, SDL_GPUTexture *t, uint32_t layer, uint32_t level, SDL_FColor color)
{
    SDL_GPUCommandBuffer *c = SDL_AcquireGPUCommandBuffer(d); check(c);
    SDL_GPUColorTargetInfo info = {.texture=t,.layer_or_depth_plane=layer,.mip_level=level,.clear_color=color,.load_op=SDL_GPU_LOADOP_CLEAR,.store_op=SDL_GPU_STOREOP_STORE};
    SDL_GPURenderPass *p = SDL_BeginGPURenderPass(c,&info,1,NULL); check(p); SDL_EndGPURenderPass(p); check(SDL_SubmitGPUCommandBuffer(c));
}
static void pixels(SDL_GPUDevice *d,SDL_GPUTexture *t,uint32_t layer,uint32_t level,uint32_t side,uint8_t expected)
{
    t=e2d_render_view_region(d,1,t,&layer,&level);check(t);
    SDL_GPUTransferBufferCreateInfo desc = {.size=side*side*4,.usage=SDL_GPU_TRANSFERBUFFERUSAGE_DOWNLOAD}; SDL_GPUTransferBuffer *b = SDL_CreateGPUTransferBuffer(d,&desc);check(b);
    SDL_GPUCommandBuffer *c = SDL_AcquireGPUCommandBuffer(d);check(c);SDL_GPUCopyPass *p=SDL_BeginGPUCopyPass(c);check(p);
    SDL_GPUTextureRegion src={.texture=t,.layer=layer,.mip_level=level,.w=side,.h=side,.d=1}; SDL_GPUTextureTransferInfo dst={.transfer_buffer=b}; SDL_DownloadFromGPUTexture(p,&src,&dst);SDL_EndGPUCopyPass(p); check(SDL_SubmitGPUCommandBuffer(c)); check(SDL_WaitForGPUIdle(d));
    const uint8_t *bytes=SDL_MapGPUTransferBuffer(d,b,false);check(bytes);for(uint32_t i=0;i<side*side*4;i++) { if(bytes[i]!=expected)fprintf(stderr,"MISMATCH layer %u mip %u side %u offset %u actual %u expected %u\n",layer,level,side,i,bytes[i],expected);check(bytes[i]==expected); }SDL_UnmapGPUTransferBuffer(d,b);SDL_ReleaseGPUTransferBuffer(d,b);
}
static float bits(uint32_t value){float f;memcpy(&f,&value,4);return f;}
extern int e2d_render_prepare_compute_images(SDL_GPUDevice *, uint32_t, SDL_GPUCommandBuffer *, SDL_GPUTexture *const *, uint32_t, uint32_t);
static PFN_vkUpdateDescriptorSets previous_update;
static uint32_t general_samplers, readonly_samplers, full_transitions;
static PFN_vkCmdPipelineBarrier previous_barrier;
static VkImage observed_image;
static void VKAPI_CALL inspect_array_barriers(VkCommandBuffer command, VkPipelineStageFlags src, VkPipelineStageFlags dst, VkDependencyFlags dependency, uint32_t memory_count, const VkMemoryBarrier *memory, uint32_t buffer_count, const VkBufferMemoryBarrier *buffers, uint32_t image_count, const VkImageMemoryBarrier *images)
{
    for (uint32_t i = 0; i < image_count; i++) if (images[i].image == observed_image) {
        check(images[i].subresourceRange.baseMipLevel == 0 && images[i].subresourceRange.levelCount == 2 && images[i].subresourceRange.baseArrayLayer == 0 && images[i].subresourceRange.layerCount == 3);
        check(images[i].oldLayout == (full_transitions ? VK_IMAGE_LAYOUT_GENERAL : VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL) && images[i].newLayout == (full_transitions ? VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL : VK_IMAGE_LAYOUT_GENERAL)); full_transitions++;
    }
    previous_barrier(command, src, dst, dependency, memory_count, memory, buffer_count, buffers, image_count, images);
}
static void VKAPI_CALL inspect_compute_sets(VkDevice device, uint32_t count, const VkWriteDescriptorSet *writes, uint32_t copies, const VkCopyDescriptorSet *copy)
{
    for (uint32_t i = 0; i < count; i++) if (writes[i].descriptorType == VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER) {
        general_samplers += writes[i].pImageInfo->imageLayout == VK_IMAGE_LAYOUT_GENERAL;
        readonly_samplers += writes[i].pImageInfo->imageLayout == VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL;
    }
    previous_update(device, count, writes, copies, copy);
}
static void storage_array_descriptors(SDL_GPUDevice *d, const char *file)
{
    FILE *input = fopen(file, "rb"); check(input); check(fseek(input, 0, SEEK_END) == 0); long length = ftell(input); check(length > 0 && length < 65536 && length % 4 == 0); rewind(input);
    uint8_t *code = SDL_malloc((size_t)length); check(code); check(fread(code, 1, (size_t)length, input) == (size_t)length); check(fclose(input) == 0);
    SDL_GPUComputePipelineCreateInfo program = {.code = code, .code_size = (size_t)length, .format = SDL_GPU_SHADERFORMAT_SPIRV, .entrypoint = "main", .num_samplers = 2, .num_readwrite_storage_textures = 2, .threadcount_x = 1, .threadcount_y = 1, .threadcount_z = 1};
    SDL_GPUComputePipeline *pipeline = SDL_CreateGPUComputePipeline(d, &program); SDL_free(code); check(pipeline);
    SDL_GPUTextureCreateInfo desc = {.type = SDL_GPU_TEXTURETYPE_2D_ARRAY, .format = SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM, .width = 4, .height = 4, .layer_count_or_depth = 3, .num_levels = 2, .usage = SDL_GPU_TEXTUREUSAGE_SAMPLER | SDL_GPU_TEXTUREUSAGE_COLOR_TARGET | SDL_GPU_TEXTUREUSAGE_COMPUTE_STORAGE_SIMULTANEOUS_READ_WRITE};
    SDL_GPUTexture *root = SDL_CreateGPUTexture(d, &desc); check(root); desc.usage = SDL_GPU_TEXTUREUSAGE_SAMPLER | SDL_GPU_TEXTUREUSAGE_COLOR_TARGET; SDL_GPUTexture *independent = SDL_CreateGPUTexture(d, &desc); check(independent);
    check(!e2d_render_view_create(d, 1, root, desc.format, 0, 3, 0, 1, 2)); check(e2d_render_view_support(d, 2) == 1);
    SDL_GPUTexture *array = e2d_render_view_create(d, 2, root, desc.format, 0, 3, 0, 1, 2); check(array); check(!e2d_render_view_create(d, 2, root, desc.format, 0, 3, 0, 2, 2));
    clear(d, root, 0, 1, (SDL_FColor){.r=.4f,.g=.4f,.b=.4f,.a=.4f}); clear(d, independent, 0, 1, (SDL_FColor){.r=.4f,.g=.4f,.b=.4f,.a=.4f});
    SDL_GPUSamplerCreateInfo sampler_info = {0}; SDL_GPUSampler *sampler = SDL_CreateGPUSampler(d, &sampler_info); check(sampler);
    SDL_GPUCommandBuffer *commands = SDL_AcquireGPUCommandBuffer(d); check(commands); SDL_GPUComputePass *pass = SDL_BeginGPUComputePass(commands, NULL, 0, NULL, 0); check(pass); SDL_BindGPUComputePipeline(pass, pipeline);
    SDL_GPUTextureSamplerBinding samples[] = {{.texture=root,.sampler=sampler},{.texture=independent,.sampler=sampler}}; SDL_BindGPUComputeSamplers(pass, 0, samples, 2);
    VulkanRenderer *r = (VulkanRenderer *)d->driverData; previous_update = r->vkUpdateDescriptorSets; general_samplers = readonly_samplers = 0; r->vkUpdateDescriptorSets = inspect_compute_sets; previous_barrier = r->vkCmdPipelineBarrier; r->vkCmdPipelineBarrier = inspect_array_barriers; full_transitions = 0; observed_image = ((VulkanTextureContainer *)root)->activeTexture->image;
    SDL_GPUTexture *writes[] = {array, array}; check(e2d_render_prepare_compute_images(d, 1, commands, writes, 2, 1) == -1); check(e2d_render_prepare_compute_images(d, 2, commands, writes, 2, 1) == 1); SDL_DispatchGPUCompute(pass, 1, 1, 1); check(e2d_render_prepare_compute_images(d, 2, commands, writes, 2, 0) == 1); r->vkUpdateDescriptorSets = previous_update; r->vkCmdPipelineBarrier = previous_barrier; check(full_transitions == 2);
    check(general_samplers == 1 && readonly_samplers == 1); SDL_EndGPUComputePass(pass); check(SDL_SubmitGPUCommandBuffer(commands)); pixels(d, root, 2, 0, 1, 204); pixels(d, root, 1, 0, 1, 204);
    SDL_ReleaseGPUComputePipeline(d, pipeline); SDL_ReleaseGPUSampler(d, sampler); e2d_render_view_destroy(d, 1, array); SDL_ReleaseGPUTexture(d, root); SDL_ReleaseGPUTexture(d, independent); check(SDL_WaitForGPUIdle(d));
    puts("PASS native array compute: layer-two store, single-mip view, duplicate-image deduplication, full-root/all-layer/mip transitions, exact GENERAL/shared and READ_ONLY/independent descriptor writes observed at Vulkan boundary");
}
void e2d_views_check(SDL_GPUDevice *d)
{
    check(e2d_render_view_support(NULL,1)==-1); check(e2d_render_view_support((SDL_GPUDevice *)(uintptr_t)1,0)==-1);
    check(e2d_render_view_support(d,1)==1);
    SDL_GPUTextureCreateInfo desc={.type=SDL_GPU_TEXTURETYPE_2D_ARRAY,.format=SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM,.width=8,.height=8,.layer_count_or_depth=3,.num_levels=3,.sample_count=SDL_GPU_SAMPLECOUNT_1,.usage=SDL_GPU_TEXTUREUSAGE_SAMPLER|SDL_GPU_TEXTUREUSAGE_COLOR_TARGET};
    SDL_GPUTexture *root=SDL_CreateGPUTexture(d,&desc);check(root);check(SDL_WaitForGPUIdle(d));VulkanTexture *native=((VulkanTextureContainer*)root)->activeTexture;VulkanMemoryAllocation *allocation=native->usedRegion->allocation;int pin=SDL_GetAtomicInt(&allocation->referenceCount);
    SDL_GPUTexture *v=e2d_render_view_create(d,1,root,desc.format,2,1,1,2,0);check(v);check(SDL_GetAtomicInt(&allocation->referenceCount)==pin+1);check(!((VulkanTextureContainer*)root)->canBeCycled);
    clear(d,root,2,1,(SDL_FColor){.r=.8f,.g=.8f,.b=.8f,.a=.8f});pixels(d,v,0,0,4,204);
    clear(d,v,0,1,(SDL_FColor){.r=.4f,.g=.4f,.b=.4f,.a=.4f});pixels(d,root,2,2,2,102);
    SDL_GPUTexture *nested=e2d_render_view_create(d,1,v,desc.format,0,1,1,1,0);check(nested);pixels(d,nested,0,0,2,102);e2d_render_view_destroy(d,1,nested);
    SDL_GPUCommandBuffer *pending=SDL_AcquireGPUCommandBuffer(d);check(pending);SDL_GPUColorTargetInfo target={.texture=v,.clear_color={.r=1,.g=1,.b=1,.a=1},.load_op=SDL_GPU_LOADOP_CLEAR,.store_op=SDL_GPU_STOREOP_STORE};SDL_GPURenderPass *pass=SDL_BeginGPURenderPass(pending,&target,1,NULL);check(pass);SDL_EndGPURenderPass(pass);
    e2d_render_view_destroy(d,1,v);check(SDL_GetAtomicInt(&allocation->referenceCount)==pin+1);check(SDL_CancelGPUCommandBuffer(pending));e2d_render_views_collect(d,1);check(SDL_GetAtomicInt(&allocation->referenceCount)==pin);SDL_ReleaseGPUTexture(d,root);check(SDL_WaitForGPUIdle(d));
    puts("PASS ordinary native image views: original/alias writes, mip/layer/nested selection, defrag allocation pin and deferred unsubmitted-command retirement");
    Memory before,live,after;check(e2d_render_memory(d,1,&before)==1);
    SDL_GPUTextureFormat formats[]={SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM,SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UINT,SDL_GPU_TEXTUREFORMAT_R32_FLOAT};
    root=e2d_render_texture_create(d,1,&desc,formats,3);check(root);check(e2d_render_memory(d,1,&live)==1);check(live.textures>before.textures&&live.total>=live.textures+live.buffers);v=e2d_render_view_create(d,1,root,formats[1],1,1,1,2,0);check(v);
    clear(d,root,1,1,(SDL_FColor){.r=.8f,.g=.8f,.b=.8f,.a=.8f});pixels(d,v,0,0,4,204);clear(d,v,0,1,(SDL_FColor){.r=bits(17),.g=bits(17),.b=bits(17),.a=bits(17)});pixels(d,root,1,2,2,17);
    check(!e2d_render_view_create(d,1,root,SDL_GPU_TEXTUREFORMAT_R8_UNORM,0,1,0,1,0));check(!e2d_render_view_create(d,1,root,formats[0],3,1,0,1,0));check(!e2d_render_view_create(d,1,root,formats[0],0,1,2,2,0));
    e2d_render_view_destroy(d,1,root);pixels(d,v,0,0,4,204);e2d_render_view_destroy(d,1,v);e2d_render_views_collect(d,1);check(e2d_render_memory(d,1,&after)==1);check(after.textures==before.textures);printf("PASS mutable native image: format aliases share encoded bytes, mip/layer targets, live memory %llu -> %llu -> %llu and child-retained owner retirement\n",(unsigned long long)before.textures,(unsigned long long)live.textures,(unsigned long long)after.textures);
    if (getenv("ELECTRON2D_TEST_ARRAY_STORAGE_SPIRV")) storage_array_descriptors(d, getenv("ELECTRON2D_TEST_ARRAY_STORAGE_SPIRV"));
    e2d_render_views_release(d,1);
}
