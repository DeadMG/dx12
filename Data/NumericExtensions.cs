namespace Data
{
    public static class NumericExtensions
    {
        public static float ToRadians(this float val)
        {
            return (float)((Math.PI / 180) * val);
        }
    }
}
