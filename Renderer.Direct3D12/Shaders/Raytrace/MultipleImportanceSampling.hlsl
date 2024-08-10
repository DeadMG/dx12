#pragma once

#include "Power.hlsl"

float powerHeuristic(int nf, float fPdf, int ng, float gPdf)
{
    float f = nf * fPdf;
    float g = ng * gPdf;
    return pow2(f) / (pow2(f) + pow2(g));
}
