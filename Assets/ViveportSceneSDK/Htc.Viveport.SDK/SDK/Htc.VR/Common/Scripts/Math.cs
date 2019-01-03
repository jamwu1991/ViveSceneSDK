// ========================================================================== //
//
//  static class Math
//  -----
//  Purpose: Contains useful Math calculation routines
//
//
//  Created: 2016-10-12
//  Updated: 2016-11-04
//
//  Copyright 2016 HTC America Innovation
// 
// ========================================================================== //
using UnityEngine;

namespace Htc.VR
{
    /// <summary>
    /// Contains useful Math calculation routines
    /// </summary>
    public static class Math
    {
        // Apply small angle approximation if the angle in degrees is smaller than this value
        public const float SmallAngleLimit = 0.001f;

        /// <summary>
        /// Bound the input value within a range
        /// </summary>
        /// <param name="value">The input value</param>
        /// <param name="min">The lower bound</param>
        /// <param name="max">The upper bound</param>
        /// <returns>Output value</returns>
        public static float Bound(float value, float min, float max)
        {
            if (value > max)
                return max;
            else if (value < min)
                return min;
            else
                return value;
        }

        /// <summary>
        /// Calculates the position of Vector2(0, 1) after applying specified angles of curvature in radians
        /// </summary>
        /// <param name="angle">Angle in radians, [-pi, pi]</param>
        /// <returns>The position after applying the curvature</returns>
        public static Vector2 Curve(float angle)
        {
            var res = Vector2.up;

            if (Mathf.Abs(angle) > SmallAngleLimit)
            {
                res.x = (1 - Mathf.Cos(angle)) / angle;
                res.y = Mathf.Sin(angle) / angle;
            }
            else
            {
                res.x = angle / 2;
            }

            return res;
        }

        /// <summary>
        /// Bounds the given angle in degrees to [0, 360)
        /// </summary>
        /// <param name="angle">Angle in degrees</param>
        /// <returns></returns>
        public static float BoundAngle(float angle)
        {
            while (angle < 0)
                angle += 360;

            while (angle >= 360)
                angle -= 360;

            return angle;
        }

        /// <summary>
        /// Returns the angle between this vector and the positive X-axis in degrees
        /// </summary>
        /// <param name="vec"></param>
        /// <returns>Angle in degrees [-180, 180)</returns>
        public static float Angle(this Vector2 vec)
        {
            return Mathf.Atan2(vec.y, vec.x) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// Returns the signed angle, in degrees, between vectors from and to. Counter-clockwise angles are positive and clockwise angles are negative.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns>Angle in degrees [-180, 180)</returns>
        public static float Angle(Vector2 from, Vector2 to)
        {
            var angle = BoundAngle(to.Angle() - from.Angle());
            if (angle >= 180f)
                angle -= 360f;
            return angle;
        }
    }
}
