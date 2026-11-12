#version 450
layout(local_size_x = 1) in;
layout(set = 0, binding = 0, r32ui) uniform uimage2DArray pixels;
void main() {
    imageAtomicAdd(pixels, ivec3(0, 0, gl_GlobalInvocationID.z), 3u);
}
