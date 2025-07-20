// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EHandleTool.cs
//
// Author: Mikael Danielsson
// Date Created: 17-04-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEditor;

using SplineArchitect.Objects;
using SplineArchitect.Utility;
using SplineArchitect.CustomTools;

namespace SplineArchitect
{
    public static class EHandleTool
    {
        public static int rotationHandleID = -99999;
        public static int scaleHandleID = -99999;
        private static Vector3[] normalsContainer = new Vector3[3];

        public static void BeforeSceneGUIGlobal(Event e, SceneView sceneView)
        {
            if (e.type == EventType.MouseDown)
            {

            }

            //Update PositionTool orientation.
            if (e.type == EventType.MouseUp)
            {
                Spline spline = EHandleSelection.selectedSpline;
                if (spline != null && spline.segments.Count > 1)
                {
                    UpdateOrientationForPositionTool(sceneView, spline);
                    EHandleSelection.UpdatedSelectedSplineObjects((selected) =>
                    {
                        selected.activationPosition = selected.localSplinePosition;
                    });
                }
            }

            //Needs to be last
            if (e.type == EventType.Repaint)
            {
                //Needs to reset rotationHandleID and scaleHandleID. Else the selection system can belive that they are selected when they are not, and you cant selected splines becouse of that.
                rotationHandleID = -99999;
                scaleHandleID = -99999;
                HideOrUnhideTools();
            }
        }

        public static void OnSceneGUI(Spline spline, Event e, SceneView sceneView)
        {
            SplineObject so = EHandleSelection.selectedSplineObject;

            if (so != null && so.transform != null && so.type != SplineObject.Type.NONE)
            {
                if (Tools.current == Tool.Move) SplineObjectMoveTool(spline, so);
                else if (Tools.current == Tool.Rotate) SplineObjectRotateTool(spline, so);
                else if (Tools.current == Tool.Scale) SplineObjectScaleTool(spline, so);

                return;
            }

            int segmentIndex = SplineUtility.ControlPointIdToSegmentIndex(spline.selectedControlPoint);

            if (segmentIndex < spline.segments.Count && segmentIndex >= 0)
            {
                Segment segment = spline.segments[segmentIndex];
                if (Tools.current == Tool.Move) ControlPointMoveTool(spline, segment);
            }
        }

        public static void Update(Spline spline)
        {
            if (spline.monitor.PosRotChange())
                UpdateOrientationForPositionTool(EHandleSceneView.GetSceneView(), spline);
        }

        public static void OnUndoGlobal()
        {
            ActivatePositionToolForControlPoint(EHandleSelection.selectedSpline);

            //Need to run last, else it will update before positions has changed and will get the wrong position.
            EActionToUpdate.Add(() => {
                UpdateOrientationForPositionTool(EHandleSceneView.GetSceneView(), EHandleSelection.selectedSpline);
            }, EActionToUpdate.Type.LATE);

            EHandleSelection.UpdatedSelectedSplineObjects((selected) =>
            {
                selected.activationPosition = selected.localSplinePosition;
            });
        }

        public static void OnPivotRotationChanged()
        {
            UpdateOrientationForPositionTool(EHandleSceneView.GetSceneView(), EHandleSelection.selectedSpline);
        }

        private static void HideOrUnhideTools()
        {
            Spline spline = EHandleSelection.selectedSpline;
            SplineObject so = EHandleSelection.selectedSplineObject;
            Tools.hidden = false;
            PositionTool.Deactivate();

            if (spline == null)
                return;

            if (so != null)
            {
                //None types done use any custom tool
                if (so.type == SplineObject.Type.NONE)
                    return;

                if (spline.segments.Count < 2)
                    return;
            }
            else 
            {
                if (spline.selectedControlPoint <= 0)
                    return;
            }

            Tools.hidden = true;

            if (Tools.current == Tool.Move)
                PositionTool.Activate();
        }

        private static void SplineObjectMoveTool(Spline spline, SplineObject so)
        {
            if (!PositionTool.DragUsingDif(out Vector3 dif))
                return;

            Vector3 combinedParentScale = SplineObjectUtility.GetCombinedParentScales(so);

            EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
            {
                Vector3 scaledDif = new Vector3(dif.x / combinedParentScale.x, dif.y / combinedParentScale.y, dif.z / combinedParentScale.z);
                scaledDif = Vector3.Scale(so.transform.localScale, scaledDif);
                Vector3 newPosition = selected.activationPosition + scaledDif;
                selected.localSplinePosition.x = Mathf.Round(newPosition.x * 100) / 100;
                selected.localSplinePosition.y = Mathf.Round(newPosition.y * 100) / 100;
                selected.localSplinePosition.z = Mathf.Round(newPosition.z * 100) / 100;

                if (GlobalSettings.GetSnapIncrement() > 0)
                    selected.localSplinePosition = GeneralUtility.RoundToClosest(newPosition, GlobalSettings.GetSnapIncrement());

            }, "Moved object");
        }

        private static void SplineObjectRotateTool(Spline spline, SplineObject so)
        {
            //Draw on top
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

            //Normals
            float time = so.splinePosition.z / spline.length;
            float fixedTime = spline.TimeToFixedTime(time);
            spline.GetNormalsNonAlloc(normalsContainer, fixedTime);

            //Rotations
            Quaternion parentRotations = SplineObjectUtility.GetCombinedParentRotations(so.soParent);
            Quaternion localSplineRotation = Quaternion.LookRotation(normalsContainer[2], normalsContainer[1]);
            Quaternion combinedRotations = localSplineRotation * parentRotations;
            Quaternion handleRotation = combinedRotations * so.localSplineRotation;

            //Pivot
            Vector3 pivot = spline.SplinePositionToWorldPosition(so.transform.parent, so.localSplinePosition, SplineObjectUtility.GetCombinedParentMatrixs(so.soParent));

            //Tool handle
            rotationHandleID = GUIUtility.GetControlID("RotationHandle".GetHashCode(), FocusType.Passive) + 1;
            Quaternion newRotation = Handles.RotationHandle(handleRotation, pivot);
            newRotation = Quaternion.Inverse(combinedRotations) * newRotation;

            if (GUI.changed)
            {
                Vector3 dif = newRotation.eulerAngles - so.localSplineRotation.eulerAngles;
                EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                {
                    selected.localSplineRotation = Quaternion.Euler(selected.localSplineRotation.eulerAngles + dif);
                }, "Rotated object");
            }
        }

        private static void SplineObjectScaleTool(Spline spline, SplineObject so)
        {
            //Draw on top
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

            //Normals
            float time = so.splinePosition.z / spline.length;
            float fixedTime = spline.TimeToFixedTime(time);
            spline.GetNormalsNonAlloc(normalsContainer, fixedTime);

            //Handle rotation
            Quaternion parentRotations = SplineObjectUtility.GetCombinedParentRotations(so.soParent);
            Quaternion localSplineRotation = Quaternion.LookRotation(normalsContainer[2], normalsContainer[1]);
            Quaternion handleRotation = localSplineRotation * so.splineRotation;

            //Pivot
            Vector3 pivot = spline.SplinePositionToWorldPosition(so.transform.parent, so.localSplinePosition, SplineObjectUtility.GetCombinedParentMatrixs(so.soParent));

            //Tool handle
            scaleHandleID = GUIUtility.GetControlID("ScaleHandle".GetHashCode(), FocusType.Passive) + 1;
            Vector3 newScale = Handles.ScaleHandle(so.transform.localScale, pivot, handleRotation, HandleUtility.GetHandleSize(pivot));

            if (GUI.changed)
            {
                Vector3 dif = newScale - so.transform.localScale;
                EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                {
                    selected.transform.localScale += dif;
                }, "Scaled object", true);
            }
        }

        public static void ControlPointMoveTool(Spline spline, Segment segment)
        {
            Segment.ControlHandle type = SplineUtility.GetControlPointType(spline.selectedControlPoint);
            Vector3 newControlPointPos;
            bool toolActive;

            if (PositionTool.activePart == PositionTool.Part.SURFACE)
                toolActive = PositionTool.DragUsingSurface(out newControlPointPos);
            else
                toolActive = PositionTool.DragUsingPos(out newControlPointPos);

            if (toolActive)
            {
                if (GlobalSettings.GetGridVisibility())
                {
                    newControlPointPos = EHandleGrid.SnapPoint(spline, newControlPointPos);
                    PositionTool.ActivationType activationType = PositionTool.ActivationType.ANCHOR;
                    if (type == Segment.ControlHandle.TANGENT_A || type == Segment.ControlHandle.TANGENT_B) activationType = PositionTool.ActivationType.TANGENT;
                    PositionTool.ActivateAndSetPosition(activationType, newControlPointPos);
                }

                EHandleUndo.RecordNow(spline, "Move control point: " + spline.selectedControlPoint);
                EHandleSegment.SegmentMovement(spline, segment, type, newControlPointPos);
                if (type == Segment.ControlHandle.ANCHOR)
                    EHandleSegment.LinkMovement(segment);
            }
        }

        public static void UpdateOrientationForPositionTool()
        {
            UpdateOrientationForPositionTool(EHandleSceneView.GetSceneView(), EHandleSelection.selectedSpline);
        }

        public static void UpdateOrientationForPositionTool(SceneView sceneView, Spline spline)
        {
            if (spline == null)
                return;

            SplineObject selectedSo = EHandleSelection.selectedSplineObject;

            if (spline.selectedControlPoint > 0)
            {
                int segementId = SplineUtility.ControlPointIdToSegmentIndex(spline.selectedControlPoint);

                if (segementId >= spline.segments.Count)
                    segementId = spline.segments.Count - 1;

                Vector3 forwardDirection = GetForwardDirectionForControlPoint(spline, spline.segments[segementId].GetPosition(Segment.ControlHandle.ANCHOR), spline.segments[segementId].GetPosition(Segment.ControlHandle.TANGENT_B));
                Vector3 upDirection = GetUpDirectionForControlPoint(spline, forwardDirection, out bool switchXYDirections);
                PositionTool.UpdateOrientation(sceneView.camera.transform.position, forwardDirection, upDirection, switchXYDirections);
            }
            else if (selectedSo != null)
                ActivatePositionToolForSplineObject(spline, selectedSo);
        }

        public static void ActivatePositionToolForSplineObject(Spline spline, SplineObject so)
        {
            if (so == null)
                return;

            if (so.transform.parent == null)
                return;

            SceneView sceneView = EHandleSceneView.GetSceneView();

            float time = so.splinePosition.z / spline.length;
            float fixedTime = spline.TimeToFixedTime(time);
            spline.GetNormalsNonAlloc(normalsContainer, fixedTime);
            Quaternion splineRotation = Quaternion.LookRotation(normalsContainer[2], normalsContainer[1]);
            Quaternion combinedRotation = splineRotation * SplineObjectUtility.GetCombinedParentRotations(so.soParent);

            normalsContainer[0] = combinedRotation * Vector3.right;
            normalsContainer[1] = combinedRotation * Vector3.up;
            normalsContainer[2] = combinedRotation * Vector3.forward;

            Vector3 position = spline.SplinePositionToWorldPosition(so.transform.parent, so.localSplinePosition, SplineObjectUtility.GetCombinedParentMatrixs(so.soParent));
            PositionTool.ActivateAndSetPosition(PositionTool.ActivationType.SPLINE_OBJECT, position, sceneView.camera.transform.position, normalsContainer[2], normalsContainer[1], false);
            so.activationPosition = so.localSplinePosition;
        }

        public static void ActivatePositionToolForControlPoint(Spline spline)
        {
            if (spline == null)
                return;

            if (spline.selectedControlPoint <= 0)
                return;

            if (EHandleSelection.selectedSplineObject != null)
                return;

            //Data for MoveTool
            int segementId = SplineUtility.ControlPointIdToSegmentIndex(spline.selectedControlPoint);

            if (segementId >= spline.segments.Count)
                segementId = spline.segments.Count - 1;

            Segment segment = spline.segments[segementId];

            //Updated position tool rotation if InterpolationType line
            if (segment.GetInterpolationType() == Segment.InterpolationType.LINE)
                segment.SetInterpolationMode(Segment.InterpolationType.LINE);
            else
            {
                //Check if tangent has same position as anchor
                if (GeneralUtility.IsEqual(segment.GetPosition(Segment.ControlHandle.TANGENT_A), segment.GetPosition(Segment.ControlHandle.ANCHOR)) ||
                    GeneralUtility.IsEqual(segment.GetPosition(Segment.ControlHandle.TANGENT_B), segment.GetPosition(Segment.ControlHandle.ANCHOR)))
                {
                    EHandleUndo.RecordNow(spline, "Set interpolation mode to line");
                    segment.SetInterpolationMode(Segment.InterpolationType.LINE);
                    spline.selectedControlPoint = SplineUtility.SegmentIndexToControlPointId(segementId, Segment.ControlHandle.ANCHOR);
                }
            }

            Segment.ControlHandle segementType = SplineUtility.GetControlPointType(spline.selectedControlPoint);
            Vector3 position = segment.GetPosition(segementType);

            Vector3 forwardDirection = GetForwardDirectionForControlPoint(spline, segment.GetPosition(Segment.ControlHandle.ANCHOR), segment.GetPosition(Segment.ControlHandle.TANGENT_B));
            Vector3 upDirection = GetUpDirectionForControlPoint(spline, forwardDirection, out bool switchXYDirections);
            PositionTool.ActivationType activationType = PositionTool.ActivationType.ANCHOR;
            Segment.ControlHandle type = SplineUtility.GetControlPointType(spline.selectedControlPoint);
            if (type == Segment.ControlHandle.TANGENT_A || type == Segment.ControlHandle.TANGENT_B) activationType = PositionTool.ActivationType.TANGENT;
            PositionTool.ActivateAndSetPosition(activationType,
                                                position,
                                                EHandleSceneView.GetSceneView().camera.transform.position,
                                                forwardDirection,
                                                upDirection,
                                                true,
                                                switchXYDirections,
                                                segment.linkTarget == Segment.LinkTarget.SPLINE_CONNECTOR);
        }

        private static Vector3 GetForwardDirectionForControlPoint(Spline spline, Vector3 anchorPoint, Vector3 tangentPoint)
        {
            Vector3 forwardDirection = (anchorPoint - tangentPoint).normalized;
            if (GeneralUtility.IsZero(forwardDirection))return spline.transform.forward;
            return forwardDirection;
        }

        private static Vector3 GetUpDirectionForControlPoint(Spline spline, Vector3 forwardDirection, out bool switchXYDirection)
        {
            float dif = Mathf.Abs(Vector3.Angle(forwardDirection, spline.transform.up) - 90);
            float degrees = dif;
            Vector3 upDirection = spline.transform.up;
            switchXYDirection = false;

            dif = Mathf.Abs(Vector3.Angle(forwardDirection, spline.transform.right) - 90);
            if (dif < degrees)
            {
                degrees = dif;
                upDirection = spline.transform.right;
                switchXYDirection = true;
            }

            dif = Mathf.Abs(Vector3.Angle(forwardDirection, spline.transform.forward) - 90);
            if (dif < degrees)
            {
                upDirection = spline.transform.forward;
                switchXYDirection = true;
            }

            return upDirection;
        }
    }
}