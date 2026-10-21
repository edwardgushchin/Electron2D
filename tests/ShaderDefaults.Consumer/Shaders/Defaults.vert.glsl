#version 450
layout(location=0) in vec2 position;
layout(location=1) in vec4 color;
layout(location=2) in vec2 uv;
layout(set=1,binding=0,std140) uniform Frame { vec2 size; };
layout(set=1,binding=1,std140) uniform Values {
    layout(offset=0) float f1;
    layout(column_major,offset=16) mat3 m3;
    layout(offset=64) float vertexOnly;
};
layout(location=0) out vec4 outColor;
layout(location=1) out vec2 outUV;
void main() {
    vec2 p=position; p.x += (f1 - .125 + vertexOnly - .5 + m3[0][1] - 2) * 8;
    gl_Position=vec4(p.x/size.x*2-1,1-p.y/size.y*2,0,1); outColor=color; outUV=uv;
}
