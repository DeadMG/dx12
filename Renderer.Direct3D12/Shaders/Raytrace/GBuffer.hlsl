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
    return uint2(asuint(data.Depth), asuint(data.Normal.X));
}

AtrousData unpackAtrous(uint2 data)
{
    uint mask = (1 << 10) - 1;
    AtrousData ret;
    ret.Depth = asfloat(data.x);
    ret.Normal.X = asint(data.y & mask);
    ret.Normal.Y = asint((data.y >> 10) & mask);
    ret.Normal.Z = asint((data.y >> 20) & mask);
    return ret;
}
