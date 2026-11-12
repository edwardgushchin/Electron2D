#version 450
layout(set=0,binding=0,r32ui) uniform readonly uimage2D inputImage;
layout(location=0) flat in uint width;
layout(location=0) out uint color;
void main() { color = imageLoad(inputImage, ivec2(0)).r + uint(imageSize(inputImage).y) + width; }
