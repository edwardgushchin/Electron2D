#version 450
layout(set=3,binding=7) uniform texture2D image;
layout(set=5,binding=13) uniform sampler state;
layout(location=0) in vec4 tint;
layout(location=1) in vec2 uv;
layout(location=0) out vec4 color;
vec4 read_image(texture2D source,sampler sampling,vec2 coords) { return texture(sampler2D(source,sampling),coords); }
void main() { color=(tint+read_image(image,state,uv))*.5; }
