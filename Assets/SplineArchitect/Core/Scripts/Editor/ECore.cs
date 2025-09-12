// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: ECore.cs
//
// Author: Mikael Danielsson
// Date Created: 28-01-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEditor.SceneManagement;

using SplineArchitect.Objects;
using SplineArchitect.PostProcessing;
using SplineArchitect.Utility;
using SplineArchitect.Libraries;

namespace SplineArchitect
{
    public static partial class ECore
    {
        public static bool firstInitialization = true;
        private static PlayModeStateChange playModeStateChange;
        private static EditorWindow lastFocusedEditorWindow;

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void AfterAssemblyReload()
        {
            AssemblyReloadEvents.beforeAssemblyReload += BeforeAssemblyReload;

            EditorApplication.update += Update;
            EditorApplication.hierarchyChanged += OnHierarchyChange;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.wantsToQuit += OnEditorWantsToQuit;

            SceneView.duringSceneGui += OnSceneGUI;
            SceneView.beforeSceneGui += BeforeSceneGUI;

            Undo.undoRedoPerformed += OnUndo;

            Selection.selectionChanged += OnSelectionChange;

            Tools.pivotRotationChanged += OnPivotRotationChanged;

            ShortcutManager.instance.shortcutBindingChanged += OnShortcutBindingChanged;

            PrefabStage.prefabStageOpened += OnPrefabStageOpened;
            PrefabStage.prefabStageClosing += OnPrefabStageClosing;
            PrefabUtility.prefabInstanceReverted += OnPrefabChange;
            PrefabUtility.prefabInstanceUpdated += OnPrefabChange;

            EHandleEvents.OnDisposeDeformJob += OnDisposeDeformJob;
        }

        private static void OnPrefabChange(GameObject instance)
        {
            EHandlePrefab.OnPrefabChange(instance);
        }

        private static void OnPrefabStageOpened(PrefabStage prefabStage)
        {
            EHandlePrefab.OnPrefabStageOpened(prefabStage);
        }

        private static void OnPrefabStageClosing(PrefabStage prefabStage)
        {
            EHandlePrefab.OnPrefabStageClosing(prefabStage);
        }

        private static void OnShortcutBindingChanged(ShortcutBindingChangedEventArgs args)
        {
            EHandleUi.OnShortcutBindingChanged();
        }

        private static void OnWindowFocusChanged()
        {
            EHandleUi.OnWindowFocusChanged();
        }

        private static void OnDisposeDeformJob()
        {
            EHandleUi.OnDisposeDeformJob();
        }

        private static bool OnEditorWantsToQuit()
        {
            EHandleUi.OnEditorWantsToQuit();
            return true;
        }

        private static void BeforeAssemblyReload()
        {
            EHandleUi.BeforeAssemblyReload();
            HandleRegistry.DisposeNativeDataOnSplines();
        }

        private static void OnPivotRotationChanged()
        {
            EHandleTool.OnPivotRotationChanged();
        }

        private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            EHandleSelection.OnHierarchyGUI(instanceID, selectionRect);
        }

        private static void OnHierarchyChange()
        {

        }

        private static void OnSelectionChange()
        {
            EHandleSelection.OnSelectionChange();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            playModeStateChange = state;
            EHandleEvents.playModeStateChange = state;

            if (state == PlayModeStateChange.ExitingEditMode)
            {
                foreach (Spline spline in HandleRegistry.GetSplines())
                {
                    spline.DisposeCachedData();
                }
            }

            HandleDeformationWorker.Clear();
        }

        private static void BeforeSceneGUI(SceneView sceneView)
        {
            if (!EHandleSceneView.IsValid(sceneView))
                return;

            if (playModeStateChange == PlayModeStateChange.ExitingPlayMode || playModeStateChange == PlayModeStateChange.ExitingEditMode)
                return;

            //Needs to be second
            EHandleTool.BeforeSceneGUIGlobal(sceneView, Event.current);
            EHandleSpline.BeforeSceneGUIGlobal(sceneView, Event.current);
            EHandleSceneView.BeforeSceneGUIGlobal(sceneView, Event.current);
            EHandleSelection.BeforeSceneGUIGlobal(sceneView, Event.current);
            EHandleEvents.InitAfterDrag(sceneView);
            EActionToSceneGUI.BeforeOnSceneGUI(Event.current);
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (!EHandleSceneView.IsValid(sceneView))
                return;

            if (playModeStateChange == PlayModeStateChange.ExitingPlayMode || playModeStateChange == PlayModeStateChange.ExitingEditMode)
                return;

            //Needs to be first
            EActionToSceneGUI.EArlyOnSceneGUI(Event.current);

            foreach (Spline spline in HandleRegistry.GetSplines())
            {
                if (spline == null)
                {
                    EHandleUndo.MarkSplineForDestroy(spline);
                    continue;
                }

                if (spline.IsEnabled() == false) continue;

                if (!EHandleSelection.IsPrimiarySelection(spline)) continue;

                EHandleSpline.OnSceneGUI(spline, Event.current);
                EHandleTool.OnSceneGUI(spline, Event.current, sceneView);
                EHandleSplineObject.OnSceneGUI(spline, Event.current);
            }

            EHandleUi.OnSceneGUIGlobal(sceneView);
            EHandleUndo.DestroyMarkedSplines();
            EHandleDrawing.OnSceneGUIGlobal(HandleRegistry.GetSplines(), Event.current);

            //Needs to be last
            EActionToSceneGUI.LateOnSceneGUI(Event.current);
        }

        private static void Update()
        {
            if (playModeStateChange == PlayModeStateChange.ExitingPlayMode || playModeStateChange == PlayModeStateChange.ExitingEditMode)
              return;

            if (firstInitialization) EHandleEvents.InvokeFirstUpdate();

            if(lastFocusedEditorWindow != EditorWindow.focusedWindow)
            {
                lastFocusedEditorWindow = EditorWindow.focusedWindow;
                OnWindowFocusChanged();
            }

            EHandleEvents.InvokeUpdateEarly();
            EActionToUpdate.EarlyUpdate();

            foreach (Spline spline in HandleRegistry.GetSplines())
            {
                if (spline == null) continue;

                EHandleSpline.InitalizeEditor(spline, firstInitialization);

                if (spline.IsEnabled() == false) continue;

                EHandleSpline.UpdateCachedData(spline, false);
                EActionToUpdate.Add(() => { spline.UpdateMonitor(); }, EActionToUpdate.Type.LATE);

                if (spline.segments.Count < 2) continue;

                EHandleSegment.HandleLinking(spline);

                bool isPrimiarySelection = EHandleSelection.IsPrimiarySelection(spline);
                bool IsSecondarySelection = EHandleSelection.IsSecondarySelection(spline);

                if (!isPrimiarySelection && !IsSecondarySelection) continue;

                EHandleSpline.UpdateLinksOnTransformMovement(spline);
                EHandleDeformation.TryDeform(spline);

                if (!isPrimiarySelection) continue;

                EHandleSplineObject.Update(spline);
                EHandleTool.Update(spline);
            }

            EHandlePrefab.UpdateGlobal();
            EHandleSplineConnector.UpdateGlobal();
            EHandleMeshContainer.RefreshAfterAssetModification(HandleRegistry.GetSplines());
            EHandleDeformation.RunWorkers();
            EHandleDeformation.RunOrthoNormalsWorkers();
            BuildProcessReport.UpdateGlobal();
            EHandleSpline.ProcessMarkedForInfoUpdates();
            EHandleUi.UpdateGlobal();
            HandleRegistry.UpdateGlobal();

            //Needs to be second last
            EActionDelayed.UpdateGlobal();
            //Needs to be last
            EActionToUpdate.LateUpdate();

            firstInitialization = false;
        }

        private static void OnUndo()
        {
            EHandleSelection.OnUndo();

            foreach (Spline spline in HandleRegistry.GetSplines())
            {
                if (spline == null) continue;

                spline.monitor.UpdateChildCount();

                foreach (Segment s in spline.segments)
                    s.splineParent = spline;

                if (spline.IsEnabled() == false) continue;
                if (spline.segments.Count < 2) continue;

                bool isPrimiarySelection = EHandleSelection.IsPrimiarySelection(spline);
                bool IsSecondarySelection = EHandleSelection.IsSecondarySelection(spline);

                if (!IsSecondarySelection && !isPrimiarySelection) continue;

                EHandleEvents.InvokeUndoSelection(spline);
            }

            EHandleTool.OnUndoGlobal();

            //Needs to be last
            EHandleUndo.UpdateUndoTriggerTime();
        }

        public static PlayModeStateChange GetLastPlayMode()
        {
            return playModeStateChange;
        }
    }
}
