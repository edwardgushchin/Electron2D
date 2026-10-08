// SPDX-License-Identifier: MIT
#version 450
layout(local_size_x=64) in;
struct Island { ivec4 state; ivec4 bodies; ivec4 contacts; ivec4 joints; };
struct Change { ivec4 inputData; ivec4 result; };
layout(std430,set=1,binding=0) buffer Islands { Island islands[]; };
layout(std430,set=1,binding=1) buffer Bodies { ivec4 bodies[]; };
layout(std430,set=1,binding=2) buffer Contacts { ivec4 contacts[]; };
layout(std430,set=1,binding=3) buffer Joints { ivec4 joints[]; };
layout(std430,set=1,binding=4) buffer Changes { Change changes[]; };
layout(std430,set=1,binding=5) buffer Status { ivec4 status; };
layout(std140,set=2,binding=0) uniform Settings { ivec4 sizes; ivec4 work; };
// Member: island, previous ID, next ID, alive. Island state: parent, ID, removal count, reserved.
// Change input: contact ID, generation, initial endpoint islands. Result: link, root, freed ID, reserved.
int root(int id)
{
    for(int depth=0;id>=0 && depth<sizes.w;depth++)
    {
        if(id>=sizes.w || islands[id].state.y!=id) { atomicMax(status.x,1); return -1; }
        int parent=islands[id].state.x;
        if(parent==id) return id;
        id=parent;
    }
    if(id>=0) atomicMax(status.x,2);
    return -1;
}
ivec4 node(int kind,int id) { if(kind==0) return bodies[id]; if(kind==1) return contacts[id]; return joints[id]; }
void put(int kind,int id,ivec4 value) { if(kind==0) bodies[id]=value; else if(kind==1) contacts[id]=value; else joints[id]=value; }
ivec4 list(int kind,int id) { if(kind==0) return islands[id].bodies; if(kind==1) return islands[id].contacts; return islands[id].joints; }
void putList(int kind,int id,ivec4 value) { if(kind==0) islands[id].bodies=value; else if(kind==1) islands[id].contacts=value; else islands[id].joints=value; }
void merge(int big,int small)
{
    for(int kind=0;kind<3;kind++)
    {
        ivec4 a=list(kind,big),b=list(kind,small);
        if(a.z==0) a=b;
        else if(b.z!=0)
        {
            ivec4 tail=node(kind,a.y),head=node(kind,b.x);
            tail.z=b.x; head.y=a.y; put(kind,a.y,tail); put(kind,b.x,head);
            a.y=b.y; a.z+=b.z;
        }
        putList(kind,big,a); putList(kind,small,ivec4(-1,-1,0,0));
    }
    islands[big].state.z+=islands[small].state.z;
    islands[small].state.z=0; islands[small].state.x=big;
    status.y++;
}
void main()
{
    int i=int(gl_GlobalInvocationID.x);
    if(work.x==0)
    {
        if(i!=0) return;
        status=ivec4(0);
        // ponytail: ordered union/list edits are serial to preserve contact event
        // ordering; member remapping is parallel and never walks merged lists.
        for(int op=0;op<work.y;op++)
        {
            Change change=changes[op]; int id=change.inputData.x;
            if(id<0 || id>=sizes.y || contacts[id].w!=1) { status.x=3; return; }
            int target;
            if(change.result.x!=0)
            {
                int a=root(change.inputData.z),b=root(change.inputData.w);
                if(status.x!=0 || (a<0 && b<0) || contacts[id].x!=-1) { status.x=4; return; }
                target=a<0?b:a;
                if(a>=0 && b>=0 && a!=b)
                {
                    int big=islands[a].bodies.z>=islands[b].bodies.z?a:b;
                    int small=big==a?b:a;
                    merge(big,small); change.result.z=small; target=big;
                }
                ivec4 group=islands[target].contacts;
                contacts[id].xyz=ivec3(target,-1,group.x);
                if(group.x>=0) contacts[group.x].y=id; else group.y=id;
                group.x=id; group.z++; islands[target].contacts=group;
            }
            else
            {
                target=root(contacts[id].x);
                if(target<0 || status.x!=0) { status.x=5; return; }
                ivec4 member=contacts[id],group=islands[target].contacts;
                if(member.y>=0) contacts[member.y].z=member.z; else group.x=member.z;
                if(member.z>=0) contacts[member.z].y=member.y; else group.y=member.y;
                group.z--; islands[target].contacts=group; islands[target].state.z++;
                contacts[id].xyz=ivec3(-1);
            }
            change.result.y=target; changes[op]=change;
        }
        return;
    }
    if(status.x!=0) return;
    if(i<sizes.x && bodies[i].w!=0 && bodies[i].x>=0) bodies[i].x=root(bodies[i].x);
    if(i<sizes.y && contacts[i].w!=0 && contacts[i].x>=0) contacts[i].x=root(contacts[i].x);
    if(i<sizes.z && joints[i].w!=0 && joints[i].x>=0) joints[i].x=root(joints[i].x);
}
