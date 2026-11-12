#version 450
layout(local_size_x = 1) in;
layout(set = 0, binding = 0, rgba8) uniform image2DArray pixels;
void main() {
    ivec3 point = ivec3(gl_GlobalInvocationID);
    vec4 color = imageLoad(pixels, point);
    imageStore(pixels, point, vec4(color.b, color.g, color.r, color.a));
}
