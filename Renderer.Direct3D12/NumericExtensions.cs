namespace Renderer.Direct3D12
{
    internal static class NumericExtensions
    {
        public static uint Align(this uint value, uint amount)
        {
            return Vortice.Mathematics.MathHelper.AlignUp(value, amount);
        }

        public static uint Align(this ulong value, uint amount)
        {
            return Vortice.Mathematics.MathHelper.AlignUp((uint)value, amount);
        }

        public static int Align(this int value, uint amount)
        {
            return (int)Vortice.Mathematics.MathHelper.AlignUp((uint)value, amount);
        }
    }
}
