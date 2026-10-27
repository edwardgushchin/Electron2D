[[vk::constant_id(0)]] const float factorSource=2.0f;
static const double factor=double(factorSource);
[[vk::constant_id(1)]] const bool choose=true;
struct Pair { double value; float small; uint count; };
struct Push {
 [[vk::offset(0)]] double delta;
 [[vk::offset(16)]] double2 offsets;
 [[vk::offset(32)]] row_major double2x2 matrix;
 [[vk::offset(64)]] double tail[2];
 [[vk::offset(80)]] Pair pair;
 [[vk::offset(120)]] double last;
};
[[vk::push_constant]] ConstantBuffer<Push> pc;
[[vk::binding(0,0)]] StructuredBuffer<double> source;
[[vk::binding(1,0)]] RWStructuredBuffer<double> target;
[[vk::binding(0,1)]] cbuffer Parameters { double2 bias; }
double pair_value(Pair p){ return p.value+double(p.small)+double(float(p.count)); }
double matrix_value(row_major double2x2 m,uint i){ return m[(i>>1)&1][i&1]; }
[numthreads(1,1,1)] void main(uint3 id:SV_DispatchThreadID){
 uint i=id.x; double x=source[i];
 target[i]=(x-1.0)*1099511627776.0*factor+bias[i&1]+pc.delta+pc.offsets[i&1]+matrix_value(pc.matrix,i)+pc.tail[i&1]+pair_value(pc.pair)+pc.last;
 if(i==0){ target[4]=factor; target[5]=choose?pc.delta:pc.last; }
}
