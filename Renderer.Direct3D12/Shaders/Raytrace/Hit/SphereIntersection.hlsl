#include "SphereHitGroup.hlsl"
#include "../Ray.hlsl"

ConstantBuffer<SphereHitGroupParameters> Sphere : register(b0);

[shader("intersection")]
void SphereIntersection()
{
    float3 direction = WorldRayDirection();
    float3 origin = WorldRayOrigin();
        
    float3 L = Sphere.WorldPosition - origin;
    float tca = dot(L, direction);
    float d2 = dot(L, L) - (tca * tca);
    
    if (d2 > Sphere.Size * Sphere.Size)
        return;
    
    float thc = sqrt((Sphere.Size * Sphere.Size) - d2);
    float t0 = tca + thc;
    float t1 = tca - thc;
    
    float tMin = min(t0, t1);
    float tMax = max(t0, t1);
    
    if (tMax < 0)
        return;
    
    float t = tMin < 0 ? tMax : tMin;
    SphereAttributes a;
    
    ReportHit(t, 0, a);
}
