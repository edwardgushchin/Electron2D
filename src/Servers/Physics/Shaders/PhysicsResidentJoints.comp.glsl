#version 450
#extension GL_GOOGLE_include_directive : require
#include "PhysicsResidentBody.inc.glsl"
#include "PhysicsResidentConstraint.inc.glsl"
#include "PhysicsResidentJoint.inc.glsl"
layout(local_size_x=64) in;
layout(std430,set=0,binding=0) readonly buffer Bodies { ResidentBody bodies[]; };
layout(std430,set=0,binding=1) readonly buffer Joints { ResidentJoint joints[]; };
layout(std430,set=0,binding=2) readonly buffer Centers { vec2 centers[]; };
layout(std430,set=1,binding=0) buffer States { JointState states[]; };
layout(std430,set=1,binding=1) buffer Constraints { Constraint constraints[]; };
layout(std430,set=1,binding=2) buffer Impulses { ContactImpulse impulses[]; };
layout(std430,set=1,binding=3) buffer Heads { uvec2 heads[]; };
layout(std430,set=1,binding=4) buffer Status { uvec2 status; };
layout(std140,set=2,binding=0) uniform Settings { uvec4 control; vec4 time; vec4 policy; uvec4 history; vec4 correctionPolicy; };
const uint none=0xffffffffu;
const float maximum=3.402823466e38;
void fail(){atomicOr(status.x,1u);}
void emitRow(uint index,ResidentJoint j,vec2 ma,vec2 mb,vec2 n,float aa,float ab,float target,float correction,vec2 bounds,float warm,bool physicalOnly)
{
    float k=(ma.x+mb.x)*dot2(n,n)+ma.y*aa*aa+mb.y*ab*ab;
    if(k==0)return;
    float softness=j.ids.y==0u&&n!=vec2(0)?j.solverPolicy.w:0;
    Constraint c=Constraint(uvec4(j.ids.zw,none,none),vec4(n,aa,ab),vec4(0,physicalOnly?2:1,0,0),vec4(1/(k+softness),softness,target,correction));
    ContactImpulse p=ContactImpulse(vec4(clamp(warm,bounds.x,bounds.y),0,clamp(warm,bounds.x,bounds.y),0),vec4(0,0,bounds));
    if(!finite4(c.normal)||!finite4(c.parameters)||!finite4(p.physical)||!finite4(p.correction)||isnan(k+softness)||isinf(k+softness)){fail();return;}
    if(ma.x>0){c.bodies.z=atomicExchange(heads[j.ids.z].x,2u*index);atomicAdd(heads[j.ids.z].y,1u);}
    if(mb.x>0){c.bodies.w=atomicExchange(heads[j.ids.w].x,2u*index+1u);atomicAdd(heads[j.ids.w].y,1u);}
    constraints[index]=c;impulses[index]=p;
}
float savedImpulse(JointState state,uint row,float ratio) {return ratio*(row<4u?state.first[row]:state.last.x);}
float impulseCap(ResidentJoint j) {return j.solverPolicy.z==maximum?maximum:min(maximum,j.solverPolicy.z*time.x);}
vec2 limitedCorrection(vec2 error,ResidentJoint j)
{
    float factor=j.solverPolicy.x==0?correctionPolicy.y:j.solverPolicy.x;
    vec2 speed=-factor*time.y*error;float size=length(speed),cap=min(time.w,j.solverPolicy.y);
    if(!finite4(vec4(speed,size,0))){fail();return vec2(0);}
    return size>cap?speed*(cap/size):speed;
}
vec2 impulseCost(uint base)
{
    vec2 linear=vec2(0),correction=vec2(0);float angular=0,angularCorrection=0;
    for(uint row=0u;row<5u;row++)
    {
        Constraint c=constraints[base+row];if(c.bodies.x==none)continue;
        ContactImpulse p=impulses[base+row];
        if(c.normal.xy==vec2(0)){angular+=c.normal.z*p.physical.x;angularCorrection+=c.normal.z*p.correction.x;}
        else{linear+=c.normal.xy*p.physical.x;correction+=c.normal.xy*p.correction.x;}
    }
    return vec2(length(linear)+length(correction),abs(angular)+abs(angularCorrection));
}
void limitImpulse(uint base,vec2 remaining)
{
    vec2 cost=impulseCost(base);if(!finite4(vec4(cost,remaining))){fail();return;}
    vec2 scale=vec2(cost.x>remaining.x?remaining.x/cost.x:1,cost.y>remaining.y?remaining.y/cost.y:1);
    for(uint row=0u;row<5u;row++)
    {
        Constraint c=constraints[base+row];if(c.bodies.x==none)continue;
        ContactImpulse p=impulses[base+row];float factor=c.normal.xy==vec2(0)?scale.y:scale.x;
        float physical=p.physical.x*factor,correction=p.correction.x*factor;
        p.physical.z+=physical-p.physical.x;p.physical.x=physical;
        p.correction.y+=correction-p.correction.x;p.correction.x=correction;
        impulses[base+row]=p;
    }
}
void main()
{
    uint i=gl_GlobalInvocationID.x;if(i>=control.y)return;
    ResidentJoint j=joints[i];uint base=history.z+5u*i;
    if(control.x==3u)
    {
        if(j.ids.y<2u&&j.solverPolicy.z<maximum)
            limitImpulse(base,max(vec2(0),states[i].budget.xx-states[i].budget.yz));
        return;
    }
    if(control.x==2u)
    {
        if(j.ids.y==3u)return;
        JointState state=states[i];
        for(uint row=0u;row<5u;row++)
        {
            float value=constraints[base+row].bodies.x==none?0:impulses[base+row].physical.x;
            if(row<4u)state.first[row]=value;else state.last.x=value;
        }
        state.last.y=time.x;
        if(j.solverPolicy.z<maximum)state.budget.yz=min(state.budget.xx,state.budget.yz+impulseCost(base));
        states[i]=state;return;
    }
    for(uint row=0u;row<5u;row++)
    {
        constraints[base+row]=Constraint(uvec4(none),vec4(0),vec4(0),vec4(0));
        impulses[base+row]=ContactImpulse(vec4(0),vec4(0));
    }
    if(j.ids.y==3u)return;
    if(j.ids.z>=control.z||(j.ids.w!=none&&j.ids.w>=control.z)){fail();return;}
    ResidentBody a=bodies[j.ids.z],b=j.ids.w==none?worldBody():bodies[j.ids.w];
    if(a.flags.x!=j.identity.x||a.flags.w==0u||(j.ids.w!=none&&(b.flags.x!=j.identity.y||b.flags.w==0u))){fail();return;}
    vec2 ma=inverseMass(a),mb=inverseMass(b),ra=rotate(a.pose.zw,j.frameA.xy),rb=rotate(b.pose.zw,j.frameB.xy);
    vec2 d=b.pose.xy+rb-a.pose.xy-ra;
    ra-=rotate(a.pose.zw,centers[j.ids.z]);rb-=rotate(b.pose.zw,j.ids.w==none?vec2(0):centers[j.ids.w]);
    if(control.x==0u)
    {
        if(j.ids.y!=2u||j.solverPolicy.z==0)return;
        float distance=length(d);if(isnan(distance)||isinf(distance)){fail();return;}
        if(distance<1.1920928955078125e-5)return;
        vec2 n=d/distance;float aa=cross2(ra,n),ab=cross2(rb,n),k=ma.x+mb.x+ma.y*aa*aa+mb.y*ab*ab;
        if(k==0)return;
        float speed=dot2(b.velocity.xy-a.velocity.xy,n)+b.velocity.z*ab-a.velocity.z*aa;
        float decay=exp(-j.policy.x*time.x*k);
        // Decay can eliminate an extreme elastic term; finite caps saturate before range validation.
        float impulse=((j.motorSpring.z-distance)*time.x*decay)*j.motorSpring.w-speed*(1-decay)/k;
        if(isnan(impulse)){fail();return;}
        float cap=impulseCap(j);
        if(j.solverPolicy.z<maximum)impulse=clamp(impulse,-cap,cap);
        if(isinf(impulse)){fail();return;}
        emitRow(base,j,ma,mb,n,aa,ab,0,0,vec2(impulse),impulse,true);return;
    }
    if(j.ids.y==2u)return;
    JointState old=states[i];uvec4 epochs=uvec4(a.flags.w,b.flags.w,j.ids.x,1u);
    // Cached impulses seed iteration; do not amplify a short-interval impact into a large cancellation.
    float ratio=old.epochs==epochs&&old.last.y>0?min(1,time.x/old.last.y):0;
    states[i].epochs=epochs;
    // A joint first awakened at an impact starts with only that interval's remaining allowance.
    bool firstSolve=(history.w&1u)==0u||old.budget.w==0;
    if(firstSolve)states[i].budget=vec4(impulseCap(j),0,0,ma.x>0||mb.x>0?1:0);
    float bias=j.solverPolicy.x==0?correctionPolicy.y:j.solverPolicy.x;
    float maxBias=min(time.w,j.solverPolicy.y);
    if(j.ids.y==0u)
    {
        vec2 correction=limitedCorrection(d,j);
        for(uint row=0u;row<2u;row++)
        {
            vec2 n=row==0u?vec2(1,0):vec2(0,1);
            emitRow(base+row,j,ma,mb,n,cross2(ra,n),cross2(rb,n),0,correction[row],vec2(-maximum,maximum),savedImpulse(old,row,ratio),false);
        }
        if((j.identity.z&1u)!=0u)
        {
            vec2 qa=rotate(a.pose.zw,j.frameA.zw),qb=rotate(b.pose.zw,j.frameB.zw);
            float angle=atan(cross2(qa,qb),dot2(qa,qb));
            for(uint row=0u;row<2u;row++)
            {
                float sign=row==0u?1:-1,gap=row==0u?angle-j.limits.z:j.limits.w-angle;
                emitRow(base+2u+row,j,ma,mb,vec2(0),sign,sign,-max(gap,0)*time.y,min(maxBias,bias*max(-gap,0)*time.y),vec2(0,maximum),savedImpulse(old,2u+row,ratio),gap>0);
            }
        }
        if(firstSolve&&(j.identity.z&2u)!=0u&&j.motorSpring.y>0)
        {
            float cap=min(maximum,j.motorSpring.y*(time.x*10000.0));
            emitRow(base+4u,j,ma,mb,vec2(0),1,1,j.motorSpring.x,0,vec2(-cap,cap),savedImpulse(old,4u,ratio),true);
        }
        return;
    }
    vec2 axis=rotate(a.pose.zw,j.frameA.zw),perp=vec2(-axis.y,axis.x);
    float translation=dot2(d,axis),side=dot2(d,perp);
    float error=translation-clamp(translation,j.limits.x,j.limits.y);
    vec2 correction=limitedCorrection(vec2(side,error),j);
    emitRow(base,j,ma,mb,perp,cross2(ra+d,perp),cross2(rb,perp),0,correction.x,vec2(-maximum,maximum),savedImpulse(old,0u,ratio),false);
    for(uint row=0u;row<2u;row++)
    {
        float sign=row==0u?1:-1,gap=row==0u?translation-j.limits.x:j.limits.y-translation;vec2 n=sign*axis;
        emitRow(base+1u+row,j,ma,mb,n,cross2(ra+d,n),cross2(rb,n),-max(gap,0)*time.y,max(0,sign*correction.y),vec2(0,maximum),savedImpulse(old,1u+row,ratio),gap>0);
    }
}
