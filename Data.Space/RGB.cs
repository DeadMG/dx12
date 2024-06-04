namespace Data.Space
{
    public readonly record struct RGB(float R, float G, float B)
    {
        public RGBA A(float A) => new RGBA(R, G, B, A);
        public RGBA A() => A(1);
    }
}
