// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: ECore.cs
//
// Author: Mikael Danielsson
// Date Created: 28-01-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEditor;

using SplineArchitect.Utility;
using SplineArchitect.PostProcessing;
using SplineArchitect.Objects;

namespace SplineArchitect
{
    public static partial class ECore
    {
        public static bool firstInitialization = true;
        private static PlayModeStateChange playModeStateChange;

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void AfterAssemblyReload()
        {
            //Callbacks
            AssemblyReloadEvents.beforeAssemblyReload += BeforeAssemblyReload;
            EditorApplication.update += Update;
            EditorApplication.hierarchyChanged += OnHierarchyChange;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            SceneView.duringSceneGui += OnSceneGUI;
            SceneView.beforeSceneGui += BeforeSceneGUI;
            Undo.undoRedoPerformed += OnUndo;
            Selection.selectionChanged += OnSelectionChange;
            Tools.pivotRotationChanged += OnPivotRotationChanged;
        }

        public static PlayModeStateChange GetLastPlayMode()
        {
            return playModeStateChange;
        }

        private static void BeforeAssemblyReload()
        {
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
            EHandleSplineObject.OnHierarchyChange();
        }

        private static void OnSelectionChange()
        {
            EHandleUi.OnSelectionChange();
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
            EHandleSceneView.TryUpdate(sceneView);

            if (!EHandleSceneView.IsValid(sceneView))
                return;

            if (playModeStateChange == PlayModeStateChange.ExitingPlayMode || playModeStateChange == PlayModeStateChange.ExitingEditMode)
                return;

            //Needs to be first
            EHandleUi.BeforeSceneGUIGlobal(Event.current);
            //Needs to be second
            EHandleTool.BeforeSceneGUIGlobal(Event.current, sceneView);
            EHandleSpline.BeforeSceneGUIGlobal(Event.current);
            EHandleSceneView.BeforeSceneGUIGlobal(Event.current);
            EHandleSelection.BeforeSceneGUIGlobal(HandleRegistry.GetSplines(), sceneView, Event.current);
            EHandleEvents.InitAfterDrag(sceneView);
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (!EHandleSceneView.IsValid(sceneView))
                return;

            if (playModeStateChange == PlayModeStateChange.ExitingPlayMode || playModeStateChange == PlayModeStateChange.ExitingEditMode)
                return;

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

            EHandleUndo.DestroyMarkedSplines();
            EHandleSpline.OnSceneGUIGlobal(Event.current);
            EHandleUi.OnSceneGUIGlobal(sceneView);
            EHandleDrawing.OnSceneGUIGlobal(HandleRegistry.GetSplines(), Event.current);

            //Needs to be last
            EActionToLateSceneGUI.LateOnSceneGUI(Event.current);
        }

        private static void Update()
        {
            if (playModeStateChange == PlayModeStateChange.ExitingPlayMode || playModeStateChange == PlayModeStateChange.ExitingEditMode)
              return;

            if (firstInitialization) EHandleEvents.InvokeFirstUpdate();

            EHandleEvents.InvokeUpdateEarly();
            EActionToUpdate.EarlyUpdate();

            foreach (Spline spline in HandleRegistry.GetSplines())
            {
                EHandleSpline.InitalizeEditor(spline, firstInitialization);

                if (spline == null) continue;
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

            EHandleSplineConnector.Update();
            EHandleMeshContainer.RefreshAfterAssetModification(HandleRegistry.GetSplines());
            EHandleDeformation.RunWorkers();
            EHandleDeformation.RunOrthoNormalsWorkers();
            BuildProcessReport.Update();
            EHandleSpline.ProcessMarkedForInfoUpdates();
            //Needs to be second last
            EActionDelayed.Update();
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
    }
}
