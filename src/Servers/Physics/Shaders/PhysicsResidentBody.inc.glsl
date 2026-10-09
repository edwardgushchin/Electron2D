struct ResidentBody { vec4 pose; vec4 velocity; vec4 force; vec4 properties; vec4 surface; uvec4 flags; };
// The spare velocity lane retains the automatic-sleep clock; flags.z carries lock/sleep policy.
void bodyForces(inout ResidentBody b,float dt,vec2 gravity,float dampingDelta,vec3 transientForce)
{
    if((b.flags.z&1024u)!=0u)return;
    b.velocity.xy*=max(0.0,1.0-dampingDelta*b.properties.z);
    b.velocity.z*=max(0.0,1.0-dampingDelta*b.properties.w);
    b.velocity.xy+=dt*(gravity*b.force.w+(b.force.xy+transientForce.xy)*b.properties.x);
    b.velocity.z+=dt*(b.force.z+transientForce.z)*b.properties.y;
    if((b.flags.z&4u)!=0u)b.velocity.z=0;
}

uint continuousMode(ResidentBody b)
{
    return b.flags.y==1u?(b.velocity.xyz!=vec3(0)?2u:0u):b.flags.y>=2u?(b.flags.z>>8)&3u:0u;
}
