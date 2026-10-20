#version 450
layout(location=0) in vec4 tint;
layout(location=0) out vec4 color;
layout(set=5,binding=7) uniform Settings { vec4 color; } settings;
layout(set=6,binding=8,std430) readonly buffer Modifiers { vec4 value; } modifier;
layout(set=2,binding=13) uniform sampler2D sourceImage;
layout(set=6,binding=9,rgba8) uniform readonly image2D sourceStorage;
vec4 read_storage() { ivec2 size=imageSize(sourceStorage);return imageLoad(sourceStorage,ivec2(0))*(size==ivec2(1) ? 1:0); }
void main() { color=tint*settings.color*modifier.value*texture(sourceImage,vec2(.5))*read_storage(); }
