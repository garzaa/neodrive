// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EHandleGrid.cs
//
// Author: Mikael Danielsson
// Date Created: 28-09-2024
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using Unity.Mathematics;

using SplineArchitect.Objects;
using SplineArchitect.Utility;
using SplineArchitect.Libraries;
using SplineArchitect.CustomTools;

namespace SplineArchitect
{
    public class EHandleGrid
    {
        public static void DrawGridAndLabels(Spline spline)
        {
            if (!GlobalSettings.GetGridVisibility())
                return;

            Bounds gridBounds = GetGridBounds(spline);
            DrawGrid(spline.transform, gridBounds, GlobalSettings.GetGridSize());
            DrawLabels(spline);
        }

        public static Bounds GetGridBounds(Spline spline, Vector3 point, bool keepLocal = true)
        {
            float gridSize = GlobalSettings.GetGridSize();
            point = spline.transform.InverseTransformPoint(point);

            float lowestX = point.x;
            float highestX = point.x;
            float lowestZ = point.z;
            float highestZ = point.z;

            int extraSize = 2;
            lowestX -= gridSize * extraSize;
            highestX += gridSize * extraSize;
            lowestZ -= gridSize * extraSize;
            highestZ += gridSize * extraSize;

            Vector3 min = spline.transform.TransformPoint(new Vector3(lowestX, spline.gridCenterPoint.y, lowestZ));
            min = SnapPoint(spline, min, true);
            Vector3 max = spline.transform.TransformPoint(new Vector3(highestX, spline.gridCenterPoint.y, highestZ));
            max = SnapPoint(spline, max, true);

            Bounds bounds = new Bounds();
            bounds.SetMinMax(min, max);

            return bounds;
        }

        public static Bounds GetGridBounds(Spline spline)
        {
            float gridSize = GlobalSettings.GetGridSize();

            float lowestX = 99999;
            float highestX = -99999;
            float lowestZ = 99999;
            float highestZ = -99999;

            foreach (Segment s in spline.segments)
            {
                Vector3 anchor = s.GetPosition(Segment.ControlHandle.ANCHOR, Space.Self);
                Vector3 tangentA = s.GetPosition(Segment.ControlHandle.TANGENT_A, Space.Self);
                Vector3 tangentB = s.GetPosition(Segment.ControlHandle.TANGENT_B, Space.Self);

                if (anchor.x < lowestX) lowestX = anchor.x;
                if (anchor.x > highestX) highestX = anchor.x;
                if (tangentA.x < lowestX) lowestX = tangentA.x;
                if (tangentA.x > highestX) highestX = tangentA.x;
                if (tangentB.x < lowestX) lowestX = tangentB.x;
                if (tangentB.x > highestX) highestX = tangentB.x;

                if (anchor.z < lowestZ) lowestZ = anchor.z;
                if (anchor.z > highestZ) highestZ = anchor.z;
                if (tangentA.z < lowestZ) lowestZ = tangentA.z;
                if (tangentA.z > highestZ) highestZ = tangentA.z;
                if (tangentB.z < lowestZ) lowestZ = tangentB.z;
                if (tangentB.z > highestZ) highestZ = tangentB.z;
            }

            int extraSize = 2;
            lowestX -= gridSize * extraSize;
            highestX += gridSize * extraSize;
            lowestZ -= gridSize * extraSize;
            highestZ += gridSize * extraSize;

            Vector3 min = spline.transform.TransformPoint(new Vector3(lowestX, spline.gridCenterPoint.y, lowestZ));
            min = SnapPoint(spline, min, true);
            Vector3 max = spline.transform.TransformPoint(new Vector3(highestX, spline.gridCenterPoint.y, highestZ));
            max = SnapPoint(spline, max, true);

            Bounds bounds = new Bounds();
            bounds.SetMinMax(min, max);

            return bounds;
        }

        public static void DrawLabels(Spline spline)
        {
            Handles.color = Color.black;
            Segment.ControlHandle selectedType = SplineUtility.GetControlPointType(spline.selectedControlPoint);

            foreach (Segment s in spline.segments)
            {
                Vector3 anchor = s.GetPosition(Segment.ControlHandle.ANCHOR, Space.Self);
                Vector3 tangentA = s.GetPosition(Segment.ControlHandle.TANGENT_A, Space.Self);
                Vector3 tangentB = s.GetPosition(Segment.ControlHandle.TANGENT_B, Space.Self);

                Vector3 labelPointAnchor = new Vector3(anchor.x, spline.gridCenterPoint.y, anchor.z);
                Vector3 worldLabelPointAnchor = spline.transform.TransformPoint(labelPointAnchor);
                Vector3 worldAnchor = spline.transform.TransformPoint(anchor);

                Vector3 labelPointTangentA = new Vector3(tangentA.x, spline.gridCenterPoint.y, tangentA.z);
                Vector3 worldLabelPointTangentA = spline.transform.TransformPoint(labelPointTangentA);
                Vector3 worldTangentA = spline.transform.TransformPoint(tangentA);

                Vector3 labelPointTangentB = new Vector3(tangentB.x, spline.gridCenterPoint.y, tangentB.z);
                Vector3 worldLabelPointTangentB = spline.transform.TransformPoint(labelPointTangentB);
                Vector3 worldTangentB = spline.transform.TransformPoint(tangentB);

                bool skipDraw = selectedType != Segment.ControlHandle.ANCHOR && (GeneralUtility.IsEqual(labelPointAnchor, labelPointTangentB) || GeneralUtility.IsEqual(labelPointAnchor, labelPointTangentA));

                if (!GeneralUtility.IsEqual(labelPointAnchor, anchor) && !skipDraw)
                {
                    float value = Mathf.Round((anchor.y - labelPointAnchor.y) * 100) / 100;
                    Handles.DrawLine(worldLabelPointAnchor, worldAnchor);
                    Handles.Label(worldLabelPointAnchor, value.ToString(), LibraryGUIStyle.textSceneView);
                }

                if(s.GetInterpolationType() == Segment.InterpolationType.SPLINE)
                {
                    skipDraw = selectedType != Segment.ControlHandle.TANGENT_A && (GeneralUtility.IsEqual(labelPointTangentA, labelPointAnchor) || GeneralUtility.IsEqual(labelPointTangentA, labelPointTangentB));

                    if (!GeneralUtility.IsEqual(labelPointTangentA, tangentA) && !skipDraw)
                    {
                        float value = Mathf.Round((tangentA.y - labelPointTangentA.y) * 100) / 100;
                        Handles.DrawLine(worldLabelPointTangentA, worldTangentA);
                        Handles.Label(worldLabelPointTangentA, value.ToString(), LibraryGUIStyle.textSceneView);
                    }

                    skipDraw = selectedType != Segment.ControlHandle.TANGENT_B && (GeneralUtility.IsEqual(labelPointTangentB, labelPointAnchor) || GeneralUtility.IsEqual(labelPointTangentB, labelPointTangentA));

                    if (!GeneralUtility.IsEqual(labelPointTangentB, tangentB) && !skipDraw)
                    {
                        float value = Mathf.Round((tangentB.y - labelPointTangentB.y) * 100) / 100;
                        Handles.DrawLine(worldLabelPointTangentB, worldTangentB);
                        Handles.Label(worldLabelPointTangentB, value.ToString(), LibraryGUIStyle.textSceneView);
                    }
                }
            }
        }

        public static void DrawGrid(Transform originTransform, Bounds bounds, float size)
        {
            float increment = bounds.min.x;
            int count = 0;
            int colorType = GlobalSettings.GetGridColorType();

            Color color1 = new Color(1, 1, 1, 1);
            if (colorType == 1)
                color1 = new Color(0.5f, 0.5f, 0.5f, 1);
            else if (colorType == 2)
                color1 = new Color(0, 0, 0, 1);

            Color color2 = new Color(color1.r, color1.g, color1.b, 0.33f);

            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

            while (increment < bounds.max.x + size)
            {
                Vector3 point1 = new Vector3(increment, bounds.max.y, bounds.min.z);
                Vector3 point2 = new Vector3(increment, bounds.max.y, bounds.max.z);

                if(originTransform != null)
                {
                    point1 = originTransform.TransformPoint(point1);
                    point2 = originTransform.TransformPoint(point2);
                }

                if (count != 0)
                {
                    Handles.color = color2;
                    Handles.DrawLine(point1, point2);
                }
                else
                {
                    Handles.color = color1;
                    Handles.DrawLine(point1, point2);
                }
                increment += size;

                if (count == 9) count = 0;
                else count++;
            }

            increment = bounds.min.z;
            count = 0;
            while (increment < bounds.max.z + size)
            {
                Vector3 point1 = new Vector3(bounds.min.x, bounds.max.y, increment);
                Vector3 point2 = new Vector3(bounds.max.x, bounds.max.y, increment);

                if (originTransform != null)
                {
                    point1 = originTransform.TransformPoint(point1);
                    point2 = originTransform.TransformPoint(point2);
                }

                if (count != 0)
                {
                    Handles.color = color2;
                    Handles.DrawLine(point1, point2);
                }
                else
                {
                    Handles.color = color1;
                    Handles.DrawLine(point1, point2);
                }
                increment += size;

                if (count == 9) count = 0;
                else count++;
            }
        }

        public static Vector3 SnapPoint(Spline spline, Vector3 worldPoint, bool keepLocal = false)
        {
            Vector3 gridPoint = spline.transform.InverseTransformPoint(worldPoint);
            gridPoint -= spline.gridCenterPoint;
            gridPoint = GeneralUtility.RoundToClosest(gridPoint, GlobalSettings.GetGridSize());
            gridPoint += spline.gridCenterPoint;

            if (!keepLocal)
            {
                gridPoint = spline.transform.TransformPoint(gridPoint);
            }

            return gridPoint;
        }

        public static void AlignGrid(Spline primary, Spline secondary)
        {
            if (primary == null)
                return;

            Vector3 gridPoint = primary.transform.TransformPoint(primary.gridCenterPoint);
            secondary.gridCenterPoint = secondary.transform.InverseTransformPoint(gridPoint);

            for(int i  = 0; i < secondary.segments.Count; i++)
            {
                Segment s = secondary.segments[i];
                Vector3 pos = s.GetPosition(Segment.ControlHandle.ANCHOR);
                Vector3 newPos = SnapPoint(secondary, pos);
                s.SetPosition(Segment.ControlHandle.ANCHOR, newPos);
                s.Translate(Segment.ControlHandle.TANGENT_A, pos - newPos);
                s.Translate(Segment.ControlHandle.TANGENT_B, pos - newPos);
            }
        }

        public static void GridToCenter(Spline spline)
        {
            spline.gridCenterPoint = spline.transform.InverseTransformPoint(spline.GetCenter());

            for (int i = 0; i < spline.segments.Count; i++)
            {
                Segment s = spline.segments[i];
                Vector3 pos = s.GetPosition(Segment.ControlHandle.ANCHOR);
                Vector3 newPos = SnapPoint(spline, pos);
                s.SetPosition(Segment.ControlHandle.ANCHOR, newPos);
                s.Translate(Segment.ControlHandle.TANGENT_A, pos - newPos);
                s.Translate(Segment.ControlHandle.TANGENT_B, pos - newPos);
            }
        }
    }
}
