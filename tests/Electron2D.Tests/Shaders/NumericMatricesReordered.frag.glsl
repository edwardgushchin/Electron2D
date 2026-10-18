#version 450
layout(location=0) out vec4 outputColor;
layout(set=3,binding=0,std140) uniform Values {
    layout(row_major) mat4 a4[2];
    layout(column_major) mat3 a3[2];
    layout(column_major) mat4 m4;
    layout(row_major) mat3 m3;
    float tail;
};
void main() {
    vec3 x=m3*vec3(1,2,3); vec4 y=m4*vec4(1,2,3,4);
    bool valid=tail==0.875;
    for(int c=0;c<3;c++) valid=valid && all(equal(m3[c],a3[0][c])) && all(equal(a3[1][c],vec3(c==0?1:0,c==1?1:0,c==2?1:0)));
    for(int c=0;c<4;c++) valid=valid && all(equal(m4[c],a4[1][c])) && all(equal(a4[0][c],vec4(c==0?1:0,c==1?1:0,c==2?1:0,c==3?1:0)));
    outputColor=valid?vec4(x.x,x.y,y.z,1):vec4(1,0,1,1);
}
