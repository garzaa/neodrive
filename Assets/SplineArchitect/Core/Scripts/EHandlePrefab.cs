// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EHandlePrefab.cs
//
// Author: Mikael Danielsson
// Date Created: 25-05-2024
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

using SplineArchitect.Objects;
using SplineArchitect.Utility;

namespace SplineArchitect
{
    public class EHandlePrefab
    {
        public static bool prefabStageOpen { get; private set; }
        public static bool prefabStageClosedLastFrame { get; private set; }

        private static bool UpdatedDeformations = false;

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void AfterAssemblyReload()
        {
            // Subscribe to the prefabStageOpened event
            PrefabStage.prefabStageOpened += OnPrefabStageOpened;
            PrefabStage.prefabStageClosing += OnPrefabStageClosing;
//#if UNITY_6000_0_OR_NEWER
//            PrefabUtility.prefabInstanceApplied += OnPrefabChange;
//#endif
            PrefabUtility.prefabInstanceReverted += OnPrefabChange;
            PrefabUtility.prefabInstanceUpdated += OnPrefabChange;

            EditorApplication.update += Update;
        }

        private static void OnPrefabChange(GameObject instance)
        {
            UpdatedPrefabDeformations();
        }

        public static void OnPrefabStageOpened(PrefabStage prefabStage)
        {
            UpdatedPrefabDeformations();
            prefabStageOpen = true;
        }

        public static void OnPrefabStageClosing(PrefabStage prefabStage)
        {
            UpdatedPrefabDeformations();
            prefabStageOpen = false;
            prefabStageClosedLastFrame = true;
        }

        public static void Update()
        {
            UpdatedDeformations = false;

            if (prefabStageClosedLastFrame)
            {
                prefabStageClosedLastFrame = false;
            }
        }

        public static bool IsPartOfAnyPrefab(GameObject go)
        {
            return PrefabUtility.IsPartOfAnyPrefab(go);
        }

        public static bool IsPrefabStageActive()
        {
            return PrefabStageUtility.GetCurrentPrefabStage() != null;
        }

        public static PrefabAssetType GetPrefabAssetType(GameObject go)
        {
            return PrefabUtility.GetPrefabAssetType(go);
        }

        public static bool IsPartOfActivePrefabStage(GameObject go)
        {
            if (PrefabStageUtility.GetPrefabStage(go) != null)
                return true;

            //Will be true when creating new spline:s inside prefabs.
            if (go.transform.parent != null && PrefabStageUtility.GetPrefabStage(go.transform.parent.gameObject) && IsPrefabStageActive())
                return true;

            //prefabStageClosing only runs during one frame. The frame after PrefabStage.prefabStageClosing += OnPrefabStageClosing.
            if (prefabStageClosedLastFrame)
                return true;

            return false;
        }

        private static void UpdatedPrefabDeformations()
        {
            if (UpdatedDeformations)
                return;

            UpdatedDeformations = true;

            foreach (Spline spline in HandleRegistry.GetSplines())
            {
                if (spline == null) 
                    continue;

                if (!IsPartOfAnyPrefab(spline.gameObject) && !IsPartOfActivePrefabStage(spline.gameObject))
                    return;

                foreach (SplineObject so in spline.splineObjects)
                {
                    if (so == null || so.transform == null)
                        continue;

                    if (so.type == SplineObject.Type.DEFORMATION && so.meshContainers != null && so.meshContainers.Count > 0)
                    {
                        so.SyncInstanceMeshesFromCache();
                        HandleDeformationWorker.Deform(so, DeformationWorker.Type.EDITOR, spline);
                    }
                    else if (so.type == SplineObject.Type.FOLLOWER)
                    {
                        //Need to sync MeshContainers becouse the follower can have an instaceMesh without a MeshContainer.
                        so.SyncMeshContainers();

                        foreach(MeshContainer mc in so.meshContainers)
                        {
                            Mesh mesh = mc.GetInstanceMesh();

                            if (mesh == null)
                                continue;

                            string assetPath = GeneralUtility.GetAssetPath(mesh);

                            if (assetPath == "")
                            {
                                Mesh originMesh = ESplineObjectUtility.GetOriginMeshFromMeshNameId(mesh);
                                mc.SetOriginMesh(originMesh);
                                mc.SetInstanceMeshToOriginMesh();
                            }
                        }

                        spline.followerUpdateList.Add(so);
                    }
                }
            }
        }
    }
}

#endif
