#version 450
layout(local_size_x=1) in;
layout(constant_id=0) const double factor=2.0LF;
layout(constant_id=1) const bool choose=true;
layout(set=0,binding=0,std430) readonly buffer Input { double values[]; } source;
layout(set=0,binding=1,std430) buffer Output { double values[]; } target;
layout(set=1,binding=0,std140) uniform Parameters { dvec2 bias; } parameters;
struct Pair { double value; float small; uint count; };
layout(push_constant,std430) uniform Push {
    layout(offset=0) double delta;
    layout(offset=16) dvec2 offsets;
    layout(offset=32,column_major) dmat2 matrix;
    layout(offset=64) double tail[2];
    layout(offset=80) Pair pair;
    layout(offset=120) double last;
} pc;
double pair_value(Pair p){ return p.value+double(p.small)+double(p.count); }
double matrix_value(dmat2 m,uint i){ return m[int(i&1u)][int((i>>1u)&1u)]; }
void main(){
    uint i=gl_GlobalInvocationID.x;
    double x=source.values[i];
    target.values[i]=(x-1.0LF)*1099511627776.0LF*factor+parameters.bias[i&1u]+pc.delta+pc.offsets[i&1u]+matrix_value(pc.matrix,i)+pc.tail[i&1u]+pair_value(pc.pair)+pc.last;
    if(i==0){ target.values[4]=factor; target.values[5]=choose?pc.delta:pc.last; }
}
