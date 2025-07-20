// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: MenuTangent.cs
//
// Author: Mikael Danielsson
// Date Created: 15-10-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Generic;

using UnityEngine;

using SplineArchitect.Utility;
using SplineArchitect.Objects;
using SplineArchitect.Libraries;

namespace SplineArchitect.Ui
{
    public class MenuTangent
    {
        private static Rect tangentWindowRect = new Rect(300, 100, 150, 0);
        private static string[] interpolationMode = new string[] { "Spline", "Line" };
        private static string containerXPos = "0";
        private static string containerYPos = "0";
        private static string containerZPos = "0";

        private static List<Segment> alignSegmentsContainer = new List<Segment>();

        public static void OnSceneGUI(Spline spline, Segment segment, Segment.ControlHandle segementType)
        {
            UpdateRect();
            GUILayout.Window(4, tangentWindowRect, windowID => DrawWindow(windowID, spline, segment, segementType), "", LibraryGUIStyle.empty);
        }

        private static void DrawWindow(int windowID, Spline spline, Segment segment, Segment.ControlHandle segementType)
        {
            EHandleUi.ResetGetBackgroundStyleId();

            //Selected border
            GUILayout.Box("", LibraryGUIStyle.lineYellowThick, GUILayout.ExpandWidth(true));

            //Header
            GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundHeader);
            EHandleUi.CreateLabelField("<b>Tangent " + (segementType == Segment.ControlHandle.TANGENT_A ? "A" : "B") + "</b> ", LibraryGUIStyle.textHeaderBlack, true);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundToolbar);

            GUI.enabled = false;
            if (segment.linkTarget != Segment.LinkTarget.NONE)
            {
                EHandleUi.CreateButton(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.iconUnlink, 35, 19, () => { }, false);
            }
            else
            {
                EHandleUi.CreateButton(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.iconLink, 35, 19, () => { }, false);
            }

            if(segment.linkTarget == Segment.LinkTarget.SPLINE_CONNECTOR)
            {
                //Mirror connector
                EHandleUi.CreateButtonToggle(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.iconMirrorConnector, LibraryGUIContent.iconMirrorConnectorActive, 35, 19, () => { }, segment.mirrorConnector, false);
            }
            GUI.enabled = true;

            EHandleUi.CreateSeparator();

            //Align tangents
            EHandleUi.CreateButton(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.iconAlignTangents, 35, 19, () =>
            {
                alignSegmentsContainer.Clear();
                alignSegmentsContainer.Add(segment);

                foreach (Segment s2 in segment.links)
                {
                    EHandleUndo.RecordNow(s2.splineParent, "Aligned tangents");

                    if (s2 == segment)
                        continue;

                    alignSegmentsContainer.Add(s2);
                }

                EHandleSegment.AlignTangents(alignSegmentsContainer);
                spline.monitor.MarkEditorDirty();
            }, segment.linkTarget == Segment.LinkTarget.ANCHOR);

            EHandleUi.CreateButton(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.iconFlatten, 35, 19, () => {}, false);
            EHandleUi.CreateButton(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.iconSplit, 35, 19, () => { }, false);

            //Next control point
            EHandleUi.CreateButton(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.iconNextControlPoint, 35, 19, () =>
            {
                EHandleUndo.RecordNow(spline, "Next control point");
                EHandleSelection.SelectPrimaryControlPoint(spline, EHandleSpline.GetNextControlPoint(spline));

                EActionToLateSceneGUI.Add(() => {
                    EHandleTool.UpdateOrientationForPositionTool(EHandleSceneView.GetSceneView(), spline);
                }, EventType.Layout);
            });

            //Prev control point
            EHandleUi.CreateButton(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.iconPrevControlPoint, 35, 19, () =>
            {
                EHandleUndo.RecordNow(spline, "Prev control point");
                EHandleSelection.SelectPrimaryControlPoint(spline, EHandleSpline.GetNextControlPoint(spline, true));

                EActionToLateSceneGUI.Add(() => {
                    EHandleTool.UpdateOrientationForPositionTool(EHandleSceneView.GetSceneView(), spline);
                }, EventType.Layout);
            });

            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundSubHeader);
            EHandleUi.CreateLabelField("<b>GENERAL</b>", LibraryGUIStyle.textSubHeader, true);
            GUILayout.EndHorizontal();

            //Position
            EHandleUi.CreateXYZInputFields("Position", segment.GetPosition(segementType), ref containerXPos, ref containerYPos, ref containerZPos, (position, dif) =>
            {
                EHandleUndo.RecordNow(spline, "Move control point(tangent): " + spline.selectedControlPoint);

                if (GlobalSettings.GetSegementMovementType() == (int)EHandleSegment.SegmentMovementType.CONTINUOUS)
                {
                    segment.SetContinuousPosition(segementType, position);
                }
                else if (GlobalSettings.GetSegementMovementType() == (int)EHandleSegment.SegmentMovementType.MIRRORED)
                {
                    segment.SetMirroredPosition(segementType, position);
                }
            }, 50);

            GUILayout.BeginHorizontal(EHandleUi.GetBackgroundStyle());
            EHandleUi.CreateLabelField($"Z position: {Mathf.Round(segment.zPosition * 100) / 100}", LibraryGUIStyle.textDefault, true, 153);
            EHandleUi.CreateLabelField($"Length: {Mathf.Round(segment.length * 100) / 100}", LibraryGUIStyle.textDefault, true);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(EHandleUi.GetBackgroundStyle());
            //Type
            GUILayout.FlexibleSpace();
            EHandleUi.CreatePopupField("Type:", 70, (int)segment.GetInterpolationType(), interpolationMode, (int newValue) =>
            {
                EHandleSelection.UpdateSelectedAnchorsRecordUndo(spline, (selected) =>
                {
                    selected.SetInterpolationMode((Segment.InterpolationType)newValue);
                }, "Updated interpolation type");
            }, 47, true);
            GUILayout.EndHorizontal();

            //Bottom
            EHandleUi.CreateLine();
            GUILayout.Box("", LibraryGUIStyle.backgroundBottomMenu, GUILayout.ExpandWidth(true));
            EHandleUi.CreateLine();
        }

        public static void UpdateRect()
        {
            Rect generalWindowRect = MenuGeneral.GetRect();
            Rect splineWindowRect = MenuSpline.GetRect();

            tangentWindowRect.height = LibraryGUIStyle.menuItemHeight * 6 + 16;
            tangentWindowRect.width = 0;

            tangentWindowRect.x = (generalWindowRect.x + generalWindowRect.width + 3) + (splineWindowRect.width + 3);
            tangentWindowRect.y = generalWindowRect.y - (tangentWindowRect.height - generalWindowRect.height);
        }
    }
}
