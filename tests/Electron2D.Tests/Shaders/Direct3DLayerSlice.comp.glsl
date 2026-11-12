#version 450
layout(local_size_x=1) in;
layout(set=0,binding=0) uniform sampler2D sourceImage;
layout(set=0,binding=1,r32ui) uniform uimage2D targetImage;
layout(set=0,binding=2,std430) buffer Result { uint result[]; };
void main() {
    result[0] = uint(textureSize(sourceImage, 0).x);
    result[1] = uint(textureQueryLevels(sourceImage));
    result[2] = uint(round(texelFetch(sourceImage, ivec2(0), 1).r * 255.0));
    result[3] = uint(imageSize(targetImage).x);
    result[4] = imageAtomicAdd(targetImage, ivec2(0), 11u);
}
