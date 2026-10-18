#version 450
layout(location=0) in vec2 position;
layout(location=1) in vec4 color;
layout(location=2) in vec2 uv;
layout(location=0) out vec4 outColor;
layout(location=1) out vec2 outUV;
layout(set=1,binding=0,std140) uniform Frame { vec2 size; };
layout(set=1,binding=1,std140) uniform Material { int selected; };
layout(set=0,binding=0) uniform sampler2D colorMaps[2];
void main() {
    gl_Position=vec4(position.x/size.x*2-1,1-position.y/size.y*2,0,1);
    outColor=color*textureLod(colorMaps[selected],vec2(.5),0);
    outUV=uv;
}
