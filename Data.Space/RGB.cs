namespace Data.Space
{
    public readonly record struct RGB(float R, float G, float B)
    {
        public RGBA A(float A) => new RGBA(R, G, B, A);
        public RGBA A() => A(1);

        public static RGB From255(float r, float g, float b) => new RGB(r / 255.0f, g / 255.0f, b / 255.0f);
    }
}
