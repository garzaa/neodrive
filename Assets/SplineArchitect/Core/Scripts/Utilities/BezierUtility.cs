// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: BezierUtility.cs
//
// Author: Mikael Danielsson
// Date Created: 11-06-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;

namespace SplineArchitect.Utility
{
    public static class BezierUtility
    {
        /// <summary>
        /// Performs a cubic interpolation between four points in 3D space using a Bezier curve approach.
        /// </summary>
        /// <returns>Returns the interpolated position along the curve at the specified time (0 - 1).</returns>
        public static Vector3 CubicLerp(Vector3 a, Vector3 ata, Vector3 btb, Vector3 b, float t)
        {
            float u = 1f - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            return a * uuu
                   + ata * (3f * uu * t)
                   + btb * (3f * u * tt)
                   + b * ttt;
        }

        /// <summary>
        /// Calculates the tangent vector at a specific point along a cubic Bezier curve, defined by four control points.
        /// </summary>
        /// <returns>Returns the normalized tangent vector at the specified parameter value along the curve.</returns>
        public static Vector3 GetTangent(Vector3 a, Vector3 ata, Vector3 btb, Vector3 b, float t)
        {
            float u = 1 - t;
            Vector3 tangent = 3 * u * u * (ata - a);
            tangent += 6 * u * t * (btb - ata);
            tangent += 3 * t * t * (b - btb);
            return Vector3.Normalize(tangent);
        }
    }
}
