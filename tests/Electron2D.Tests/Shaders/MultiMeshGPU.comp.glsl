#version 450
layout(local_size_x=2) in;
layout(set=0,binding=0,std430) buffer Instances { float data[]; };
layout(set=0,binding=1,std430) buffer Commands { uint commands[]; };
layout(set=1,binding=0,std140) uniform Parameters { vec4 p; };
void main() {
    uint i=gl_GlobalInvocationID.x,o=i*16;
    data[o]=0.5+0.5*p.x; data[o+1]=0; data[o+2]=0; data[o+3]=12+32*i+4*p.x;
    data[o+4]=0; data[o+5]=0.5+0.5*p.x; data[o+6]=0; data[o+7]=16;
    data[o+8]=1-p.x; data[o+9]=p.x; data[o+10]=0; data[o+11]=1;
    data[o+12]=0.25; data[o+13]=0.5; data[o+14]=0.75; data[o+15]=1;
    if(i==0) commands[1]=1+uint(p.x);
}
