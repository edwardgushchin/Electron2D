#version 450
#extension GL_EXT_shader_explicit_arithmetic_types_int64:require
#extension GL_EXT_shader_atomic_int64:require
#extension GL_KHR_memory_scope_semantics:require
layout(local_size_x=64) in;
layout(set=0,binding=0,std430) buffer Cells { int64_t signedCell; uint64_t unsignedCell; } cells;
layout(set=0,binding=1,std430) buffer Results { uint64_t values[]; } outputData;
layout(constant_id=0) const uint64_t extra=7ul;
layout(push_constant,std430) uniform Push { uint64_t base; } pc;

void main(){
 uint i=gl_LocalInvocationID.x;
 if(i==0){
  cells.signedCell=int64_t(-1099511627776l); cells.unsignedCell=0xfedcba9800000007ul;
  outputData.values[0]=uint64_t(atomicAdd(cells.signedCell, int64_t(-5)));
  outputData.values[1]=uint64_t(atomicMin(cells.signedCell, int64_t(-1125899906842624l)));
  outputData.values[2]=uint64_t(atomicMax(cells.signedCell, int64_t(1125899906842624l)));
  outputData.values[3]=uint64_t(atomicExchange(cells.signedCell, int64_t(-7)));
  outputData.values[4]=uint64_t(atomicCompSwap(cells.signedCell, int64_t(-8), int64_t(19)));
  outputData.values[5]=uint64_t(atomicCompSwap(cells.signedCell, int64_t(-7), int64_t(-17)));
  outputData.values[6]=uint64_t(cells.signedCell);
  outputData.values[7]=atomicAnd(cells.unsignedCell, 0xffff0000ffff0000ul);
  outputData.values[8]=atomicOr(cells.unsignedCell, 0x0000111100001111ul);
  outputData.values[9]=atomicXor(cells.unsignedCell, 0x8000000000000001ul);
  outputData.values[10]=atomicMin(cells.unsignedCell, 0x8000000000000000ul);
  outputData.values[11]=atomicMax(cells.unsignedCell, 0xf000000000000000ul);
  outputData.values[12]=atomicExchange(cells.unsignedCell, 0xfffffffffffffffful);
  outputData.values[13]=atomicAdd(cells.unsignedCell, 1ul);
  outputData.values[14]=cells.unsignedCell;
  atomicStore(cells.unsignedCell, pc.base+extra, gl_ScopeDevice, gl_StorageSemanticsBuffer, gl_SemanticsRelease);
  outputData.values[16]=atomicLoad(cells.unsignedCell, gl_ScopeDevice, gl_StorageSemanticsBuffer, gl_SemanticsAcquire);
  outputData.values[17]=atomicAdd(cells.unsignedCell, uint64_t(-1l));
  atomicAdd(cells.unsignedCell, 1ul);
 }
 memoryBarrierBuffer(); barrier();
 outputData.values[64+i]=atomicAdd(cells.unsignedCell, 1ul);
 memoryBarrierBuffer(); barrier();
 if(i==0)outputData.values[15]=cells.unsignedCell;
}
