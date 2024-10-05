#include "Colour.hlsl"
#include "Direction.hlsl"

struct AtrousData
{
    float Depth;
    Direction Normal;
};

bool filterData(AtrousData data)
{
    return data.Depth > 0;
}

uint2 packAtrous(AtrousData data)
{    
    return uint2(asuint(data.Depth), (uint)data.Normal);
}

AtrousData unpackAtrous(uint2 data)
{
    uint mask = (1 << 10) - 1;
    AtrousData ret;
    ret.Depth = asfloat(data.x);
    ret.Normal = (Direction) data.y;
    return ret;
}
