// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: MeshUtility.cs
//
// Author: Mikael Danielsson
// Date Created: 29-10-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Generic;

using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

using SplineArchitect.Jobs;
using SplineArchitect.Objects;

namespace SplineArchitect.Utility
{
    public class MeshUtility
    {
        private static List<int> triangleContainer = new List<int>();

        /// <summary>
        /// Copies triangle indices from an original mesh to a target mesh for all submeshes, maintaining the original topology.
        /// </summary>
        public static void SetOriginalTriangles(Mesh mesh, Mesh originalMesh)
        {
            for (int subMeshIndex = 0; subMeshIndex < originalMesh.subMeshCount; subMeshIndex++)
            {
                triangleContainer.Clear();
                originalMesh.GetTriangles(triangleContainer, subMeshIndex);
                mesh.SetTriangles(triangleContainer, subMeshIndex);
            }
        }

        /// <summary>
        /// Reverses the triangle indices for each submesh in a mesh, flipping the mesh's normals.
        /// </summary>
        public static void ReverseTriangles(Mesh mesh)
        {
            for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++)
            {
                triangleContainer.Clear();

                mesh.GetTriangles(triangleContainer, subMeshIndex);

                for (int i = 0; i < triangleContainer.Count; i += 3)
                {
                    int temp = triangleContainer[i];
                    triangleContainer[i] = triangleContainer[i + 1];
                    triangleContainer[i + 1] = temp;
                }

                mesh.SetTriangles(triangleContainer, subMeshIndex);
            }
        }

        /// <summary>
        /// Handles the normalization and tangential properties of a mesh based on configuration settings, reversing triangle orientations for mirror deformations.
        /// </summary>
        /// <returns>The modified mesh with updated normals and tangents.</returns>
        public static Mesh HandleOrthoNormals(MeshContainer mc, SplineObject so)
        {
            Mesh mesh = mc.GetInstanceMesh();
            if (mc.IsMeshFilter())
            {
                //Normals
                if (so.normalType == SplineObject.NormalType.DEFAULT || so.normalType == SplineObject.NormalType.SEAMLESS)
                    mesh.RecalculateNormals();

                //Tangents
                if (so.tangentType == SplineObject.TangentType.DEFAULT)
                    mesh.RecalculateTangents();
            }

            return mesh;
        }

        /// <summary>
        /// Recalculates seamless normals of a mesh.
        /// </summary>
        public static void RecalculateNormalsSeamlessWithJobs(MeshContainer mc)
        {
            Mesh mesh = mc.GetInstanceMesh();

            NativeHashMap<int, int> vertexMap = new NativeHashMap<int, int>(mesh.vertexCount, Allocator.TempJob);
            NativeArray<Vector3> vertecies = new NativeArray<Vector3>(HandleCachedResources.FetchOriginVertices(mc), Allocator.TempJob);
            JobHandle jobHandle;

            //Iterate through all submeshes
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                int[] subMeshTriangles = mesh.GetTriangles(i);
                if (subMeshTriangles == null || subMeshTriangles.Length == 0) continue;

                triangleContainer.Clear();

                //Create and schedule a TriangleLinkingJob for the current submesh
                TriangleLinkingJob triangleLinkingJob = new TriangleLinkingJob()
                {
                    triangles = new NativeArray<int>(subMeshTriangles, Allocator.TempJob),
                    vertices = vertecies,
                    vertextMap = vertexMap
                };

                jobHandle = triangleLinkingJob.Schedule();
                jobHandle.Complete();

                foreach(int i2 in triangleLinkingJob.triangles)
                    triangleContainer.Add(i2);

                //Set the linked triangles for the current submesh
                mesh.SetTriangles(triangleContainer, i);

                //Dispose of the nativeArray.
                triangleLinkingJob.triangles.Dispose();
            }

            vertexMap.Dispose();
            vertecies.Dispose();
        }

        /// <summary>
        /// Recalculates seamless normals of a mesh.
        /// </summary>
        public static void RecalculateNormalsSeamlessWithJobs(SplineObject so)
        {
            foreach(MeshContainer mc in so.meshContainers)
            {
                RecalculateNormalsSeamlessWithJobs(mc);
            }
        }
    }
}
