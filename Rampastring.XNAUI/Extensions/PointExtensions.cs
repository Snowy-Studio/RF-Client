using Microsoft.Xna.Framework;

namespace Rampastring.XNAUI.Extensions;

public static class PointExtensions
{
    public static Point Add(this Point p1, Point p2)
#if XNA
        => new(p1.X + p2.X, p1.Y + p2.Y);
#else
        => p1 + p2;
#endif
    
    public static Point Subtract(this Point p1, Point p2)
#if XNA
        => new(p1.X - p2.X, p1.Y - p2.Y);
#else
        => p1 - p2;
#endif
    
    public static Point FromInt(int value)
#if XNA
        => new(value, value);
#else
        => new(value);
#endif
}