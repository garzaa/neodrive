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
        private static List<Component> componentContainer = new List<Component>();

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

                bool allreadyExists = false;
                foreach (MeshContainer mc2 in so.meshContainers)
                {
                    if (mc2 != null && mc2.Contains(component))
                    {
                        allreadyExists = true;
                        break;
                    }
                }
                if (allreadyExists) continue;

                Mesh originMesh = ESplineObjectUtility.GetOriginMeshFromMeshNameId(sharedMesh);
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

        public static void DeleteUnvalidMeshContainers(SplineObject so)
        {
            for (int i = so.meshContainers.Count - 1; i >= 0; i--)
            {
                MeshContainer mc = so.meshContainers[i];

                if (mc == null)
                {
                    so.RemoveMeshContainer(mc);
                    continue;
                }

                if (mc.GetMeshContainerComponent() == null)
                {
                    so.RemoveMeshContainer(mc);
                    continue;
                }

                Mesh instanceMesh = mc.GetInstanceMesh();

                if (instanceMesh == null)
                {
                    so.RemoveMeshContainer(mc);
                }
            }
        }

        public static void DeleteDuplicates(Spline spline)
        {
            foreach(SplineObject so in spline.splineObjects)
            {
                if (so.meshContainers == null && so.meshContainers.Count < 2)
                    continue;

                componentContainer.Clear();
                for (int i = so.meshContainers.Count - 1; i >= 0; i--)
                {
                    MeshContainer mc = so.meshContainers[i];
                    Component c = mc.GetMeshContainerComponent();

                    if (componentContainer.Contains(c))
                    {
                        so.meshContainers.RemoveAt(i);
                        Debug.Log("[Spline Architect] Found MeshContainer duplicate! It's now removed. " +
                                  "this is for fixing a bug that can happen in older Spline Architect versions (1.2.5 or less).");
                    }
                    else
                    {
                        componentContainer.Add(c);
                    }
                }
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
            Mesh originMesh = ESplineObjectUtility.GetOriginMeshFromMeshNameId(instanceMesh);
            mc.SetOriginMesh(originMesh == null ? instanceMesh : originMesh);
            mc.UpdateResourceKey();
            mc.SetInstanceMesh(HandleCachedResources.FetchInstanceMesh(mc));
            so.monitor.ForceUpdate();
        }

        public static void RefreshInstanceMesh(SplineObject so, MeshContainer mc)
        {
            Mesh orginMesh = mc.GetOriginMesh();
            Mesh newInstanceMesh = Object.Instantiate(orginMesh);
            mc.SetInstanceMesh(newInstanceMesh);
            HandleCachedResources.AddOrUpdateInstanceMesh(mc);
            so.monitor.ForceUpdate();

            if (orginMesh.isReadable != newInstanceMesh.isReadable)
                Debug.LogError("[Spline Architect] Redable status dif error!");
        }
    }
}
