#version 450
layout(local_size_x = 1) in;
layout(set = 0, binding = 0, rgba8) uniform writeonly image2D destination;
layout(set = 0, binding = 1) uniform sampler2D source;
layout(set = 0, binding = 2, rgba8) uniform writeonly image2D tail;
void main() {
    imageStore(destination, ivec2(0), texelFetch(source, ivec2(0), 0));
    imageStore(tail, ivec2(0), vec4(1.0));
}
