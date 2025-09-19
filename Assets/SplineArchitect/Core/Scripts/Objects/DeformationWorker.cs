// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: DeformationWorker.cs
//
// Author: Mikael Danielsson
// Date Created: 02-07-2024
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;

using SplineArchitect.Utility;
using SplineArchitect.Jobs;

namespace SplineArchitect.Objects
{
    public class DeformationWorker
    {
        public enum Type
        {
            NONE,
            EDITOR,
            RUNETIME
        }

        public enum State
        {
            IDLE,
            READY,
            READY_AND_FULL,
            WORKING
        }

        //General public
        public Type type;
        public State state { get; private set; }
        public Spline spline { get; private set; }

        //General private
        private float splineLength;

        //Job
        private JobHandle jobHandle;
        private DeformJob deformJob;

        //Lists
        private List<SplineObject> splineObjects = new List<SplineObject>();
        private int totalVertices;

        public DeformationWorker(Type type)
        {
            state = State.IDLE;
            this.type = type;
        }

        public void Add(SplineObject so, Spline spline)
        {
            if (so.meshContainers.Count == 0)
            {
                Debug.LogWarning($"[Spline Architect] Tried to add SplineObject {so.name} with empty meshContainers list.");
                return;
            }

            int vertices = 0;

            foreach (MeshContainer mc in so.meshContainers)
            {
                Mesh instanceMesh = mc.GetInstanceMesh();
                if (instanceMesh == null)
                    return;

                vertices += instanceMesh.vertexCount;
            }

            totalVertices += vertices;
            splineObjects.Add(so);

            if (this.spline == null)
                this.spline = spline;

            if (state == State.IDLE)
            {
                state = State.READY;
                splineLength = spline.length;
            }

            if (totalVertices > 25000)
                state = State.READY_AND_FULL;
        }

        public void Start()
        {
            if (spline == null)
            {
                Reset();
                return;
            }

            NativeArray<Vector3> vertices = new NativeArray<Vector3>(totalVertices, Allocator.TempJob);
            NativeHashMap<int, float4x4> localSpaces = new NativeHashMap<int, float4x4>(splineObjects.Count, Allocator.TempJob);
            NativeArray<int> verticesMap = new NativeArray<int>(splineObjects.Count, Allocator.TempJob);
            NativeArray<bool> mirrorMap = new NativeArray<bool>(splineObjects.Count, Allocator.TempJob);
            NativeArray<bool> alignToEndMap = new NativeArray<bool>(splineObjects.Count, Allocator.TempJob);
            NativeArray<SnapData> snapDatas = new NativeArray<SnapData>(splineObjects.Count, Allocator.TempJob);
            int offset = 0;

            //Set deform data
            //SplineObjects
            for (int i = 0; i < splineObjects.Count; i++)
            {
                SplineObject so = splineObjects[i];

                if(so == null)
                {
                    Reset();
                    return;
                }

                float4x4 matrix = SplineObjectUtility.GetCombinedParentMatrixs(so);
                localSpaces.Add(i, matrix);

                //MeshContainers
                for (int i2 = 0; i2 < so.meshContainers.Count; i2++)
                {
                    MeshContainer mc = so.meshContainers[i2];
                    Vector3[] originVertices = HandleCachedResources.FetchOriginVertices(mc);
                    NativeArray<Vector3>.Copy(originVertices, 0, vertices, offset, originVertices.Length);
                    offset += originVertices.Length;
                }

                mirrorMap[i] = so.mirrorDeformation;
                alignToEndMap[i] = so.alignToEnd;
                verticesMap[i] = offset;

                if(so.snapMode != SplineObject.SnapMode.NONE) snapDatas[i] = so.CalculateSnapData();
            }

            deformJob = DeformationUtility.CreateDeformJob(spline, 
                                                           vertices, 
                                                           spline.nativeSegmentsLocal, 
                                                           localSpaces, 
                                                           verticesMap, 
                                                           mirrorMap, 
                                                           SplineObject.Type.DEFORMATION, 
                                                           alignToEndMap, 
                                                           snapDatas);

            jobHandle = deformJob.Schedule(deformJob.vertices.Length, 1);
            state = State.WORKING;
        }

        public void Complete(Action<SplineObject, MeshContainer, Vector3[]> completeAction)
        {
            jobHandle.Complete();

            int verticesId = 0;

            for (int i = 0; i < splineObjects.Count; i++)
            {
                SplineObject so = splineObjects[i];

                if (so == null)
                    continue;

                so.transform.localPosition = so.localSplinePosition;
                so.transform.localRotation = so.localSplineRotation;

                so.monitor.UpdateSplineLength(splineLength);

                for (int y = 0; y < so.meshContainers.Count; y++)
                {
                    MeshContainer mc = so.meshContainers[y];
                    Vector3[] vertices = HandleCachedResources.FetchNewVerticesContainer(mc);
                    NativeArray<Vector3>.Copy(deformJob.vertices, verticesId, vertices, 0, vertices.Length);
                    verticesId += vertices.Length;
                    completeAction.Invoke(so, mc, vertices);
                }

                so.UpdateExternalComponents();
            }

            DeformationUtility.DisposeDeformJob(deformJob);
            Reset();
        }

        public bool IsCompleted()
        {
            return jobHandle.IsCompleted;
        }

        private void Reset()
        {
            //Clear lists
            splineObjects.Clear();

            //Reset data
            type = Type.NONE;
            spline = null;
            state = State.IDLE;
            splineLength = 0;
            totalVertices = 0;
        }
    }
}
