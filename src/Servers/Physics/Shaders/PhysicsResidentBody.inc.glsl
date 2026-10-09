struct ResidentBody { vec4 pose; vec4 velocity; vec4 force; vec4 properties; vec4 surface; uvec4 flags; };
// The spare velocity lane retains the automatic-sleep clock; flags.z carries lock/sleep policy.
// Surface xyz is virtual velocity; its w lane retains the authored motion-recovery priority.
void bodyForces(inout ResidentBody b,float dt,vec4 fields,float dampingDelta,vec3 transientForce)
{
    if((b.flags.z&1024u)!=0u)return;
    b.velocity.xy*=max(0.0,1.0-dampingDelta*fields.z);
    b.velocity.z*=max(0.0,1.0-dampingDelta*fields.w);
    b.velocity.xy+=dt*(fields.xy+(b.force.xy+transientForce.xy)*b.properties.x);
    b.velocity.z+=dt*(b.force.z+transientForce.z)*b.properties.y;
    if((b.flags.z&4u)!=0u)b.velocity.z=0;
}

uint continuousMode(ResidentBody b)
{
    return b.flags.y==1u?(b.velocity.xyz!=vec3(0)?2u:0u):b.flags.y>=2u?(b.flags.z>>8)&3u:0u;
}

bool finiteField(vec4 value){return !any(isnan(value))&&!any(isinf(value));}
bool resolveBodyFields(inout ResidentBody b,inout vec4 stored,vec4 selected,float delta)
{
    vec4 next=b.flags.y==0u?vec4(0):vec4(selected.xy*b.force.w,
        ((b.flags.z&16384u)!=0u?0:selected.z)+b.properties.z,((b.flags.z&32768u)!=0u?0:selected.w)+b.properties.w);
    vec2 factors=max(vec2(0),vec2(1)-delta*next.zw);
    if(!finiteField(next)||!finiteField(vec4(factors,0,0)))return false;
    if((b.flags.z&8192u)!=0u&&next!=stored&&b.flags.y>=2u)
    {b.flags.z=(b.flags.z&~80u)|32u;b.velocity.w=0;}
    stored=next;b.flags.z|=8192u;return true;
}
