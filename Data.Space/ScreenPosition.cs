﻿using System.Numerics;

namespace Data.Space
{
    public readonly record struct ScreenPosition(int X, int Y)
    {
        public ScreenPosition Min(ScreenPosition other)
        {
            return new ScreenPosition { X = Math.Min(X, other.X), Y = Math.Min(Y, other.Y) };
        }

        public ScreenPosition Max(ScreenPosition other)
        {
            return new ScreenPosition { X = Math.Max(X, other.X), Y = Math.Max(Y, other.Y) };
        }

        public ScreenPosition Clamp(ScreenSize size)
        {
            return new ScreenPosition(Math.Max(0, Math.Min(X, size.Width)), Math.Max(0, Math.Min(Y, size.Height)));
        }

        public Vector2 AsVector()
        {
            return new Vector2(X, Y);
        }

        public static ScreenPosition operator -(ScreenPosition left, ScreenPosition right) => new ScreenPosition(left.X - right.X, left.Y - right.Y);
        public static ScreenPosition operator +(ScreenPosition left, ScreenPosition right) => new ScreenPosition(left.X + right.X, left.Y + right.Y);
        public static ScreenPosition operator *(ScreenPosition left, float amount) => new ScreenPosition((int)Math.Round(left.X * amount), (int)Math.Round(left.Y * amount));
    }
}
