// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EHandleShortcuts.cs
//
// Author: Mikael Danielsson
// Date Created: 23-03-2024
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEditor.ShortcutManagement;
using UnityEditor;

using SplineArchitect.Objects;
using SplineArchitect.Utility;
using SplineArchitect.Ui;

namespace SplineArchitect
{
    public class EHandleShortcuts
    {
        //Ids
        public const string hideUiId = "Spline Architect/Hide ui";
        public const string toggleGridVisibilityId = "Spline Architect/Toggle grid visibility";
        public const string toggleNormalsId = "Spline Architect/Toggle normals";

        [Shortcut(hideUiId, KeyCode.H, ShortcutModifiers.Alt)]
        public static void HideUi()
        {
            SceneView sceneView = EHandleSceneView.GetCurrent();

            foreach (ToolbarToggleBase ttb in ToolbarToggleBase.instances)
            {
                ToolbarToggleControlPanel ttcp = ttb as ToolbarToggleControlPanel;

                if (ttcp == null)
                    continue;

                if (ttcp.currentSceneView == null)
                    continue;

                if(ttcp.currentSceneView == sceneView)
                    ttcp.ToggleWindow();
            }
        }

        [Shortcut(toggleGridVisibilityId)]
        public static void ToggleGridVisibility()
        {
            GlobalSettings.SetGridVisibility(!GlobalSettings.GetGridVisibility());
        }

        [Shortcut(toggleNormalsId)]
        public static void ToggleNormals()
        {
            GlobalSettings.SetShowNormals(!GlobalSettings.GetShowNormals());
        }

        [Shortcut("Spline Architect/Toggle splines")]
        public static void ToggleSplines()
        {
            int value = (int)GlobalSettings.GetSplineHideMode() + 1;
            if (value > 2) value = 0;

            GlobalSettings.SetSplineHideMode((SplineHideMode)value);
        }

        [Shortcut("Spline Architect/Toggle creation mode")]
        public static void ToggleCreationMode()
        {
            EHandleSpline.controlPointCreationActive = !EHandleSpline.controlPointCreationActive;
        }

        [Shortcut("Spline Architect/Select all anchors")]
        public static void SelectAllAnchors()
        {
            Spline spline = EHandleSelection.selectedSpline;
            if (spline == null)
                return;

            EHandleUndo.RecordNow(spline, "Selected all anchors");
            int totalAnchors = spline.segments.Count;
            int[] anchors = new int[totalAnchors - 1];

            for (int i = 0; i < totalAnchors - 1; i++)
                anchors[i] = i * 3 + 1003;

            EHandleSelection.SelectSecondaryAnchors(spline, anchors);
            EHandleSelection.SelectPrimaryControlPoint(spline, 1000);

            EActionToSceneGUI.Add(() => {
                EHandleTool.UpdateOrientationForPositionTool(EHandleSceneView.GetCurrent(), spline);
            }, EActionToSceneGUI.Type.LATE, EventType.Layout);
        }

        [Shortcut("Spline Architect/Next control point", KeyCode.Period, ShortcutModifiers.Alt | ShortcutModifiers.Shift)]
        public static void NextControlPoint()
        {
            Spline spline = EHandleSelection.selectedSpline;
            if (spline == null)
                return;

            if (EHandleSelection.selectedSplineObject != null)
                return;

            EHandleUndo.RecordNow(spline, "Next control point");
            EHandleSelection.SelectPrimaryControlPoint(spline, EHandleSpline.GetNextControlPoint(spline));

            EActionToSceneGUI.Add(() => {
                EHandleTool.UpdateOrientationForPositionTool(EHandleSceneView.GetCurrent(), spline);
            }, EActionToSceneGUI.Type.LATE, EventType.Layout);
        }
         
        [Shortcut("Spline Architect/Prev control point", KeyCode.Comma, ShortcutModifiers.Alt | ShortcutModifiers.Shift)]
        public static void PrevControlPoint()
        {
            Spline spline = EHandleSelection.selectedSpline;
            if (spline == null)
                return;

            if (EHandleSelection.selectedSplineObject != null)
                return;

            EHandleUndo.RecordNow(spline, "Prev control point");
            EHandleSelection.SelectPrimaryControlPoint(spline, EHandleSpline.GetNextControlPoint(spline, true));

            EActionToSceneGUI.Add(() => {
                EHandleTool.UpdateOrientationForPositionTool(EHandleSceneView.GetCurrent(), spline);
            }, EActionToSceneGUI.Type.LATE, EventType.Layout);
        }

        [Shortcut("Spline Architect/Flatten control points")]
        public static void FlattenControlPoints()
        {
            Spline spline = EHandleSelection.selectedSpline;
            if (spline == null)
                return;

            if(spline.selectedControlPoint != 0)
            {
                EHandleSelection.UpdateSelectedAnchorsRecordUndo(spline, (selected) =>
                {
                    EHandleSpline.FlattenControlPoints(spline, selected);
                }, "Flatten control points");

                EActionToSceneGUI.Add(() => {
                    EHandleTool.UpdateOrientationForPositionTool(EHandleSceneView.GetCurrent(), spline);
                }, EActionToSceneGUI.Type.LATE, EventType.Layout);
            }
            else
            {
                EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                {
                    EHandleSpline.FlattenControlPoints(selected);
                }, "Flatten control points");

                EActionToSceneGUI.Add(() => {
                    EHandleTool.UpdateOrientationForPositionTool(EHandleSceneView.GetCurrent(), spline);
                }, EActionToSceneGUI.Type.LATE, EventType.Layout);
            }
        }
    }
}
