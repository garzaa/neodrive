// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: WindowExtended.cs
//
// Author: Mikael Danielsson
// Date Created: 05-08-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using SplineArchitect.Libraries;
using SplineArchitect.Objects;
using SplineArchitect.Utility;

namespace SplineArchitect.Ui
{
    public class WindowExtended : WindowBase
    {
        //General
        private string windowTitle = "";
        private static List<Segment> segmentContainer = new List<Segment>();

        //Spline object
        const int maxSplineObjectNameLength = 25;
        private static GUIContent guiContentContainer = new GUIContent("SplineObject");
        private static string[] typeOptions = new string[] { "Deformation", "Follower", "None" };
        private static string[] normalsOption = new string[] { "Default", "Seamless", "Don't calculate" };
        private static string[] tangentsOptions = new string[] { "Default", "Don't calculate" };
        private static string[] componentModeOptions = new string[] { "Remove from build", "Inactive", "Active" };

        //Control point
        private static string[] interpolationMode = new string[] { "Spline", "Line" };

        //Addons
        private static List<(string, Action<Spline, int>)> addonsDrawWindowCp = new();
        private static List<(string, GUIContent, GUIContent)> addonsButtonsCp = new();
        private static List<(string, Func<Spline, Rect>)> addonsCalcWindowSizeCp = new();

        private static List<(string, Action<Spline, SplineObject>)> addonsDrawWindowSo = new();
        private static List<(string, GUIContent, GUIContent)> addonsButtonsSo = new();
        private static List<(string, Func<Spline, SplineObject, Rect>)> addonsCalcWindowSizeSo = new();

        protected override void OnGUIExtended()
        {
            Spline spline = EHandleSelection.selectedSpline;

            if (spline == null)
                return;

            if (GlobalSettings.GetExtendedWindowMinimized())
            {
                //Maximize
                EUiUtility.CreateButton(ButtonType.SUB_MENU, LibraryGUIContent.iconMaximize, 25, 25, () =>
                {
                    EActionToSceneGUI.Add(() =>
                    {
                        GlobalSettings.SetExtendedWindowMinimized(false);
                    }, EActionToSceneGUI.Type.LATE, EventType.Repaint);
                    EHandleSceneView.RepaintCurrent();
                });

                return;
            }

            SplineObject so = EHandleSelection.selectedSplineObject;
            int segmentIndex = SplineUtility.ControlPointIdToSegmentIndex(spline.selectedControlPoint);

            if (so != null)
            {
                OnGUISplineObject(spline, so);
            }
            else if(segmentIndex >= 0 && spline.segments.Count > 0 && segmentIndex < spline.segments.Count)
            {
                OnGUIControlPoint(spline, spline.segments[segmentIndex], segmentIndex, SplineUtility.GetControlPointType(spline.selectedControlPoint));
            }
        }

        private void OnGUISplineObject(Spline spline, SplineObject so)
        {
            #region top
            bool isDeformation = so.type == SplineObject.Type.DEFORMATION;
            bool isFollower = so.type == SplineObject.Type.FOLLOWER;
            bool isNone = so.type == SplineObject.Type.NONE;

            EUiUtility.ResetGetBackgroundStyleId();

            //Title
            GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundHeader);
            //Error, no read/write access
            if (!EHandleSplineObject.HasReadWriteAccessEnabled(so) && so.type == SplineObject.Type.DEFORMATION) EUiUtility.CreateErrorWarningMessageIcon(LibraryGUIContent.warningMsgCantDoRealtimeDeformation);
            windowTitle = so.name;
            if (windowTitle.Length > maxSplineObjectNameLength) windowTitle = $"{windowTitle.Substring(0, maxSplineObjectNameLength)}..";
            if (EHandleSelection.selectedSplineObjects.Count > 0) windowTitle = $"{windowTitle} + ({EHandleSelection.selectedSplineObjects.Count})";
            EUiUtility.CreateLabelField($"<b>{windowTitle}</b>", LibraryGUIStyle.textHeaderBlack, true);

            GUILayout.FlexibleSpace();
            //Minimize
            EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.iconMinimize, 19, 14, () =>
            {
                EActionToSceneGUI.Add(() =>
                {
                    GlobalSettings.SetExtendedWindowMinimized(true);
                }, EActionToSceneGUI.Type.LATE, EventType.Repaint);
                EHandleSceneView.RepaintCurrent();
            });
            GUILayout.EndHorizontal();

            EUiUtility.CreateHorizontalBlackLine();
            GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundToolbar);

            //Snap
            bool enableSnap = isDeformation && so.meshContainers != null && so.meshContainers.Count > 0;
            EUiUtility.CreateButtonToggle(ButtonType.DEFAULT, LibraryGUIContent.iconMagnet,
                                         so.snapMode == SplineObject.SnapMode.SPLINE_OBJECTS ? LibraryGUIContent.iconMagnetActive2 : LibraryGUIContent.iconMagnetActive, 35, 19, () =>
                                         {
                                             EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                                             {
                                                 if (selected.snapMode == SplineObject.SnapMode.NONE) selected.snapMode = SplineObject.SnapMode.CONTROL_POINTS;
                                                 else if (selected.snapMode == SplineObject.SnapMode.CONTROL_POINTS) selected.snapMode = SplineObject.SnapMode.SPLINE_OBJECTS;
                                                 else if (selected.snapMode == SplineObject.SnapMode.SPLINE_OBJECTS) selected.snapMode = SplineObject.SnapMode.NONE;
                                             }, "Toggle snap deformation");
                                         }, so.snapMode > 0 && enableSnap, isDeformation && so.meshContainers != null && so.meshContainers.Count > 0);

            //Mirror
            EUiUtility.CreateButtonToggle(ButtonType.DEFAULT, LibraryGUIContent.iconMirrorDeformation, LibraryGUIContent.iconMirrorDeformationActive, 35, 19, () =>
            {
                EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                {
                    selected.mirrorDeformation = !selected.mirrorDeformation;
                }, "Toggle mirror deformation");
            }, so.mirrorDeformation, isDeformation && so.meshContainers.Count > 0);

            EUiUtility.CreateSeparator();

            //Objects to spline center
            EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.iconToCenter, 35, 19, () =>
            {
                EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                {
                    EHandleSplineObject.ToSplineCenter(selected);
                }, "Object to spline center");

                EActionToSceneGUI.Add(() =>
                {
                    EHandleTool.UpdateOrientationForPositionTool(EHandleSceneView.GetCurrent(), spline);
                }, EActionToSceneGUI.Type.LATE, EventType.Layout);
            }, isFollower || isDeformation);

            //Export mesh
            EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.iconExport, 35, 19, () =>
            {
                string path = EditorUtility.SaveFilePanelInProject("Export mesh", $"{so.name}", "asset", "Assets/");
                int count = 0;

                if (!string.IsNullOrEmpty(path))
                {
                    //For some very odd reason I need to do this. Likely becouse of EditorUtility.SaveFilePanelInProject.
                    EHandleSelection.stopUpdateSelection = true;
                    EHandleSelection.UpdatedSelectedSplineObjects((selected) =>
                    {
                        string[] data = path.Split('/');

                        path = "";
                        for (int i = 0; i < data.Length - 1; i++)
                            path += $"{data[i]}/";

                        if (count == 0) EHandleSplineObject.ExportMeshes(selected, $"{path}{so.name}");
                        else EHandleSplineObject.ExportMeshes(selected, $"{path}{so.name}{count}");
                        count++;
                    });
                    EHandleSelection.stopUpdateSelection = false;
                }
            });

            GUILayout.EndHorizontal();
            EUiUtility.CreateHorizontalBlackLine();
            #endregion

            #region general
            if (spline.selectedObjectMenu == "general")
            {
                //Sub title
                GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundSubHeader);
                EUiUtility.CreateLabelField("<b>GENERAL</b>", LibraryGUIStyle.textSubHeader, true);
                GUILayout.EndHorizontal();

                //Position
                GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
                EUiUtility.CreateXYZInputFields("Position", so.localSplinePosition, (position, dif) =>
                {
                    EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                    {
                        selected.localSplinePosition -= dif;
                        selected.activationPosition = so.localSplinePosition;
                    }, "Updated position");

                    EActionToSceneGUI.Add(() =>
                    {
                        EHandleTool.UpdateOrientationForPositionTool(EHandleSceneView.GetCurrent(), spline);
                    }, EActionToSceneGUI.Type.LATE, EventType.Layout);
                }, 90, 10, 62, isNone);
                GUILayout.EndHorizontal();

                //Rotation
                bool rotFieldDisabled = so.followAxels.x == 0 || so.followAxels.y == 0 || so.followAxels.z == 0 || isNone;
                Vector3 localSplineRotation = so.localSplineRotation.eulerAngles;
                if (rotFieldDisabled) localSplineRotation = spline.WorldRotationToSplineRotation(so.transform.rotation, so.localSplinePosition.z / spline.length).eulerAngles;
                GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
                EUiUtility.CreateXYZInputFields("Rotation", localSplineRotation, (euler, dif) =>
                {
                    EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                    {
                        selected.localSplineRotation.eulerAngles = euler;
                    }, "Updated rotation");
                }, 90, 10, 62, rotFieldDisabled);
                GUILayout.EndHorizontal();

                if (so.snapMode > 0 && enableSnap)
                {
                    GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
                    EUiUtility.CreateLabelField("Snap length:", LibraryGUIStyle.textDefault, true, 97);

                    //Snap length start
                    EUiUtility.CreateFloatFieldWithLabel("Start", so.snapLengthStart, (newValue) => {
                        //Update selected an record undo
                        EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                        {
                            selected.snapLengthStart = newValue;
                        }, "Set snap length start");
                    }, 50, 38, true);

                    EUiUtility.CreateEmpty(0);

                    //Snap length end
                    EUiUtility.CreateFloatFieldWithLabel("End", so.snapLengthEnd, (newValue) => {
                        //Update selected an record undo
                        EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                        {
                            selected.snapLengthEnd = newValue;
                        }, "Set snap length end");
                    }, 50, 34, true);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
                    GUILayout.Space(2);
                    EUiUtility.CreateLabelField("Snap offset:", LibraryGUIStyle.textDefault, true, 95);

                    //Snap length start
                    EUiUtility.CreateFloatFieldWithLabel("Start", so.snapOffsetStart, (newValue) => {
                        //Update selected an record undo
                        EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                        {
                            selected.snapOffsetStart = newValue;
                        }, "Set snap offset start");
                    }, 50, 38, true);

                    EUiUtility.CreateEmpty(0);

                    //Snap length end
                    EUiUtility.CreateFloatFieldWithLabel("End", so.snapOffsetEnd, (newValue) => {
                        //Update selected an record undo
                        EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                        {
                            selected.snapOffsetEnd = newValue;
                        }, "Set snap offset end");
                    }, 50, 34, true);
                    GUILayout.EndHorizontal();
                }

                if (isFollower)
                {
                    //Follow rotation
                    EUiUtility.CreateToggleXYZField("Follow rotation: ", so.followAxels, (Vector3Int newValue) => {

                        EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                        {
                            selected.followAxels = newValue;

                            if (selected.followAxels == Vector3.one)
                            {
                                selected.localSplineRotation = spline.WorldRotationToSplineRotation(selected.transform.rotation, selected.localSplinePosition.z / spline.length);
                            }
                        }, "Update follower axels");
                    });
                }

                if (isDeformation)
                {
                    //Normals
                    GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
                    EUiUtility.CreatePopupField("Normals: ", 70, (int)so.normalType, normalsOption, (int newValue) =>
                    {
                        EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                        {
                            selected.normalType = (SplineObject.NormalType)newValue;
                        }, "Update normal state");
                    }, 60, true);

                    GUILayout.Space(14);

                    //Tangents
                    EUiUtility.CreatePopupField("Tangents: ", 70, (int)so.tangentType, tangentsOptions, (int newValue) =>
                    {
                        EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                        {
                            selected.tangentType = (SplineObject.TangentType)newValue;
                        }, "Update tangent state");
                    }, 66, true);
                    GUILayout.EndHorizontal();
                }

                GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
                if (isFollower)
                {
                    //World position mode
                    EUiUtility.CreateLabelField("Lock position: ", LibraryGUIStyle.textDefault, true, 88);
                    EUiUtility.CreateCheckbox(so.lockPosition, (bool newValue) => {

                        EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                        {
                            selected.lockPosition = newValue;
                        }, "Toggled lock position");
                    });
                }

                GUILayout.FlexibleSpace();
                if (EHandlePrefab.IsPrefabStageActive() && spline.componentMode == ComponentMode.INACTIVE)
                {
                    EUiUtility.CreatePopupField("Component:", 70, 1, componentModeOptions, (int newValue) =>
                    {
                    }, 80, true, false);
                }
                else
                {
                    //Component
                    if (spline.deformationMode == DeformationMode.GENERATE &&
                        so.componentMode == ComponentMode.REMOVE_FROM_BUILD &&
                        so.type == SplineObject.Type.DEFORMATION &&
                        so.meshContainers != null &&
                        so.meshContainers.Count > 0)
                    {
                        EUiUtility.CreateErrorWarningMessageIcon(LibraryGUIContent.errorMsgSplineObjectGenerateDeformationsRuntime);
                    }
                    else if (so.gameObject.isStatic && so.componentMode != ComponentMode.REMOVE_FROM_BUILD)
                    {
                        EUiUtility.CreateErrorWarningMessageIcon(LibraryGUIContent.errorMsgStaticSplineObject);
                    }
                    else EUiUtility.CreateInfoMessageIcon(LibraryGUIContent.infoMsgACOComponentMode);
                    EUiUtility.CreatePopupField("Component:", 70, (int)so.componentMode, componentModeOptions, (int newValue) =>
                    {
                        EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                        {
                            selected.componentMode = (ComponentMode)newValue;

                            //Set component mode for whole hierarchy
                            SplineObject firstSo = selected;
                            for (int i = 0; i < 25; i++)
                            {
                                if (firstSo.soParent == null)
                                    break;

                                firstSo = firstSo.soParent;
                            }

                            EHandleUndo.RecordNow(firstSo);
                            firstSo.componentMode = (ComponentMode)newValue;

                            foreach (SplineObject so2 in so.splineParent.splineObjects)
                            {
                                if (firstSo.IsParentTo(so2))
                                {
                                    EHandleUndo.RecordNow(so2);
                                    so2.componentMode = (ComponentMode)newValue;
                                }
                            }
                        }, "Change component mode");

                        EHandleSpline.MarkForInfoUpdate(spline);
                    }, 80, true, spline.componentMode == ComponentMode.ACTIVE);
                }

                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());

                EUiUtility.CreateEmpty(13);
                EUiUtility.CreateToggleField("Auto type: ", so.autoType, (value) =>
                {
                    EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                    {
                        selected.autoType = value;
                    }, "Toggled auto type");
                }, so.type != SplineObject.Type.NONE, true, 67);

                //Type
                EUiUtility.CreatePopupField("Type:", 70, (int)so.type, typeOptions, (int newValue) =>
                {
                    SplineObject.Type state = (SplineObject.Type)newValue;

                    EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                    {
                        selected.type = state;
                        EditorUtility.SetDirty(selected);
                    }, "Update type");

                    EHandleSpline.MarkForInfoUpdate(spline);
                }, -1, true, !so.autoType);
                GUILayout.EndHorizontal();
            }
            #endregion

            #region info
            else if (spline.selectedObjectMenu == "info")
            {
                GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundSubHeader);
                EUiUtility.CreateLabelField("<b>INFO</b>", LibraryGUIStyle.textSubHeader, true);
                GUILayout.EndHorizontal();

                //Vertecies label
                int vertecies = so.deformedVertecies;
                int deformations = so.deformations;
                List<SplineObject> selection = EHandleSelection.selectedSplineObjects;
                if (selection.Count > 0)
                {
                    foreach (SplineObject oc2 in selection)
                    {
                        vertecies += oc2.deformedVertecies;
                        deformations += oc2.deformations;
                    }
                }
                EUiUtility.CreateLabelField("Vertecies: " + vertecies, LibraryGUIStyle.textDefault);

                //Deformations label
                EUiUtility.CreateLabelField("Deformations: " + deformations, LibraryGUIStyle.textDefault);
            }
            #endregion

            #region addons
            else
            {
                bool foundAddon = false;

                for (int i = 0; i < addonsDrawWindowSo.Count; i++)
                {
                    if (addonsDrawWindowSo[i].Item1 == spline.selectedObjectMenu)
                    {
                        foundAddon = true;
                        addonsDrawWindowSo[i].Item2.Invoke(spline, so);
                    }
                }

                if (foundAddon == false)
                    spline.selectedObjectMenu = "general";
            }
            #endregion

            #region bottom
            EUiUtility.CreateHorizontalBlackLine();

            GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundBottomMenu);

            //General
            EUiUtility.CreateButtonToggle(ButtonType.SUB_MENU, LibraryGUIContent.iconGeneral, LibraryGUIContent.iconGeneralActive, 35, 18, () =>
            {
                EHandleUndo.RecordNow(spline, "Changed object menu");
                spline.selectedObjectMenu = "general";
            }, spline.selectedObjectMenu == "general");

            //Addons
            for (int i = 0; i < addonsButtonsSo.Count; i++)
            {
                EUiUtility.CreateButtonToggle(ButtonType.SUB_MENU, addonsButtonsSo[i].Item2, addonsButtonsSo[i].Item3, 35, 18, () =>
                {
                    EHandleUndo.RecordNow(spline, "Changed sub menu");
                    spline.selectedObjectMenu = addonsButtonsSo[i].Item1;
                }, spline.selectedObjectMenu == addonsButtonsSo[i].Item1);
            }

            //Info
            EUiUtility.CreateButtonToggle(ButtonType.SUB_MENU, LibraryGUIContent.iconInfo, LibraryGUIContent.iconInfoActive, 35, 18, () =>
            {
                EHandleUndo.RecordNow(spline, "Changed object menu");
                spline.selectedObjectMenu = "info";
            }, spline.selectedObjectMenu == "info");

            GUILayout.EndHorizontal();
            EUiUtility.CreateHorizontalBlackLine();
            #endregion
        }

        private void OnGUIControlPoint(Spline spline, Segment segment, int segmentIndex, Segment.ControlHandle handle)
        {
            EUiUtility.ResetGetBackgroundStyleId();

            #region Top
            //Header
            string selectionCountText = spline.selectedAnchors.Count > 0 ? $" + ({spline.selectedAnchors.Count})" : "";
            if (handle == Segment.ControlHandle.TANGENT_A) windowTitle = $"Tangent A {segmentIndex + 1}";
            else if (handle == Segment.ControlHandle.TANGENT_B) windowTitle = $"Tangent B {segmentIndex + 1}";
            else windowTitle = $"Anchor {segmentIndex + 1} {selectionCountText}";
            GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundHeader);
            EUiUtility.CreateLabelField($"<b>{windowTitle}</b>", LibraryGUIStyle.textHeaderBlack, true);

            GUILayout.FlexibleSpace();
            //Minimize
            EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.iconMinimize, 19, 14, () =>
            {
                EActionToSceneGUI.Add(() =>
                {
                    GlobalSettings.SetExtendedWindowMinimized(true);
                }, EActionToSceneGUI.Type.LATE, EventType.Repaint);
                EHandleSceneView.RepaintCurrent();
            });
            GUILayout.EndHorizontal();

            EUiUtility.CreateHorizontalBlackLine();
            GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundToolbar);
            //Unlink
            if (segment.linkTarget != Segment.LinkTarget.NONE)
            {
                EUiUtility.CreateButton(ButtonType.DEFAULT_ACTIVE, LibraryGUIContent.iconUnlink, 35, 19, () =>
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
                    EHandleSceneView.RepaintCurrent();
                });
            }
            //Link
            else
            {
                EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.iconLink, 35, 19, () =>
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
                        SplineConnector closestConnector = SplineConnectorUtility.GetClosest(anchorPoint, HandleRegistry.GetSplineConnectors());
                        Vector3 linkPointConnector = new Vector3(99999, 99999, 99999);
                        if (closestConnector != null) linkPointConnector = closestConnector.transform.position;
                        float connectorDistance = Vector3.Distance(anchorPoint, linkPointConnector) - 0.01f;

                        if (connectorDistance > anchorDistance)
                        {
                            s.SetAnchorPosition(linkPointAnchor);
                            s.linkTarget = Segment.LinkTarget.ANCHOR;

                            if (spline.loop && spline.segments[0] == s)
                                spline.segments[spline.segments.Count - 1].SetPosition(Segment.ControlHandle.ANCHOR, linkPointAnchor);

                            SplineUtility.GetSegmentsAtPointNoAlloc(segmentContainer, HandleRegistry.GetSplines(), linkPointAnchor);
                            foreach (Segment s2 in segmentContainer)
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
                    EHandleSceneView.RepaintCurrent();
                });
            }

            if (segment.linkTarget == Segment.LinkTarget.SPLINE_CONNECTOR)
            {
                //Mirror connector
                EUiUtility.CreateButtonToggle(ButtonType.DEFAULT, LibraryGUIContent.iconMirrorConnector, LibraryGUIContent.iconMirrorConnectorActive, 35, 19, () =>
                {
                    EHandleSelection.UpdateSelectedAnchorsRecordUndo(spline, (selected) =>
                    {
                        selected.mirrorConnector = !segment.mirrorConnector;
                    }, "Mirrored spline connector");
                    EHandleSceneView.RepaintCurrent();
                }, segment.mirrorConnector);
            }

            EUiUtility.CreateSeparator();

            //Align tangents
            EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.iconAlignTangents, 35, 19, () =>
            {
                EHandleSelection.UpdateSelectedAnchors(spline, (seg) =>
                {
                    if (seg.links.Count == 0)
                        return;

                    segmentContainer.Clear();
                    segmentContainer.Add(seg);

                    foreach (Segment s2 in seg.links)
                    {
                        EHandleUndo.RecordNow(s2.splineParent, "Aligned tangents");

                        if (s2 == seg)
                            continue;

                        segmentContainer.Add(s2);
                    }

                    EHandleSegment.AlignTangents(segmentContainer);
                });

                EHandleSceneView.RepaintCurrent();
            }, segment.linkTarget == Segment.LinkTarget.ANCHOR);

            //Flatten
            EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.iconFlatten, 35, 19, () =>
            {
                EHandleSelection.UpdateSelectedAnchorsRecordUndo(spline, (selected) =>
                {
                    EHandleSpline.FlattenControlPoints(spline, selected);
                }, "Flatten control points");

                EHandleTool.ActivatePositionToolForControlPoint(spline);
                EHandleSceneView.RepaintCurrent();
            });

            //Split
            int selectedSegmentId = SplineUtility.ControlPointIdToSegmentIndex(spline.selectedControlPoint);
            EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.iconSplit, 35, 19, () =>
            {
                EHandleSpline.Split(spline, selectedSegmentId);
                EHandleSceneView.RepaintCurrent();
            }, !spline.loop && selectedSegmentId > 0 && selectedSegmentId < (spline.segments.Count - 1));

            //Next control point
            EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.iconNextControlPoint, 35, 19, () =>
            {
                EHandleUndo.RecordNow(spline, "Next control point");
                EHandleSelection.SelectPrimaryControlPoint(spline, EHandleSpline.GetNextControlPoint(spline));

                EHandleTool.ActivatePositionToolForControlPoint(spline);
                EHandleSceneView.RepaintCurrent();
            });

            //Prev control point
            EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.iconPrevControlPoint, 35, 19, () =>
            {
                EHandleUndo.RecordNow(spline, "Prev control point");
                EHandleSelection.SelectPrimaryControlPoint(spline, EHandleSpline.GetNextControlPoint(spline, true));

                EHandleTool.ActivatePositionToolForControlPoint(spline);
                EHandleSceneView.RepaintCurrent();
            });
            GUILayout.EndHorizontal();
            EUiUtility.CreateHorizontalBlackLine();
            #endregion

            #region general
            if (spline.selectedAnchorMenu == "general")
            {
                GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundSubHeader);
                EUiUtility.CreateLabelField("<b>GENERAL</b>", LibraryGUIStyle.textSubHeader, true);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
                EUiUtility.CreateXYZInputFields("Position", segment.GetPosition(handle), (position, dif) =>
                {
                    EHandleSelection.UpdateSelectedAnchorsRecordUndo(spline, (selected) =>
                    {
                        if (handle == Segment.ControlHandle.TANGENT_A || handle == Segment.ControlHandle.TANGENT_B)
                        {
                            if (GlobalSettings.GetHandleType() == HandleType.CONTINUOUS)
                            {
                                segment.SetContinuousPosition(handle, position);
                            }
                            else if (GlobalSettings.GetHandleType() == HandleType.MIRRORED)
                            {
                                segment.SetMirroredPosition(handle, position);
                            }
                        }
                        else
                        {
                            selected.TranslateAnchor(dif);
                            EHandleSegment.LinkMovement(selected);
                        }

                    }, "Moved control handle: " + spline.selectedControlPoint);

                    EHandleTool.ActivatePositionToolForControlPoint(spline);
                    EHandleSceneView.RepaintCurrent();
                }, 65, 10, 62);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
                EUiUtility.CreateLabelField($"Z position: {Mathf.Round(segment.zPosition * 100) / 100}", LibraryGUIStyle.textDefault, true, 153);
                EUiUtility.CreateLabelField($"Length: {Mathf.Round(segment.length * 100) / 100}", LibraryGUIStyle.textDefault, true);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
                //Type
                GUILayout.FlexibleSpace();
                EUiUtility.CreatePopupField("Type:", 70, (int)segment.GetInterpolationType(), interpolationMode, (int newValue) =>
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
                EUiUtility.CreateLabelField("<b>DEFORMATION</b>", LibraryGUIStyle.textSubHeader, true);
                GUILayout.EndHorizontal();

                //Scale X
                GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
                EUiUtility.CreateSliderAndInputField("Scale X: ", segment.scale.x, (float newValue) =>
                {
                    EHandleSelection.UpdateSelectedAnchorsRecordUndo(spline, (selected) =>
                    {
                        selected.scale.x = newValue;
                    }, "Changed scale x");
                }, 0, 10, 90, 50, 0, true);
                //Default
                EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.iconDefault, 20, 18, () =>
                {
                    GUI.FocusControl(null);
                    EHandleSelection.UpdateSelectedAnchorsRecordUndo(spline, (selected) => { selected.scale.x = Segment.defaultScale; }, "Assigned default value");
                }, segment.scale.x != Segment.defaultScale);
                GUILayout.EndHorizontal();

                //Scale Y
                GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
                EUiUtility.CreateSliderAndInputField("Scale Y: ", segment.scale.y, (float newValue) =>
                {
                    EHandleSelection.UpdateSelectedAnchorsRecordUndo(spline, (selected) =>
                    {
                        selected.scale.y = newValue;
                    }, "Changed scale y");
                }, 0, 10, 90, 50, 0, true);
                //Default
                EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.iconDefault, 20, 18, () =>
                {
                    GUI.FocusControl(null);
                    EHandleSelection.UpdateSelectedAnchorsRecordUndo(spline, (selected) => { selected.scale.y = Segment.defaultScale; }, "Assigned default value");
                }, segment.scale.y != Segment.defaultScale);
                GUILayout.EndHorizontal();

                //Z rotation
                GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
                EUiUtility.CreateSliderAndInputField("Rotation: ", segment.zRotation, (float newValue) =>
                {
                    EHandleSelection.UpdateSelectedAnchorsRecordUndo(spline, (selected) =>
                    {
                        selected.zRotation = newValue;
                    }, "Changed rotation");
                }, -180, 180, 90, 50, 0, true);
                //Default
                EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.iconDefault, 20, 18, () =>
                {
                    GUI.FocusControl(null);
                    EHandleSelection.UpdateSelectedAnchorsRecordUndo(spline, (selected) => { selected.zRotation = Segment.defaultZRotation; }, "Assigned default value");
                }, segment.zRotation != Segment.defaultZRotation);
                GUILayout.EndHorizontal();

                if (spline.loop && spline.normalType == Spline.NormalType.DYNAMIC && spline.selectedControlPoint == 1000)
                {
                    //Z rotation loop alignment
                    GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
                    EUiUtility.CreateSliderAndInputField("Rotation alignment: ", spline.segments[spline.segments.Count - 1].zRotation, (float newValue) =>
                    {
                        EHandleUndo.RecordNow(spline, "Changed rotation alignment");
                        spline.segments[spline.segments.Count - 1].zRotation = newValue;
                    }, -180, 180, 90, 50, 0, true);
                    //Default
                    EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.iconDefault, 20, 18, () =>
                    {
                        GUI.FocusControl(null);
                        EHandleUndo.RecordNow(spline, "Assigned default value");
                        spline.segments[spline.segments.Count - 1].zRotation = Segment.defaultZRotation;
                    }, spline.segments[spline.segments.Count - 1].zRotation != Segment.defaultZRotation);
                    GUILayout.EndHorizontal();
                }

                //Saddle skew X
                GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
                EUiUtility.CreateSliderAndInputField("Saddle skew X: ", segment.saddleSkew.x, (float newValue) =>
                {
                    EHandleSelection.UpdateSelectedAnchorsRecordUndo(spline, (selected) =>
                    {
                        selected.saddleSkew.x = newValue;
                    }, "Changed saddle skew x");
                }, -5, 5, 90, 50, 0, true);
                //Default
                EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.iconDefault, 20, 18, () =>
                {
                    GUI.FocusControl(null);
                    EHandleSelection.UpdateSelectedAnchorsRecordUndo(spline, (selected) => { selected.saddleSkew.x = Segment.defaultSaddleSkewX; }, "Assigned default value");
                }, segment.saddleSkew.x != Segment.defaultSaddleSkewX);
                GUILayout.EndHorizontal();

                //Saddle skew Y
                GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
                EUiUtility.CreateSliderAndInputField("Saddle skew Y: ", segment.saddleSkew.y, (float newValue) =>
                {
                    EHandleSelection.UpdateSelectedAnchorsRecordUndo(spline, (selected) =>
                    {
                        selected.saddleSkew.y = newValue;
                    }, "Changed saddle skew y");
                }, -5, 5, 90, 50, 0, true);
                //Default
                EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.iconDefault, 20, 18, () =>
                {
                    GUI.FocusControl(null);
                    EHandleSelection.UpdateSelectedAnchorsRecordUndo(spline, (selected) => { selected.saddleSkew.y = Segment.defaultSaddleSkewY; }, "Assigned default value");
                }, segment.saddleSkew.y != Segment.defaultSaddleSkewY);
                GUILayout.EndHorizontal();

                //NoiseLayer
                GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
                EUiUtility.CreateSliderAndInputField("Noise: ", segment.noise, (float newValue) =>
                {
                    EHandleSelection.UpdateSelectedAnchorsRecordUndo(spline, (selected) =>
                    {
                        selected.SetNoise(newValue);
                    }, "Changed noise");
                }, 0, 1, 90, 50, 0, true);
                //Default
                EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.iconDefault, 20, 18, () =>
                {
                    GUI.FocusControl(null);
                    EHandleSelection.UpdateSelectedAnchorsRecordUndo(spline, (selected) => { selected.noise = Segment.defaultNoise; }, "Assigned default value");
                }, segment.noise != Segment.defaultNoise);
                GUILayout.EndHorizontal();

                //Contrast
                GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
                EUiUtility.CreateSliderAndInputField("Contrast: ", segment.contrast, (float newValue) =>
                {
                    EHandleSelection.UpdateSelectedAnchorsRecordUndo(spline, (selected) =>
                    {
                        selected.SetContrast(newValue);
                    }, "Changed contrast");
                }, -20, 20, 90, 50, 0, true);
                //Default
                EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.iconDefault, 20, 18, () =>
                {
                    GUI.FocusControl(null);
                    EHandleSelection.UpdateSelectedAnchorsRecordUndo(spline, (selected) => { selected.contrast = Segment.defaultContrast; }, "Assigned default value");
                }, segment.contrast != Segment.defaultContrast);
                GUILayout.EndHorizontal();
            }
            #endregion

            #region addons
            else
            {
                bool foundAddon = false;

                for (int i = 0; i < addonsDrawWindowCp.Count; i++)
                {
                    if (addonsDrawWindowCp[i].Item1 == spline.selectedAnchorMenu)
                    {
                        foundAddon = true;
                        addonsDrawWindowCp[i].Item2.Invoke(spline, segmentIndex);
                    }
                }

                if (foundAddon == false)
                    spline.selectedAnchorMenu = "general";
            }
            #endregion

            #region bottom
            //Bottom
            EUiUtility.CreateHorizontalBlackLine();

            GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundBottomMenu);

            //General
            EUiUtility.CreateButtonToggle(ButtonType.SUB_MENU, LibraryGUIContent.iconGeneral, LibraryGUIContent.iconGeneralActive, 35, 18, () =>
            {
                EHandleUndo.RecordNow(spline, "Changed anchor menu");
                spline.selectedAnchorMenu = "general";
            }, spline.selectedAnchorMenu == "general");

            //Deformation
            EUiUtility.CreateButtonToggle(ButtonType.SUB_MENU, LibraryGUIContent.iconCurve, LibraryGUIContent.iconCurveActive, 35, 18, () =>
            {
                EHandleUndo.RecordNow(spline, "Changed anchor menu");
                spline.selectedAnchorMenu = "deformation";
            }, spline.selectedAnchorMenu == "deformation");

            //Addon buttons
            for (int i = 0; i < addonsButtonsCp.Count; i++)
            {
                EUiUtility.CreateButtonToggle(ButtonType.SUB_MENU, addonsButtonsCp[i].Item2, addonsButtonsCp[i].Item3, 35, 18, () =>
                {
                    EHandleUndo.RecordNow(spline, "Changed sub menu");
                    spline.selectedAnchorMenu = addonsButtonsCp[i].Item1;
                }, spline.selectedAnchorMenu == addonsButtonsCp[i].Item1);
            }

            GUILayout.EndHorizontal();
            #endregion
        }

        protected override void UpdateWindowSize()
        {
            Spline spline = EHandleSelection.selectedSpline;

            if (spline == null)
                return;

            SplineObject so = EHandleSelection.selectedSplineObject;

            if (GlobalSettings.GetExtendedWindowMinimized())
            {
                cachedRect.width = 27;
                cachedRect.height = 27;
            }
            else if(so != null)
            {
                if (spline.selectedObjectMenu == "general")
                {
                    if (so.type == SplineObject.Type.NONE)
                        cachedRect.height = menuItemHeight * 6 + 33;
                    else if (so.snapMode > 0 && so.type == SplineObject.Type.DEFORMATION && so.meshContainers != null && so.meshContainers.Count > 0)
                        cachedRect.height = menuItemHeight * 9 + 33;
                    else
                        cachedRect.height = menuItemHeight * 7 + 33;

                    cachedRect.width = 290;
                }
                else if (spline.selectedObjectMenu == "info")
                {
                    cachedRect.height = menuItemHeight * 4 + 33;
                    cachedRect.width = 158;
                }
                //Addons
                else
                {
                    for (int i = 0; i < addonsCalcWindowSizeSo.Count; i++)
                    {
                        if (spline.selectedObjectMenu == addonsCalcWindowSizeSo[i].Item1)
                        {
                            Rect rect = addonsCalcWindowSizeSo[i].Item2.Invoke(spline, so);
                            cachedRect.height = rect.height;
                            cachedRect.width = rect.width;
                            break;
                        }
                    }
                }

                //Expand window for title.
                guiContentContainer.text = windowTitle;
                Vector2 labelSize = LibraryGUIStyle.textHeader.CalcSize(guiContentContainer);
                labelSize.x += LibraryGUIStyle.textHeader.padding.left + LibraryGUIStyle.textHeader.padding.right + 70;
                if (labelSize.x > cachedRect.width) cachedRect.width = labelSize.x;
            }
            else if(spline.selectedControlPoint != 0)
            {
                if (spline.selectedAnchorMenu == "general")
                {
                    cachedRect.width = 270;
                    cachedRect.height = menuItemHeight * 6 + 10;
                }
                else if (spline.selectedAnchorMenu == "deformation")
                {
                    cachedRect.height = menuItemHeight * 10 + 10;

                    if (spline.loop && spline.normalType == Spline.NormalType.DYNAMIC && spline.selectedControlPoint == 1000)
                        cachedRect.height += menuItemHeight;

                    cachedRect.width = 272;
                }
                else
                {
                    //Addons
                    for (int i = 0; i < addonsCalcWindowSizeCp.Count; i++)
                    {
                        if (spline.selectedAnchorMenu == addonsCalcWindowSizeCp[i].Item1)
                        {
                            Rect rect = addonsCalcWindowSizeCp[i].Item2.Invoke(spline);
                            cachedRect.height = rect.height;
                            cachedRect.width = rect.width;
                            break;
                        }
                    }
                }
            }
        }

        public static void AddSubMenuControlPoint(string id, GUIContent button, GUIContent buttonActive, Action<Spline, int> DrawWindow, Func<Spline, Rect> calcWindowSize)
        {
            for (int i = 0; i < addonsDrawWindowCp.Count; i++)
            {
                if (addonsDrawWindowCp[i].Item1 == id)
                {
                    addonsDrawWindowCp.RemoveAt(i);
                    addonsButtonsCp.RemoveAt(i);
                    addonsCalcWindowSizeCp.RemoveAt(i);
                }
            }

            addonsDrawWindowCp.Add((id, DrawWindow));
            addonsButtonsCp.Add((id, button, buttonActive));
            addonsCalcWindowSizeCp.Add((id, calcWindowSize));
        }

        public static void AddSubMenuSplineObject(string id, GUIContent button, GUIContent buttonActive, Action<Spline, SplineObject> DrawWindow, Func<Spline, SplineObject, Rect> calcWindowSize)
        {
            for (int i = 0; i < addonsDrawWindowSo.Count; i++)
            {
                if (addonsDrawWindowSo[i].Item1 == id)
                {
                    addonsDrawWindowSo.RemoveAt(i);
                    addonsButtonsSo.RemoveAt(i);
                    addonsCalcWindowSizeSo.RemoveAt(i);
                }
            }

            addonsDrawWindowSo.Add((id, DrawWindow));
            addonsButtonsSo.Add((id, button, buttonActive));
            addonsCalcWindowSizeSo.Add((id, calcWindowSize));
        }
    }
}
