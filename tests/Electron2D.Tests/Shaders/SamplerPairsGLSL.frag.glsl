#version 450
layout(location=0) in vec4 color;
layout(location=0) out vec4 outputColor;
layout(set=2,binding=0) uniform texture2D colorMaps[2];
layout(set=2,binding=0) uniform sampler colorStates[2];
layout(set=2,binding=1) uniform texture2D otherMaps[2];
layout(set=2,binding=1) uniform sampler otherStates[2];
layout(set=3,binding=0,std140) uniform Material { int selected; };
vec4 readMap(texture2D image, sampler state) { return texture(sampler2D(image,state),vec2(.5)); }
void main() { outputColor=color*(readMap(colorMaps[selected],colorStates[selected])+readMap(otherMaps[selected],otherStates[selected]))*.5; }
