using System;
using System.Runtime.CompilerServices;

namespace Rampastring.XNAUI.Extensions;

public static class MathExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Clamp(int value, int min, int max)
    {
#if NET6_0_OR_GREATER
        return Math.Clamp(value, min, max);
#else
        if (min > max)
            throw new ArgumentException("Max must be greater than min.");

        if (value < min)
            return min;
        if (value > max)
            return max;

        return value;
#endif
    }
}