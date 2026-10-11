#version 450
layout(set=5,binding=7,std140) uniform Parameters { vec4 tint; vec4 controls; };
layout(set=2,binding=13) uniform sampler2DShadow shadowMap;
layout(location=0) out vec4 color;
void main() { float s=texture(shadowMap,vec3(.5,.5,controls.w)); color=vec4(s,s,s,1); }
