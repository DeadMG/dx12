#include "SphereHitGroup.hlsl"
#include "../Ray.hlsl"

ConstantBuffer<SphereHitGroupParameters> Sphere : register(b0);

[shader("intersection")]
void SphereIntersection()
{
    half3 direction = WorldRayDirection();
    half3 origin = WorldRayOrigin();
        
    half3 L = Sphere.WorldPosition - origin;
    half tca = dot(L, direction);
    half d2 = dot(L, L) - (tca * tca);
    
    if (d2 > Sphere.Size * Sphere.Size)
        return;
    
    half thc = sqrt((Sphere.Size * Sphere.Size) - d2);
    half t0 = tca + thc;
    half t1 = tca - thc;
    
    half tMin = min(t0, t1);
    half tMax = max(t0, t1);
    
    if (tMax < 0)
        return;
    
    half t = tMin < 0 ? tMax : tMin;
    SphereAttributes a;
    
    ReportHit(t, 0, a);
}
