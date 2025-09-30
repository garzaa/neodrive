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

        public static void BeforeSceneGUIGlobal(SceneView sceneView, Event e)
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
                UpdateOrientationForPositionTool(EHandleSceneView.GetCurrent(), spline);
        }

        public static void OnUndoGlobal()
        {
            ActivatePositionToolForControlPoint(EHandleSelection.selectedSpline);

            //Need to run last, else it will update before positions has changed and will get the wrong position.
            EActionToUpdate.Add(() => {
                UpdateOrientationForPositionTool(EHandleSceneView.GetCurrent(), EHandleSelection.selectedSpline);
            }, EActionToUpdate.Type.LATE);

            EHandleSelection.UpdatedSelectedSplineObjects((selected) =>
            {
                selected.activationPosition = selected.localSplinePosition;
            });
        }

        public static void OnPivotRotationChanged()
        {
            UpdateOrientationForPositionTool(EHandleSceneView.GetCurrent(), EHandleSelection.selectedSpline);
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
                Vector3 newPosition;
                newPosition = selected.activationPosition + scaledDif;
                selected.localSplinePosition.x = Mathf.Round(newPosition.x * 100) / 100;
                selected.localSplinePosition.y = Mathf.Round(newPosition.y * 100) / 100;
                selected.localSplinePosition.z = Mathf.Round(newPosition.z * 100) / 100;

                selected.localSplinePosition = EHandleSplineObject.SnapPosition(selected.localSplinePosition);

            }, "Moved object");
        }

        private static void SplineObjectRotateTool(Spline spline, SplineObject so)
        {
            //Draw on top
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

            //Normals
            float time = so.splinePosition.z / spline.length;
            if (so.alignToEnd) time = (spline.length - so.splinePosition.z) / spline.length;
            float fixedTime = spline.TimeToFixedTime(time);
            spline.GetNormalsNonAlloc(normalsContainer, fixedTime);

            if (so.alignToEnd)
            {
                normalsContainer[0] = -normalsContainer[0];
                normalsContainer[2] = -normalsContainer[2];
            }

            //Rotations
            Quaternion parentRotations = SplineObjectUtility.GetCombinedParentRotations(so.soParent);
            Quaternion localSplineRotation = Quaternion.LookRotation(normalsContainer[2], normalsContainer[1]);
            Quaternion combinedRotations = localSplineRotation * parentRotations;
            Quaternion handleRotation = combinedRotations * so.localSplineRotation;

            //Pivot
            Vector3 pivot = spline.SplinePositionToWorldPosition(so.transform.parent, so.localSplinePosition, SplineObjectUtility.GetCombinedParentMatrixs(so.soParent), so.alignToEnd);

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
            if (so.alignToEnd) time = (spline.length - so.splinePosition.z) / spline.length;
            float fixedTime = spline.TimeToFixedTime(time);
            spline.GetNormalsNonAlloc(normalsContainer, fixedTime);

            if (so.alignToEnd)
            {
                normalsContainer[0] = -normalsContainer[0];
                normalsContainer[2] = -normalsContainer[2];
            }

            //Handle rotation
            Quaternion parentRotations = SplineObjectUtility.GetCombinedParentRotations(so.soParent);
            Quaternion localSplineRotation = Quaternion.LookRotation(normalsContainer[2], normalsContainer[1]);
            Quaternion handleRotation = localSplineRotation * so.splineRotation;

            //Pivot
            Vector3 pivot = spline.SplinePositionToWorldPosition(so.transform.parent, so.localSplinePosition, SplineObjectUtility.GetCombinedParentMatrixs(so.soParent), so.alignToEnd);

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

            if (toolActive || PositionTool.released)
            {
                if (GlobalSettings.GetGridVisibility() && PositionTool.released)
                {
                    newControlPointPos = EHandleGrid.SnapPoint(spline, newControlPointPos);
                    ActivatePositionToolForControlPoint(spline);
                }

                EHandleUndo.RecordNow(spline, "Move control point: " + spline.selectedControlPoint);
                EHandleSegment.SegmentMovement(spline, segment, type, newControlPointPos);
                if (type == Segment.ControlHandle.ANCHOR) EHandleSegment.LinkMovement(segment);

                if(PositionTool.released) ActivatePositionToolForControlPoint(spline);
            }
        }

        public static void UpdateOrientationForPositionTool()
        {
            UpdateOrientationForPositionTool(EHandleSceneView.GetCurrent(), EHandleSelection.selectedSpline);
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

                (Vector3, Vector3) directions = GetForwardDirectionForControlPoint(spline,
                                                                                  spline.segments[segementId].GetPosition(Segment.ControlHandle.ANCHOR),
                                                                                  spline.segments[segementId].GetPosition(Segment.ControlHandle.TANGENT_A),
                                                                                  spline.segments[segementId].GetPosition(Segment.ControlHandle.TANGENT_B),
                                                                                  SplineUtility.GetControlPointType(spline.selectedControlPoint));
                PositionTool.UpdateOrientation(sceneView.camera.transform.position, directions.Item1, directions.Item2);
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

            SceneView sceneView = EHandleSceneView.GetCurrent();

            float time = so.splinePosition.z / spline.length;
            if(so.alignToEnd) time = (spline.length - so.splinePosition.z) / spline.length;
            float fixedTime = spline.TimeToFixedTime(time);
            spline.GetNormalsNonAlloc(normalsContainer, fixedTime);
            Quaternion splineRotation = Quaternion.LookRotation(normalsContainer[2], normalsContainer[1]);
            Quaternion combinedRotation = splineRotation * SplineObjectUtility.GetCombinedParentRotations(so.soParent);

            normalsContainer[0] = combinedRotation * Vector3.right;
            normalsContainer[1] = combinedRotation * Vector3.up;
            normalsContainer[2] = combinedRotation * Vector3.forward;

            if (so.alignToEnd)
            {
                normalsContainer[0] = -normalsContainer[0];
                normalsContainer[2] = -normalsContainer[2];
            }

            Vector3 position = spline.SplinePositionToWorldPosition(so.transform.parent, so.localSplinePosition, SplineObjectUtility.GetCombinedParentMatrixs(so.soParent), so.alignToEnd);
            PositionTool.ActivateAndSetPosition(PositionTool.ActivationType.SPLINE_OBJECT, position, sceneView.camera.transform.position, normalsContainer[2], normalsContainer[1], false);
            so.activationPosition = so.localSplinePosition;
            EHandleEvents.InvokeAfterSplineObjectActivatePositionTool(so);
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

            Segment.ControlHandle segementType = SplineUtility.GetControlPointType(spline.selectedControlPoint);
            Vector3 position = segment.GetPosition(segementType);
            (Vector3, Vector3) directions = GetForwardDirectionForControlPoint(spline,
                                                                          spline.segments[segementId].GetPosition(Segment.ControlHandle.ANCHOR),
                                                                          spline.segments[segementId].GetPosition(Segment.ControlHandle.TANGENT_A),
                                                                          spline.segments[segementId].GetPosition(Segment.ControlHandle.TANGENT_B),
                                                                          segementType);

            PositionTool.ActivationType activationType = PositionTool.ActivationType.ANCHOR;
            Segment.ControlHandle type = SplineUtility.GetControlPointType(spline.selectedControlPoint);
            if (type == Segment.ControlHandle.TANGENT_A || type == Segment.ControlHandle.TANGENT_B) activationType = PositionTool.ActivationType.TANGENT;
            PositionTool.ActivateAndSetPosition(activationType,
                                                position,
                                                EHandleSceneView.GetCurrent().camera.transform.position,
                                                directions.Item1,
                                                directions.Item2,
                                                true);
            PositionTool.locked = segment.linkTarget == Segment.LinkTarget.SPLINE_CONNECTOR;
            PositionTool.lockedWarningMsg = "[Spline Architect] Can't move position handle when connected to a Spline Connector.";
        }

        private static (Vector3, Vector3) GetForwardDirectionForControlPoint(Spline spline, Vector3 anchor, Vector3 tangentA, Vector3 tangentB, Segment.ControlHandle tangentType)
        {
            Vector3 upDirection = spline.transform.up;
            Vector3 forwardDirection = (tangentA - anchor).normalized;
            if (tangentType == Segment.ControlHandle.TANGENT_B) forwardDirection = (anchor - tangentB).normalized;

            float dot = Vector3.Dot(forwardDirection, spline.transform.up);
            if (Mathf.Abs(dot) > 0.99f)
            { 
                upDirection = spline.transform.forward;
            }

            if (GeneralUtility.IsZero(forwardDirection)) return (spline.transform.forward, upDirection);
            return (forwardDirection, upDirection);
        }
    }
}