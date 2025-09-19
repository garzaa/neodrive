// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: DeformJob.cs
//
// Author: Mikael Danielsson
// Date Created: 12-02-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;

using SplineArchitect.Objects;
using SplineArchitect.Utility;

namespace SplineArchitect.Jobs
{
    [BurstCompile]
    public struct DeformJob : IJobParallelFor
    {
        public NativeArray<Vector3> vertices;
        public NativeArray<Vector3> forwardDir;
        public NativeArray<Vector3> upDir;
        public NativeArray<Vector3> rightDir;

        [ReadOnly] public NativeHashMap<int, float4x4> localSpaces;
        [ReadOnly] public NativeArray<int> verticesMap;
        [ReadOnly] public NativeArray<bool> mirrorMap;
        [ReadOnly] public NativeArray<bool> alignToEndMap;
        [ReadOnly] public NativeArray<NativeSegment> nativeSegments;
        [ReadOnly] public NativeArray<NoiseLayer> noises;
        [ReadOnly] public Vector3 splineUpDirection;
        [ReadOnly] public float splineLength;
        [ReadOnly] public NativeArray<SnapData> snapDatas;
        [ReadOnly] public NativeList<float> lengthMap;
        [ReadOnly] public NativeList<Vector3> normalsArray;
        [ReadOnly] public NativeList<Vector3> positionMap;
        [ReadOnly] public float splineResolution;
        [ReadOnly] public bool loop;
        [ReadOnly] public Spline.NormalType normalType;
        [ReadOnly] public SplineObject.Type deformationType;

        public void Execute(int i)
        {
            Vector3 vertice = vertices[i];
            int snapDataIndex = i;

            float4x4 localSpace;
            bool alignToEnd = false;
            if (deformationType == SplineObject.Type.FOLLOWER)
            {
                localSpace = localSpaces[verticesMap[i]];
                alignToEnd = alignToEndMap[i];
            }
            else
            {
                localSpace = localSpaces[0];

                for (int i2 = 0; i2 < verticesMap.Length; i2++)
                {
                    if (i < verticesMap[i2])
                    {
                        if (mirrorMap[i2])
                            vertice = -vertice;

                        localSpace = localSpaces[i2];
                        snapDataIndex = i2;
                        alignToEnd = alignToEndMap[i2];
                        break;
                    }
                }
            }

            //Vertice to spline position
            vertice = math.transform(localSpace, vertice);

            //Snapping, needs to be before time.
            if (snapDatas.Length > 0 && (snapDatas[snapDataIndex].end || snapDatas[snapDataIndex].start))
            {
                SnapData snapData = snapDatas[snapDataIndex];

                if (snapData.end && snapData.start)
                {
                    float point = snapData.soStartPoint + (snapData.soEndPoint - snapData.soStartPoint) / 2;

                    float partLength = vertice.z - point;
                    float length = Mathf.Abs(snapData.soEndPoint - snapData.soStartPoint) / 2;

                    if(partLength > 0) vertice.z = Mathf.Lerp(point, snapData.snapEndPoint, Mathf.Abs(partLength) / length);
                    else vertice.z = Mathf.Lerp(point, snapData.snapStartPoint, Mathf.Abs(partLength) / length);
                }
                else if (snapData.start)
                {
                    float partLength = Mathf.Abs(vertice.z - snapData.soEndPoint);
                    float length = Mathf.Abs(snapData.soStartPoint - snapData.soEndPoint);
                    vertice.z = Mathf.Lerp(snapData.soEndPoint, snapData.snapStartPoint, partLength / length);
                }
                else
                {
                    float partLength = Mathf.Abs(vertice.z - snapData.soStartPoint);
                    float length = Mathf.Abs(snapData.soEndPoint - snapData.soStartPoint);
                    vertice.z = Mathf.Lerp(snapData.soStartPoint, snapData.snapEndPoint, partLength / length);
                }
            }

            if (alignToEnd) vertice.z = splineLength - vertice.z;
            float time = vertice.z / splineLength;
            float fixedTime = SplineUtilityNative.TimeToFixedTime(lengthMap, splineResolution, time, loop);

            int segment = Mathf.Clamp(SplineUtility.GetSegment(nativeSegments.Length, fixedTime), 1, nativeSegments.Length - 1);
            float segmentTime = SplineUtility.GetSegmentTime(segment, nativeSegments.Length, fixedTime);
            float constrastedSegementTime = SplineUtilityNative.GetContrastedSegementTime(nativeSegments, segment, segmentTime);

            Vector3 splinePoint;
            Vector3 xDirection;
            Vector3 yDirection;
            Vector3 zDirection;

            //Is exstension
            if (!loop && (time >= 1 || time <= 0)) splinePoint = SplineUtilityNative.GetPositionExtended(nativeSegments, splineLength, time);
            else splinePoint = SplineUtilityNative.GetPositionFast(positionMap, nativeSegments, splineResolution, fixedTime);

            //Static normals
            if (normalType != Spline.NormalType.DYNAMIC)
            {
                //Get spline direction
                zDirection = SplineUtilityNative.GetDirection(nativeSegments, fixedTime);
                if (alignToEnd) zDirection = -zDirection;
                //Calculate directions
                xDirection = Vector3.Cross(zDirection, -splineUpDirection).normalized;
                yDirection = Vector3.Cross(xDirection, -zDirection).normalized;
                zDirection = -zDirection;
            }
            //Dynamic normals
            else
            {
                float n = fixedTime * (normalsArray.Length / 3);
                int normalIndex = (int)math.floor(n);

                //Note: Need to look in to this more later. Why is this needed?
                if (normalIndex < 0 || (normalIndex * 3) + 1 >= normalsArray.Length)
                    normalIndex = Mathf.Clamp(normalIndex, 0, (normalsArray.Length / 3) - 1);

                //Get directions
                xDirection = normalsArray[normalIndex * 3];
                yDirection = normalsArray[normalIndex * 3 + 1];
                zDirection = normalsArray[normalIndex * 3 + 2];
                if (alignToEnd)
                {
                    xDirection = -xDirection;
                    zDirection = -zDirection;
                }
            }

            //Saddle skew
            Vector2 saddleSkew = SplineUtilityNative.GetSadleSkew(nativeSegments, segment, constrastedSegementTime);
            vertice.y += saddleSkew.y * (vertice.x * vertice.x);
            vertice.x += saddleSkew.x * (vertice.y * vertice.y) * -Mathf.Sign(vertice.x);

            //Scale
            Vector2 scale = SplineUtilityNative.GetScale(nativeSegments, segment, constrastedSegementTime);
            vertice.x *= scale.x;
            vertice.y *= scale.y;

            //NoiseLayer
            float noiseModification = SplineUtilityNative.GetNoise(nativeSegments, segment, constrastedSegementTime);
            vertice.y += NoiseUtility.GetNoiseValue(noises, vertice, noiseModification);

            //Rotation
            Quaternion rotation = Quaternion.Inverse(SplineUtilityNative.GetZRotation(nativeSegments , - zDirection, fixedTime, constrastedSegementTime));
            xDirection = rotation * xDirection;
            yDirection = rotation * yDirection;

            //Calculate the new world position
            vertice = splinePoint + xDirection * vertice.x + yDirection * vertice.y;

            //To objects local space
            vertices[i] = math.transform(math.inverse(localSpace), vertice);
            forwardDir[i] = -zDirection;
            upDir[i] = yDirection;
            rightDir[i] = xDirection;
        }
    }
}
