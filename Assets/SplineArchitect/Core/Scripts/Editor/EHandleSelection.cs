// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EHandleSelection.cs
//
// Author: Mikael Danielsson
// Date Created: 06-10-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

using SplineArchitect.Objects;
using SplineArchitect.CustomTools;
using SplineArchitect.Utility;
using Object = UnityEngine.Object;
using System.Linq;

namespace SplineArchitect
{
    public static class EHandleSelection
    {
        //Selection Spline
        public static Spline selectedSpline { get; private set; }
        public static HashSet<Spline> selectedSplines { get; private set; } = new HashSet<Spline>();
        public static Spline hoveredSpline { get; private set; }
        private static Spline oldHoveredSpline;
        private static List<Object> selection = new List<Object>();

        //Selection SplineObject
        public static SplineObject selectedSplineObject;
        public static List<SplineObject> selectedSplineObjects = new List<SplineObject>();

        //Selection control point
        public static int hoveredCp { get; private set; }
        private static int oldHoveredCp;

        //General
        public static bool stopNextUpdateSelection { private get; set; } = false;
        public static bool stopUpdateSelection { private get; set; } = false;
        private static Ray mouseRay;
        private static bool assemblyReload = true;
        private static bool markForForceUpdate = false;

        public static void BeforeSceneGUIGlobal(HashSet<Spline> splines, SceneView sceneView, Event e)
        {
            PositionTool.UpdateHoveredData(e, EMouseUtility.GetMouseRay(e.mousePosition));

            if (e.type == EventType.MouseDown && e.button == 0 && !EHandleModifier.AltActive(e))
            {
                if (PositionTool.Press(e, EMouseUtility.GetMouseRay(e.mousePosition)))
                {
#if UNITY_6000_0_OR_NEWER
                    e.Use();
#endif
                }
            }

            if (GUIUtility.hotControl == 0)
            {
                bool hovering = false;

                if (TryUpdateHoveredControlPoint(e, selectedSpline, sceneView))
                    hovering = true;

                if (!hovering && TryUpdateHoveredSpline(e, splines, EHandleModifier.CtrlActive(e)))
                    hovering = true;
            }
            else
            {
                hoveredCp = 0;
                hoveredSpline = null;
            }

            if (e.type == EventType.MouseUp && e.button == 0)
            {
                TrySelectHoveredControlPoint(selectedSpline, e, EHandleModifier.CtrlActive(e));
                TrySelectHoveredSpline(e, EHandleModifier.CtrlActive(e));

                //Does not seem to be needed. OnSelectionChagne will also run.
                //ForceUpdate();
            }

            if (e.type == EventType.Layout)
            {
                if (EHandleEvents.updateSelection)
                {
                    EHandleEvents.updateSelection = false;
                    ForceUpdate();
                }
            }

            if (assemblyReload)
            {
                assemblyReload = false;
                //This is needed else Look rotation viewing vector is zero after assembly reload.
                EHandleTool.ActivatePositionToolForControlPoint(selectedSpline);
            }

            if(markForForceUpdate)
            {
                markForForceUpdate = false;
                ForceUpdate();
            }
        }

        public static void OnSelectionChange()
        {
            UpdateSelection(TryGetSelectionTransform());
        }

        public static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            if (Event.current != null && Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                if (selectionRect.Contains(Event.current.mousePosition))
                {
                    // Get the GameObject associated with the instance ID
                    GameObject go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

                    if (go == null || go.transform == null)
                        return;

                    Spline spline = go.transform.GetComponent<Spline>();

                    if (spline == null)
                        return;

                    //Dont record undo when no spline was selected. Else deformations/followers will get the wrong position after doing ctrl + z.
                    if(selectedSpline == null)
                    {
                        spline.selectedAnchors.Clear();
                        spline.selectedControlPoint = 0;
                    }
                    else
                    {
                        EHandleUndo.RecordNow(spline, "Selected spline");
                        //Also need to record the transform becouse of transform.position change
                        EHandleUndo.RecordNow(spline.transform);
                        spline.selectedAnchors.Clear();
                        spline.selectedControlPoint = 0;
                    }
                }
            }
            else if(Event.current != null && Event.current.type == EventType.DragPerform)
            {
                markForForceUpdate = true;
            }
        }

        public static void OnUndo()
        {
            Transform selection = TryGetSelectionTransform();

            if (selection == null)
                return;

            UpdateSelection(selection);
        }

        private static void UpdateSelection(Transform newSelection)
        {
            if (stopNextUpdateSelection)
            {
                stopNextUpdateSelection = false;
                return;
            }

            if (stopUpdateSelection)
                return;

            selectedSpline = null;
            selectedSplines.Clear();
            selectedSplineObject = null;
            selectedSplineObjects.Clear();

            if (newSelection == null)
                return;

            Spline spline = TryFindSpline(newSelection);
            SplineObject so = newSelection.GetComponent<SplineObject>();
            SplineConnector sc = newSelection.GetComponent<SplineConnector>();

            //Select Spline
            if (so == null)
            {
                if (spline != null && spline.IsEnabled())
                {
                    selectedSpline = spline;
                }

                foreach (Object o in Selection.objects)
                {
                    GameObject go = o as GameObject;

                    if(go == null)
                        continue;

                    Spline spline2 = go.GetComponent<Spline>();

                    if (spline2 != null && spline2 != selectedSpline)
                    {
                        if (selectedSplines.Contains(spline2))
                            continue;

                        selectedSplines.Add(spline2);
                    }
                }
            }
            //Select SplineObject
            else if (so != null && spline != null)
            {
                //Inactive SplineObjects can be created using scripts. We should not select them if the user trys to.
                if (!so.IsEnabled() || !so.splineParent.ContainsSplineObject(so))
                    return;

                selectedSpline = spline;
                selectedSplineObject = so;

                //Needs to deslect becouse if the user select an object in the hirarcy window.
                EHandleUndo.RecordNow(spline, "Selected spline object");
                spline.selectedAnchors.Clear();
                spline.selectedControlPoint = 0;

                if (spline.segments.Count > 1)
                    EHandleTool.ActivatePositionToolForSplineObject(spline, so);

                foreach (Object o in Selection.objects)
                {
                    GameObject go = o as GameObject;

                    if (go == null)
                        continue;

                    SplineObject so2 = go.GetComponent<SplineObject>();

                    if (so2 != null && so2 != selectedSplineObject)
                    {
                        selectedSplineObjects.Add(so2);

                        if(!selectedSplines.Contains(so2.splineParent) && so2.splineParent != selectedSpline)
                            selectedSplines.Add(so2.splineParent);
                    }
                }
            }

            Spline TryFindSpline(Transform transform)
            {
                Spline spline2 = transform.GetComponent<Spline>();

                for (int i = 0; i < 25; i++)
                {
                    if (spline2 != null)
                        return spline2;

                    if (transform.parent == null)
                        break;

                    transform = transform.parent;
                    spline2 = transform.GetComponent<Spline>();
                }

                return null;
            }
        }

        private static bool TryUpdateHoveredSpline(Event e, HashSet<Spline> splines, bool multiselectActive)
        {
            hoveredSpline = null;

            if (GlobalSettings.GetSplineHideMode() > 0)
                return false;

            if (EHandleSpline.controlPointCreationActive)
                return false;

            mouseRay = EMouseUtility.GetMouseRay(e.mousePosition);
            float closestDistance = 99999999;
            Vector3 mousePoint = Vector3.zero;
            Spline hovered = null;

            foreach (Spline spline in splines)
            {
                if (spline == null)
                    continue;

                if (spline.transform == null)
                    continue;

                if (!spline.IsEnabled())
                    continue;

                if (spline.segments.Count < 2)
                    continue;

                //If selected spline
                if (spline == selectedSpline)
                {
                    //Skip if ctrl is not hold down
                    if (!multiselectActive)
                        continue;

                    //Skip if so is selected
                    if (selectedSplineObject != null && selectedSplineObject.splineParent == spline)
                        continue;

                    //Skip if control point is selected
                    if (selectedSpline == spline && spline.selectedControlPoint != 0)
                        continue;
                }

                if (!spline.bounds.IntersectRay(mouseRay))
                    continue;

                mousePoint = EHandleSpline.ClosestMousePoint(spline, mouseRay, 12, out float distance, out float time, 20);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    hovered = spline;
                }
            }

            if (closestDistance < EHandleSegment.DistanceModifier(mousePoint) * 0.5f)
            {
                hoveredSpline = hovered;
            }

            if (oldHoveredSpline != hoveredSpline)
            {
                oldHoveredSpline = hoveredSpline;
                SceneView.RepaintAll();
            }

            if (hoveredSpline != null)
                return true;

            return false;
        }

        private static void TrySelectHoveredSpline(Event e, bool multiselectActive)
        {
            if (hoveredSpline == null)
                return;

            if (EHandleUi.MousePointerAboveAnyMenu(e))
                return;

            if (multiselectActive)
            {
                selection.Clear();
                selection.AddRange(Selection.objects);

                if (selectedSpline == hoveredSpline || selectedSplines.Contains(hoveredSpline))
                    selection.Remove(hoveredSpline.gameObject);
                else
                {
                    selection.Add(hoveredSpline.gameObject);
                    Selection.activeTransform = hoveredSpline.transform;
                }

                Selection.objects = selection.ToArray();
            }
            else
            {
                Selection.objects = null;
                Selection.activeTransform = hoveredSpline.transform;
            }

            e.Use();

            EHandleUndo.RecordNow(hoveredSpline, "Select Spline");
            hoveredSpline.selectedAnchors.Clear();
            hoveredSpline.selectedControlPoint = 0;

            if (selectedSpline == null)
                return;

            EHandleUndo.RecordNow(selectedSpline, "Select Spline");
            selectedSpline.selectedAnchors.Clear();
            selectedSpline.selectedControlPoint = 0;
        }

        private static bool TryUpdateHoveredControlPoint(Event e, Spline spline, SceneView sceneView)
        {
            if (spline == null)
                return false;

            hoveredCp = 0;

            Ray mouseRay = EMouseUtility.GetMouseRay(e.mousePosition);

            //Bounds for control points
            if (!spline.controlPointsBounds.IntersectRay(mouseRay))
                return false;

            Vector3 editorCameraPosition = sceneView.camera.transform.position;
            List<int> intersectingControlPoint = EHandleSpline.GetIntersectingControlPoints(spline, mouseRay);

            float distanceCheck = 999999;

            foreach(int i in intersectingControlPoint)
            {
                if (spline.selectedControlPoint == i)
                    continue;

                int segmentId = SplineUtility.ControlPointIdToSegmentIndex(i);
                Segment.ControlHandle controlHandle = SplineUtility.GetControlPointType(i);
                Segment.InterpolationType interpolationMode = spline.segments[segmentId].GetInterpolationType();

                if (interpolationMode == Segment.InterpolationType.LINE && controlHandle != Segment.ControlHandle.ANCHOR)
                    continue;

                float distanceToCamera = Vector3.Distance(spline.segments[segmentId].GetPosition(controlHandle), editorCameraPosition);

                if (distanceToCamera < distanceCheck)
                {
                    distanceCheck = distanceToCamera;
                    hoveredCp = i;
                }
            }

            if (oldHoveredCp != hoveredCp)
            {
                oldHoveredCp = hoveredCp;
                hoveredSpline = null;
                SceneView.RepaintAll();
            }

            if (hoveredCp != 0)
                return true;

            return false;
        }

        private static void TrySelectHoveredControlPoint(Spline spline, Event e, bool multiselectActive)
        {
            if (hoveredCp == 0)
                return;

            if (spline == null || !spline.IsEnabled())
                return;

            if (EHandleUi.MousePointerAboveAnyMenu(e))
                return;

            //Record new control point selection
            EHandleUndo.RecordNow(spline, "Selected/Deselect control point: " + hoveredCp);
            if (multiselectActive && SplineUtility.GetControlPointType(hoveredCp) == Segment.ControlHandle.ANCHOR &&
                            SplineUtility.GetControlPointType(spline.selectedControlPoint) == Segment.ControlHandle.ANCHOR)
            {
                if (spline.selectedAnchors.Contains(hoveredCp))
                    spline.selectedAnchors.Remove(hoveredCp);
                else if (!spline.selectedAnchors.Contains(spline.selectedControlPoint))
                {
                    spline.selectedAnchors.Add(spline.selectedControlPoint);
                    spline.selectedControlPoint = hoveredCp;
                }
            }
            else
            {
                spline.selectedControlPoint = hoveredCp;
                spline.selectedAnchors.Clear();
            }

            Selection.objects = null;
            Selection.activeTransform = spline.transform;

            selectedSplines.Clear();
            selectedSplineObjects.Clear();
            selectedSplineObject = null;

            //Inactivate the next mouseDown and mouseUp event DuringSceneGUI.
            e.Use();

            EHandleTool.ActivatePositionToolForControlPoint(spline);
        }

        public static Transform TryGetSelectionTransform()
        {
            if (Selection.activeTransform != null)
            {
                return Selection.activeTransform;
            }
            else if (Selection.activeObject != null)
            {
                GameObject gameObject = Selection.activeObject as GameObject;
                if (gameObject != null && gameObject.transform != null)
                {
                    return gameObject.transform;
                }
            }

            return null;
        }

        public static void MarkForForceUpdate()
        {
            markForForceUpdate = true;
        }

        public static void ForceUpdate()
        {
            UpdateSelection(TryGetSelectionTransform());
        }

        public static void SelectPrimaryControlPoint(Spline spline, int controlpointId)
        {
            spline.selectedControlPoint = controlpointId;
            EHandleTool.ActivatePositionToolForControlPoint(spline);
        }

        public static void SelectSecondaryAnchors(Spline spline, int[] anchors)
        {
            spline.selectedAnchors.Clear();
            foreach (int a in anchors)
            {
                spline.selectedAnchors.Add(a);
            }
        }

        public static bool IsPrimiarySelection(Spline spline)
        {
            if (spline == selectedSpline)
                return true;

            return false;
        }

        public static bool IsSecondarySelection(Spline spline)
        {
            if (selectedSplines.Contains(spline))
                return true;

            return false;
        }

        public static void UpdatedSelectedSplinesRecordUndo(Action<Spline> action, string recordName, EHandleUndo.RecordType recordType = EHandleUndo.RecordType.RECORD_OBJECT)
        {
            EHandleUndo.RecordNow(selectedSpline, recordName, recordType);
            action.Invoke(selectedSpline);

            foreach (Spline spline2 in selectedSplines)
            {
                EHandleUndo.RecordNow(spline2, recordName, recordType);
                action.Invoke(spline2);
            }
        }

        public static void UpdatedSelectedSplines(Action<Spline> action)
        {
            action.Invoke(selectedSpline);
            foreach (Spline spline2 in selectedSplines) action.Invoke(spline2);
        }

        public static void UpdatedSelectedSplineObjectsRecordUndo(Action<SplineObject> action, string recordName, bool recordTransform = false)
        {
            Object objectToRecord = selectedSplineObject;

            if (recordTransform)
                objectToRecord = selectedSplineObject.transform;

            EHandleUndo.RecordNow(objectToRecord, recordName);
            action.Invoke(selectedSplineObject);

            foreach (SplineObject so2 in selectedSplineObjects)
            {
                Object objectToRecord2 = so2;

                if (recordTransform)
                    objectToRecord2 = so2.transform;

                EHandleUndo.RecordNow(objectToRecord2, recordName);
                action.Invoke(so2);
            }
        }

        public static void UpdatedSelectedSplineObjects(Action<SplineObject> action)
        {
            if (selectedSplineObject == null)
                return;

            action.Invoke(selectedSplineObject);

            foreach (SplineObject so2 in selectedSplineObjects)
            {
                action.Invoke(so2);
            }
        }

        public static void UpdateSelectedAnchors(Spline spline, Action<Segment> action)
        {
            action.Invoke(spline.segments[SplineUtility.ControlPointIdToSegmentIndex(spline.selectedControlPoint)]);
            foreach (int i in spline.selectedAnchors)
                action.Invoke(spline.segments[SplineUtility.ControlPointIdToSegmentIndex(i)]);
        }

        public static void UpdateSelectedAnchorsRecordUndo(Spline spline, Action<Segment> action, string recordName)
        {
            EHandleUndo.RecordNow(spline, recordName);
            action.Invoke(spline.segments[SplineUtility.ControlPointIdToSegmentIndex(spline.selectedControlPoint)]);
            foreach (int i in spline.selectedAnchors)
                action.Invoke(spline.segments[SplineUtility.ControlPointIdToSegmentIndex(i)]);
        }
    }
}