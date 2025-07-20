// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EHandleMeshContainer.cs
//
// Author: Mikael Danielsson
// Date Created: 18-02-2024
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Generic;

using UnityEngine;

using SplineArchitect.Objects;
using SplineArchitect.Utility;

namespace SplineArchitect
{
    public static class EHandleMeshContainer
    {
        private static bool refresh;

        public static void Refresh()
        {
            refresh = true;
        }

        public static void RefreshAfterAssetModification(HashSet<Spline> splines)
        {
            if (!refresh)
                return;

            refresh = false;

            foreach (Spline spline in splines)
            {
                foreach (SplineObject so in spline.splineObjects)
                {
                    if (so == null || so.transform == null)
                        continue;

                    bool foundModification = false;

                    foreach (MeshContainer mc in so.meshContainers)
                    {
                        if (mc.TryUpdateTimestamp())
                        {
                            foundModification = true;
                            Update(so, mc);
                        }
                        else if (mc.HasReadabilityDif())
                        {
                            foundModification = true;
                            RefreshInstanceMesh(so, mc);
                        }
                    }

                    if (foundModification)
                    {
                        so.monitor.ForceUpdate();
                        EHandleSpline.MarkForInfoUpdate(spline);
                        EHandleDeformation.TryDeform(spline, false);
                    }
                }
            }
        }

        public static void Initialize(SplineObject so)
        {
            for (int i = 0; i < so.gameObject.GetComponentCount(); i++)
            {
                if (so.type != SplineObject.Type.DEFORMATION)
                    continue;

                Component component = so.gameObject.GetComponentAtIndex(i);
                MeshFilter meshFilter = component as MeshFilter;
                MeshCollider meshCollider = component as MeshCollider;

                Mesh sharedMesh = null;

                if (meshFilter != null)
                {
                    sharedMesh = meshFilter.sharedMesh;

                }
                else if (meshCollider != null)
                {
                    sharedMesh = meshCollider.sharedMesh;
                }

                if (sharedMesh == null)
                    continue;

                foreach (MeshContainer mc2 in so.meshContainers)
                {
                    if (mc2.Contains(component))
                        continue;
                }

                Mesh originMesh = GetOriginMeshFromMeshNameId(sharedMesh);

                if (originMesh != null && meshFilter != null)
                {
                    meshFilter.sharedMesh = originMesh;
                }
                else if (originMesh != null && meshCollider != null)
                {
                    meshCollider.sharedMesh = originMesh;
                }

                MeshContainer mc = new MeshContainer(component);
                so.AddMeshContainer(mc);
                mc.SetInstanceMesh(HandleCachedResources.FetchInstanceMesh(mc));
            }
        }

        public static void DeleteNull(SplineObject so)
        {
            for (int i = so.meshContainers.Count - 1; i >= 0; i--)
            {
                MeshContainer mc = so.meshContainers[i];
                if (mc.GetMeshContainerComponent() == null)
                    so.RemoveMeshContainer(mc);
            }
        }

        public static void TryUpdate(Spline spline, SplineObject so)
        {
            if (so.type != SplineObject.Type.DEFORMATION)
                return;

            bool changed = false;

            //Update meshContainers
            for (int i = so.meshContainers.Count - 1; i >= 0; i--)
            {
                MeshContainer mc = so.meshContainers[i];

                if (mc.MeshContainerExist() == false)
                    continue;

                Mesh instanceMesh = mc.GetInstanceMesh();

                if (instanceMesh == null)
                    continue;

                if (instanceMesh.name != mc.GetResourceKey())
                {
                    changed = true;
                    Update(so, mc);
                }
            }

            if (changed)
                EHandleSpline.MarkForInfoUpdate(spline);
        }

        public static void Update(SplineObject so, MeshContainer mc)
        {
            Mesh instanceMesh = mc.GetInstanceMesh();
            Mesh originMesh = GetOriginMeshFromMeshNameId(instanceMesh);
            mc.SetOriginMesh(originMesh == null ? instanceMesh : originMesh);
            mc.UpdateResourceKey();
            mc.SetInstanceMesh(HandleCachedResources.FetchInstanceMesh(mc));
            so.monitor.ForceUpdate();
        }

        public static Mesh GetOriginMeshFromMeshNameId(Mesh mesh)
        {
            Mesh originMesh = null;

            string[] data = mesh.name.Split('*');
            if (data.Length == 3)
            {
                if (int.TryParse(data[1], out int id))
                {
                    Object obj = UnityEditor.EditorUtility.InstanceIDToObject(id);
                    if(obj is Mesh) originMesh = (Mesh)UnityEditor.EditorUtility.InstanceIDToObject(id);
                }
            }

            return originMesh;
        }

        public static void RefreshInstanceMesh(SplineObject so, MeshContainer mc)
        {
            Mesh orginMesh = mc.GetOriginMesh();
            Mesh newInstanceMesh = Object.Instantiate(orginMesh);
            mc.SetInstanceMesh(newInstanceMesh);
            HandleCachedResources.AddOrUpdateInstanceMesh(mc);
            so.monitor.ForceUpdate();

            if (orginMesh.isReadable != newInstanceMesh.isReadable)
                Debug.LogError("Redable status dif error!");
        }
    }
}
