// float16_t is mentioned only in inactive/commented source.
#if 0
float16_t ignored;
#endif
#line 1 "float16_t"
[[vk::binding(0,0)]] RWStructuredBuffer<float> target;
[numthreads(1,1,1)] void main(){half value=1.0001220703125;target[0]=value;}
