#version 450
layout(local_size_x = 1) in;
layout(set=3,binding=7) uniform usampler2D unsignedImage;
layout(set=3,binding=8) uniform isampler2D signedImage;
layout(set=5,binding=2,r32ui) uniform uimage2D unsignedOutput;
layout(set=5,binding=3,r32i) uniform iimage2D signedOutput;
void main() {
    imageStore(unsignedOutput, ivec2(0), textureLod(unsignedImage, vec2(0.5), 0.0) + uvec4(1));
    imageStore(signedOutput, ivec2(0), textureLod(signedImage, vec2(0.5), 0.0) - ivec4(3));
}
