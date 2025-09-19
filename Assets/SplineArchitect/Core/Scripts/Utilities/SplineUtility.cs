// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: SplineUtility.cs
//
// Author: Mikael Danielsson
// Date Created: 11-09-2024
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;

using SplineArchitect.Objects;
using SplineArchitect.Jobs;

namespace SplineArchitect.Utility
{
    public static partial class SplineUtility
    {
        /// <summary>
        /// Creates an Spline component on the specified GameObject.
        /// </summary>
        /// <param name="go">The GameObject to add the Spline component to.</param>
        /// <returns>The created Spline component.</returns>
        public static Spline Create(GameObject go, List<Vector3> segmentPoints)
        {
            if (segmentPoints.Count < 6)
            {
                throw new ArgumentException("The segment list must contain at least 6 segmentPoints.", nameof(segmentPoints));
            }

            Spline spline = go.AddComponent<Spline>();

            for (int i = 0; i < segmentPoints.Count; i += 3)
            {
                Vector3 anchor = segmentPoints[i + 0];
                Vector3 tangentA = segmentPoints[i + 1];
                Vector3 tangentB = segmentPoints[i + 2];

                spline.CreateSegment(anchor, tangentA, tangentB);
            }

            spline.UpdateCachedData();

            return spline;
        }

        public static Spline GetClosest(Vector3 point, HashSet<Spline> splines, float distanceToBounds, out float time, out Vector3 nearestPoint)
        {
            Spline closest = null;
            float closestD = 99999;
            time = 0;
            nearestPoint = Vector3.zero;

            foreach (Spline spline in splines)
            {
                if (!spline.IsEnabled())
                    continue;

                if(spline.segments == null || spline.segments.Count < 2)
                    continue;

                float boundsD = Vector3.Distance(spline.controlPointsBounds.ClosestPoint(point), point);

                if (boundsD > distanceToBounds)
                    continue;

                Vector3 nPoint = spline.GetNearestPoint(point, out float time2, 8, 20);
                float d = Vector3.Distance(nPoint, point);

                if (closestD > d)
                {
                    time = time2;
                    closestD = d;
                    closest = spline;
                    nearestPoint = nPoint;
                }
            }

            return closest;
        }

        /// <summary>
        /// Converts a segment index and control point type to a unique control point ID in an Spline.
        /// </summary>
        /// <param name="segementIndex">The index of the segment in the spline.</param>
        /// <param name="type">The type of control point (Anchor, Tangent A, Tangent B).</param>
        /// <returns>A unique control point ID starting at 1000, calculated using the segment index and type.</returns>
        public static int SegmentIndexToControlPointId(int segementIndex, Segment.ControlHandle type)
        {
            return segementIndex * 3 + 1000 + ((int)type - 1);
        }

        /// <summary>
        /// Converts a control point ID back to its corresponding segment index in an Spline.
        /// </summary>
        /// <param name="controlPointId">The unique ID of the control point, starting at 1000.</param>
        /// <returns>The segment index associated with the given control point ID.</returns>
        public static int ControlPointIdToSegmentIndex(int controlPointId)
        {
            int value = (controlPointId - 1000) % 3;
            return ((controlPointId - 1000) - value) / 3;
        }

        /// <summary>
        /// Returns the opposite tangent type for a given tangent type in a segment.
        /// </summary>
        /// <param name="type">The current tangent type (Tangent A or Tangent B).</param>
        /// <returns>The opposite tangent type (Tangent B if input is Tangent A, and vice versa).</returns>
        public static Segment.ControlHandle GetOppositeTangentType(Segment.ControlHandle type)
        {
            return type == Segment.ControlHandle.TANGENT_A ? Segment.ControlHandle.TANGENT_B : Segment.ControlHandle.TANGENT_A;
        }

        /// <summary>
        /// Determines the type of control point (Anchor, Tangent A, or Tangent B) based on the control point ID.
        /// </summary>
        /// <param name="controlPointId">The unique ID of the control point.</param>
        /// <returns>The type of control point (Anchor, Tangent A, Tangent B, or NONE).</returns>
        public static Segment.ControlHandle GetControlPointType(int controlPointId)
        {
            if (controlPointId == 0) return Segment.ControlHandle.NONE;
            else if (controlPointId % 3 == 0) return Segment.ControlHandle.TANGENT_B;
            else if (controlPointId % 3 == 2) return Segment.ControlHandle.TANGENT_A;
            else return Segment.ControlHandle.ANCHOR;
        }

        /// <summary>
        /// Retrieves segments from active Splines that are located at a specified point within a given tolerance.
        /// </summary>
        /// <param name="activeSplines">A dictionary of active Splines to search.</param>
        /// <param name="worldPoint">The point in world space to check for segments.</param>
        /// <param name="epsilon">The tolerance within which a segment's anchor point is considered to match the specified point. Default is 0.001f.</param>
        /// <returns>A list of tuples containing the Spline and Segment found at the specified point.</returns>
        public static void GetSegmentsAtPointNoAlloc(List<Segment> closestSegmentContainer, HashSet<Spline> activeSplines, Vector3 worldPoint, float epsilon = 0.001f)
        {
            closestSegmentContainer.Clear();

            foreach (Spline spline in activeSplines)
            {
                if (spline == null)
                    continue;

                if (!spline.IsEnabled())
                    continue;

                Vector3 closestPoint = spline.controlPointsBounds.ClosestPoint(worldPoint);
                float distanceToBounds = Vector3.Distance(closestPoint, worldPoint);

                if (distanceToBounds > 15)
                    continue;

                for(int i = 0; i < spline.segments.Count; i++)
                {
                    Segment s = spline.segments[i];

                    if (spline.loop && spline.segments.Count - 1 == i)
                        continue;

                    float d = Vector3.Distance(s.GetPosition(Segment.ControlHandle.ANCHOR), worldPoint);

                    if (GeneralUtility.IsZero(d, epsilon))
                    {
                        closestSegmentContainer.Add(s);
                    }
                }
            }
        }

        public static int GetSegment(int length, float time)
        {
            int i = Mathf.CeilToInt(time * (length - 1));
            i = Mathf.Clamp(i, 1, length);
            return i;
        }

        /// <summary>
        /// Computes a time value for a specific segment in a path, adjusting based on the segment's position and the total time (0 - 1).
        /// </summary>
        /// <returns>The time value within the specified segment.</returns>
        public static float GetSegmentTime(int segment, float segmentsLength, float time)
        {
            float value = 0;
            segmentsLength = segmentsLength - 1;

            if (time > 0)
            {
                float sectionTime = 1 / segmentsLength;
                float currentSectionTime = sectionTime - ((segment / segmentsLength) - time);
                value = currentSectionTime / sectionTime;
            }

            return value;
        }

        /// <summary>
        /// Validates a time value depending on whether the spline is looped or not.
        /// </summary>
        /// <returns>The validated time value.</returns>
        public static float GetValidatedTime(float time, bool loop)
        {
            if (!loop)
                return Mathf.Clamp(time, 0, 1);

            if (time < 0)
                time = 1 - Mathf.Abs(time);
            else if (time > 1)
                time = time - Mathf.Floor(time);

            return time;
        }

        public static Vector3 GetValidatedPosition(Vector3 position, float splineLength, bool loop)
        {
            if (!loop) return position;

            if (position.z >= splineLength)
            {
                int loops = Mathf.FloorToInt(position.z / splineLength);
                position.z -= splineLength * loops;
            }

            if (position.z < 0)
            {
                int loops = Mathf.CeilToInt(Mathf.Abs(position.z) / splineLength);
                position.z += splineLength * loops;
            }

            return position;
        }

        public static float GetValidatedZDistance(Vector3 point1, Vector3 point2, float splineLength, bool loop)
        {
            point1 = GetValidatedPosition(point1, splineLength, loop);
            point2 = GetValidatedPosition(point2, splineLength, loop);

            float a = point2.z - point1.z;

            if (!loop) return a;

            float b = point2.z + (splineLength - point1.z);
            float closestToZero = Mathf.Abs(a) < Mathf.Abs(b) ? a : b;
            float c = (point2.z - splineLength) - point1.z;
            closestToZero = Mathf.Abs(closestToZero) < Mathf.Abs(c) ? closestToZero : c;
            return closestToZero;
        }
    }
}
