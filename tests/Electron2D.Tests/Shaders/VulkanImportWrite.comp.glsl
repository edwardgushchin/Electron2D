#version 450
layout(local_size_x = 1) in;
layout(set = 0, binding = 0, rgba8) uniform image2D pixels;
void main() {
    const ivec2 point = ivec2(0);
    vec4 color = imageLoad(pixels, point);
    imageStore(pixels, point, vec4(color.b, color.g, 0.0, color.a));
}
