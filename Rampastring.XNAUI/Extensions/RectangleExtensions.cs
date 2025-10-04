using Microsoft.Xna.Framework;

namespace Rampastring.XNAUI.Extensions;

public static class RectangleExtensions
{
    public static Rectangle FromLocationAndSize(Point location, Point size)
#if XNA
        => new(location.X, location.Y, size.X, size.Y);    
#else
        => new(location, size);
#endif
    
    public static Point GetSize(this Rectangle rectangle)
#if XNA
        => new(rectangle.Width, rectangle.Height);    
#else
        => rectangle.Size;
#endif 
    
    public static void SetSize(this Rectangle rectangle, Point size)
#if XNA
        => (rectangle.Width, rectangle.Height) = (size.X, size.Y);    
#else
        => rectangle.Size = size;
#endif
    
    public static Rectangle WithSize(this Rectangle rectangle, Point size)
#if XNA
        => rectangle with { Width = size.X, Height = size.Y };    
#else
        => rectangle with { Size = size };
#endif
}