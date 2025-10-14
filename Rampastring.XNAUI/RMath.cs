using System;
using Microsoft.Xna.Framework;

namespace Rampastring.XNAUI;

/// <summary>
/// Contains static vector math functions.
/// </summary>
public static class RMath
{
    /// <summary>
    /// Returns the angle / direction of a vector in radians.
    /// </summary>
    /// <param name="vector">The vector.</param>
    public static float AngleFromVector(Vector2 vector)
    {
        return (float)Math.Atan2(vector.Y, vector.X);
    }

    /// <summary>
    /// Creates and returns a new vector with the given length and angle.
    /// </summary>
    /// <param name="length">The length of the vector.</param>
    /// <param name="angle">The angle of the vector.</param>
    public static Vector2 VectorFromLengthAndAngle(float length, float angle)
    {
        return new Vector2(length * (float)Math.Cos(angle), length * (float)Math.Sin(angle));
    }

    public static Color MultiplyAlpha(Color color)
    {
        return new Color((color.A * color.R) / 255,
            (color.A * color.G) / 255,
            (color.A * color.B) / 255,
            color.A);
    }
}
