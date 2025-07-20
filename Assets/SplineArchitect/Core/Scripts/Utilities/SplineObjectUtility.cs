// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: SplineObjectUtility.cs
//
// Author: Mikael Danielsson
// Date Created: 11-09-2024
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using Unity.Mathematics;
using UnityEngine;

using SplineArchitect.Objects;

namespace SplineArchitect.Utility
{
    public class SplineObjectUtility
    {
        /// <summary>
        /// Calculates and returns the combined world bounds of all mesh containers in an SplineObject.
        /// </summary>
        /// <param name="so">The SplineObject whose mesh containers are used to calculate the combined bounds.</param>
        /// <returns>The combined world bounds of the meshes within the SplineObject.</returns>
        /// <remarks>
        /// This function iterates through each mesh container, transforming their local bounds to world space and encapsulating them into a single bounding box.
        /// </remarks>
        public static Bounds GetCombinedBounds(SplineObject so)
        {
            Bounds bounds = new Bounds();
            bool initalized = false;

            foreach(MeshContainer mc in so.meshContainers)
            {
                Mesh sharedMesh = mc.GetInstanceMesh();

                if (sharedMesh == null)
                    continue;

                Bounds localBounds = sharedMesh.bounds;
                Vector3 worldCenter = so.transform.TransformPoint(localBounds.center);
                Vector3 worldSize = Vector3.Scale(localBounds.size, so.transform.lossyScale);
                Bounds worldBounds = new Bounds(worldCenter, worldSize);

                if (!initalized)
                {
                    initalized = true;
                    bounds = worldBounds;
                }
                else
                {
                    bounds.Encapsulate(worldBounds);
                }
            }

            return bounds;
        }

        /// <summary>
        /// Calculates the combined position of an SplineObject's parent hierarchy by summing their local spline positions.
        /// </summary>
        /// <param name="soParent">The SplineObject whose parent hierarchy positions will be combined.</param>
        /// <returns>The combined position of all parent SplineObject in the hierarchy.</returns>
        /// <remarks>
        /// This function traverses up to 25 levels of the parent hierarchy, summing the local spline positions of each parent.
        /// </remarks>
        public static Vector3 GetCombinedParentPositions(SplineObject soParent)
        {
            SplineObject parent = soParent;

            Vector3 value = Vector3.zero;

            for (int i = 0; i < 25; i++)
            {
                if (parent == null)
                    break;

                value += parent.localSplinePosition;
                parent = parent.soParent;
            }

            return value;
        }

        /// <summary>
        /// Calculates the combined scale of an SplineObject's parent hierarchy by multiplying their local scales.
        /// </summary>
        /// <param name="soParent">The SplineObject whose parent hierarchy scales will be combined.</param>
        /// <returns>The combined scale of all parent SplineObjects in the hierarchy.</returns>
        /// <remarks>
        /// This function traverses up to 25 levels of the parent hierarchy, multiplying the local scales of each parent.
        /// </remarks>
        public static Vector3 GetCombinedParentScales(SplineObject soParent)
        {
            SplineObject parent = soParent;

            Vector3 value = Vector3.one;

            for (int i = 0; i < 25; i++)
            {
                if (parent == null)
                    break;

                value = Vector3.Scale(value, parent.transform.localScale);
                parent = parent.soParent;
            }

            return value;
        }

        /// <summary>
        /// Calculates the combined rotation of an SplineObject's parent hierarchy by multiplying their local rotations.
        /// </summary>
        /// <param name="soParent">The SplineObject whose parent hierarchy rotations will be combined.</param>
        /// <returns>The combined rotation of all parent SplineObjects in the hierarchy.</returns>
        /// <remarks>
        /// This function traverses up to 25 levels of the parent hierarchy, multiplying the local rotations of each parent.
        /// </remarks>
        public static Quaternion GetCombinedParentRotations(SplineObject soParent)
        {
            SplineObject parent = soParent;
            Quaternion value = Quaternion.identity;

            for (int i = 0; i < 25; i++)
            {
                if (parent == null)
                    break;

                value = parent.localSplineRotation * value;
                parent = parent.soParent;
            }

            return value;
        }

        /// <summary>
        /// Calculates the combined transformation matrix of an SplineObject's parent hierarchy by multiplying their local transformation matrices.
        /// </summary>
        /// <param name="so">The SplineObject whose parent hierarchy matrices will be combined.</param>
        /// <returns>The combined transformation matrix of all parent SplineObjects in the hierarchy.</returns>
        /// <remarks>
        /// This function traverses up to 25 levels of the parent hierarchy, multiplying the local position, rotation, and scale of each parent.
        /// </remarks>
        public static float4x4 GetCombinedParentMatrixs(SplineObject so, bool forceSplineSpace = false)
        {
            float4x4 matrix = float4x4.identity;

            for (int i = 0; i < 25; i++)
            {
                if (so == null)
                    break;

                if (forceSplineSpace || so.type == SplineObject.Type.DEFORMATION)
                    matrix = math.mul(float4x4.TRS(so.localSplinePosition, so.localSplineRotation, so.transform.localScale), matrix);
                else
                    matrix = math.mul(float4x4.TRS(so.transform.localPosition, so.transform.localRotation, so.transform.localScale), matrix);

                so = so.soParent;
            }

            return matrix;
        }

        /// <summary>
        /// Calculates the combined hash codes of an SplineObject's parent hierarchy, summing the hash codes of each parent.
        /// </summary>
        /// <param name="so">The SplineObject whose parent hierarchy hash codes will be combined.</param>
        /// <returns>The combined hash code of all parent SplineObjects in the hierarchy.</returns>
        /// <remarks>
        /// This function traverses up to 25 levels of the parent hierarchy, summing the hash codes of each parent that is of type DEFORMATION.
        /// </remarks>
        public static int GetCombinedParentHashCodes(SplineObject so)
        {
            int haschCode = 0;

            for (int i = 0; i < 25; i++)
            {
                so = so.soParent;

                if (so == null)
                    break;

                if (so.type != SplineObject.Type.DEFORMATION)
                    break;

                haschCode += so.GetHashCode();
            }

            return haschCode;
        }
    }
}