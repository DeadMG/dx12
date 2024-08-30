#pragma once

struct Colour
{
    uint8_t4_packed value;
};

float4 asFloat(Colour c)
{
    return unpack_u8u32(c.value) / 255.0f;
}

Colour asColour(float4 c)
{
    Colour col;
    col.value = pack_u8(c * 255.0f);
    return col;
}
