// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: Spline_CachedData.cs
//
// Author: Mikael Danielsson
// Date Created: 25-03-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;

using UnityEngine;
using Unity.Collections;
using Unity.Jobs;

using SplineArchitect.Utility;
using SplineArchitect.Jobs;

namespace SplineArchitect.Objects
{
    public partial class Spline : MonoBehaviour
    {
        public const float boundsOffset = 2;

        //Public, stored
        [HideInInspector]
        public bool calculateLocalPositions = true;
        [HideInInspector]
        public bool calculateControlPointBounds = true;

        //Public, runtime
        [NonSerialized]
        public Bounds bounds;
        [NonSerialized]
        public Bounds controlPointsBounds;
        [NonSerialized]
        public NativeList<float> distanceMap;
        [NonSerialized]
        public NativeList<Vector3> normalsLocal;
        [NonSerialized]
        public NativeList<Vector3> positionMapLocal;
        [NonSerialized]
        public NativeList<Vector3> positionMap;
        [NonSerialized]
        public NativeArray<NativeSegment> nativeSegmentsLocal;
        [NonSerialized]
        public NativeArray<NoiseLayer> nativeNoises;

        //Private, stored
        [HideInInspector]
        [SerializeField]
        private float resolutionSplineData = 500;
        [HideInInspector]
        [SerializeField]
        private float samplingStepsSplineData = 10;
        [HideInInspector]
        [SerializeField]
        private float resolutionNormal = 1000;

        public void UpdateCachedData()
        {
            CacheSegmentsLocal();
            CacheNoises();

            if (!distanceMap.IsCreated)
                distanceMap = new NativeList<float>(0, Allocator.Persistent);

            if (!normalsLocal.IsCreated)
                normalsLocal = new NativeList<Vector3>(0, Allocator.Persistent);

            if (!positionMapLocal.IsCreated)
                positionMapLocal = new NativeList<Vector3>(0, Allocator.Persistent);

            if (segments.Count > 1)
            {
                CalculateSplineData();

                if (normalType == NormalType.DYNAMIC)
                    CalculateCachedNormals();

#if UNITY_EDITOR
                if (!positionMap.IsCreated)
                    positionMap = new NativeList<Vector3>(0, Allocator.Persistent);

                CalculatePositionMap(Space.World);
                CalculateSplineBounds();
#endif
            }

            if (segments.Count > 0)
            {
                if (calculateControlPointBounds)
                    CalculateControlpointBounds();
            }

            void CacheSegmentsLocal()
            {
                if (nativeSegmentsLocal.IsCreated)
                    nativeSegmentsLocal.Dispose();

                nativeSegmentsLocal = SplineUtilityNative.ToNativeArray(segments, Space.Self, Allocator.Persistent);
            }

            void CacheNoises()
            {
                if (nativeNoises.IsCreated)
                    nativeNoises.Dispose();

                nativeNoises = new NativeArray<NoiseLayer>(noises.Count, Allocator.Persistent);

                for (int i = 0; i < noises.Count; i++)
                {
                    if (!noises[i].enabled)
                        continue;

                    if (noises[i].group == noiseGroupMesh)
                        nativeNoises[i] = noises[i];
                }
            }
        }

        /// <summary>
        /// Disposes all native data used for calculations.
        /// </summary>
        public void DisposeCachedData()
        {
            if (distanceMap.IsCreated)
                distanceMap.Dispose();

            if (normalsLocal.IsCreated)
                normalsLocal.Dispose();

            if (positionMapLocal.IsCreated)
                positionMapLocal.Dispose();

#if UNITY_EDITOR
            if (positionMap.IsCreated)
                positionMap.Dispose();
#endif

            if (nativeSegmentsLocal.IsCreated)
                nativeSegmentsLocal.Dispose();

            if (nativeNoises.IsCreated)
                nativeNoises.Dispose();
        }

        /// <summary>
        /// Calculates the bounding box for all control points.
        /// </summary>
        public void CalculateControlpointBounds()
        {
            Vector3 min = Vector3.positiveInfinity;
            Vector3 max = Vector3.negativeInfinity;

            foreach (Segment s in segments)
            {
                min.x = Mathf.Min(min.x, s.GetPosition(Segment.ControlHandle.ANCHOR).x);
                min.x = Mathf.Min(min.x, s.GetPosition(Segment.ControlHandle.TANGENT_A).x);
                min.x = Mathf.Min(min.x, s.GetPosition(Segment.ControlHandle.TANGENT_B).x);

                min.y = Mathf.Min(min.y, s.GetPosition(Segment.ControlHandle.ANCHOR).y);
                min.y = Mathf.Min(min.y, s.GetPosition(Segment.ControlHandle.TANGENT_A).y);
                min.y = Mathf.Min(min.y, s.GetPosition(Segment.ControlHandle.TANGENT_B).y);

                min.z = Mathf.Min(min.z, s.GetPosition(Segment.ControlHandle.ANCHOR).z);
                min.z = Mathf.Min(min.z, s.GetPosition(Segment.ControlHandle.TANGENT_A).z);
                min.z = Mathf.Min(min.z, s.GetPosition(Segment.ControlHandle.TANGENT_B).z);

                max.x = Mathf.Max(max.x, s.GetPosition(Segment.ControlHandle.ANCHOR).x);
                max.x = Mathf.Max(max.x, s.GetPosition(Segment.ControlHandle.TANGENT_A).x);
                max.x = Mathf.Max(max.x, s.GetPosition(Segment.ControlHandle.TANGENT_B).x);

                max.y = Mathf.Max(max.y, s.GetPosition(Segment.ControlHandle.ANCHOR).y);
                max.y = Mathf.Max(max.y, s.GetPosition(Segment.ControlHandle.TANGENT_A).y);
                max.y = Mathf.Max(max.y, s.GetPosition(Segment.ControlHandle.TANGENT_B).y);

                max.z = Mathf.Max(max.z, s.GetPosition(Segment.ControlHandle.ANCHOR).z);
                max.z = Mathf.Max(max.z, s.GetPosition(Segment.ControlHandle.TANGENT_A).z);
                max.z = Mathf.Max(max.z, s.GetPosition(Segment.ControlHandle.TANGENT_B).z);
            }

            Vector3 offset = new Vector3(boundsOffset, boundsOffset, boundsOffset);
            controlPointsBounds.SetMinMax(min - offset, max + offset);
        }

        public void CalculateSplineBounds()
        {
            Vector3 min = Vector3.positiveInfinity;
            Vector3 max = Vector3.negativeInfinity;

            float precision = 1 / (25 * (length / 100));

            for (float time = 0; time < 1; time += precision)
            {
                UpdateMinMax(time);
            }

            //Make sure to get end.
            UpdateMinMax(1);

            Vector3 offset = new Vector3(boundsOffset, boundsOffset, boundsOffset);
            bounds.SetMinMax(min - offset, max + offset);

            void UpdateMinMax(float time)
            {
                Vector3 position = GetPosition(time);
                min.x = Mathf.Min(min.x, position.x);
                min.y = Mathf.Min(min.y, position.y);
                min.z = Mathf.Min(min.z, position.z);

                max.x = Mathf.Max(max.x, position.x);
                max.y = Mathf.Max(max.y, position.y);
                max.z = Mathf.Max(max.z, position.z);
            }
        }

        /// <summary>
        /// Computes and caches positions.
        /// </summary>
        public void CalculatePositionMap(Space space)
        {
            PositionMapJob positionMapJob = new PositionMapJob()
            {
                positionMap = space == Space.Self ? positionMapLocal : positionMap,
                nativeSegments = space == Space.Self ? nativeSegmentsLocal : SplineUtilityNative.ToNativeArray(segments, Space.World),
                resolution = GetResolutionSpline()
            };

            JobHandle jobHandle = positionMapJob.Schedule();
            jobHandle.Complete();
            if (space == Space.World)
                positionMapJob.nativeSegments.Dispose();
        }

        /// <summary>
        /// Computes and stores a mapping of time values to their corresponding positions, ensuring accurate length proportionality.
        /// </summary>
        public void CalculateSplineData()
        {
            SplineDataJob splineDataJob = new SplineDataJob()
            {
                distanceMap = distanceMap,
                positionMapLocal = positionMapLocal,
                splineLength = new NativeArray<float>(1, Allocator.TempJob),
                segmentZPositions = new NativeArray<float>(segments.Count, Allocator.TempJob),
                segmentLengths = new NativeArray<float>(segments.Count, Allocator.TempJob),
                nativeSegments = nativeSegmentsLocal,
                resolution = GetResolutionSpline(),
                samplingStep = GetSamplingStepDistanceMap(),
                calculateLocalPositions = calculateLocalPositions
            };

            JobHandle jobHandle = splineDataJob.Schedule();
            jobHandle.Complete();

            for (int i = 0; i < segments.Count; i++)
            {
                Segment s = segments[i];
                s.zPosition = splineDataJob.segmentZPositions[i];
                s.length = splineDataJob.segmentLengths[i];
            }

            length = splineDataJob.splineLength[0];
            splineDataJob.splineLength.Dispose();
            splineDataJob.segmentZPositions.Dispose();
            splineDataJob.segmentLengths.Dispose();

            if(!calculateLocalPositions)
            {
                positionMapLocal.Clear();
                positionMapLocal.TrimExcess();
            }
        }

        /// <summary>
        /// Calculates and caches normals along the spline.
        /// </summary>
        public void CalculateCachedNormals()
        {
            NormalsJob cachedNormalsJob = new NormalsJob()
            {
                normals = normalsLocal,
                splineUpDirection = Vector3.up,
                nativeSegments = nativeSegmentsLocal,
                normalResolution = GetResolutionNormal()
            };

            JobHandle jobHandle = cachedNormalsJob.Schedule();
            jobHandle.Complete();
        }

        /// <summary>
        /// Retrieves the resolution length map value.
        /// </summary>
        /// <param name="rawValue">If true, returns the raw value.</param>
        /// <returns>The resolution length map.</returns>
        public float GetResolutionSpline(bool rawValue = false)
        {
            if (rawValue) return resolutionSplineData;

            float value = 1 / (resolutionSplineData - 1);
            return Mathf.Clamp(value, 0.00001f, 0.1f);
        }

        /// <summary>
        /// Gets the resolution normal value.
        /// </summary>
        /// <param name="rawValue">If true, returns the raw value.</param>
        /// <returns>The resolution normal.</returns>
        public float GetResolutionNormal(bool rawValue = false)
        {
            if (rawValue) return resolutionNormal;

            float value = 1 / (resolutionNormal * (length / 100));
            return Mathf.Clamp(value, 0.00001f, 0.25f);
        }

        /// <summary>
        /// Retrieves the sampling step distance map.
        /// </summary>
        /// <param name="rawValue">If true, returns the raw value.</param>
        /// <returns>The sampling step distance map.</returns>
        public float GetSamplingStepDistanceMap(bool rawValue = false)
        {
            if (rawValue) return samplingStepsSplineData;

            return GetResolutionSpline() / samplingStepsSplineData;
        }

        /// <summary>
        /// Sets the resolution length map value.
        /// </summary>
        /// <param name="value">The value to set.</param>
        public void SetResolutionSplineData(float value)
        {
            resolutionSplineData = value;
        }

        /// <summary>
        /// Sets the resolution normal value.
        /// </summary>
        /// <param name="value">The value to set.</param>
        public void SetResolutionNormal(float value)
        {
            resolutionNormal = value;
        }

        /// <summary>
        /// Sets the sampling step distance map, clamped between 3 and 1000.
        /// </summary>
        /// <param name="value">The value to set.</param>
        public void SetSamplingStepDistanceMap(float value)
        {
            samplingStepsSplineData = Mathf.Clamp(value, 3, 1000);
        }
    }
}
