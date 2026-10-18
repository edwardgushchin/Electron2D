#version 450
layout(location=0) in vec2 position;
layout(location=1) in vec4 color;
layout(location=2) in vec2 uv;
layout(set=1,binding=0,std140) uniform Frame { vec2 size; };
layout(set=1,binding=1,std140) uniform Values {
    layout(row_major) mat4 a4[2];
    layout(column_major) mat3 a3[2];
    layout(column_major) mat4 m4;
    layout(row_major) mat3 m3;
    float tail;
};
layout(location=0) out vec4 outColor;
layout(location=1) out vec2 outUV;
void main() {
    vec3 x=m3*vec3(1,2,3); vec4 y=m4*vec4(1,2,3,4);
    bool valid=tail==0.875;
    for(int c=0;c<3;c++) valid=valid && all(equal(m3[c],a3[0][c])) && all(equal(a3[1][c],vec3(c==0?1:0,c==1?1:0,c==2?1:0)));
    for(int c=0;c<4;c++) valid=valid && all(equal(m4[c],a4[1][c])) && all(equal(a4[0][c],vec4(c==0?1:0,c==1?1:0,c==2?1:0,c==3?1:0)));
    vec2 p=position+vec2(x.x*8,0);
    gl_Position=vec4(p.x/size.x*2-1,1-p.y/size.y*2,0,1);
    outColor=valid?color:vec4(1,0,1,1); outUV=uv;
}
