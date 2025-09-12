// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EHandleScene.cs
//
// Author: Mikael Danielsson
// Date Created: 17-01-2024
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

using SplineArchitect.Objects;

namespace SplineArchitect
{
    public class EHandleScene
    {
        public static bool sceneIsLoadedPlaymode;
        public static bool sceneIsClosing;
        public static bool editorIsQuitting;

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void AfterAssemblyReload()
        {
            //Callbacks
            //EditorSceneManager.sceneDirtied += OnSceneDirtied;
            //EditorSceneManager.sceneOpening += OnSceneOpening;
            EditorSceneManager.sceneClosing += OnSceneClosing;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneClosed += OnSceneClosed;
            EditorSceneManager.sceneSaving += OnSceneSaving;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EditorApplication.quitting += OnEditorIsQuitting;
        }

        private static void OnEditorIsQuitting()
        {
            editorIsQuitting = true;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            ECore.firstInitialization = true;
        }

        private static void OnSceneClosed(Scene scene)
        {
            HandleRegistry.ClearScene(scene.name);
            HandleCachedResources.ClearScene(scene.name);

            EHandleEvents.sceneIsClosing = false;
            sceneIsClosing = false;
        }

        private static void OnSceneClosing(Scene scene, bool removingScene)
        {
            EHandleEvents.sceneIsClosing = true;
            sceneIsClosing = true;
        }

        private static void OnSceneSaving(Scene scene, string path)
        {
            foreach (Spline spline in HandleRegistry.GetSplines())
            {
                //Spline HideFlags
                if (spline.componentMode == ComponentMode.REMOVE_FROM_BUILD)
                    spline.hideFlags = HideFlags.DontSaveInBuild;
                else
                    spline.hideFlags = HideFlags.None;

                //SplineObject HideFlags
                foreach (SplineObject so in spline.splineObjects)
                {
                    if (so == null)
                        continue;

                    //Meshes HideFlags
                    if (so.type == SplineObject.Type.FOLLOWER || spline.deformationMode == DeformationMode.SAVE_IN_SCENE)
                    {
                        foreach (MeshContainer mc in so.meshContainers) 
                            UpdateHideFlagsMeshContainer(mc, HideFlags.None);
                    }
                    else if (spline.deformationMode == DeformationMode.SAVE_IN_BUILD)
                    {
                        foreach (MeshContainer mc in so.meshContainers) 
                            UpdateHideFlagsMeshContainer(mc, HideFlags.DontSaveInEditor);
                    }
                    else
                    {
                        foreach (MeshContainer mc in so.meshContainers) 
                            UpdateHideFlagsMeshContainer(mc, HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor);
                    }

                    so.hideFlags = HideFlags.DontSaveInBuild;

                    if (so.componentMode == ComponentMode.REMOVE_FROM_BUILD)
                        continue;

                    so.hideFlags = HideFlags.None;
                }
            }

            //SplineConnectors HideFlags
            foreach (SplineConnector sc in HandleRegistry.GetSplineConnectors())
            {
                if (sc == null) continue;

                sc.hideFlags = HideFlags.DontSaveInBuild;

                foreach (Segment s in sc.connections)
                {
                    if (s.splineParent.componentMode == ComponentMode.ACTIVE)
                    {
                        sc.hideFlags = HideFlags.None;
                        break;
                    }
                }
            }
        }

        private static void UpdateHideFlagsMeshContainer(MeshContainer mc, HideFlags hideFlags)
        {
            Mesh instanceMesh = mc.GetInstanceMesh();

            if (instanceMesh == null)
                return;

            instanceMesh.hideFlags = hideFlags;
        }

        ////Playmode.

        private static void OnSceneUnloaded(Scene scene)
        {
            EHandleEvents.sceneIsLoadedPlaymode = false;
            sceneIsLoadedPlaymode = false;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            EHandleEvents.sceneIsLoadedPlaymode = true;
            sceneIsLoadedPlaymode = true;
            ECore.firstInitialization = true;
        }
    }
}
