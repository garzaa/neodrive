// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: PositionMapJob.cs
//
// Author: Mikael Danielsson
// Date Created: 24-03-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using Unity.Collections;

using SplineArchitect.Objects;
using SplineArchitect.Utility;

namespace SplineArchitect.Jobs
{
    [BurstCompile]
    public struct PositionMapJob : IJob
    {
        public NativeList<Vector3> positionMap;

        [ReadOnly] public NativeArray<NativeSegment> nativeSegments;
        [ReadOnly] public float resolution;

        public void Execute()
        {
            positionMap.Clear();

            for(float t = 0; t < 1; t += resolution)
            {
                positionMap.Add(SplineUtilityNative.GetPosition(nativeSegments, t));
            }

            positionMap.Add(SplineUtilityNative.GetPosition(nativeSegments, 1));
        }
    }
}
