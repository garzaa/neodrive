// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EHandleDrawing.cs
//
// Author: Mikael Danielsson
// Date Created: 29-01-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

using SplineArchitect.Objects;
using SplineArchitect.Utility;
using SplineArchitect.Libraries;
using SplineArchitect.CustomTools;

namespace SplineArchitect
{
    public static class EHandleDrawing
    {
        private static Vector3[] triangle = new Vector3[4];
        private static Vector3[] normalsContainer = new Vector3[3];

        public static void OnSceneGUIGlobal(HashSet<Spline> splines, Event e)
        {
            if (e.type != EventType.Repaint)
                return;

            GlobalSettings.SplineHideMode hideMode = GlobalSettings.GetSplineHideMode();
            SceneView sceneView = EHandleSceneView.GetSceneView();

            foreach (Spline spline in splines)
            {
                if (!spline.IsEnabled())
                    continue;

                if (!sceneView.drawGizmos)
                    continue;

                bool primiarySelection = EHandleSelection.IsPrimiarySelection(spline);
                bool secondarySelection = EHandleSelection.IsSecondarySelection(spline);

                if (hideMode != GlobalSettings.SplineHideMode.NONE && !primiarySelection && !secondarySelection)
                    continue;

                DrawSpline(spline, primiarySelection, secondarySelection, hideMode);
                DrawLinkPreviewDots(spline, hideMode);
            }

            foreach (Spline spline in splines)
            {
                if (!spline.IsEnabled())
                    continue;

                bool isSecondarySelection = EHandleSelection.IsSecondarySelection(spline);
                bool isPrimiarySelection = EHandleSelection.IsPrimiarySelection(spline);

                if (isPrimiarySelection)
                {
                    if (sceneView.drawGizmos)
                    {
                        DrawControlPointsOnSelected(spline, hideMode);
                        DrawNormals(spline);
                    }

                    PositionTool.Draw(e);
                }

                if (sceneView.drawGizmos && (isPrimiarySelection || isSecondarySelection))
                {
                    EHandleGrid.DrawGridAndLabels(spline);
                }
            }

            DrawSplineConnectorPreviewDots(hideMode);
        }

        public static void DebugDrawSplineSpace(Spline spline)
        {
            Handles.color = Color.yellow;
            Handles.DrawLine(spline.transform.position, spline.transform.position + spline.transform.forward * spline.length);

            Handles.color = LibraryColor.white_RGB80_A100;
            for (int i = 0; i < spline.length; i += 10)
            {
                Handles.DotHandleCap(0, spline.transform.position + spline.transform.forward * i, Quaternion.identity, 0.1f, EventType.Repaint);
            }

            Handles.color = Color.blue;
            if (EHandleSelection.selectedSplineObject != null)
            {
                SplineObject selection = EHandleSelection.selectedSplineObject;
                Handles.DotHandleCap(0, spline.transform.TransformPoint(selection.splinePosition), Quaternion.identity, 0.25f, EventType.Repaint);

                Handles.color = Color.red;
                for (int i = 0; i < selection.transform.childCount; i++)
                {
                    SplineObject acoChild = selection.transform.GetChild(i).GetComponent<SplineObject>();

                    if (acoChild != null)
                        Handles.DotHandleCap(0, spline.transform.TransformPoint(acoChild.splinePosition), Quaternion.identity, 0.25f, EventType.Repaint);
                }
            }
        }

        public static void DrawSegmentIndicator(Event e, Spline spline, EHandleSegment.SegmentIndicatorData segmentIndicatorData)
        {
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            Handles.color = Color.white;
            float size = EHandleSegment.GetControlPointSize(spline.indicatorPosition);

            if (spline.indicatorDistanceToSpline < EHandleSpline.GetIndicatorActivationDistance(spline))
            {
                Handles.DotHandleCap(0, spline.indicatorPosition, Quaternion.identity, size, EventType.Repaint);
            }
            else if (!spline.loop)
            {
                int precision = EHandleSegment.GetDrawLinesCount(segmentIndicatorData.anchor,
                                                                 segmentIndicatorData.tangent,
                                                                 segmentIndicatorData.newAnchor,
                                                                 segmentIndicatorData.newTangentB,
                                                                 25);
                DrawSegement(segmentIndicatorData.anchor,
                                           segmentIndicatorData.tangent,
                                           segmentIndicatorData.originFromStart ? segmentIndicatorData.newTangentA : segmentIndicatorData.newTangentB,
                                           segmentIndicatorData.newAnchor,
                                           precision,
                                           2.5f);

                Handles.DotHandleCap(0, segmentIndicatorData.newAnchor, Quaternion.identity, size, EventType.Repaint);
                Handles.DotHandleCap(0, segmentIndicatorData.newTangentA, Quaternion.identity, size, EventType.Repaint);
                Handles.DotHandleCap(0, segmentIndicatorData.newTangentB, Quaternion.identity, size, EventType.Repaint);
            }

            if (e.type == EventType.Repaint)
                SceneView.RepaintAll();
        }

        public static void DrawIndicatorGrid(Event e, EHandleSegment.SegmentIndicatorData segmentIndicatorData)
        {
            float size = EHandleSegment.GetControlPointSize(segmentIndicatorData.newAnchor);

            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

            if (segmentIndicatorData.gridSpline != null)
            {
                Bounds bounds = EHandleGrid.GetGridBounds(segmentIndicatorData.gridSpline, segmentIndicatorData.newAnchor);

                Handles.color = Color.white;
                EHandleGrid.DrawGrid(segmentIndicatorData.gridSpline.transform, bounds, GlobalSettings.GetGridSize());
            }

            Handles.color = Color.yellow;

            Handles.DrawLine(segmentIndicatorData.newAnchor, segmentIndicatorData.newTangentA, 1);
            Handles.DrawLine(segmentIndicatorData.newAnchor, segmentIndicatorData.newTangentB, 1);

            Handles.color = Color.white;
            Handles.DotHandleCap(0, segmentIndicatorData.newAnchor, Quaternion.identity, size, EventType.Repaint);
            Handles.DotHandleCap(0, segmentIndicatorData.newTangentA, Quaternion.identity, size, EventType.Repaint);
            Handles.DotHandleCap(0, segmentIndicatorData.newTangentB, Quaternion.identity, size, EventType.Repaint);

            if (e.type == EventType.Repaint)
                SceneView.RepaintAll();
        }

        private static void DrawSpline(Spline spline, bool primiarySelection, bool secondarySelection, GlobalSettings.SplineHideMode hideMode)
        {
            if (spline.segments.Count < 2)
                return;

            for (int i = 1; i < spline.segments.Count; i++)
            {
                Segment segmentFrom = spline.segments[i - 1];
                Segment segmentTo = spline.segments[i];

                int linesCount = EHandleSegment.GetDrawLinesCount(segmentFrom, segmentTo);

                Vector3 anchor1 = segmentFrom.GetPosition(Segment.ControlHandle.ANCHOR);
                Vector3 tangent1A = segmentFrom.GetPosition(Segment.ControlHandle.TANGENT_A);
                Vector3 tangent2B = segmentTo.GetPosition(Segment.ControlHandle.TANGENT_B);
                Vector3 anchor2 = segmentTo.GetPosition(Segment.ControlHandle.ANCHOR);

                if (primiarySelection && hideMode != GlobalSettings.SplineHideMode.SELECTED_OCCLUDED)
                {
                    Handles.color = LibraryColor.black_A70;
                    Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
                    DrawSegement(anchor1, tangent1A, tangent2B, anchor2, linesCount, 1);
                }

                Handles.color = spline.color;
                if (primiarySelection || secondarySelection)
                    Handles.color = Color.yellow;

                if (primiarySelection && EHandleSpline.controlPointCreationActive && spline.indicatorSegement == i && EHandleSpline.GetIndicatorActivationDistance(spline) > spline.indicatorDistanceToSpline)
                    Handles.color = Color.white;

                Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

                float splineSize = 2f;
                if (spline == EHandleSelection.hoveredSpline) splineSize = 3f;
                else Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

                DrawSegement(anchor1, tangent1A, tangent2B, anchor2, linesCount, splineSize);
                DrawTriangleOnSegment(spline, segmentFrom, primiarySelection || secondarySelection, hideMode);
            }
        }

        public static void DrawSegement(Vector3 anchorA, Vector3 tangnetA, Vector3 tangentB, Vector3 anchorB, int lines, float size)
        {
            for (int i = 0; i < lines; i++)
            {
                float startTime = (float)i / lines;
                float endTime = (float)(i + 1) / lines;

                Vector3 startPoint = BezierUtility.CubicLerp(anchorA, tangnetA, tangentB, anchorB, startTime);
                Vector3 endPoint = BezierUtility.CubicLerp(anchorA, tangnetA, tangentB, anchorB, endTime);

                Handles.DrawLine(startPoint, endPoint, size);
            }
        }

        private static void DrawTriangleOnSegment(Spline spline, Segment segment, bool selected, GlobalSettings.SplineHideMode hideMode)
        {
            float time = (segment.zPosition + segment.length * 0.5f) / spline.length;
            float fixedTime = spline.TimeToFixedTime(time);
            spline.GetNormalsNonAlloc(normalsContainer, fixedTime);
            Vector3 position = spline.GetPosition(fixedTime);
            float size = EHandleSegment.GetControlPointSize(position) * 1.3f;

            triangle[0] = position - (normalsContainer[0] * size * 0) + (normalsContainer[2] * size);
            triangle[1] = position + (normalsContainer[0] * size * 0) + (normalsContainer[2] * size);
            triangle[2] = position + (normalsContainer[0] * size) - (normalsContainer[2] * size);
            triangle[3] = position - (normalsContainer[0] * size) - (normalsContainer[2] * size);

            Handles.color = Color.white;

            if (selected && hideMode != GlobalSettings.SplineHideMode.SELECTED_OCCLUDED)
            {
                Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
                Handles.DrawSolidRectangleWithOutline(triangle, LibraryColor.grey_RGB20_A50, Color.yellow);
            }
            else
            {
                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                Handles.DrawSolidRectangleWithOutline(triangle, LibraryColor.grey_RGB20_A50, spline.color);
            }
        }

        private static void DrawSplineConnectorPreviewDots(GlobalSettings.SplineHideMode hideMode)
        {
            Spline selected = EHandleSelection.selectedSpline;

            if (selected == null)
                return;

            if (selected.selectedControlPoint == 0)
                return;

            if (PositionTool.activePart == PositionTool.Part.NONE)
                return;

            int segmentIndex = SplineUtility.ControlPointIdToSegmentIndex(selected.selectedControlPoint);
            Segment selectedSegment = selected.segments[segmentIndex];
            Vector3 selectedPos = selectedSegment.GetPosition(Segment.ControlHandle.ANCHOR);
            float controlDistance = 100 * EHandleSegment.DistanceModifier(selectedPos);

            foreach (SplineConnector sc in HandleRegistry.GetSplineConnectors())
            {
                if (Vector3.Distance(sc.transform.position, selectedPos) > controlDistance)
                    continue;

                float size = EHandleSegment.GetControlPointSize(sc.transform.position) * 0.55f;

                if (hideMode != GlobalSettings.SplineHideMode.SELECTED_OCCLUDED)
                    Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
                else
                    Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

                Handles.color = LibraryColor.orange_A100;

                Handles.DotHandleCap(0, sc.transform.position, Quaternion.identity, size, EventType.Repaint);
            }
        }

        private static void DrawLinkPreviewDots(Spline spline, GlobalSettings.SplineHideMode hideMode)
        {
            Spline selected = EHandleSelection.selectedSpline;

            if (selected == null || SplineUtility.GetControlPointType(selected.selectedControlPoint) != Segment.ControlHandle.ANCHOR)
                return;

            if (PositionTool.activePart == PositionTool.Part.NONE)
                return;

            if (selected == spline)
                return;

            int dontDrawLastIfLoop = 0;
            if (spline.loop)
                dontDrawLastIfLoop = 1;

            int segmentIndex = SplineUtility.ControlPointIdToSegmentIndex(selected.selectedControlPoint);
            Vector3 selectedPos = selected.segments[segmentIndex].GetPosition(Segment.ControlHandle.ANCHOR);
            float controlDistance = 100 * EHandleSegment.DistanceModifier(selectedPos);

            //Skip if to far away.
            if (Vector3.Distance(spline.controlPointsBounds.ClosestPoint(selectedPos), selectedPos) > controlDistance)
                return;

            for (int i = 0; i < spline.segments.Count - dontDrawLastIfLoop; i++)
            {
                Segment s = spline.segments[i];
                Vector3 anchorPosition = s.GetPosition(Segment.ControlHandle.ANCHOR);

                if (Vector3.Distance(anchorPosition, selectedPos) > controlDistance)
                    continue;

                if (GeneralUtility.IsEqual(selectedPos, anchorPosition))
                    continue;

                float size = EHandleSegment.GetControlPointSize(anchorPosition) * 0.55f;

                if(hideMode != GlobalSettings.SplineHideMode.SELECTED_OCCLUDED)
                    Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
                else
                    Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

                Handles.color = spline.color;

                Handles.DotHandleCap(0, anchorPosition, Quaternion.identity, size, EventType.Repaint);
            }
        }

        private static void DrawControlPointsOnSelected(Spline spline, GlobalSettings.SplineHideMode hideMode)
        {
            int dontDrawLastIfLoop = 0;
            if (spline.loop)
                dontDrawLastIfLoop = 1;

            int hoveredSegmentId = SplineUtility.ControlPointIdToSegmentIndex(EHandleSelection.hoveredCp);
            Segment.ControlHandle hoveredType = SplineUtility.GetControlPointType(EHandleSelection.hoveredCp);

            //Draw lines between anchor and tangents
            for (int i = 0; i < spline.segments.Count - dontDrawLastIfLoop; i++)
            {
                Segment segment = spline.segments[i];

                int anchorId = SplineUtility.SegmentIndexToControlPointId(i, Segment.ControlHandle.ANCHOR);
                float anchorSize = EHandleSegment.GetControlPointSize(segment.GetPosition(Segment.ControlHandle.ANCHOR));
                float tangentASize = EHandleSegment.GetControlPointSize(segment.GetPosition(Segment.ControlHandle.TANGENT_A));
                float tangentBSize = EHandleSegment.GetControlPointSize(segment.GetPosition(Segment.ControlHandle.TANGENT_B));
                float lineAToAnchorSize = 2f;
                float lineBToAnchorSize = 2f;

                if (hoveredSegmentId == i)
                {
                    if (hoveredType == Segment.ControlHandle.ANCHOR)
                    {
                        lineAToAnchorSize = 3;
                        lineBToAnchorSize = 3;
                        anchorSize *= 1.1f;
                    }

                    if (hoveredType == Segment.ControlHandle.TANGENT_A)
                    {
                        lineAToAnchorSize = 3;
                        tangentASize *= 1.15f;
                    }

                    if (hoveredType == Segment.ControlHandle.TANGENT_B)
                    {
                        lineBToAnchorSize = 3;
                        tangentBSize *= 1.15f;
                    }
                }

                //Settings
                Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
                Handles.color = LibraryColor.black_A50;

                if (hideMode != GlobalSettings.SplineHideMode.SELECTED_OCCLUDED)
                {
                    //Shadow controlpoints
                    Handles.DotHandleCap(0, segment.GetPosition(Segment.ControlHandle.ANCHOR), Quaternion.identity, anchorSize * 1.3f, EventType.Repaint);
                    if (segment.GetInterpolationType() == Segment.InterpolationType.SPLINE)
                    {
                        Handles.DotHandleCap(0, segment.GetPosition(Segment.ControlHandle.TANGENT_A), Quaternion.identity, tangentASize, EventType.Repaint);
                        Handles.DotHandleCap(0, segment.GetPosition(Segment.ControlHandle.TANGENT_B), Quaternion.identity, tangentBSize, EventType.Repaint);

                        //Settings
                        Handles.color = LibraryColor.black_A70;
                        //Shadow lines
                        Handles.DrawLine(segment.GetPosition(Segment.ControlHandle.ANCHOR), segment.GetPosition(Segment.ControlHandle.TANGENT_A));
                        Handles.DrawLine(segment.GetPosition(Segment.ControlHandle.ANCHOR), segment.GetPosition(Segment.ControlHandle.TANGENT_B));
                    }
                }

                //Settings
                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                Handles.color = LibraryColor.grey_A100;

                if (segment.GetInterpolationType() == Segment.InterpolationType.SPLINE)
                {
                    //Lines
                    Handles.DrawLine(segment.GetPosition(Segment.ControlHandle.ANCHOR), segment.GetPosition(Segment.ControlHandle.TANGENT_A), lineAToAnchorSize);
                    Handles.DrawLine(segment.GetPosition(Segment.ControlHandle.ANCHOR), segment.GetPosition(Segment.ControlHandle.TANGENT_B), lineBToAnchorSize);
                }

                //Anchor
                if (anchorId != spline.selectedControlPoint)
                {
                    float size = 1.3f;
                    bool isAnchorSelected = spline.selectedAnchors.Contains(anchorId);

                    Handles.color = Color.yellow;
                    if(isAnchorSelected && hideMode != GlobalSettings.SplineHideMode.SELECTED_OCCLUDED) Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
                    else Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                    Handles.DotHandleCap(0, segment.GetPosition(Segment.ControlHandle.ANCHOR), Quaternion.identity, anchorSize * size, EventType.Repaint);

                    if (!isAnchorSelected)
                    {
                        Handles.color = spline.color;
                        Handles.DotHandleCap(0, segment.GetPosition(Segment.ControlHandle.ANCHOR), Quaternion.identity, anchorSize, EventType.Repaint);
                    }
                }
                if(segment.GetInterpolationType() == Segment.InterpolationType.SPLINE)
                {
                    //Tanget a
                    Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                    if (anchorId + 1 != spline.selectedControlPoint)
                    {
                        Handles.color = spline.color;
                        Handles.DotHandleCap(0, segment.GetPosition(Segment.ControlHandle.TANGENT_A), Quaternion.identity, tangentASize, EventType.Repaint);
                    }

                    //Tanget b
                    if (anchorId + 2 != spline.selectedControlPoint)
                    {
                        Handles.color = spline.color;
                        Handles.DotHandleCap(0, segment.GetPosition(Segment.ControlHandle.TANGENT_B), Quaternion.identity, tangentBSize, EventType.Repaint);
                    }
                }

                if(segment.linkTarget != Segment.LinkTarget.NONE)
                {
                    Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
                    Handles.color = segment.linkTarget == Segment.LinkTarget.ANCHOR ? Color.yellow : LibraryColor.orange_A100;
                    Handles.DotHandleCap(0, segment.GetPosition(Segment.ControlHandle.ANCHOR), Quaternion.identity, anchorSize * 0.4f, EventType.Repaint);
                }
            }
        }

        private static void DrawNormals(Spline spline)
        {
            if (GlobalSettings.ShowNormals() == false)
                return;

            if (spline.segments.Count < 2)
                return;

            if (spline.normalsLocal.Length <= 0 && spline.normalType == Spline.NormalType.DYNAMIC)
                return;

            float spaceLength = 1 / (GlobalSettings.GetNormalsSpacing() * (spline.length / 100));
            float length = GlobalSettings.GetNormalsLength();
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

            Vector3 c1;
            for (float time = 0; time <= 1; time += spaceLength)
            {
                float fixedTime = spline.TimeToFixedTime(time);
                c1 = spline.GetPosition(fixedTime);
                spline.GetNormalsNonAlloc(normalsContainer, fixedTime, Space.World);

                Handles.color = Color.red;
                Handles.DrawLine(c1, c1 + normalsContainer[0] * length);

                Handles.color = Color.green;
                Handles.DrawLine(c1, c1 + normalsContainer[1] * length);
            }

            c1 = spline.GetPosition(1);
            spline.GetNormalsNonAlloc(normalsContainer, 1);

            Handles.color = Color.red;
            Handles.DrawLine(c1, c1 + normalsContainer[0] * length);

            Handles.color = Color.green;
            Handles.DrawLine(c1, c1 + normalsContainer[1] * length);
        }
    }
}