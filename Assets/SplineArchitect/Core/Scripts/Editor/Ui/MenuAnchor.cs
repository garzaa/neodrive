// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: MenuAnchor.cs
//
// Author: Mikael Danielsson
// Date Created: 15-10-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System;

using UnityEngine;

using SplineArchitect.Objects;
using SplineArchitect.Utility;
using SplineArchitect.Libraries;

namespace SplineArchitect.Ui
{
    public class MenuAnchor
    {
        //General
        private static Rect anchorWindowRect = new Rect(300, 100, 150, 0);
        private static List<Segment> closestSegmentContainer = new List<Segment>();
        private static string[] interpolationMode = new string[] { "Spline", "Line"};
        private static List<Segment> alignSegmentsContainer = new List<Segment>();

        //Position
        private static string containerXPos = "0";
        private static string containerYPos = "0";
        private static string containerZPos = "0";

        //Deformation
        private static string containerZRot = "0";
        private static string containerZRotAlignment = "0";
        private static string containerContrast = "0";
        private static string containerNoise = "0";
        private static string containerScaleX = "0";
        private static string containerScaleY = "0";
        private static string containerSadleSkewX = "0";
        private static string containerSadleSkewY = "0";

        //Addons
        public static List<(string, Action<Spline, int>)> addonsDrawWindow = new();
        public static List<(string, GUIContent, GUIContent)> addonsButtons = new();
        public static List<(string, Func<Spline, Rect>)> addonsCalcWindowSize = new();

        public static void OnSceneGUI(Spline spline, int segmentIndex)
        {
            UpdateRect(spline);
            anchorWindowRect = GUILayout.Window(3, anchorWindowRect, windowID => DrawWindow(windowID, spline, segmentIndex), "", LibraryGUIStyle.empty);

            if(Event.current.type == EventType.Repaint)
                EHandleSegment.UpdateLoopEndData(spline, spline.segments[segmentIndex]);
        }

        private static void DrawWindow(int windowID, Spline spline, int segmentIndex)
        {
            #region top
            EHandleUi.ResetGetBackgroundStyleId();

            Segment segment = spline.segments[segmentIndex];

            //Selected border
            GUILayout.Box("", LibraryGUIStyle.lineYellowThick, GUILayout.ExpandWidth(true));

            //Header
            int selectionCount = spline.selectedAnchors.Count;
            string headerText = "<b>Anchor " + ((spline.selectedControlPoint - 1000) / 3 + 1) + "</b> " + (selectionCount > 0 ? " + (" + selectionCount + ")" : "");
            GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundHeader);
            EHandleUi.CreateLabelField("<b>" + headerText + "</b> ", LibraryGUIStyle.textHeaderBlack, true);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundToolbar);

            //Unlink
            if (segment.linkTarget != Segment.LinkTarget.NONE)
            {
                EHandleUi.CreateButton(EHandleUi.ButtonType.DEFAULT_ACTIVE, LibraryGUIContent.iconUnlink, 35, 19, () => 
                {
                    EHandleSelection.UpdateSelectedAnchorsRecordUndo(spline, (s) =>
                    {
                        s.linkTarget = Segment.LinkTarget.NONE;

                        if (s.links != null && s.links.Count <= 2)
                        {
                            foreach (Segment s2 in s.links)
                            {
                                EHandleUndo.RecordNow(s2.splineParent, "Unlinked anchor");
                                s2.linkTarget = Segment.LinkTarget.NONE;
                            }
                        }

                        if (s.splineConnector != null)
                            s.splineConnector.RemoveConnection(s);

                        s.splineConnector = null;
                    }, "Unlinked anchor");

                    EHandleTool.ActivatePositionToolForControlPoint(spline);
                });
            }
            //Link
            else
            {
                EHandleUi.CreateButton(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.iconLink, 35, 19, () =>
                {
                    EHandleSelection.UpdateSelectedAnchorsRecordUndo(spline, (s) =>
                    {
                        //Get anchor point
                        Vector3 anchorPoint = s.GetPosition(Segment.ControlHandle.ANCHOR);

                        //Closest segment
                        Segment closestSegment = EHandleSpline.GetClosestSegment(HandleRegistry.GetSplines(), anchorPoint, out _, out _, s);
                        Vector3 linkPointAnchor = closestSegment.GetPosition(Segment.ControlHandle.ANCHOR);
                        float anchorDistance = Vector3.Distance(anchorPoint, linkPointAnchor);

                        //Closest connector
                        SplineConnector closestConnector = SplineConnectorUtility.GetClosest(anchorPoint);
                        Vector3 linkPointConnector = new Vector3(99999, 99999, 99999);
                        if(closestConnector != null) linkPointConnector = closestConnector.transform.position;
                        float connectorDistance = Vector3.Distance(anchorPoint, linkPointConnector) - 0.01f;

                        if (connectorDistance > anchorDistance)
                        {
                            s.SetAnchorPosition(linkPointAnchor);
                            s.linkTarget = Segment.LinkTarget.ANCHOR;

                            if (spline.loop && spline.segments[0] == s)
                                spline.segments[spline.segments.Count - 1].SetPosition(Segment.ControlHandle.ANCHOR, linkPointAnchor);

                            SplineUtility.GetSegmentsAtPointNoAlloc(closestSegmentContainer, HandleRegistry.GetSplines(), linkPointAnchor);
                            foreach (Segment s2 in closestSegmentContainer)
                            {
                                EHandleUndo.RecordNow(s2.splineParent, "Linked anchor");
                                s2.linkTarget = Segment.LinkTarget.ANCHOR;
                            }
                        }
                        else
                        {
                            closestConnector.AlignSegment(segment);
                            s.linkTarget = Segment.LinkTarget.SPLINE_CONNECTOR;

                            if (spline.loop && spline.segments[0] == s)
                                closestConnector.AlignSegment(spline.segments[spline.segments.Count - 1]);
                        }

                    }, "Linked anchor");

                    EHandleTool.ActivatePositionToolForControlPoint(spline);
                });
            }

            if (segment.linkTarget == Segment.LinkTarget.SPLINE_CONNECTOR)
            {
                //Mirror connector
                EHandleUi.CreateButtonToggle(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.iconMirrorConnector, LibraryGUIContent.iconMirrorConnectorActive, 35, 19, () =>
                {
                    EHandleSelection.UpdateSelectedAnchorsRecordUndo(spline, (selected) =>
                    {
                        selected.mirrorConnector = !segment.mirrorConnector;
                    }, "Mirrored spline connector");
                }, segment.mirrorConnector);
            }

            EHandleUi.CreateSeparator();

            //Align tangents
            EHandleUi.CreateButton(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.iconAlignTangents, 35, 19, () =>
            {
                EHandleSelection.UpdateSelectedAnchors(spline, (seg) =>
                {
                    if (seg.links.Count == 0)
                        return;

                    alignSegmentsContainer.Clear();
                    alignSegmentsContainer.Add(seg);

                    foreach (Segment s2 in seg.links)
                    {
                        EHandleUndo.RecordNow(s2.splineParent, "Aligned tangents");

                        if (s2 == seg)
                            continue;

                        alignSegmentsContainer.Add(s2);
                    }

                    EHandleSegment.AlignTangents(alignSegmentsContainer);
                });

                spline.monitor.MarkEditorDirty();
            }, segment.linkTarget == Segment.LinkTarget.ANCHOR);

            //Flatten
            EHandleUi.CreateButton(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.iconFlatten, 35, 19, () =>
            {
                EHandleSelection.UpdateSelectedAnchorsRecordUndo(spline, (selected) =>
                {
                    if (spline.selectedAnchors.Count == 0)
                    {
                        Vector3 anchor = segment.GetPosition(Segment.ControlHandle.ANCHOR);
                        Vector3 tangentA = segment.GetPosition(Segment.ControlHandle.TANGENT_A);
                        Vector3 tangentB = segment.GetPosition(Segment.ControlHandle.TANGENT_B);

                        tangentA = new Vector3(tangentA.x, anchor.y, tangentA.z);
                        tangentB = new Vector3(tangentB.x, anchor.y, tangentB.z);

                        segment.SetPosition(Segment.ControlHandle.TANGENT_A, tangentA);
                        segment.SetPosition(Segment.ControlHandle.TANGENT_B, tangentB);
                    }
                    else
                    {
                        EHandleSpline.FlattenControlPoints(spline, selected);
                    }
                }, "Flatten control points");

                EActionToLateSceneGUI.Add(() => {
                    EHandleTool.UpdateOrientationForPositionTool(EHandleSceneView.GetSceneView(), spline);
                }, EventType.Layout);
            });

            //Split
            int selectedSegmentId = SplineUtility.ControlPointIdToSegmentIndex(spline.selectedControlPoint);
            EHandleUi.CreateButton(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.iconSplit, 35, 19, () =>
            {
                EHandleSpline.Split(spline, selectedSegmentId);
            }, !spline.loop && selectedSegmentId > 0 && selectedSegmentId < (spline.segments.Count - 1));

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

            GUILayout.EndHorizontal();
            #endregion

            #region general
            if (spline.selectedAnchorMenu == "general")
            {
                GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundSubHeader);
                EHandleUi.CreateLabelField("<b>GENERAL</b>", LibraryGUIStyle.textSubHeader, true);
                GUILayout.EndHorizontal();

                //Position
                EHandleUi.CreateXYZInputFields("Position", segment.GetPosition(Segment.ControlHandle.ANCHOR), ref containerXPos, ref containerYPos, ref containerZPos, (position, dif) =>
                {
                    EHandleSelection.UpdateSelectedAnchorsRecordUndo(spline, (selected) =>
                    {
                        selected.TranslateAnchor(dif);

                        EHandleSegment.LinkMovement(selected);
                    }, "Moved control handle (anchor): " + spline.selectedControlPoint);

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
            }
            #endregion

            #region deformation
            else if (spline.selectedAnchorMenu == "deformation")
            {
                GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundSubHeader);
                EHandleUi.CreateLabelField("<b>DEFORMATION</b>", LibraryGUIStyle.textSubHeader, true);
                GUILayout.EndHorizontal();

                //Scale X
                GUILayout.BeginHorizontal(EHandleUi.GetBackgroundStyle());
                EHandleUi.CreateSliderAndInputField("Scale X: ", segment.scale.x, ref containerScaleX, (float newValue) =>
                {
                    EHandleSelection.UpdateSelectedAnchorsRecordUndo(spline, (selected) =>
                    {
                        selected.scale.x = newValue;
                    }, "Changed scale x");
                }, 0, 10, 90, 50, 0, true);
                //Default
                EHandleUi.CreateButton(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.iconDefault, 20, 18, () =>
                {
                    GUI.FocusControl(null);
                    EHandleSelection.UpdateSelectedAnchorsRecordUndo(spline, (selected) => { selected.scale.x = Segment.defaultScale; }, "Assigned default value");
                }, segment.scale.x != Segment.defaultScale);
                GUILayout.EndHorizontal();

                //Scale Y
                GUILayout.BeginHorizontal(EHandleUi.GetBackgroundStyle());
                EHandleUi.CreateSliderAndInputField("Scale Y: ", segment.scale.y, ref containerScaleY, (float newValue) =>
                {
                    EHandleSelection.UpdateSelectedAnchorsRecordUndo(spline, (selected) =>
                    {
                        selected.scale.y = newValue;
                    }, "Changed scale y");
                }, 0, 10, 90, 50, 0, true);
                //Default
                EHandleUi.CreateButton(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.iconDefault, 20, 18, () =>
                {
                    GUI.FocusControl(null);
                    EHandleSelection.UpdateSelectedAnchorsRecordUndo(spline, (selected) => { selected.scale.y = Segment.defaultScale; }, "Assigned default value");
                }, segment.scale.y != Segment.defaultScale);
                GUILayout.EndHorizontal();

                //Z rotation
                GUILayout.BeginHorizontal(EHandleUi.GetBackgroundStyle());
                EHandleUi.CreateSliderAndInputField("Rotation: ", segment.zRotation, ref containerZRot, (float newValue) =>
                {
                    EHandleSelection.UpdateSelectedAnchorsRecordUndo(spline, (selected) =>
                    {
                        selected.zRotation = newValue;
                    }, "Changed rotation");
                }, -180, 180, 90, 50, 0, true);
                //Default
                EHandleUi.CreateButton(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.iconDefault, 20, 18, () =>
                {
                    GUI.FocusControl(null);
                    EHandleSelection.UpdateSelectedAnchorsRecordUndo(spline, (selected) => { selected.zRotation = Segment.defaultZRotation; }, "Assigned default value");
                }, segment.zRotation != Segment.defaultZRotation);
                GUILayout.EndHorizontal();

                if (spline.loop && spline.normalType == Spline.NormalType.DYNAMIC && spline.selectedControlPoint == 1000)
                {
                    //Z rotation loop alignment
                    GUILayout.BeginHorizontal(EHandleUi.GetBackgroundStyle());
                    EHandleUi.CreateSliderAndInputField("Rotation alignment: ", spline.segments[spline.segments.Count - 1].zRotation, ref containerZRotAlignment, (float newValue) =>
                    {
                        EHandleUndo.RecordNow(spline, "Changed rotation alignment");
                        spline.segments[spline.segments.Count - 1].zRotation = newValue;
                    }, -180, 180, 90, 50, 0, true);
                    //Default
                    EHandleUi.CreateButton(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.iconDefault, 20, 18, () =>
                    {
                        GUI.FocusControl(null);
                        EHandleUndo.RecordNow(spline, "Assigned default value");
                        spline.segments[spline.segments.Count - 1].zRotation = Segment.defaultZRotation;
                    }, spline.segments[spline.segments.Count - 1].zRotation != Segment.defaultZRotation);
                    GUILayout.EndHorizontal();
                }

                //Saddle skew X
                GUILayout.BeginHorizontal(EHandleUi.GetBackgroundStyle());
                EHandleUi.CreateSliderAndInputField("Saddle skew X: ", segment.saddleSkew.x, ref containerSadleSkewX, (float newValue) =>
                {
                    EHandleSelection.UpdateSelectedAnchorsRecordUndo(spline, (selected) =>
                    {
                        selected.saddleSkew.x = newValue;
                    }, "Changed saddle skew x");
                }, -5, 5, 90, 50, 0, true);
                //Default
                EHandleUi.CreateButton(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.iconDefault, 20, 18, () =>
                {
                    GUI.FocusControl(null);
                    EHandleSelection.UpdateSelectedAnchorsRecordUndo(spline, (selected) => { selected.saddleSkew.x = Segment.defaultSaddleSkewX; }, "Assigned default value");
                }, segment.saddleSkew.x != Segment.defaultSaddleSkewX);
                GUILayout.EndHorizontal();

                //Saddle skew Y
                GUILayout.BeginHorizontal(EHandleUi.GetBackgroundStyle());
                EHandleUi.CreateSliderAndInputField("Saddle skew Y: ", segment.saddleSkew.y, ref containerSadleSkewY, (float newValue) =>
                {
                    EHandleSelection.UpdateSelectedAnchorsRecordUndo(spline, (selected) =>
                    {
                        selected.saddleSkew.y = newValue;
                    }, "Changed saddle skew y");
                }, -5, 5, 90, 50, 0, true);
                //Default
                EHandleUi.CreateButton(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.iconDefault, 20, 18, () =>
                {
                    GUI.FocusControl(null);
                    EHandleSelection.UpdateSelectedAnchorsRecordUndo(spline, (selected) => { selected.saddleSkew.y = Segment.defaultSaddleSkewY; }, "Assigned default value");
                }, segment.saddleSkew.y != Segment.defaultSaddleSkewY);
                GUILayout.EndHorizontal();

                //NoiseLayer
                GUILayout.BeginHorizontal(EHandleUi.GetBackgroundStyle());
                EHandleUi.CreateSliderAndInputField("Noise: ", segment.noise, ref containerNoise, (float newValue) =>
                {
                    EHandleSelection.UpdateSelectedAnchorsRecordUndo(spline, (selected) =>
                    {
                        selected.SetNoise(newValue);
                    }, "Changed noise");
                }, 0, 1, 90, 50, 0, true);
                //Default
                EHandleUi.CreateButton(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.iconDefault, 20, 18, () =>
                {
                    GUI.FocusControl(null);
                    EHandleSelection.UpdateSelectedAnchorsRecordUndo(spline, (selected) => { selected.noise = Segment.defaultNoise; }, "Assigned default value");
                }, segment.noise != Segment.defaultNoise);
                GUILayout.EndHorizontal();

                //Contrast
                GUILayout.BeginHorizontal(EHandleUi.GetBackgroundStyle());
                EHandleUi.CreateSliderAndInputField("Contrast: ", segment.contrast, ref containerContrast, (float newValue) =>
                {
                    EHandleSelection.UpdateSelectedAnchorsRecordUndo(spline, (selected) =>
                    {
                        selected.SetContrast(newValue);
                    }, "Changed contrast");
                }, -20, 20, 90, 50, 0, true);
                //Default
                EHandleUi.CreateButton(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.iconDefault, 20, 18, () =>
                {
                    GUI.FocusControl(null);
                    EHandleSelection.UpdateSelectedAnchorsRecordUndo(spline, (selected) => { selected.contrast = Segment.defaultContrast; }, "Assigned default value");
                }, segment.contrast != Segment.defaultContrast);
                GUILayout.EndHorizontal();
            }
            #endregion
            else
            {
                bool foundAddon = false;

                for (int i = 0; i < addonsDrawWindow.Count; i++)
                {
                    if (addonsDrawWindow[i].Item1 == spline.selectedAnchorMenu)
                    {
                        foundAddon = true;
                        addonsDrawWindow[i].Item2.Invoke(spline, segmentIndex);
                    }
                }

                if (foundAddon == false)
                    spline.selectedAnchorMenu = "general";
            }

            #region bottom
            //Bottom
            GUILayout.Box("", LibraryGUIStyle.lineGrey, GUILayout.ExpandWidth(true));

            GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundBottomMenu);

            //GEneral
            EHandleUi.CreateButtonToggle(EHandleUi.ButtonType.SUB_MENU, LibraryGUIContent.iconGeneral, LibraryGUIContent.iconGeneralActive, 35, 18, () => 
            {
                EHandleUndo.RecordNow(spline, "Changed anchor menu");
                spline.selectedAnchorMenu = "general";
            }, spline.selectedAnchorMenu == "general");

            //Deformation
            EHandleUi.CreateButtonToggle(EHandleUi.ButtonType.SUB_MENU, LibraryGUIContent.iconSpline, LibraryGUIContent.iconSplineActive, 35, 18, () =>
            {
                EHandleUndo.RecordNow(spline, "Changed anchor menu");
                spline.selectedAnchorMenu = "deformation";
            }, spline.selectedAnchorMenu == "deformation");

            //Addon buttons
            for (int i = 0; i < addonsButtons.Count; i++)
            {
                EHandleUi.CreateButtonToggle(EHandleUi.ButtonType.SUB_MENU, addonsButtons[i].Item2, addonsButtons[i].Item3, 35, 18, () =>
                {
                    EHandleUndo.RecordNow(spline, "Changed sub menu");
                    spline.selectedAnchorMenu = addonsButtons[i].Item1;
                }, spline.selectedAnchorMenu == addonsButtons[i].Item1);
            }

            GUILayout.EndHorizontal();
            GUILayout.Box("", LibraryGUIStyle.lineGrey, GUILayout.ExpandWidth(true));
            #endregion
        }

        public static Vector2 GetSize()
        {
            return new Vector2(LibraryGUIStyle.menuItemHeight * 5 + 2, 150);
        }

        public static void UpdateRect(Spline spline)
        {
            Rect generalWindowRect = MenuGeneral.GetRect();
            Rect splineWindowRect = MenuSpline.GetRect();

            if (spline.selectedAnchorMenu == "general")
                anchorWindowRect.height = LibraryGUIStyle.menuItemHeight * 6 + 16;
            if (spline.selectedAnchorMenu == "deformation")
            {
                anchorWindowRect.height = LibraryGUIStyle.menuItemHeight * 10 + 16;

                if(spline.loop && spline.normalType == Spline.NormalType.DYNAMIC && spline.selectedControlPoint == 1000)
                    anchorWindowRect.height += LibraryGUIStyle.menuItemHeight;
            }

            //Addons
            for (int i = 0; i < addonsCalcWindowSize.Count; i++)
            {
                if (spline.selectedAnchorMenu == addonsCalcWindowSize[i].Item1)
                {
                    Rect rect = addonsCalcWindowSize[i].Item2.Invoke(spline);
                    anchorWindowRect.height = rect.height;
                    anchorWindowRect.width = rect.width;
                    break;
                }
            }

            if (spline.selectedAnchorMenu == "instantiate")
                anchorWindowRect.height = LibraryGUIStyle.menuItemHeight * 8 + 16;

            anchorWindowRect.width = 0;

            anchorWindowRect.x = (generalWindowRect.x + generalWindowRect.width + 3) + (splineWindowRect.width + 3);
            anchorWindowRect.y = generalWindowRect.y - (anchorWindowRect.height - generalWindowRect.height);
        }

        public static void AddSubMenu(string id, GUIContent button, GUIContent buttonActive, Action<Spline, int> DrawWindow, Func<Spline, Rect> calcWindowSize)
        {
            for (int i = 0; i < addonsDrawWindow.Count; i++)
            {
                if (addonsDrawWindow[i].Item1 == id)
                {
                    addonsDrawWindow.RemoveAt(i);
                    addonsButtons.RemoveAt(i);
                    addonsCalcWindowSize.RemoveAt(i);
                }
            }

            addonsDrawWindow.Add((id, DrawWindow));
            addonsButtons.Add((id, button, buttonActive));
            addonsCalcWindowSize.Add((id, calcWindowSize));
        }
    }
}
