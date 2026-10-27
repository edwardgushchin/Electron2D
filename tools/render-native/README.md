# Private render allocator and sampler bridge

This producer compiles the allocator/sampler declarations of SDL 3.4.18 from the archive
SHA-256 pinned in CMakeLists.txt. It does not build or replace SDL. The selected
target's SDL core is supplied through SDL3_LIBRARIES. The generated declarations
retain SDL's notice; the platform package includes licence/SDL-LICENSE.txt.

e2d_render_memory uses ABI 1 and returns 1 for a coherent native snapshot, 0 for
an unsupported core/revision/driver, and -1 for invalid arguments or inconsistent
allocator data. Output contains three uint64 fields: textures, buffers and total.
The current native adapter is Vulkan. It locks the native allocator without
submitting, waiting for GPU work or invoking application callbacks.

Build Electron2DRender for delivery and Electron2DRenderCheck for the native
concurrency/allocation check. Run test_generate_layout.py with Python -B for
bounded extraction and modified-source rejection. Runtime acceptance requires
the selected matching core; a successful compilation alone does not qualify it.

`e2d_render_sampler_border` uses ABI 1, copies the SDL sampler creation fields,
and selects normalized U/V address modes (0..3) plus border color (0..5).
At least one axis must clamp to the border. It requires the same pinned Vulkan
core and returns an SDL-owned sampler or null with SDL_GetError. A normal SDL
wrapper receives the new Vulkan handle before publication; SDL retains command
references and deferred destruction. No native driver handles leave the module.
The public sampler consumer checks actual float/integer/depth pixels, array
images and mixed/filtering modes. Unnormalized coordinates and mirror-clamp
are separate capability/operation-validation prerequisites.
