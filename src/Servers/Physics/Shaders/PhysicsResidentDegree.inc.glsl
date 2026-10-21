// Joint rows precede contacts in each retained incidence list.
uint jointDegree(uint body,uint contactStart,uint pointCount)
{
    uvec2 list=heads[body];uint at=list.x,count=0u;
    for(uint visited=0u;visited<list.y&&at!=none;visited++)
    {
        uint index=at>>1;if(index<contactStart)return count;
        if(index>=pointCount){fail();return list.y;}
        Constraint row=constraints[index];bool second=(at&1u)!=0u;
        if((second?row.bodies.y:row.bodies.x)!=body){fail();return list.y;}
        count++;at=second?row.bodies.w:row.bodies.z;
    }
    if(at!=none)fail();return count;
}
