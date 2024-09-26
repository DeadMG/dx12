#pragma once

float4 pow2(float4 value)
{
    return value * value;
}

float pow2(float value)
{
    return value * value;
}

half pow2(half value)
{
    return value * value;
}

uint pow2(uint value)
{
    return value * value;
}

float zeroSafePow(float value, float exponent)
{
    if (value == 0)
        return 0;
    return pow(value, exponent);
}
